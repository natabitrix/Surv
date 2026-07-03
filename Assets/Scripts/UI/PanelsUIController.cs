// Assets/Scripts/InventorySystem/PlayerPanelsUIController.cs
using Assets.Scripts.Core;
using Assets.Scripts.Creatures;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
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
        // [SerializeField] private ItemDatabase _itemDatabase;


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
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private CharacterPreviewManager previewManager;
        [SerializeField] private TooltipManager tooltipManager;

        private bool _isInventoryOpened = false;
        private bool _isRadialMenuOpened = false;

        public Item RadialMenuCurrentTarget = null;
        public GameObject RadialMenuCurrentTargetGO = null;

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

            if (previewManager != null) previewManager.OpenPreview();
            if (inventoryManager != null) inventoryManager.RefreshPlayerStatsDisplay();

            if (inventoryManager.equipment != null)
            {
                inventoryManager.equipment.OnEquipped += RefreshPreview;
                inventoryManager.equipment.OnUnequipped += RefreshPreview;
            }

            _input.OnInteractTriggered += inventoryManager.UseItemFromSlot;
            _input.OnInteractStopPressed += inventoryManager.OnUseItemFinished;
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
            if (previewManager != null) previewManager.ClosePreview();
            PanelMode(true);
        }

        public void OpenRadialMenu(GameObject targetGO)
        {
            // ✅ ПРОВЕРКА: Не открывать если уже открыто
            if (_isRadialMenuOpened)
            {
                // Debug.Log("[PanelsUIController] CloseRadialMenu: меню уже закрыто, пропускаем");
                return;
            }

            if (targetGO == null)
            {
                Debug.LogError("[PanelsUIController] Target GameObject не найден!");
                return;
            }
            Item targetItem = null;
            Creature targetCreature = null;
            Corpse targetCorpse = null;
            if (targetGO.TryGetComponent(out RadialMenu menu))
            {
                if (menu.item != null) targetItem = menu.item;
                else if (menu.creature != null) targetCreature = menu.creature;
                else if (menu.corpse != null) targetCorpse = menu.corpse;
            }

            if (targetItem == null && targetCreature == null && targetCorpse == null)
            {
                Debug.LogError("[PanelsUIController] item, creature и corpse не найдены!");
                return;
            }

            RadialMenuCurrentTarget = targetItem;
            RadialMenuCurrentTargetGO = targetGO;

            if (RadialMenuTargetName != null)
            {
                if (targetItem != null)
                    RadialMenuTargetName.text = $"{targetItem.itemName}";

                if (targetCreature != null)
                    RadialMenuTargetName.text = $"СУЩЕСТВО {targetCreature.gameObject.name}";

                if (targetCorpse != null)
                    RadialMenuTargetName.text = $"ТЕЛО {targetCorpse.gameObject.name}";
            }


            RadialMenuPanel.SetActive(true);
            _isRadialMenuOpened = true;
            PanelMode(true);
        }

        public void CloseRadialMenu()
        {
            // ✅ ПРОВЕРКА: Не закрывать если уже закрыто
            if (!_isRadialMenuOpened)
            {
                return;
            }

            RadialMenuCurrentTarget = null;
            RadialMenuCurrentTargetGO = null;

            if (RadialMenuTargetName != null)
                RadialMenuTargetName.text = " ";

            RadialMenuPanel.SetActive(false);
            _isRadialMenuOpened = false;
            PanelMode(false);

            if (tooltipManager != null)
                tooltipManager.HideTooltip();


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

            if (previewManager != null) previewManager.ClosePreview();
            if (tooltipManager != null) tooltipManager.HideTooltip();

            if (inventoryManager.equipment != null)
            {
                inventoryManager.equipment.OnEquipped -= RefreshPreview;
                inventoryManager.equipment.OnUnequipped -= RefreshPreview;
            }

            _input.OnInteractTriggered -= inventoryManager.UseItemFromSlot;
            _input.OnInteractStopPressed -= inventoryManager.OnUseItemFinished;
        }

        // Закрытие панели энграмм
        public void CloseEngramsPanel()
        {
            EngramsPanel.SetActive(false);
        }

        // Закрытие сундука
        public void CloseChestPanel_()
        {

            var openChestUI = ChestUI.CurrentOpenChest;
            if (openChestUI != null && openChestUI.SourceChest != null)
            {
                Debug.Log("CloseChestPanel Close");
                openChestUI.SourceChest.Close();

            }
        }
        // Закрытие сундука
        public void CloseChestPanel()
        {
            var openChestUI = ChestUI.CurrentOpenChest;

            if (openChestUI != null)
            {
                // 1. Пробуем закрыть через универсальный интерфейс IInteractable
                if (openChestUI.SourceInteractable != null)
                {
                    // Если у интерактивного объекта есть метод Close(), вызываем его
                    // Для этого можно использовать динамический вызов или проверку типов
                    if (openChestUI.SourceInteractable is ChestController chest)
                    {
                        chest.Close();
                    }
                    else if (openChestUI.SourceInteractable is Corpse corpse)
                    {
                        corpse.CloseInventory();
                    }
                    // Можно добавить другие типы, если они поддерживают закрытие
                }

                // 2. Очищаем данные в UI
                openChestUI.Close();
            }
            else
            {
                Debug.LogWarning("CloseChestPanel: CurrentOpenChest равен null");
            }
        }

        // Закрытие всех панелей
        public void CloseAllPanels()
        {
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
            PlayerInventoryDropButton.onClick.AddListener(() => inventoryManager?.DropItemsFromInventory());
            TooltipTrigger.AddTooltip(PlayerInventoryDropButton.gameObject, "Выбросить всё");
            PlayerInventoryDropButton.AddComponent<ButtonScaleEffect>();

            PlayerInventoryMoveButton.onClick.AddListener(() => inventoryManager?.MoveAllToChest());
            TooltipTrigger.AddTooltip(PlayerInventoryMoveButton.gameObject, "Переложить всё");
            PlayerInventoryMoveButton.AddComponent<ButtonScaleEffect>();

            // Right Action Buttons
            OtherInventoryDropButton.onClick.AddListener(() => inventoryManager?.DropItemsFromChest());
            TooltipTrigger.AddTooltip(OtherInventoryDropButton.gameObject, "Выбросить всё");
            OtherInventoryDropButton.AddComponent<ButtonScaleEffect>();

            OtherInventoryMoveButton.onClick.AddListener(() => inventoryManager?.MoveAllToPlayer());
            TooltipTrigger.AddTooltip(OtherInventoryMoveButton.gameObject, "Забрать всё");
            OtherInventoryMoveButton.AddComponent<ButtonScaleEffect>();

            // Radial Menu Buttons
            RadialMenuPickupButton.onClick.AddListener(() => RadialMenuPickup());
            RadialMenuPickupButton.AddComponent<ButtonScaleEffect>();

            RadialMenuDestroyButton.onClick.AddListener(() => RadialMenuDestroy());
            RadialMenuDestroyButton.AddComponent<ButtonScaleEffect>();

            RadialMenuDragCorpseButton.onClick.AddListener(() => RadialMenuDragCorpse());
            RadialMenuDragCorpseButton.AddComponent<ButtonScaleEffect>();
        }

        private void RadialMenuPickup()
        {
            if (_playerController.TryGetComponent(out ItemHandler itemHandler))
            {
                if (RadialMenuCurrentTarget != null)
                {
                    itemHandler.PickupItem(RadialMenuCurrentTarget);
                    itemHandler.DestroyItem(RadialMenuCurrentTargetGO);
                }
            }
            CloseRadialMenu();
        }
        private void RadialMenuDestroy()
        {
            if (_playerController.TryGetComponent(out ItemHandler itemHandler))
            {
                if (RadialMenuCurrentTarget != null)
                {
                    itemHandler.DestroyItem(RadialMenuCurrentTargetGO);
                }
            }
            CloseRadialMenu();
        }
        private void RadialMenuDragCorpse()
        {

            // _playerInteraction = context.PlayerInteraction;
            // if (IsDragging) StopDragging();
            // else StartDragging();

            CloseRadialMenu();
        }

        private void Update()
        {
            ToggleInventory();
            inventoryManager.DropItemFromSlotByDropKey();
        }

        private void RefreshPreview()
        {
            if (previewManager != null) previewManager.RefreshPreview();
        }
    }
}