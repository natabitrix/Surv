# 03. Current State — что работает и как устроено

> **Карта архитектуры и инвариантов.** Не пересказ кода — только то, что НЕ очевидно из файлов.
> Обновляется после каждого закрытого пункта.

---

## 0. Архитектура менеджеров

**Три уровня жизни:**

| Уровень | Кто | Где | DontDestroyOnLoad |
|---|---|---|---|
| **Глобальные** | `GameController`, `PlayerProgress`, `GameTimeUpdater`, `LoadingScreenManager`, `AudioManager`, `LanguageManager` | `[GameAssets]` (обе сцены) | ✅ (через родителя) |
| **Сценовые (мир)** | `CorpseManager`, `LootBagManager`, `TamingManager`, `WorldManager` | `===WorldManagers===` (GameWorld), создаются `WorldBootstrap` через `AddComponent` | ❌ |
| **Сценовые (UI/игрок)** | `HUDManager`, `UIManager`, `PauseManager`, `DeathScreenManager`, `ChestUIManager`, `InventoryManager`, `PanelsUIController`, `PlayerSurvivalSystem`, `RadialMenuController` и др. | `===GameManagers===`, `===InventoryManager===`, `===PanelsController===`, `---Player---` | ❌ |

**Инварианты:**
- `[GameAssets]` **есть в обеих сценах**. При загрузке второй — `Destroy(gameObject)` (защита от дубликата). Флаг `_isPrimary` защищает `Instance` в `OnDestroy`.
- `WorldBootstrap` ждёт `GameAssets.Instance != null` в корутине `Start` — порядок `Awake` между объектами не гарантирован.
- `PlayerSurvivalSystem` — **сценовый**, в `Start()` берёт `PlayerProgress.GetLoadedSaveData()`.
- `PlayerController` регистрируется в `PlayerProgress` в `Awake`, дерегистрируется в `OnDestroy`.

**Bootstrap-порядок загрузки GameWorld:**
1. `WorldBootstrap.Start` → ждёт `GameAssets`.
2. `CreateWorldManagers()` — AddComponent + Initialize для 4 сценовых.
3. Ждёт `PlayerProgress.playerController`.
4. `LoadingScreenManager.RegisterTask(...)` × 4: трупы → сумки → прирученные → мир.
5. `LoadingScreenManager.StartLoading()`.

---

## 1. ScriptableObject базы данных

`Assets/Data/`:
- `GameSettings` — `isMultiplayer`, `corpseLifetime`, `lootBagLifetime`, `arrowLifetime`, `arrowPickupDelay`, `arrowPoolPrewarm`, `deathScreenFreezesWorld`, `singleplayerPauseTimeScale`.
- `ItemDatabase` — `allItems[]`, `ItemLookup` (lazy).
- `RecipeDatabase` — `AllRecipes[]`, `GetRecipeByName`, `GetRecipeForItem`.
- `CreatureDatabase` — `Lookup` (lazy), `GetCreature(id)`.
- `NotificationSoundConfig` — `entries[] {type, clip, volume}`.
- `RadialMenuGlobalConfig` — `tamedMenu`, `knockedOutMenu`, `corpseMenu`, `structureMenu`, `chestMenu`.
- `PlayerMovementSettings` — все настройки движения.
- `LevelTable.json` (Resources) — XP и энграмм-очки по уровням.
- `StatConfig.json` (Resources/Configs) — статы (base, rate, отображение).

**Инварианты:**
- Все базы — **ассеты**, НЕ MonoBehaviour. Ссылки — в `[GameAssets]`.
- `Lookup` строится лениво, кэшируется. `#if UNITY_EDITOR OnValidate()` сбрасывает кэш.

---

## 2. Инвентарь и ChestUI

**Файлы:** `InventorySystem/*.cs`

**Что делает:** `InventoryData` (слоты) + `InventoryManager` (логика игрока) + `ChestUI` (UI контейнеров) + `ChestUIManager` (singleton-роутер).

**Ключевые инварианты:**
- **`ChestUIManager` — единственная легальная точка Open/Close инвентарей.** `ChestUI.OpenWith` / `Close` напрямую **не вызывать**.
- `ChestUIManager.CurrentSource` — единственный источник правды «какой инвентарь открыт». `ChestUI` больше не знает, кто его открыл.
- `IInventorySource : IInteractable` — реализуют `Corpse`, `Creature`, `ChestController`, `LootBag`, `Structure`. `OnInventoryOpened/Closed` вызываются менеджером.
- `ChestUI.Close()` **принудительно** вызывает `SetSlot(null)` для всех слотов. Иначе остаются «призраки» после закрытия.
- `ChestInventory.IsCorpseInventory()` — определяет по префиксу `saveKey`: `Corpse_`, `PlayerCorpse_`, `CreatureCorpse_`, `TamingCorpse_`, `LootBag_`. Для таких `Save()`/`Load()` — no-op, а `OnDestroy` удаляет `Chest_*.save`, если был.

**Ловушки:**
- `ReferenceEquals(CurrentSource, this)` вместо `==` — иначе CS0252 (Unity Object == null).
- `IInventorySource` **не дублирует** `GetInventory`/`HasInventory` (они уже в `IInteractable`) — иначе CS0108.

---

## 3. Игрок

**Файлы:** `Player/*.cs`

**Ключевые инварианты:**
- **`PlayerController._input.look` обнуляется** после `CameraRotation`. Иначе Input System выдаёт то же значение каждый кадр → yaw/pitch накапливаются бесконечно.
- `PlayerController.Start` сбрасывает `_input.look` и `mouseScrollDelta` — Input System при инициализации может выдать гигантскую дельту.
- `IsMounted` → `CharacterController.enabled = false`, физика не толкает игрока в седле. `RidingController.Mount` переприцепляет игрока к `riderAttachPoint` с компенсацией `lossyScale`.
- `PlayerInteraction.OnAttackInteractFinished` берёт контакт через `OverlapBox` по модели оружия / `OverlapSphere` вокруг рук. Вызывается из Animation Event.
- `GetComponents<IInteractable>()` возвращает и **отключённые** MonoBehaviour — обязательно фильтровать по `.enabled`.
- `PlayerAimingSystem.IsCrosshairVisible` — независимо от `IsAiming`. Прицел виден всегда, когда экипировано ranged-оружие (не в Selfie, не в панелях, не в паузе, не на экране смерти).
- `PlayerEquipment` при экипировке лука ищет `ArrowSpawnPoint` в модели и передаёт в `PlayerRangedCombat`.

**Ловушки:**
- `PlayerInputHandler` обрабатывает `Cancel` (Esc), `Hotbar1..0`, `Interact` (E), `TargetInventory` (F), `Attack` (ЛКМ), `RightClick` (ПКМ) через события. Хотбар — через `OnHotbarSlotPressed` с удержанием.
- При открытии панели `E` в инвентаре **не должна** уходить в мир — `PlayerInteraction.HandleInteractStarted/Ended` проверяет `PanelsUIController.IsInventoryOpened()`.

---

## 4. Трупы

**Файлы:** `Corpses/*.cs`

**Инварианты:**
- **Один префаб = живой + труп.** `Corpse.enabled = false` у живого, `= true` у мёртвого.
- `Corpse.OnDestroy` → только `ForgetCorpse(instanceId)` (in-memory). **Файл не трогает.**
- Файл удаляется **только** через `UnregisterCorpse(instanceId)` — при harvest/despawn.
- При смерти существа `SetLayerRecursively(Corpse)` — иначе `EntityInfoPanel` найдёт труп как живого.
- `CorpseManager.RemoveAliveComponentsFromCreatureCorpse` выставляет `Creature.Health = 0` — для `HealthMaterialBlender`.
- `Corpse.ActivateRagdoll` перед `isKinematic = false` сбрасывает позы в T-позу (`ResetPosesToInitial` + `Physics.SyncTransforms`).

**Ловушки:**
- При загрузке из сейва **слой не сохраняется** — `SpawnCorpseFromData` выставляет `Corpse` вручную.
- `IsQuitting` **удалён** (был утечкой состояния между сценами).

---

## 5. Сумки (LootBag)

**Файлы:** `Loot/LootBag*.cs`

**Инварианты:**
- `LootBag.OnDestroy` → только `ForgetLootBag(instanceId)`. **Файл не трогает.**
- Удаление файла — через `UnregisterLootBag` (в `ForceDespawn`).
- `ForceDespawn` вызывается при: истечении `_bagDisappearTime`, опустошении инвентаря.
- `LootBagManager.CreateLootBagFromItems` — единая точка создания сумки из списка предметов.

---

## 6. Приручение (Taming)

**Файлы:** `Creatures/Components/CreatureTaming.cs`, `Creatures/Taming/*.cs`

**Инварианты:**
- `TamingManager` — сценовый, `DontDestroyOnLoad` убран.
- Папка `persistentDataPath/Taming/`, файлы `taming_{guid}.save`.
- `saveKey` инвентаря нокаута — **`TamingCorpse_{instanceId}`** (единый префикс, чтобы `IsCorpseInventory` его распознавал).
- `TamingInstanceId` создаётся **до** `ChestInventory` — иначе `saveKey` плавает.
- `OnApplicationQuit/Pause/Focus` → `SaveAll()`.
- Периодическое сохранение — раз в 10 сек из `CreatureTaming.Tick` (при `knockedOut || tamed`).
- `LoadAllTamingCreaturesAsync` пересчитывает `torpor` и `food` по `elapsed = GameTime.ElapsedSeconds(savedAt, Now)`. Если нокаутнутый и `torpor <= 0` — файл удаляется, существо не спавнится.
- При смерти — `UnregisterCreature` (удаляет файл) + `Destroy(ChestInventory)`.
- `TamingFromLoad` / `KnockOutFromLoad` — восстанавливают состояние без регистрации.

**Ловушки:**
- `CreatureCombat.DealDamageToTarget` должен вызывать `target.NotifyAttackedBy(_creature)` **на цели**, а не на себе.

---

## 7. Радиальное меню

**Файлы:** `UI/RadialMenuUI/*.cs`, `Interactables/RadialMenu.cs`

**Инварианты:**
- `RadialMenuEntry.children` — `[SerializeReference]` (иначе warning про serialization depth).
- `MAX_DEPTH = 3` (корень + 2 подменю).
- `[Назад]` добавляется автоматически на уровнях > 0.
- Radio → `SetRadioValue` + автовозврат на уровень выше.
- Action → `ExecuteAction` + `Rebuild` (обновить dynamic labels) + `CloseRadialMenu(setPanelMode = actionId == "open_inventory")`.
- `RadialMenu.ExecuteAction` делегирует в `Creature` / `Corpse` / `Structure` / `Chest`.
- `RadialMenuCanvas.sortingOrder = 1`, `HUDCanvas.sortingOrder = 0` — иначе HUD перехватывает raycast.
- `Panel.Image.RaycastTarget = false` для декоративных фонов.

**Ловушки:**
- При закрытии меню → `_input.ForceResetInteract()`. Иначе «зажатая E» продолжает дёргать `OnInteractTriggered` → `UseSelectedSlot` в новом UI.
- Флаг `_interactWasForceReset` защищает от повторного `OnInteractCanceled` при физическом отпускании E.

---

## 8. Боевка существ (CreatureCombat) и Статы

**Файлы:** `Creatures/Components/CreatureCombat.cs`, `Creatures/Creature.cs`, `Creatures/CreatureData.cs`, `Creatures/CreatureGlobalConfig.cs`, `UI/DamageNumber*.cs`

**Архитектура данных:**
- **`CreatureData`** (SO) — уникальные данные вида. Разбит на блоки:
  - `BaseStats` (здоровье, стамина, макс. скорость, макс. урон и т.д.).
  - `CombatProps` (радиус агрессии, дальность, кулдаун).
  - `RiddenProps` (кулдаун атаки верхом).
- **`CreatureGlobalConfig`** (SO, один на игру) — общие настройки для всех существ: наркотики, разрешения на добычу инструментами, радиусы блуждания и т.д. Ссылка лежит в `GameAssets`.
- **`Creature.cs`** — геттеры вычисляют финальные статы (например, `RideSpeed` = `maxSpeed * 3`, `TamedAttackDamage` = `maxDamage * 10`). Это задел на будущую систему прокачки уровней.

**Ключевые инварианты:**
- **Атрибуция урона (`CombatMember`)**: Enum `Player`, `Wild`, `Tamed`, `Mounted`, `Other` передается в `TakeDamage` и `DamageNumberPool` для определения цвета цифры.
  - Свой (Игрок, Прирученный, Верхом) по врагу = **Зеленый**.
  - Дикий по своему (Игроку/Прирученному) = **Желтый**.
  - Дикий по дикому = **урон не отображается** (скрыт в `DamageNumberPool`).
- **Атака верхом**: В `DealDamageToTarget` проверяется `if (_creature.tamed && _creature.Riding.IsRidden)`. Если да — используется `OverlapBox` перед мордой и передается `CombatMember.Mounted`.
- **Поиск целей (Targeting)**: В `FindWildTarget`, `FindNearestEnemy`, `FindEnemyNearPlayer` **обязательно** используется `hit.GetComponentInParent<Creature>()`. 
  - *Ловушка:* `OverlapSphere` часто задевает коллайдеры костей (головы, ног). `GetComponent<Creature>()` вернет `null`, и ИИ проигнорирует цель.
- **Ленивая инициализация `CreatureGlobalConfig`**: В `Creature.cs` свойство `Config` пытается получить `GameAssets.Instance.CreatureGlobalConfig` при первом обращении. Это защищает от `NullReferenceException`, если существо заспавнилось раньше, чем `GameAssets` (порядок `Awake` не гарантирован).
- **Множитель зоны урона**: `hitCollider.GetComponent<CreatureHitZone>()?.damageMultiplier ?? 1f`.
- **Память об атакующем**: `LastAttacker` живет 15 сек (`ATTACKER_MEMORY_DURATION`), потом `null`.
- Цифры урона — Screen Space Overlay, всегда одного размера, не наклоняются.

---

## 9. Оседлание (Riding)

**Файлы:** `Creatures/Components/CreatureRiding.cs`, `Player/RidingController.cs`

**Инварианты:**
- `CreatureRiding.Mount` → `Agent.updateRotation = false` (иначе A/D крутят существо).
- `RidingController.Mount` переприцепляет игрока к `riderAttachPoint` с компенсацией `lossyScale` (иначе scale дино ломает игрока).
- `TickRidden` — прямое управление `Agent.velocity`, не `SetDestination`.
- `ForceDismount` при: `CreatureKnockout`, `CreatureDeath`, `PlayerSurvivalSystem.OnPlayerDeath`, `OnApplicationQuit/Pause/Focus/Destroy` у `RidingController`.
- В Selfie `look = Vector2.zero` — иначе камера крутит существо.

**Ловушки:**
- `E` работает **без наведения на цель**, если `IsMounted` или `_currentlyDraggingCorpse`.

---

## 10. Инфо-панель существ

**Файлы:** `UI/EntityInfoPanel*.cs`

**Инварианты:**
- Raycast из **центра экрана** (`ViewportPointToRay(0.5, 0.5)`), не из курсора.
- `_holdTime = 0.2` (задержка перед показом), `_lingerTime = 1–2` (держится после ухода).
- Игнорирует мёртвых и не-нокаутнутых трупов (`!IsAlive && !knockedOut`).
- Мгновенно переключается на новую цель.

---

## 11. Уведомления (top-note + звуки)

**Файлы:** `UI/Notifications/*.cs`

**Инварианты:**
- `NotificationType`: `None`, `LevelUp`, `TameComplete`.
- `NotificationSoundConfig` — ScriptableObject, ссылка в `GameAssets`.
- `_uiAudioSource` создаётся в `Awake`, если не назначен (2D, `spatialBlend = 0`).
- Если тип не найден — молчание + warning в редакторе (`#if UNITY_EDITOR`).

---

## 12. Время (GameTime) и сохранения

**Файлы:** `Core/GameTime*.cs`

**Инварианты:**
- `GameTime.Now` — SinglePlayer: накопленное игровое время. Multiplayer: Unix timestamp UTC.
- `GameTimeUpdater` тикает только при `timeScale > 0` и `Application.isFocused`.
- **Все системы сохранений используют `savedAtTime` вместо `creationTimeUtc`.** Трупы, сумки, прирученные.
- `GameTime.Save()` вызывается раз в 30 сек + на pause/focus/quit.

---

## 13. Строительство

**Файлы:** `Player/PlayerBuildMode.cs`, `Core/WorldManager.cs`, `Structures/*.cs`

**Инварианты:**
- `WorldManager.RegisterStructure(placed, itemId, parentId = null)` — запись в чанк + `StructureIdentity`.
- Фундамент: два клика (поставить → повернуть мышью → поставить).
- Стены/потолки/двери: один клик по снапу.
- `_previewInstance` — временный объект, все коллайдеры и `SnapPoint` удаляются.
- `_baseObject` — структура, к которой крепимся (для `StructureAttachment`).

**Ловушки:**
- Тэги структур (`Foundation`, `Wall`, ...) должны быть в Tag Manager.
- `ClearExistingStructures` удаляет всё по тегам при загрузке мира.