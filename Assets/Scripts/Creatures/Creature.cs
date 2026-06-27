using UnityEngine;
using UnityEngine.AI;
using Assets.Scripts.Core;
using Assets.Scripts.Items;
using Assets.Scripts.Player.Data;
using Assets.Scripts.Interactables;
using Assets.Scripts.Utils;
using UnityEngine.Localization.SmartFormat.Utilities;
using Assets.Scripts.InventorySystem;

namespace Assets.Scripts.Creatures
{
    public class Creature : BaseLivingEntity
    {

        [Header("Ragdoll")]
        public RagdollSettings ragdollSettings;

        [Header("Wandering")]
        public float wanderRange = 20f;
        public float minWanderDelay = 2f;
        public float maxWanderDelay = 5f;

        [Header("Movement")]
        public float walkSpeed = 2f;
        public float chaseSpeed = 5f;

        [Header("Aggression")]
        public float aggressionRadius = 30f;
        public float attackRange = 2f;
        public float attackDamage = 10f;
        public float attackCooldown = 2f;

        [Header("Stats")]
        [SerializeField] private float _maxHealth = 100f; // Добавляем поле для инспектора
        [SerializeField] private float _maxStamina = 50f;

        [System.Serializable]
        public class LootEntry
        {
            public Item item;
            public int minAmount = 1;
            public int maxAmount = 1;
            [Range(0f, 1f)] public float dropChance = 1f; // Шанс от 0 до 1
        }

        [Header("Loot & Harvesting")]
        [Tooltip("Предметы, которые рандомно попадут в инвентарь трупа (мясо, шкуры, детали)")]
        public LootEntry[] inventoryLootTable;

        [Tooltip("Ресурсы, которые выпадают при РАЗБИВАНИИ тела (железо, электроника, кости)")]
        public Corpse.ResourceDrop[] harvestDrops;

        [Tooltip("Сколько ударов нужно, чтобы полностью разобрать тело")]
        public int maxHarvestHits = 5;

        [Header("Corpse UI")]
        [Tooltip("Ссылка на UI инвентаря (тот же, что используется для сундуков)")]
        [SerializeField] private ChestUI _chestUI;

        [Header("Audio")]
        public AudioClip FootstepAudioClip;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
        public AudioClip AttackAudioClip;
        [Range(0, 1)] public float AttackAudioVolume = 0.5f;
        public AudioClip TakeDamageAudioClip;
        [Range(0, 1)] public float TakeDamageAudioVolume = 0.5f;
        public AudioClip DeathAudioClip;
        [Range(0, 1)] public float DeathAudioVolume = 0.5f;

        private NavMeshAgent agent;
        private Transform playerTransform;

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

        protected override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();

            // Debug.Log($"[Creature] {gameObject.name} инициализирован, здоровье: {GetHealth()}, maxHealth: {GetMaxHealth()}");

            if (_animator != null)
            {
                _animIDSpeed = Animator.StringToHash("Speed");
                _animIDIsMoving = Animator.StringToHash("IsMoving");
                _animIDAttack = Animator.StringToHash("Attack");
            }

            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }

            // Подписка на смерть
            // OnDeath += HandleDeath;
        }

        public void SetTarget(Transform target)
        {
            playerTransform = target;
        }

        void Update()
        {
            if (!IsAlive() || agent == null)
            {
                // // Добавим логгирование для отладки
                // if (!IsAlive())
                // {
                //     Debug.Log($"[Creature] {gameObject.name} мертв, обновление остановлено");
                // }
                // if (agent == null)
                // {
                //     Debug.Log($"[Creature] {gameObject.name} не имеет NavMeshAgent, обновление остановлено");
                // }
                return;
            }

            if (playerTransform == null)
            {
                SetState(CreatureState.Wander);
                HandleWandering();
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (isAttacking)
            {
                // Во время атаки не меняем состояние
                // Debug.Log($"[Creature] {gameObject.name} находится в состоянии атаки");
            }
            else if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                SetState(CreatureState.Attack);
                PerformAttack();
            }
            else if (distanceToPlayer <= aggressionRadius)
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
        }


        // Проверка достижения цели (вызывается в Update, если NavMeshAgent.enabled)
        void LateUpdate()
        {
            if (isWandering && agent != null && agent.isOnNavMesh && agent.remainingDistance < agent.stoppingDistance)
            {
                isWandering = false;
                // Debug.Log($"[{gameObject.name}] Остановился");
            }
        }

        private void SetState(CreatureState newState)
        {
            if (currentState == newState) return;
            currentState = newState;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                switch (currentState)
                {
                    case CreatureState.Chase:
                        agent.speed = chaseSpeed;
                        agent.isStopped = false;
                        break;
                    case CreatureState.Wander:
                        agent.speed = walkSpeed;
                        agent.isStopped = false;
                        break;
                    case CreatureState.Attack:
                        agent.isStopped = true;
                        break;
                }
            }
        }

        protected override void Die()
        {
            // 1. Сначала отменяем всё, что может помешать
            CancelInvoke();

            // 2. Отключаем аниматор ДО включения физики
            // if (animator != null)
            // {
            //     animator.enabled = false;
            //     animator.Update(0f); // Принудительно обновляем, чтобы сбросить позу
            // }

            // 3. Отключаем навмеш и коллайдер тела
            var agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            // var mainCollider = GetComponent<Collider>();
            // if (mainCollider != null) mainCollider.enabled = false;

            // 4. Активируем рэгдолл с гашением скоростей
            ActivateRagdoll();
            StartCoroutine(StopMovingRagdoll());


            base.Die(); // Вызываем базовую логику смерти
        }

        public void ActivateRagdoll()
        {
            foreach (var part in ragdollSettings.ragdollParts)
            {
                if (part == null) continue;
                var rb = part.GetComponent<Rigidbody>();
                if (rb == null) continue;

                // Включаем физику
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                // Сбрасываем скорости
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.linearDamping = 5f;
                rb.angularDamping = 5f;

                // Ограничения физики
                rb.maxAngularVelocity = 5f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                // Игнорируем коллизии между частями робота
                foreach (var otherPart in ragdollSettings.ragdollParts)
                {
                    if (part != otherPart && otherPart.TryGetComponent<Collider>(out var otherCol))
                    {
                        Physics.IgnoreCollision(rb.GetComponent<Collider>(), otherCol, true);
                    }
                }

                if (playerTransform != null)
                {
                    if (playerTransform.TryGetComponent<Collider>(out var playerCol))
                    {
                        Physics.IgnoreCollision(rb.GetComponent<Collider>(), playerCol, true);
                    }
                }
            }
        }

        public void DeactivateRagdoll()
        {
            foreach (var part in ragdollSettings.ragdollParts)
            {
                if (part == null) continue;
                var rb = part.GetComponent<Rigidbody>();
                if (rb != null && rb.isKinematic == false)
                {
                    rb.isKinematic = true;

                    // Если скорость маленькая, "замораживаем" физику полностью
                    if (rb.linearVelocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
                    {
                        rb.Sleep(); // Переводит Rigidbody в спящий режим (физика не просчитывается)
                    }
                }

                if (playerTransform != null)
                {
                    if (playerTransform.TryGetComponent<Collider>(out var playerCol))
                    {
                        Physics.IgnoreCollision(rb.GetComponent<Collider>(), playerCol, false);
                    }
                }
            }
        }



        private System.Collections.IEnumerator StopMovingRagdoll()
        {
            // Ждем, пока робот упадет и немного подергается
            yield return new WaitForSeconds(2.5f);

            DeactivateRagdoll();
            CreateCorpseInventory();

            // Включаем меню
            var menu = GetComponent<RadialMenu>();
            if (menu != null) menu.enabled = true;
        }


        private void HandleChasing()
        {
            if (!agent.isOnNavMesh) return;
            agent.SetDestination(playerTransform.position);
        }

        private void HandleWandering()
        {
            if (Time.time >= nextWanderTime)
            {
                StartWandering();
            }

            if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
            {
                if (agent.velocity.magnitude < 0.1f)
                {
                    _animator?.SetBool(_animIDIsMoving, false);
                }
            }
        }

        private void PerformAttack()
        {
            // Debug.Log($"[Creature] {gameObject.name} начинает атаку игрока");
            isAttacking = true;
            lastAttackTime = Time.time;

            if (_animator != null)
            {
                _animator.SetTrigger(_animIDAttack);
            }

            Invoke(nameof(DealDamageToPlayer), 0.5f);

            Invoke(nameof(ResetAttackState), attackCooldown);
        }

        private void DealDamageToPlayer()
        {
            // Debug.Log($"[Creature] {gameObject.name} наносит урон игроку: {attackDamage}");
            PlayerSurvivalSystem.Instance?.TakeDamage(attackDamage);
        }

        private void ResetAttackState()
        {
            isAttacking = false;
            if (playerTransform != null &&
                Vector3.Distance(transform.position, playerTransform.position) <= aggressionRadius)
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
                float currentSpeed = agent.velocity.magnitude;
                _animator.SetFloat(_animIDSpeed, currentSpeed);
                _animator.SetBool(_animIDIsMoving, currentSpeed > 0.1f);
            }
        }

        private void StartWandering()
        {
            if (!agent.isOnNavMesh) return;

            isWandering = true;

            Vector3 randomDirection = Random.insideUnitSphere * wanderRange;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRange, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            float delay = Random.Range(minWanderDelay, maxWanderDelay);
            nextWanderTime = Time.time + delay;
        }

        // Вызывается, когда NavMeshAgent достигает цели
        void OnDestinationReached()
        {
            if (agent.remainingDistance < agent.stoppingDistance)
            {
                isWandering = false;
                // Debug.Log($"[{gameObject.name}] Добрался до места");
                // Здесь можно добавить анимацию остановки
            }
        }

        // private void HandleDeath(BaseLivingEntity entity)
        // {
        //     Debug.Log("[Creature] Существо умерло. Создаем инвентарь...");

        //     // 1. Создаем инвентарь
        //     var chestInv = gameObject.AddComponent<ChestInventory>();
        //     string corpseKey = $"Corpse_{System.Guid.NewGuid().ToString()}";
        //     chestInv.Initialize(12, corpseKey);

        //     Debug.Log($"[Creature] Инвентарь создан. Размер Data: {chestInv.Data?.slots.Count ?? 0}");

        //     // 2. Заполняем случайным лутом
        //     PopulateInventory(chestInv);

        //     // 3. Настраиваем Corpse
        //     var corpse = GetComponent<Corpse>();
        //     if (corpse == null)
        //     {
        //         corpse = gameObject.AddComponent<Corpse>();
        //     }

        //     // 🔥 Инициализируем данные, но НЕ включаем компонент!
        //     var chestUI = FindAnyObjectByType<ChestUI>();
        //     corpse.Initialize(chestInv, chestUI, harvestDrops, maxHarvestHits);

        //     // 🔥 ВАЖНО: Отключаем Corpse до конца анимации смерти
        //     corpse.enabled = false;
        // }

        private void CreateCorpseInventory()
        {
            // 1. Создаем инвентарь
            var chestInv = gameObject.AddComponent<ChestInventory>();
            string corpseKey = $"Corpse_{System.Guid.NewGuid().ToString()}";
            chestInv.Initialize(12, corpseKey);

            // 2. Заполняем случайным лутом
            PopulateInventory(chestInv);

            // 3. Настраиваем Corpse
            var corpse = GetComponent<Corpse>();
            if (corpse == null)
            {
                corpse = gameObject.AddComponent<Corpse>();
            }
            corpse.enabled = true;

            // Передаем настройки из Creature в Corpse
            var chestUI = FindAnyObjectByType<ChestUI>();
            corpse.Initialize(chestInv, chestUI, harvestDrops, maxHarvestHits);
        }

        private void PopulateInventory(ChestInventory inv)
        {
            if (inventoryLootTable == null || inv == null)
            {
                Debug.LogWarning("[Creature] inventoryLootTable или inv равны null!");
                return;
            }

            var data = inv.Data;
            if (data == null)
            {
                Debug.LogError("[Creature] inv.Data равна null! Ячейки не созданы.");
                return;
            }

            foreach (var entry in inventoryLootTable)
            {
                if (entry.item == null) continue;

                float roll = Random.value;

                if (roll <= entry.dropChance)
                {
                    int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                    data.AddItemAnywhere(entry.item, amount);
                }
            }
        }
        protected override float GetMaxHealthFromConfiguration()
        {
            return _maxHealth;
        }

        protected override float GetMaxStaminaFromConfiguration()
        {
            return _maxStamina;
        }

        // Вызывается в событии анимации!
        private void SoundOnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                PlaySound(FootstepAudioClip, FootstepAudioVolume);
            }
        }

        // Вызывается в событии анимации!
        private void SoundOnAttack(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                PlaySound(AttackAudioClip, AttackAudioVolume);
            }
        }

        // Вызывается в событии анимации!
        private void SoundOnTakeDamage(AnimationEvent animationEvent)
        {
            // Debug.Log("SoundOnTakeDamage: " + animationEvent.animatorClipInfo.weight);
            if (animationEvent.animatorClipInfo.weight > 0.2f)
            {
                PlaySound(TakeDamageAudioClip, TakeDamageAudioVolume);
            }
        }

        // Вызывается в событии анимации!
        private void SoundOnDeath(AnimationEvent animationEvent)
        {
            // Debug.Log("SoundOnDeath: " + animationEvent.animatorClipInfo.weight);
            if (animationEvent.animatorClipInfo.weight > 0.2f)
            {
                PlaySound(DeathAudioClip, DeathAudioVolume);
            }
        }

        private void PlaySound(AudioClip audioClip, float audioClipVolume)
        {
            if (audioClip != null)
            {
                // Получаем глобальную громкость
                float globalVolume = 1f;
                if (AudioManager.Instance != null)
                {
                    globalVolume = AudioManager.Instance.masterVolume;
                }

                // Итоговая громкость = Глобальная * Настройка существа
                float finalVolume = globalVolume * audioClipVolume;

                // Создаем источник вручную с правильной громкостью
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

        // void OnDestroy()
        // {
        //     OnDeath -= HandleDeath;
        // }
    }

    [System.Serializable]
    public class RagdollSettings
    {
        public Transform[] ragdollParts; // Перетащи все кости рэгдолла в инспекторе
        [Header("Физика после смерти")]
        public float linearDamp = 0.5f;  // Гашение линейной скорости
        public float angularDamp = 2f;   // Гашение вращения (важно!)
        public float massMultiplier = 0.5f; // Уменьшаем массу для мягкого падения
        [Range(0, 1)] public float velocityInherit = 0.1f; // 0 = игнорировать анимацию, 1 = сохранять
    }

}