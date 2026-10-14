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
    public class Creature : BaseLivingEntity
    {
        [Header("Database")]
        [Tooltip("Ссылка на базу данных существ")]
        public CreatureDatabase creatureDatabase;

        [Tooltip("ID существа (например, 'Raptor', 'Dodo')")]
        public string creatureId;

        // Кэшированная ссылка на данные
        private CreatureData _data;
        public CreatureData Data => _data;

        [Header("Components")]
        private CreatureCombat _combat;
        private CreatureDeath _death;
        private CreatureKnockout _knockout;
        private CreatureTaming _taming;
        private CreatureCommands _commands;

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

        [Header("Audio (заполняется из базы)")]
        [HideInInspector] public AudioClip FootstepAudioClip;
        [HideInInspector, Range(0, 1)] public float FootstepAudioVolume = 0.5f;
        [HideInInspector] public AudioClip AttackAudioClip;
        [HideInInspector, Range(0, 1)] public float AttackAudioVolume = 0.5f;
        [HideInInspector] public AudioClip TakeDamageAudioClip;
        [HideInInspector, Range(0, 1)] public float TakeDamageAudioVolume = 0.5f;
        [HideInInspector] public AudioClip DeathAudioClip;
        [HideInInspector, Range(0, 1)] public float DeathAudioVolume = 0.5f;

        private NavMeshAgent _agent;
        private Transform _playerTransform;

        public void RequestDeath() => Die();
        public void Die()
        {
            if (_death != null)
            {
                PlaySound(DeathAudioClip, DeathAudioVolume);
                _death.Die();
            }

        }

        private enum CreatureState { Wander, Chase, Attack }
        private CreatureState currentState = CreatureState.Wander;

        private float nextWanderTime = 0f;
        private bool isWandering = false;

        // === AI прирученного ===
        private const float FOLLOW_DIST_LOWEST = 1.5f;
        private const float FOLLOW_DIST_LOW = 2.5f;
        private const float FOLLOW_DIST_MEDIUM = 3.5f;
        private const float FOLLOW_DIST_HIGH = 5f;
        private const float FOLLOW_DIST_HIGHEST = 7f;
        private const float FOLLOW_CHASE_EXTRA = 2f;   // гистерезис между stop и chase

        public bool DieManually = false;

        // === Геттеры ===
        public CreatureCombat Combat => _combat;
        public CreatureDeath Death => _death;
        public CreatureKnockout Knockout => _knockout;
        public CreatureTaming Taming => _taming;
        public CreatureCommands Commands => _commands;

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


        protected override void Awake()
        {

            base.Awake();

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

            // === Применяем поведение ===
            tamable = _data.tamable;
            tamableKO = _data.tamableKO;
            tamablePassive = _data.tamablePassive;

            // === Применяем звуки ===
            FootstepAudioClip = _data.footstepClip;
            FootstepAudioVolume = _data.footstepVolume;
            AttackAudioClip = _data.attackClip;
            AttackAudioVolume = _data.attackVolume;
            TakeDamageAudioClip = _data.takeDamageClip;
            TakeDamageAudioVolume = _data.takeDamageVolume;
            DeathAudioClip = _data.deathClip;
            DeathAudioVolume = _data.deathVolume;

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

        public void SetTarget(Transform target)
        {
            _playerTransform = target;
        }

        public override string GetDisplayName()
        {
            if (Data != null && !string.IsNullOrEmpty(Data.displayName))
                return Data.displayName;
            return !string.IsNullOrEmpty(creatureId) ? creatureId : gameObject.name;
        }

        public override float GetDamageMultiplier(Collider hitCollider)
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
        // === НОКАУТ (override из BaseLivingEntity) ===
        // ==========================================

        public override void KnockOut()
        {
            if (_knockout != null) _knockout.KnockOut();
        }

        public override void KnockOutFromLoad()
        {
            if (_knockout != null) _knockout.KnockOutFromLoad();
        }

        public override void RecoverFromKnockout()
        {
            if (_knockout != null) _knockout.RecoverFromKnockout();
        }

        // ==========================================
        // === ПРИРУЧЕНИЕ (override из BaseLivingEntity) ===
        // ==========================================

        public override bool AddTamingProgress(float amount) => _taming != null && _taming.AddTamingProgress(amount);
        public override void AddTorpor(float amount) => _taming?.AddTorpor(amount);
        public override void AddFood(float amount) => _taming?.AddFood(amount);
        public override bool ConsumeFood(float amount) => _taming != null && _taming.ConsumeFood(amount);
        public override void Unclaim() => _taming?.Unclaim();

        // ==========================================
        // === КОМАНДЫ (override из BaseLivingEntity) ===
        // ==========================================

        public override void SetTamedFollowing(bool value) => _commands?.SetTamedFollowing(value);
        public override void SetTamedWandering(bool value) => _commands?.SetTamedWandering(value);
        public override void SetTamedMating(bool value) => _commands?.SetTamedMating(value);
        public override void SetTamedBehavior(TamedBehavior value) => _commands?.SetTamedBehavior(value);
        public override void SetFollowDistance(FollowDistance value) => _commands?.SetFollowDistance(value);

        public override void ExecuteRadialAction(string actionId) => _commands?.ExecuteRadialAction(actionId);
        public override string GetRadialRadioValue(string radioGroupId) => _commands?.GetRadialRadioValue(radioGroupId);
        public override void SetRadialRadioValue(string radioGroupId, string radioValue) => _commands?.SetRadialRadioValue(radioGroupId, radioValue);
        public override string GetRadialDynamicLabel(string actionId) => _commands?.GetRadialDynamicLabel(actionId);
        public override string GetBehaviorText() => _commands != null ? _commands.TamedBehaviorToText() : "Wild";

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
                // TODO: AI прирученного (Follow/Stay/Idle) — отдельная система,
                // сейчас просто Wander или Idle. Можно оставить как есть для теста.
                HandleTamedState();
                UpdateAnimation();
                return;
            }

            // === Обычный AI ===
            if (_agent == null) return;

            // Игрока нет вообще — нет смысла искать
            if (_playerTransform == null)
            {
                SetState(CreatureState.Wander);
                HandleWandering();
                return;
            }

            // Если атака идёт — ждём
            if (_combat.IsAttacking) { /* ждём */ }
            else
            {
                _combat.FindWildTarget();

                if (_combat.WildTargetTransform != null)
                {
                    float dist = Vector3.Distance(transform.position, _combat.WildTargetTransform.position);

                    if (dist <= AttackRange && Time.time >= _combat.LastAttackTime + AttackCooldown)
                    {
                        SetState(CreatureState.Attack);
                        _combat.PerformAttack();
                    }
                    else
                    {
                        SetState(CreatureState.Chase);
                        _agent.isStopped = false;
                        _agent.speed = ChaseSpeed;
                        _agent.SetDestination(_combat.WildTargetTransform.position);
                    }
                }
                else
                {
                    SetState(CreatureState.Wander);
                    HandleWandering();
                }
            }

            UpdateAnimation();

            if (DieManually)
            {
                Die();
            }
        }

        void LateUpdate()
        {
            if (isWandering && _agent != null && _agent.isOnNavMesh && _agent.remainingDistance < _agent.stoppingDistance)
            {
                isWandering = false;
            }
        }

        private void SetState(CreatureState newState)
        {
            if (currentState == newState) return;
            currentState = newState;

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                switch (currentState)
                {
                    case CreatureState.Chase:
                        _agent.speed = ChaseSpeed;
                        _agent.isStopped = false;
                        break;
                    case CreatureState.Wander:
                        _agent.speed = WalkSpeed;
                        _agent.isStopped = false;
                        break;
                    case CreatureState.Attack:
                        _agent.isStopped = true;
                        break;
                }
            }
        }

        private void HandleWandering()
        {
            // === ФИКС: проверяем, что агент активен и на NavMesh ===
            if (_agent == null)
            {
                Debug.LogError("_agent == null");
                return;
            }
            if (!_agent.enabled)
            {
                Debug.LogError("!_agent.enabled");
                return;
            }
            if (!_agent.isOnNavMesh)
            {
                Debug.LogError("!_agent.isOnNavMesh");
                return;
            }
            if (Time.time >= nextWanderTime)
            {
                StartWandering();
            }

            if (_agent.remainingDistance <= _agent.stoppingDistance && !_agent.pathPending)
            {
                if (_agent.velocity.magnitude < 0.1f)
                {
                    animator?.SetBool(animIDIsMoving, false);
                }
            }
        }

        private void UpdateAnimation()
        {
            if (animator == null || _combat.IsAttacking) return;
            if (_agent == null) return;

            float currentSpeed = _agent.velocity.magnitude;
            animator.SetFloat(animIDSpeed, currentSpeed);
            animator.SetBool(animIDIsMoving, currentSpeed > 0.1f);
        }

        private void StartWandering()
        {
            if (!_agent.isOnNavMesh) return;

            isWandering = true;

            Vector3 randomDirection = Random.insideUnitSphere * WanderRange;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, WanderRange, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }

            float delay = Random.Range(MinWanderDelay, MaxWanderDelay);
            nextWanderTime = Time.time + delay;
        }


        // ==========================================
        // === AI ПРИРУЧЕННОГО ===
        // ==========================================

        private void HandleTamedState()
        {
            if (_agent == null) return;
            if (!_agent.enabled || !_agent.isOnNavMesh) return;

            // === 1. Flee ===
            if (tamedBehavior == TamedBehavior.Flee)
            {
                if (HandleFlee()) return;
            }

            // === 2. Neutral ===
            if (tamedBehavior == TamedBehavior.Neutral)
            {
                if (HandleNeutral()) return;
            }

            // === 3. AttackOwnerTarget ===
            if (tamedBehavior == TamedBehavior.AttackOwnerTarget)
            {
                if (HandleAttackOwnerTarget()) return;
            }

            // === 4. Aggressive ===
            if (tamedBehavior == TamedBehavior.Aggressive)
            {
                _combat.UpdateCurrentEnemy();

                if (_combat.CurrentEnemy != null)
                {
                    _combat.ChaseAndAttack(_combat.CurrentEnemy.transform);
                    return;
                }
            }
            else
            {
                _combat.ClearTargets();
            }

            // === 5. Follow ===
            if (tamedFollowing && _playerTransform != null)
            {
                FollowOwner();
                return;
            }

            // === 6. Wander ===
            if (tamedWandering)
            {
                WanderTamed();
                return;
            }

            // === 7. Idle ===
            IdleTamed();
        }

        private void FollowOwner()
        {
            float stopDist = GetFollowStopDistance();
            float chaseDist = stopDist + FOLLOW_CHASE_EXTRA;

            float dist = Vector3.Distance(transform.position, _playerTransform.position);

            if (dist > chaseDist)
            {
                // Догонять
                _agent.isStopped = false;
                _agent.speed = Data != null ? Data.tamedFollowChaseSpeed : ChaseSpeed;
                _agent.SetDestination(_playerTransform.position);
            }
            else if (dist <= stopDist)
            {
                // Остановиться
                _agent.isStopped = true;
            }
            // Между stopDist и chaseDist — не трогаем (гистерезис)
        }

        private float GetFollowStopDistance()
        {
            switch (tamedFollowDistance)
            {
                case FollowDistance.Lowest: return FOLLOW_DIST_LOWEST;
                case FollowDistance.Low: return FOLLOW_DIST_LOW;
                case FollowDistance.Medium: return FOLLOW_DIST_MEDIUM;
                case FollowDistance.High: return FOLLOW_DIST_HIGH;
                case FollowDistance.Highest: return FOLLOW_DIST_HIGHEST;
                default: return FOLLOW_DIST_MEDIUM;
            }
        }

        private void WanderTamed()
        {
            _agent.isStopped = false;
            _agent.speed = WalkSpeed;

            if (!isWandering || Time.time >= nextWanderTime)
            {
                StartTamedWandering();
            }
        }

        private void StartTamedWandering()
        {
            if (!_agent.isOnNavMesh) return;

            isWandering = true;

            float radius = Data != null ? Data.tamedWanderRadius : 10f;
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }

            float delay = Random.Range(MinWanderDelay, MaxWanderDelay);
            nextWanderTime = Time.time + delay;
        }

        /// <summary>
        /// Сбросить состояние блуждания — чтобы AI заново запустил Wander.
        /// Вызывается при переключении в режим Wander.
        /// </summary>
        public void ResetWanderState()
        {
            isWandering = false;
            nextWanderTime = 0f;
        }

        private void IdleTamed()
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        void OnDestinationReached()
        {
            if (_agent.remainingDistance < _agent.stoppingDistance)
            {
                isWandering = false;
            }
        }

        // ==========================================
        // === ПОВЕДЕНИЯ: FLEE / NEUTRAL / ATTACK_OWNER ===
        // ==========================================

        /// <summary>Flee: бежать от обидчика, пока он не выйдет из радиуса.</summary>
        private bool HandleFlee()
        {
            var threat = _combat.LastAttacker;

            Debug.Log($"[Flee] {gameObject.name}: LastAttacker={(threat != null ? threat.name : "null")}, tamed={tamed}, knockedOut={knockedOut}");

            if (threat == null || !threat.IsAlive())
            {
                _combat.ClearLastAttacker();
                return false;
            }

            float threatDist = Vector3.Distance(transform.position, threat.transform.position);
            float maxThreatDist = Data != null ? Data.tamedFleeMaxThreatDistance : 30f;

            Debug.Log($"[Flee] {gameObject.name}: threatDist={threatDist:F1}, maxThreatDist={maxThreatDist}");

            if (threatDist > maxThreatDist)
            {
                _combat.ClearLastAttacker();
                return false;
            }

            _combat.FleeFrom(threat.transform);
            return true;
        }

        /// <summary>Neutral: атаковать обидчика, пока он в радиусе.</summary>
        private bool HandleNeutral()
        {
            var threat = _combat.LastAttacker;
            if (threat == null || !threat.IsAlive())
            {
                _combat.ClearLastAttacker();
                return false;
            }

            float threatDist = Vector3.Distance(transform.position, threat.transform.position);
            float chaseRadius = Data != null ? Data.tamedNeutralChaseRadius : 20f;

            if (threatDist > chaseRadius)
            {
                _combat.ClearLastAttacker();
                return false;
            }

            _combat.ChaseAndAttack(threat.transform);
            return true;
        }

        /// <summary>AttackOwnerTarget: защищать игрока — атаковать врага рядом с ним.</summary>
        private bool HandleAttackOwnerTarget()
        {
            if (_playerTransform == null) return false;

            float playerDist = Vector3.Distance(transform.position, _playerTransform.position);
            float ownerRadius = Data != null ? Data.tamedAttackOwnerRadius : 30f;

            if (playerDist > ownerRadius) return false;

            var enemy = _combat.FindEnemyNearPlayer();
            if (enemy != null)
            {
                _combat.ChaseAndAttack(enemy.transform);
                return true;
            }

            return false;
        }

        protected override float GetMaxHealthFromConfiguration()
        {
            return _data?.maxHealth ?? 100f;
        }

        protected override float GetMaxStaminaFromConfiguration()
        {
            return _data?.maxStamina ?? 50f;
        }

        protected override float GetMaxFoodFromConfiguration()
        {
            return _data?.maxFood ?? 100f;
        }

        // === Звуки через Animation Events ===

        private void SoundOnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                PlaySound(FootstepAudioClip, FootstepAudioVolume);
            }
        }

        private void SoundOnAttack(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                PlaySound(AttackAudioClip, AttackAudioVolume);
            }
        }

        private void SoundOnTakeDamage(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.2f)
            {
                PlaySound(TakeDamageAudioClip, TakeDamageAudioVolume);
            }
        }

        private void SoundOnDeath(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.2f)
            {
                PlaySound(DeathAudioClip, DeathAudioVolume);
            }
        }

        private void PlaySound(AudioClip audioClip, float audioClipVolume)
        {
            if (audioClip != null)
            {
                float globalVolume = 1f;
                if (AudioManager.Instance != null)
                {
                    globalVolume = AudioManager.Instance.masterVolume;
                }

                float finalVolume = globalVolume * audioClipVolume;

                GameObject soundObj = new GameObject($"{audioClip.name}");
                soundObj.transform.position = transform.position;

                AudioSource source = soundObj.AddComponent<AudioSource>();
                source.clip = audioClip;
                source.volume = finalVolume;
                source.spatialBlend = 1f;
                source.Play();

                Destroy(soundObj, audioClip.length + 0.1f);
            }
        }
    }

}