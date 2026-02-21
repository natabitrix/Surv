// Assets/Scripts/InventorySystem/ChestUI.cs
using Assets.Scripts.Core;
using Assets.Scripts.Interactables;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    public class ChestUI : MonoBehaviour
    {
        // === СТАТИЧЕСКАЯ ССЫЛКА НА ОТКРЫТЫЙ СУНДУК ===
        public static ChestUI CurrentOpenChest { get; private set; }

        public InventoryManager InventoryManager;

        public Transform playerSlotParent;
        public Transform chestSlotParent;
        public GameObject slotPrefab;
        public Canvas rootCanvas;
        public RectTransform dragLayer;

        private ChestInventory _currentChest;

        private List<InventorySlotUI> slotUIs;

        // ✅ Прямой доступ к данным инвентаря сундука
        public InventoryData Data => _currentChest?.Data;

        public ChestController SourceChest { get; private set; }

        void CreateChestSlots()
        {
            if (slotUIs != null)
            {
                foreach (Transform t in chestSlotParent)
                    Destroy(t.gameObject);
                slotUIs.Clear();
            }
            else
            {
                slotUIs = new List<InventorySlotUI>();
            }

            if (_currentChest?.Data?.slots == null) return;

            for (int i = 0; i < _currentChest.Data.slots.Count; i++)
            {
                var go = Instantiate(slotPrefab, chestSlotParent);
                var ui = go.GetComponent<InventorySlotUI>();
                ui.SetupChest(i, this);
                ui.SetSlot(GetSlot(i));
                slotUIs.Add(ui);
            }
        }

        public void OpenWith(ChestInventory chest)
        {
            // Отписываемся от старого
            if (_currentChest?.Data != null)
            {
                _currentChest.Data.OnInventoryChanged -= RefreshUI;
            }

            _currentChest = chest;
            SourceChest = chest.GetComponent<ChestController>(); // ← сохраняем
            CurrentOpenChest = this; // ← ЗАПОМИНАЕМ ТЕКУЩИЙ ОТКРЫТЫЙ

            // Подписываемся на новый
            if (_currentChest?.Data != null)
            {
                _currentChest.Data.OnInventoryChanged += RefreshUI;
            }

            CreateChestSlots();
        }

        public void Close()
        {
            if (_currentChest.Data != null)
            {
                _currentChest.Data.OnInventoryChanged -= RefreshUI;
            }

            _currentChest = null;
            CurrentOpenChest = null; // ← ОБНУЛЯЕМ

        }

        public InventorySlot GetSlot(int index)
        {
            if (_currentChest?.Data?.slots != null && index >= 0 && index < _currentChest.Data.slots.Count)
            {
                return _currentChest.Data.slots[index];
            }
            return null;
        }

        // игрок — это не один InventoryData, а инвентарь + хотбар + логика закрепления.
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

                // Сохраняем item ДО любых изменений слота!
                Item item = slot.item;
                int countToMove = slot.count;

                // Переносим столько, сколько возможно
                int actuallyMoved = progress.AddItemToPlayerInventory(item, countToMove);

                if (actuallyMoved > 0)
                {
                    // Удаляем из сундука РЕАЛЬНО перенесённое количество
                    chestData.RemoveItemFromSlot(i, actuallyMoved);

                    // Агрегируем для уведомления (используем сохранённый item!)
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

            // 🔸 Сохраняем данные ДО удаления
            string itemName = slot.item.itemName;
            Sprite icon = slot.item.icon;
            int countBefore = slot.count; // ← важно!

            // Удаляем ВЕСЬ слот
            _currentChest.Data.RemoveItemFromSlot(slotIndex);

            // Показываем уведомление
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

        public void RefreshUI()
        {
            if (_currentChest == null || slotUIs == null) return;

            var slots = _currentChest.Data.slots;
            int count = Mathf.Min(slots.Count, slotUIs.Count);

            for (int i = 0; i < count; i++)
            {
                slotUIs[i].SetSlot(slots[i]);
            }
        }
    }

}