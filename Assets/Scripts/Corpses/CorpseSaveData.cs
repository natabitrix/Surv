using System;
using System.Collections.Generic;
using Assets.Scripts.InventorySystem;
using UnityEngine;

namespace Assets.Scripts.Corpses
{
    [System.Serializable]
    public class CorpseSaveData
    {
        public string instanceId;
        public string ownerPlayerId;      // "player_001" или SteamID в мультиплеере
        public string corpseType;          // "PlayerCorpse", "CreatureCorpse"
        public string creatureId;          // для существ: "Raptor", "Dodo"

        // Позиция и поворот
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;

        // Время
        public long creationTimeUtc;       // Unix timestamp (UTC) создания
        public float despawnDuration;      // сколько секунд живет труп
        public bool isLootBag;             // это труп или сумка

        // Инвентарь
        public string inventorySaveKey;                  // ключ для ChestInventory
        public SerializableInventory inventoryData;      // данные инвентаря

        // Добыча (для существ)
        public List<ResourceDropData> harvestDrops = new();
        public List<int> remainingAmounts = new();
        public int harvestHits;
        public bool isDepleted;
    }

    [System.Serializable]
    public class ResourceDropData
    {
        public string itemId;
        public int totalAmount;
    }
}