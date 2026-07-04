using Assets.Scripts.Items;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    [System.Serializable]
    public class InventorySlot
    {
        public Item item;
        public int count;

        public bool IsEmpty => item == null;
    }
}

