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