// Assets/Scripts/InventorySystem/InventoryData.cs
using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Crafting;
using Assets.Scripts.Items;
using Assets.Scripts.UI;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    [System.Serializable]
    public class InventoryData
    {
        public int size;
        public event System.Action OnInventoryChanged;
        public List<InventorySlot> slots;

        public InventoryData(int size)
        {
            this.size = size;
            slots = new List<InventorySlot>();
            slots.Capacity = size; // опционально: резервируем память
            for (int i = 0; i < size; i++)
            {
                slots.Add(new InventorySlot());
            }
        }

        public int AddItemAnywhere(Item item, int amount = 1, float durability = -2f)
        {
            if (item == null || amount <= 0) return 0;

            int remaining = amount;

            // 1. Дособираем в существующие стаки
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (!slot.IsEmpty && slot.item == item && slot.count < item.maxStack)
                {
                    int space = item.maxStack - slot.count;
                    int add = Mathf.Min(space, remaining);
                    slot.count += add;
                    remaining -= add;
                }
            }

            // 2. Заполняем пустые слоты
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (slots[i].IsEmpty)
                {
                    int add = Mathf.Min(item.maxStack, remaining);
                    slots[i].item = item;
                    slots[i].count = add;

                    // Если durability == -2f, берем из item (новое), иначе берем переданное (существующее)
                    slots[i].currentDurability = (durability == -2f) ? (item.itemType == ItemType.Tool ? item.maxDurability : -1f) : durability;

                    remaining -= add;
                }
            }

            int added = amount - remaining;
            if (added > 0)
            {
                NotifyChanged();
            }

            var progress = PlayerProgress.Instance;
            progress.Save("InventoryData.AddItemAnywhere");

            return added;
        }

        // public void MoveOrSwap(int from, int to)
        // {
        //     if (from == to) return;

        //     var slotFrom = slots[from];
        //     var slotTo = slots[to];

        //     // Сохраняем значения из первого слота
        //     var tmpItem = slotFrom.item;
        //     var tmpCount = slotFrom.count;
        //     var tmpDurability = slotFrom.currentDurability; // ОБЯЗАТЕЛЬНО сохраняем прочность

        //     // Переносим данные из второго в первый
        //     slotFrom.item = slotTo.item;
        //     slotFrom.count = slotTo.count;
        //     slotFrom.currentDurability = slotTo.currentDurability; // Переносим прочность

        //     // Переносим сохраненные данные во второй
        //     slotTo.item = tmpItem;
        //     slotTo.count = tmpCount;
        //     slotTo.currentDurability = tmpDurability; // Переносим прочность

        //     Debug.Log("MoveOrSwap");

        //     NotifyChanged();
        // }


        public void ClearSlot(int index)
        {
            var slot = slots[index];
            slot.item = null;
            slot.count = 0;
            slot.currentDurability = -1f; // ГЛАВНЫЙ ФИКС
        }

        // Выбрасывает все из слота
        public void RemoveItemFromSlot(int index)
        {
            if (index >= 0 && index < slots.Count)
            {
                var slot = slots[index];
                if (!slot.IsEmpty)
                {
                    ClearSlot(index);
                    NotifyChanged();
                }
            }

            Debug.Log("RemoveItemFromSlot");
        }

        //overload: Выбрасывает указанное кол-во из слота
        public void RemoveItemFromSlot(int index, int count)
        {
            if (index < 0 || index >= slots.Count) return;
            var slot = slots[index];
            if (slot.IsEmpty) return;

            slot.count = Mathf.Max(0, slot.count - count);
            if (slot.count <= 0)
            {
                ClearSlot(index);
            }
            NotifyChanged();
        }

        // Только перенос между инвентарями, хотбар не затрагивается
        public Dictionary<Item, int> TransferAllTo(InventoryData target)
        {
            var summary = new Dictionary<Item, int>();

            if (target == null || slots == null)
                return summary;

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.item == null)
                    continue;

                Item item = slot.item;
                int originalCount = slot.count;
                float originalDurability = slot.currentDurability;

                // Передаём ВЕСЬ стак за один вызов
                int actuallyMoved = target.AddItemAnywhere(item, originalCount, originalDurability);

                if (actuallyMoved > 0)
                {
                    RemoveItemFromSlot(i, actuallyMoved);

                    // Агрегируем для уведомления (используем сохранённый item!)
                    if (summary.ContainsKey(item))
                        summary[item] += actuallyMoved;
                    else
                        summary[item] = actuallyMoved;

                }
            }

            return summary;
        }


        // Проверяет, достаточно ли ингредиентов в этом инвентаре
        public bool HasIngredients(Recipe recipe)
        {
            if (recipe == null || recipe.ingredients == null) return false;

            // Сначала посчитаем, сколько каждого предмета нужно
            var required = new Dictionary<Item, int>();
            foreach (var ing in recipe.ingredients)
            {
                if (ing.item == null || ing.amount <= 0) continue;
                required[ing.item] = required.GetValueOrDefault(ing.item, 0) + ing.amount;
            }

            // Теперь проверим, хватает ли в слотах
            var available = new Dictionary<Item, int>();
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.item != null)
                {
                    available[slot.item] = available.GetValueOrDefault(slot.item, 0) + slot.count;
                }
            }

            foreach (var kvp in required)
            {
                if (!available.TryGetValue(kvp.Key, out int count) || count < kvp.Value)
                    return false;
            }
            return true;
        }

        // Потребляет ингредиенты из инвентаря (только если их достаточно!)
        public bool ConsumeIngredients(Recipe recipe)
        {
            if (!HasIngredients(recipe)) return false;

            foreach (var ing in recipe.ingredients)
            {
                int remaining = ing.amount;
                // Удаляем по одному, пока не удалим всё
                for (int i = 0; i < slots.Count && remaining > 0; i++)
                {
                    var slot = slots[i];
                    if (!slot.IsEmpty && slot.item == ing.item)
                    {
                        int remove = Mathf.Min(slot.count, remaining);
                        slot.count -= remove;
                        remaining -= remove;
                        if (slot.count <= 0)
                        {
                            slot.item = null;
                            slot.count = 0;
                        }
                        if (remaining <= 0) break;
                    }
                }
            }

            NotifyChanged();
            return true;
        }


        // 1. Проверка наличия ресурсов с учетом множителя
        public bool HasIngredientsForRepair(Recipe recipe, float multiplier)
        {
            foreach (var ing in recipe.ingredients)
            {
                int required = Mathf.CeilToInt(ing.amount * multiplier);
                if (GetTotalCountOfItem(ing.item) < required) return false;
            }
            return true;
        }

        // 2. Потребление ресурсов
        public void ConsumeRepairIngredients(Recipe recipe, float multiplier)
        {
            foreach (var ing in recipe.ingredients)
            {
                int toRemove = Mathf.CeilToInt(ing.amount * multiplier);
                RemoveItemAmount(ing.item, toRemove);
            }
            NotifyChanged();
        }

        // 3. Исправленный метод RemoveItemAmount (универсальный для снятия любого количества)
        private void RemoveItemAmount(Item item, int amount)
        {
            int remaining = amount;
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (!slots[i].IsEmpty && slots[i].item == item)
                {
                    int remove = Mathf.Min(slots[i].count, remaining);
                    slots[i].count -= remove;
                    remaining -= remove;

                    if (slots[i].count <= 0)
                    {
                        ClearSlot(i); // Используем наш метод очистки (с прочностью -1)
                    }
                }
            }
        }
        
        public int GetTotalCountOfItem(Item item)
        {
            if (item == null) return 0;

            int total = 0;
            foreach (var slot in slots)
            {
                // Проверяем: не пуст ли слот и совпадает ли ID предмета (или ссылка)
                if (!slot.IsEmpty && slot.item == item)
                {
                    total += slot.count;
                }
            }
            return total;
        }

        // Методы сохранения/загрузки

        public void FromSerializable(SerializableInventory serializable, Dictionary<string, Item> itemDatabase)
        {
            if (serializable?.slots == null || serializable.slots.Length != size)
            {
                Debug.LogWarning("Несоответствие размера инвентаря при загрузке.");
                return;
            }

            for (int i = 0; i < size; i++)
            {
                var saved = serializable.slots[i];
                // if (saved.itemId == -1)
                if (string.IsNullOrEmpty(saved.itemId))
                {
                    slots[i] = new InventorySlot();
                }
                else if (itemDatabase.TryGetValue(saved.itemId, out var item))
                {
                    slots[i] = new InventorySlot
                    {
                        item = item,
                        count = saved.count,
                        // Если прочность в файле < 0, принудительно ставим макс. прочность
                        currentDurability = (saved.durability < 0 && (item.itemType == ItemType.Tool || item.itemType == ItemType.Weapon))
                        ? item.maxDurability
                        : saved.durability
                    };

                    // ФИКС: Если прочность -1, но предмет — инструмент, починим её
                    if (slots[i].currentDurability < 0 && (item.itemType == ItemType.Tool || item.itemType == ItemType.Weapon))
                    {
                        slots[i].currentDurability = item.maxDurability;
                    }

                }
                else
                {
                    slots[i] = new InventorySlot();
                    Debug.LogError($"Item ID {saved.itemId} не найден!");
                }
            }



            NotifyChanged();
        }

        public SerializableInventory ToSerializable(int size)
        {
            var data = new SerializableInventory
            {
                slots = new SerializableInventorySlot[size]
            };

            for (int i = 0; i < size; i++)
            {
                var slot = slots[i];
                string itemId = slot.item != null ? slot.item.Id : "";
                data.slots[i] = new SerializableInventorySlot
                {
                    itemId = itemId,
                    count = slot.count,
                    durability = slot.currentDurability // СОХРАНЕНИЕ прочности
                };
            }

            return data;
        }

        public void NotifyChanged()
        {
            OnInventoryChanged?.Invoke();

        }
    }

    [System.Serializable]
    public class SerializableInventorySlot
    {
        // public int itemId = -1; // -1 = empty
        public string itemId = "";
        public int count = 0;
        public float durability = -1f;
    }

    [System.Serializable]
    public class SerializableInventory
    {
        public SerializableInventorySlot[] slots;
    }

}