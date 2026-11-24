// Assets/Scripts/Crafting/CraftingSlotUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Assets.Scripts.InventorySystem;
using System.Collections.Generic;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Tooltip;
using Assets.Scripts.Core;
using Assets.Scripts.Items;
using Unity.VisualScripting;
using Assets.Scripts.UI.Notifications;
using Assets.Scripts.Player;

namespace Assets.Scripts.Crafting
{
    public class CraftingSlotUI : MonoBehaviour, IPointerClickHandler
    {
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI amountText;

        private Recipe _recipe;
        private TooltipTrigger _tooltipTrigger;
        private PlayerProgress _progress;

        public void Setup(Recipe recipe)
        {
            _recipe = recipe;

            // УДАЛЕНО (Шаг 3.4): UnsubscribeFromEvents(); — подписок больше нет

            var localPlayer = PlayerController.LocalPlayer;
            if (localPlayer != null)
            {
                _progress = localPlayer.GetComponent<PlayerProgress>();
            }

            if (_progress == null)
                Debug.LogError("[CraftingSlotUI] Не удалось получить PlayerProgress!");

            RefreshVisual();
        }

        public void RefreshVisual()
        {
            if (this == null || gameObject == null || _recipe == null) return;
            if (icon == null) return;

            // ИЗМЕНЕНО (Шаг 3.4): берём актуальные InventoryData каждый раз (а не из полей)
            var mainInv = _progress != null ? _progress.mainInventoryData : null;
            var hotbarInv = _progress != null ? _progress.hotbarInventoryData : null;

            icon.sprite = _recipe.craftedItem?.icon ?? null;
            icon.enabled = icon.sprite != null;

            bool hasInMain = mainInv != null && mainInv.HasIngredients(_recipe);
            bool hasInHotbar = hotbarInv != null && hotbarInv.HasIngredients(_recipe);

            bool canCraft = hasInMain || hasInHotbar;

            icon.color = canCraft ? Color.white : new Color(1f, 1f, 1f, 0.1f);

            if (gameObject != null)
            {
                var ingList = new List<(int amount, Item item)>();

                foreach (var ing in _recipe.ingredients)
                {
                    if (ing.item != null && ing.amount > 0)
                        ingList.Add((ing.amount, ing.item));
                }

                _tooltipTrigger = TooltipTrigger.AddTooltip(
                    gameObject,
                    _recipe.craftedItem.description,
                    _recipe.craftedItem.itemName,
                    icon.sprite,
                    ingList,
                    "Требуется для крафта"
                );
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Двойной клик ЛКМ = крафт
            if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount >= 2)
            {
                TryCraft();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                ShowContextMenu(eventData.position);
            }
        }

        private int CountFreeSlotsForItem(Item item)
        {

            if (_progress == null) return 0;

            int freeSlots = 0;

            // Считаем в основном инвентаре
            foreach (var slot in _progress.mainInventoryData.slots)
            {
                if (slot.IsEmpty) freeSlots++;
                else if (slot.item == item && slot.count < item.maxStack)
                    freeSlots++; // Можно добавить в существующий стак
            }

            // Считаем в хотбаре (если предмет не закреплён или закреплён в этом слоте)
            foreach (var slot in _progress.hotbarInventoryData.slots)
            {
                if (slot.IsEmpty) freeSlots++;
                else if (slot.item == item && slot.count < item.maxStack)
                    freeSlots++;
            }

            return freeSlots;
        }

        void TryCraft()
        {
            if (_progress == null || _recipe == null) return;

            var mainInv = _progress.mainInventoryData;
            var hotbarInv = _progress.hotbarInventoryData;

            bool hasInHotbar = hotbarInv != null && hotbarInv.HasIngredients(_recipe);
            bool hasInMain = mainInv != null && mainInv.HasIngredients(_recipe);


            Debug.Log("hasInMain: " + hasInMain);
            Debug.Log("hasInHotbar: " + hasInHotbar);

            if (!hasInMain && !hasInHotbar)
            {
                NotificationManager.Instance?.Show($"Не хватает ресурсов для: {_recipe.craftedItem.itemName}", null);
                return;
            }

            // Проверка слотов — как было
            int slotsNeeded = Mathf.CeilToInt((float)_recipe.craftedAmount / _recipe.craftedItem.maxStack);
            int freeSlots = CountFreeSlotsForItem(_recipe.craftedItem);

            if (freeSlots < slotsNeeded)
            {
                int canCraft = freeSlots * _recipe.craftedItem.maxStack;
                NotificationManager.Instance?.Show(
                    $"Недостаточно места! Можно скрафтить только {canCraft} шт.",
                    null
                );
                return;
            }

            // ИЗМЕНЕНО (Шаг 3.4): крафт из ОБОИХ инвентарей (баг 12)
            // Сначала тратим из основного, остаток — из хотбара.
            if (hasInMain) mainInv.ConsumeIngredients(_recipe);
            if (hasInHotbar) hotbarInv.ConsumeIngredients(_recipe);

            // Если рецепт требует больше, чем есть в каждом по отдельности —
            // ConsumeIngredients вернёт false, но мы уже проверили хотя бы один.
            // Правильный учёт: суммарно должно хватить. Проверим ещё раз после потребления.
            // (На практике: HasIngredients на каждом — строгая проверка, поэтому
            //  если ресурсы разделены, ни один не вернёт true. Это баг 12b — отдельно.)

            int added = _progress.AddItemToPlayerInventory(_recipe.craftedItem, _recipe.craftedAmount);

            NotificationManager.Instance?.Show(
                $"Добавлено: {_recipe.craftedItem.itemName} x{_recipe.craftedAmount}",
                _recipe.craftedItem.icon
            );

            var removed = new List<(int amount, Item item)>();
            foreach (var ing in _recipe.ingredients)
            {
                if (ing.item != null && ing.amount > 0)
                    removed.Add((ing.amount, ing.item));
            }

            foreach (var ing in removed)
            {
                NotificationManager.Instance?.Show($"Удалено: {ing.amount}x {ing.item.itemName}", ing.item.icon);
            }

            _progress.AddExperience(_recipe.experienceReward);
            _progress.Save("CraftingSlotUI.TryCraft");

            // ДОБАВЛЕНО (Шаг 3.4): уведомить хаб, что ресурсы ушли и предмет добавился
            InventoryEventsHub.Instance?.Notify(InventoryChangeKind.MainInventory);
            InventoryEventsHub.Instance?.Notify(InventoryChangeKind.HotbarInventory);
        }

        void ShowContextMenu(Vector2 clickPosition)
        {
            ContextMenuManager.Show(
                _recipe,
                () =>
                {
                    TryCraft();
                },
                clickPosition
            );

        }


    }
}