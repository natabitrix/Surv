// Assets/Scripts/InventorySystem/PlayerPanelsUIController.cs
using Assets.Scripts.Core;
using Assets.Scripts.Corpses;
using Assets.Scripts.Creatures;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using Assets.Scripts.UI.RadialMenuUI;
using Assets.Scripts.UI.Tooltip;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class PanelsUIController : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler _input;
        [SerializeField] private PlayerController _playerController;
        [Header("Panels")]
        public GameObject TopButtons;
        public GameObject EngramsPanel;
        public GameObject InventoryPanel;
        public GameObject RadialMenuPanel;

        [Header("Panels Switch Buttons")]
        public Button EngramsPanelButton;
        public Button InventoryPanelButton;
        public Button CloseAllButton;
        public Button CloseRadialMenuButton;

        [Header("Slots Switch Buttons")]
        public Button InventorySlotsButton;
        public Button CraftingSlotsButton;

        [Header("Inventory Action Buttons")]
        public Button PlayerInventoryDropButton;
        public Button PlayerInventoryMoveButton;
        public Button OtherInventoryDropButton;
        public Button OtherInventoryMoveButton;

        [Header("Slots for switch")]
        public GameObject InventorySlots;
        public GameObject CraftingSlots;

        [Header("Panels for switch")]
        public GameObject PlayerCenterPanel;
        public GameObject PlayerRightPanel;
        public GameObject OtherCenterPanel;
        public GameObject OtherRightPanel;

        [Header("Radial Menu")]
        public TMP_Text RadialMenuTargetName;
        public Button RadialMenuPickupButton;
        public Button RadialMenuDestroyButton;
        public Button RadialMenuRepairButton;
        public Button RadialMenuDragCorpseButton;

        [Header("Other Managers")]
        [SerializeField] private InventoryManager _inventoryManager;
        [SerializeField] private CharacterPreviewManager _previewManager;
        [SerializeField] private TooltipManager _tooltipManager;
        [SerializeField] private ItemUsageRouter _itemUsageRouter;

        private bool _isInventoryOpened = false;
        private bool _isRadialMenuOpened = false;
        // private bool _isDeathScreenShow = false;

        public Item RadialMenuCurrentTarget = null;
        public GameObject RadialMenuCurrentTargetGO = null;

        public static PanelsUIController Instance { get; private set; }

        private void Start()
        {
            // Top Buttons
            EngramsPanelButton.onClick.AddListener(() => ShowEngramsPanel());
            TooltipTrigger.AddTooltip(EngramsPanelButton.gameObject, "Энграммы");
            EngramsPanelButton.AddComponent<ButtonScaleEffect>();

            InventoryPanelButton.onClick.AddListener(() => ShowInventoryPanel());
            TooltipTrigger.AddTooltip(InventoryPanelButton.gameObject, "Инвентарь");
            InventoryPanelButton.AddComponent<ButtonScaleEffect>();

            CloseAllButton.onClick.AddListener(() => CloseAllPanels());
            TooltipTrigger.AddTooltip(CloseAllButton.gameObject, "Закрыть");
            CloseAllButton.AddComponent<ButtonScaleEffect>();

            CloseRadialMenuButton.onClick.AddListener(() => CloseRadialMenu());
            TooltipTrigger.AddTooltip(CloseRadialMenuButton.gameObject, "Закрыть");
            CloseRadialMenuButton.AddComponent<ButtonScaleEffect>();

            // Left Header Buttons
            InventorySlotsButton.onClick.AddListener(() => ShowInventorySlots());
            TooltipTrigger.AddTooltip(InventorySlotsButton.gameObject, "Инвентарь");

            CraftingSlotsButton.onClick.AddListener(() => ShowCraftingSlots());
            TooltipTrigger.AddTooltip(CraftingSlotsButton.gameObject, "Ремесло");

            // Left Action Buttons
            PlayerInventoryDropButton.onClick.AddListener(() => _inventoryManager?.DropItemsFromInventory());
            TooltipTrigger.AddTooltip(PlayerInventoryDropButton.gameObject, "Выбросить всё");
            PlayerInventoryDropButton.AddComponent<ButtonScaleEffect>();

            PlayerInventoryMoveButton.onClick.AddListener(() => _inventoryManager?.MoveAllToChest());
            TooltipTrigger.AddTooltip(PlayerInventoryMoveButton.gameObject, "Переложить всё");
            PlayerInventoryMoveButton.AddComponent<ButtonScaleEffect>();

            // Right Action Buttons
            OtherInventoryDropButton.onClick.AddListener(() => _inventoryManager?.DropItemsFromChest());
            TooltipTrigger.AddTooltip(OtherInventoryDropButton.gameObject, "Выбросить всё");
            OtherInventoryDropButton.AddComponent<ButtonScaleEffect>();

            OtherInventoryMoveButton.onClick.AddListener(() => _inventoryManager?.MoveAllToPlayer());
            TooltipTrigger.AddTooltip(OtherInventoryMoveButton.gameObject, "Забрать всё");
            OtherInventoryMoveButton.AddComponent<ButtonScaleEffect>();

        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            ToggleInventory();
            _inventoryManager.DropItemFromSlotByDropKey();
        }

        // === TOGGLE PANELS ===
        // Управление состоянием панелей и курсора
        private void PanelMode(bool on = false)
        {
            _isInventoryOpened = on;
            _playerController.LockCameraOnEsc = on;
            _input.SetCursorVisible(on);
        }

        public bool IsInventoryOpened() => _isInventoryOpened;
        public bool IsRadialMenuOpened() => _isRadialMenuOpened;
        public bool IsPanelOpened() => _isInventoryOpened || _isRadialMenuOpened;

        // Открытие инвентаря игрока
        public void OpenPlayerInventory()
        {
            TopButtons.SetActive(true);
            InventoryPanel.SetActive(true);
            PlayerCenterPanel.SetActive(true);
            PlayerRightPanel.SetActive(true);
            OtherCenterPanel.SetActive(false);
            OtherRightPanel.SetActive(false);
            ShowInventorySlots();

            if (_previewManager != null) _previewManager.OpenPreview();
            if (_inventoryManager != null) _inventoryManager.RefreshPlayerStatsDisplay();

            if (_inventoryManager.equipment != null)
            {
                _inventoryManager.equipment.OnEquipped += RefreshPreview;
                _inventoryManager.equipment.OnUnequipped += RefreshPreview;
            }

            _input.OnInteractTriggered += _inventoryManager.UseSelectedSlot;
            _input.OnInteractStopPressed += _itemUsageRouter.OnUseItemFinished;
        }

        // Открытие чужого инвентаря (сундука)
        public void OpenOtherInventory()
        {
            TopButtons.SetActive(true);
            InventoryPanel.SetActive(true);
            OtherCenterPanel.SetActive(true);
            OtherRightPanel.SetActive(true);
            PlayerCenterPanel.SetActive(false);
            PlayerRightPanel.SetActive(false);

            PlayerInventoryMoveButton.interactable = true;

            // === Определяем тип источника: сундук vs существо/труп/сумка ===
            var source = ChestUIManager.Instance?.CurrentSource;
            bool isChest = source is ChestController;

            // Сундук — нельзя «выбросить всё» (нельзя выбросить предметы из сундука)
            // Существо / труп / сумка — можно
            OtherInventoryDropButton.interactable = !isChest;

            if (_previewManager != null) _previewManager.ClosePreview();

            PanelMode(true);

            _input.OnInteractTriggered += _inventoryManager.UseSelectedSlot;
            _input.OnInteractStopPressed += _itemUsageRouter.OnUseItemFinished;
        }

        public void OpenRadialMenu(GameObject targetGO)
        {
            if (_isRadialMenuOpened) return;

            if (targetGO == null)
            {
                Debug.LogError("[PanelsUIController] Target GameObject не найден!");
                return;
            }

            var ctx = targetGO.GetComponent<IRadialMenuContext>();
            if (ctx == null)
            {
                Debug.LogError($"[PanelsUIController] На {targetGO.name} нет IRadialMenuContext!");
                return;
            }

            var config = ctx.GetMenuConfig();
            if (config == null)
            {
                Debug.LogWarning($"[PanelsUIController] Пустой RadialMenuConfig для {ctx.GetMenuTitle()}");
                return;
            }

            RadialMenuCurrentTargetGO = targetGO;

            // Открываем через RadialMenuController
            var controller = RadialMenuController.Instance;
            if (controller == null)
            {
                Debug.LogError("[PanelsUIController] RadialMenuController.Instance == null!");
                return;
            }

            controller.Open(ctx);

            _isRadialMenuOpened = true;
            PanelMode(true);
        }

        public void CloseRadialMenu(bool setPanelMode = false)
        {
            if (!_isRadialMenuOpened) return;

            RadialMenuCurrentTarget = null;
            RadialMenuCurrentTargetGO = null;

            RadialMenuController.Instance?.Close();

            _isRadialMenuOpened = false;
            PanelMode(setPanelMode);

            if (_tooltipManager != null) _tooltipManager.HideTooltip();

            // Сбрасываем зажатие E, если меню закрылось
            _input?.ForceResetInteract();
            // Сбрасываем зажатие T (не E!)
            // _input?.ForceResetRadialMenu();
        }


        // Закрытие панелей инвентаря
        public void CloseInventoryPanel()
        {
            TopButtons.SetActive(false);
            InventoryPanel.SetActive(false);
            OtherCenterPanel.SetActive(false);
            OtherRightPanel.SetActive(false);
            PlayerCenterPanel.SetActive(false);
            PlayerRightPanel.SetActive(false);

            if (_previewManager != null) _previewManager.ClosePreview();
            if (_tooltipManager != null) _tooltipManager.HideTooltip();

            if (_inventoryManager.equipment != null)
            {
                _inventoryManager.equipment.OnEquipped -= RefreshPreview;
                _inventoryManager.equipment.OnUnequipped -= RefreshPreview;
            }

            _input.OnInteractTriggered -= _inventoryManager.UseSelectedSlot;
            _input.OnInteractStopPressed -= _itemUsageRouter.OnUseItemFinished;
        }

        // Закрытие панели энграмм
        public void CloseEngramsPanel()
        {
            EngramsPanel.SetActive(false);
        }

        // Закрытие сундука
        public void CloseChestPanel()
        {
            ChestUIManager.Instance?.Close();

            _input.OnInteractTriggered -= _inventoryManager.UseSelectedSlot;
            _input.OnInteractStopPressed -= _itemUsageRouter.OnUseItemFinished;
        }

        // Закрытие всех панелей
        public void CloseAllPanels()
        {

            ChestUIManager.Instance?.Close();

            CloseInventoryPanel();
            CloseEngramsPanel();
            CloseChestPanel();
            PanelMode(false);

        }

        // Переключение инвентаря по клавише
        public void ToggleInventory()
        {
            if (_input.openInventory)
            {
                if (!_isInventoryOpened)
                {
                    OpenPlayerInventory();
                    PanelMode(true);
                }
                else
                {
                    CloseInventoryPanel();
                    CloseEngramsPanel();
                    PanelMode(false);
                }
                _input.ResetOpenInventory();
            }
        }

        // Переключение между вкладками инвентаря и крафта
        public void ShowInventorySlots()
        {
            SetActive(InventorySlots, CraftingSlots);
            SetActiveButton(InventorySlotsButton, CraftingSlotsButton);
            PlayerInventoryDropButton.interactable = true;
            PlayerInventoryMoveButton.interactable = true;
        }

        public void ShowCraftingSlots()
        {
            SetActive(CraftingSlots, InventorySlots);
            SetActiveButton(CraftingSlotsButton, InventorySlotsButton);
            PlayerInventoryDropButton.interactable = false;
            PlayerInventoryMoveButton.interactable = false;
        }

        public void ShowEngramsPanel() => SetActive(EngramsPanel, InventoryPanel);
        public void ShowInventoryPanel() => SetActive(InventoryPanel, EngramsPanel);

        void SetActive(GameObject on, GameObject off)
        {
            on.SetActive(true);
            off.SetActive(false);
        }

        void SetActiveButton(Button on, Button off)
        {
            TextMeshProUGUI onText = on.GetComponentInChildren<TextMeshProUGUI>();
            TextMeshProUGUI offText = off.GetComponentInChildren<TextMeshProUGUI>();
            Color buttonOnTextColor = onText.color;
            Color buttonOffTextColor = buttonOnTextColor;
            buttonOnTextColor.a = 1f;
            buttonOffTextColor.a = 0.5f;
            onText.color = buttonOnTextColor;
            offText.color = buttonOffTextColor;

            on.interactable = false;
            off.interactable = true;
        }


        // === Подписки на события ===

        private void RefreshPreview()
        {
            if (_previewManager != null) _previewManager.RefreshPreview();
        }
    }
}