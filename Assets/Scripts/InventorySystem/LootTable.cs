using Assets.Scripts.Items;
using System;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    [Serializable]
    public class LootEntry
    {
        public Item item;
        public int minAmount = 1;
        public int maxAmount = 1;
        [Range(0f, 1f)] public float dropChance = 1f; // Шанс выпадения от 0 до 1
    }
}