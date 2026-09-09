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
        public static ChestUI Instance { get; private set; }
        public static ChestUI CurrentOpenChest { get; private set; }

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

        // ✅ Универсальный метод открытия (работает и с сундуками, и с трупами)
        // public void OpenWith(ChestInventory chest, IInteractable source = null)
        // {
        //     // Отписываемся от старого
        //     // if (_currentChest?.Data != null)
        //     // {
        //     //     _currentChest.Data.OnInventoryChanged -= RefreshUI;
        //     // }
        //     // ✅ ВСЕГДА закрываем старый инвентарь перед открытием нового
        //     if (_currentChest != null)
        //     {
        //         Close();  // ← Это гарантирует отписку и очистку
        //     }

        //     _currentChest = chest;
        //     CurrentOpenChest = this;

        //     // Сохраняем источник (может быть ChestController или Corpse)
        //     SourceInteractable = source;
        //     SourceChest = source as ChestController; // Будет null, если это Corpse

        //     // Подписываемся на новый
        //     if (_currentChest?.Data != null)
        //     {
        //         _currentChest.Data.OnInventoryChanged += RefreshUI;
        //     }

        //     CreateChestSlots();
        // }

        // public void Close()
        // {
        //     if (_currentChest?.Data != null)
        //     {
        //         _currentChest.Data.OnInventoryChanged -= RefreshUI;
        //     }

        //     _currentChest = null;
        //     SourceChest = null;
        //     SourceInteractable = null;
        //     CurrentOpenChest = null;
        // }

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

                // Сохраняем item ДО любых изменений слота!
                Item item = slot.item;
                int countToMove = slot.count;
                float originalDurability = slot.currentDurability;

                // Переносим столько, сколько возможно
                int actuallyMoved = progress.AddItemToPlayerInventory(item, countToMove, originalDurability);

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
            int countBefore = slot.count;

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

        // public void RefreshUI()
        // {
        //     // if (_currentChest == null || slotUIs == null) return;
        //     // ✅ Проверяем, что текущий инвентарь все еще действителен
        //     if (_currentChest == null || _currentChest.gameObject == null || slotUIs == null)
        //     {
        //         Close();
        //         return;
        //     }
        //     var slots = _currentChest.Data.slots;
        //     int count = Mathf.Min(slots.Count, slotUIs.Count);

        //     for (int i = 0; i < count; i++)
        //     {
        //         slotUIs[i].SetSlot(slots[i]);
        //     }

        //     _currentChest.Save("ChestUI.RefreshUI");

        // }

        // void OnDestroy()
        // {
        //     if (Instance == this) Instance = null;
        // }





public void OpenWith(ChestInventory chest, IInteractable source = null)
{
    Debug.Log($"[ChestUI] ===== OPEN WITH CALLED =====");
    Debug.Log($"[ChestUI] Новый chest: {(chest != null ? chest.name : "null")}, saveKey: {(chest != null ? chest.saveKey : "null")}");
    Debug.Log($"[ChestUI] Новый source: {(source != null ? source.GetType().Name : "null")}");
    Debug.Log($"[ChestUI] Текущий _currentChest до открытия: {(_currentChest != null ? _currentChest.name : "null")}");
    Debug.Log($"[ChestUI] Текущий SourceInteractable до открытия: {(SourceInteractable != null ? SourceInteractable.GetType().Name : "null")}");
    
    // ✅ ВСЕГДА закрываем старый инвентарь перед открытием нового
    if (_currentChest != null)
    {
        Debug.Log($"[ChestUI] Вызываем Close() для старого инвентаря");
        Close();  // ← Это гарантирует отписку и очистку
    }

    _currentChest = chest;
    CurrentOpenChest = this;

    // Сохраняем источник (может быть ChestController или Corpse)
    SourceInteractable = source;
    SourceChest = source as ChestController; // Будет null, если это Corpse

    Debug.Log($"[ChestUI] После установки: _currentChest = {(_currentChest != null ? _currentChest.name : "null")}");
    Debug.Log($"[ChestUI] После установки: SourceInteractable = {(SourceInteractable != null ? SourceInteractable.GetType().Name : "null")}");

    // Подписываемся на новый
    if (_currentChest?.Data != null)
    {
        Debug.Log($"[ChestUI] Подписываемся на OnInventoryChanged");
        _currentChest.Data.OnInventoryChanged += RefreshUI;
    }

    CreateChestSlots();
    Debug.Log($"[ChestUI] ===== OPEN WITH FINISHED =====");
}

public void Close()
{
    Debug.Log($"[ChestUI] ===== CLOSE CALLED =====");
    Debug.Log($"[ChestUI] Закрываем инвентарь: {(_currentChest != null ? _currentChest.name : "null")}");
    Debug.Log($"[ChestUI] Текущий SourceInteractable: {(SourceInteractable != null ? SourceInteractable.GetType().Name : "null")}");
    
    if (_currentChest?.Data != null)
    {
        Debug.Log($"[ChestUI] Отписываемся от OnInventoryChanged");
        _currentChest.Data.OnInventoryChanged -= RefreshUI;
    }

    _currentChest = null;
    SourceChest = null;
    SourceInteractable = null;
    CurrentOpenChest = null;
    
    Debug.Log($"[ChestUI] После очистки: _currentChest = null, CurrentOpenChest = null");
    Debug.Log($"[ChestUI] ===== CLOSE FINISHED =====");
}

public void RefreshUI()
{
    Debug.Log($"[ChestUI] ===== REFRESH UI CALLED =====");
    Debug.Log($"[ChestUI] _currentChest: {(_currentChest != null ? _currentChest.name : "null")}");
    Debug.Log($"[ChestUI] _currentChest?.gameObject: {(_currentChest?.gameObject != null ? _currentChest.gameObject.name : "null")}");
    
    // ✅ Проверяем, что текущий инвентарь все еще действителен
    if (_currentChest == null || _currentChest.gameObject == null || slotUIs == null)
    {
        Debug.Log($"[ChestUI] Инвентарь уничтожен или null! Вызываем Close()");
        Close();
        return;
    }
    
    var slots = _currentChest.Data.slots;
    int count = Mathf.Min(slots.Count, slotUIs.Count);
    Debug.Log($"[ChestUI] Обновляем UI: {count} слотов");

    for (int i = 0; i < count; i++)
    {
        slotUIs[i].SetSlot(slots[i]);
    }

    _currentChest.Save("ChestUI.RefreshUI");
    Debug.Log($"[ChestUI] ===== REFRESH UI FINISHED =====");
}

void OnDestroy()
{
    Debug.Log($"[ChestUI] ===== ON DESTROY CALLED =====");
    if (Instance == this) Instance = null;
    
    if (_currentChest?.Data != null)
    {
        Debug.Log($"[ChestUI] OnDestroy: отписываемся от OnInventoryChanged");
        _currentChest.Data.OnInventoryChanged -= RefreshUI;
    }
    Debug.Log($"[ChestUI] ===== ON DESTROY FINISHED =====");
}




    }

}