using UnityEngine;
using Assets.Scripts.Items;
using Assets.Scripts.Corpses;
using Assets.Scripts.Audio;
using Assets.Scripts.InventorySystem;

namespace Assets.Scripts.Creatures
{
    public enum CreatureBehaviorType
    {
        Passive,      // Не атакует, убегает
        Aggressive,   // Атакует игрока
        Skittish,     // Убегает при приближении
        Territorial,  // Атакует, если игрок близко
    }

    [CreateAssetMenu(fileName = "Creature_", menuName = "Game/Creature Data")]
    public class CreatureData : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный ID (например, 'Raptor', 'Dodo'). Используется в сохранениях и мультиплеере.")]
        public string creatureId;

        [Tooltip("Имя для UI")]
        public string displayName;

        public Sprite icon;

        [TextArea(2, 4)]
        public string description;

        [Header("Префаб")]
        [Tooltip("Единый префаб существа (используется и для живого, и для трупа)")]
        public GameObject prefab;

        [Tooltip("Уровень существа. Для ARK-стиля — 1..150.")]
        public int level = 1;

        [Header("Статы")]
        public float maxHealth = 100f;
        public float maxStamina = 50f;
        public float walkSpeed = 2f;
        public float chaseSpeed = 5f;

        [Header("Агрессия")]
        public float aggressionRadius = 30f;
        public float attackRange = 2f;
        public float attackDamage = 10f;
        public float attackCooldown = 2f;

        [Header("Блуждание")]
        public float wanderRange = 20f;
        public float minWanderDelay = 2f;
        public float maxWanderDelay = 5f;

        [Header("Поведение")]
        public CreatureBehaviorType behaviorType = CreatureBehaviorType.Aggressive;
        public bool tamable = false;
        public bool tamableKO = false;
        public bool tamablePassive = false;

        [Header("Лут при убийстве (в инвентарь трупа)")]
        public LootEntry[] inventoryLootTable;

        [Header("Лут при разборе (ресурсы)")]
        public Corpse.ResourceDrop[] harvestDrops;
        public int maxHarvestHits = 5;

        [Header("Разрешения для добычи")]
        public bool allowFists = false;
        public bool allowAxe = true;
        public bool allowPickaxe = true;
        public bool allowSword = true;
        public bool allowSickle = false;

        [Header("Звуки")]
        public AudioClip footstepClip;
        [Range(0, 1)] public float footstepVolume = 0.5f;
        public AudioClip attackClip;
        [Range(0, 1)] public float attackVolume = 0.5f;
        public AudioClip takeDamageClip;
        [Range(0, 1)] public float takeDamageVolume = 0.5f;
        public AudioClip deathClip;
        [Range(0, 1)] public float deathVolume = 0.5f;

        [Header("Эффекты")]
        public ParticleSystem damageEffect;
        public ImpactType impactType = ImpactType.Flesh;

        [Header("Ограничения (опционально)")]
        [Tooltip("Если > 0, переопределяет глобальное время жизни трупа. Если -1, используется глобальное.")]
        public float corpseLifetimeOverride = -1f;
    }
}