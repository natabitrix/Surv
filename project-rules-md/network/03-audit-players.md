# Аудит 3: Игроки и сессия

> Система: игроки (`PlayerController`, `PlayerSurvivalSystem`), прогресс (`PlayerProgress`), сессия (`SessionMode`, `GameTime`), глобальные менеджеры (`GameAssets`).
> Файлы: `PlayerController.cs`, `PlayerSurvivalSystem.cs`, `PlayerProgress.cs`, `GameAssets.cs`, `SessionMode.cs`, `GameTime.cs`.
> Статус: ✅ аудит завершён.

---

## Цепочка владения
GameAssets (глобальный, DontDestroyOnLoad)
├── Awake: Instance = this, DontDestroyOnLoad, CreateGlobalManagers()
├── GameController, PlayerProgress, GameTimeUpdater — дочерние GameObject'ы
└── GameTime.Initialize() ← SessionMode внутри GameTime

PlayerProgress (глобальный, DontDestroyOnLoad, ОДИН на игру)
├── _level, _experience, _engramPoints, _statLevels
├── hotbarInventoryData, mainInventoryData
├── _playerController : PlayerController ← ОДНА ссылка
├── _inventoryManager : InventoryManager ← ОДНА ссылка
├── Initialize(itemDb, recipeDb) → LoadData()
├── RegisterPlayerController(controller)
├── RegisterInventoryManager(manager)
├── Save() / LoadData() / ReloadFromFile()
└── Instance — статический

PlayerController (сценовый, ===Player===, ОДИН)
├── Awake: Instance = this (синглтон, Destroy дубликата)
├── Awake: PlayerProgress.Instance?.RegisterPlayerController(this)
├── _playerSurvivalSystem = PlayerSurvivalSystem.Instance
├── _input, _animator, _controller
└── IsMounted, MoveInput, LookInput, SprintInput — геттеры из _input

PlayerSurvivalSystem (сценовый, Instance — статический, ОДИН)
├── Awake: Instance = this (Destroy дубликата)
├── Start: LoadFrom(PlayerProgress.Instance.GetLoadedSaveData())
├── Update: смерть (DieManually || health <= 0)
├── TakeDamage, AddFood, AddWater, RecoveryHealth
├── SaveTo(saveData) / LoadFrom(saveData)
├── OnPlayerDeathRoutine: создаёт труп через CorpseManager.Instance
└── TogglePlayerActive(pc) — отключает компоненты игрока

SessionMode (статический)
├── IsMultiplayer => _settings.isMultiplayer
└── Initialize(GameSettings) — вызывается из GameAssets?


**Ключевое наблюдение:** всё — синглтоны. `PlayerProgress.Instance`, `PlayerController.Instance`, `PlayerSurvivalSystem.Instance`, `InventoryManager.Instance`, `ChestUIManager.Instance`. При коопе 2–4 игроков эти `Instance` должны стать per-player, иначе второй игрок перезапишет первого.

---

## Вопрос А. Что при одновременном действии двух игроков?

| Сценарий | Что произойдёт в текущем коде |
|---|---|
| Два игрока в одной сцене | `PlayerController.Awake` второго игрока: `Instance != null && Instance != this` → **`Destroy(gameObject)`**. Второй игрок **удаляется**. Кооп невозможен в принципе. |
| Два игрока регистрируются в `PlayerProgress` | `RegisterPlayerController` перезаписывает `_playerController`. Первый игрок «теряется». `PlayerProgress.LateUpdate` кэширует позицию **последнего** контроллера. |
| Два игрока имеют `PlayerSurvivalSystem` | `Awake` второго: `Instance != null` → **`Destroy(gameObject)`**. Второй игрок без выживания. |
| Два игрока открывают инвентарь | `InventoryManager.Instance` — один. `_selectedSlotIndex` — один. Второй игрок перезаписывает выбор первого. `ChestUIManager.CurrentSource` — один. |
| Один игрок умирает, другой — нет | `PlayerSurvivalSystem.OnPlayerDeathRoutine` вызывает `PlayerProgress.Instance.ReloadFromFile()` — это **глобальный** прогресс. Второй игрок потеряет свой прогресс. |
| `PlayerProgress.Save()` | `Save()` пишет **один** файл `Player.save`. Второй игрок перезапишет файл первого. |
| `GameAssets.CreateGlobalManagers` | Создаёт **один** `PlayerProgress`. Второй игрок должен получить свой — но `CreateChildManager<PlayerProgress>` вызывается один раз. |
| `GameTime.Now` | Один на игру. При коопе — должен быть серверным временем, но `GameTimeUpdater` тикает локально у каждого. |

**Вывод:** текущая архитектура физически не поддерживает N игроков. Не «есть гонки», а «второй игрок удаляется при Awake». Это блокер №1 для коопа.

---

## Вопрос Б. Может ли клиент соврать?

| Действие | Где проверка | Можно ли соврать |
|---|---|---|
| Двигать себя | `PlayerController` двигает `transform` локально (CharacterController) | Да, клиент полностью авторитетен по своей позиции |
| Наносить урон | `PlayerController.Attack` → `equipment.UseCurrentTool` → `PlayerInteraction.OnAttackInteractFinished` → `Creature.TakeDamage` | Да, клиент решает, попал ли он (OverlapBox локально) |
| Получать урон | `PlayerSurvivalSystem.TakeDamage` — публичный | Да, клиент может «не получить» урон (не вызвать метод) |
| Умирать | `PlayerSurvivalSystem.Update` проверяет `_health <= 0` | Да, клиент решает, умер ли он (может не допустить `_health <= 0`) |
| Сохранять прогресс | `PlayerProgress.Save()` пишет локальный файл | Да, клиент может подделать файл |
| Распределять статы | `PlayerProgress.AllocateStatPoint` — публичный | Да (см. аудит 1) |
| `SessionMode.IsMultiplayer` | Читается из `GameSettings.isMultiplayer` — ScriptableObject, локальный ассет | Да, клиент может выставить `isMultiplayer = false` и играть «в синглплеере» на сервере |
| `GameTime.Now` | SinglePlayer: локальное накопленное время; Multiplayer: `DateTimeOffset.UtcNow` | В мультиплеере — клиентское UTC, не серверное. Рассинхрон времени между клиентами. |

**Вывод:** клиент полностью авторитетен по всему: позиция, урон, смерть, прогресс, время. Для коопа нужен серверный слой, который:
1. Валидирует позицию (скорость, телепорт).
2. Валидирует урон (дистанция, кулдаун, попадание).
3. Решает смерть (серверное HP).
4. Хранит прогресс (серверный сейв).
5. Даёт серверное время.

---

## Вопрос В. Насколько критична задержка?

| Операция | Критичность | Комментарий |
|---|---|---|
| Движение игрока | Высокая | 100 мс задержки = «резиновый» персонаж. Нужен client-side prediction + server reconciliation. |
| Поворот камеры | Низкая | Локально, не реплицируется (только для наблюдателей) |
| Атака (анимация) | Низкая | Локально, но урон — по серверу |
| Получение урона | Высокая | Если клиент «не знает» об уроне, он не проиграет анимацию. Сервер должен присылать `ClientRpc TakeDamage` |
| Смерть | Высокая | Сервер решает, клиент показывает экран смерти по `ClientRpc` |
| Прогресс (XP, уровень) | Средняя | Можно показывать локально, сервер корректирует |
| Инвентарь | Средняя | См. аудит 1 |
| Время (`GameTime.Now`) | Средняя | Должно быть серверным; клиент синхронизирует часы |

**Вывод:** движение и урон критичны к задержке, но требуют серверной валидации. Идеал: client-side prediction для движения (клиент двигает локально, сервер корректирует), server-authoritative для урона/смерти.

---

## Таблица разметки

| Система | Кто авторитетен | Почему | Что переделать |
|---|---|---|---|
| Игроки: количество (`PlayerController.Instance`, `PlayerSurvivalSystem.Instance`) | Сервер (спавн) + клиент (свой игрок) | Сейчас `Awake` удаляет второго игрока. Кооп невозможен | Убрать синглтон-паттерн из `PlayerController` / `PlayerSurvivalSystem`. Каждый игрок — свой экземпляр. `Instance` → `LocalPlayer` (только свой) + `AllPlayers` (список на сервере). |
| Прогресс игрока (`PlayerProgress.Instance`) | Сервер | Один `PlayerProgress` на игру. Второй игрок перезапишет первого | `PlayerProgress` → per-player (по `connectionId` / `playerId`). `Instance` → `GetPlayerProgress(playerId)`. Сейв — per-player файл. |
| Инвентарь игрока (`InventoryManager.Instance`) | Сервер | Один `InventoryManager`. Второй игрок перезапишет выбор первого | `InventoryManager` → per-player. `Instance` → `GetInventoryManager(playerId)`. |
| Выживание (`PlayerSurvivalSystem.Instance`) | Сервер | Один `PlayerSurvivalSystem`. Второй игрок удаляется | Per-player. `Health` → `NetworkVariable`. `TakeDamage` → серверный. |
| Позиция игрока (`PlayerController.transform`) | Клиент + валидация сервера | Client-side prediction для отзывчивости, сервер проверяет скорость/телепорт | `NetworkTransform` (клиент-авторитарный) + серверная проверка `maxSpeed`. CharacterController — локально. |
| Камера / ввод (`PlayerInputHandler`, `_input`) | Клиент | Только для своего игрока | Не реплицируется. `PlayerInput` — только на локальном игроке. |
| Атака игрока (`PlayerController.Attack` → `PlayerInteraction`) | Сервер (расчёт) + клиент (анимация) | Клиент решает, попал ли (OverlapBox локально) | `ServerRpc RequestAttack(targetId, hitPoint)`. Сервер считает урон (см. аудит 2). |
| Смерть игрока (`PlayerSurvivalSystem.OnPlayerDeathRoutine`) | Сервер | Создаёт труп, чистит инвентарь, `ReloadFromFile` | Серверный `Die()`. `ClientRpc OnPlayerDied`. `ReloadFromFile` → только на сервере. |
| Сохранение (`PlayerProgress.Save`) | Сервер/хост | Один файл `Player.save` | Per-player файл. На клиенте — no-op. |
| Время (`GameTime.Now`, `GameTimeUpdater`) | Сервер | Клиентское UTC рассинхронизировано | `GameTime.Now` → серверное время (синхронизация часов). `GameTimeUpdater` — только на сервере. |
| Режим сессии (`SessionMode.IsMultiplayer`) | Сервер | `GameSettings.isMultiplayer` — локальный ассет, клиент может подделать | `SessionMode` → серверный флаг. `GameSettings` — только для дефолтов. |
| Глобальные менеджеры (`GameAssets.CreateGlobalManagers`) | Сервер (создание) | Один `PlayerProgress` на игру | Развести: глобальные (GameController, GameTime) — на сервере; per-player (PlayerProgress, InventoryManager) — на каждого. |

---

## Ключевые выводы

1. **Синглтон-паттерн — главный блокер.** `PlayerController.Instance`, `PlayerSurvivalSystem.Instance`, `PlayerProgress.Instance`, `InventoryManager.Instance` — все удаляют второй экземпляр. До коопа нужно убрать `Destroy(gameObject)` в `Awake` и заменить `Instance` на `LocalPlayer` + `AllPlayers`.

2. **`PlayerProgress` — один на игру.** Второй игрок перезапишет прогресс первого. Нужен per-player прогресс с ключом по `playerId`.

3. **`PlayerProgress.Save()` пишет один файл.** Второй игрок перезапишет сейв первого. Нужен per-player файл (`Player_{playerId}.save`).

4. **`PlayerSurvivalSystem.OnPlayerDeathRoutine` вызывает `PlayerProgress.Instance.ReloadFromFile()`** — глобальный откат. Второй игрок потеряет прогресс. При сети — только серверный откат для умершего игрока.

5. **`GameTime.Now` в мультиплеере использует `DateTimeOffset.UtcNow`** — клиентское время, не серверное. Рассинхрон между клиентами. Нужна синхронизация часов с сервером.

6. **`SessionMode.IsMultiplayer` читает `GameSettings.isMultiplayer`** — локальный ScriptableObject. Клиент может подделать. Нужен серверный флаг.

7. **`GameAssets.CreateGlobalManagers` создаёт одного `PlayerProgress`** — нужен per-player для коопа. Глобальные (GameController, GameTime) — на сервере; per-player (PlayerProgress, InventoryManager) — на каждого.

8. **`PlayerController.Awake` регистрируется в `PlayerProgress.Instance`** — при N игроках нужна регистрация по `playerId`.

---

## Переделать до сети

- [ ] `PlayerController.Awake` — убрать `Destroy(gameObject)` для дубликата; `Instance` → `LocalPlayer` (только свой) + `AllPlayers` (список).
- [ ] `PlayerSurvivalSystem.Awake` — убрать `Destroy(gameObject)`; `Instance` → per-player.
- [ ] `PlayerProgress.Instance` — per-player: `GetPlayerProgress(playerId)`. Сейв — `Player_{playerId}.save`.
- [ ] `InventoryManager.Instance` — per-player: `GetInventoryManager(playerId)`.
- [ ] `PlayerProgress.RegisterPlayerController` — регистрация по `playerId`, не по singleton.
- [ ] `PlayerProgress.RegisterInventoryManager` — то же.
- [ ] `PlayerSurvivalSystem.OnPlayerDeathRoutine` — `ReloadFromFile` только для умершего игрока (серверный).
- [ ] `PlayerProgress.Save` — no-op на клиенте; per-player файл на сервере.
- [ ] `GameTime.Now` — серверное время; синхронизация часов с сервером.
- [ ] `GameTimeUpdater` — только на сервере.
- [ ] `SessionMode.IsMultiplayer` — серверный флаг; `GameSettings.isMultiplayer` — только дефолт.
- [ ] `GameAssets.CreateGlobalManagers` — развести: глобальные (GameController, GameTime) на сервере; per-player (PlayerProgress, InventoryManager) на каждого.
- [ ] `PlayerController.transform` — client-side prediction + `NetworkTransform` + серверная валидация скорости.
- [ ] `PlayerController.Attack` — `ServerRpc RequestAttack(targetId, hitPoint)`; сервер считает урон.
- [ ] `PlayerSurvivalSystem.TakeDamage` — `internal`, вход через серверный метод; `Health` → `NetworkVariable`.
- [ ] `PlayerSurvivalSystem.Update` — проверка смерти только на сервере.
- [ ] `PlayerInputHandler` / `PlayerInput` — только на локальном игроке (не на сервере).
- [ ] `PlayerProgress.AllocateStatPoint` / `TrySpendEngramPoints` — серверные (см. аудит 1).