using System;
using Assets.Scripts.InventorySystem;

namespace Assets.Scripts.Loot
{
    [System.Serializable]
    public class LootBagSaveData
    {
        public string instanceId;
        public string ownerPlayerId;      // "player_001" или "world"

        // Позиция и поворот
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;

        // Время
        public long creationTimeUtc;
        public float despawnDuration;

        // Инвентарь
        public string inventorySaveKey;
        public SerializableInventory inventoryData;
    }
}