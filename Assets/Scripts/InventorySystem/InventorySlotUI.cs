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
using Assets.Scripts.Crafting;
using Assets.Scripts.UI.Notifications;


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
        public Image durabilityBarFill;

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

        // ДОБАВЛЕНО (фикс A): ссылка на ChestUI, ИЗ которого начали тащить.
        // Раньше GetOriginalSlot() лез в глобальный ChestUI.Instance — если тот уже
        // закрыт/переключён на другой источник, оригинальный слот не находился
        // и предмет терялся. Теперь слот знает только свой UI.
        private ChestUI _dragSourceChestUI;

        // External services inventoryUI
        private PlayerProgress _progress;
        private InventoryManager _inventoryManager;
        private TooltipTrigger _tooltipTrigger;
        private PlayerInputHandler _inputHandler;
        private ItemUsageRouter _itemUsageRouter;

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
            _inventoryManager = ui.InventoryManager;
            if (_inventoryManager != null)
            {
                _inputHandler = _inventoryManager.InputHandler;
                _itemUsageRouter = _inventoryManager.ItemUsageRouter;
                _progress = _inventoryManager.Progress;
            }
        }

        public void SetupHotbar(int slotIndex, HotbarUI ui)
        {
            index = slotIndex;
            owner = SlotOwner.Hotbar;
            hotbarUI = ui;
            inventoryUI = null;
            chestUI = null;
            _inventoryManager = ui.InventoryManager;
            if (_inventoryManager != null)
            {
                _inputHandler = _inventoryManager.InputHandler;
                _itemUsageRouter = _inventoryManager.ItemUsageRouter;
                _progress = _inventoryManager.Progress;
            }
        }

        public void SetupChest(int slotIndex, ChestUI ui)
        {
            index = slotIndex;
            owner = SlotOwner.Chest;
            chestUI = ui;
            inventoryUI = null;
            hotbarUI = null;
            _inventoryManager = ui.InventoryManager;
            if (_inventoryManager != null)
            {
                _inputHandler = _inventoryManager.InputHandler;
                _itemUsageRouter = _inventoryManager.ItemUsageRouter;
                _progress = _inventoryManager.Progress;
            }
        }

        /// <summary>
        /// ДОБАВЛЕНО (фикс drag): страховка от "осиротевшего" драга.
        /// Если GameObject источника уничтожен (сумка опустела/задеспавнилась),
        /// Unity не присылает OnEndDrag — иконка висит вечно, предмет теряется.
        /// Здесь ловим этот случай сами.
        /// </summary>
        private void Update()
        {
            if (dragIcon == null) return;

            // Драг ещё жив: наш объект цел и мы всё ещё считаемся перетаскиваемым
            bool sourceAlive = this != null && gameObject != null && isActiveAndEnabled;
            bool dragContextAlive = DragContext.draggedItem != null && DragContext.draggedCount > 0;

            if (sourceAlive && dragContextAlive) return;

            Debug.LogWarning("[InventorySlotUI] Драг оборван (источник уничтожен?) — возвращаем предмет и убираем иконку.");

            Destroy(dragIcon);
            dragIcon = null;

            if (dragContextAlive)
                ReturnToOriginalSlot();

            CleanupDragContext();
            RefreshAllUIs();
        }
        
        private void EnsureReferences()
        {
            if (_inventoryManager == null)
            {
                if (inventoryUI != null) _inventoryManager = inventoryUI.InventoryManager;
                if (_inventoryManager == null && hotbarUI != null) _inventoryManager = hotbarUI.InventoryManager;
                if (_inventoryManager == null && chestUI != null) _inventoryManager = chestUI.InventoryManager;

                if (_inventoryManager == null)
                {
                    var lp = PlayerController.LocalPlayer;
                    if (lp != null) _inventoryManager = lp.GetComponent<InventoryManager>();
                }
            }

            if (_inventoryManager != null)
            {
                // Благодаря ленивым геттерам эти вызовы всегда актуальны
                if (_inputHandler == null) _inputHandler = _inventoryManager.InputHandler;
                if (_itemUsageRouter == null) _itemUsageRouter = _inventoryManager.ItemUsageRouter;
                if (_progress == null) _progress = _inventoryManager.Progress;
            }
        }

        // Получаем слот безопасно
        public InventorySlot GetSlot()
        {
            EnsureReferences();

            if (_progress == null)
            {
                Debug.LogError("PlayerProgress is null");
                return null;
            }

            try
            {
                if (IsChestSlot)
                {
                    var slot = chestUI?.GetSlot(index);
                    return slot;
                }
                else if (IsHotBarSlot)
                {
                    // Хотбар: слоты 0-9 напрямую из hotbarInventory
                    if (_progress.hotbarInventoryData != null &&
                        index >= 0 && index < _progress.hotbarInventoryData.slots.Count)
                    {
                        return _progress.hotbarInventoryData.slots[index];
                    }
                }
                else if (IsMainInventorySlot)
                {
                    if (_progress.mainInventoryData != null &&
                        index >= 0 && index < _progress.mainInventoryData.slots.Count)
                    {
                        return _progress.mainInventoryData.slots[index];
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
            EnsureReferences();

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
                if (durabilityBarFill != null)
                {
                    if (slot.item.hasDurability)
                    {
                        float currentValue = slot.currentDurability;
                        float maxValue = slot.item.maxDurability;
                        durabilityBarFill.fillAmount = (maxValue > 0) ? Mathf.Clamp01(currentValue / maxValue) : 0f;

                        // Опционально: можно задать цвет (полная - зеленая, почти сломанная - красная)
                        durabilityBarFill.color = durabilityBarFill.fillAmount < 0.2f ? Color.red : Color.green;
                        if (durabilityBarFill.fillAmount == 0)
                        {
                            icon.color = Color.red;
                        }
                    }
                    else
                    {
                        // Просто скрываем заливку, если это не инструмент
                        durabilityBarFill.fillAmount = 0f;
                    }
                }

                // список для ремонта
                var ingList = new List<(int amount, Item item)>();
                ingList.Clear();

                string durabilityText = "";

                if (slot.item.hasDurability)
                {
                    Recipe recipe = GameAssets.Instance?.RecipeDatabase.GetRecipeForItem(slot.item);
                    var invData = _progress.mainInventoryData;

                    durabilityText = $"\n\nПрочность: {Mathf.RoundToInt(slot.currentDurability)} / {slot.item.maxDurability}";

                    float repairMultiplier = invData.GetRepairMultiplier(slot.item.maxDurability, slot.currentDurability);

                    foreach (var ing in recipe.ingredients)
                    {
                        if (ing.item == null || ing.amount <= 0) continue;

                        int actual = invData.GetRepairAmount(ing.amount, repairMultiplier);
                        ingList.Add((actual, ing.item));
                    }
                }
                else
                {
                    ingList = null;
                }


                string tooltipText = $"{slot.item.description}{durabilityText}";

                _tooltipTrigger = TooltipTrigger.AddTooltip(
                    gameObject,
                    tooltipText,
                    slot.item.itemName,
                    icon.sprite,
                    ingList,
                    "Требуется для ремонта"
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

                if (durabilityBarFill != null) durabilityBarFill.fillAmount = 0f;
            }
        }

        // region === DRAG & DROP ===

        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureReferences();

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
            DragContext.draggedDurability = slot.currentDurability;
            DragContext.isDragFromChest = IsChestSlot;
            DragContext.fromOwner = owner;
            DragContext.fromSlotIndex = index;

            // ДОБАВЛЕНО (фикс A): запоминаем свой ChestUI как источник драга
            _dragSourceChestUI = IsChestSlot ? chestUI : null;

            // Удаляем из исходного слота
            slot.count -= amount;
            if (slot.count <= 0)
            {
                slot.item = null;
                slot.count = 0;
                slot.currentDurability = -1f;
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
            EnsureReferences();

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

                    // === ВАЖНО: помечаем ТОЛЬКО при дропе в хотбар ===
                    if (IsHotBarSlot)
                    {
                        // Debug.Log($"[Hotbar] Item '{item.itemName}' marked for slot {index}");
                        _progress.MarkItemAsHotbarPreferred(item, index);
                    }

                    if (DragContext.fromOwner == SlotOwner.Hotbar && !IsHotBarSlot)
                    {
                        // Мы утащили предмет с хотбара в инвентарь (или сундук)
                        _progress.UnmarkItemAsHotbarPreferred(DragContext.draggedItem);
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
                // ДОБАВЛЕНО: если originalSlot не найден (ChestUI закрылся) — принудительно вернуть в DragContext
                if (GetOriginalSlot() == null && DragContext.draggedItem != null && DragContext.draggedCount > 0)
                {
                    // Не смогли вернуть — предмет потерян. Логируем и очищаем.
                    Debug.LogError($"[InventorySlotUI] Не удалось вернуть {DragContext.draggedItem.itemName} x{DragContext.draggedCount} — оригинальный слот недоступен (источник закрыт?). Предмет потерян.");
                }
                // ДОБАВЛЕНО (фикс C): остаток, который не влез обратно в стак
                else if (DragContext.draggedCount > 0)
                {
                    Debug.LogError($"[InventorySlotUI] Не удалось вернуть полностью: {DragContext.draggedItem?.itemName} x{DragContext.draggedCount} — в исходном слоте нет места.");
                }
            }

            CleanupDragContext();
            RefreshAllUIs();

            // ПРАВКА (Пункт 9): на клиенте в мультиплеере не пишем на диск
            if (!(SessionMode.IsMultiplayer && !SessionMode.IsServer))
            {
                var progress = _progress;
                progress?.Save("InventorySlotUI.OnDrop"); // ДОБАВЛЕНО: ?. вместо безусловного вызова
            }

        }

        private bool MoveItemToSlot(InventorySlot targetSlot, Item item, int count)
        {
            if (targetSlot == null) return false;

            // ВАЖНО: Добавим параметр прочности в метод или берем из DragContext
            float droppedDurability = DragContext.draggedDurability; // Нужно добавить в DragContext!

            if (targetSlot.IsEmpty)
            {
                targetSlot.item = item;
                targetSlot.count = count;
                targetSlot.currentDurability = droppedDurability;
                return true;
            }
            else if (targetSlot.item == item && targetSlot.item.maxStack > 1) // Стакаем
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
                var originalSlot = GetOriginalSlot();
                if (originalSlot == null)
                {
                    Debug.LogError("[MoveItemToSlot] Swap: originalSlot == null! Отмена, targetSlot не трогаем.");
                    return false;
                }

                var tempItem = targetSlot.item;
                var tempCount = targetSlot.count;
                var tempDurability = targetSlot.currentDurability;

                targetSlot.item = item;
                targetSlot.count = count;
                targetSlot.currentDurability = droppedDurability;

                originalSlot.item = tempItem;
                originalSlot.count = tempCount;
                originalSlot.currentDurability = tempDurability;

                return true;
            }
        }

        /// <summary>
        /// Найти слот, из которого начали перетаскивание.
        /// ПРАВКА (фикс A): для chest-дропа используется сохранённая ссылка на свой ChestUI,
        /// а не глобальный ChestUI.Instance.
        /// </summary>
        private InventorySlot GetOriginalSlot()
        {
            try
            {
                if (DragContext.isDragFromChest)
                {
                    // было: var chestUI = ChestUI.Instance;
                    var sourceUI = _dragSourceChestUI != null ? _dragSourceChestUI : chestUI; //удалить: ChestUI.Instance
                    if (sourceUI != null)
                        return sourceUI.GetSlot(DragContext.fromSlotIndex);
                }
                else
                {
                    if (DragContext.fromOwner == SlotOwner.Hotbar)
                    {
                        var hotbar = _progress?.hotbarInventoryData;
                        if (hotbar != null && DragContext.fromSlotIndex >= 0 && DragContext.fromSlotIndex < hotbar.slots.Count)
                            return hotbar.slots[DragContext.fromSlotIndex];
                    }
                    else if (DragContext.fromOwner == SlotOwner.Inventory)
                    {
                        var mainInv = _progress?.mainInventoryData;
                        // int mainIndex = DragContext.fromSlotIndex - 10;
                        int mainIndex = DragContext.fromSlotIndex;

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

        /// <summary>
        /// Вернуть перетаскиваемый предмет в исходный слот (дроп не состоялся).
        /// ПРАВКА (фикс C): возвращается и прочность; невозвращённый остаток
        /// остаётся в DragContext и логируется вызывающим, а не исчезает молча.
        /// </summary>
        private void ReturnToOriginalSlot()
        {
            var originalSlot = GetOriginalSlot();
            if (originalSlot == null) return;

            if (originalSlot.IsEmpty)
            {
                originalSlot.item = DragContext.draggedItem;
                originalSlot.count = DragContext.draggedCount;
                // ДОБАВЛЕНО: прочность раньше терялась при возврате
                originalSlot.currentDurability = DragContext.draggedDurability;

                DragContext.draggedCount = 0;
            }
            else if (originalSlot.item == DragContext.draggedItem)
            {
                int space = originalSlot.item.maxStack - originalSlot.count;
                int add = Mathf.Min(space, DragContext.draggedCount);
                originalSlot.count += add;

                // ДОБАВЛЕНО: вычитаем возвращённое, остаток не теряем молча
                DragContext.draggedCount -= add;
            }
        }

        private void CleanupDragContext()
        {
            DragContext.draggedItem = null;
            DragContext.draggedCount = 0;
            DragContext.draggedDurability = -1f;
            DragContext.isDragFromChest = false;
            DragContext.fromOwner = default;
            DragContext.fromSlotIndex = -1;

            // ДОБАВЛЕНО (фикс A): не держим ссылку на чужой UI между драгами
            _dragSourceChestUI = null;
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
            EnsureReferences();

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
            EnsureReferences();

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
            EnsureReferences();
            var slot = GetSlot();
            if (slot?.IsEmpty == false)
            {
                if (owner == SlotOwner.Chest)
                {
                    ContextMenuManager.Show(
                        slot.item,
                        () =>
                        {
                            _inventoryManager.SelectSlot(index, owner, this);
                            HighLightHoverSlot(true);
                            _itemUsageRouter.UseItem(slot, owner, index); // Use (для наркотика/еды)
                        },
                        () =>
                        {
                            _inventoryManager.DropItemFromSlot(index, owner);
                            HighLightHoverSlot(false);
                        },
                        () =>
                        {
                            _inventoryManager.TryRepairItem(index, owner);
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
                            _itemUsageRouter.UseItem(slot, owner, index, this);
                        },
                        () =>
                        {
                            _inventoryManager.DropItemFromSlot(index, owner);
                            HighLightHoverSlot(false);
                        },
                        () =>
                        {
                            _inventoryManager.TryRepairItem(index, owner);
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

        public void HighLightSelectedSlot(bool setSelected)
        {
            if (selectedBackground != null)
                selectedBackground.SetActive(setSelected);
            // НЕ останавливаем корутины флеша!
        }

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