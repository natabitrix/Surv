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
> Обновлено: [06.10.2026]

## Реализовано

### 1. Менеджеры и архитектура

- **`[GameAssets]`** — GameObject в **обеих** сценах (`MainMenu` и `GameWorld`), `DontDestroyOnLoad`. Единый реестр ассетов и создатель глобальных менеджеров.
- **Разделение менеджеров:**
  - **Глобальные** (`DontDestroyOnLoad`, живут всю сессию): `GameController`, `PlayerProgress`, `GameTimeUpdater`, `LoadingScreenManager`, `AudioManager`, `LanguageManager`. Создаются один раз из `[GameAssets]`.
  - **Сценовые** (умирают со сценой): всё остальное — `CorpseManager`, `LootBagManager`, `TamingManager`, `WorldManager`, `HUDManager`, `UIManager`, `PauseManager`, `DeathScreenManager`, `ChestUIManager`, `PlayerSurvivalSystem` и др.
- **`WorldBootstrap`** — сценовый, лежит на `===WorldManagers===` в `GameWorld`. Создаёт `CorpseManager`, `LootBagManager`, `TamingManager`, `WorldManager` через `AddComponent + Initialize()`. Ждёт `GameAssets.Instance != null` в корутине `Start` (порядок `Awake` в Unity не гарантирован между объектами).
- **Сцены:**
  - `MainMenu` — главное меню. Свой `[GameAssets]`. Если игра стартует отсюда — этот экземпляр становится primary (`DontDestroyOnLoad`), а `[GameAssets]` из `GameWorld` при загрузке удалится.
  - `GameWorld` — игровой мир. Свой `[GameAssets]`. Если игра стартует отсюда — он становится primary. Если пришли из `MainMenu` — удалится.

### 2. Глобальные менеджеры (DontDestroyOnLoad)

Создаются из `[GameAssets].Awake()` один раз за сессию. `GameController`, `PlayerProgress`, `GameTimeUpdater` — дочерние GameObject'ы `[GameAssets]`. `LoadingScreenManager`, `AudioManager`, `LanguageManager` — компоненты на самом `[GameAssets]`.

| Менеджер | Назначение |
|---|---|
| `GameController` | `GameSettings`, `SessionMode.Initialize`. Вызывается через `Initialize(settings)`. |
| `PlayerProgress` | Уровень, XP, энгрamm-очки, инвентарь, `_loadedSaveData`. `Initialize(itemDb, recipeDb)`. |
| `GameTimeUpdater` | Тик `GameTime`. |
| `LoadingScreenManager` | Экран загрузки. Ссылки на `LoadingScreenCanvas`, `ProgressBar`, `StatusText` — дочерний Canvas внутри `[GameAssets]`. |
| `AudioManager` | Громкость, `PlayerPrefs`. |
| `LanguageManager` | Локализация, `PlayerPrefs`. |

### 2.1 Сценовые менеджеры мира `===WorldManagers===` (GameWorld)

Создаются `WorldBootstrap` в корутине `Start` через `AddComponent + Initialize()`. Живут до выгрузки сцены.

| Менеджер | Назначение |
|---|---|
| `CorpseManager` | Сохранение/загрузка трупов. `Initialize()` берёт `PlayerCorpsePrefab`, `CreatureDatabase`, `ItemDatabase` из `GameAssets.Instance`. |
| `LootBagManager` | Сохранение/загрузка сумок. `Initialize()` берёт `LootBagPrefab`, `ItemDatabase` из `GameAssets.Instance`. |
| `TamingManager` | Сохранение/загрузка нокаутнутых и прирученных. `Initialize()` берёт `CreatureDatabase`, `ItemDatabase` из `GameAssets.Instance`. |
| `WorldManager` | Чанковая система построек. `Initialize(chunkSize, loadRadius, saveInterval)`. |

### 2.2 Сценовые менеджеры UI `===GameManagers===` (GameWorld)

| Менеджер | Назначение |
|---|---|
| `HUDManager` | Отображение статов. |
| `CharacterPreviewManager` | Превью персонажа в инвентаре. |
| `CombatAudioManager` | Звуки ударов. |
| `NotificationManager` | Уведомления. |
| `TooltipManager` | Подсказки. |
| `ContextMenuManager` | Контекстное меню слотов. |
| `PauseManager` | Окно паузы. |
| `DeathScreenManager` | Экран смерти. |
| `UIManager` | HUD/хотбар/экраны. Подписан на `PlayerSurvivalSystem.OnPlayerDied/OnPlayerRespawned`. |
| `DamageNumberPool` | Пул всплывающих чисел урона. |
| `EntityInfoPanelManager` | Инфо-панель над существом под прицелом. |
| `ArrowPool` | Пул стрел. |
| `AimUI` | Прицел. |

### 2.3 `===InventoryManager===` (GameWorld)

| Компонент | Назначение |
|---|---|
| `InventoryManager` | Управление инвентарями. |
| `ChestUIManager` | Единая точка входа для инвентарей. Владелец состояния «какой инвентарь открыт». Сценовый. |

### 2.4 `===PanelsController===` (GameWorld)

| Компонент | Назначение |
|---|---|
| `PanelsUIController` | Управление панелями инвентаря, энгрamm, крафтинга. |

### 2.5 `---Player---` (GameWorld)

| Компонент | Назначение |
|---|---|
| `Character Controller` | Движение. |
| `Capsule Collider` | Коллайдер. |
| `Basic Rigid Body Push (Script)` | Толкание Rigidbody. |
| `Player Input` | Input System. |
| `PlayerInputHandler (Script)` | Обработка ввода, события. |
| `Camera Manager (Script)` | Cinemachine-камеры. |
| `Player Equipment (Script)` | Экипировка. |
| `Player Ranged Combat (Script)` | Стрельба. |
| `Player Build Mode (Script)` | Строительство. |
| `Item Usage System (Script)` | Использование предметов. |
| `Item Handler (Script)` | Подбор/уничтожение предметов. |
| `Player Interaction (Script)` | Raycast/OverlapSphere. |
| `Player Aiming System (Script)` | Зум и прицел. |
| `Player Controller (Script)` | Главный контроллер. |
| `Player Survival System (Script)` | **Сценовый**, `Start()` берёт `PlayerProgress.GetLoadedSaveData()`. |
| `Cinemachine Impulse Source` | Тряска. |
| `Player Damage Feedback (Script)` | Фидбек урона. |
| `Item Usage Router (Script)` | Роутинг использования. |

### 2.6 Deprecated (removed)

Удалены в ходе рефакторинга (ветка `refactoring-managers`):
- `ManagersSpawner.cs` — заменён на `[GameAssets]` в сцене.
- `SceneBootstrap.cs` — логика переехала в `WorldBootstrap`.
- `InventoryConfig.cs` — `splitStackKey` переедет в будущий `SettingsController`.
- Префаб `===CoreManagers===` — заменён на `[GameAssets]` в обеих сценах.
- Свойство `IsQuitting` в `CorpseManager`, `LootBagManager` — не нужно (менеджеры сценовые).

**Причина:** менеджеры, хранящие ссылки на сценовые GameObject (`CorpseManager`, `LootBagManager`, `TamingManager`, `WorldManager`, `HUDManager`, `UIManager`, `PauseManager`, `DeathScreenManager` и др.), были `DontDestroyOnLoad`. При выходе в MainMenu они держали мёртвые ссылки. Плюс `Corpse.OnDestroy` удалял файлы сохранений при выгрузке сцены.

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
  - `int level = 1` — уровень (для инфо-панели)
  - Статы: `maxHealth`, `maxStamina`, `walkSpeed`, `chaseSpeed`
  - Food: `maxFood`, `foodDrainRateKnockedOut`, `foodDrainRateTamed`, `foodEatThresholdPercent`, `starvationDamagePerSecond`, `torporRecoveryRate`
  - Наркотики: `narcoticItem`, `narcoticTorporAmount`
  - Агрессия: `aggressionRadius`, `attackRange`, `attackDamage`, `attackCooldown`
  - Блуждание: `wanderRange`, `minWanderDelay`, `maxWanderDelay`
  - Поведение: `behaviorType`, `tamable`, `tamableKO`, `tamablePassive`
  - Лут: `inventoryLootTable`, `harvestDrops`, `maxHarvestHits`, `tamingFoodPreferences`
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
  - `Level` → `Data?.level ?? 1`
  - `GetDisplayName()` — переопределён

- **`BaseLivingEntity`** — базовый класс:
  - `health`, `maxHealth`, `stamina`, `maxStamina`, `food`, `maxFood`
  - `torpor`, `maxTorpor`, `knockedOut`, `tamed`, `tamingProgress`
  - `TakeDamage()`, `Die()`, `Heal()`, `KnockOut()`, `RecoverFromKnockout()`, `FinishTaming()`
  - `IInteractable`, `IImpactSoundProvider`, `IInventorySource`
  - `OnDeath` — событие
  - `GetDisplayName()`, `GetStatusText()` — виртуальные

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
  - Реализует `IInventorySource`

- **`CorpseManager`** (MonoBehaviour) — **сценовый**, на `===WorldManagers===`:
  - Создаётся `WorldBootstrap.CreateWorldManagers()`.
  - `Initialize()` берёт `PlayerCorpsePrefab`, `CreatureDatabase`, `ItemDatabase` из `GameAssets.Instance`.
  - Хранит `_loadedCorpses` — Dictionary<string, GameObject>.
  - `RegisterCorpse(corpseGO, ownerPlayerId, creatureId = null)` — регистрация + запись файла.
  - **`ForgetCorpse(instanceId)`** — чистит in-memory, файл НЕ трогает. Вызывается из `Corpse.OnDestroy`.
  - **`UnregisterCorpse(instanceId)`** — удаляет и in-memory, и файл. Вызывается только при harvest/despawn.
  - `LoadAllCorpsesAsync()` — загрузка.
  - `SpawnCorpseFromData(data)` — создание трупа из данных.
  - **`DontDestroyOnLoad` и `IsQuitting` — удалены.**
  - Файлы: `persistentDataPath/Corpses/corpse_{guid}.save`.

- **`CorpseSaveData`** (сериализуемый класс):
  - `instanceId`, `ownerPlayerId`, `corpseType`, `creatureId`
  - Позиция/поворот
  - `savedAtTime`, `despawnDuration`, `isLootBag`
  - `inventorySaveKey`, `inventoryData` (SerializableInventory)
  - `harvestDrops`, `remainingAmounts`, `harvestHits`, `isDepleted`

- **Сценарий смерти игрока:**
  1. `PlayerSurvivalSystem.OnPlayerDeath()` — отключает живые компоненты игрока.
  2. Создает `corpseBodyPrefab` из `CorpseManager.corpseBodyPrefab`.
  3. Активирует `Corpse`.
  4. `CreateCorpseInventory("PlayerCorpse", 110)`.
  5. `CopyPlayerItemsToCorpse(mainInventory, hotbarInventory)`.
  6. `CorpseManager.RegisterCorpse(corpseGO, "player_001")`.
  7. Очищает инвентарь игрока.
  8. Показывает экран смерти.

- **Сценарий смерти существа:**
  1. `BaseLivingEntity.Die()` — отключает `NavMeshAgent`, `Animator`, удаляет taming-файл.
  2. Активирует `Corpse` на **том же объекте**.
  3. Копирует лут из `CreatureData`:
     - `corpse.harvestDrops = data.harvestDrops`
     - `corpse.inventoryLootTable = data.inventoryLootTable`
     - `corpse.maxHarvestHits = data.maxHarvestHits`
     - `corpse.allowFists/Axe/Pickaxe/Sword/Sickle = data.allow*`
  4. `CreateCorpseInventory("CreatureCorpse", 100)`.
  5. `PopulateInventoryFromLootTable()` — заполняет инвентарь.
  6. `CorpseManager.RegisterCorpse(gameObject, "world", creatureId)`.

- **Сценарий загрузки трупа:**
  1. `CorpseManager.LoadAllCorpsesAsync()` — читает все `corpse_*.save`.
  2. Проверяет `elapsed < despawnDuration` через `GameTime` (иначе удаляет).
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

- **Сценарий выгрузки сцены (выход в MainMenu):**
  1. `Corpse.OnDestroy` вызывается при уничтожении объекта сцены.
  2. **НЕ удаляет файл** — только `CorpseManager.ForgetCorpse(InstanceId)`.
  3. Файлы сохранений живут между сессиями.
  4. При следующем заходе `CorpseManager.LoadAllCorpsesAsync()` загружает их заново.

### 6. Система сумок (LootBag)

- **`LootBag`** (MonoBehaviour) — компонент сумки:
  - `InstanceId`, `OwnerPlayerId` — для сохранения
  - `ChestInventory` — инвентарь
  - `_bagDisappearTime` — время жизни
  - `Initialize(inventory, disappearTime, source)` — без `ChestUI`
  - `SetPersistenceData(instanceId, ownerPlayerId)`
  - `SetRemainingTime(remainingTime)`
  - `ForceDespawn()` — исчезновение + `UnregisterLootBag` (удаляет файл)
  - `OnDestroy()` — только `ForgetLootBag` (файл НЕ трогает), закрывает UI через `ChestUIManager`
  - Реализует `IInventorySource`

- **`LootBagManager`** (MonoBehaviour) — **сценовый**, на `===WorldManagers===`:
  - Создаётся `WorldBootstrap.CreateWorldManagers()`.
  - `Initialize()` берёт `LootBagPrefab`, `ItemDatabase` из `GameAssets.Instance`.
  - Хранит `_loadedLootBags` — Dictionary<string, GameObject>.
  - `RegisterLootBag(lootBagGO, ownerPlayerId = "world")` — регистрация + запись файла.
  - **`ForgetLootBag(instanceId)`** — чистит in-memory, файл НЕ трогает. Вызывается из `LootBag.OnDestroy`.
  - **`UnregisterLootBag(instanceId)`** — удаляет и in-memory, и файл. Вызывается только при despawn.
  - `LoadAllLootBagsAsync()` — загрузка.
  - `SpawnLootBagFromData(data)` — создание сумки.
  - `CreateLootBagFromItems` — создание сумки из предметов при выбрасывании.
  - **`DontDestroyOnLoad` и `IsQuitting` — удалены.**
  - Файлы: `persistentDataPath/LootBags/lootbag_{guid}.save`.

- **`LootBagSaveData`** (сериализуемый класс):
  - `instanceId`, `ownerPlayerId`
  - Позиция/поворот
  - `savedAtTime`, `despawnDuration`
  - `inventorySaveKey`, `inventoryData`

- **Сценарий появления сумки:**
  - **Разбор трупа** — `Corpse.CreateLootBag()`.
  - **Выброс вещей** — `InventoryManager.DropItemFromSlot` → `LootBagManager.CreateLootBagFromItems`.
  - **Разрушение сундука** — TODO.

- **Сценарий выгрузки сцены:**
  - `LootBag.OnDestroy` — только `ForgetLootBag`. Файл не трогается.
  - При следующем заходе `LootBagManager.LoadAllLootBagsAsync()` загружает из файла.

### 7. Загрузочный экран и WorldBootstrap

- **`WorldBootstrap`** (MonoBehaviour, живёт на `===WorldManagers===` в GameWorld):
  - **НЕ** `DontDestroyOnLoad` — умирает вместе со сценой.
  - `_chunkSize`, `_loadRadiusInChunks`, `_saveInterval` — настройки `WorldManager`.
  - **`Awake`** — только `Instance = this`. НЕ создаёт сценовые менеджеры здесь (порядок `Awake` в Unity не гарантирован между объектами).
  - **`Start`** → `BootstrapRoutine`:
    1. Ждёт `GameAssets.Instance != null` (с таймаутом 5 сек и логированием ошибки).
    2. `CreateWorldManagers()` — `AddComponent` для `CorpseManager`, `LootBagManager`, `TamingManager`, `WorldManager`, вызов `Initialize()` у каждого.
    3. Ждёт `PlayerProgress.Instance.playerController`.
    4. Регистрирует задачи в `LoadingScreenManager`:
       - `CorpseManager.LoadAllCorpsesAsync()`
       - `LootBagManager.LoadAllLootBagsAsync()`
       - `TamingManager.LoadAllTamingCreaturesAsync()`
       - `WorldManager.LoadWorldAsync(playerPos)`
    5. `LoadingScreenManager.StartLoading()`.
  - **Удалён `SceneBootstrap.cs`** — вся логика переехала сюда.
  - **Удалён `ManagersSpawner.cs`** — `[GameAssets]` сам себя создаёт в `Awake`.
  - **Порядок загрузки:** трупы → сумки → прирученные → мир → ожидание физики.

- **`LoadingScreenManager`** (MonoBehaviour, глобальный, на `[GameAssets]`):
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
  - **`ForceReset()`** — сброс состояния (`_isLoading = false`, `_tasks.Clear()`, `StopCoroutine`, `Hide()`). Вызывается из `MainMenuManager.LoadGameScene` перед `Show("Загрузка игры...")`.

### 8. Инвентарь и ChestUI

- **`InventoryData`** — данные инвентаря (слоты).
- **`InventorySlot`** — один слот (`item`, `count`, `currentDurability`).
- **`InventoryManager`** — логика игрока (использование, drop).
- **`ChestInventory`** — контейнер (сундук, труп, сумка, нокаут):
  - `saveKey`, `size`, `Data`
  - `Initialize(size, saveKey)`
  - `Save()`, `Load()`
  - `OnDestroy()` — сохраняет/удаляет файл (в зависимости от `IsCorpseInventory()`).
  - **Оптимизация:** `ChestUI.CreateChestSlots()` переиспользует слоты, а не создает заново.
  - **`IsCorpseInventory()`** — определяет по префиксу `saveKey`: `Corpse_`, `PlayerCorpse_`, `CreatureCorpse_`, `TamingCorpse_`, `LootBag_`. Для таких инвентарей `Save()`/`Load()` — no-op, а `OnDestroy` удаляет файл, если он был.

- **`ChestUI`** — «тупой» UI-слой для контейнеров:
  - `_currentChest`, `slotUIs`, `slotPrefab`
  - `OpenWith(chest, source)` / `Close()`
  - **`ForceSetChest`** — страховка в `OpenWith`, гарантирует что `_currentChest` обновился.
  - **Сброс слотов в `Close`** — принудительный `SetSlot(null)` для всех слотов.
  - **⚠️ Не вызывать `OpenWith`/`Close` напрямую — только через `ChestUIManager`.**
  - Статический `CurrentOpenChest` **убран**.

- **`ChestUIManager`** (Singleton, `===InventoryManager===` в GameWorld) — **сценовый**:
  - Владеет состоянием «какой инвентарь открыт» (`CurrentSource`).
  - `Open(source)`, `Close()`, `Toggle(source)` — единственные легальные способы.
  - `ForceCloseIfSource(source)` — для `OnDestroy` источников.
  - **Нет** `OnEnable/OnDisable` на `sceneLoaded` — не нужно, `ChestUIManager` не переживает сцену.
  - `_chestUI` находится через `FindAnyObjectByType<ChestUI>()` в `Start`.
  - **`DontDestroyOnLoad` убран** — сценовый.

- **`IInventorySource`** (интерфейс, наследник `IInteractable`):
  - `OnInventoryOpened()` / `OnInventoryClosed()` — вызываются менеджером.
  - `GetInventory()` / `HasInventory()` — **уже есть в `IInteractable`**, в `IInventorySource` не дублируются (иначе CS0108).
  - Реализуется: `Corpse`, `BaseLivingEntity`, `ChestController`, `LootBag`.

- **Источники инвентаря:**
  - `Corpse` — `_isOpen` управляется только через `OnInventoryOpened/Closed`.
  - `BaseLivingEntity` — то же.
  - `ChestController` — `_inventory` автопоиск в `Awake`, анимация `open`/`close` в `OnInventoryOpened/Closed`.
  - `LootBag` — открытие/закрытие через менеджер.

- **Известные особенности:**
  - `ChestUI` больше не знает, кто его открыл — источник передаётся менеджером.
  - Все `OnDestroy` источников вызывают `ChestUIManager.Close()`, если были открыты.
  - `ReferenceEquals(CurrentSource, this)` вместо `==` — иначе CS0252.

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
  - `recipeDatabase`, `itemDatabase` — приходят через `Initialize(itemDb, recipeDb)`.
  - `beginnerItems`
  - `playerController`, `inventoryManager` — регистрируются в рантайме.
  - `OnPlayerLoaded` — событие после загрузки.
  - `GetLoadedSaveData()` — возвращает последнее загруженное сохранение. Используется `PlayerSurvivalSystem.Start()`.

- **`InventoryManager`** — логика:
  - `UseSelectedSlot()`, `DropItemFromSlot()`
  - `MoveAllToChest()`, `MoveAllToPlayer()`
  - `EquipSavedEquippedItem()`, `SaveEquippedItem()`
  - `TryRepairItem()`

- **`InventoryUI`**, **`HotbarUI`** — UI игрока.
- **`ItemUsageRouter`** — единая точка входа для «использовать предмет из слота». Роутит запрос: игрок (экип/еда/постройка), чужой инвентарь (наркотик на нокаутнутом).

### 11. Известные особенности

- **`Corpse.OnDestroy` и `LootBag.OnDestroy` НЕ удаляют файл сохранения.** Файл удаляется только через `CorpseManager.UnregisterCorpse` / `LootBagManager.UnregisterLootBag` (при harvest/despawn). Это позволяет выгружать сцену без потери данных.
- **`CorpseManager.ForgetCorpse` / `LootBagManager.ForgetLootBag`** — чистят in-memory словари без удаления файла. Вызываются из `OnDestroy` трупа/сумки.
- **`UnregisterCorpse` / `UnregisterLootBag`** — удаляют файл. Вызываются только при harvest/despawn.
- **`WorldBootstrap`** — единственное место регистрации задач загрузки. `SceneBootstrap` удалён.
- **`LoadingScreenManager` — глобальный**, живёт на `[GameAssets]` (не на `===CoreManagers===`).
- **`CorpseManager`, `LootBagManager`, `TamingManager`, `WorldManager` — сценовые**, `DontDestroyOnLoad` убран.
- **`PlayerSurvivalSystem` — сценовый**, на `---Player---`. В `Start()` берёт `PlayerProgress.GetLoadedSaveData()`.
- **`[GameAssets]`** — в обеих сценах, с защитой от дубликата (`Destroy(gameObject)` если `Instance != null`).
- **`GameAssets.Awake`** создаёт `GameController`, `PlayerProgress`, `GameTimeUpdater` как дочерние GameObject'ы. `LoadingScreenManager`, `AudioManager`, `LanguageManager` — компоненты на самом `[GameAssets]`.
- **`DontDestroyOnLoad`** вызывается только на корне `[GameAssets]`. Дочерние переживают смену сцены через родителя.
- **`[GameAssets]`** — флаг `_isPrimary` защищает от сброса `Instance` в `OnDestroy` при удалении дубликата.
- **`TamingManager.IsQuitting`** — удалён, не нужен. `OnApplicationQuit/Pause/Focus` → `SaveAll()` оставлены.
- **`GameTime`** — единая система времени, используется в `CorpseManager`, `LootBagManager`, `TamingManager`.
- **`LoadingScreenManager.ForceReset()`** — обязательный вызов в `MainMenuManager.LoadGameScene`.
- **`PlayerController._input.look`** — обнуляется после обработки в `CameraRotation`. Иначе Input System выдаёт то же значение каждый кадр → yaw/pitch накапливаются бесконечно.
- **`PlayerController.Start`** — сброс `_input.look` и `mouseScrollDelta`. Input System при инициализации может выдать гигантскую дельту.
- **`PlayerProgress.UnregisterPlayerController`** — вызывается из `PlayerController.OnDestroy`. Иначе ссылка на уничтоженный объект остаётся «мёртвой» и ломает `WorldBootstrap`.

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

- **`Item.cs`**:
  - Добавлены поля `aimFov` и `aimBlendTime` для настройки зума.

Стрела летит из ArrowSpawnPoint в точку под прицелом камеры (_aimDistance для проецирования).
Прицел UI управляется отдельным состоянием IsCrosshairVisible в PlayerAimingSystem, а не IsAiming. 
Скрывается при панелях, паузе, смерти, Selfie.

### 13. Боевка и Torpor (Knockout)

Реализована система боевки, достаточная для начала приручения.

- **Ближний бой**:
  - `PlayerInteraction.OnAttackInteractFinished()` — вызывается из Animation Event клипа атаки.
  - `GetEquippedItemContact()` — `OverlapBox` по геометрии экипированной модели оружия.
  - `GetHandContact()` — `OverlapSphere` вокруг костей рук (кулаки).
  - Урон через `PlayerController.GetAttackDamage()` → `BaseLivingEntity.TakeDamage()`.
  - Трата прочности (`-10` за удар), при `currentDurability <= 0` — оружие ломается.
  - Звуки ударов через `CombatAudioManager` + `IImpactSoundProvider` (`ImpactType`).

- **Дальний бой**:
  - `PlayerRangedCombat.SpawnArrow()` — стрела летит из `ArrowSpawnPoint` в точку под прицелом камеры (`_aimDistance`).
  - `Arrow` — полёт через `RaycastAll` в `FixedUpdate`, fallback на `OnCollisionEnter`.
  - Урон = `bow.damage + arrowItem.damage`, torpor = `bow.torporDamage + arrowItem.torporDamage`.
  - Пул стрел через `ArrowPool` + `ObjectPool<Arrow>`.
  - При попадании — `Pickable` активируется через `_pickupDelay` секунд.

- **Torpor-урон** (для приручения):
  - `Item.torporDamage` — поле у оружия/стрел.
  - `Arrow.Launch(..., torpor, ...)` — стрела несёт torpor.
  - `BaseLivingEntity.TakeDamage(damage, torpor, playerInteraction)` — накапливает torpor.
  - При `torpor >= maxTorpor` — **мгновенный нокаут** прямо в `TakeDamage`.

- **Knockout (нокаут)**:
  - `BaseLivingEntity.KnockOut()` — AI отключается, Animator глушится, `RadialMenu` включается, создаётся `ChestInventory` для приручения (100 слотов, ключ `TamingCorpse_{TamingInstanceId}`).
  - Порядок: сначала `RegisterCreature` в `TamingManager`, потом создание инвентаря с ключом на основе `TamingInstanceId`.
  - `BaseLivingEntity.RecoverFromKnockout()` — при `torpor <= 0` возвращает существо в нормальное состояние.
  - `Creature.Update()` — если `knockedOut`, AI не работает; torpor убывает каждый кадр (`torporRecoveryRate`).
  - Нокаут **обратим**, не путать со смертью.

- **Известные особенности боевки**:
  - `Corpse` не участвует в нокауте — это отдельная система (`KnockOut` создаёт свой `ChestInventory`, не через `Corpse`).
  - `GetComponents<IInteractable>()` **возвращает отключённые `MonoBehaviour`** — обязательно фильтровать по `enabled`.
  - При смерти существа слой переключается на `Corpse` (через `SetLayerRecursively`), чтобы инфо-панель не находила труп.
  - В `CorpseManager.SpawnCorpseFromData()` слой тоже выставляется на `Corpse` — потому что слой не сохраняется в `corpse_*.save`.

### 14. Инфо-панель над существом (ARK SpyGlass-style)

Screen Space инфо-панель, показывающая статы существа под прицелом. Вдохновлена модом Awesome SpyGlass.

- **`EntityInfoPanelManager`** (Singleton, сцена):
  - `Raycast` из центра экрана (`Camera.ViewportPointToRay(0.5, 0.5)`).
  - Ищет `BaseLivingEntity` на слое `Creatures`.
  - `_holdTime` — задержка перед показом (0.2 сек, против мельтешения).
  - `_lingerTime` — панель держится 1–2 сек после ухода цели из прицела.
  - Мгновенно переключается на новую цель.
  - Игнорирует мёртвые и не-нокаутнутые трупы.

- **`EntityInfoPanel`** (Screen Space UI, левый нижний угол):
  - Никогда не наклоняется, всегда одного размера, поверх 3D.
  - Показывает: имя, уровень, статус, health bar + текст, torpor bar + текст.
  - `Show(target)` / `Hide()` / `Refresh()`.
  - Torpor bar скрывается, если `torpor == 0` и не в нокауте.

- **`BaseLivingEntity`**:
  - `GetDisplayName()` — виртуальный, `Creature` переопределяет через `Data.displayName`.
  - `GetStatusText()` — «Wild · Tamable (KO)», «Tamed» и т.п.
  - `SetLayerRecursively(obj, layer)` — переключение слоя на `Corpse` при смерти.

- **`Creature`**:
  - `Level` → `Data?.level ?? 1`.
  - `GetDisplayName()` — переопределён.

### 15. Floating Damage Numbers (всплывающие цифры урона)

Screen Space всплывающие числа урона над точкой удара. Вдохновлено ARK.

- **`DamageNumber`** (MonoBehaviour, на префабе):
  - `TextMeshProUGUI _text` — текст числа.
  - `_lifetime`, `_riseDistance`, `_alphaCurve` — настройки движения и затухания.
  - `Show(amount, worldPos)` — ставит текст, активирует.
  - В `Update`: `worldPos + Vector3.up * (_riseDistance * t)` → `Camera.main.WorldToScreenPoint` → `transform.position`.
  - Alpha по `_alphaCurve`, возврат в пул по истечении `_lifetime`.
  - Всегда одного размера, не наклоняется (Screen Space Overlay).

- **`DamageNumberPool`** (MonoBehaviour, на `===GameManagers===`):
  - `_prefab` — префаб `DamageNumber`.
  - `_canvasRoot` — RectTransform `DamageNumberCanvas`.
  - `_initialSize = 20`, `_spawnOffsetY = 0.3f`.
  - `ShowDamage(amount, worldPos)` — берёт из пула, ставит как child `_canvasRoot`.
  - `Return(num)` — возвращает в пул.

- **`DamageNumberCanvas`** — Canvas (Screen Space Overlay, Sort Order 100) в `UICanvases`. Дети — числа урона.

- **Интеграция**:
  - `BaseLivingEntity.TakeDamage()` — вызывает `DamageNumberPool.Instance.ShowDamage(finalDamage, popupPos)`.
  - `popupPos = playerInteraction.GetTargetHitPosition()` или `hitPoint` (для стрел).
  - По игроку чисел **нет** — у него HUD.

### 16. Зоны урона (Crit / Armor)

- **`CreatureHitZone`** (MonoBehaviour) — маркер на кости (голова, панцирь):
  - `damageMultiplier` — множитель (>1 крит, <1 броня, 1 дефолт).
  - `label` — имя для отладки.
  - `owner` — ссылка на `BaseLivingEntity`, заполняется в `Awake` из родителя.
  - Визуализация в редакторе через `OnDrawGizmosSelected` (красный/синий/серый).

- **Как работает**:
  - В `PlayerInteraction.GetEquippedItemContact` / `GetHandContact` — `OverlapBox` / `OverlapSphere` собирает коллайдеры, запоминает последний в `_targetHitCollider`.
  - `BaseLivingEntity.TakeDamage()` — спрашивает `GetDamageMultiplier(col)`, умножает урон.
  - `Creature.GetDamageMultiplier(col)` — `col.GetComponent<CreatureHitZone>()?.damageMultiplier ?? 1f`.

- **Настройка**: на префабе существа на кость головы добавить `CreatureHitZone` (и, при необходимости, коллайдер).

- **Важное:** `CharacterJoint` может конфликтовать с триггер-коллайдером на голове. Проверено — работает при правильной настройке коллайдера.

### 17. Фидбек при получении урона игроком

- **`PlayerSurvivalSystem.OnPlayerDamaged`** — событие `Action<float>`, вызывается в `TakeDamage(damage)`.

- **`PlayerDamageFeedback`** (MonoBehaviour, на `---Player---`):
  - `_impulseSource` — `CinemachineImpulseSource`. Тряска: `GenerateImpulse(force)`, где `force = Mathf.Min(damage * _impulsePerDamage, _maxImpulse)`.
  - `_hurtClips[]` — массив клипов боли, выбирается случайный.
  - `_hurtVolume` — громкость.
  - `_damageVignette` — Image с радиальным градиентом.
  - `_flashPeak`, `_flashFadeSpeed` — вспышка при ударе, линейное затухание в `Update`.
  - Подписка на `OnPlayerDamaged` через `TrySubscribe()` (в `OnEnable` и `Start`, флаг `_subscribed`).

- **Cinemachine Impulse**:
  - `CinemachineImpulseSource` — на `---Player---`.
  - `CinemachineImpulseListener` — на **всех трёх** Cinemachine-камерах (`_thirdPersonVcam`, `_firstPersonVcam`, `_selfieVcam`).
  - Иначе тряска работает только на той камере, где стоит listener.

- **`DamageVignetteCanvas`** — Canvas (Screen Space Overlay, Sort Order 50) в `UICanvases`. Внутри — Image `DamageVignette` (растянут на весь экран, `Raycast Target: false`, `Color` с alpha=0).

### 18. Ragdoll трупов — итоговая схема

После нескольких итераций принята следующая логика:

- **Анимация смерти у существ убрана.** В `BaseLivingEntity.Die()` сразу вызывается `CreateCorpse()`, без `SetTrigger(animIDDeath)`.
  - Причина: Animator оставлял кости в позе смерти, и при активации ragdoll joint-ы «выталкивали» их в T-позу → дёргание и разлёт.
  - Реалистично: существа падают как куклы сразу после смерти (как в ARK).

- **`BaseLivingEntity.CreateCorpse()`** — новый подход:
  - **`Instantiate(creature.Data.prefab, position, rotation)`** вместо модификации живого робота.
  - `corpseGO` — свежий инстанс префаба, кости в T-позе, joint-ы в норме → поведение идентично загрузке из сохранения.
  - До `ActivateRagdoll`:
    - `Creature.enabled = false`.
    - `NavMeshAgent` — `Destroy`.
    - Все `Animator` в детях — `enabled = false`.
    - `SetLayerRecursively(corpseGO, Corpse)`.
  - Затем: `Corpse.enabled = true`, копирование лута из `CreatureData`, `CreateCorpseInventory`, `PopulateInventoryFromLootTable`.
  - **`CorpseManager.RegisterCorpse(corpseGO, "world", creatureId)`** — передаётся **corpseGO**, а не живой робот.
  - В конце — `Destroy(gameObject)` старого робота.

- **`Corpse.ActivateRagdoll()`** — сброс поз:
  - `CaptureInitialPoses()` в `Awake` — запоминает `localPosition` / `localRotation` каждой кости из `ragdollSettings.ragdollParts`.
  - `ResetPosesToInitial()` в начале `ActivateRagdoll()` — восстанавливает их перед `isKinematic = false`.
  - `Physics.SyncTransforms()` после сброса.
  - Это устраняет дёргание и разлёт.

- **`CreateCorpse` вызывается напрямую из `Die()`**, не через `OnDeathAnimationFinished`.

- **Известные особенности**:
  - «Щелчок» из позы смерти в T-позу при включённой анимации смерти — неизбежен. Поэтому анимация смерти **отключена**.
  - Труп — отдельный объект, не тот же самый. Ссылки на убитого робота инвалидируются (`Destroy`).

### 19. Приручение (Taming) и сохранение существ

Реализована система приручения (нокаут + кормление + прогресс) и сохранение нокаутнутых и прирученных существ между сессиями.

#### 19.1 Food-стат существ

- **`CreatureData`** — поля:
  - `maxFood` — максимум еды (100).
  - `foodDrainRateKnockedOut` — скорость падения еды в нокауте (0.5/сек).
  - `foodDrainRateTamed` — скорость падения еды у прирученного (0.1/сек).
  - `foodEatThresholdPercent` — порог (0..1), ниже которого существо ест.
  - `starvationDamagePerSecond` — урон при food == 0.
  - `torporRecoveryRate` — как быстро падает torpor.
  - `narcoticItem`, `narcoticTorporAmount` — наркотик и его эффект.

- **`BaseLivingEntity`** — поля:
  - `food`, `maxFood`, `GetFood()`, `AddFood()`, `ConsumeFood()`.
  - `SetFood()`, `SetHealth()`, `SetStamina()` — сеттеры для загрузки.

- **`Creature.Update`** — food-тик:
  - Падает у нокаутнутых (`foodDrainRateKnockedOut`) и прирученных (`foodDrainRateTamed`).
  - При `food <= maxFood * foodEatThresholdPercent` — `TryEatFood()`.
  - `TryEatFood` — ищет еду из `tamingFoodPreferences`, восстанавливает food, добавляет `tamingProgress`.
  - При завершении приручения — `FinishTaming()`.
  - **Сохранение сразу после еды** — `TamingManager.UpdateSave`.

#### 19.2 `TamingManager`

- **Сценовый**, на `===WorldManagers===` в GameWorld. Создаётся `WorldBootstrap.CreateWorldManagers()`.
- `Initialize()` берёт `CreatureDatabase`, `ItemDatabase` из `GameAssets.Instance`.
- **Убран `DontDestroyOnLoad`, убран `IsQuitting`.**
- `OnApplicationQuit/Pause/Focus` → `SaveAll()` — оставлены.
- По аналогии с `CorpseManager` и `LootBagManager`:
  - Папка `persistentDataPath/Taming/`, файлы `taming_{guid}.save`.
  - `RegisterCreature(creatureGO, ownerPlayerId)` — регистрирует нокаутнутое/прирученное, возвращает GUID.
  - `UpdateSave(instanceId, ownerPlayerId)` — пересохраняет состояние.
  - `UnregisterCreature(instanceId)` — удаляет файл (при пробуждении дикого, смерти, отмене). Логирует удаление/отсутствие.
  - `LoadAllTamingCreaturesAsync()` — загрузка с проверкой времени через `GameTime`.
  - `SpawnFromData(data)` — создаёт существо из префаба, восстанавливает состояние. **`saveKey = $"TamingCorpse_{data.instanceId}"`** (единый префикс).
  - `SaveAll()` — сохраняет **все** загруженные существа. Вызывается в `OnApplicationQuit`, `OnApplicationPause(true)`, `OnApplicationFocus(false)`.
  - Периодическое сохранение — раз в 10 секунд из `Creature.Update` (при `knockedOut || tamed`).

#### 19.3 `TamingSaveData`

- `instanceId`, `creatureId`.
- Состояние: `knockedOut`, `tamed`, `torpor`, `maxTorpor`, `tamingProgress`, `health`, `food`, `stamina`.
- `torporRecoveryRate`, `foodDrainRateKnockedOut`, `foodDrainRateTamed` — для расчёта времени.
- Позиция/поворот.
- `ownerPlayerId`, `customName`, `tamedState` — задел на будущее.
- `savedAtTime` — `GameTime.Now`.
- `inventoryData` — `SerializableInventory`.

#### 19.4 Загрузка нокаутнутого/прирученного

- **`BaseLivingEntity.KnockOutFromLoad()`** — применяет состояние нокаута без регистрации.
- **`BaseLivingEntity.TamedFromLoad()`** — применяет состояние прирученного.
- **`BaseLivingEntity.ExitKnockoutState()`** — общая логика выхода из нокаута: `DeactivateRagdoll`, включение AI, `animator.Rebind()`, `SetCreatureLayer()`, удаление taming-файла (если `!tamed`), удаление `ChestInventory` нокаута (если `!tamed`).
- **`BaseLivingEntity.FinishTaming()`** — вызывает `ExitKnockoutState`, сохраняет `tamed = true`, пересохраняет файл.
- **`BaseLivingEntity.Die()`** — удаляет `taming_{guid}.save` (`UnregisterCreature`), удаляет `ChestInventory` нокаута, потом создаёт труп через `CorpseManager`.

#### 19.5 Смена слоя `Corpse` ↔ `Creature`

- **`SetCreatureLayer()`** — метод в `BaseLivingEntity`, ставит слой `Creature` рекурсивно.
- Вызывается в `FinishTaming`, `TamedFromLoad`, `KnockOutFromLoad`.
- При смерти — `SetLayerRecursively(gameObject, Corpse)`.

#### 19.6 Единая система времени (`GameTime`)

- **`GameTime.cs`** (статический):
  - `Now` — `long`. SinglePlayer: накопленное игровое время. Multiplayer: Unix timestamp UTC.
  - `ElapsedSeconds(from, to)` — сколько секунд прошло.
  - `Initialize()`, `AddTime()`, `Save()`.
- **`GameTimeUpdater.cs`** — **глобальный**, создаётся из `[GameAssets].Awake()`:
  - Тикает `GameTime`, только если `Time.timeScale > 0` и `Application.isFocused`.
  - Сохраняет раз в 30 секунд, при `OnApplicationPause`, `OnApplicationFocus`, `OnApplicationQuit`.
- Используется в `CorpseManager`, `LootBagManager`, `TamingManager` — `savedAtTime` вместо `creationTimeUtc`.

#### 19.7 Использование наркотиков

- **`ItemUsageRouter.UseFromOtherInventory`** — обрабатывает использование наркотика на нокаутнутом.
- `target.AddTorpor(narcoticTorporAmount)`, сохранение через `TamingManager.UpdateSave`.

#### 19.8 Известные особенности

- **`ChestUIManager`** — решена проблема рассинхрона инвентарей. Все Open/Close идут через менеджер. `ChestUI.CurrentOpenChest` убран.
- **CS0252** — в `OnDestroy` источников `ReferenceEquals(CurrentSource, this)`, не `==`.
- **CS0108** — `IInventorySource` не дублирует `GetInventory`/`HasInventory` из `IInteractable`.
- **Save-файлы нокаутнутых.** Единый префикс `TamingCorpse_` (не `Taming_`). `ChestInventory` не пишет `Chest_*.save` для `TamingCorpse_*` — данные идут в `Taming/taming_{guid}.save`. Удаление файла и `ChestInventory` при выходе из нокаута (диким) и при смерти.
- **`TamingInstanceId`** — регистрация в `TamingManager` до создания инвентаря, чтобы `saveKey` был стабилен.
- **`TamingManager.SpawnFromData`** — `saveKey = $"TamingCorpse_{data.instanceId}"` (было `"Taming_..."`).
- **`RadialMenu`** — на префабе `corpse`/`creature` назначены **вручную**. Логика выбора в `PanelsUIController.OpenRadialMenu` учитывает `Corpse.enabled` и `BaseLivingEntity.tamed/knockedOut`.

## TODO

### Ближайшее (Следующий шаг)

- [ ] **`SettingsController`** — единая система настроек (MainMenu + Pause). Префаб UI + скрипт, хранение в JSON. Сюда же переедет `splitStackKey` из удалённого `InventoryConfig`.
- [ ] **AI прирученных существ** — Follow/Stay/Idle/Attack команды через `RadialMenu`.
- [ ] **Радиальное меню для существ** — команды вместо «ТЕЛО».
- [ ] **Фикс багов** — список собирается перед началом работы.

### Сделано (архив)

- [x] **ChestUIManager** — единая точка входа для инвентарей, устранён рассинхрон `_currentChest`/`_isOpen`.
- [x] **IInventorySource** — интерфейс источников инвентаря (Corpse, BaseLivingEntity, ChestController, LootBag).
- [x] **Сброс слотов в `ChestUI.Close`** — принудительный `SetSlot(null)`.
- [x] **`ChestUI.ForceSetChest`** — страховка в `OpenWith`.
- [x] **Фикс CS0252** — `ReferenceEquals` в `OnDestroy` источников.
- [x] **Фикс CS0108** — убраны дубли `GetInventory`/`HasInventory` из `IInventorySource`.
- [x] **Фикс течи save-файлов** нокаутнутых существ (единый префикс `TamingCorpse_`, удаление при выходе из нокаута/смерти).
- [x] **ChestController** — реализован `IInventorySource`, анимация `open`/`close`.
- [x] **LootBag** — реализован `IInventorySource`, открытие через менеджер.
- [x] **Выброс вещей → сумка** (`InventoryManager.DropItemFromSlot` → `LootBagManager.CreateLootBagFromItems`).
- [x] **Torpor-урон** — Item, Arrow, TakeDamage.
- [x] **Knockout** — KnockOut/RecoverFromKnockout в BaseLivingEntity.
- [x] **Инфо-панель** — EntityInfoPanelManager + EntityInfoPanel.
- [x] **Смена слоя при смерти** — SetLayerRecursively на Corpse.
- [x] **Цифры урона** (floating damage numbers) — всплывающие числа урона.
- [x] **Критические удары** (голова/слабое место) — множитель урона.
- [x] **Урон существ по игроку в ближнем бою** — фидбек (звук, эффект, тряска камеры).
- [x] **Food-стат у существ** — падение, поедание, порог.
- [x] **Taming: прогресс** — кормление, torpor, наркотики.
- [x] **Сохранение нокаутнутых** — `TamingManager`, `TamingSaveData`.
- [x] **Сохранение прирученных** — тот же `TamingManager`.
- [x] **`GameTime`** — единая система времени (сингл/мульти).
- [x] **Миграция `Corpse`/`LootBag` на `GameTime`** — `savedAtTime` вместо `creationTimeUtc`.
- [x] **Смена слоя Corpse↔Creature** — `SetCreatureLayer()`.
- [x] **Сохранение поворота игрока** — Euler углы.
- [x] **Исправление `Self referencing loop`** — фикс сериализации `Quaternion`.
- [x] **Логика `IsVisibleByCamera`** — набросок, но не подключена.
- [x] **`OnApplicationQuit/Pause/Focus` в `TamingManager`** — `SaveAll`.
- [x] **Переезд сценовых менеджеров на сцену GameWorld** — выполнено.
  - `[GameAssets]` в обеих сценах — глобальные менеджеры.
  - `WorldBootstrap` на `===WorldManagers===` — сценовые менеджеры.
  - `Corpse.OnDestroy` / `LootBag.OnDestroy` не удаляют файлы.
  - `PlayerSurvivalSystem`, `ChestUIManager` — сценовые.
  - `SceneBootstrap`, `ManagersSpawner`, `InventoryConfig` — удалены.
  - Префаб `===CoreManagers===` — удалён.

### Дальше

- [ ] **Использование прирученных** (Riding, команды, инвентарь).
- [ ] **Респавн по кроватям** (спальные мешки, точки возрождения).
- [ ] **Экосистема** (хищники/жертвы, respawn по регионам).
- [ ] **Броня** (защита, прочность, резисты).
- [ ] **Разрушение сундука → сумка** (`ChestController.OnDestroy` → `LootBagManager`).
- [ ] **Отображение владельца сумок/трупов в UI:**
  - В `PanelsUIController` — заголовок панели чужого инвентаря с `OwnerPlayerId` / именем существа.
  - В `EntityInfoPanelManager` — показывать владельца для `LootBag` и `Corpse`.
- [ ] **Карта местности**.
- [ ] **Фермерство**.
- [ ] **Температура / погода** (гипертермия/гипотермия, биомы).
- [ ] **Статы** (доводка системы, влияние на геймплей).
- [ ] **Биомы + ресурсы по регионам**.
- [ ] **Разведение** (спаривание, imprinting, мутации).
- [ ] **Племена**.
- [ ] **Боссы**.
- [ ] **Мультиплеер** (Mirror / Netcode for GameObjects).
- [ ] **Система выбора персонажа** (меши, материалы, blend shapes).
- [ ] **Building stability** (Structural integrity).
- [ ] **Electrical system** (Generators, Cables, Appliances).
- [ ] **Irrigation system** (Pipes, Taps, Reservoirs).
- [ ] **Blueprints & Quality tiers**.



## Важные файлы для контекста

При создании нового чата скинуть:
Продолжаем ARK-клон. Только что завершили рефакторинг менеджеров
(ветка `refactoring-managers` слита в main). Всё работает:
цикл MainMenu → GameWorld → MainMenu, сохранение/загрузка трупов,
сумок, прирученных, построек.

Глобальные менеджеры — в `[GameAssets]` (обе сцены, DontDestroyOnLoad).
Сценовые — в `WorldBootstrap` на `===WorldManagers===` (GameWorld).
`Corpse.OnDestroy`/`LootBag.OnDestroy` больше не удаляют файлы.

Теперь делаем (): 
Прикладываю project-architecture.md и файлы.

## Формат работы в чате
Не пиши сразу весь код. Вначале обсуждаем, потом идем по пунктам. 
После каждого пункта тестируем, подтверждаем, если есть баги чиним, только потом идем к следующему пункту. 
При рассуждении в конце подводи итоги, что и где исправить.
Если я скинула логи и описание бага, не строй сразу предположения, задавай вопросы в какой ситуации произошел баг и требуй скриншоты, логи, актуальные файлы.
После закрытия очередного пункта предлагай коммит в краткой форме на русском языке и обновления файла project-architecture.md.

## СЦЕНЫ

## MainMenu

MainMenu (Scene)
│
├── [GameAssets]                                       ← корневой, DontDestroyOnLoad
│   ├── GameAssets (Script)
│   │     ├── _gameSettings: GameSettings
│   │     ├── _itemDatabase: ItemDatabase
│   │     ├── _creatureDatabase: CreatureDatabase
│   │     ├── _recipeDatabase: RecipeDatabase
│   │     ├── _playerCorpsePrefab: GameObject
│   │     └── _lootBagPrefab: GameObject
│   │
│   ├── LoadingScreenManager (Script)
│   │     ├── _loadingCanvas: LoadingScreenCanvas
│   │     ├── _progressBar: ProgressBar (Image)
│   │     └── _statusText: StatusText (TMP)
│   │
│   ├── AudioManager (Script)
│   │
│   ├── LanguageManager (Script)
│   │
│   └── LoadingScreenCanvas (GameObject, child)
│       └── Canvas (Screen Space - Overlay, sortingOrder = 9999)
│           ├── Background (Image, чёрный, на весь экран)
│           ├── ProgressBar (Image, fill = 0)
│           └── StatusText (TextMeshProUGUI)
│
├── MainMenuManager (GameObject)
│   └── MainMenuManager (Script)
│         ├── MainMenuPanel: GameObject
│         ├── SettingsPanel: GameObject
│         ├── GameScene: "GameWorld"
│         ├── timeScale: 1
│         └── _settingsController: SettingsPanelController
│
├── EventSystem (GameObject)
│   └── EventSystem (Script) + StandaloneInputModule
│
├── Camera (GameObject)
│   └── Camera (Component)
│
└── Canvas_MainMenu (GameObject)
    └── Canvas (Screen Space - Overlay)
        ├── MainMenuPanel (GameObject)
        │     ├── Кнопка "Играть"    → MainMenuManager.LoadGameScene()
        │     ├── Кнопка "Настройки" → MainMenuManager.OpenSettings()
        │     └── Кнопка "Выход"     → MainMenuManager.quitGame()
        └── SettingsPanel (GameObject)
              └── (UI настроек)

## GameWorld

GameWorld (Scene)
│
├── [GameAssets]                                       ← корневой, DontDestroyOnLoad
│   ├── GameAssets (Script)                            ← при Awake видит Instance != null → удаляет себя
│   ├── LoadingScreenManager (Script)                  ← удаляется вместе с GameObject
│   ├── AudioManager (Script)                          ← удаляется
│   ├── LanguageManager (Script)                       ← удаляется
│   └── LoadingScreenCanvas (GameObject, child)        ← удаляется
│   # Если игра стартует с GameWorld напрямую — этот [GameAssets] становится primary (DontDestroyOnLoad),
│   # и удаляется только [GameAssets] из MainMenu (если он там есть при повторной загрузке).
│
├── ===WorldManagers=== (GameObject)                   ← сценовый, умирает со сценой
│   └── WorldBootstrap (Script)
│         ├── _chunkSize: 32
│         ├── _loadRadiusInChunks: 2
│         ├── _saveInterval: 5
│         └── (в Start → корутина: ждёт GameAssets, создаёт CorpseManager/LootBagManager/TamingManager/WorldManager)
│
│   ── компоненты, добавляемые на ===WorldManagers=== через AddComponent: ──
│       ├── CorpseManager
│       ├── LootBagManager
│       ├── TamingManager
│       └── WorldManager
│
├── ===GameManagers=== (GameObject)                    ← сценовый
│   ├── HUDManager (Script)
│   ├── UIManager (Script)
│   ├── PauseManager (Script)
│   ├── DeathScreenManager (Script)
│   ├── NotificationManager (Script)
│   ├── TooltipManager (Script)
│   ├── ContextMenuManager (Script)
│   ├── CharacterPreviewManager (Script)
│   ├── CombatAudioManager (Script)
│   ├── ArrowPool (Script)
│   ├── AimUI (Script)
│   ├── EntityInfoPanelManager (Script)
│   └── DamageNumberPool (Script)
│
├── ===InventoryManager=== (GameObject)                ← сценовый
│   ├── InventoryManager (Script)
│   └── ChestUIManager (Script)
│
├── ===PanelsController=== (GameObject)                ← сценовый
│   └── PanelsUIController (Script)
│
├── ---Player--- (GameObject, Tag: Player, Layer: Player)
│   ├── CharacterController
│   ├── Capsule Collider
│   ├── Basic Rigid Body Push (Script)
│   ├── Player Input
│   ├── PlayerInputHandler (Script)
│   ├── CameraManager (Script)
│   ├── PlayerEquipment (Script)
│   ├── PlayerRangedCombat (Script)
│   ├── PlayerBuildMode (Script)
│   ├── ItemUsageSystem (Script)
│   ├── ItemHandler (Script)
│   ├── PlayerInteraction (Script)
│   ├── PlayerAimingSystem (Script)
│   ├── PlayerController (Script)
│   ├── PlayerSurvivalSystem (Script)                  ← СЦЕНОВЫЙ
│   ├── CinemachineImpulseSource
│   ├── PlayerDamageFeedback (Script)
│   ├── ItemUsageRouter (Script)
│   │
│   ├── VisualCharacter (child)
│   │     └── (меш персонажа, Animator)
│   ├── Head (child)
│   ├── EyeCenterForCamera (child)
│   └── CinemachineCameraTarget (child)
│
├── Cameras (GameObject)
│   ├── Directional Light
│   ├── MainCamera (Tag: MainCamera)
│   ├── ThirdPersonPlayerFollowCamera
│   ├── FirstPersonCamera
│   ├── SelfieCamera
│   ├── PostProcessingDefaultVolume
│   ├── UnderWaterVolume
│   ├── PlayerHurtVolume
│   └── CharacterPreviewCamera
│
├── UICanvases (GameObject)
│   ├── InventoryCanvas
│   ├── InteractionCanvas
│   ├── AimCanvas
│   ├── NotificationTopCanvas
│   ├── NotificationLeftCanvas
│   ├── HUDCanvas
│   ├── ContextMenuCanvas
│   ├── EntityInfoCanvas
│   ├── DamageNumberCanvas
│   ├── DamageVignetteCanvas
│   ├── RadialMenuCanvas
│   ├── PauseCanvas
│   └── DeathScreenCanvas
│
├── EventSystem (GameObject)
│
├── NavMeshBacker (GameObject)
│
├── _Env (GameObject)                                  ← окружение, ландшафт
│
├── Interactables (GameObject)                         ← ящики, костры и т.д.
│
├── NPC_1 (GameObject)
├── Robot_George (GameObject)
│
└── __CreatureSpawner__ (GameObject)




## СТОЛПЫ АРК
+Собирательство/добыча
+Крафт
+Строительство 
+Инвентарь
+Прогрессия
+Боевка
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