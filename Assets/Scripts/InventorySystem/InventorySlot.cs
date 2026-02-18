using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    public class InventorySlot
    {
        public Item item;
        public int count;

        public bool IsEmpty => item == null;
    }
}

