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

### 20. Система уведомлений (top-note) со звуками

Общая система всплывающих уведомлений с поддержкой звуков по типу события.

#### 20.1 `NotificationType` (enum)

`Assets/Scripts/UI/Notifications/NotificationType.cs`:
- `None`, `LevelUp`, `TameComplete`.
- Расширяемый: для новых событий достаточно добавить значение.

#### 20.2 `NotificationSoundConfig` (ScriptableObject)

`Assets/Scripts/UI/Notifications/NotificationSoundConfig.cs`:
- `Entry[] entries` — массив `{ NotificationType type, AudioClip clip, float volume }`.
- `TryGet(type, out clip, out volume)` — поиск звука по типу.
- Ассет: `Assets/Data/Audio/NotificationSoundConfig.asset`.
- Ссылка в `GameAssets.NotificationSoundConfig`.

#### 20.3 `NotificationManager` — звук

- `_uiAudioSource` (2D, `spatialBlend = 0`) — создаётся в `Awake`, если не назначен.
- `PlayNotificationSound(NotificationType type)` — берёт клип из `GameAssets.Instance.NotificationSoundConfig`, играет через `PlayOneShot` с учётом `AudioManager.masterVolume`.
- Если тип не найден или клип null — молчание + warning в редакторе (`#if UNITY_EDITOR`).

#### 20.4 Интеграция
PlayerProgress.ShowLevelUpNote() → ShowTopNote(NotificationType.LevelUp, "...").

BaseLivingEntity.FinishTaming() → ShowTopNote(NotificationType.TameComplete, $"{GetDisplayName()} приручен!").

#### 20.5 Что НЕ сделано (задел)
Иконки в top-note — отложены.
Цвета текста по типу — отложены.
Отдельные конфиги для левого уведомления (Show) — не тронуты.

### 21. Радиальное меню (Radial Menu)

Динамическое радиальное меню с поддержкой многоуровневых подменю, Action/Radio-кнопок
и конфигурируемых наборов для разных типов сущностей.

#### 21.1 Данные

- **`RadialMenuEntryType`** (enum): `Action`, `Submenu`, `Radio`.
- **`RadialMenuEntry`** (serializable):
  - `label`, `icon`, `type`, `actionId`, `radioGroupId`, `radioValue`,
    `useDynamicLabel`, `hideIfUnavailable`, `children` (`[SerializeReference]`).
- **`RadialMenuConfig`** (SO): `titleOverride`, `rootEntries`.
- **`RadialMenuGlobalConfig`** (SO): `tamedMenu`, `knockedOutMenu`, `corpseMenu`,
  `structureMenu`, `chestMenu`. Один ассет на игру, ссылка в `GameAssets`.

#### 21.2 UI

- **`RadialMenuController`** (MonoBehaviour, на `===GameManagers===`):
  - Стек уровней (`List<RadialMenuEntry[]>`), `MAX_DEPTH = 3`.
  - `Open(ctx)`, `Close()`, `GoBack()`.
  - Динамическое построение кнопок из `RadialMenuEntry[]`.
  - Позиционирование по кругу (`_radius`, `_startAngleDeg`, `_maxButtonsPerRing`, `_ringStep`).
  - `[Назад]` добавляется автоматически на уровнях > 0.
  - Radio → `SetRadioValue` + автовозврат на уровень выше.
  - Action → `ExecuteAction` + `Rebuild` (обновить labels).
  - `useDynamicLabel` → `GetDynamicLabel(actionId)`.
  - `hideIfUnavailable` → `IsActionAvailable(actionId)`.
- **`RadialMenuButton`** (MonoBehaviour на префабе): `_label`, `_button`, `_icon`, `Setup(...)`.
- **`RadialMenuCanvas/Panel`**: `RadialMenuButtons` (контейнер, без `GridLayoutGroup`), `CloseRadialMenuButton`, `TargetName`, `Bg`.

#### 21.3 Контекст

- **`IRadialMenuContext`**: `GetMenuTitle`, `GetMenuConfig`, `IsActionAvailable`,
  `ExecuteAction`, `GetRadioValue`, `SetRadioValue`, `GetDynamicLabel`.
- **`RadialMenu`** (MonoBehaviour на префабах):
  - Реализует `IRadialMenuContext` + `IInteractable`.
  - Для существ: `IInteractable.GetInteractType = None` (отвечает `BaseLivingEntity`).
  - Для структур: `GetInteractType = RadialMenu` (показывает E, открывает меню).
  - `GetMenuConfig()` — по состоянию: `knockedOut` → `tamed` → `corpse` → `chest` → `structure` (из `RadialMenuGlobalConfig`).
  - `IsActionAvailable` — по флагам `CreatureData` (`canBePickedUp`, `canBeRidden`).

#### 21.4 Действия (в `BaseLivingEntity`)

- `ExecuteRadialAction(actionId)`: `toggle_follow`, `toggle_wander`, `toggle_mating`,
  `open_inventory`, `pick_up` (TODO), `ride` (TODO), `rename` (TODO), `unclaim`.
- `GetRadialRadioValue(groupId)` / `SetRadialRadioValue(groupId, value)`:
  - `behavior` ↔ `TamedBehavior` (passive / flee / neutral / attack_owner_target / aggressive).
  - `follow_distance` ↔ `FollowDistance` (lowest / low / medium / high / highest).
- `GetRadialDynamicLabel(actionId)`: динамический текст для toggle-кнопок
  (`Включить следование` / `Отключить следование`).
- `Unclaim()` — снять приручение (стать диким), удалить сейв и инвентарь.

#### 21.5 Команды прирученного

- **`TamedBehavior`** (enum): `Passive`, `Flee`, `Neutral`, `AttackOwnerTarget`, `Aggressive`.
- **`FollowDistance`** (enum): `Lowest`, `Low`, `Medium`, `High`, `Highest`.
- **Поля в `BaseLivingEntity`**: `tamedFollowing`, `tamedWandering`, `tamedMating`,
  `tamedBehavior`, `tamedFollowDistance`.
- **Взаимоисключение**: `SetTamedFollowing(true)` → `tamedWandering = false`; и наоборот.
- **Сохранение**: в `TamingSaveData` (`tamedFollowing`, `tamedWandering`, `tamedMating`,
  `tamedBehavior`, `tamedFollowDistance`). Удалено старое поле `tamedState` (int).
- **`FinishTaming`**: дефолтные команды — `Following = true`, `Behavior = AttackOwnerTarget`,
  `FollowDistance = Medium`.

#### 21.6 Флаги способностей (`CreatureData`)

- `canBePickedUp` — кнопка «Подобрать».
- `canBeRidden` — кнопка «Оседлать».
- Кнопки скрыты через `hideIfUnavailable` + `IsActionAvailable`.

#### 21.7 Известные особенности

- `RadialMenuEntry.children` — `[SerializeReference]`, иначе Unity warning про serialization depth.
- `GetInteractType()` в `BaseLivingEntity` для прирученных/нокаутнутых возвращает
  `InteractType.RadialMenu` (hold E), а для инвентаря — `InteractType.OpenTargetInventory` (F).
- `RadialMenu` (новый) — на том же GameObject, что `BaseLivingEntity`.
- `RadialMenuCanvas` включается автоматически через `RadialMenuController._menuRoot.SetActive(true)` в `Open()`.

### 22. Действия радиального меню для структур, трупов и сундуков

Радиальное меню работает не только для существ, но и для структур/трупов/сундуков.
Каждый тип имеет свой набор действий через `ExecuteRadialAction`.

#### 22.1 `Structure.ExecuteRadialAction`

Файл: `Assets/Scripts/Structures/Structure.cs`.

Действия:
- `pick_up` — `ItemHandler.PickupItem(item)` + `DestroyItem(gameObject)`.
- `destroy` — `ItemHandler.DestroyItem(gameObject)`.
- `open_inventory` — `OpenInventory()` (если есть инвентарь).
- `rename` — TODO.

#### 22.2 `Corpse.ExecuteRadialAction`

Файл: `Assets/Scripts/Corpses/Corpse.cs`.

Действия:
- `drag_corpse` — `StartDragging()` / `StopDragging()`.
- `open_inventory` — `OpenInventory()`.

#### 22.3 `RadialMenu.ExecuteAction` — делегирование

Файл: `Assets/Scripts/Interactables/RadialMenu.cs`.
public void ExecuteAction(string actionId)
{
    if (_living != null) { _living.ExecuteRadialAction(actionId); return; }
    if (corpse != null && corpse.enabled) { corpse.ExecuteRadialAction(actionId); return; }
    if (structure != null) { structure.ExecuteRadialAction(actionId); return; }
    if (chest != null) { chest.Open(); return; }   // fallback для сундуков
    Debug.Log($"[RadialMenu] Action: {actionId} (нет обработчика)");
}

#### 22.4 Конфиги
tamedMenu — меню прирученного (общее для всех видов).

knockedOutMenu — меню нокаутнутого.

corpseMenu — меню трупа (drag_corpse, open_inventory).

structureMenu — меню структур (pick_up, destroy, repair).

chestMenu — меню сундуков (open_inventory, destroy, repair).

Все — в GameAssets.RadialMenuGlobalConfig.

#### 22.5 Открытие инвентаря через радиальное меню
ChestUIManager.Open теперь вызывает PanelsUIController.OpenOtherInventory() после успешного OpenWith. Это устраняет дублирование вызовов и делает открытие инвентаря единообразным — и через F (TargetInventory), и через Action open_inventory.

#### 22.6 Сброс «зажатой E» при закрытии меню
Проблема: игрок зажимает E → открывается меню → клик по Action (Открыть инвентарь) → меню закрывается, инвентарь открывается, но E всё ещё физически зажата. OnInteractTriggered (RepeatInteract) продолжает дёргаться → UseSelectedSlot используется при наведении на слот.

Решение:

PlayerInputHandler.ForceResetInteract() — принудительно сбрасывает _isInteractHeld, останавливает RepeatInteract, дёргает OnInteractStopPressed.

Флаг _interactWasForceReset — чтобы последующий реальный OnInteractCanceled (от физического отпускания E) не дёргал OnInteractStopPressed повторно.

PanelsUIController.CloseRadialMenu — вызывает _input.ForceResetInteract().

Все подписчики (_inventoryManager, _itemUsageRouter) корректно отписываются.

#### 22.7 Сортировка канвасов
RadialMenuCanvas.sortingOrder = 1, HUDCanvas = 0.

Иначе HUD перехватывает raycast (hover/клики не работают).

GridLayoutGroup на RadialMenuButtons убран — позиционирование по кругу через RadialMenuController.PositionButton.

#### 22.8 Известные особенности
RadialMenu.GetInteractType() возвращает InteractType.RadialMenu — для структур.

Для существ отвечает BaseLivingEntity.GetInteractType().

Panel.Image.RaycastTarget — если true, перехватывает клики. Лучше выключить (декоративный фон).

AimCanvas / HUDCanvas — RaycastTarget только на кнопках.

### 23. Боевая система существ

Существа наносят урон друг другу и игроку. Работает в обе стороны:
дикий бьёт игрока и его прирученных; прирученный (в Aggressive) бьёт диких.

#### 23.1 Урон существо→существо

- **`BaseLivingEntity.TakeDamage(float damage, float torporAmount = 0f)`** — overload
  без `PlayerInteraction`. Используется, когда атакующий — существо.
- Внутри — `TakeDamage(damage, torpor, null, null, null)`, `playerInteraction` может быть `null`.
- Popup урона — в `transform.position + up`, если `playerInteraction == null`.

#### 23.2 Текущая цель атаки (`Creature`)

- **`_currentAttackTarget`** (`BaseLivingEntity`) — цель-существо.
- **`_isAttackingPlayer`** (bool) — цель — игрок.
- **`_wildTargetTransform`** (`Transform`) — кэш трансформа для chase (дикий AI).

#### 23.3 Универсальный `DealDamageToPlayer`

Имя оставлено для совместимости с Animation Event. Логика:
- Если `!_isAttackingPlayer && _currentAttackTarget != null` → бьёт существо.
- Иначе → `PlayerSurvivalSystem.TakeDamage(AttackDamage)`.

#### 23.4 Прирученный бьёт диких (Итерация 1)

- В `ChaseAndAttack(target)` — устанавливается `_currentAttackTarget` или `_isAttackingPlayer`.
- `PerformAttack` → Animation Event → `DealDamageToPlayer` → бьёт цель.
- `ResetAttackState` для прирученного — сбрасывает `_currentAttackTarget`, `_isAttackingPlayer`.

#### 23.5 Дикий бьёт игрока и прирученных (Итерация 2)

- **`FindWildTarget()`** — `OverlapSphere` по слою `Creature`:
  - Игрок (`_playerTransform`) — если в радиусе `AggressionRadius`.
  - Прирученные (`living.tamed && IsAlive() && !knockedOut`).
  - **Ближайшая** цель побеждает.
- `Creature.Update` (дикий) → `FindWildTarget` → Chase / Attack к `_wildTargetTransform`.
- `HandleChasing` больше не используется в диком AI.

#### 23.6 Известные особенности

- **Дикие не бьют диких** — фильтр `!living.tamed` в `FindWildTarget`.
  Пищевая цепочка (хищник vs травоядное) — отдельная задача.
- **Прирученный не реагирует на удары** (`Neutral` / `Flee` / `AttackOwnerTarget` — заглушки).
  Нужна система «кто ударил» (кто последний нанёс урон).
- **`FindWildTarget` вызывается каждый кадр** в `Update` — `OverlapSphere`.
  Оптимизация (раз в 0.2 сек) — задача на будущее.
- **`_wildTargetTransform`** — кэш для `SetDestination`. Сбрасывается в `ResetAttackState`.
- **Animation Event** `DealDamageToPlayer` — имя не менялось, для совместимости.

### 24. Поведения прирученных: Flee / Neutral / AttackOwnerTarget

Полная реализация всех `TamedBehavior` с ограничениями радиусов.

#### 24.1 Память об атакующем (`BaseLivingEntity`)

- **`_lastAttacker`** (`BaseLivingEntity`) + `LastAttackedTime`.
- **`LastAttacker`** (свойство) — возвращает `null`, если прошло > `ATTACKER_MEMORY_DURATION` (15 сек).
- **`NotifyAttackedBy(attacker)`** — вызывается атакующим **перед** `TakeDamage`.
- **`ClearLastAttacker()`** — принудительный сброс.

#### 24.2 `CreatureData` — новые параметры

public float tamedFleeDistance = 20f;            // Flee: макс. дистанция убегания
public float tamedFleeMaxThreatDistance = 30f;   // Flee: обидчик дальше — Flee кончается
public float tamedNeutralChaseRadius = 20f;      // Neutral: радиус chase
public float tamedAttackOwnerRadius = 30f;       // AttackOwnerTarget: радиус от игрока


#### 24.3 Поведения (Creature.HandleTamedState)
Приоритет:

Flee — если атакован, бежать от обидчика (пока он в tamedFleeMaxThreatDistance).

Neutral — если атакован, атаковать обидчика (пока он в tamedNeutralChaseRadius).

AttackOwnerTarget — если игрок в tamedAttackOwnerRadius, искать врага рядом с игроком (FindEnemyNearPlayer) и атаковать.

Aggressive — искать врага в tamedAggroRadius (существующая логика).

Follow — если tamedFollowing.

Wander — если tamedWandering.

Idle — иначе.

#### 24.4 Creature.DealDamageToPlayer
Перед TakeDamage вызывается target.NotifyAttackedBy(this). Так цель запоминает атакующего.

#### 24.5 FleeFrom(threat)
Вычисляет awayDir = (position - threat.position).normalized, targetPos = position + awayDir * 5f.
NavMesh.SamplePosition → SetDestination.
Каждый кадр пересчитывается — если обидчик догоняет, направление меняется.

#### 24.6 FindEnemyNearPlayer
OverlapSphere вокруг игрока (_playerTransform.position) в радиусе tamedAggroRadius.
Фильтр: !tamed, IsAlive(), не сам.
Возвращает ближайшего к прирученному.

#### 24.7 Что НЕ сделано (заглушки)
«Кто ударил игрока» — система есть только для BaseLivingEntity, не для игрока.
AttackOwnerTarget использует «враг рядом с игроком» — не «конкретный обидчик».

Рефакторинг Creature.cs — файл раздут (~700 строк).
AI / боевка / приручение / анимация смешаны.
Планируется вынести в CreatureCombat или State Machine.

### 25. Рефакторинг Creature: компонентная архитектура

Цель: `Creature` = hub, компоненты = модули. `BaseLivingEntity` — данные + интерфейсы.

#### 25.1 Архитектура
GameObject "Dodo"
├── Creature : BaseLivingEntity (hub)
├── CreatureCombat (боевка, Этап 1)
├── CreatureDeath (смерть, Этап 2)
├── CreatureKnockout (нокаут, Этап 3)
├── CreatureTaming (приручение + еда, Этап 4)
├── CreatureCommands (радиальное меню, Этап 5)
├── CreatureAudio (звуки, Этап 6)
├── CreatureWildAI (дикий AI, Этап 7)
└── CreatureTamedAI (прирученный AI, Этап 8)


#### 25.2 Этап 1: `CreatureCombat`

**Файл:** `Assets/Scripts/Creatures/Components/CreatureCombat.cs`.

**Содержит:**
- Урон: `TakeDamage` (3 overload), `ReduceHealth`, `PlayDamageEffect`.
- Память об атакующем: `LastAttacker`, `NotifyAttackedBy`, `ClearLastAttacker`.
- Атака: `PerformAttack`, `DealDamageToTarget`, `ResetAttackState`, `ChaseAndAttack`.
- Поиск целей: `FindWildTarget`, `FindNearestEnemy`, `FindEnemyNearPlayer`, `UpdateCurrentEnemy`.
- Бегство: `FleeFrom`.
- Свойства: `CurrentAttackTarget`, `IsAttackingPlayer`, `WildTargetTransform`, `CurrentEnemy`, `LastAttackTime`, `IsAttacking`.

**Ссылки:** `_creature` (получается в `Awake` через `GetComponent<Creature>()`).

**Не содержит:** `Update` — оркестрация через `Creature.Update`.

#### 25.3 `BaseLivingEntity` — изменения

- `TakeDamage` (4 overload) — **виртуальные заглушки** `{ }` (для полиморфизма).
- `LastAttacker`, `NotifyAttackedBy`, `ClearLastAttacker` — **удалены** (перенесены в `CreatureCombat`).
- Добавлены методы для `CreatureCombat`: `ReduceHealth`, `AddTorporDirect`, `IsHealthBelowZero`, `SetDead`.
- `LastAttacker` — теперь через `_creature.Combat.LastAttacker`.

#### 25.4 `Creature` — hub

**Ссылки на компоненты:**
- `_combat` (`CreatureCombat`).

**Публичные геттеры для компонентов:**
- `Agent` (`NavMeshAgent`), `PlayerTransform`, `Combat`, `Animator`, `AnimIDAttack`, `AnimIDTakeDamage`.
- `RequestDeath()` → `Die()`.

**Override `TakeDamage`:**

public override void TakeDamage(float damage, float torporAmount = 0f)
    => _combat.TakeDamage(damage, torporAmount);
// ... и остальные overload'ы

#### 25.5 PlayerInteraction — изменения
_hitCreature — тип Creature (было BaseLivingEntity).

GetComponent<Creature>() вместо GetComponent<BaseLivingEntity>().

Почему: Creature — единственный наследник BaseLivingEntity. Если появится NPC без Creature — вернём BaseLivingEntity + abstract TakeDamage.

#### 25.6 Известные особенности
CreatureCombat содержит _lastAttacker (дублирование с BaseLivingEntity — на Этапе 1 убрали из базы, но NotifyAttackedBy пишет в CreatureCombat).

Поля движения (isWandering, nextWanderTime, currentState) — в Creature, до Этапов 7-8.

ResetAttackState для дикого — временно упрощён (без WildAI.SetState, будет на Этапе 7).

Animation Event DealDamageToTarget — в CreatureCombat.

#### 25.7 Этап 2: CreatureDeath ✅

**Файл:** `Assets/Scripts/Creatures/Components/CreatureDeath.cs`.

**Содержит:**
- `Die()` — смерть, вызов `SetDead()`, закрытие инвентаря, `TamingManager.UnregisterCreature`, уничтожение `ChestInventory`, `CreateCorpse()`, `SetBodyRagdoll()`, `OnDeath?.Invoke()`.
- `CreateCorpse()` — `Corpse.enabled = true`, копирование лута из `CreatureData`, `InitializeCorpse`, `CreateCorpseInventory`, `PopulateInventoryFromLootTable`, `CorpseManager.RegisterCorpse`, слой `Corpse`.
- `SetBodyRagdoll(bool destroyAgent)` — `Animator.WriteDefaultValues + enabled = false`, `RagdollController.ActivateRagdoll + StopMovingRagdoll`, `NavMeshAgent.enabled = false`.

**Ссылки:** `_creature` (в `Awake`).

#### 25.8 `BaseLivingEntity` — изменения (Этап 2)

- `Die`, `CreateCorpse` — **удалены**.
- `SetBodyRagdoll` — **оставлен** (нужен `KnockOut` до Этапа 3).
- `OnDeath`, `SetDead`, `_isDead`, `IsDead`, `IsAlive` — **остаются**.

#### 25.9 `Creature` — изменения (Этап 2)

- `_death` (`CreatureDeath`) — ссылка в `Awake`.
- `Death => _death` — геттер.
- `Die()` — обёртка, вызывает `_death.Die()`.
- `RequestDeath()` — вызывает `Die()`.
- `DieManually` (в `Update`) → `Die()`.
- `TickFood` (голодная смерть) → `Die()`.

#### 25.10 Этап 3: CreatureKnockout ✅

**Файл:** `Assets/Scripts/Creatures/Components/CreatureKnockout.cs`.

**Содержит:**
- `KnockOut()` — установка `knockedOut = true`, `SetBodyRagdoll(false)`, отключение `Corpse`, включение `RadialMenu`, создание `ChestInventory` (если нет), регистрация в `TamingManager`.
- `KnockOutFromLoad()` — установка `knockedOut = true`, ragdoll, `SetCreatureLayer`, включение `RadialMenu` (без регистрации).
- `RecoverFromKnockout()` — проверка `!IsDead && knockedOut` → `ExitKnockoutState()`.
- `ExitKnockoutState()` — `knockedOut = false`, `torpor = 0`, `TamingManager.UnregisterCreature` (если не tamed), `DeactivateRagdoll`, `Warp` на NavMesh (если off-mesh), включение `Animator`, `RecoverFromKnockout` через `creature.enabled = true`, отключение `Corpse` и `RadialMenu`, `CloseInventory`, удаление `ChestInventory` (если не tamed).
- `SetBodyRagdoll(bool destroyAgent)` — ragdoll + `Agent.enabled = false`.

**Ссылки:** `_creature` (в `Awake`).

#### 25.11 `BaseLivingEntity` — изменения (Этап 3)

- `KnockOut`, `KnockOutFromLoad`, `RecoverFromKnockout` — **виртуальные заглушки**.
- `ExitKnockoutState`, `SetBodyRagdoll` — **удалены**.
- `FinishTaming` → вызывает `RecoverFromKnockout()` (вместо `ExitKnockoutState()`).
- `AddTorpor` → вызывает `KnockOut()` (виртуальный → `Creature` → `_knockout`).

#### 25.12 `Creature` — изменения (Этап 3)

- `_knockout` (`CreatureKnockout`) — ссылка.
- `Knockout => _knockout` — геттер.
- `override KnockOut`, `KnockOutFromLoad`, `RecoverFromKnockout` → делегируют в `_knockout`.
- Геттеры: `AnimIDIsMoving`, `AnimIDSpeed`, `AnimIDDeath`.


#### 25.13 Этап 4: CreatureTaming ✅

**Файл:** `Assets/Scripts/Creatures/Components/CreatureTaming.cs`.

**Содержит:**
- `Tick()` — food tick (drain для knockedOut/tamed) + `ShouldEat` + `TryEatFood` + периодическое сохранение (раз в 10 сек).
- `AddTamingProgress`, `FinishTaming`, `Unclaim`.
- `AddTorpor`, `AddFood`, `ConsumeFood`.
- `TickFood`, `ShouldEat`, `TryEatFood`.

**Ссылки:** `_creature`, `TamingManager`, `NotificationManager`.

#### 25.14 `BaseLivingEntity` — изменения (Этап 4)

- `AddTamingProgress`, `AddTorpor`, `AddFood`, `ConsumeFood`, `Unclaim` — **виртуальные заглушки**.
- `FinishTaming` — **удалён**.
- `SetCreatureLayer` — **public** (доступ из `CreatureTaming`).

#### 25.15 `Creature` — изменения (Этап 4)

- `_taming` (`CreatureTaming`) — ссылка.
- `Taming => _taming` — геттер.
- `Health` — public property (get/set).
- Override `AddTamingProgress`, `AddTorpor`, `AddFood`, `ConsumeFood`, `Unclaim`.
- `Update`: `_taming.Tick()` вместо food-блока.
- Удалены: `TickFood`, `ShouldEat`, `TryEatFood`, `_tamingSaveTimer`, `TamingSaveInterval`.

#### 25.16 Этап 5: CreatureCommands ✅

**Файл:** `Assets/Scripts/Creatures/Components/CreatureCommands.cs`.

**Содержит:**
- Команды: `SetTamedFollowing`, `SetTamedWandering`, `SetTamedMating`, `SetTamedBehavior`, `SetFollowDistance`.
- Радиальное меню: `ExecuteRadialAction`, `GetRadialRadioValue`, `SetRadialRadioValue`, `GetRadialDynamicLabel`.
- Хелперы: `TamedBehaviorToId`, `IdToTamedBehavior`, `FollowDistanceToId`, `IdToFollowDistance`, `TamedBehaviorToText`.

**Ссылки:** `_creature`, `TamingManager`.

#### 25.17 `BaseLivingEntity` — изменения (Этап 5)

- `SetTamed*`, `SetFollowDistance`, `ExecuteRadialAction`, `GetRadialRadioValue`,
  `SetRadialRadioValue`, `GetRadialDynamicLabel`, `GetBehaviorText` — **виртуальные заглушки**.
- Хелперы enum↔string — **удалены** (перенесены в `CreatureCommands`).
- Поля `tamedFollowing`, `tamedWandering`, `tamedMating`, `tamedBehavior`, `tamedFollowDistance` — **остаются** (данные).

#### 25.18 `Creature` — изменения (Этап 5)

- `_commands` (`CreatureCommands`) — ссылка.
- `Commands => _commands` — геттер.
- Override `SetTamed*`, `SetFollowDistance`, `ExecuteRadialAction`, `GetRadialRadioValue`,
  `SetRadialRadioValue`, `GetRadialDynamicLabel`, `GetBehaviorText`.

#### 25.19 Fix: `NotifyAttackedBy` на цели

**Баг:** `CreatureCombat.DealDamageToTarget` вызывал `NotifyAttackedBy(_creature)` **на себе** (атакующем), а не на **цели**. В результате `LastAttacker` записывался в атакующего, а не в жертву.

**Решение:**
_currentAttackTarget.Combat.NotifyAttackedBy(_creature);

#### 25.20 Этап 6: CreatureAudio ✅

**Файл:** `Assets/Scripts/Creatures/Components/CreatureAudio.cs`.

**Содержит:**
- `Initialize(CreatureData)` — кэш клипов из `Data`.
- `SoundOnFootstep/SoundOnAttack/SoundOnTakeDamage/SoundOnDeath(AnimationEvent)` — Animation Events.
- `PlayDeathSound()` — для `Creature.Die`.
- `PlaySound(AudioClip, float)` — воспроизведение 3D-звука с учётом `AudioManager.masterVolume`.

**Ссылки:** `_creature`, `AudioManager`.

#### 25.21 `Creature` — изменения (Этап 6)

- `_audio` (`CreatureAudio`) — ссылка.
- `Audio => _audio` — геттер.
- В `Awake`: `_audio = GetComponent<CreatureAudio>()`, `_audio.Initialize(_data)`.
- Удалены поля `*AudioClip`, `*AudioVolume`.
- Удалены методы `SoundOn*`, `PlaySound`.
- В `Die`: `PlaySound(DeathAudioClip, ...)` → `_audio.PlayDeathSound()`.

#### 25.22 Этап 7: CreatureWildAI ✅

**Файл:** `Assets/Scripts/Creatures/Components/CreatureWildAI.cs`.

**Содержит:**
- `Tick()` - Вызывается из Creature.Update, когда существо дикое.
- `SetState()`.
- `HandleWandering()`.
- `StartWandering()`.

**Ссылки:** `_creature`.

#### 25.23 `Creature` — изменения (Этап 7)

- `_сreatureWildAI` (`CreatureWildAI`) — ссылка.
- `Audio => _wildAI` — геттер.
- В `Awake`: `_wildAI = GetComponent<CreatureWildAI>()`.
- Удалены CreatureState, currentState, SetState, HandleWandering, StartWandering, OnDestinationReached.
- В `Update`: _wildAI.Tick(); вместо дикого AI блока.
- Раскомментировано _creature.WildAI.SetState(...) в ResetAttackState.

#### 25.24 Этап 8: CreatureTimedAI ✅

**Файл:** `Assets/Scripts/Creatures/Components/CreatureTimedAI.cs`.

**Содержит:**
- `Tick()` — вызывается из `Creature.Update`, когда существо приручено.
- `HandleTamedState()` — распределение по `tamedBehavior`.
- `FollowOwner`, `WanderTamed`, `StartTamedWandering`, `IdleTamed`, `ResetWanderState`.
- `HandleFlee`, `HandleNeutral`, `HandleAttackOwnerTarget`.
- Свой `LateUpdate` (сброс `_isWandering`).
- Поля: `_isWandering`, `_nextWanderTime`.

**Ссылки:** `_creature`, `CreatureCombat`, `CreatureData`.

#### 25.25 `Creature` — изменения (Этап 8)

- `_timedAI` (`CreatureTimedAI`) — ссылка.
- `TimedAI => _timedAI` — геттер.
- В `Awake`: `_timedAI = GetComponent<CreatureTimedAI>()`.
- В `Update`: `_timedAI.Tick()` вместо `HandleTamedState()`.
- Удалены: `HandleTamedState`, `FollowOwner`, `GetFollowStopDistance`, `WanderTamed`,
  `StartTamedWandering`, `IdleTamed`, `ResetWanderState`, `HandleFlee`, `HandleNeutral`,
  `HandleAttackOwnerTarget`, `FOLLOW_DIST_*`, `FOLLOW_CHASE_EXTRA`.
- `CreatureCommands.SetTamedWandering` → `_creature.TimedAI.ResetWanderState()`.


### 26. Система оседлания (Creature Riding)

Реализована полноценная механика оседлания прирученных существ (ARK-style).

**Новые компоненты:**
- `CreatureRiding` (`Assets/Scripts/Creatures/Components/CreatureRiding.cs`) — на существе:
  - `IsRidden`, `Mount(PlayerController)`, `Dismount()`, `ForceDismount()`.
  - `TickRidden(...)` — прямое управление скоростью и поворотом.
  - Отключает `Agent.updateRotation` во время езды (чтобы `A/D/S` не крутили существо).
- `RidingController` (`Assets/Scripts/Player/RidingController.cs`) — на игроке:
  - Переприцеп игрока к `riderAttachPoint` (с компенсацией scale).
  - Принудительное спешивание при выходе (`OnApplicationQuit/Pause/Focus/Destroy`).

**Правки в других системах:**
- `CreatureData`: поля `canBeRidden`, `rideSpeed`, `rideSprintSpeed`, `turnSpeed`.
- `Creature`: блокировка AI при `IsRidden`, геттеры `RideSpeed`, `TurnSpeed`, вызов `TickRidden`.
- `PlayerController`: `IsMounted`, `SetMounted(bool)` (отключает `CharacterController` и анимацию), `RidingController`.
- `PlayerInteraction`: универсальное `E` (спешивание/отпускание трупа без цели), скрытие UI-подсказок в этих состояниях.
- `CreatureKnockout`, `CreatureDeath`, `PlayerSurvivalSystem`: принудительное спешивание при нокауте/смерти.
- Аниматор: параметры `IsRiding`, `Mounting`, `Dismounting`.

**Известные особенности:**
- `E` работает без наведения на цель (по флагу `IsMounted` или `_currentlyDraggingCorpse`).
- Селфи-камера не вращает существо (`look = Vector2.zero` в `TickRidden`).



### 27. Удаление BaseLivingEntity (Финальный рефакторинг)

Класс `BaseLivingEntity` полностью удалён. Архитектура существ переведена на чистый компонентный подход.

**Что изменилось:**
- **`Creature`** — единственный класс данных существа. Все статы (`health`, `torpor`, `food`, `tamed`, `knockedOut` и т.д.) и логика (`IsAlive`, `Heal`, `SetDead`, `TamedFromLoad`) перенесены сюда из `BaseLivingEntity`.
- **`CreatureInteractable`** — новый компонент. Забрал на себя интерфейсы `IInteractable`, `IInventorySource`, `IImpactSoundProvider`. Теперь открытие инвентаря, проверка коллайдеров и звуки ударов живут здесь.
- **`Creature`** больше не наследуется от абстрактного класса, а просто от `MonoBehaviour`.
- Все пустые `virtual` и `override` методы-заглушки удалены. Вызовы методов (`AddTorpor`, `KnockOut`, `SetTamedFollowing` и т.д.) теперь идут напрямую к компонентам (`CreatureTaming`, `CreatureKnockout`, `CreatureCommands`).

**Обновления зависимостей:**
- `PlayerInteraction` ищет `CreatureInteractable` вместо `BaseLivingEntity`.
- `CorpseManager` и `TamingManager` работают напрямую с `Creature`.
- `RadialMenu` получает данные через `CreatureInteractable`.

**Известные особенности:**
- Инвентарь существ теперь хранится в `CreatureInteractable` (`GetInventory()`, `SetInventory()`).
- Урон и звуки ударов обрабатываются в `CreatureInteractable` (на нем висит `IImpactSoundProvider`).

- **E в инвентаре не трогает мир.** `PlayerInteraction.HandleInteractStarted/Ended`
  в начале проверяют `PanelsUIController.IsInventoryOpened()` и выходят.
  Иначе отпускание E в инвентаре при существе рядом вызывало оседлание.






  
## TODO

### Ближайшее (Следующий шаг)

- [ ] **`SettingsController`** — единая система настроек (MainMenu + Pause). Префаб UI + скрипт, хранение в JSON. Сюда же переедет `splitStackKey` из удалённого `InventoryConfig`.
- [ ] **Фикс багов** — список собирается перед началом работы.
**Отложено:**
- «Кто ударил игрока» (для `AttackOwnerTarget`).
- Рефакторинг `Creature.cs`.
- Пищевая цепочка (дикие бьют диких).

### Сделано (архив)
- [x] **AI прирученных существ** — Follow/Stay/Idle/Attack команды через `RadialMenu`.
- [x] **Радиальное меню для существ** — команды вместо «ТЕЛО».
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
│   ├── 📦GameAssets (Script)                            ← при Awake видит Instance != null → удаляет себя
│   ├── LoadingScreenManager (Script)                  ← удаляется вместе с GameObject
│   ├── AudioManager (Script)                          ← удаляется
│   ├── LanguageManager (Script)                       ← удаляется
│   └── LoadingScreenCanvas (GameObject, child)        ← удаляется
│   # Если игра стартует с GameWorld напрямую — этот [GameAssets] становится primary (DontDestroyOnLoad),
│   # и удаляется только [GameAssets] из MainMenu (если он там есть при повторной загрузке).
│
├── 🏁WorldManagers (GameObject)                   ← сценовый, умирает со сценой
│   └── WorldBootstrap (Script)
│         ├── _chunkSize: 32
│         ├── _loadRadiusInChunks: 2
│         ├── _saveInterval: 5
│         └── (в Start → корутина: ждёт GameAssets, создаёт CorpseManager/LootBagManager/TamingManager/WorldManager)
│
│   ── компоненты, добавляемые на 🏁WorldManagers через AddComponent: ──
│       ├── CorpseManager
│       ├── LootBagManager
│       ├── TamingManager
│       └── WorldManager
│
├── 🧠GameManagers (GameObject)                    ← сценовый
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
├── 🎒InventoryManager (GameObject)                ← сценовый
│   ├── InventoryManager (Script)
│   └── ChestUIManager (Script)
│
├── 📟PanelsController (GameObject)                ← сценовый
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
├── 📽️Cameras (GameObject)
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
├── 🖼️UICanvases (GameObject)
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