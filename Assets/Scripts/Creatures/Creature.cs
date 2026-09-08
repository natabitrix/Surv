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

        // [Header("Ragdoll")]
        // public RagdollSettings ragdollSettings;

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
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _maxStamina = 50f;

        [System.Serializable]
        public class LootEntry
        {
            public Item item;
            public int minAmount = 1;
            public int maxAmount = 1;
            [Range(0f, 1f)] public float dropChance = 1f;
        }

        // [Header("Loot & Harvesting")]
        // [Tooltip("Предметы, которые рандомно попадут в инвентарь трупа (мясо, шкуры, детали)")]
        // public LootEntry[] inventoryLootTable;

        // [Tooltip("Ресурсы, которые выпадают при разбивании тела (железо, электроника, кости)")]
        // public Corpse.ResourceDrop[] harvestDrops;

        // [Tooltip("Сколько ударов нужно, чтобы полностью разобрать тело")]
        // public int maxHarvestHits = 5;

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
        }

        public void SetTarget(Transform target)
        {
            playerTransform = target;
        }

        void Update()
        {
            if (!IsAlive() || agent == null)
            {
                return;
            }

            if (playerTransform == null)
            {
                SetState(CreatureState.Wander);
                HandleWandering();
                return;
            }

            if (torpor > 0) torpor = Mathf.Max(0, torpor - torporRecoveryRate * Time.deltaTime);

            if (torpor >= maxTorpor && !knockedOut)
            {
                knockedOut = true;
                agent.isStopped = true;
            }
            
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (isAttacking)
            {
                // Во время атаки не меняем состояние
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

        void LateUpdate()
        {
            if (isWandering && agent != null && agent.isOnNavMesh && agent.remainingDistance < agent.stoppingDistance)
            {
                isWandering = false;
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

        // protected override void Die()
        // {
        //     CancelInvoke();

        //     // var agent = GetComponent<NavMeshAgent>();
        //     // if (agent != null) agent.enabled = false;

        //     ActivateRagdoll();
        //     StartCoroutine(StopMovingRagdoll());

        //     base.Die();
        // }

        // public void ActivateRagdoll()
        // {
        //     foreach (var part in ragdollSettings.ragdollParts)
        //     {
        //         if (part == null) continue;
        //         var rb = part.GetComponent<Rigidbody>();
        //         if (rb == null) continue;

        //         rb.isKinematic = false;
        //         rb.useGravity = true;
        //         rb.interpolation = RigidbodyInterpolation.Interpolate;

        //         rb.linearVelocity = Vector3.zero;
        //         rb.angularVelocity = Vector3.zero;
        //         rb.linearDamping = 5f;
        //         rb.angularDamping = 5f;

        //         rb.maxAngularVelocity = 5f;
        //         rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        //         foreach (var otherPart in ragdollSettings.ragdollParts)
        //         {
        //             if (part != otherPart && otherPart.TryGetComponent<Collider>(out var otherCol))
        //             {
        //                 Physics.IgnoreCollision(rb.GetComponent<Collider>(), otherCol, true);
        //             }
        //         }

        //         if (playerTransform != null)
        //         {
        //             if (playerTransform.TryGetComponent<Collider>(out var playerCol))
        //             {
        //                 Physics.IgnoreCollision(rb.GetComponent<Collider>(), playerCol, true);
        //             }
        //         }
        //     }
        // }

        // public void DeactivateRagdoll()
        // {
        //     foreach (var part in ragdollSettings.ragdollParts)
        //     {
        //         if (part == null) continue;
        //         var rb = part.GetComponent<Rigidbody>();
        //         if (rb != null && rb.isKinematic == false)
        //         {
        //             rb.isKinematic = true;

        //             if (rb.linearVelocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
        //             {
        //                 rb.Sleep();
        //             }
        //         }

        //         if (playerTransform != null)
        //         {
        //             if (playerTransform.TryGetComponent<Collider>(out var playerCol))
        //             {
        //                 Physics.IgnoreCollision(rb.GetComponent<Collider>(), playerCol, false);
        //             }
        //         }
        //     }
        // }

        // private System.Collections.IEnumerator StopMovingRagdoll()
        // {
        //     yield return new WaitForSeconds(2.5f);

        //     DeactivateRagdoll();

        //     CreateCorpseInventory();

        //     var menu = GetComponent<RadialMenu>();
        //     if (menu != null) menu.enabled = true;
        // }

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

        void OnDestinationReached()
        {
            if (agent.remainingDistance < agent.stoppingDistance)
            {
                isWandering = false;
            }
        }

        // void CreateCorpseInventory()
        // {
        //     var chestInv = gameObject.AddComponent<ChestInventory>();
        //     string corpseKey = $"Corpse_{System.Guid.NewGuid().ToString()}";
        //     chestInv.Initialize(100, corpseKey);

        //     PopulateInventory(chestInv);

        //     var corpse = GetComponent<Corpse>();
        //     corpse.enabled = true;

        //     var chestUI = FindAnyObjectByType<ChestUI>();
        //     corpse.Initialize(chestInv, chestUI, harvestDrops, maxHarvestHits);

        //     Debug.Log($"[Creature] Труп {gameObject.name} создан на {transform.position}");
        // }

        // private void PopulateInventory(ChestInventory inv)
        // {
        //     if (inventoryLootTable == null || inv == null)
        //     {
        //         Debug.LogWarning("[Creature] inventoryLootTable или inv равны null!");
        //         return;
        //     }

        //     var data = inv.Data;
        //     if (data == null)
        //     {
        //         Debug.LogError("[Creature] inv.Data равна null! Ячейки не созданы.");
        //         return;
        //     }

        //     foreach (var entry in inventoryLootTable)
        //     {
        //         if (entry.item == null) continue;

        //         float roll = Random.value;

        //         if (roll <= entry.dropChance)
        //         {
        //             int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
        //             data.AddItemAnywhere(entry.item, amount);
        //         }
        //     }
        // }

        protected override float GetMaxHealthFromConfiguration()
        {
            return _maxHealth;
        }

        protected override float GetMaxStaminaFromConfiguration()
        {
            return _maxStamina;
        }

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

    // [System.Serializable]
    // public class RagdollSettings
    // {
    //     public Transform[] ragdollParts;
    //     [Header("Физика после смерти")]
    //     public float linearDamp = 0.5f;
    //     public float angularDamp = 2f;
    //     public float massMultiplier = 0.5f;
    //     [Range(0, 1)] public float velocityInherit = 0.1f;
    // }
}