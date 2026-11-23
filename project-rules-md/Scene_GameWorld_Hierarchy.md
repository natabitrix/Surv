### Иерархия сцены: GameWorld
```text
├── 📦GameAssets
│   │  ✓ GameAssets
│   │  ✓ LoadingScreenManager
│   │  ✓ AudioManager
│   │  ✓ LanguageManager
│   └── LoadingScreenCanvas [выкл]
│       │  ✓ Canvas
│       │  ✓ CanvasScaler
│       │  ✓ GraphicRaycaster
│       │  ✓ CanvasRenderer
│       ├── LoadingScreenBackground [выкл]
│       │      ✓ CanvasRenderer
│       │      ✓ Image
│       └── LoadingScreenProggressBar [выкл]
│              ✓ CanvasRenderer
│              ✓ Image
│              ✓ LayoutElement
├── 🏁WorldBootstrap (WorldManagers)
│      ✓ WorldBootstrap
├── 🕹️GameManagers
│      ✓ HUDManager
│      ✓ UIManager
│      ✓ PauseManager
│      ✓ DeathScreenManager
│      ✓ NotificationManager
│      ✓ TooltipManager
│      ✓ ContextMenuManager
│      ✓ CharacterPreviewManager
│      ✓ CombatAudioManager
│      ✓ ArrowPool
│      ✓ AimUI
│      ✓ EntityInfoPanelManager
│      ✓ DamageNumberPool
│      ✓ ChestUIManager
├── 📟PanelsController
│      ✓ PanelsUIController
│      ✓ RadialMenuController
├── 📽️Cameras
│   ├── Directional Light
│   │      ✓ Light
│   │      ✓ UniversalAdditionalLightData
│   ├── MainCamera
│   │      ✓ Camera
│   │      ✓ AudioListener
│   │      ✓ CinemachineBrain
│   │      ✓ UniversalAdditionalCameraData
│   │      ✓ UnderwaterEffects
│   ├── ThirdPersonPlayerFollowCamera
│   │      ✓ CinemachineCamera
│   │      ✓ CinemachineFollow
│   │      ✓ CinemachineRotateWithFollowTarget
│   │      ✓ CinemachineDeoccluder
│   │      ✓ CinemachineImpulseListener
│   ├── FirstPersonCamera
│   │      ✓ CinemachineCamera
│   │      ✓ CinemachineFollow
│   │      ✓ CinemachineRotateWithFollowTarget
│   │      ✓ CinemachineDeoccluder
│   │      ✓ CinemachineImpulseListener
│   ├── SelfieCamera
│   │      ✓ CinemachineCamera
│   │      ✓ CinemachineImpulseListener
│   │      ✓ SelfieCameraOrbit
│   ├── PostProcessingDefaultVolume
│   │      ✓ Volume
│   ├── UnderWaterVolume
│   │      ✓ Volume
│   ├── PlayerHurtVolume
│   │      ✓ Volume
│   └── CharacterPreviewCamera [выкл]
│          ✓ Camera
│          ✓ UniversalAdditionalCameraData
├── 🖼️UICanvases
│   ├── HUDCanvas [выкл]
│   │   │  ✓ Canvas
│   │   │  ✓ CanvasScaler
│   │   │  ✓ GraphicRaycaster
│   │   └── Container [выкл]
│   │          ✓ VerticalLayoutGroup
│   │          ✓ CanvasRenderer
│   ├── InteractionCanvas
│   │   │  ✓ Canvas
│   │   │  ✓ CanvasScaler
│   │   │  ✓ GraphicRaycaster
│   │   └── InteractionUI [выкл]
│   ├── ➕AimCanvas
│   │   │  ✓ Canvas
│   │   │  ✓ CanvasScaler
│   │   │  ✓ GraphicRaycaster
│   │   └── AimCrosshair [выкл]
│   │          ✓ CanvasRenderer
│   │          ✓ Image
│   ├── 🦖EntityInfoCanvas
│   │   │  ✓ Canvas
│   │   │  ✓ CanvasScaler
│   │   │  ✓ GraphicRaycaster
│   │   └── EntityInfoPanel [выкл]
│   │          ✓ VerticalLayoutGroup
│   │          ✓ EntityInfoPanel
│   │          ✓ ContentSizeFitter
│   ├── DamageNumberCanvas
│   │      ✓ Canvas
│   │      ✓ CanvasScaler
│   │      ✓ GraphicRaycaster
│   ├── DamageVignetteCanvas
│   │   │  ✓ Canvas
│   │   │  ✓ CanvasScaler
│   │   │  ✓ GraphicRaycaster
│   │   └── DamageVignette
│   │          ✓ CanvasRenderer
│   │          ✓ Image
│   ├── 🎒InventoryCanvas
│   │   │  ✓ Canvas
│   │   │  ✓ CanvasScaler
│   │   │  ✓ GraphicRaycaster
│   │   ├── TopButtons [выкл]
│   │   │      ✓ CanvasRenderer
│   │   │      ✗ HorizontalLayoutGroup
│   │   ├── EngramsPanel [выкл]
│   │   │      ✓ CanvasRenderer
│   │   ├── InventoryPanel [выкл]
│   │   │      ✓ HorizontalLayoutGroup
│   │   │      ✓ CanvasRenderer
│   │   ├── HotbarUI--Hotbar
│   │   │      ✓ CanvasRenderer
│   │   │      ✗ Image
│   │   │      ✓ GridLayoutGroup
│   │   │      ✓ HotbarUI
│   │   ├── DragLayer
│   │   │      ✓ CanvasRenderer
│   │   └── ChestUI
│   │          ✓ ChestUI
│   ├── NotificationTopCanvas
│   │   │  ✓ Canvas
│   │   │  ✓ CanvasScaler
│   │   │  ✓ GraphicRaycaster
│   │   │  ✓ CanvasRenderer
│   │   └── NoteTopUI [выкл]
│   │          ✓ CanvasRenderer
│   ├── NotificationLeftCanvas
│   │      ✓ Canvas
│   │      ✓ CanvasScaler
│   │      ✓ GraphicRaycaster
│   │      ✓ CanvasRenderer
│   │      ✓ VerticalLayoutGroup
│   ├── ContextMenuCanvas [выкл]
│   │   │  ✓ Canvas
│   │   │  ✓ CanvasScaler
│   │   │  ✓ GraphicRaycaster
│   │   │  ✓ CanvasRenderer
│   │   └── FullOverlay [выкл]
│   │          ✓ CanvasRenderer
│   │          ✓ Image
│   │          ✓ CloseOnOutsideClick
│   ├── ⚪RadialMenuCanvas [выкл]
│   │   │  ✓ Canvas
│   │   │  ✓ CanvasScaler
│   │   │  ✓ GraphicRaycaster
│   │   └── Panel [выкл]
│   │          ✓ CanvasRenderer
│   │          ✓ Image
│   ├── ⏸️PauseCanvas [выкл]
│   │   │  ✓ Canvas
│   │   │  ✓ CanvasScaler
│   │   │  ✓ GraphicRaycaster
│   │   │  ✓ CanvasRenderer
│   │   ├── FullOverlay [выкл]
│   │   │      ✓ CanvasRenderer
│   │   │      ✓ Image
│   │   ├── PausePanel [выкл]
│   │   │      ✓ CanvasRenderer
│   │   │      ✓ ContentSizeFitter
│   │   │      ✓ LayoutElement
│   │   │      ✓ VerticalLayoutGroup
│   │   └── SettingsPanel [выкл]
│   │          ✓ CanvasRenderer
│   │          ✓ VerticalLayoutGroup
│   └── 🪦DeathScreenCanvas [выкл]
│       │  ✓ Canvas
│       │  ✓ CanvasScaler
│       │  ✓ GraphicRaycaster
│       │  ✓ CanvasRenderer
│       └── FullOverlay [выкл]
│              ✓ CanvasRenderer
│              ✓ Image
├── EventSystem
│      ✓ EventSystem
│      ✓ InputSystemUIInputModule
├── NavMeshBacker
│      ✓ NavMeshSurface
├── --------------------
└── 🦸‍♀️---Player---
    │  ✓ PlayerController
    │  ✓ CharacterController
    │  ✓ CapsuleCollider
    │  ✓ BasicRigidBodyPush
    │  ✓ PlayerInput
    │  ✓ PlayerInputHandler
    │  ✓ CameraManager
    │  ✓ PlayerEquipment
    │  ✓ PlayerRangedCombat
    │  ✓ PlayerBuildMode
    │  ✓ ItemUsageSystem
    │  ✓ ItemHandler
    │  ✓ PlayerInteraction
    │  ✓ PlayerAimingSystem
    │  ✓ CinemachineImpulseSource
    │  ✓ PlayerDamageFeedback
    │  ✓ ItemUsageRouter
    │  ✓ RidingController
    │  ✓ PlayerSurvivalSystem
    │  ✓ InventoryManager
    │  ✓ PlayerProgress
    ├── PlayerCameraRoot
    ├── Character
    │   │  ✓ Animator
    │   │  ✓ AnimationEvents
    │   ├── Mesh
    │   │      ✓ SkinnedMeshRenderer
    │   └── metarig.004
    └── Directional Light
           ✓ Light
           ✓ UniversalAdditionalLightData
```
✓ — активные компоненты, ✗ — неактивные, [выкл] — неактивный объект  
*На схеме отображена ограниченная вложенность до 2 уровня.*
