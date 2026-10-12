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
        public CreatureBehaviorType BehaviorType => _data?.behaviorType ?? CreatureBehaviorType.Aggressive;

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

        private enum CreatureState { Wander, Chase, Attack }
        private CreatureState currentState = CreatureState.Wander;

        private float nextWanderTime = 0f;
        private float lastAttackTime = -10f;
        private bool isAttacking = false;
        private bool isWandering = false;

        private float _tamingSaveTimer = 0f;
        private const float TamingSaveInterval = 10f;

        // === AI прирученного ===
        private const float FOLLOW_DIST_LOWEST = 1.5f;
        private const float FOLLOW_DIST_LOW = 2.5f;
        private const float FOLLOW_DIST_MEDIUM = 3.5f;
        private const float FOLLOW_DIST_HIGH = 5f;
        private const float FOLLOW_DIST_HIGHEST = 7f;
        private const float FOLLOW_CHASE_EXTRA = 2f;   // гистерезис между stop и chase

        private Transform _currentEnemy;
        private float _nextEnemySearchTime = 0f;
        private const float ENEMY_SEARCH_INTERVAL = 0.5f;  // искать врага раз в 0.5 сек

        // === Текущая цель атаки ===
        private BaseLivingEntity _currentAttackTarget;   // если цель — существо
        private bool _isAttackingPlayer = false;         // если цель — игрок
        private Transform _wildTargetTransform;  // кэш цели для chase

        public bool DieManually = false;

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

        void Update()
        {
            if (!IsAlive()) return;
            if (IsSinglePlayerLoading()) return;

            // === Torpor: убывает всегда, даже в нокауте ===
            if (torpor > 0f)
            {
                torpor = Mathf.Max(0f, torpor - Data.torporRecoveryRate * Time.deltaTime);
            }

            // Food — падает у нокаутнутых и прирученных
            if (knockedOut || tamed)
            {
                float drain = knockedOut ? Data.foodDrainRateKnockedOut : Data.foodDrainRateTamed;
                TickFood(drain);
                if (ShouldEat()) TryEatFood();
            }

            // Периодическое сохранение нокаутнутого/прирученного
            if ((knockedOut || tamed) && !string.IsNullOrEmpty(TamingInstanceId))
            {
                _tamingSaveTimer += Time.deltaTime;
                if (_tamingSaveTimer >= TamingSaveInterval)
                {
                    _tamingSaveTimer = 0f;
                    TamingManager.Instance?.UpdateSave(TamingInstanceId, "player_001");
                }
            }

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
                // Сбрасываем анимацию — существо стоит
                // if (animator != null)
                // {
                //     animator.SetFloat(animIDSpeed, 0f);
                //     animator.SetBool(animIDIsMoving, false);
                // }
                // TODO: AI прирученного (Follow/Stay/Idle) — отдельная система,
                // сейчас просто Wander или Idle. Можно оставить как есть для теста.
                HandleTamedState();
                UpdateAnimation();
                return;
            }


            // === Обычный AI ===
            // if (_agent == null) return;

            // if (_playerTransform == null)
            // {
            //     SetState(CreatureState.Wander);
            //     HandleWandering();
            //     return;
            // }

            // float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            // if (isAttacking)
            // {
            //     // Во время атаки не меняем состояние
            // }
            // else if (distanceToPlayer <= AttackRange && Time.time >= lastAttackTime + AttackCooldown)
            // {
            //     SetState(CreatureState.Attack);
            //     PerformAttack();
            // }
            // else if (distanceToPlayer <= AggressionRadius)
            // {
            //     SetState(CreatureState.Chase);
            //     HandleChasing();
            // }
            // else
            // {
            //     SetState(CreatureState.Wander);
            //     HandleWandering();
            // }

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
            if (isAttacking) { /* ждём */ }
            else
            {
                // Ищем цель (игрок + прирученные)
                FindWildTarget();

                if (_wildTargetTransform != null)
                {
                    float dist = Vector3.Distance(transform.position, _wildTargetTransform.position);

                    if (dist <= AttackRange && Time.time >= lastAttackTime + AttackCooldown)
                    {
                        SetState(CreatureState.Attack);
                        PerformAttack();
                    }
                    else
                    {
                        SetState(CreatureState.Chase);
                        _agent.isStopped = false;
                        _agent.speed = ChaseSpeed;
                        _agent.SetDestination(_wildTargetTransform.position);
                    }
                }
                else
                {
                    SetState(CreatureState.Wander);
                    HandleWandering();
                }
            }

            UpdateAnimation();
            // UpdateCurrentEnemy();

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

        // private void HandleChasing()
        // {
        //     if (!_agent.isOnNavMesh) return;
        //     _agent.SetDestination(_playerTransform.position);
        // }

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

        private void PerformAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;

            if (animator != null)
            {
                animator.SetTrigger(animIDAttack);
            }
            else
            {
                Invoke(nameof(DealDamageToTarget), 0.5f);
            }

            Invoke(nameof(ResetAttackState), AttackCooldown);
        }

        /// <summary>
        /// Вызывается из Animation Event (или через Invoke, если нет аниматора).
        /// Универсально: бьёт игрока или существо — в зависимости от текущей цели.
        /// Имя оставлено для совместимости с Animation Event.
        /// </summary>
        public void DealDamageToTarget()
        {
            // Если цель — существо
            if (!_isAttackingPlayer && _currentAttackTarget != null)
            {
                if (_currentAttackTarget.IsAlive())
                {
                    _currentAttackTarget.NotifyAttackedBy(this);
                    _currentAttackTarget.TakeDamage(AttackDamage, 0f);
                }
                return;
            }

            // Иначе — игрок
            PlayerSurvivalSystem.Instance?.TakeDamage(AttackDamage);
        }

        private void ResetAttackState()
        {
            isAttacking = false;

            // Прирученный — возвращаемся к обычной логике (там своя логика в HandleTamedState)
            if (tamed)
            {
                _currentAttackTarget = null;
                _isAttackingPlayer = false;
                return;
            }

            // Дикий — к chase/wander на игрока
            if (_playerTransform != null &&
                Vector3.Distance(transform.position, _playerTransform.position) <= AggressionRadius)
            {
                SetState(CreatureState.Chase);
            }
            else
            {
                SetState(CreatureState.Wander);
            }
        }

        private void UpdateAnimation()
        {
            if (animator == null || isAttacking) return;
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
                UpdateCurrentEnemy();

                if (_currentEnemy != null)
                {
                    ChaseAndAttack(_currentEnemy);
                    return;
                }
            }
            else
            {
                _currentEnemy = null;
                _currentAttackTarget = null;
                _isAttackingPlayer = false;
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

        private void UpdateCurrentEnemy()
        {
            // Если цель жива и в радиусе — оставляем
            if (_currentEnemy != null)
            {
                var enemyLiving = _currentEnemy.GetComponent<BaseLivingEntity>();
                if (enemyLiving == null || !enemyLiving.IsAlive() || enemyLiving.tamed)
                {
                    _currentEnemy = null;
                    _currentAttackTarget = null;
                }
                else
                {
                    float d = Vector3.Distance(transform.position, _currentEnemy.position);
                    if (d > Data.tamedAggroRadius * 1.5f)
                    {
                        _currentEnemy = null;
                        _currentAttackTarget = null;
                    }
                }
            }

            // Если цели нет — искать
            if (_currentEnemy == null && Time.time >= _nextEnemySearchTime)
            {
                _nextEnemySearchTime = Time.time + ENEMY_SEARCH_INTERVAL;
                _currentEnemy = FindNearestEnemy();
            }
        }

        private Transform FindNearestEnemy()
        {
            if (Data == null) return null;

            int creatureLayer = LayerMask.NameToLayer("Creature");
            if (creatureLayer == -1) return null;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                Data.tamedAggroRadius,
                1 << creatureLayer,
                QueryTriggerInteraction.Ignore);

            Transform nearest = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var living = hit.GetComponent<BaseLivingEntity>();
                if (living == null) continue;
                if (living.tamed || living.knockedOut || living.IsDead()) continue;
                if (!living.IsAlive()) continue;

                // Не игрок
                // if (living is PlayerController) continue;
                // Если это существо, у которого есть Data — проверим, что это не наш хозяин
                // (игрок тут не BaseLivingEntity, так что проверка выше)

                float d = Vector3.Distance(transform.position, hit.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = hit.transform;
                }
            }

            return nearest;
        }


        /// <summary>
        /// Найти ближайшую цель для дикого существа: игрок + прирученные существа.
        /// Возвращает:
        /// - _currentAttackTarget != null → цель — существо.
        /// - _isAttackingPlayer == true → цель — игрок.
        /// - оба false / null → цели нет.
        /// </summary>
        private void FindWildTarget()
        {
            float bestDist = float.MaxValue;
            BaseLivingEntity bestCreature = null;
            bool bestIsPlayer = false;
            Transform bestTransform = null;

            // === 1. Игрок ===
            if (_playerTransform != null)
            {
                float d = Vector3.Distance(transform.position, _playerTransform.position);
                if (d <= AggressionRadius)
                {
                    bestDist = d;
                    bestIsPlayer = true;
                    bestTransform = _playerTransform;
                }
            }

            // === 2. Прирученные существа ===
            int creatureLayer = LayerMask.NameToLayer("Creature");
            if (creatureLayer != -1)
            {
                Collider[] hits = Physics.OverlapSphere(
                    transform.position,
                    AggressionRadius,
                    1 << creatureLayer,
                    QueryTriggerInteraction.Ignore);

                foreach (var hit in hits)
                {
                    if (hit.gameObject == gameObject) continue;

                    var living = hit.GetComponent<BaseLivingEntity>();
                    if (living == null) continue;
                    if (!living.tamed) continue;
                    if (living.knockedOut || living.IsDead() || !living.IsAlive()) continue;

                    float d = Vector3.Distance(transform.position, hit.transform.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestCreature = living;
                        bestIsPlayer = false;
                        bestTransform = hit.transform;
                    }
                }
            }

            // === 3. Установить результат ===
            _currentAttackTarget = bestCreature;
            _isAttackingPlayer = bestIsPlayer;
            _wildTargetTransform = bestTransform;
        }



        private void ChaseAndAttack(Transform target)
        {
            if (target == null) return;

            // === Установить текущую цель ===
            var targetLiving = target.GetComponent<BaseLivingEntity>();
            if (targetLiving != null)
            {
                _currentAttackTarget = targetLiving;
                _isAttackingPlayer = false;
            }
            else
            {
                // Не существо (например, игрок) — бьём игрока
                _currentAttackTarget = null;
                _isAttackingPlayer = true;
            }

            float dist = Vector3.Distance(transform.position, target.position);

            if (dist <= AttackRange)
            {
                _agent.isStopped = true;

                if (Time.time >= lastAttackTime + AttackCooldown)
                {
                    PerformAttack();
                }
            }
            else
            {
                _agent.isStopped = false;
                _agent.speed = ChaseSpeed;
                _agent.SetDestination(target.position);
            }
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
            var threat = LastAttacker;
            if (threat == null || !threat.IsAlive())
            {
                ClearLastAttacker();
                return false;
            }

            float threatDist = Vector3.Distance(transform.position, threat.transform.position);
            float maxThreatDist = Data != null ? Data.tamedFleeMaxThreatDistance : 30f;

            if (threatDist > maxThreatDist)
            {
                ClearLastAttacker();
                return false;
            }

            FleeFrom(threat.transform);
            return true;
        }

        /// <summary>Neutral: атаковать обидчика, пока он в радиусе.</summary>
        private bool HandleNeutral()
        {
            var threat = LastAttacker;
            if (threat == null || !threat.IsAlive())
            {
                ClearLastAttacker();
                return false;
            }

            float threatDist = Vector3.Distance(transform.position, threat.transform.position);
            float chaseRadius = Data != null ? Data.tamedNeutralChaseRadius : 20f;

            if (threatDist > chaseRadius)
            {
                ClearLastAttacker();
                return false;
            }

            ChaseAndAttack(threat.transform);
            return true;
        }

        /// <summary>AttackOwnerTarget: защищать игрока — атаковать врага рядом с ним.</summary>
        private bool HandleAttackOwnerTarget()
        {
            if (_playerTransform == null) return false;

            float playerDist = Vector3.Distance(transform.position, _playerTransform.position);
            float ownerRadius = Data != null ? Data.tamedAttackOwnerRadius : 30f;

            if (playerDist > ownerRadius) return false;

            var enemy = FindEnemyNearPlayer();
            if (enemy != null)
            {
                ChaseAndAttack(enemy.transform);
                return true;
            }

            return false;
        }

        /// <summary>Бежать в противоположную сторону от угрозы.</summary>
        private void FleeFrom(Transform threat)
        {
            if (threat == null) return;

            Vector3 awayDir = (transform.position - threat.position).normalized;
            Vector3 targetPos = transform.position + awayDir * 5f;

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.speed = ChaseSpeed;
                _agent.SetDestination(hit.position);
            }
        }

        /// <summary>Найти дикого врага рядом с игроком (в радиусе tamedAggroRadius от игрока).</summary>
        private BaseLivingEntity FindEnemyNearPlayer()
        {
            if (_playerTransform == null) return null;
            if (Data == null) return null;

            int creatureLayer = LayerMask.NameToLayer("Creature");
            if (creatureLayer == -1) return null;

            Collider[] hits = Physics.OverlapSphere(
                _playerTransform.position,
                Data.tamedAggroRadius,
                1 << creatureLayer,
                QueryTriggerInteraction.Ignore);

            BaseLivingEntity nearest = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var living = hit.GetComponent<BaseLivingEntity>();
                if (living == null) continue;
                if (living.tamed || living.knockedOut || living.IsDead() || !living.IsAlive()) continue;

                float d = Vector3.Distance(transform.position, hit.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = living;
                }
            }

            return nearest;
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

        private void TickFood(float drainRate)
        {
            food = Mathf.Max(0f, food - drainRate * Time.deltaTime);

            // Голодная смерть (по аналогии с игроком)
            if (food <= 0f && Data.starvationDamagePerSecond > 0f)
            {
                // Наносим урон через TakeDamage, чтобы работали попапы/смерть
                // Но осторожно: TakeDamage шлёт popup и анимацию. Лучше — прямой health -=.
                health = Mathf.Max(0f, health - Data.starvationDamagePerSecond * Time.deltaTime);
                if (health <= 0f) Die();
            }
        }

        private bool ShouldEat()
        {
            if (Data.maxFood <= 0f) return false;
            return food <= Data.maxFood * Data.foodEatThresholdPercent;
        }

        /// <summary>
        /// Пытается найти и съесть подходящую еду в инвентаре.
        /// </summary>
        private bool TryEatFood()
        {
            if (GetInventory()?.Data == null || Data.tamingFoodPreferences == null
                || Data.tamingFoodPreferences.Length == 0)
                return false;

            var inventoryData = GetInventory().Data;

            foreach (var pref in Data.tamingFoodPreferences)
            {
                if (pref.food == null) continue;

                var slot = inventoryData.slots.Find(s => !s.IsEmpty && s.item == pref.food);
                if (slot != null)
                {
                    var eaten = slot.item;
                    inventoryData.RemoveItemFromSlot(inventoryData.slots.IndexOf(slot), 1);

                    float recovery = eaten.foodRecoveryAmount > 0f ? eaten.foodRecoveryAmount : 10f;
                    AddFood(recovery);

                    // Прогресс приручения — только если ещё не приручен
                    if (!tamed)
                    {
                        bool finished = AddTamingProgress(pref.progressAmount);

                        if (finished)
                        {
                            Debug.Log($"[{gameObject.name}] Съел {eaten.itemName}, food: {food:F1}, progress: 100,0% (ПРИРУЧЕН)");
                        }
                        else
                        {
                            Debug.Log($"[{gameObject.name}] Съел {eaten.itemName}, food: {food:F1}, progress: {tamingProgress:F1}%");
                        }
                    }

                    // === СРАЗУ СОХРАНЯЕМ ===
                    if (!string.IsNullOrEmpty(TamingInstanceId))
                    {
                        TamingManager.Instance?.UpdateSave(TamingInstanceId, "player_001");
                    }

                    return true;
                }
            }
            return false;
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