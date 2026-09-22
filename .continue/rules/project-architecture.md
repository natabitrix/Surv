# Project Architecture - ARK Survival Evolved Clone (Unity 6.5)

This is a survival game clone inspired by ARK: Survival Evolved, built with Unity 6.5 (6000.5.0f1).

## Core Game Systems (ARK-style)

- **Player Character**: 
  - First-person/Third-person controller with locomotion (walk, run, sprint, crouch, prone, swim, climb)
  - Survival stats: Health, Stamina, Oxygen, Food, Water, Weight, Torpidity
  - Temperature system (Hyperthermia/Hypothermia) based on environment and clothing
  - Leveling system with stats allocation and engram points
  
- **World & Environment**:
  - Large open-world map with diverse biomes (beaches, jungles, mountains, caves, swamps, snow)
  - Day/Night cycle with dynamic lighting and time-based events
  - Dynamic weather system (rain, fog, sandstorms, snowstorms)
  - Resource spawn nodes (trees, rocks, bushes, metal deposits, crystal, obsidian, oil)

- **Creature System (Taming & Breeding)**:
  - AI using Behavior Trees or State Machine (Idle, Wander, Flee, Attack, Follow, Defend)
  - Taming mechanics: Knockout taming (torpor-based) or passive taming
  - Creature stats: Health, Stamina, Oxygen, Food, Weight, Melee Damage, Movement Speed, Torpidity
  - Breeding system: Mating, gestation/incubation, imprinting, mutations
  - Rideable creatures with mounted combat

- **Inventory & Crafting**:
  - Weight-based inventory system with stacking
  - Crafting system with engram unlocks and skill requirements
  - Item durability and repair
  - Blueprints and quality tiers (Primitive, Ramshackle, Apprentice, Journeyman, Mastercraft, Ascendant)
  - Resource gathering with tool efficiency (pickaxe vs tree, hatchet vs stone)

- **Building System**:
  - Snap-based building with structure pieces (foundations, walls, ceilings, doors, stairs)
  - Building stability/structural integrity
  - Electrical system (generators, outlets, cables, appliances)
  - Irrigation system (pipes, taps, reservoirs)

- **Combat & Weapons**:
  - Melee combat with timing and hitboxes
  - Ranged weapons with projectile physics (bow, crossbow, firearms)
  - Dino vs Dino and Player vs Dino combat
  - Armor system with protection values and durability

- **Multiplayer/Network** (if applicable):
  - Server-authoritative networking (Mirror/Photon/Netcode for GameObjects)
  - Tribe system (clans with ranks and shared structures/dinos)
  - PvP/PvE modes

## Project Structure (Based on Actual Folders)
Surv/
├── .continue/
│ └── rules/
│ └── project-architecture.md
├── vscode/ # VS Code settings
├── Assets/
│ ├── AddressableAssetsData/ # Addressables configuration
│ ├── Animations/ # All animation controllers & clips
│ ├── Animations_Pack/ # Additional animation assets
│ ├── Art/ # 3D Models, Materials, Textures
│ ├── Audio/ # Sound effects and music
│ ├── Data/ # ScriptableObjects (items, creatures, recipes)
│ ├── InputSystem/ # Input Action assets
│ ├── Localization/ # Localization tables
│ ├── Resources/ # Runtime-loaded resources
│ ├── Samples/ # Unity package samples
│ ├── Scenes/ # Game levels and UI scenes
│ ├── Scripts/ # ALL C# source code
│ ├── Settings/ # Project settings (Input, Physics, Quality)
│ ├── Shaders/ # Custom shaders
│ ├── TextMesh Pro/ # TMP assets
│ └── URPDefaultResources/ # URP defaults
├── _ExternalPackages/ # External dependencies
├── _Models/ # 3D model source files
├── _Prefabs/ # Reusable prefabs
├── _Recovery/ # Unity recovery data
├── _TerrainAutoUpgrade/ # Terrain data
├── Packages/ # Unity package manifests
├── MyInputActions.inputactions # Input Action asset (root)
├── Surv.slnx # Visual Studio solution
├── .gitignore
├── Issues.md # Known issues tracker
├── TODO.md # Development roadmap
├── Modelfile # AI model config


## Coding Standards

- **Language**: C# with .NET Standard 2.1
- **Naming Conventions**:
  - Classes, Structs, Interfaces (`I` prefix): `PascalCase`
  - Methods, Properties, Events: `PascalCase`
  - Public/Serialized Fields: `camelCase`
  - Private Fields: `_camelCase` (with underscore)
  - Constants: `UPPER_SNAKE_CASE`
  - Enums: `PascalCase` (singular for flags)
- **Unity-Specific**:
  - Use `[SerializeField]` for inspector exposure, avoid public fields
  - Prefer `GetComponent` caching in `Awake()`, avoid `FindObjectOfType` in performance-critical code
  - Use `ScriptableObject` for all data-driven systems (items, creatures, recipes, engrams)
  - Use Addressables for dynamic asset loading
  - Use Unity's new Input System (`MyInputActions`) for all controls
- **Architecture Patterns**:
  - Use **Service Locator** for global systems (GameManager, UIManager, NetworkManager)
  - Use **Observer Pattern**/Events for decoupled communication
  - Use **State Machine** for player and AI states
  - Use **Object Pooling** for projectiles, effects, and creatures
- **Performance**:
  - Use Object Pooling for frequently spawned objects
  - Implement LODs for creatures and world objects
  - Use Jobs/Burst for heavy computations (pathfinding, world generation)
  - Profile regularly with Unity Profiler
- **Documentation**:
  - XML comments for all public APIs
  - README for each major system folder
  - Keep `Issues.md` and `TODO.md` updated

## Key Dependencies

- **Unity 6.5 (6000.5.0f1)** with Universal Render Pipeline (URP)
- **Input System** - New Input System package
- **Addressables** - Asset management
- **TextMeshPro** - UI and world text
- **Localization** - Multi-language support
- **ExternalPackages/** - Custom or third-party plugins

## Data-Driven Design

All core game data should be ScriptableObjects stored in `Assets/Data/`:
- `GameSettings` - Game Settings
- `CreatureData` - Species stats, spawn weights, taming values
- `ItemData` - Item properties, weight, stack size, durability
- `RecipeData` - Crafting requirements and results
- `EngramData` - Unlock requirements and costs
- `BiomeData` - Biome parameters, spawn tables, weather
- `StructureData` - Building piece properties and costs

## Important Files

- `MyInputActions.inputactions` - Main input map
- `Surv.slnx` - Solution file for development
- `Issues.md` / `TODO.md` - Project management

## Documentation & Learning Resources

### Unity Official Documentation
- [Unity 6 Manual](https://docs.unity3d.com/6000.0/Documentation/Manual/)
- [Unity Scripting API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/)
- [URP Documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@16.0/manual/)
- [Unity Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/)
- [Unity Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/)
- [Unity Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.4/manual/)
- [Unity UI Toolkit](https://docs.unity3d.com/Manual/UIElements.html) (if using)
- [Unity Physics](https://docs.unity3d.com/Manual/PhysicsSection.html)

### ARK & Survival Game Reference
- [ARK Survival Evolved Wiki](https://ark.wiki.gg/wiki/ARK_Survival_Evolved_Wiki) - For game design reference
- [ARK Dev Kit Documentation](https://devkit.ark.wiki.gg/) - Official modding docs
- [Survival Game Design Patterns](https://www.gamedeveloper.com/design/survival-game-design-patterns) - General survival mechanics

### Unity Best Practices & Patterns
- [Unity Best Practices Guide](https://resources.unity.com/games/unity-best-practices-guide-2022-lp)
- [Game Programming Patterns](https://gameprogrammingpatterns.com/) (State Machine, Observer, etc.)
- [Unity ECS Documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/) (if using DOTS)

### Networking (if using multiplayer)
- [Mirror Networking](https://mirror-networking.gitbook.io/docs/) - If using Mirror
- [Unity Netcode for GameObjects](https://docs-multiplayer.unity3d.com/netcode/current/about/) - If using NGO
- [Photon Unity Networking](https://doc.photonengine.com/en-us/pun/v2) - If using Photon

### Tools & Utilities
- [GitHub Documentation](https://docs.github.com/en) - For version control
- [Visual Studio Unity Debugging](https://learn.microsoft.com/en-us/visualstudio/gamedev/unity/get-started/visual-studio-tools-for-unity) - IDE integration
- [Unity Profiler Guide](https://docs.unity3d.com/Manual/Profiler.html) - Performance optimization


---

# Current Implementation Status

> Это описание того, что **уже реализовано** в проекте.
> Обновлено: [17.09.2026]

## Реализовано

### 1. Менеджеры и архитектура

- **Префаб `===CoreManagers===`** — все глобальные менеджеры на одном объекте.
- **`ManagersSpawner`** — создает `===CoreManagers===` в Bootstrap-сцене. Защита от дубликатов через `static bool _managersCreated`.
- **`DontDestroyOnLoad`** — все менеджеры живут между сценами.
- **Сцены:**
  - `MainMenu` — главное меню.
  - `GameWorld` — игровой мир.
  - Bootstrap-сцена (или `MainMenu` как первая) — создает менеджеров.

### 2. Менеджеры в `===CoreManagers===`

| Менеджер                | Назначение           
|-------------------------|---------------------------------------------------
| `LoadingScreenManager`  | Управление загрузочным экраном 
| `GameController`        | Сохранение игры, выход 
| `PlayerSurvivalSystem`  | Здоровье, еда, вода, кислород, выносливость
| `PlayerProgress`        | Уровень, опыт, энгрamm-очки, инвентарь, экипировка 
| `WorldManager`          | Чанковая система сохранения построек 
| `LanguageManager`       | Локализация 
| `InventoryConfig`       | Настройки инвентаря 
| `AudioManager`          | Громкость 
| `CorpseManager`         | Сохранение/загрузка трупов 
| `SceneBootstrap`        | Регистрация задач загрузки 
| `LootBagManager`        | Сохранение/загрузка сумок 


### 2.1 Менеджеры на объекте `===GameManagers===` на сцене
> Все эти менеджеры — **сценовые** (не `DontDestroyOnLoad`). Они пересоздаются при загрузке сцены `GameWorld`.
> Глобальные менеджеры (сохранение, прогресс) — в префабе `===CoreManagers===` с `DontDestroyOnLoad`.
| Менеджер                    | Назначение           
|-----------------------------|-----------------------------------------------
| `HUDManager`                | Отображение статов 
| `CharacterPreviewManager`   | Превью персонажа при открытие его инвентаря 
| `CombatAudioManager`        | Звуки ударов рязными инструментами 
| `NotificationManager`       | Уведомления 
| `TooltipManager`            | Подсказки при наведении 
| `ContextMenuManager`        | Контекстное меню с кнопками для слотов инвентаря 
| `PauseManager`              | Окно паузы с оверлеем и кнопками 
| `DeathScreenManager`        | Окно экрана смерти с оверлеем и кнопками 

### 2.2 Компоненты на объекте `===InventoryManager===` на сцене
| Компонент                   | Назначение           
|-----------------------------|-----------------------------------------------
| `InventoryManager`          | Управление инвентарями 

### 2.3 Компоненты на объекте `===PanelsController===` на сцене
| Компонент                   | Назначение           
|-----------------------------|-----------------------------------------------
| `PanelsUIController`        | Управление кнопками и панелями инветаря, энграмм, крафтинга

### 2.4 Компоненты на объекте `___CreatureSpawner___` на сцене
| Компонент                   | Назначение           
|-----------------------------|-----------------------------------------------
| `CreatureSpawner`           | Менеджер спавна существ

### 2.5 Компоненты на объекте `---Player---` на сцене

| Компонент                       | Назначение                                                        |
|---------------------------------|-------------------------------------------------------------------|
| `Character Controller`          | Физическое движение игрока (встроенный Unity)                     |
| `Capsule Collider`              | Коллайдер для физики                                              |
| `Basic Rigid Body Push (Script)`| Толкание Rigidbody-объектов (например, ящиков)                    |
| `Player Input`                  | Новый Input System (Unity) — источник ввода                       |
| `Player Input Handler (Script)` | Обработка ввода, события (Interact, Fire, Hotbar, ...)            |
| `Camera Manager (Script)`       | Управление Cinemachine-камерой, режимы (first/third person)       |
| `Player Equipment (Script)`     | Экипировка инструментов/оружия, `corpseDragAnchor`, `IsEquipped`  |
| `Player Build Mode (Script)`    | Режим строительства, preview построек                             |
| `Item Usage System (Script)`    | Использование предметов (еда, ресурсы, оружие)                    |
| `Item Handler (Script)`         | Подбор предметов, `PickupItem()`, `DestroyItem()`                 |
| `Player Interaction (Script)`   | Raycast/OverlapSphere-взаимодействие с IInteractable              |
| `Player Controller (Script)`    | Главный контроллер: движение, камера, атака, ссылки на системы    |

**Дочерние объекты:**
- `PlayerCameraRoot` — точка привязки Cinemachine-камеры
  - `Character` — визуальная модель игрока (меш, Animator, Ragdoll, Corpse, RadialMenu)
  - `Directional Light` — направленный свет (для модели?)
- `UI` → `UICanvases`:
  - `InventoryCanvas` — инвентарь игрока
  - `InteractionCanvas` — подсказки взаимодействия
  - `NotificationTopCanvas` — верхние уведомления
  - `NotificationLeftCanvas` — левые уведомления
  - `HUDCanvas` — HUD (статы)
  - `ContextMenuCanvas` — контекстное меню слотов
  - `PauseCanvas` — окно паузы
  - `DeathScreenCanvas` — экран смерти
  - `RadialMenuCanvas` — радиальное меню
- `_Env` — окружение (трава, деревья, камни)?
- `Interactables` — интерактивные объекты в сцене?

**Важно:** на `Character` (дочернем объекте) висят:
- `Animator`
- `Corpse (Script)` — **отключен** (активируется при смерти)
- `RadialMenu (Script)` — **отключен**
- `RagdollSettings` — настройки ragdoll
- `Rigidbody` — для ragdoll

**При смерти игрока:**
1. `PlayerController` и живые компоненты **отключаются**.
2. Создается `corpseBodyPrefab` (отдельный префаб с `Corpse`).
3. `PlayerController` **уничтожается** или **скрывается**.



### 3. ScriptableObject базы данных

| База                | Назначение 
|---------------------|----------------------------------------------------
| `ItemDatabase`      | Реестр всех предметов 
| `RecipeDatabase`    | Реестр рецептов 
| `CreatureDatabase`  | Реестр существ 
| `GameSettings`      | Глобальные настройки (время жизни трупов/сумок) 


**Важно:** все базы — **ассеты** (`ScriptableObject`), **не** `MonoBehaviour` в сцене.

### 4. Данные существ

- **`CreatureData`** (ScriptableObject) — данные одного существа:
  - `creatureId`, `displayName`, `icon`, `description`
  - `prefab` — **единый префаб** (живой = труп)
  - Статы: `maxHealth`, `maxStamina`, `walkSpeed`, `chaseSpeed`
  - Агрессия: `aggressionRadius`, `attackRange`, `attackDamage`, `attackCooldown`
  - Блуждание: `wanderRange`, `minWanderDelay`, `maxWanderDelay`
  - Поведение: `behaviorType`, `tamable`, `tamableKO`, `tamablePassive`
  - Лут: `inventoryLootTable`, `harvestDrops`, `maxHarvestHits`
  - Разрешения: `allowFists`, `allowAxe`, `allowPickaxe`, `allowSword`, `allowSickle`
  - Звуки: `footstepClip`, `attackClip`, `takeDamageClip`, `deathClip`
  - Эффекты: `damageEffect`, `impactType`
  - `corpseLifetimeOverride`

- **`Creature`** (MonoBehaviour) — компонент существа:
  - Ссылка на `CreatureDatabase` + `creatureId`
  - Свойство `Data` — кэш `CreatureData`
  - Статы берутся из `CreatureData` через свойства (`WanderRange`, `ChaseSpeed`, ...)
  - AI: State Machine (Wander / Chase / Attack)
  - Использует `NavMeshAgent`

- **`BaseLivingEntity`** — базовый класс:
  - `health`, `maxHealth`, `stamina`, `maxStamina`
  - `TakeDamage()`, `Die()`, `Heal()`
  - `IInteractable`, `IImpactSoundProvider`
  - `OnDeath` — событие

### 5. Система смерти и трупов

- **`Corpse`** (MonoBehaviour) — компонент трупа:
  - Висит на **том же префабе**, что и живое существо/игрок
  - **Отключен** по умолчанию (`enabled = false`)
  - Включается при смерти (`enabled = true`)
  - `InstanceId`, `OwnerPlayerId`, `CorpseId` — для сохранения
  - Инвентарь трупа (`ChestInventory`)
  - `inventoryLootTable` — лут, попадающий в инвентарь
  - `harvestDrops` — ресурсы при разборе
  - `maxHarvestHits` — количество ударов
  - `RagdollSettings` — настройки ragdoll
  - `LootBagPrefab` — префаб сумки (создается при разборе)

- **`CorpseManager`** (MonoBehaviour) — сохранение/загрузка:
  - Хранит `_loadedCorpses` — Dictionary<string, GameObject>
  - `RegisterCorpse(corpseGO, ownerPlayerId, creatureId = null)` — регистрация
  - `UnregisterCorpse(instanceId)` — удаление файла
  - `LoadAllCorpsesAsync()` — загрузка через корутину
  - `SpawnCorpseFromData(data)` — создание трупа из данных
  - `IsQuitting` — защита от удаления файла при выходе
  - Файлы: `persistentDataPath/Corpses/corpse_{guid}.save`

- **`CorpseSaveData`** (сериализуемый класс):
  - `instanceId`, `ownerPlayerId`, `corpseType`, `creatureId`
  - Позиция/поворот
  - `creationTimeUtc`, `despawnDuration`, `isLootBag`
  - `inventorySaveKey`, `inventoryData` (SerializableInventory)
  - `harvestDrops`, `remainingAmounts`, `harvestHits`, `isDepleted`

- **Сценарий смерти игрока:**
  1. `PlayerSurvivalSystem.OnPlayerDeath()` — отключает живые компоненты игрока.
  2. Создает `corpseBodyPrefab` из `CorpseManager.corpseBodyPrefab`.
  3. Активирует `Corpse`.
  4. `CreateCorpseInventory("PlayerCorpse", 110, chestUI)`.
  5. `CopyPlayerItemsToCorpse(mainInventory, hotbarInventory)`.
  6. `CorpseManager.RegisterCorpse(corpseGO, "player_001")`.
  7. Очищает инвентарь игрока.
  8. Показывает экран смерти.

- **Сценарий смерти существа:**
  1. `BaseLivingEntity.Die()` — отключает `NavMeshAgent`, `Animator`.
  2. Активирует `Corpse` на **том же объекте**.
  3. Копирует лут из `CreatureData`:
     - `corpse.harvestDrops = data.harvestDrops`
     - `corpse.inventoryLootTable = data.inventoryLootTable`
     - `corpse.maxHarvestHits = data.maxHarvestHits`
     - `corpse.allowFists/Axe/Pickaxe/Sword/Sickle = data.allow*`
  4. `CreateCorpseInventory("CreatureCorpse", 100, chestUI)`.
  5. `PopulateInventoryFromLootTable()` — заполняет инвентарь.
  6. `CorpseManager.RegisterCorpse(gameObject, "world", creatureId)`.

- **Сценарий загрузки трупа:**
  1. `CorpseManager.LoadAllCorpsesAsync()` — читает все `corpse_*.save`.
  2. Проверяет `age < despawnDuration` (иначе удаляет).
  3. `SpawnCorpseFromData(data)`:
     - Для игрока — `corpseBodyPrefab`.
     - Для существа — `creatureDatabase.GetCreature(data.creatureId).prefab`.
     - Отключает живые компоненты (`Creature`, `NavMeshAgent`, `Animator`).
     - Включает `Corpse`.
     - Восстанавливает инвентарь из `data.inventoryData`.
     - Запускает таймер `StartDespawnTimer(remainingTime)`.

- **Сценарий разбора трупа:**
  1. `Corpse.OnHarvestComplete()` — вызывается после `maxHarvestHits` ударов.
  2. Создает `LootBag` с остатками инвентаря.
  3. `LootBagManager.RegisterLootBag(bagGO, "world")`.
  4. `CorpseManager.UnregisterCorpse(InstanceId)` — удаляет файл.
  5. `Destroy(gameObject, 0.5f)`.

### 6. Система сумок (LootBag)

- **`LootBag`** (MonoBehaviour) — компонент сумки:
  - `InstanceId`, `OwnerPlayerId` — для сохранения
  - `ChestInventory` — инвентарь
  - `ChestUI` — UI
  - `_bagDisappearTime` — время жизни
  - `Initialize(inventory, chestUI, disappearTime, source)`
  - `SetPersistenceData(instanceId, ownerPlayerId)`
  - `SetRemainingTime(remainingTime)`
  - `ForceDespawn()` — исчезновение
  - `OnDestroy()` — уведомляет `LootBagManager` (если не выход)

- **`LootBagManager`** (MonoBehaviour) — сохранение/загрузка:
  - Хранит `_loadedLootBags` — Dictionary<string, GameObject>
  - `RegisterLootBag(lootBagGO, ownerPlayerId = "world")` — регистрация
  - `UnregisterLootBag(instanceId)` — удаление файла
  - `LoadAllLootBagsAsync()` — загрузка
  - `SpawnLootBagFromData(data)` — создание сумки
  - `CreateLootBagFromItems` — создание сумки из предметов при выбрасывании
  - `IsQuitting` — защита при выходе
  - Файлы: `persistentDataPath/LootBags/lootbag_{guid}.save`

- **`LootBagSaveData`** (сериализуемый класс):
  - `instanceId`, `ownerPlayerId`
  - Позиция/поворот
  - `creationTimeUtc`, `despawnDuration`
  - `inventorySaveKey`, `inventoryData`

- **Сценарий появления сумки:**
  - **Разбор трупа** — `Corpse.CreateLootBag()`.
  - **Выброс вещей** — TODO.
  - **Разрушение сундука** — TODO.

### 7. Загрузочный экран

- **`LoadingScreenManager`** (MonoBehaviour) — синглтон:
  - `_loadingCanvas`, `_progressBar` (Image), `_statusText`
  - `_minShowTime`, `_physicsSettleTime`
  - `Show(status)`, `Hide()`
  - `RegisterTask(name, IEnumerator routine)` — регистрация задачи
  - `StartLoading()` — запуск всех задач
  - `LoadingRoutine()`:
    1. Показывает экран.
    2. Выполняет задачи по очереди.
    3. `SetProgress(0.9f, "Ожидание физики...")` — ждет `_physicsSettleTime`.
    4. `SetProgress(1f, "Готово")`.
    5. Скрывает экран.

- **`SceneBootstrap`** (MonoBehaviour) — регистратор задач:
  - Ждет `PlayerProgress` и `PlayerController`.
  - Регистрирует задачи:
    1. `CorpseManager.LoadAllCorpsesAsync()` — "Загрузка трупов..."
    2. `LootBagManager.LoadAllLootBagsAsync()` — "Загрузка сумок..."
    3. `WorldManager.LoadWorldAsync(playerPos)` — "Загрузка мира..."
  - `LoadingScreenManager.StartLoading()`.

- **Порядок загрузки:** трупы → сумки → мир → ожидание физики.

### 8. Инвентарь и ChestUI

- **`InventoryData`** — данные инвентаря (слоты).
- **`InventorySlot`** — один слот (`item`, `count`, `currentDurability`).
- **`InventoryManager`** — логика игрока (использование, drop).
- **`ChestInventory`** — контейнер (сундук, труп, сумка):
  - `saveKey`, `size`, `Data`
  - `Initialize(size, saveKey)`
  - `Save()`, `Load()`
  - `OnDestroy()` — сохраняет/удаляет файл (в зависимости от `IsCorpseInventory()`).
  - **Оптимизация:** `ChestUI.CreateChestSlots()` переиспользует слоты, а не создает заново.

- **`ChestUI`** — UI для контейнеров:
  - `_currentChest`, `slotUIs`, `slotPrefab`
  - `OpenWith(chest, source)`, `Close()`
  - `CreateChestSlots()` — создает/переиспользует слоты
  - `RefreshUI()` — обновляет UI
  - **Оптимизация:** использует `Destroy` только для **лишних** слотов.

### 9. Управление и ввод

- **Input System** (Unity).
- **`PlayerInputHandler`** — обработка ввода:
  - Move, Look, Jump, Sprint, Crouch, Crawl
  - Interact, TargetInventory
  - Attack, RightClick
  - Hotbar (0-9)
  - Drop, OpenInventory, Cancel, HideTool

- **`PlayerController`** — контроллер игрока:
  - `CharacterController`, `PlayerInput`, `PlayerInputHandler`
  - Статы движения: `MoveSpeed`, `SprintSpeed`, `SwimSpeed`, ...
  - `PlayerMovementSettings` (ScriptableObject) — все настройки движения.
  - Ссылки: `equipment`, `buildMode`, `itemUsageSystem`, `panelsController`.
  - Трансформы: `VisualCharacter`, `Head`, `EyeCenterForCamera`.
  - Регистрация: `PlayerProgress.Instance?.RegisterPlayerController(this)`.
  - Singleton: `PlayerController.Instance`.

### 10. Инвентарь игрока

- **`PlayerProgress`** — данные игрока:
  - `hotbarInventoryData` (10 слотов)
  - `mainInventoryData` (100 слотов)
  - `engramData`
  - `recipeDatabase`, `itemDatabase`
  - `beginnerItems`
  - `playerController`, `inventoryManager` — регистрируются в рантайме.
  - `OnPlayerLoaded` — событие после загрузки.

- **`InventoryManager`** — логика:
  - `UseItemFromSlot()`, `DropItemFromSlot()`
  - `MoveAllToChest()`, `MoveAllToPlayer()`
  - `EquipSavedEquippedItem()`, `SaveEquippedItem()`
  - `TryRepairItem()`

- **`InventoryUI`**, **`HotbarUI`** — UI игрока.

### 11. Известные особенности

- **Трупы не удаляются** при выходе из игры (`IsQuitting` в менеджерах).
- **Трупы удаляются** при разборе (`OnHarvestComplete` → `UnregisterCorpse`).
- **Animator** у трупов **отключается** (`animator.enabled = false`), **НЕ удаляется** — иначе ragdoll ломается.
- **`Corpse` висит на корне префаба** существа (не на дочернем).
- **`RagdollSettings`** заполнены костями (Torso, UpperLeg.L, UpperLeg.R, ...).
- **`SceneBootstrap`** регистрирует задачи загрузки — **единственное место**.
- **`LoadingScreenManager`** — singleton, живет в `===CoreManagers===`.
- **`ChestUI.CreateChestSlots()`** — переиспользует слоты (оптимизация).
- **`Corpse.OnHarvestComplete()`** — `UnregisterCorpse(InstanceId)` **раскомментирован** (удаляет файл).

### 12. Система камер и прицеливания (ARK-style)

Реализована система камер и прицеливания, вдохновленная ARK: Survival Evolved.

- **`CameraManager.cs`** (Singleton):
  - Управляет тремя режимами: `ThirdPerson`, `FirstPerson`, `Selfie`.
  - Переключение TPS ↔ FPS: колесо мыши (вверх — FPS, вниз — TPS).
  - Переключение Selfie: клавиша `K` (toggle). Запоминает предыдущий режим.
  - Управляет приоритетами Cinemachine-камер: неактивные — 0, активная — 100, Selfie — 150.
  - Событие `OnCameraModeChanged`.

- **`SelfieCameraOrbit.cs`**:
  - Активен только в режиме Selfie.
  - Вращение мышью вокруг игрока, зум колесом мыши.
  - Не вращает тело игрока (движение WASD сохраняется).

- **`PlayerAimingSystem.cs`** (Singleton):
  - Отвечает за **зум** по ПКМ: плавно меняет `Lens.FieldOfView` активной камеры (TPS или FPS) через `Mathf.MoveTowards`.
  - Берет `aimFov` и `aimBlendTime` из `Item`.
  - **Зум доступен только для оружия дальнего боя (`Item.isRanged == true`).**
  - Событие `OnAimingChanged(bool)` — для тех, кому важно именно состояние зума.
  - Игнорирует зум в режиме Selfie.

- **Видимость прицела (ARK-style):**
  - `PlayerAimingSystem` также вычисляет `IsCrosshairVisible` и шлёт событие `OnCrosshairVisibilityChanged`.
  - **Прицел виден всегда**, когда экипировано дальнобойное оружие (`item.isRanged == true`), в FPS и TPS, независимо от ПКМ.
  - **Прицел скрыт**, если: оружие не экипировано, экипировано не-дальнобойное, активна Selfie-камера.
  - `AimUI.cs` подписывается на `OnCrosshairVisibilityChanged` и включает/выключает `AimCrosshair`.

- **`AimCanvas` + `AimCrosshair` + `AimUI.cs`**:
  - UI-прицел в центре экрана.
  - Показывается/скрывается по событию `PlayerAimingSystem.OnCrosshairVisibilityChanged`.
  - `Raycast Target: false` на Image.

- **`AimUI.cs` + `AimCanvas` + `AimCrosshair`**:
  - UI-прицел в центре экрана.
  - Показывается/скрывается по событию `PlayerAimingSystem.OnAimingChanged`.
  - `Raycast Target: false` на Image.

- **`Item.cs`**:
  - Добавлены поля `aimFov` и `aimBlendTime` для настройки зума.

Стрела летит из ArrowSpawnPoint в точку под прицелом камеры (_aimDistance для проецирования).
Прицел UI управляется отдельным состоянием IsCrosshairVisible в PlayerAimingSystem, а не IsAiming. 
Скрывается при панелях, паузе, смерти, Selfie.

## TODO

- [x] **Выброс вещей → сумка** (`InventoryManager.DropItemFromSlot` → `LootBagManager.CreateLootBagFromItems`).
- [ ] **Разрушение сундука → сумка** (`ChestController.OnDestroy` → `LootBagManager`).
- [ ] **Система выбора персонажа** (меши, материалы, blend shapes).
- [ ] **Мультиплеер** (Mirror / Netcode).
- [ ] **Предзагрузка слотов `ChestUI`** при старте (убрать лаг при первом открытии).
- [ ] **Температура** (Hyperthermia/Hypothermia).
- [ ] **Taming** (Knockout / Passive).
- [ ] **Breeding** (Mating, Imprinting, Mutations).
- [ ] **Rideable creatures** (Mounted combat).
- [ ] **Building stability** (Structural integrity).
- [ ] **Electrical system** (Generators, Cables, Appliances).
- [ ] **Irrigation system** (Pipes, Taps, Reservoirs).
- [ ] **Ranged weapons** (Bow, Crossbow, Firearms).
- [ ] **Armor system** (Protection, Durability).
- [ ] **Blueprints & Quality tiers**.

## Важные файлы для контекста

При создании нового чата скинуть:

### Обязательно
- `PROJECT-ARCHITECTURE.md` (этот файл)



СТОЛПЫ АРК
+Собирательство/добыча
+Крафт
+Строительство 
+Инвентарь
+Прогрессия
Боевка
Приручение существ
Использование прирученных существ
Экосистема (хищники/жертвы, респавн)
Броня
Температура/погода
Респавн по регионам или в кровати
Статы
Биомы + ресурсы по регионам
Фермерство
Разведение
Племена
Боссы
Мультиплеер
