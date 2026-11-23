### Иерархия сцены: MainMenu
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
├── MainMenuManager
│      ✓ MainMenuManager
│      ✓ SettingsPanelController
├── MainMenuCanvas
│   │  ✓ Canvas
│   │  ✓ CanvasScaler
│   │  ✓ GraphicRaycaster
│   │  ✓ CanvasRenderer
│   └── FullOverlay
│       │  ✓ CanvasRenderer
│       │  ✓ Image
│       ├── MainMenuPanel
│       │      ✓ CanvasRenderer
│       │      ✓ ContentSizeFitter
│       │      ✓ LayoutElement
│       │      ✓ VerticalLayoutGroup
│       └── SettingsPanel [выкл]
│              ✓ CanvasRenderer
│              ✓ VerticalLayoutGroup
├── Main Camera
│      ✓ Camera
│      ✓ AudioListener
│      ✓ UniversalAdditionalCameraData
├── Directional Light
│      ✓ Light
│      ✓ UniversalAdditionalLightData
└── EventSystem
       ✓ EventSystem
       ✓ InputSystemUIInputModule
```
✓ — активные компоненты, ✗ — неактивные, [выкл] — неактивный объект  
*На схеме отображена ограниченная вложенность до 2 уровня.*
