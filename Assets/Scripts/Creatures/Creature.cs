using UnityEngine;
using UnityEngine.AI;
using Assets.Scripts.Core;
using Assets.Scripts.Items;
using Assets.Scripts.Player.Data;
using Assets.Scripts.Interactables;
using Assets.Scripts.Utils;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.UI;
using Assets.Scripts.Creatures.Taming;
using Assets.Scripts.Creatures.Components;
using Assets.Scripts.Player;

namespace Assets.Scripts.Creatures
{
    public class Creature : MonoBehaviour
    {
        [Header("Database")]
        [Tooltip("Ссылка на базу данных существ")]
        public CreatureDatabase creatureDatabase;

        [Tooltip("ID существа (например, 'Raptor', 'Dodo')")]
        public string creatureId;

        // --- Перенесено из BaseLivingEntity ---
        [Header("Taming Stats")]
        public float torpor = 0f;
        public float maxTorpor = 100f;

        [Header("Food")]
        public float food = 100f;
        public float maxFood = 100f;

        public bool tamed = false;
        public bool tamable = false;
        public bool tamableKO = false;
        public bool knockedOut = false;
        public bool tamablePassive = false;
        public float tamingProgress = 0f;
        public string TamingInstanceId { get; set; } = "";

        // Команды прирученного
        public bool tamedFollowing = false;
        public bool tamedWandering = false;
        public bool tamedMating = false;
        public TamedBehavior tamedBehavior = TamedBehavior.Neutral;
        public FollowDistance tamedFollowDistance = FollowDistance.Medium;

        protected float health;
        protected float maxHealth;
        protected float stamina;
        protected float maxStamina;

        protected bool _isDead = false;

        // Ссылки на системы (если они нужны в Creature)
        protected PlayerController playerController;
        protected PlayerProgress playerProgress;
        protected PlayerSurvivalSystem survivalSystem;

        // Аниматор (если он был в базе)
        protected Animator animator;
        protected int animIDIsMoving;
        protected int animIDSpeed;
        protected int animIDAttack;
        protected int animIDTakeDamage;
        protected int animIDDeath;

        [Header("ParticleSystem for Damage Effect")]
        public ParticleSystem damageEffect;

        // Событие
        public System.Action<Creature> OnDeath; // Раньше было Action<BaseLivingEntity>, меняем на Creature


        public bool IsDead() => _isDead;
        public float GetHealth() => health;
        public float GetMaxHealth() => maxHealth;
        public float GetStamina() => stamina;
        public float GetMaxStamina() => maxStamina;
        public float GetFood() => food;
        public float GetMaxFood() => maxFood;
        public bool IsAlive() => !_isDead && health > 0;

        public void SetHealth(float value) => health = Mathf.Clamp(value, 0f, maxHealth);
        public void SetFood(float value) => food = Mathf.Clamp(value, 0f, maxFood);
        public void SetStamina(float value) => stamina = Mathf.Clamp(value, 0f, maxStamina);
        public void ReduceHealth(float amount) => health -= amount;
        public bool IsHealthBelowZero() => health <= 0;

        public void SetDead() { _isDead = true; knockedOut = false; health = 0; }

        public void Heal(float amount)
        {
            health = Mathf.Clamp(health + amount, 0, maxHealth);
        }

        public void SetCreatureLayer() // Был в BaseLivingEntity
        {
            int creatureLayer = LayerMask.NameToLayer("Creature");
            if (creatureLayer != -1) SetLayerRecursively(gameObject, creatureLayer);
            else Debug.LogWarning("[Creature] Слой 'Creature' не найден!");
        }

        protected void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        // Статы
        // protected virtual void InitializeStats()
        // {
        //     maxHealth = GetMaxHealthFromConfiguration();
        //     health = maxHealth;

        //     maxStamina = GetMaxStaminaFromConfiguration();
        //     stamina = maxStamina;

        //     maxFood = GetMaxFoodFromConfiguration();
        //     food = maxFood;
        // }

        public void TamedFromLoad()
        {
            tamed = true;
            knockedOut = false;
            SetCreatureLayer();
            if (TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;
        }

        // Кэшированная ссылка на данные
        private CreatureData _data;
        public CreatureData Data => _data;

        [Header("Components")]
        private CreatureCombat _combat;
        private CreatureDeath _death;
        private CreatureKnockout _knockout;
        private CreatureTaming _taming;
        private CreatureCommands _commands;
        private CreatureAudio _audio;
        private CreatureWildAI _wildAI;
        private CreatureTamedAI _tamedAI;
        private CreatureRiding _riding;
        private CreatureInteractable _interactable;

        // === Статы (берутся из _data через свойства) ===
        public int Level => _data?.level ?? 1;
        public float WanderRange => _data?.wanderRange ?? 20f;
        public float MinWanderDelay => _data?.minWanderDelay ?? 2f;
        public float MaxWanderDelay => _data?.maxWanderDelay ?? 5f;
        public float WalkSpeed => _data?.walkSpeed ?? 2f;
        public float ChaseSpeed => _data?.chaseSpeed ?? 5f;
        public float AggressionRadius => _data?.aggressionRadius ?? 30f;
        public float AttackRange => _data?.attackRange ?? 2f;
        public float AttackDamage => _data?.attackDamage ?? 10f;
        public float AttackCooldown => _data?.attackCooldown ?? 2f;

        public float RideSpeed => _data?.rideSpeed ?? 4f;
        public float RideSprintSpeed => _data?.rideSprintSpeed ?? 8f;
        public float TurnSpeed => _data?.turnSpeed ?? 3f;

        private NavMeshAgent _agent;
        private Transform _playerTransform;

        public void RequestDeath() => Die();
        public void Die()
        {
            if (_death != null)
            {
                _audio?.PlayDeathSound();
                _death.Die();
            }
        }

        public bool DieManually = false;

        // === Геттеры ===
        public CreatureCombat Combat => _combat;
        public CreatureDeath Death => _death;
        public CreatureKnockout Knockout => _knockout;
        public CreatureTaming Taming => _taming;
        public CreatureCommands Commands => _commands;
        public CreatureAudio Audio => _audio;
        public CreatureWildAI WildAI => _wildAI;
        public CreatureTamedAI TamedAI => _tamedAI;
        public CreatureRiding Riding => _riding;
        public CreatureInteractable Interactable => _interactable;

        public NavMeshAgent Agent => _agent;
        public Transform PlayerTransform => _playerTransform;

        public CreatureBehaviorType BehaviorType => _data?.behaviorType ?? CreatureBehaviorType.Aggressive;

        public Animator Animator => animator;
        public int AnimIDAttack => animIDAttack;
        public int AnimIDTakeDamage => animIDTakeDamage;
        public int AnimIDIsMoving => animIDIsMoving;
        public int AnimIDSpeed => animIDSpeed;
        public int AnimIDDeath => animIDDeath;

        public float Health { get => health; set => health = value; }


        private void Awake()
        {

            // base.Awake();

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



            // === Загружаем данные из базы ===
            if (creatureDatabase == null)
            {
                Debug.LogError($"[Creature] creatureDatabase не назначена на {gameObject.name}!");
                return;
            }

            if (string.IsNullOrEmpty(creatureId))
            {
                Debug.LogError($"[Creature] creatureId не задан на {gameObject.name}!");
                return;
            }

            _data = creatureDatabase.GetCreature(creatureId);
            if (_data == null)
            {
                Debug.LogError($"[Creature] Существо '{creatureId}' не найдено в базе!");
                return;
            }

            _combat = GetComponent<CreatureCombat>();
            if (_combat == null) Debug.LogError($"[Creature] Нет CreatureCombat на {gameObject.name}!");

            _death = GetComponent<CreatureDeath>();
            if (_death == null) Debug.LogError($"[Creature] Нет CreatureDeath на {gameObject.name}!");

            _knockout = GetComponent<CreatureKnockout>();
            if (_knockout == null) Debug.LogError($"[Creature] Нет CreatureKnockout на {gameObject.name}!");

            _taming = GetComponent<CreatureTaming>();
            if (_taming == null) Debug.LogError($"[Creature] Нет CreatureTaming на {gameObject.name}!");

            _commands = GetComponent<CreatureCommands>();
            if (_commands == null) Debug.LogError($"[Creature] Нет CreatureCommands на {gameObject.name}!");

            _audio = GetComponent<CreatureAudio>();
            if (_audio == null) Debug.LogError($"[Creature] Нет CreatureAudio на {gameObject.name}!");

            _wildAI = GetComponent<CreatureWildAI>();
            if (_wildAI == null) Debug.LogError($"[Creature] Нет CreatureWildAI на {gameObject.name}!");

            _tamedAI = GetComponent<CreatureTamedAI>();
            if (_tamedAI == null) Debug.LogError($"[Creature] Нет CreatureTamedAI на {gameObject.name}!");

            _riding = GetComponent<CreatureRiding>();
            if (_riding == null) Debug.LogError($"[Creature] Нет CreatureRiding на {gameObject.name}!");

            _interactable = GetComponent<CreatureInteractable>();
            if (_interactable == null) Debug.LogError($"[Creature] Нет CreatureInteractable на {gameObject.name}!");

            // === Применяем поведение ===
            tamable = _data.tamable;
            tamableKO = _data.tamableKO;
            tamablePassive = _data.tamablePassive;

            // === Инициализация звуков ===
            _audio.Initialize(_data);

            // === Применяем эффекты ===
            damageEffect = _data.damageEffect;

            // === Применяем статы здоровья ===
            maxHealth = _data.maxHealth;
            health = maxHealth;
            maxStamina = _data.maxStamina;
            stamina = maxStamina;
            maxFood = _data.maxFood;
            food = maxFood;

            // === Инициализация компонентов ===
            _agent = GetComponent<NavMeshAgent>();

            if (playerController != null)
            {
                _playerTransform = playerController.transform;
            }
            else if (_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerTransform = player.transform;
            }
        }

        void Update()
        {
            if (!IsAlive()) return;
            if (IsSinglePlayerLoading()) return;

            // === Torpor: убывает всегда, даже в нокауте ===
            if (torpor > 0f)
            {
                torpor = Mathf.Max(0f, torpor - Data.torporRecoveryRate * Time.deltaTime);
            }

            // Food + Taming save — через CreatureTaming
            _taming.Tick();

            // === Нокаут: проверяем выход ===
            // Вход происходит в TakeDamage(), здесь только выход.
            if (knockedOut)
            {
                // Просыпается, если torpor 0 и не приручен
                if (torpor <= 0f && !tamed)
                {
                    RecoverFromKnockout();
                    // Сбрасываем прогресс, если не приручен
                    tamingProgress = 0f;
                }

                return; // пока в нокауте — AI не работает
            }

            // === Прирученное существо ===
            if (tamed)
            {
                // === Верхом — AI не работает ===
                if (_riding != null && _riding.IsRidden)
                {
                    if (playerController != null)
                        _riding.TickRidden(playerController.MoveInput, playerController.LookInput, playerController.SprintInput, false);
                    UpdateAnimation();
                    return;

                }

                if (_tamedAI != null) _tamedAI.Tick();
                UpdateAnimation();
                return;
            }

            // === Дикий AI — через CreatureWildAI ===
            if (_wildAI != null) _wildAI.Tick();
            UpdateAnimation();

            if (DieManually) Die();
        }

        public void SetTarget(Transform target)
        {
            _playerTransform = target;
        }

        public string GetDisplayName()
        {
            if (Data != null && !string.IsNullOrEmpty(Data.displayName))
                return Data.displayName;
            return !string.IsNullOrEmpty(creatureId) ? creatureId : gameObject.name;
        }

        public float GetDamageMultiplier(Collider hitCollider)
        {
            if (hitCollider == null) return 1f;

            var zone = hitCollider.GetComponent<CreatureHitZone>();
            if (zone != null)
                return zone.damageMultiplier;

            return 1f;
        }

        private bool IsSinglePlayerLoading()
        {
            return !SessionMode.IsMultiplayer && LoadingScreenManager.Instance.IsLoading;
        }

        // ==========================================
        // === УРОН ===
        // ==========================================

        public void TakeDamage(float damage, float torporAmount = 0f)
        {
            if (_combat == null) return;
            _combat.TakeDamage(damage, torporAmount);
        }

        public void TakeDamage(float damage, PlayerInteraction pi)
        {
            if (_combat == null) return;
            _combat.TakeDamage(damage, pi);
        }

        public void TakeDamage(float damage, float torporAmount, PlayerInteraction pi)
        {
            if (_combat == null) return;
            _combat.TakeDamage(damage, torporAmount, pi);
        }

        public void TakeDamage(float damage, float torporAmount, PlayerInteraction pi, Collider hit, Vector3? point)
        {
            if (_combat == null) return;
            _combat.TakeDamage(damage, torporAmount, pi, hit, point);
        }

        // ==========================================
        // === НОКАУТ ===
        // ==========================================

        public void KnockOut()
        {
            if (_knockout != null) _knockout.KnockOut();
        }

        public void KnockOutFromLoad()
        {
            if (_knockout != null) _knockout.KnockOutFromLoad();
        }

        public void RecoverFromKnockout()
        {
            if (_knockout != null) _knockout.RecoverFromKnockout();
        }

        // ==========================================
        // === ПРИРУЧЕНИЕ ===
        // ==========================================

        public bool AddTamingProgress(float amount) => _taming != null && _taming.AddTamingProgress(amount);
        public void AddTorpor(float amount) => _taming?.AddTorpor(amount);
        public void AddFood(float amount) => _taming?.AddFood(amount);
        public bool ConsumeFood(float amount) => _taming != null && _taming.ConsumeFood(amount);
        public void Unclaim() => _taming?.Unclaim();

        // ==========================================
        // === КОМАНДЫ ===
        // ==========================================

        public void SetTamedFollowing(bool value) => _commands?.SetTamedFollowing(value);
        public void SetTamedWandering(bool value) => _commands?.SetTamedWandering(value);
        public void SetTamedMating(bool value) => _commands?.SetTamedMating(value);
        public void SetTamedBehavior(TamedBehavior value) => _commands?.SetTamedBehavior(value);
        public void SetFollowDistance(FollowDistance value) => _commands?.SetFollowDistance(value);

        public void ExecuteRadialAction(string actionId) => _commands?.ExecuteRadialAction(actionId);
        public string GetRadialRadioValue(string radioGroupId) => _commands?.GetRadialRadioValue(radioGroupId);
        public void SetRadialRadioValue(string radioGroupId, string radioValue) => _commands?.SetRadialRadioValue(radioGroupId, radioValue);
        public string GetRadialDynamicLabel(string actionId) => _commands?.GetRadialDynamicLabel(actionId);
        public string GetBehaviorText() => _commands != null ? _commands.TamedBehaviorToText() : "Wild";


        private void UpdateAnimation()
        {
            if (animator == null) return;
            if (_combat != null && _combat.IsAttacking) return;
            if (_agent == null) return;

            float currentSpeed = _agent.velocity.magnitude;
            animator.SetFloat(animIDSpeed, currentSpeed);
            animator.SetBool(animIDIsMoving, currentSpeed > 0.1f);
        }

        protected float GetMaxHealthFromConfiguration()
        {
            return _data?.maxHealth ?? 100f;
        }

        protected float GetMaxStaminaFromConfiguration()
        {
            return _data?.maxStamina ?? 50f;
        }

        protected float GetMaxFoodFromConfiguration()
        {
            return _data?.maxFood ?? 100f;
        }


    }

}