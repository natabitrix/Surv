using UnityEngine;
using Assets.Scripts.Core;
using Assets.Scripts.Audio;
using Assets.Scripts.Player;
using Assets.Scripts.Effects;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Corpses;
using Assets.Scripts.UI;
using System.Collections;
using UnityEngine.AI;
using Assets.Scripts.Creatures.Taming;
using Assets.Scripts.UI.Notifications;
using Assets.Scripts.UI.RadialMenuUI;

namespace Assets.Scripts.Creatures
{
    public abstract class BaseLivingEntity : MonoBehaviour, IInteractable, IImpactSoundProvider, IInventorySource
    {
        [Header("Audio")]
        [SerializeField] private ImpactType _impactType = ImpactType.Flesh;
        public virtual ImpactType GetImpactType() => _impactType;

        [Header("ParticleSystem for Damage Effect")]
        public ParticleSystem damageEffect;

        [Header("Taming")]
        public bool tamed = false;
        public bool tamable = false;
        public bool tamableKO = false;
        public bool knockedOut = false;
        public bool tamablePassive = false;
        public float tamingProgress = 0f;

        // === Команды прирученного ===
        [Header("Tamed Commands")]
        [Tooltip("Существо следует за игроком.")]
        public bool tamedFollowing = false;

        [Tooltip("Существо бродит в радиусе.")]
        public bool tamedWandering = false;

        [Tooltip("Существо может спариваться.")]
        public bool tamedMating = false;

        [Tooltip("Отношение к врагам.")]
        public TamedBehavior tamedBehavior = TamedBehavior.Neutral;

        [Tooltip("Дистанция следования за игроком.")]
        public FollowDistance tamedFollowDistance = FollowDistance.Medium;

        public string TamingInstanceId { get; set; } = "";

        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        public virtual string GetDisplayName() => gameObject.name;

        public virtual float GetDamageMultiplier(Collider hitCollider) => 1f;

        // ==========================================
        // === ИНВЕНТАРЬ ===
        // ==========================================
        [SerializeField] private ChestInventory _inventory;
        private bool _isOpen = false;

        // [Header("Debug")]
        // [SerializeField] private bool _debugDamage = false;

        [Header("Taming Stats")]
        public float torpor = 0f;
        public float maxTorpor = 100f;

        [Header("Food")]
        public float food = 100f;
        public float maxFood = 100f;

        public virtual float GetMaxFood() => maxFood;
        public float GetFood() => food;

        // Ссылки на системы
        protected PlayerController playerController;
        protected PlayerProgress playerProgress;
        protected PlayerSurvivalSystem survivalSystem;
        protected RagdollController ragdollController;

        // Статы, специфичные для этого существа
        protected float health;
        protected float maxHealth;
        protected float stamina;
        protected float maxStamina;

        // Ссылка на аниматор
        protected Animator animator;
        protected int animIDIsMoving;
        protected int animIDSpeed;
        protected int animIDAttack;
        protected int animIDTakeDamage;
        protected int animIDDeath;

        // События
        public System.Action<BaseLivingEntity> OnDeath;

        protected virtual void Awake()
        {
            playerProgress = PlayerProgress.Instance;
            survivalSystem = PlayerSurvivalSystem.Instance;

            if (playerProgress != null && playerProgress.playerController != null)
                playerController = playerProgress.playerController;

            animator = GetComponent<Animator>();
            if (animator != null)
            {
                animIDIsMoving = Animator.StringToHash("IsMoving");
                animIDSpeed = Animator.StringToHash("Speed");
                animIDAttack = Animator.StringToHash("Attack");
                animIDTakeDamage = Animator.StringToHash("TakeDamage");
                animIDDeath = Animator.StringToHash("Death");
            }

            ragdollController = GetComponent<RagdollController>();

            InitializeStats();
        }

        // ==========================================
        // === ИНТЕРФЕЙС IInteractable ===
        // ==========================================
        // public InteractType GetInteractType() => tamed || (tamableKO && knockedOut) ? InteractType.OpenTargetInventory : InteractType.None;
        // public InteractType GetInteractType2() => tamable && tamablePassive ? InteractType.Interact : InteractType.None;
        // public InteractType GetInteractType2() =>
        //     (tamable && tamablePassive) || (tamed && Data != null && Data.canBeRidden)
        //     ? InteractType.Interact
        //     : InteractType.None;

        public abstract InteractType GetInteractType();
        public abstract InteractType GetInteractType2();
        public void Interact(InteractContext context)
        {
            if ((tamed || (tamableKO && knockedOut)) && context.isTargetInventory)
            {
                OpenInventory();
            }
        }

        // ==========================================
        // === IInventorySource ===
        // ==========================================
        public ChestInventory GetInventory() => _inventory;
        public bool HasInventory() => _inventory != null;
        public bool ShouldDetachAfterInteract() => false;

        public void OnInventoryOpened()
        {
            _isOpen = true;
        }

        public void OnInventoryClosed()
        {
            _isOpen = false;
        }

        public void OpenInventory()
        {
            if (ChestUIManager.Instance == null)
            {
                Debug.LogError("[BaseLivingEntity] ChestUIManager не найден!");
                return;
            }

            ChestUIManager.Instance.Toggle(this);
        }

        public void CloseInventory()
        {
            if (_isOpen && ChestUIManager.Instance != null)
            {
                ChestUIManager.Instance.Close();
            }
        }

        public void SetInventory(ChestInventory inventory)
        {
            _inventory = inventory;
        }

        // Статы
        protected virtual void InitializeStats()
        {
            maxHealth = GetMaxHealthFromConfiguration();
            health = maxHealth;

            maxStamina = GetMaxStaminaFromConfiguration();
            stamina = maxStamina;

            maxFood = GetMaxFoodFromConfiguration();
            food = maxFood;
        }

        protected abstract float GetMaxHealthFromConfiguration();
        protected abstract float GetMaxStaminaFromConfiguration();
        protected abstract float GetMaxFoodFromConfiguration();

        // ==========================================
        // === НОКАУТ (виртуальные обёртки) ===
        // ==========================================

        public virtual void KnockOut() { }
        public virtual void KnockOutFromLoad() { }
        public virtual void RecoverFromKnockout() { }

        public virtual void Heal(float amount)
        {
            health = Mathf.Clamp(health + amount, 0, maxHealth);
        }

        // ==========================================
        // === ПРИРУЧЕНИЕ (виртуальные обёртки) ===
        // ==========================================

        public virtual bool AddTamingProgress(float amount) => false;
        public virtual void AddTorpor(float amount) { }
        public virtual void AddFood(float amount) { }
        public virtual bool ConsumeFood(float amount) => false;
        public virtual void Unclaim() { }

        public virtual void TamedFromLoad()
        {
            tamed = true;
            knockedOut = false;
            SetCreatureLayer();
            if (TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;
        }

        // ==========================================
        // === КОМАНДЫ И РАДИАЛЬНОЕ МЕНЮ (виртуальные обёртки) ===
        // ==========================================

        public virtual void SetTamedFollowing(bool value) { }
        public virtual void SetTamedWandering(bool value) { }
        public virtual void SetTamedMating(bool value) { }
        public virtual void SetTamedBehavior(TamedBehavior value) { }
        public virtual void SetFollowDistance(FollowDistance value) { }

        public virtual void ExecuteRadialAction(string actionId) { }
        public virtual string GetRadialRadioValue(string radioGroupId) => null;
        public virtual void SetRadialRadioValue(string radioGroupId, string radioValue) { }
        public virtual string GetRadialDynamicLabel(string actionId) => null;
        public virtual string GetBehaviorText() => "Wild";


        public void SetCreatureLayer()
        {
            int creatureLayer = LayerMask.NameToLayer("Creature");
            if (creatureLayer != -1) SetLayerRecursively(gameObject, creatureLayer);
            else Debug.LogWarning("[BaseLivingEntity] Слой 'Creature' не найден!");
        }

        protected void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        protected virtual void OnDestroy()
        {
            // Если этот источник был открыт — закрываем UI
            if (ChestUIManager.Instance != null && ReferenceEquals(ChestUIManager.Instance.CurrentSource, this))
            {
                ChestUIManager.Instance.Close();
            }
        }


        protected bool _isDead = false;
        public bool IsDead() => _isDead;
        public float GetHealth() => health;
        public float GetMaxHealth() => maxHealth;
        public float GetStamina() => stamina;
        public float GetMaxStamina() => maxStamina;
        public bool IsAlive() => !_isDead && health > 0;

        public void SetHealth(float value) => health = Mathf.Clamp(value, 0f, maxHealth);
        public void SetFood(float value) => food = Mathf.Clamp(value, 0f, maxFood);
        public void SetStamina(float value) => stamina = Mathf.Clamp(value, 0f, maxStamina);

        /// <summary>Прямое уменьшение здоровья (без анимации/попапа). Используется CreatureCombat.</summary>
        public void ReduceHealth(float amount) => health -= amount;

        /// <summary>Здоровье ниже нуля?</summary>
        public bool IsHealthBelowZero() => health <= 0;

        /// <summary>Установить dead-флаг (используется CreatureDeath).</summary>
        public void SetDead() { _isDead = true; knockedOut = false; health = 0; }

    }
}