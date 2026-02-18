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

namespace Assets.Scripts.Crafting
{
    public class CraftingSlotUI : MonoBehaviour, IPointerClickHandler
    {
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI amountText;

        private Recipe _recipe;
        private InventoryData _mainInventoryData;
        private InventoryData _hotbarInventoryData;
        private TooltipTrigger _tooltipTrigger;

        public void Setup(Recipe recipe)
        {
            _recipe = recipe;
            UnsubscribeFromEvents(); // Отписываемся от старых данных
            var progress = PlayerProgress.Instance;
            if (progress != null)
            {
                _mainInventoryData = progress.mainInventoryData;
                _hotbarInventoryData = progress.hotbarInventoryData;

                // Подписываемся на ОБА инвентаря — ингредиенты могут быть в любом!
                _mainInventoryData.OnInventoryChanged += RefreshVisual;
                _hotbarInventoryData.OnInventoryChanged += RefreshVisual;
            }

            RefreshVisual();
        }

        private void UnsubscribeFromEvents()
        {
            if (_mainInventoryData != null) _mainInventoryData.OnInventoryChanged -= RefreshVisual;
            if (_hotbarInventoryData != null) _hotbarInventoryData.OnInventoryChanged -= RefreshVisual;
        }

        public void RefreshVisual()
        {
            if (this == null || gameObject == null || _recipe == null) return;
            if (icon == null) return;

            icon.sprite = _recipe.craftedItem?.icon ?? null;
            icon.enabled = icon.sprite != null;

            icon.sprite = _recipe.craftedItem.icon;
            icon.enabled = icon.sprite != null;


            // Проверяем наличие ингредиентов в ОБОИХ инвентарях
            bool hasInMain = _mainInventoryData?.HasIngredients(_recipe) ?? false;
            bool hasInHotbar = _hotbarInventoryData?.HasIngredients(_recipe) ?? false;
            bool canCraft = hasInMain || hasInHotbar;
            icon.color = canCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);

            // Tooltip: тоже проверяем
            if (gameObject != null)
            {
                _tooltipTrigger = TooltipTrigger.AddTooltip(
                    gameObject,
                    _recipe.description,
                    _recipe.craftedItem.itemName,
                    icon.sprite
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
        }

        private int CountFreeSlotsForItem(Item item)
        {
            var progress = PlayerProgress.Instance;
            if (progress == null) return 0;

            int freeSlots = 0;

            // Считаем в основном инвентаре
            foreach (var slot in progress.mainInventoryData.slots)
            {
                if (slot.IsEmpty) freeSlots++;
                else if (slot.item == item && slot.count < item.maxStack)
                    freeSlots++; // Можно добавить в существующий стак
            }

            // Считаем в хотбаре (если предмет не закреплён или закреплён в этом слоте)
            foreach (var slot in progress.hotbarInventoryData.slots)
            {
                if (slot.IsEmpty) freeSlots++;
                else if (slot.item == item && slot.count < item.maxStack)
                    freeSlots++;
            }

            return freeSlots;
        }

        void TryCraft()
        {
            var progress = PlayerProgress.Instance;
            if (progress == null || _recipe == null) return;

            // Проверяем наличие ингредиентов в ОБОИХ инвентарях
            bool hasInMain = progress.mainInventoryData.HasIngredients(_recipe);
            bool hasInHotbar = progress.hotbarInventoryData.HasIngredients(_recipe);

            if (!hasInMain && !hasInHotbar)
            {
                NotificationManager.Instance?.Show($"Не хватает ресурсов для: {_recipe.recipeName}", null);
                return;
            }

            // 2. ⚠️ КРИТИЧЕСКИ ВАЖНО: проверяем слоты ДО потребления ингредиентов!
            int slotsNeeded = Mathf.CeilToInt((float)_recipe.craftedAmount / _recipe.craftedItem.maxStack);
            int freeSlots = CountFreeSlotsForItem(_recipe.craftedItem); // Учитывает стаки существующих кирок

            if (freeSlots < slotsNeeded)
            {
                int canCraft = freeSlots * _recipe.craftedItem.maxStack;
                NotificationManager.Instance?.Show(
                    $"Недостаточно места! Можно скрафтить только {canCraft} шт.",
                    null
                );
                return; // ← НЕ потребляем ингредиенты!
            }

            // Потребляем ингредиенты — сначала из основного инвентаря
            if (hasInMain)
            {
                progress.mainInventoryData.ConsumeIngredients(_recipe);
                progress.mainInventoryData.NotifyChanged();
            }
            else if (hasInHotbar)
            {
                progress.hotbarInventoryData.ConsumeIngredients(_recipe);
                progress.hotbarInventoryData.NotifyChanged();
            }

            // Добавляем результат крафта
            int added = progress.AddItemToPlayerInventory(_recipe.craftedItem, _recipe.craftedAmount);


            // Собираем список удалённых ингредиентов
            var removed = new List<string>();
            foreach (var ing in _recipe.ingredients)
            {
                if (ing.item != null && ing.amount > 0)
                {
                    removed.Add($"{ing.amount}x {ing.item.itemName}");
                }
            }


            // Уведомления
            NotificationManager.Instance?.Show(
                $"Добавлено: {_recipe.craftedItem.itemName} x{_recipe.craftedAmount}",
                _recipe.craftedItem.icon
            );

            foreach (var line in removed)
            {
                NotificationManager.Instance?.Show($"Удалено: {line}", null);
            }

            // Опыт за крафт
            progress.AddExperience(_recipe.experienceReward);
            progress.Save();
        }

        // Важно: отписаться при уничтожении, чтобы избежать ошибок
        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

    }
}