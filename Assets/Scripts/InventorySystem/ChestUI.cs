// Assets/Scripts/InventorySystem/ChestUI.cs
using Assets.Scripts.Core;
using Assets.Scripts.Corpses;
using Assets.Scripts.Creatures;
using Assets.Scripts.Interactables;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Notifications;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    public class ChestUI : MonoBehaviour
    {
        public static ChestUI Instance { get; private set; }

        public InventoryManager inventoryManager;
        public Transform playerSlotParent;
        public Transform chestSlotParent;
        public GameObject slotPrefab;
        public Canvas rootCanvas;
        public RectTransform dragLayer;

        private ChestInventory _currentChest;
        private List<InventorySlotUI> slotUIs;

        // ✅ Прямой доступ к данным инвентаря сундука
        public InventoryData Data => _currentChest?.Data;
        public ChestInventory CurrentChest => _currentChest;

        // Поддержка как ChestController, так и Corpse (или любого IInteractable)
        public ChestController SourceChest { get; private set; }
        public IInteractable SourceInteractable { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void CreateChestSlots()
        {
            if (_currentChest?.Data?.slots == null) return;

            int requiredSize = _currentChest.Data.slots.Count;

            if (slotUIs == null) slotUIs = new List<InventorySlotUI>();

            // Удаляем лишние
            while (slotUIs.Count > requiredSize)
            {
                var last = slotUIs[slotUIs.Count - 1];
                slotUIs.RemoveAt(slotUIs.Count - 1);
                Destroy(last.gameObject);
            }

            // Создаем недостающие
            while (slotUIs.Count < requiredSize)
            {
                var go = Instantiate(slotPrefab, chestSlotParent);
                var ui = go.GetComponent<InventorySlotUI>();
                ui.SetupChest(slotUIs.Count, this);
                slotUIs.Add(ui);
            }

            // Обновляем существующие
            for (int i = 0; i < slotUIs.Count; i++)
            {
                slotUIs[i].SetSlot(GetSlot(i));
            }
        }

        public InventorySlot GetSlot(int index)
        {
            if (_currentChest?.Data?.slots != null && index >= 0 && index < _currentChest.Data.slots.Count)
            {
                return _currentChest.Data.slots[index];
            }
            return null;
        }

        // Игрок — это не один InventoryData, а инвентарь + хотбар + логика закрепления.
        public Dictionary<Item, int> MoveAllToPlayer()
        {
            var summary = new Dictionary<Item, int>();

            if (_currentChest?.Data == null)
                return summary;

            var chestData = _currentChest.Data;
            var progress = PlayerProgress.Instance;

            for (int i = chestData.slots.Count - 1; i >= 0; i--)
            {
                var slot = chestData.slots[i];
                if (slot.IsEmpty || slot.item == null) continue;

                Item item = slot.item;
                int countToMove = slot.count;
                float originalDurability = slot.currentDurability;

                int actuallyMoved = progress.AddItemToPlayerInventory(item, countToMove, originalDurability);

                if (actuallyMoved > 0)
                {
                    chestData.RemoveItemFromSlot(i, actuallyMoved);

                    if (summary.ContainsKey(item))
                        summary[item] += actuallyMoved;
                    else
                        summary[item] = actuallyMoved;
                }
            }

            return summary;
        }

        public void RemoveItemFromSlot(int slotIndex)
        {
            if (_currentChest?.Data == null || slotIndex < 0 || slotIndex >= _currentChest.Data.slots.Count)
                return;

            var slot = _currentChest.Data.slots[slotIndex];
            if (slot.IsEmpty || slot.item == null)
                return;

            string itemName = slot.item.itemName;
            Sprite icon = slot.item.icon;
            int countBefore = slot.count;

            _currentChest.Data.RemoveItemFromSlot(slotIndex);

            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.Show(
                    $"Выброшено: {itemName} x{countBefore}",
                    icon
                );
            }
        }

        public void RemoveItemsFromChest()
        {
            for (int i = 0; i < (slotUIs?.Count ?? 0); i++)
            {
                RemoveItemFromSlot(i);
            }
        }

        /// <summary>
        /// Открыть UI с указанным инвентарём.
        /// ⚠️ НЕ вызывать напрямую — используй ChestUIManager.Open().
        /// </summary>
        public void OpenWith(ChestInventory chest, IInteractable source = null)
        {
            if (chest == null)
            {
                Debug.LogError("[ChestUI.OpenWith] chest == null!");
                return;
            }

            // Всегда закрываем старый перед открытием нового
            Close();

            // === ForceSetChest: страховка ===
            _currentChest = chest;
            if (_currentChest != chest)
            {
                Debug.LogError("[ChestUI.OpenWith] _currentChest не установился! Принудительно.");
                _currentChest = chest;
            }

            SourceInteractable = source;
            SourceChest = source as ChestController;

            // Подписываемся на новый
            if (_currentChest?.Data != null)
            {
                _currentChest.Data.OnInventoryChanged += RefreshUI;
            }

            CreateChestSlots();
        }

        /// <summary>
        /// Закрыть UI.
        /// ⚠️ НЕ вызывать напрямую — используй ChestUIManager.Close().
        /// </summary>
        public void Close()
        {

            // 1. Принудительный сброс всех слотов UI
            if (slotUIs != null)
            {
                foreach (var s in slotUIs)
                    s?.SetSlot(null);
            }

            // 2. Отписка от события
            if (_currentChest?.Data != null)
            {
                _currentChest.Data.OnInventoryChanged -= RefreshUI;
            }

            // 3. Сброс ссылок
            _currentChest = null;
            SourceChest = null;
            SourceInteractable = null;
        }

        public void RefreshUI()
        {
            // Проверяем, что текущий инвентарь все еще действителен
            if (_currentChest == null || _currentChest.gameObject == null || slotUIs == null)
            {
                Close();
                return;
            }

            var slots = _currentChest.Data.slots;
            int count = Mathf.Min(slots.Count, slotUIs.Count);

            for (int i = 0; i < count; i++)
            {
                slotUIs[i].SetSlot(slots[i]);
            }

            _currentChest.Save("ChestUI.RefreshUI");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_currentChest?.Data != null)
            {
                _currentChest.Data.OnInventoryChanged -= RefreshUI;
            }
        }
    }
}