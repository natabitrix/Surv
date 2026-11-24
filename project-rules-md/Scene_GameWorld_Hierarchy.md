### Иерархия сцены: GameWorld
```text
└── 🎒InventoryCanvas
    │  ✓ Canvas
    │  ✓ CanvasScaler
    │  ✓ GraphicRaycaster
    ├── TopButtons [выкл]
    │   │  ✓ CanvasRenderer
    │   │  ✗ HorizontalLayoutGroup
    │   ├── InventoryButton [выкл]
    │   │   └── InventoryButtonButton [выкл]
    │   │       │  ✓ CanvasRenderer
    │   │       │  ✓ Button
    │   │       ├── ButtonBg2 [выкл]
    │   │       │      ✓ CanvasRenderer
    │   │       │      ✓ Image
    │   │       │      ✓ Shadow
    │   │       └── Icon [выкл]
    │   │              ✓ CanvasRenderer
    │   │              ✓ Image
    │   │              ✓ Shadow
    │   ├── EngramsButton [выкл]
    │   │   └── EngramsButtonButton [выкл]
    │   │       │  ✓ CanvasRenderer
    │   │       │  ✓ Button
    │   │       ├── ButtonBg2 [выкл]
    │   │       │      ✓ CanvasRenderer
    │   │       │      ✓ Image
    │   │       │      ✓ Shadow
    │   │       ├── Icon [выкл]
    │   │       │      ✓ CanvasRenderer
    │   │       │      ✓ Image
    │   │       │      ✓ Shadow
    │   │       └── Points Count Text [выкл]
    │   │              ✓ CanvasRenderer
    │   │              ✓ TextMeshProUGUI
    │   └── CloseAllButton [выкл]
    │       │  ✓ LayoutElement
    │       └── CloseAllButtonButton [выкл]
    │           │  ✓ CanvasRenderer
    │           │  ✓ Button
    │           ├── ButtonBg2 [выкл]
    │           │      ✓ CanvasRenderer
    │           │      ✓ Image
    │           │      ✓ Shadow
    │           └── Icon [выкл]
    │                  ✓ CanvasRenderer
    │                  ✓ Image
    │                  ✓ Shadow
    ├── EngramsPanel [выкл]
    │   │  ✓ CanvasRenderer
    │   ├── PanelBg [выкл]
    │   │   │  ✓ CanvasRenderer
    │   │   │  ✓ Image
    │   │   │  ✓ Shadow
    │   │   │  ✓ LayoutElement
    │   │   └── PanelBgMask [выкл]
    │   │       │  ✓ CanvasRenderer
    │   │       │  ✓ Image
    │   │       │  ✓ RectMask2D
    │   │       └── PanelBg2 [выкл]
    │   │              ✓ CanvasRenderer
    │   │              ✓ Image
    │   ├── Header [выкл]
    │   │   └── Title [выкл]
    │   │          ✓ CanvasRenderer
    │   │          ✓ TextMeshProUGUI
    │   └── EngramUI--Engram Slots Scroll View [выкл]
    │       │  ✓ CanvasRenderer
    │       │  ✓ Image
    │       │  ✓ ScrollRect
    │       │  ✓ EngramUI
    │       ├── Viewport [выкл]
    │       │   │  ✓ CanvasRenderer
    │       │   │  ✓ Image
    │       │   │  ✓ Mask
    │       │   └── Content [выкл]
    │       │       └── Engram Slots [выкл]
    │       │              ✓ CanvasRenderer
    │       │              ✓ GridLayoutGroup
    │       └── Scrollbar Vertical [выкл]
    │           │  ✓ CanvasRenderer
    │           │  ✓ Image
    │           │  ✓ Scrollbar
    │           └── Sliding Area [выкл]
    │               └── Handle [выкл]
    │                      ✓ CanvasRenderer
    │                      ✓ Image
    ├── InventoryPanel [выкл]
    │   │  ✓ HorizontalLayoutGroup
    │   │  ✓ CanvasRenderer
    │   ├── Left [выкл]
    │   │   ├── PanelBg [выкл]
    │   │   │   │  ✓ CanvasRenderer
    │   │   │   │  ✓ Image
    │   │   │   │  ✓ Shadow
    │   │   │   │  ✓ LayoutElement
    │   │   │   └── PanelBgMask [выкл]
    │   │   │       │  ✓ CanvasRenderer
    │   │   │       │  ✓ Image
    │   │   │       │  ✓ RectMask2D
    │   │   │       └── PanelBg2 [выкл]
    │   │   │              ✓ CanvasRenderer
    │   │   │              ✓ Image
    │   │   └── PlayerLeftPanel [выкл]
    │   │       │  ✓ VerticalLayoutGroup
    │   │       ├── Header [выкл]
    │   │       │   │  ✓ HorizontalLayoutGroup
    │   │       │   ├── InventorySlotsButton [выкл]
    │   │       │   │   └── InventoryButtonButton [выкл]
    │   │       │   │          ✓ CanvasRenderer
    │   │       │   │          ✓ Button
    │   │       │   └── CraftingSlotsButton [выкл]
    │   │       │       └── CraftingButtonButton [выкл]
    │   │       │              ✓ CanvasRenderer
    │   │       │              ✓ Button
    │   │       ├── Buttons [выкл]
    │   │       │   │  ✓ CanvasRenderer
    │   │       │   │  ✓ HorizontalLayoutGroup
    │   │       │   ├── DropButton [выкл]
    │   │       │   │   └── DropButtonButton [выкл]
    │   │       │   │          ✓ CanvasRenderer
    │   │       │   │          ✓ Button
    │   │       │   └── MoveButton [выкл]
    │   │       │       └── MoveButtonButton [выкл]
    │   │       │              ✓ CanvasRenderer
    │   │       │              ✓ Button
    │   │       ├── InventoryUI--Inventory Slots Scroll View [выкл]
    │   │       │   │  ✓ CanvasRenderer
    │   │       │   │  ✓ Image
    │   │       │   │  ✓ ScrollRect
    │   │       │   │  ✓ InventoryUI
    │   │       │   ├── Viewport [выкл]
    │   │       │   │   │  ✓ CanvasRenderer
    │   │       │   │   │  ✓ Image
    │   │       │   │   │  ✓ Mask
    │   │       │   │   └── Content [выкл]
    │   │       │   └── Scrollbar Vertical [выкл]
    │   │       │       │  ✓ CanvasRenderer
    │   │       │       │  ✓ Image
    │   │       │       │  ✓ Scrollbar
    │   │       │       └── Sliding Area [выкл]
    │   │       └── CraftingUI--Crafting Slots Scroll View [выкл]
    │   │           │  ✓ CanvasRenderer
    │   │           │  ✓ Image
    │   │           │  ✓ ScrollRect
    │   │           │  ✓ CraftingUI
    │   │           ├── Viewport [выкл]
    │   │           │   │  ✓ CanvasRenderer
    │   │           │   │  ✓ Image
    │   │           │   │  ✓ Mask
    │   │           │   └── Content [выкл]
    │   │           └── Scrollbar Vertical [выкл]
    │   │               │  ✓ CanvasRenderer
    │   │               │  ✓ Image
    │   │               │  ✓ Scrollbar
    │   │               └── Sliding Area [выкл]
    │   ├── Center [выкл]
    │   │   ├── PanelBg [выкл]
    │   │   │   │  ✓ CanvasRenderer
    │   │   │   │  ✓ Image
    │   │   │   │  ✓ Shadow
    │   │   │   │  ✓ LayoutElement
    │   │   │   └── PanelBgMask [выкл]
    │   │   │       │  ✓ CanvasRenderer
    │   │   │       │  ✓ Image
    │   │   │       │  ✓ RectMask2D
    │   │   │       └── PanelBg2 [выкл]
    │   │   │              ✓ CanvasRenderer
    │   │   │              ✓ Image
    │   │   ├── PlayerCenterPanel [выкл]
    │   │   │   │  ✓ VerticalLayoutGroup
    │   │   │   ├── Header [выкл]
    │   │   │   │   │  ✗ HorizontalLayoutGroup
    │   │   │   │   ├── MeButton [выкл]
    │   │   │   │   │   └── MeButtonButton [выкл]
    │   │   │   │   │          ✓ CanvasRenderer
    │   │   │   │   │          ✓ Button
    │   │   │   │   └── OtherButton [выкл]
    │   │   │   │       └── OtherButtonButton [выкл]
    │   │   │   │              ✓ CanvasRenderer
    │   │   │   │              ✓ Button
    │   │   │   ├── InfoPanel [выкл]
    │   │   │   │   │  ✓ CanvasRenderer
    │   │   │   │   │  ✗ Image
    │   │   │   │   │  ✓ HorizontalLayoutGroup
    │   │   │   │   ├── PlayerArmorLeft [выкл]
    │   │   │   │   │   │  ✓ VerticalLayoutGroup
    │   │   │   │   │   ├── ArmorHead [выкл]
    │   │   │   │   │   │      ✓ CanvasRenderer
    │   │   │   │   │   │      ✓ Image
    │   │   │   │   │   ├── ArmorShit [выкл]
    │   │   │   │   │   │      ✓ CanvasRenderer
    │   │   │   │   │   │      ✓ Image
    │   │   │   │   │   └── ArmorLegs [выкл]
    │   │   │   │   │          ✓ CanvasRenderer
    │   │   │   │   │          ✓ Image
    │   │   │   │   ├── InfoCenter [выкл]
    │   │   │   │   │   │  ✓ VerticalLayoutGroup
    │   │   │   │   │   ├── PlayerName [выкл]
    │   │   │   │   │   ├── PlayerLevel [выкл]
    │   │   │   │   │   └── PlayerInfo [выкл]
    │   │   │   │   └── PlayerArmorRight [выкл]
    │   │   │   │       │  ✓ VerticalLayoutGroup
    │   │   │   │       ├── ArmorHead [выкл]
    │   │   │   │       │      ✓ CanvasRenderer
    │   │   │   │       │      ✓ Image
    │   │   │   │       ├── ArmorShit [выкл]
    │   │   │   │       │      ✓ CanvasRenderer
    │   │   │   │       │      ✓ Image
    │   │   │   │       └── ArmorLegs [выкл]
    │   │   │   │              ✓ CanvasRenderer
    │   │   │   │              ✓ Image
    │   │   │   ├── XP Bar [выкл]
    │   │   │   │   │  ✓ CanvasRenderer
    │   │   │   │   │  ✓ Image
    │   │   │   │   ├── ValueBarFill [выкл]
    │   │   │   │   │      ✓ CanvasRenderer
    │   │   │   │   │      ✓ Image
    │   │   │   │   │      ✓ LayoutElement
    │   │   │   │   ├── Player XP Text [выкл]
    │   │   │   │   │      ✓ CanvasRenderer
    │   │   │   │   │      ✓ TextMeshProUGUI
    │   │   │   │   └── Player Stat Points Text [выкл]
    │   │   │   │          ✓ CanvasRenderer
    │   │   │   │          ✓ TextMeshProUGUI
    │   │   │   └── StatAllocationPanel [выкл]
    │   │   │       │  ✓ VerticalLayoutGroup
    │   │   │       ├── StatRow [выкл]
    │   │   │       │   │  ✓ CanvasRenderer
    │   │   │       │   │  ✓ StatUI
    │   │   │       │   ├── LabelStats [выкл]
    │   │   │       │   │      ✓ CanvasRenderer
    │   │   │       │   │      ✓ Image
    │   │   │       │   └── Button [выкл]
    │   │   │       │          ✓ CanvasRenderer
    │   │   │       │          ✓ Button
    │   │   │       │          ✓ Shadow
    │   │   │       │          ✓ ButtonEffects
    │   │   │       ├── StatRow (1) [выкл]
    │   │   │       │   │  ✓ CanvasRenderer
    │   │   │       │   │  ✓ StatUI
    │   │   │       │   ├── LabelStats [выкл]
    │   │   │       │   │      ✓ CanvasRenderer
    │   │   │       │   │      ✓ Image
    │   │   │       │   └── Button [выкл]
    │   │   │       │          ✓ CanvasRenderer
    │   │   │       │          ✓ Button
    │   │   │       │          ✓ Shadow
    │   │   │       │          ✓ ButtonEffects
    │   │   │       ├── StatRow (2) [выкл]
    │   │   │       │   │  ✓ CanvasRenderer
    │   │   │       │   │  ✓ StatUI
    │   │   │       │   ├── LabelStats [выкл]
    │   │   │       │   │      ✓ CanvasRenderer
    │   │   │       │   │      ✓ Image
    │   │   │       │   └── Button [выкл]
    │   │   │       │          ✓ CanvasRenderer
    │   │   │       │          ✓ Button
    │   │   │       │          ✓ Shadow
    │   │   │       │          ✓ ButtonEffects
    │   │   │       ├── StatRow (3) [выкл]
    │   │   │       │   │  ✓ CanvasRenderer
    │   │   │       │   │  ✓ StatUI
    │   │   │       │   ├── LabelStats [выкл]
    │   │   │       │   │      ✓ CanvasRenderer
    │   │   │       │   │      ✓ Image
    │   │   │       │   └── Button [выкл]
    │   │   │       │          ✓ CanvasRenderer
    │   │   │       │          ✓ Button
    │   │   │       │          ✓ Shadow
    │   │   │       │          ✓ ButtonEffects
    │   │   │       ├── StatRow (4) [выкл]
    │   │   │       │   │  ✓ CanvasRenderer
    │   │   │       │   │  ✓ StatUI
    │   │   │       │   ├── LabelStats [выкл]
    │   │   │       │   │      ✓ CanvasRenderer
    │   │   │       │   │      ✓ Image
    │   │   │       │   └── Button [выкл]
    │   │   │       │          ✓ CanvasRenderer
    │   │   │       │          ✓ Button
    │   │   │       │          ✓ Shadow
    │   │   │       │          ✓ ButtonEffects
    │   │   │       ├── StatRow (5) [выкл]
    │   │   │       │   │  ✓ CanvasRenderer
    │   │   │       │   │  ✓ StatUI
    │   │   │       │   ├── LabelStats [выкл]
    │   │   │       │   │      ✓ CanvasRenderer
    │   │   │       │   │      ✓ Image
    │   │   │       │   └── Button [выкл]
    │   │   │       │          ✓ CanvasRenderer
    │   │   │       │          ✓ Button
    │   │   │       │          ✓ Shadow
    │   │   │       │          ✓ ButtonEffects
    │   │   │       ├── StatRow (6) [выкл]
    │   │   │       │   │  ✓ CanvasRenderer
    │   │   │       │   │  ✓ StatUI
    │   │   │       │   ├── LabelStats [выкл]
    │   │   │       │   │      ✓ CanvasRenderer
    │   │   │       │   │      ✓ Image
    │   │   │       │   └── Button [выкл]
    │   │   │       │          ✓ CanvasRenderer
    │   │   │       │          ✓ Button
    │   │   │       │          ✓ Shadow
    │   │   │       │          ✓ ButtonEffects
    │   │   │       ├── StatRow (7) [выкл]
    │   │   │       │   │  ✓ CanvasRenderer
    │   │   │       │   │  ✓ StatUI
    │   │   │       │   ├── LabelStats [выкл]
    │   │   │       │   │      ✓ CanvasRenderer
    │   │   │       │   │      ✓ Image
    │   │   │       │   └── Button [выкл]
    │   │   │       │          ✓ CanvasRenderer
    │   │   │       │          ✓ Button
    │   │   │       │          ✓ Shadow
    │   │   │       │          ✓ ButtonEffects
    │   │   │       ├── StatRow (8) [выкл]
    │   │   │       │   │  ✓ CanvasRenderer
    │   │   │       │   │  ✓ StatUI
    │   │   │       │   ├── LabelStats [выкл]
    │   │   │       │   │      ✓ CanvasRenderer
    │   │   │       │   │      ✓ Image
    │   │   │       │   └── Button [выкл]
    │   │   │       │          ✓ CanvasRenderer
    │   │   │       │          ✓ Button
    │   │   │       │          ✓ Shadow
    │   │   │       │          ✓ ButtonEffects
    │   │   │       ├── StatRow (9) [выкл]
    │   │   │       │   │  ✓ CanvasRenderer
    │   │   │       │   │  ✓ StatUI
    │   │   │       │   ├── LabelStats [выкл]
    │   │   │       │   │      ✓ CanvasRenderer
    │   │   │       │   │      ✓ Image
    │   │   │       │   └── Button [выкл]
    │   │   │       │          ✓ CanvasRenderer
    │   │   │       │          ✓ Button
    │   │   │       │          ✓ Shadow
    │   │   │       │          ✓ ButtonEffects
    │   │   │       └── StatRow (10) [выкл]
    │   │   │           │  ✓ CanvasRenderer
    │   │   │           │  ✓ StatUI
    │   │   │           ├── LabelStats [выкл]
    │   │   │           │      ✓ CanvasRenderer
    │   │   │           │      ✓ Image
    │   │   │           └── Button [выкл]
    │   │   │                  ✓ CanvasRenderer
    │   │   │                  ✓ Button
    │   │   │                  ✓ Shadow
    │   │   │                  ✓ ButtonEffects
    │   │   └── OtherCenterPanel [выкл]
    │   │       │  ✓ CanvasRenderer
    │   │       │  ✓ VerticalLayoutGroup
    │   │       └── Header [выкл]
    │   │           │  ✓ HorizontalLayoutGroup
    │   │           ├── MeButton [выкл]
    │   │           │   └── MeButtonButton [выкл]
    │   │           │          ✓ CanvasRenderer
    │   │           │          ✓ Button
    │   │           └── OtherButton [выкл]
    │   │               └── OtherButtonButton [выкл]
    │   │                      ✓ CanvasRenderer
    │   │                      ✓ Button
    │   └── Right [выкл]
    │       ├── PanelBg [выкл]
    │       │   │  ✓ CanvasRenderer
    │       │   │  ✓ Image
    │       │   │  ✓ Shadow
    │       │   │  ✓ LayoutElement
    │       │   └── PanelBgMask [выкл]
    │       │       │  ✓ CanvasRenderer
    │       │       │  ✓ Image
    │       │       │  ✓ RectMask2D
    │       │       └── PanelBg2 [выкл]
    │       │              ✓ CanvasRenderer
    │       │              ✓ Image
    │       ├── PlayerRightPanel [выкл]
    │       │   │  ✓ CanvasRenderer
    │       │   ├── CharacterPreviewImageBorder [выкл]
    │       │   │      ✓ CanvasRenderer
    │       │   │      ✓ Shadow
    │       │   │      ✓ Image
    │       │   └── CharacterPreviewImage [выкл]
    │       │          ✓ CanvasRenderer
    │       │          ✓ RawImage
    │       │          ✗ Shadow
    │       └── OtherRightPanel [выкл]
    │           │  ✓ VerticalLayoutGroup
    │           ├── Header [выкл]
    │           │   │  ✓ HorizontalLayoutGroup
    │           │   ├── InventoryButton [выкл]
    │           │   │   └── InventoryButtonButton [выкл]
    │           │   │          ✓ CanvasRenderer
    │           │   │          ✓ Button
    │           │   └── CraftingButton [выкл]
    │           │       └── CraftingButtonButton [выкл]
    │           │              ✓ CanvasRenderer
    │           │              ✓ Button
    │           ├── Buttons [выкл]
    │           │   │  ✓ CanvasRenderer
    │           │   │  ✓ HorizontalLayoutGroup
    │           │   ├── DropButton [выкл]
    │           │   │   └── DropButtonButton [выкл]
    │           │   │          ✓ CanvasRenderer
    │           │   │          ✓ Button
    │           │   └── MoveButton [выкл]
    │           │       └── MoveButtonButton [выкл]
    │           │              ✓ CanvasRenderer
    │           │              ✓ Button
    │           └── ChestUI--Inventory Slots Scroll View [выкл]
    │               │  ✓ CanvasRenderer
    │               │  ✓ Image
    │               │  ✓ ScrollRect
    │               ├── Viewport [выкл]
    │               │   │  ✓ CanvasRenderer
    │               │   │  ✓ Image
    │               │   │  ✓ Mask
    │               │   └── Content [выкл]
    │               └── Scrollbar Vertical [выкл]
    │                   │  ✓ CanvasRenderer
    │                   │  ✓ Image
    │                   │  ✓ Scrollbar
    │                   └── Sliding Area [выкл]
    ├── HotbarUI--Hotbar
    │      ✓ CanvasRenderer
    │      ✗ Image
    │      ✓ GridLayoutGroup
    │      ✓ HotbarUI
    ├── DragLayer
    │      ✓ CanvasRenderer
    └── ChestUI
           ✓ ChestUI
```
✓ — активные компоненты, ✗ — неактивные, [выкл] — неактивный объект  
*На схеме отображена ограниченная вложенность до 6 уровня.*
