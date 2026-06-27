using System.Collections.Generic;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Assets.Scripts.UI.Tooltip;
using Assets.Scripts.Core;
using System.Collections;
using Assets.Scripts.Items;


namespace Assets.Scripts.InventorySystem
{
    public enum SlotOwner
    {
        Inventory,
        Hotbar,
        Chest,

    }

    public class InventorySlotUI : MonoBehaviour,
        IBeginDragHandler, IEndDragHandler, IDragHandler, IDropHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Image icon;
        // public Image background;
        public GameObject selectedBackground;
        public GameObject flashBackground;
        public GameObject hoverBackground;
        public TextMeshProUGUI countText;
        public TextMeshProUGUI keyText;

        // UI references
        // public InventoryManager InventoryManager;
        public InventoryUI inventoryUI;
        public HotbarUI hotbarUI;
        public ChestUI chestUI;

        // Slot identity
        public int index;
        public SlotOwner owner;

        // Drag state
        private GameObject dragIcon;
        private RectTransform dragRT;

        // External services inventoryUI
        private InventoryManager _inventoryManager;
        private TooltipTrigger _tooltipTrigger;
        [SerializeField] private PlayerInputHandler _inputHandler;

        private void Awake()
        {
            if (_inputHandler == null)
            {
                _inputHandler = FindAnyObjectByType<PlayerInputHandler>();
            }
        }

        // Заменяем ссылки на MonoBehaviour индексами и флагами
        private bool IsChestSlot => owner == SlotOwner.Chest;
        private bool IsHotBarSlot => owner == SlotOwner.Hotbar;
        private bool IsMainInventorySlot => owner == SlotOwner.Inventory;
        private bool IsPlayerSlot => IsMainInventorySlot || owner == SlotOwner.Hotbar;

        public void Setup(int slotIndex, InventoryUI ui)
        {
            index = slotIndex;
            owner = SlotOwner.Inventory;
            inventoryUI = ui;
            hotbarUI = null;
            chestUI = null;
            _inventoryManager = ui.inventoryManager;
        }

        public void SetupHotbar(int slotIndex, HotbarUI ui)
        {
            index = slotIndex;
            owner = SlotOwner.Hotbar;
            hotbarUI = ui;
            inventoryUI = null;
            chestUI = null;
            _inventoryManager = ui.inventoryManager;
        }

        public void SetupChest(int slotIndex, ChestUI ui)
        {
            index = slotIndex;
            owner = SlotOwner.Chest;
            chestUI = ui;
            inventoryUI = null;
            hotbarUI = null;
            _inventoryManager = ui.inventoryManager;
        }

        // Получаем слот безопасно
        public InventorySlot GetSlot()
        {
            var progress = PlayerProgress.Instance;
            if (progress == null)
            {
                Debug.LogError("progress is null");
                return null;
            }

            try
            {
                if (IsChestSlot)
                {
                    return chestUI?.GetSlot(index);
                }
                else if (IsHotBarSlot)
                {
                    // Хотбар: слоты 0-9 напрямую из hotbarInventory
                    if (progress.hotbarInventoryData != null &&
                        index >= 0 && index < progress.hotbarInventoryData.slots.Count)
                    {
                        return progress.hotbarInventoryData.slots[index];
                    }
                }
                else if (IsMainInventorySlot)
                {
                    // var data = PlayerProgress.Instance.inventoryData;
                    // if (data != null && index >= 0 && index < data.slots.Count)
                    //     return data.slots[index];
                    if (progress.mainInventoryData != null &&
                        index >= 0 && index < progress.mainInventoryData.slots.Count)
                    {
                        return progress.mainInventoryData.slots[index];
                    }
                }
            }
            catch (System.NullReferenceException)
            {
                // Защита от "зомби"-объектов после recompile
                return null;
            }
            return null;
        }

        public void SetSlot(InventorySlot slot)
        {
            if (this == null || gameObject == null || icon == null)
                return;

            bool isValid = slot != null && !slot.IsEmpty;
            bool hasValidIcon = isValid && slot.item.icon != null;

            icon.enabled = isValid;
            icon.sprite = hasValidIcon ? slot.item.icon : null;
            icon.color = Color.white;

            // display hotbar key
            if (index < 10 && owner == SlotOwner.Hotbar)
            {
                keyText.text = index == 9 ? "0" : $"{index + 1}";
            }
            else
            {
                keyText.gameObject.SetActive(false);
            }
            countText.text = hasValidIcon && slot.count > 1 ? $"x{slot.count}" : "";

            if (isValid)
            {
                _tooltipTrigger = TooltipTrigger.AddTooltip(
                    gameObject,
                    slot.item.description,
                    slot.item.itemName,
                    icon.sprite
                );
            }
            else
            {
                if (TryGetComponent<TooltipTrigger>(out var trigger))
                {
                    trigger.Title = "";
                    trigger.Content = "";
                    trigger.Icon = null;
                }
                _tooltipTrigger = null;
            }
        }

        // region === DRAG & DROP ===

        public void OnBeginDrag(PointerEventData eventData)
        {
            var slot = GetSlot();
            if (slot == null || slot.IsEmpty) return;

            int amount;
            // 1. Правая кнопка мыши - берем 1 шт
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                amount = 1;
            }
            // 2. Проверка клавиши модификатора ЧЕРЕЗ КОНФИГ (вместо жесткого LeftShift)
            // else if (Input.GetKey(InventoryConfig.Instance.splitStackKey))
            else if (_inputHandler.leftShift)
            {
                amount = Mathf.CeilToInt(slot.count / 2f);
            }
            // 3. Иначе весь стак
            else
            {
                amount = slot.count;
            }

            // Сохраняем контекст без ссылок на MonoBehaviour
            DragContext.draggedItem = slot.item;
            DragContext.draggedCount = amount;
            DragContext.isDragFromChest = IsChestSlot;
            DragContext.fromOwner = owner;
            DragContext.fromSlotIndex = index;

            // Удаляем из исходного слота
            slot.count -= amount;
            if (slot.count <= 0)
            {
                slot.item = null;
                slot.count = 0;
            }

            // Определяем drag layer
            RectTransform dragLayer = null;
            Canvas rootCanvas = null;

            if (owner == SlotOwner.Inventory && inventoryUI != null)
            {
                dragLayer = inventoryUI.dragLayer;
                rootCanvas = inventoryUI.rootCanvas;
            }
            else if (owner == SlotOwner.Hotbar && hotbarUI != null)
            {
                dragLayer = hotbarUI.dragLayer;
                rootCanvas = hotbarUI.rootCanvas;
            }
            else if (owner == SlotOwner.Chest && chestUI != null)
            {
                dragLayer = chestUI.dragLayer;
                rootCanvas = chestUI.rootCanvas;
            }

            if (dragLayer == null || rootCanvas == null)
            {
                Debug.LogError("Drag layer or canvas missing for slot owner: " + owner);
                return;
            }

            // Создаём иконку перетаскивания
            dragIcon = new GameObject("DragIcon");
            dragIcon.transform.SetParent(dragLayer, false);

            dragRT = dragIcon.AddComponent<RectTransform>();
            var img = dragIcon.AddComponent<Image>();
            img.sprite = icon.sprite;
            img.raycastTarget = false;
            dragRT.sizeDelta = icon.rectTransform.sizeDelta;

            RefreshAllUIs();
            UpdateDragPosition(eventData, dragLayer, rootCanvas);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragIcon == null) return;

            RectTransform dragLayer = GetDragLayer();
            Canvas canvas = GetRootCanvas();
            if (dragLayer == null || canvas == null) return;

            UpdateDragPosition(eventData, dragLayer, canvas);
        }

        private void UpdateDragPosition(PointerEventData eventData, RectTransform dragLayer, Canvas canvas)
        {
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position, cam, out Vector2 localPoint))
            {
                dragRT.anchoredPosition = localPoint;
            }
        }

        private RectTransform GetDragLayer()
        {
            return owner switch
            {
                SlotOwner.Inventory => inventoryUI?.dragLayer,
                SlotOwner.Hotbar => hotbarUI?.dragLayer,
                SlotOwner.Chest => chestUI?.dragLayer,
                _ => null
            };
        }

        private Canvas GetRootCanvas()
        {
            return owner switch
            {
                SlotOwner.Inventory => inventoryUI?.rootCanvas,
                SlotOwner.Hotbar => hotbarUI?.rootCanvas,
                SlotOwner.Chest => chestUI?.rootCanvas,
                _ => null
            };
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragIcon != null)
            {
                Destroy(dragIcon);
                dragIcon = null;
            }

            if (DragContext.draggedItem != null && DragContext.draggedCount > 0)
            {
                ReturnToOriginalSlot();
                CleanupDragContext();
            }

            RefreshAllUIs();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (DragContext.draggedItem == null || DragContext.draggedCount <= 0)
            {
                CleanupDragContext();
                RefreshAllUIs();
                return;
            }

            bool success = false;
            var targetSlot = GetSlot();

            if (targetSlot != null)
            {
                success = MoveItemToSlot(targetSlot, DragContext.draggedItem, DragContext.draggedCount);

                if (success)
                {
                    Item item = DragContext.draggedItem;
                    int moved = DragContext.draggedCount;

                    // Debug.Log($"item {item.itemName} {index}");
                    // PlayerProgress.Instance.MarkItemAsHotbarPreferred(item, index);
                    // === ВАЖНО: помечаем ТОЛЬКО при дропе в хотбар ===
                    if (IsHotBarSlot)
                    {
                        Debug.Log($"[Hotbar] Item '{item.itemName}' marked for slot {index}");
                        PlayerProgress.Instance.MarkItemAsHotbarPreferred(item, index);
                    }

                    string action = null;
                    if (IsChestSlot && !DragContext.isDragFromChest)
                    {
                        action = "Перемещено";
                    }
                    if (IsPlayerSlot && DragContext.isDragFromChest)
                    {
                        action = "Добавлено";
                    }
                    if (NotificationManager.Instance != null && action != null)
                    {
                        NotificationManager.Instance.Show(
                            $"{action}: {item.itemName} x{moved}",
                            item.icon
                        );
                    }

                }
            }

            if (!success)
            {
                ReturnToOriginalSlot();
            }

            CleanupDragContext();
            RefreshAllUIs();
        }

        private bool MoveItemToSlot(InventorySlot targetSlot, Item item, int count)
        {
            if (targetSlot == null) return false;

            if (targetSlot.IsEmpty)
            {
                targetSlot.item = item;
                targetSlot.count = count;
                return true;
            }
            else if (targetSlot.item == item)
            {
                int space = item.maxStack - targetSlot.count;
                if (space >= count)
                {
                    targetSlot.count += count;
                    return true;
                }
                else
                {
                    targetSlot.count += space;
                    DragContext.draggedCount = count - space;
                    return false;
                }
            }
            else
            {
                // Swap
                var tempItem = targetSlot.item;
                var tempCount = targetSlot.count;

                targetSlot.item = item;
                targetSlot.count = count;

                var originalSlot = GetOriginalSlot();
                if (originalSlot != null)
                {
                    originalSlot.item = tempItem;
                    originalSlot.count = tempCount;
                }
                return true;
            }
        }

        private InventorySlot GetOriginalSlot()
        {
            try
            {
                if (DragContext.isDragFromChest)
                {
                    var currentChestUI = ChestUI.CurrentOpenChest;
                    if (currentChestUI != null)
                        return currentChestUI.GetSlot(DragContext.fromSlotIndex);
                }
                else
                {
                    if (DragContext.fromOwner == SlotOwner.Hotbar)
                    {
                        var hotbar = PlayerProgress.Instance?.hotbarInventoryData;
                        if (hotbar != null && DragContext.fromSlotIndex >= 0 && DragContext.fromSlotIndex < hotbar.slots.Count)
                            return hotbar.slots[DragContext.fromSlotIndex];
                    }
                    else if (DragContext.fromOwner == SlotOwner.Inventory)
                    {
                        var mainInv = PlayerProgress.Instance?.mainInventoryData;
                        int mainIndex = DragContext.fromSlotIndex - 10; // конвертация!

                        if (mainInv != null && mainIndex >= 0 && mainIndex < mainInv.slots.Count)
                            return mainInv.slots[mainIndex];
                    }
                }
            }
            catch (System.NullReferenceException)
            {
                return null;
            }
            return null;
        }

        private void ReturnToOriginalSlot()
        {
            var originalSlot = GetOriginalSlot();
            if (originalSlot == null) return;

            if (originalSlot.IsEmpty)
            {
                originalSlot.item = DragContext.draggedItem;
                originalSlot.count = DragContext.draggedCount;
            }
            else if (originalSlot.item == DragContext.draggedItem)
            {
                int space = originalSlot.item.maxStack - originalSlot.count;
                int add = Mathf.Min(space, DragContext.draggedCount);
                originalSlot.count += add;
            }
        }

        private void CleanupDragContext()
        {
            DragContext.draggedItem = null;
            DragContext.draggedCount = 0;
            DragContext.isDragFromChest = false;
            DragContext.fromOwner = default;
            DragContext.fromSlotIndex = -1;
        }

        // end region === DRAG & DROP ===

        private void RefreshAllUIs()
        {
            inventoryUI?.RefreshUI();
            hotbarUI?.RefreshUI();
            chestUI?.RefreshUI();
        }


        // === POINTER EVENTS ===

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_inventoryManager == null)
            {
                Debug.Log("_inventoryManager is null!");
                return;
            }

            if (index < 0)
            {
                Debug.Log("index is < 0!");
            }

            _inventoryManager.SelectSlot(index, owner, this);
            HighLightHoverSlot(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _inventoryManager.SelectSlot(-1, owner, null);
            HighLightHoverSlot(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                _inventoryManager.SelectSlot(index, owner, this);
                ShowContextMenu(eventData.position);
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                // _inventoryManager.SelectSlot(index, owner, this);
            }
        }

        void ShowContextMenu(Vector2 clickPosition)
        {
            var slot = GetSlot();
            if (slot?.IsEmpty == false)
            {
                if (owner == SlotOwner.Chest)
                {
                    ContextMenuManager.Show(
                        slot.item,
                        null,
                        () =>
                        {
                            _inventoryManager.DropItemFromSlot(index, owner);
                            HighLightHoverSlot(false);
                        },
                        clickPosition
                    );
                }
                else
                {
                    ContextMenuManager.Show(
                        slot.item,
                        () =>
                        {
                            _inventoryManager.SelectSlot(index, owner, this);
                            HighLightHoverSlot(true);
                            _inventoryManager.UseItemFromSlot();//!!!!!
                            // HighLightHoverSlot(false);
                        },
                        () =>
                        {
                            _inventoryManager.DropItemFromSlot(index, owner);
                            HighLightHoverSlot(false);
                        },
                        clickPosition
                    );
                }
            }
        }


        // === VISUAL STATES ===

        public void SetVisualState(bool isHovered, bool isSelected, bool flash = false)
        {
            HighLightHoverSlot(isHovered);
            HighLightSelectedSlot(isSelected);
            if (flash) FlashSlot();
        }

        public void HighLightHoverSlot(bool setSelected)
        {
            if (hoverBackground != null) hoverBackground.SetActive(setSelected);
        }

        // public void HighLightSelectedSlot(bool setSelected)
        // {
        //     StopAllCoroutines(); // ← важно!
        //     if (selectedBackground != null) selectedBackground.SetActive(setSelected);
        // }

        public void HighLightSelectedSlot(bool setSelected)
        {
            if (selectedBackground != null)
                selectedBackground.SetActive(setSelected);
            // НЕ останавливаем корутины флеша!
        }

        // public void FlashSlot(float duration = 0.2f)
        // {
        //     if (selectedBackground == null) return;
        //     selectedBackground.SetActive(true);
        //     StartCoroutine(DisableAfterDelay(selectedBackground, duration));
        // }

        public void FlashSlot(float duration = 0.2f)
        {
            if (flashBackground == null) return;

            flashBackground.SetActive(true);
            StartCoroutine(DisableAfterDelay(flashBackground, duration));
        }

        private IEnumerator DisableAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null) obj.SetActive(false);
        }




    }
}