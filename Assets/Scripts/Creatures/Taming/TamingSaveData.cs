using System.Collections.Generic;
using Assets.Scripts.InventorySystem;
using UnityEngine;

namespace Assets.Scripts.Creatures.Taming
{
    [System.Serializable]
    public class TamingSaveData
    {
        public string instanceId;
        public string creatureId;

        // Состояние
        public bool knockedOut;
        public bool tamed;
        public float torpor;
        public float maxTorpor;
        public float tamingProgress;
        public float health;
        public float food;
        public float stamina;

        // Для расчёта времени
        public float torporRecoveryRate;         // чтобы пересчитать torpor за отсутствие
        public float foodDrainRateKnockedOut;    // чтобы пересчитать food за отсутствие
        public float foodDrainRateTamed;

        // Позиция/поворот
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;

        // Владелец (для прирученных)
        public string ownerPlayerId = "world";
        public string customName = "";

        // === Команды прирученного ===
        public bool tamedFollowing;
        public bool tamedWandering;
        public bool tamedMating;
        public TamedBehavior tamedBehavior = TamedBehavior.Neutral;
        public FollowDistance tamedFollowDistance = FollowDistance.Medium;

        // Время
        public long savedAtTime;
        public float despawnDuration;            // 0 = не деспавнится (прирученные)

        // Инвентарь
        public SerializableInventory inventoryData;
    }
}