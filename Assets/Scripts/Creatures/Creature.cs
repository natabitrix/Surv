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
using Assets.Scripts.Corpses;
using System.Collections;

// === СОЗДАНИЕ СУЩЕСТВА ===
// 1. Импортировать модель с анимациями и материалами, создать Humanoid или Generic аватар из модели
// 2. Перетащить модель на сцену, распаковать префаб модели
// 3. На корень повесить компоненты Animator, NavMeshAgent, Creature
// 4. Дочерние объекты: Меш, Скелет
// 5. Создать материал с шейдером DamageRobotTexture (создать дамаженные текстуры, создать нормали из текстур)
// 6. Скопировать клипы в папку префаба, в Idle, Walk-Run клипах установить LoopTime
// 7. Скопировать AnimationController из другого существа, поменять клипы
// 8. В Animator назначить AnimationController и созданный аватар
// 9. В анимациях установить события:
// 		на атаку: DealDamageToTarget(), SoundOnAttack()
// 		на урон: SoundOnTakeDamage()
// 		на смерть: SoundOnDeath()
// 		на бег, ходьбу: SoundOnFootstep()
// 10. Создать RagDoll на костях GameObject -> 3D Object -> Ragdoll... (Rigidbody, Collider, CharacterJoint)
// 11. Создать кость DragRigidbody (повесить Rigidbody и CharacterJoint)
// 12. Заполнить в Creature поля 
// 		CreatureId - такой же как в CreatureDatа
// 		CorpseDragRigidbody - часть тела за которую тащить труп (нога, рука)
// 		InteractionCollider - рекомендуется Body или создать дочерний
// 		CreatureRidingRiderAttachPoint - точка оседлания, дочерний трансформ к шее, спине
// 		RagdollParts - кости на которых есть Rigidbody
// 13. Запаковать префаб, перетащить в папку
// 14. Создать ассет CreatureDatа, заполнить данные, CreatureId - такой же как в Creatur, ссылку на префаб
// 15. Добавить ассет CreatureDatа в ассет _CreatureDatabase

namespace Assets.Scripts.Creatures
{
    public class Creature : MonoBehaviour
    {

        private CreatureDatabase _creatureDatabase;

        [Tooltip("ID существа в базе (например, 'Robot_Mike')")]
        public string creatureId;

        [Header("Taming Stats")]
        public float torpor = 0f;
        public float maxTorpor = 100f;

        [Header("Food")]
        public float food = 100f;
        public float maxFood = 100f;

        public bool Tamable => _data != null && _data.tamable;
        public bool TamableKO => _data != null && _data.tamableKO;
        public bool TamablePassive => _data != null && _data.tamablePassive;

        public bool tamed = false;
        public bool knockedOut = false;

        public float tamingProgress = 0f;
        public string TamingInstanceId { get; set; } = "";

        // Команды прирученного
        public bool isFollowing = false;
        public bool isWandering = false;
        public bool isMating = false;
        public TamedBehavior tamedBehavior = TamedBehavior.Neutral;
        public FollowDistance tamedFollowDistance = FollowDistance.Medium;


        [Header("Corpse Drag Target")]
        public Rigidbody CorpseDragRigidbody;
        // public Transform CorpseCenterBone;

        [Header("Corpse/CreatureInteractable Interaction")]
        public Collider InteractionCollider;

        // [Header("Corpse Interaction Anchor (для стабильного рейкаста)")]
        // public Transform CorpseInteractionAnchor;

        [Header("CreatureRiding Riding Points")]
        public Transform CreatureRidingRiderAttachPoint;
        public Vector3 CreatureRidingDismountOffset = new Vector3(2f, 0f, 0f);

        [Header("RagdollController Ragdoll Settings")]
        public RagdollSettings RagdollControllerRagdollSettings;

        protected float health;
        protected float maxHealth;
        protected float stamina;
        protected float maxStamina;

        protected bool _isDead = false;

        // Ссылки на системы (если они нужны в Creature)
        // protected PlayerController playerController;
        // protected PlayerProgress playerProgress;
        // protected PlayerSurvivalSystem survivalSystem;

        protected PlayerController playerController;

        // ИЗМЕНЕНО (Шаг 3.5b): ленивые ссылки — Creature.Awake не должен зависеть от LocalPlayer
        private PlayerProgress _playerProgress;
        protected PlayerProgress playerProgress
        {
            get
            {
                if (_playerProgress == null)
                {
                    var lp = PlayerController.LocalPlayer;
                    if (lp != null) _playerProgress = lp.GetComponent<PlayerProgress>();
                }
                return _playerProgress;
            }
        }

        private PlayerSurvivalSystem _survivalSystem;
        protected PlayerSurvivalSystem survivalSystem
        {
            get
            {
                if (_survivalSystem == null)
                {
                    var lp = PlayerController.LocalPlayer;
                    if (lp != null) _survivalSystem = lp.GetComponent<PlayerSurvivalSystem>();
                }
                return _survivalSystem;
            }
        }

        // Аниматор (если он был в базе)
        protected Animator animator;
        protected int animIDIsMoving;
        protected int animIDSpeed;
        protected int animIDAttack;
        protected int animIDTakeDamage;
        protected int animIDDeath;

        [Header("ParticleSystem for Damage Effect")]
        public ParticleSystem DamageEffect => _data?.damageEffect;

        public System.Action<Creature> OnDeath;

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

        public void SetCreatureLayer()
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

        // Ленивая загрузка конфига
        private CreatureGlobalConfig _config;
        public CreatureGlobalConfig Config
        {
            get
            {
                if (_config == null && GameAssets.Instance != null)
                {
                    _config = GameAssets.Instance.CreatureGlobalConfig;
                }
                return _config;
            }
        }

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
        public int Level => _data?.baseStats.level ?? 1;

        public float WanderRange => Config.wanderRange;
        public float MinWanderDelay => Config.minWanderDelay;
        public float MaxWanderDelay => Config.maxWanderDelay;

        public float TamedFleeDistance => Config.tamedFleeDistance;
        public float TamedFleeMaxThreatDistance => Config.tamedFleeMaxThreatDistance;
        public float TamedNeutralChaseRadius => Config.tamedNeutralChaseRadius;
        public float TamedAttackOwnerRadius => Config.tamedAttackOwnerRadius;

        public float Speed => _data.baseStats.maxSpeed;
        public float SprintSpeed => _data.baseStats.maxSpeed * 2f;

        public float TamedSpeed => _data.baseStats.maxSpeed * 2f; //пока так для тестов, потом будет прокачка статов
        public float TamedSprintSpeed => _data.baseStats.maxSpeed * 4f; //пока так для тестов, потом будет прокачка статов
        public float TamedWanderSpeed => _data.baseStats.maxSpeed * 0.5f; //пока так для тестов, потом будет прокачка статов

        public float RideSpeed => _data.baseStats.maxSpeed * 3f; //пока так для тестов, потом будет прокачка статов
        public float RideSprintSpeed => _data.baseStats.maxSpeed * 5f; //пока так для тестов, потом будет прокачка статов

        public float AggressionRadius => _data.combatProps.aggressionRadius;
        public float AttackRange => _data.combatProps.attackRange;

        public float AttackDamage => _data.baseStats.maxDamage;
        public float TamedAttackDamage => _data.baseStats.maxDamage * 10f; //пока так для тестов, потом будет прокачка статов

        public float AttackCooldown => _data.combatProps.attackCooldown;
        public float RideAttackCooldown => _data.riddenProps.rideAttackCooldown;

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
        public CreatureBehaviorType BehaviorType => _data.behaviorType;
        public Animator Animator => animator;
        public int AnimIDAttack => animIDAttack;
        public int AnimIDTakeDamage => animIDTakeDamage;
        public int AnimIDIsMoving => animIDIsMoving;
        public int AnimIDSpeed => animIDSpeed;
        public int AnimIDDeath => animIDDeath;
        public float Health { get => health; set => health = value; }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                animIDIsMoving = Animator.StringToHash("IsMoving");
                animIDSpeed = Animator.StringToHash("Speed");
                animIDAttack = Animator.StringToHash("Attack");
                animIDTakeDamage = Animator.StringToHash("TakeDamage");
                animIDDeath = Animator.StringToHash("Death");
            }

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

            // подсистемы создаём в Awake — TamingManager.SpawnFromData
            // использует creature.Interactable сразу после Instantiate (до Start).
            // Данные (_data) грузятся в Start, когда GameAssets готов.
            _combat = gameObject.AddComponent<CreatureCombat>();
            _death = gameObject.AddComponent<CreatureDeath>();
            _knockout = gameObject.AddComponent<CreatureKnockout>();
            _taming = gameObject.AddComponent<CreatureTaming>();
            _commands = gameObject.AddComponent<CreatureCommands>();
            _audio = gameObject.AddComponent<CreatureAudio>();
            _wildAI = gameObject.AddComponent<CreatureWildAI>();
            _tamedAI = gameObject.AddComponent<CreatureTamedAI>();
            _riding = gameObject.AddComponent<CreatureRiding>();
            _interactable = gameObject.AddComponent<CreatureInteractable>();
            gameObject.AddComponent<RagdollController>();
            gameObject.AddComponent<HealthMaterialBlender>();

            var corpse = gameObject.AddComponent<Corpse>();
            corpse.enabled = false;

            var menu = gameObject.AddComponent<RadialMenu>();
            menu.enabled = false;
        }

        /// <summary>
        /// ждём GameAssets и грузим данные существа.
        /// Awake не может ждать — порядок Awake между объектами не гарантирован.
        /// </summary>
        private IEnumerator Start()
        {
            float waitTimer = 0f;
            while (GameAssets.Instance == null)
            {
                waitTimer += Time.unscaledDeltaTime;
                if (waitTimer > 5f)
                {
                    Debug.LogError("[Creature] GameAssets.Instance не появился за 5 секунд!");
                    yield break;
                }
                yield return null;
            }

            var assets = GameAssets.Instance;
            _creatureDatabase = assets.CreatureDatabase;

            if (_creatureDatabase == null)
            {
                Debug.LogError($"[Creature] _creatureDatabase не назначена на {gameObject.name}!");
                yield break;
            }

            if (string.IsNullOrEmpty(creatureId))
            {
                Debug.LogError($"[Creature] creatureId не задан на {gameObject.name}!");
                yield break;
            }

            _config = assets.CreatureGlobalConfig;

            _data = _creatureDatabase.GetCreature(creatureId);
            if (_data == null)
            {
                Debug.LogError($"[Creature] Существо '{creatureId}' не найдено в базе!");
                yield break;
            }

            // Инициализация звуков (требует _data)
            _audio.Initialize(_data);

            // Статы
            maxHealth = _data.baseStats.maxHealth;
            health = maxHealth;
            maxStamina = _data.baseStats.maxStamina;
            stamina = maxStamina;
            maxFood = _data.baseStats.maxFood;
            food = maxFood;
        }

        void Update()
        {
            // ДОБАВЛЕНО (Шаг 3.5c): данные ещё не загружены (Start не отработал) — пропускаем кадр
            if (_data == null || _taming == null) return;

            if (!IsAlive()) return;
            if (IsSinglePlayerLoading()) return; ;

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
                    var pc = PlayerController.LocalPlayer;
                    if (pc != null)
                    {
                        bool blocked = pc.LockCamera;
                        Vector2 move = blocked ? Vector2.zero : pc.MoveInput;
                        Vector2 look = blocked ? Vector2.zero : pc.LookInput;
                        bool sprint = blocked ? false : pc.SprintInput;

                        _riding.TickRidden(move, look, sprint, false);
                    }

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

        public void TakeDamage(float damage, float torporAmount, PlayerInteraction pi, Collider hit, Vector3? point, CombatMember attacker)
        {
            if (_combat == null) return;
            _combat.TakeDamage(damage, torporAmount, pi, hit, point, attacker);
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

        // ==========================================
        // === СОБЫТИЯ АНИМАЦИИ ===
        // ==========================================

        public void DealDamageToTarget() => _combat?.DealDamageToTarget();
        public void SoundOnAttack(AnimationEvent animEv) => _audio?.SoundOnAttack(animEv);
        public void SoundOnTakeDamage(AnimationEvent animEv) => _audio?.SoundOnTakeDamage(animEv);
        public void SoundOnDeath(AnimationEvent animEv) => _audio?.SoundOnDeath(animEv);
        public void SoundOnFootstep(AnimationEvent animEv) => _audio?.SoundOnFootstep(animEv);

        private void UpdateAnimation()
        {
            if (animator == null) return;
            if (_combat != null && _combat.IsAttacking) return;
            if (_agent == null) return;

            float currentSpeed = _agent.velocity.magnitude;
            animator.SetFloat(animIDSpeed, currentSpeed);
            animator.SetBool(animIDIsMoving, currentSpeed > 0.1f);
        }



    }

}