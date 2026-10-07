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
                    slots[i].currentDurability = (durability == -2f) ? (item.hasDurability ? item.maxDurability : -1f) : durability;
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

        /// <summary>
        /// Возвращает множитель стоимости ремонта (0..1) по доле утраченной прочности.
        /// 0 — предмет целый (ремонт не нужен), 1 — прочность на нуле (ремонт = полная стоимость крафта).
        /// Возвращает 0, если у предмета нет прочности (maxDurability &lt;= 0) или она не отслеживается (currentDurability &lt; 0).
        /// </summary>
        public float GetRepairMultiplier(float maxDurability, float currentDurability)
        {
            if (maxDurability <= 0f) return 0f;
            if (currentDurability >= maxDurability - 0.001f) return 0f;

            float missing = (maxDurability - currentDurability) / maxDurability;
            return Mathf.Clamp01(missing);
        }

        // Единая формула: сколько единиц ресурса нужно на ремонт
        public int GetRepairAmount(int baseAmount, float multiplier)
        {
            if (multiplier <= 0f) return 0;
            return Mathf.Max(1, Mathf.CeilToInt(baseAmount * multiplier));
        }

        // Проверка наличия ресурсов с учетом множителя
        public bool HasIngredientsForRepair(Recipe recipe, float multiplier)
        {
            if (multiplier <= 0f) return true;   // ремонт не нужен — ресурсы не нужны

            foreach (var ing in recipe.ingredients)
            {
                if (ing.item == null || ing.amount <= 0) continue;

                int required = GetRepairAmount(ing.amount, multiplier);
                if (GetTotalCountOfItem(ing.item) < required) return false;
            }
            return true;
        }

        // Потребление ресурсов
        public void ConsumeRepairIngredients(Recipe recipe, float multiplier)
        {
            if (multiplier <= 0f) return;

            foreach (var ing in recipe.ingredients)
            {
                if (ing.item == null || ing.amount <= 0) continue;

                int toRemove = GetRepairAmount(ing.amount, multiplier);
                if (toRemove <= 0) continue;

                RemoveItemAmount(ing.item, toRemove);
            }

            NotifyChanged();
        }

        // Исправленный метод RemoveItemAmount (универсальный для снятия любого количества)
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
            // === 1. Проверка входных данных ===
            if (serializable?.slots == null)
            {
                Debug.LogWarning("[InventoryData] Serialized inventory is null, skipping load.");
                return;
            }

            if (serializable.slots.Length != size)
            {
                Debug.LogWarning($"[InventoryData] Size mismatch: expected {size}, got {serializable.slots.Length}. Skipping load.");
                return;
            }

            if (itemDatabase == null)
            {
                Debug.LogError("[InventoryData] Item database is null! Cannot resolve items.");
                return;
            }

            // === 2. Десериализация ===
            for (int i = 0; i < size; i++)
            {
                var saved = serializable.slots[i];

                // Пустой слот
                if (string.IsNullOrEmpty(saved.itemId))
                {
                    slots[i] = new InventorySlot();
                    continue;
                }

                // Ищем предмет
                if (!itemDatabase.TryGetValue(saved.itemId, out var item))
                {
                    Debug.LogError($"[InventoryData] Item ID '{saved.itemId}' not found in database!");
                    slots[i] = new InventorySlot();
                    continue;
                }

                // === 3. Создаём слот ===
                var newSlot = new InventorySlot
                {
                    item = item,
                    count = saved.count,
                    currentDurability = saved.durability,
                };

                // === 4. Восстановление прочности ===
                if (item.hasDurability)
                {
                    // Если в файле прочность < 0 — ставим максимум
                    if (newSlot.currentDurability < 0)
                    {
                        // Debug.Log($"[InventoryData] Restoring durability for '{item.itemName}': {newSlot.currentDurability} → {item.maxDurability}");
                        newSlot.currentDurability = item.maxDurability;
                    }
                    // Ограничиваем прочность максимумом
                    else if (newSlot.currentDurability > item.maxDurability)
                    {
                        Debug.LogWarning($"[InventoryData] Durability for '{item.itemName}' exceeds max ({newSlot.currentDurability} > {item.maxDurability}). Clamping.");
                        newSlot.currentDurability = item.maxDurability;
                    }
                }
                else
                {
                    // Если у предмета нет прочности — ставим -1
                    newSlot.currentDurability = -1f;
                }

                slots[i] = newSlot;
            }

            // === 5. Уведомляем об изменениях ===
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