using UnityEngine;
using Assets.Scripts.Items;
using Assets.Scripts.Corpses;
using Assets.Scripts.Audio;
using Assets.Scripts.Loot;

namespace Assets.Scripts.Creatures
{
    public enum CreatureBehaviorType { Passive, Aggressive, Skittish, Territorial }

    [System.Serializable]
    public struct TamingFoodPreference
    {
        public Item food;
        public float progressAmount;
    }

    [System.Serializable]
    public class BaseStats
    {
        public int level = 1;
        public float maxHealth = 1000f;
        public float maxStamina = 100f;
        public float maxFood = 100f;
        public float maxOxygen = 100f;
        public float maxWeight = 100f;
        public float maxSpeed = 2f;
        public float maxDamage = 10f;
        public float maxTorpor = 100f;
    }

    [System.Serializable]
    public class CombatProps
    {
        public float aggressionRadius = 30f;
        public float attackRange = 2f;
        public float attackCooldown = 2f;
    }

    // [System.Serializable]
    // public class WildProps
    // {
    // }

    // [System.Serializable]
    // public class TamedProps
    // {

    // }

    [System.Serializable]
    public class RiddenProps
    {
        public float rideAttackCooldown = 0.8f;
    }


    [CreateAssetMenu(fileName = "Creature_", menuName = "Game/Creature Data")]
    public class CreatureData : ScriptableObject
    {
        [Header("Идентификация")]
        public string creatureId;
        public string displayName;
        public Sprite icon;
        // [TextArea(2, 4)] public string description;
        public GameObject prefab;

        public CreatureBehaviorType behaviorType = CreatureBehaviorType.Aggressive;

        [Header("Базовые статы")]
        public BaseStats baseStats = new();

        [Header("Общая боевка")]
        public CombatProps combatProps = new();

        // [Header("Дикие: свойства")]
        // public WildProps wildProps = new();

        // [Header("Прирученные: свойства")]
        // public TamedProps tamedProps = new();

        [Header("Верхом: свойства")]
        public RiddenProps riddenProps = new();

        [Header("Приручение и Еда")]
        public bool tamable = false;
        public bool tamableKO = false;
        public bool tamablePassive = false;

        public TamingFoodPreference[] tamingFoodPreferences;

        public float torporRecoveryRate = 1f;
        public float foodDrainRateKnockedOut = 0.5f;
        public float foodDrainRateTamed = 0.1f;
        [Range(0f, 1f)] public float foodEatThresholdPercent = 0.5f;
        public float starvationDamagePerSecond = 1f;

        [Header("Лут и Добыча")]
        public LootEntry[] inventoryLootTable;
        public Corpse.ResourceDrop[] harvestDrops;
        public int maxHarvestHits = 5;
 
        [Header("Звуки и Эффекты")]
        public AudioClip footstepClip;
        [Range(0, 1)] public float footstepVolume = 0.5f;
        public AudioClip attackClip;
        [Range(0, 1)] public float attackVolume = 0.5f;
        public AudioClip takeDamageClip;
        [Range(0, 1)] public float takeDamageVolume = 0.5f;
        public AudioClip deathClip;
        [Range(0, 1)] public float deathVolume = 0.5f;
        public ParticleSystem damageEffect;
        public ImpactType impactType = ImpactType.Flesh;

        [Header("Ограничения")]
        public bool canBePickedUp = false;
        public bool canBeRidden = false;

    }
}