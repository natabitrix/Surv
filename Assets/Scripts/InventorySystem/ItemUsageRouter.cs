using Assets.Scripts.Building;
using Assets.Scripts.Core;
using Assets.Scripts.Creatures;
using Assets.Scripts.Creatures.Taming;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    /// <summary>
    /// Единая точка входа для "использовать предмет из слота".
    /// Роутит запрос в зависимости от контекста: инвентарь игрока, сундук, существо.
    /// </summary>
    public class ItemUsageRouter : MonoBehaviour
    {
        [Header("Player Context")]
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private PlayerBuildMode _buildMode;
        [SerializeField] private ItemUsageSystem _itemUsageSystem;
        [SerializeField] private PanelsUIController _panelsController;

        private int _accumulatedFoodCount = 0;
        private Item _currentFoodItem = null;

        /// <summary>
        /// Использовать предмет из слота. Точка входа для UI и контекстного меню.
        /// </summary>
        public void UseItem(InventorySlot slot, SlotOwner owner, int localIndex, InventorySlotUI ui = null)
        {
            if (slot == null || slot.IsEmpty || slot.item == null) return;

            switch (owner)
            {
                case SlotOwner.Hotbar:
                case SlotOwner.Inventory:
                    UseFromPlayerInventory(slot, owner, localIndex, ui);
                    break;

                case SlotOwner.Chest:
                    UseFromOtherInventory(slot, localIndex);
                    break;
            }
        }

        // === Игрок: экип / еда / постройка ===
        private void UseFromPlayerInventory(InventorySlot slot, SlotOwner owner, int localIndex, InventorySlotUI ui)
        {
            var progress = PlayerProgress.Instance;
            if (progress == null) return;

            var item = slot.item;
            int globalIndex = owner == SlotOwner.Hotbar ? localIndex : localIndex + 10;

            // Снятие уже экипированного
            if (item.itemType == ItemType.Tool || item.itemType == ItemType.Weapon)
            {
                if (_equipment.IsEquipped && item == _equipment.GetCurrentItem())
                {
                    _equipment.Unequip();
                    if (globalIndex == _equipment.EquippedSlotIndex) return;
                }
            }
            else if (item.itemType == ItemType.Placeable)
            {
                if (_buildMode.IsActive() && item == _buildMode.GetCurrentItem())
                {
                    _buildMode.ExitBuildMode();
                    if (globalIndex == _buildMode.ActiveBuildSlotIndex) return;
                }
            }

            switch (item.itemType)
            {
                case ItemType.Tool:
                case ItemType.Weapon:
                    _buildMode.ExitBuildMode();
                    if (slot.currentDurability > 0)
                        _equipment.Equip(item, globalIndex);
                    break;

                case ItemType.Food:
                    _itemUsageSystem.UseItem(item, 1);
                    RemoveOneFromPlayerSlot(owner, localIndex);
                    ui?.SetVisualState(true, false, true);

                    // === Накопление для уведомления ===
                    if (_currentFoodItem == null) _currentFoodItem = item;
                    else if (_currentFoodItem != item) { _accumulatedFoodCount = 0; _currentFoodItem = item; }
                    _accumulatedFoodCount++;

                    break;

                case ItemType.Placeable:
                    _equipment.Unequip();
                    _buildMode.ExitBuildMode();
                    _buildMode.StartBuildMode(item, globalIndex);
                    _panelsController.CloseAllPanels();
                    break;
            }

            progress.Save("ItemUsageRouter.UseFromPlayerInventory");
        }

        // === Чужой инвентарь: только существа, никаких сундуков ===
        private void UseFromOtherInventory(InventorySlot slot, int localIndex)
        {
            var chestUI = ChestUI.Instance;
            if (chestUI == null) return;

            // Только нокаутнутое существо
            if (chestUI.SourceInteractable is not BaseLivingEntity target) return;

            if (!target.knockedOut || target.tamed) return;

            if (target is not Creature creature || creature.Data == null) return;

            var data = creature.Data;

            // Наркотик → torpor
            if (data.narcoticItem != null && slot.item == data.narcoticItem)
            {
                var usedItem = slot.item;
                var usedIcon = slot.item.icon;

                chestUI.Data.RemoveItemFromSlot(localIndex, 1);
                target.AddTorpor(data.narcoticTorporAmount);
                if (!string.IsNullOrEmpty(target.TamingInstanceId))
                {
                    TamingManager.Instance?.UpdateSave(target.TamingInstanceId, "player_001");
                }

                NotificationManager.Instance?.Show(
                    $"Наркотик: {usedItem.itemName}. Torpor: {target.torpor:F0}/{target.maxTorpor:F0}",
                    usedIcon
                );
                return;
            }

            // Еда → подсказка
            foreach (var pref in data.tamingFoodPreferences)
            {
                if (pref.food == slot.item)
                {
                    NotificationManager.Instance?.Show(
                        "Положите еду — существо съест её само.",
                        slot.item.icon
                    );
                    return;
                }
            }

            NotificationManager.Instance?.Show(
                "Этот предмет нельзя использовать на существе.",
                null
            );
        }

        // вызывается, когда игрок отпустил E
        public void OnUseItemFinished()
        {
            if (_accumulatedFoodCount > 0 && _currentFoodItem != null)
            {
                NotificationManager.Instance?.Show(
                    $"Использовано: {_currentFoodItem.itemName} x{_accumulatedFoodCount}",
                    _currentFoodItem.icon
                );
            }

            _accumulatedFoodCount = 0;
            _currentFoodItem = null;
        }

        private void RemoveOneFromPlayerSlot(SlotOwner owner, int localIndex)
        {
            var progress = PlayerProgress.Instance;
            if (progress == null) return;

            if (owner == SlotOwner.Hotbar)
                progress.hotbarInventoryData.RemoveItemFromSlot(localIndex, 1);
            else
                progress.mainInventoryData.RemoveItemFromSlot(localIndex, 1);
        }
    }
}