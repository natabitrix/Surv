using Assets.Scripts.Core;
using Assets.Scripts.Creatures;
using Assets.Scripts.Creatures.Components;
using Assets.Scripts.Creatures.Taming;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Notifications;
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
        private PlayerEquipment _equipment;
        private PlayerBuildMode _buildMode;
        private ItemUsageSystem _itemUsageSystem;
        private PlayerProgress _progress;

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

        private void Awake()
        {
            _equipment = GetComponent<PlayerEquipment>();
            if (_equipment == null)
                Debug.LogError($"PlayerEquipment is missing on {gameObject.name}!", this);

            _buildMode = GetComponent<PlayerBuildMode>();
            if (_buildMode == null)
                Debug.LogError($"PlayerBuildMode is missing on {gameObject.name}!", this);

            _itemUsageSystem = GetComponent<ItemUsageSystem>();
            if (_itemUsageSystem == null)
                Debug.LogError($"ItemUsageSystem is missing on {gameObject.name}!", this);

            _progress = GetComponent<PlayerProgress>();
            if (_progress == null)
                Debug.LogError($"PlayerProgress is missing on {gameObject.name}!", this);
        }

        // === Игрок: экип / еда / постройка ===
        private void UseFromPlayerInventory(InventorySlot slot, SlotOwner owner, int localIndex, InventorySlotUI ui)
        {
            
            if (_progress == null) return;

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

            _progress.Save("ItemUsageRouter.UseFromPlayerInventory");
        }

        // === Чужой инвентарь: пока только существа ===
        private void UseFromOtherInventory(InventorySlot slot, int localIndex)
        {
            var chestUI = ChestUI.Instance;
            if (chestUI == null) return;

            if (chestUI.SourceInteractable is not CreatureInteractable cInteractable) return;

            cInteractable.TryGetComponent<Creature>(out var creature);

            if (creature == null || creature.Data == null) return;

            var usedItem = slot.item;
            var usedIcon = slot.item.icon;

            // Наркотик → torpor
            var config = creature.Config;

            if (config != null && config.narcoticItem != null && slot.item == config.narcoticItem)
            {

                chestUI.Data.RemoveItemFromSlot(localIndex, 1);
                creature.AddTorpor(config.narcoticTorporAmount);
                if (!string.IsNullOrEmpty(creature.TamingInstanceId))
                {
                    TamingManager.Instance?.UpdateSave(creature.TamingInstanceId, "player_001");
                }

                NotificationManager.Instance?.Show(
                    $"Наркотик: {usedItem.itemName}. Torpor: {creature.torpor:F0}/{creature.maxTorpor:F0}",
                    usedIcon
                );
                return;
            }
            else
            {
                // Еда → отхиливание
                foreach (var pref in creature.Data.tamingFoodPreferences)
                {
                    if (pref.food == slot.item)
                    {
                        chestUI.Data.RemoveItemFromSlot(localIndex, 1);
                        creature.AddFood(1);
                    }
                }
                return;
            }
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
            if (_progress == null) return;

            if (owner == SlotOwner.Hotbar)
                _progress.hotbarInventoryData.RemoveItemFromSlot(localIndex, 1);
            else
                _progress.mainInventoryData.RemoveItemFromSlot(localIndex, 1);
        }
    }
}