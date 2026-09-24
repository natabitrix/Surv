using UnityEngine;
using Assets.Scripts.Core;
using Assets.Scripts.Audio;
using Assets.Scripts.Player;
using Assets.Scripts.Effects;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Corpses;

namespace Assets.Scripts.Creatures
{
    public abstract class BaseLivingEntity : MonoBehaviour, IInteractable, IImpactSoundProvider
    {
        [Header("Audio")]
        [SerializeField] private ImpactType _impactType = ImpactType.Flesh;
        public virtual ImpactType GetImpactType() => _impactType;

        [Header("ParticleSystem for Damage Effect")]
        public ParticleSystem damageEffect;

        public bool tamed = false;          // прирученное существо
        public bool tamable = false;        // приручаемое существо
        public bool tamableKO = false;      // приручаемое оглушением
        public bool knockedOut = false;     // оглушенное существо
        public bool tamablePassive = false; // приручаемое пассивным (кормлением, другими механиками)

        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        // ==========================================
        // === ИНВЕНТАРЬ (Открытие по F) ===
        // ==========================================
        [SerializeField] private ChestInventory _inventory;
        [SerializeField] private ChestUI _chestUI;
        private bool _isOpen = false;

        [Header("Taming Stats")]
        public float torpor = 0f;
        public float maxTorpor = 100f;
        public float torporRecoveryRate = 1f; // Как быстро просыпается

        // Ссылки на системы
        protected PlayerProgress playerProgress;
        protected PlayerSurvivalSystem survivalSystem;

        // Статы, специфичные для этого существа
        protected float health;
        protected float maxHealth;
        protected float stamina;
        protected float maxStamina;

        // Ссылка на аниматор
        protected Animator animator;
        protected int animIDTakeDamage;
        protected int animIDDeath;

        // События
        public System.Action<BaseLivingEntity> OnDeath;

        protected virtual void Awake()
        {
            // Ищем синглтоны
            playerProgress = PlayerProgress.Instance;
            survivalSystem = PlayerSurvivalSystem.Instance;

            // Получаем аниматор
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                animIDTakeDamage = Animator.StringToHash("TakeDamage");
                animIDDeath = Animator.StringToHash("Death");
            }

            // Инициализация статов
            InitializeStats();
        }

        // ==========================================
        // === ИНТЕРФЕЙС IInteractable ===
        // ==========================================
        public InteractType GetInteractType() => tamed || (tamableKO && knockedOut) ? InteractType.OpenTargetInventory : InteractType.None;
        public InteractType GetInteractType2() => tamable && tamablePassive ? InteractType.Interact : InteractType.None;

        public void Interact(InteractContext context)
        {
            Debug.Log("tamed: " + tamed);
            Debug.Log("tamableKO: " + tamableKO);
            Debug.Log("knockedOut: " + knockedOut);
            Debug.Log("context.isTargetInventory: " + context.isTargetInventory);

            if ((tamed || (tamableKO && knockedOut)) && context.isTargetInventory)
            {
                OpenInventory();
            }
        }

        // ==========================================
        // === ИНВЕНТАРЬ ===
        // ==========================================
        public ChestInventory GetInventory() => _inventory;
        public bool HasInventory() => _inventory != null;
        public bool ShouldDetachAfterInteract() => false;

        public void OpenInventory()
        {
            if (_isOpen) CloseInventory();
            else if (_chestUI != null)
            {
                _chestUI.OpenWith(_inventory);
                _isOpen = true;
            }
            else
            {
                Debug.LogError("[BaseLivingEntity] _chestUI не назначен! Инвентарь не откроется.");
            }
        }

        public void CloseInventory()
        {
            if (_isOpen && _chestUI != null)
            {
                _chestUI.Close();
                _isOpen = false;
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
        }

        // Абстрактные методы, которые должны реализовать наследники
        protected abstract float GetMaxHealthFromConfiguration();
        protected abstract float GetMaxStaminaFromConfiguration();

        // Общий метод получения урона
        // Старая перегрузка — для совместимости (ближний бой, harvest и т.п.)
        public virtual void TakeDamage(float damage, PlayerInteraction playerInteraction)
        {
            TakeDamage(damage, 0f, playerInteraction);
        }

        // Новая — с torpor
        public virtual void TakeDamage(float damage, float torporAmount, PlayerInteraction playerInteraction)
        {
            health -= damage;

            // Накопление torpor
            if (torporAmount > 0f && !_isDead)
            {
                torpor = Mathf.Min(maxTorpor, torpor + torporAmount);

                // Мгновенный нокаут при достижении порога
                if (!knockedOut && torpor >= maxTorpor)
                {
                    KnockOut();
                }
            }

            if (animator != null && damage > 0)
            {
                animator.SetTrigger(animIDTakeDamage);
            }

            if (playerInteraction != null)
            {
                Vector3 targetHitPosition = playerInteraction.GetTargetHitPosition();
                Vector3 targetHitNormal = playerInteraction.GetTargetHitNormal();
                PlayDamageEffect(targetHitPosition);
            }

            Debug.Log($"[{gameObject.name}] Torpor: {torpor:F1} / {maxTorpor};  Damage: {damage:F1};  Health: {health:F1} / {maxHealth}");

            if (health <= 0)
            {
                Die();
            }
        }

        // Система частиц при нанесении урона
        void PlayDamageEffect(Vector3 targetHitPosition)
        {
            if (damageEffect != null)
            {
                var effectInstance = Instantiate(damageEffect, targetHitPosition, transform.rotation);
                effectInstance.Play();

                var main = effectInstance.main;
                Destroy(effectInstance.gameObject, main.duration + 1.5f);
            }
        }

        // Общий метод смерти
        protected virtual void Die()
        {
            if (_isDead) return;
            _isDead = true;
            knockedOut = false;
            health = 0;

            CancelInvoke();

            // === 1. Отключаем NavMeshAgent ===
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) Destroy(agent);

            // === 2. Отключаем Animator (для ragdoll, НЕ удаляем!) ===
            if (animator != null)
            {
                animator.enabled = false;
                // animator.SetTrigger(animIDDeath);
            }


            // === 3. Получаем creatureId из Creature ===
            string creatureId = null;
            Creature creature = GetComponent<Creature>();
            if (creature != null)
            {
                creatureId = creature.creatureId;
                creature.enabled = false;   // ← добавить
            }

            // === 4. Активируем Corpse ===
            var corpse = GetComponent<Corpse>();
            if (corpse != null)
            {
                corpse.enabled = true;
                corpse.ActivateRagdoll();
                corpse.StartCoroutine(corpse.StopMovingRagdoll());
                corpse.InitializeCorpse();

                // === 5. Копируем настройки лута из CreatureData ===
                if (creature != null && creature.Data != null)
                {
                    var data = creature.Data;

                    corpse.harvestDrops = data.harvestDrops;
                    corpse.inventoryLootTable = data.inventoryLootTable;
                    corpse.maxHarvestHits = data.maxHarvestHits;
                    corpse.allowFists = data.allowFists;
                    corpse.allowAxe = data.allowAxe;
                    corpse.allowPickaxe = data.allowPickaxe;
                    corpse.allowSword = data.allowSword;
                    corpse.allowSickle = data.allowSickle;
                }

                // === 6. Создаём инвентарь трупа ===
                corpse.CreateCorpseInventory(
                    "CreatureCorpse",
                    100,
                    FindAnyObjectByType<ChestUI>()
                );

                // === 7. Заполняем инвентарь лутом из inventoryLootTable ===
                corpse.PopulateInventoryFromLootTable();
            }

            // === 8. Активируем RadialMenu ===
            var menu = GetComponent<RadialMenu>();
            if (menu != null) menu.enabled = true;

            // === 9. Регистрируем труп в CorpseManager ===
            if (CorpseManager.Instance != null)
            {
                CorpseManager.Instance.RegisterCorpse(
                    gameObject,
                    "world",
                    creatureId
                );
            }

            // Вызываем событие смерти
            OnDeath?.Invoke(this);
        }

        /// <summary>
        /// Погружает существо в нокаут: AI выключается, аниматор глушится,
        /// открывается доступ к инвентарю (RadialMenu + ChestInventory).
        /// НЕ путать со смертью — это обратимое состояние.
        /// </summary>
        public virtual void KnockOut()
        {
            if (_isDead || knockedOut) return;

            knockedOut = true;
            Debug.Log($"[{gameObject.name}] НОКАУТ (torpor={torpor:F1}/{maxTorpor})");

            // 1. Останавливаем AI
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.isStopped = true;
            if (agent != null)
                agent.enabled = false;

            // 2. Глушим аниматор (пока нет анимации «спит»)
            if (animator != null)
            {
                // animator.SetTrigger(animIDDeath);
                animator.enabled = false;
            }

            // 3. Отключаем Creature (AI-логику)
            // var creature = GetComponent<Creature>();
            // if (creature != null)
            //     creature.enabled = false;

            // 4. Включаем RadialMenu и инвентарь
            var menu = GetComponent<RadialMenu>();
            if (menu != null) menu.enabled = true;

            // 5. Создаём инвентарь, если его нет
            if (_inventory == null)
            {
                var chestInv = gameObject.AddComponent<ChestInventory>();
                string key = $"TamingCorpse_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
                chestInv.Initialize(100, key);

                _inventory = chestInv;
                _chestUI = FindAnyObjectByType<ChestUI>();

                Debug.Log($"[{gameObject.name}] Инвентарь для приручения создан.");
            }

            // === 4. Активируем Corpse ===
            // var corpse = GetComponent<Corpse>();
            // if (corpse != null)
            // {
            //     corpse.enabled = true;

            //     // corpse.ActivateRagdoll();
            //     // corpse.StartCoroutine(corpse.StopMovingRagdoll());

            //     // === Создаём инвентарь ===
            //     corpse.CreateCorpseInventory(
            //         "CreatureCorpse",
            //         100,
            //         FindAnyObjectByType<ChestUI>()
            //     );
            // }

        }

        /// <summary>
        /// Выводит существо из нокаута: включает AI, аниматор, выключает RadialMenu.
        /// </summary>
        public virtual void RecoverFromKnockout()
        {
            if (_isDead || !knockedOut) return;

            knockedOut = false;
            torpor = 0f;
            Debug.Log($"[{gameObject.name}] ВЫШЕЛ ИЗ НОКАУТА");

            // 1. Включаем AI
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
                if (agent.isOnNavMesh)
                    agent.isStopped = false;
                else
                    Debug.LogWarning($"[{gameObject.name}] NavMeshAgent не на NavMesh после нокаута!");
            }

            // 2. Включаем аниматор
            if (animator != null)
                animator.enabled = true;

            // 3. Включаем Creature
            var creature = GetComponent<Creature>();
            if (creature != null)
                creature.enabled = true;

            var corpse = GetComponent<Corpse>();
            if (corpse != null)
                corpse.enabled = false;

            // 4. Выключаем RadialMenu
            var menu = GetComponent<RadialMenu>();
            if (menu != null)
                menu.enabled = false;

            // 5. Закрываем инвентарь
            CloseInventory();
        }

        /// <summary>
        /// Вызывается в конце анимации смерти (через Animation Event).
        /// Выключает коллайдер и аниматор
        /// </summary>
        public void OnDeathAnimationFinished()
        {
            if (animator != null)
            {
                animator.enabled = false;
            }

            var menu = GetComponent<RadialMenu>();
            if (menu != null) menu.enabled = true;

            var corpse = GetComponent<Corpse>();
            if (corpse != null)
            {
                corpse.enabled = true;
                corpse.CreateCorpseInventory(
                    "CreatureCorpse",
                    100,
                    FindAnyObjectByType<ChestUI>()
                );
            }
        }

        // Метод для восстановления здоровья
        public virtual void Heal(float amount)
        {
            health = Mathf.Clamp(health + amount, 0, maxHealth);
        }

        // Геттеры
        protected bool _isDead = false;
        public bool IsDead() => _isDead;
        public float GetHealth() => health;
        public float GetMaxHealth() => maxHealth;
        public float GetStamina() => stamina;
        public float GetMaxStamina() => maxStamina;
        public bool IsAlive() => !_isDead && health > 0;
    }
}