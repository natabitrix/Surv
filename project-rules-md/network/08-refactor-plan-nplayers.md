# План рефакторинга под N игроков (Путь B)

> Цель: убрать синглтоны из per-player менеджеров, сделать `LocalPlayer` + `AllPlayers`, per-player сейвы. Без сети.
> Тестируется в синглплеере: два префаба игрока в тестовой сцене.
> Правило: после каждого шага — тест → коммит на русском → обновить `03-current-state.md`. Есть баги — чиним до перехода.

---

## Общая картина: что меняем и почему

| Файл | Сейчас | Станет | Зачем |
|---|---|---|---|
| `PlayerController` | `Instance` (синглтон, `Destroy` дубликата) | `LocalPlayer` (свой) + `AllPlayers` (список) | Второй игрок не удаляется |
| `PlayerSurvivalSystem` | `Instance` (синглтон, `Destroy` дубликата) | per-player (компонент на игроке) | У каждого свой Health/Stamina |
| `PlayerProgress` | `Instance` (один на игру, один файл) | `GetPlayerProgress(playerId)`, `Player_{playerId}.save` | Разные уровни/инвентари/позиции |
| `InventoryManager` | `Instance` (синглтон) | per-player (компонент на игроке) | Разные инвентари |
| `ChestUIManager` | `Instance` (синглтон) | `LocalPlayer`-scoped (какой сундук открыт у меня) | Не открывать чужой сундук |
| `GameAssets` | Создаёт один `PlayerProgress` | Разводит глобальные и per-player | Глобальные — одни, per-player — на каждого |

**Ключевая идея:** `Instance` → два разных понятия:
- **`LocalPlayer`** — «мой игрок» (для UI, ввода, камеры). Один на клиента.
- **`AllPlayers`** — «все игроки в мире» (для AI, репликации). Список на сервере/хосте.

В синглплеере `LocalPlayer == AllPlayers[0]`. В коопе — `LocalPlayer` у каждого свой, `AllPlayers` — общий список.

---

## Принятые решения (по 4 вопросам)

1. **`_playerId`** — сериализованное поле `[SerializeField] private string _playerId = "player_001";`. В синглплеере — один игрок. В коопе — сервер назначит при спавне.
2. **`LocalPlayer`** — первый в `Awake` (`if (LocalPlayer == null) LocalPlayer = this;`). Временно, до NGO. Потом — явная регистрация из `WorldBootstrap` / при спавне своего игрока.
3. **`AllPlayers`** — `List<PlayerController>`. 2–4 игрока, O(n) — не проблема. Порядок сохраняется.
4. **Ввод** — только `LocalPlayer` (`if (this != LocalPlayer) return;` в `Update`). Второй игрок в синглплеере — «болванчик» (живой, но без ввода).

---

## Шаг 0. Подготовка (без изменений кода)

**Что делаем:**
1. Создать тестовую сцену `TestScene_TwoPlayers` (или использовать `GameWorld` с двумя префабами).
2. Создать два префаба игрока (или один префаб, два экземпляра в сцене).
3. Убедиться, что оба спавнятся (сейчас — нет, второй удаляется).

**Тест:** запустить сцену, увидеть в консоли `[PlayerController] Destroy(gameObject)` для второго. Это подтверждение бага, а не ошибка.

**Коммит:** не нужен (только тестовая сцена, не в git или в `.gitignore`).

---

## Шаг 1. `PlayerController` — убрать `Destroy`, `LocalPlayer` + `AllPlayers`

**Файл:** `Assets/Scripts/Player/PlayerController.cs`

**Что меняем:**

Было:

    public static PlayerController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // ...
        PlayerProgress.Instance?.RegisterPlayerController(this);
    }

    private void OnDestroy()
    {
        if (PlayerProgress.Instance != null && PlayerProgress.Instance.playerController == this)
            PlayerProgress.Instance.UnregisterPlayerController();
        if (Instance == this) Instance = null;
    }

Стало:

    public static PlayerController LocalPlayer { get; private set; }
    public static readonly List<PlayerController> AllPlayers = new();

    [SerializeField] private string _playerId = "player_001";
    public string PlayerId => _playerId;

    private void Awake()
    {
        if (!AllPlayers.Contains(this))
            AllPlayers.Add(this);

        if (LocalPlayer == null)
            LocalPlayer = this;

        // ... остальная инициализация ...
        PlayerProgress.Instance?.RegisterPlayerController(this); // временно, Шаг 3 заменит
    }

    private void OnDestroy()
    {
        AllPlayers.Remove(this);
        if (LocalPlayer == this) LocalPlayer = null;
        PlayerProgress.Instance?.UnregisterPlayerController();
    }

**Важно:**
- `Instance` больше нет. Все, кто использовал `PlayerController.Instance`, должны использовать `LocalPlayer` (UI/ввод) или `AllPlayers` (AI/репликация).
- `PlayerProgress.Instance` — временно (Шаг 3 заменит на `GetPlayerProgress(_playerId)`).

**Что сломается:** все `PlayerController.Instance` в проекте. Поиск: `grep -rn "PlayerController.Instance" Assets/Scripts/`. Заменить:
- UI/ввод/камера → `LocalPlayer`
- AI/мир/«ближайший игрок» → `AllPlayers` (или `FindClosestPlayer()`)

**Тест:**
1. Два префаба в сцене — оба живы, не удаляются.
2. `AllPlayers.Count == 2`, `LocalPlayer != null`.
3. Управление работает только у `LocalPlayer` (второй — без ввода, но живой).

**Коммит:** `Шаг 1: PlayerController — LocalPlayer + AllPlayers, убран Destroy`

---

## Шаг 2. `PlayerSurvivalSystem` — убрать `Destroy`, per-player

**Файл:** `Assets/Scripts/Core/PlayerSurvivalSystem.cs`

**Что меняем:**

Было:

    public static PlayerSurvivalSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

Стало:

    // Instance больше нет — компонент на каждом игроке
    // Доступ: playerController.GetComponent<PlayerSurvivalSystem>()

    private void Awake()
    {
        // НЕ удаляем — каждый игрок со своим выживанием
    }

**Важно:**
- `PlayerSurvivalSystem` — компонент на игроке, не глобальный менеджер.
- Все, кто использовал `PlayerSurvivalSystem.Instance`, должны получать его через `PlayerController`:
  - `LocalPlayer.GetComponent<PlayerSurvivalSystem>()` — для UI
  - `playerController.GetComponent<PlayerSurvivalSystem>()` — для урона

**Поиск:** `grep -rn "PlayerSurvivalSystem.Instance" Assets/Scripts/`

**Тест:**
1. У каждого игрока свой `Health`, `Food`, `Water`.
2. Урон одному не меняет `Health` другого.
3. Смерть одного не вызывает `ReloadFromFile` у другого.

**Коммит:** `Шаг 2: PlayerSurvivalSystem — per-player, убран Instance`

---

## Шаг 3. `PlayerProgress` — per-player (`playerId` → прогресс)

**Файл:** `Assets/Scripts/Core/PlayerProgress.cs`

**Что меняем:**

Было:

    public static PlayerProgress Instance { get; private set; }

    public void Initialize(ItemDatabase itemDb, RecipeDatabase recipeDb)
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // ...
    }

Стало:

    private static readonly Dictionary<string, PlayerProgress> _progressByPlayerId = new();
    public static PlayerProgress GetPlayerProgress(string playerId)
        => _progressByPlayerId.TryGetValue(playerId, out var p) ? p : null;

    public string PlayerId { get; private set; }

    public void Initialize(ItemDatabase itemDb, RecipeDatabase recipeDb, string playerId)
    {
        PlayerId = playerId;
        if (_progressByPlayerId.ContainsKey(playerId))
        {
            Debug.LogError($"[PlayerProgress] Дубликат playerId '{playerId}'!");
            return;
        }
        _progressByPlayerId[playerId] = this;
        // ...
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(PlayerId))
            _progressByPlayerId.Remove(PlayerId);
    }

**Сейв:**

Было:

    private const string SAVE_FILE_NAME = "Player.save";

Стало:

    private string GetSaveFileName() => $"Player_{PlayerId}.save";

**`GameAssets.CreateGlobalManagers`:**

Было:

    CreateChildManager<PlayerProgress>("PlayerProgress", m =>
        m.Initialize(_itemDatabase, _recipeDatabase));

Стало:

    // PlayerProgress больше НЕ создаётся в GameAssets (он per-player).
    // Создаётся на каждом игроке в сцене (или при спавне игрока).

**Важно:**
- `PlayerProgress` теперь не глобальный менеджер, а per-player.
- `GameAssets` больше не создаёт `PlayerProgress`. Он создаётся на игроке (или при спавне).
- `PlayerController.Awake` регистрируется в `GetPlayerProgress(_playerId)` (Шаг 1).

**Поиск:** `grep -rn "PlayerProgress.Instance" Assets/Scripts/`

**Тест:**
1. Два игрока — разные `PlayerProgress`, разные уровни/энграммы.
2. Сейв одного — `Player_player_001.save`, другого — `Player_player_002.save`.
3. Загрузка — каждый берёт свой файл.
4. `PlayerProgress.GetPlayerProgress("player_001")` возвращает правильный.

**Коммит:** `Шаг 3: PlayerProgress — per-player, Player_{playerId}.save`

---

## Шаг 4. `InventoryManager` — per-player

**Файл:** `Assets/Scripts/InventorySystem/InventoryManager.cs`

**Что меняем:**

Было:

    public static InventoryManager Instance { get; private set; }

    private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; }

Стало:

    // Instance больше нет — компонент на каждом игроке
    // Доступ: playerController.GetComponent<InventoryManager>()

    private PlayerProgress _playerProgress;
    public PlayerProgress PlayerProgress => _playerProgress;

    private void Awake()
    {
        // НЕ удаляем — каждый игрок со своим инвентарём
    }

    private IEnumerator Start()
    {
        _playerProgress = PlayerProgress.GetPlayerProgress(GetComponent<PlayerController>().PlayerId);
        while (_playerProgress == null) yield return null;
        _playerProgress.RegisterInventoryManager(this);
        // ...
    }

**Важно:**
- `InventoryManager` — компонент на игроке, не глобальный.
- `PlayerProgress.Instance` → `_playerProgress` (свой).
- `ChestUIManager` — пока оставляем синглтоном (Шаг 6), но `CurrentSource` должен быть per-player.

**Поиск:** `grep -rn "InventoryManager.Instance" Assets/Scripts/`

**Тест:**
1. Два игрока — разные инвентари.
2. Добавление предмета одному не меняет инвентарь другого.
3. Сейв — per-player (через `PlayerProgress`).

**Коммит:** `Шаг 4: InventoryManager — per-player, убран Instance`

---

## Шаг 5. `GameAssets` — развести глобальные и per-player

**Файл:** `Assets/Scripts/Core/GameAssets.cs`

**Что меняем:**

Было:

    private void CreateGlobalManagers()
    {
        CreateChildManager<GameController>("GameController", m => m.Initialize(_gameSettings));
        CreateChildManager<PlayerProgress>("PlayerProgress", m => m.Initialize(_itemDatabase, _recipeDatabase));  // убрать
        CreateChildManager<GameTimeUpdater>("GameTimeUpdater");
        GameTime.Initialize();
    }

Стало:

    private void CreateGlobalManagers()
    {
        // Глобальные (одни на игру/сервер)
        CreateChildManager<GameController>("GameController", m => m.Initialize(_gameSettings));
        CreateChildManager<GameTimeUpdater>("GameTimeUpdater");
        GameTime.Initialize();

        // Per-player (PlayerProgress, InventoryManager) — создаются на игроках, не здесь
    }

**Важно:**
- `GameAssets` — только глобальные.
- `PlayerProgress` / `InventoryManager` — на игроках.
- `WorldBootstrap` (загрузка GameWorld) — должен создавать `PlayerProgress` для `LocalPlayer` при спавне (или `PlayerController` сам создаёт).

**Тест:**
1. Запуск `GameWorld` — `GameAssets` создаёт только глобальные.
2. `LocalPlayer` — имеет свой `PlayerProgress`.
3. Второй игрок — свой `PlayerProgress`.

**Коммит:** `Шаг 5: GameAssets — разведены глобальные и per-player менеджеры`

---

## Шаг 6. `ChestUIManager` — `LocalPlayer`-scoped

**Файл:** `Assets/Scripts/InventorySystem/ChestUIManager.cs`

**Что меняем:**

Было:

    public static ChestUIManager Instance { get; private set; }
    public IInventorySource CurrentSource { get; private set; }

Стало:

    // Instance остаётся (UI один на клиента), но CurrentSource — per-player
    public IInventorySource CurrentSource { get; private set; }
    // Источник открыт только для LocalPlayer. Другие игроки не видят.

**Важно:**
- `ChestUIManager` — один на клиента (UI один). Но `CurrentSource` — «какой сундук открыт у меня».
- Данные сундука — серверные (см. аудит 1). Пока — локальные (синглплеер).

**Тест:**
1. Два игрока — каждый может открыть свой сундук.
2. Открытие сундука одним не открывает его у другого.

**Коммит:** `Шаг 6: ChestUIManager — CurrentSource per-player (локально)`

---

## Шаг 7. Финальный тест «два игрока в одной сцене»

**Что проверяем:**
1. Два префаба игрока в сцене — оба живы.
2. `LocalPlayer` — один (свой), `AllPlayers.Count == 2`.
3. У каждого свой `Health`, `Food`, `Water` — урон не перекидывается.
4. У каждого свой `PlayerProgress` — разные уровни, разные сейвы.
5. У каждого свой `InventoryManager` — разные инвентари.
6. `ChestUIManager.CurrentSource` — у каждого свой.
7. AI (если есть) видит обоих игроков (через `AllPlayers`).

**Если всё работает — Шаг 7 закрыт. Дальше — сеть (NGO + Relay).**

**Коммит:** `Шаг 7: два игрока в одной сцене — тест пройден`

---

## Риски и что делать, если сломалось

| Риск | Что делать |
|---|---|
| `PlayerController.Instance` используется в 50 местах | Найти все `grep`, заменить на `LocalPlayer` (UI/ввод) или `AllPlayers` (AI). Не торопиться, идти по файлам. |
| `PlayerProgress.Instance` в `InventoryData.AddItemAnywhere` | Убрать `Save()` из `AddItemAnywhere` (см. аудит 1). `Save()` — на `PlayerProgress` (per-player). |
| `PlayerSurvivalSystem.Instance` в `CreatureCombat.DealDamageToTarget` | Заменить на `target.GetComponent<PlayerSurvivalSystem>()`. Урон — по цели, не по синглтону. |
| `PlayerProgress.Save()` пишет один файл | Per-player `Player_{playerId}.save` (Шаг 3). |
| `GameTimeUpdater` тикает у каждого | Пока оставить (синглплеер). При сети — только на сервере (аудит 3). |
| `ChestUIManager.CurrentSource` — глобальный | `LocalPlayer`-scoped (Шаг 6). |
| Два игрока не сталкиваются | `CharacterController` не сталкивается с `CharacterController`. Это нормально для синглплеера; в коопе — `NetworkTransform` + серверная физика. |

---

## Порядок работ (сводка)

    Шаг 0: Тестовая сцена (два префаба) — подтвердить баг
    Шаг 1: PlayerController — LocalPlayer + AllPlayers
    Шаг 2: PlayerSurvivalSystem — per-player
    Шаг 3: PlayerProgress — per-player, Player_{playerId}.save
    Шаг 4: InventoryManager — per-player
    Шаг 5: GameAssets — развести глобальные и per-player
    Шаг 6: ChestUIManager — CurrentSource per-player
    Шаг 7: Финальный тест «два игрока»

**После каждого шага:** тест → коммит на русском → обновить `03-current-state.md`.