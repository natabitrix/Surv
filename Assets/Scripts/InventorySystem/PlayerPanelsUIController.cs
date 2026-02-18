// Assets/Scripts/InventorySystem/PlayerPanelsUIController.cs
using Assets.Scripts.Core;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Tooltip;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.InventorySystem
{
    public class PlayerPanelsUIController : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler _input;
        [SerializeField] private PlayerController _playerController;

        [Header("Panels")]
        public GameObject TopButtons;
        public GameObject EngramsPanel;
        public GameObject InventoryPanel;

        [Header("Panels Switch Buttons")]
        public Button EngramsPanelButton;
        public Button InventoryPanelButton;
        public Button CloseAllButton;

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

        [Header("Other Managers")]
        public CharacterPreviewManager previewManager;
        public TooltipManager tooltipManager;

        private bool _isInventoryOpened = false;

        // Ссылка на менеджер логики
        public InventoryManager inventoryManager;

        // === TOGGLE PANELS ===
        // Управление состоянием панелей и курсора
        private void PanelMode(bool on = false)
        {
            _isInventoryOpened = on;
            _playerController.LockCameraOnEsc = on;
            _input.SetCursorVisible(on);
        }

        public bool IsInventoryOpened()
        {
            return _isInventoryOpened;
        }

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
            _input.OnInteractEnded += inventoryManager.OnUseItemFinished;
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
            _input.OnInteractEnded -= inventoryManager.OnUseItemFinished;
        }

        // Закрытие панели энграмм
        public void CloseEngramsPanel()
        {
            EngramsPanel.SetActive(false);
        }

        // Закрытие сундука
        public void CloseChestPanel()
        {
            var openChestUI = ChestUI.CurrentOpenChest;
            if (openChestUI != null && openChestUI.SourceChest != null)
            {
                openChestUI.SourceChest.Close();
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