using UnityEngine;
using UnityEngine.AI;
using Assets.Scripts.Core;
using Assets.Scripts.Items;
using Assets.Scripts.Player.Data;
using Assets.Scripts.Interactables;
using Assets.Scripts.Utils;
using Assets.Scripts.InventorySystem;

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

        private Animator _animator;
        private int _animIDSpeed;
        private int _animIDIsMoving;
        private int _animIDAttack;

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

            // === Инициализация компонентов ===
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();

            if (_animator != null)
            {
                _animIDSpeed = Animator.StringToHash("Speed");
                _animIDIsMoving = Animator.StringToHash("IsMoving");
                _animIDAttack = Animator.StringToHash("Attack");
            }

            if (_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerTransform = player.transform;
            }
        }

        public void SetTarget(Transform target)
        {
            _playerTransform = target;
        }

        void Update()
        {
            if (!IsAlive()) return;

            // === Torpor: убывает всегда, даже в нокауте ===
            if (torpor > 0f)
            {
                float before = torpor;
                torpor = Mathf.Max(0f, torpor - torporRecoveryRate * Time.deltaTime);

                // Логируем раз в секунду, только в нокауте (чтобы не спамить)
                if (knockedOut && Mathf.FloorToInt(Time.time) != Mathf.FloorToInt(Time.time - Time.deltaTime))
                {
                    // Debug.Log($"[{gameObject.name}] Torpor падает: {torpor:F1} / {maxTorpor}");
                }
            }

            // === Нокаут: проверяем выход ===
            // Вход происходит в TakeDamage(), здесь только выход.
            if (knockedOut)
            {
                if (torpor <= 0f)
                {
                    RecoverFromKnockout();
                }
                return; // пока в нокауте — AI не работает
            }

            // === Обычный AI ===
            if (_agent == null) return;

            if (_playerTransform == null)
            {
                SetState(CreatureState.Wander);
                HandleWandering();
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            if (isAttacking)
            {
                // Во время атаки не меняем состояние
            }
            else if (distanceToPlayer <= AttackRange && Time.time >= lastAttackTime + AttackCooldown)
            {
                SetState(CreatureState.Attack);
                PerformAttack();
            }
            else if (distanceToPlayer <= AggressionRadius)
            {
                SetState(CreatureState.Chase);
                HandleChasing();
            }
            else
            {
                SetState(CreatureState.Wander);
                HandleWandering();
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

        private void HandleChasing()
        {
            if (!_agent.isOnNavMesh) return;
            _agent.SetDestination(_playerTransform.position);
        }

        private void HandleWandering()
        {
            if (Time.time >= nextWanderTime)
            {
                StartWandering();
            }

            if (_agent.remainingDistance <= _agent.stoppingDistance && !_agent.pathPending)
            {
                if (_agent.velocity.magnitude < 0.1f)
                {
                    _animator?.SetBool(_animIDIsMoving, false);
                }
            }
        }

        private void PerformAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;

            if (_animator != null)
            {
                _animator.SetTrigger(_animIDAttack);
            }

            Invoke(nameof(DealDamageToPlayer), 0.5f);

            Invoke(nameof(ResetAttackState), AttackCooldown);
        }

        private void DealDamageToPlayer()
        {
            PlayerSurvivalSystem.Instance?.TakeDamage(AttackDamage);
        }

        private void ResetAttackState()
        {
            isAttacking = false;
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
            if (_animator != null && !isAttacking)
            {
                float currentSpeed = _agent.velocity.magnitude;
                _animator.SetFloat(_animIDSpeed, currentSpeed);
                _animator.SetBool(_animIDIsMoving, currentSpeed > 0.1f);
            }
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

        void OnDestinationReached()
        {
            if (_agent.remainingDistance < _agent.stoppingDistance)
            {
                isWandering = false;
            }
        }

        protected override float GetMaxHealthFromConfiguration()
        {
            return _data?.maxHealth ?? 100f;
        }

        protected override float GetMaxStaminaFromConfiguration()
        {
            return _data?.maxStamina ?? 50f;
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