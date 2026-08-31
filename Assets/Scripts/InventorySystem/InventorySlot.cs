using Assets.Scripts.Items;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    [System.Serializable]
    public class InventorySlot
    {
        public Item item;
        public int count;
        public float currentDurability = -1f; // -1 значит "нет прочности"

        public bool IsEmpty => item == null;

        // Метод для инициализации прочности при первом получении предмета
        public void InitializeDurability(Item newItem)
        {
            if (newItem != null && newItem.itemType == ItemType.Tool) // Или другое условие
            {
                currentDurability = 100f; // Начальная прочность
            }
        }

        public void SyncDurability()
        {
            if (item != null && (item.itemType == ItemType.Tool || item.itemType == ItemType.Weapon))
            {
                if (currentDurability < 0) currentDurability = item.maxDurability;
            }
        }

        
    }
}

