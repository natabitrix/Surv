using UnityEngine;
using Assets.Scripts.Items;
using Assets.Scripts.Corpses;
using Assets.Scripts.Audio;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Loot;
using Assets.Scripts.UI.RadialMenuUI;

namespace Assets.Scripts.Creatures
{
    public enum CreatureBehaviorType
    {
        Passive,      // Не атакует, убегает
        Aggressive,   // Атакует игрока
        Skittish,     // Убегает при приближении
        Territorial,  // Атакует, если игрок близко
    }

    [System.Serializable]
    public struct TamingFoodPreference
    {
        public Item food;
        [Tooltip("Сколько процентов прогресса приручения дает одна единица этой еды.")]
        public float progressAmount;
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

        [Header("Приручение")]
        [Tooltip("Массив предпочтений в еде для приручения.")]
        public TamingFoodPreference[] tamingFoodPreferences;

        [Tooltip("Предмет, который считается наркотиком для этого существа.")]
        public Item narcoticItem;

        [Tooltip("Сколько единиц торпора восстанавливает одна единица наркотика.")]
        public float narcoticTorporAmount = 40f;
        [Tooltip("Как быстро просыпается")]
        public float torporRecoveryRate = 1f;

        [Header("Food / Hunger")]
        [Tooltip("Максимальный запас еды.")]
        public float maxFood = 100f;

        [Tooltip("Скорость падения еды в нокауте (единиц/сек). В ARK быстрее, чем у прирученного.")]
        public float foodDrainRateKnockedOut = 0.5f;

        [Tooltip("Скорость падения еды у прирученного (единиц/сек).")]
        public float foodDrainRateTamed = 0.1f;

        [Tooltip("Порог в процентах от максимума (0..1), ниже которого существо ест.")]
        [Range(0f, 1f)] public float foodEatThresholdPercent = 0.5f;

        [Tooltip("Сколько здоровья теряется в секунду, когда food == 0.")]
        public float starvationDamagePerSecond = 1f;

        // === Tamed AI ===
        [Header("Tamed AI (после приручения)")]
        [Tooltip("Радиус, в котором существо ищет врагов (при агрессивном поведении).")]
        public float tamedAggroRadius = 15f;

        [Tooltip("Радиус блуждания (команда 'Блуждать').")]
        public float tamedWanderRadius = 10f;

        [Tooltip("Скорость бега при следовании за игроком (если игрок далеко).")]
        public float tamedFollowChaseSpeed = 5f;

        [Tooltip("Можно ли подобрать существо (для мелких).")]
        public bool canBePickedUp = false;

        [Tooltip("Можно ли оседлать существо.")]
        public bool canBeRidden = false;


    }
}