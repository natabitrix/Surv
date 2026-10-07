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

        public string TamingInstanceId { get; set; } = "";

        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        public virtual string GetDisplayName() => gameObject.name;

        public virtual string GetStatusText()
        {
            if (tamed) return "Tamed";

            if (tamableKO && tamablePassive) return "Wild · Tamable (KO / Passive)";
            if (tamableKO) return "Wild · Tamable (KO)";
            if (tamablePassive) return "Wild · Tamable (Passive)";

            return "Wild";
        }

        public virtual float GetDamageMultiplier(Collider hitCollider) => 1f;

        // ==========================================
        // === ИНВЕНТАРЬ ===
        // ==========================================
        [SerializeField] private ChestInventory _inventory;
        private bool _isOpen = false;

        [Header("Debug")]
        [SerializeField] private bool _debugDamage = false;

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
        public InteractType GetInteractType() => tamed || (tamableKO && knockedOut) ? InteractType.OpenTargetInventory : InteractType.None;
        public InteractType GetInteractType2() => tamable && tamablePassive ? InteractType.Interact : InteractType.None;

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
        // === УРОН ===
        // ==========================================

        public virtual void TakeDamage(float damage, PlayerInteraction playerInteraction)
            => TakeDamage(damage, 0f, playerInteraction, null, null);

        public virtual void TakeDamage(float damage, float torporAmount, PlayerInteraction playerInteraction)
            => TakeDamage(damage, torporAmount, playerInteraction, null, null);

        public virtual void TakeDamage(float damage, float torporAmount, PlayerInteraction playerInteraction, Collider hitCollider = null, Vector3? hitPoint = null)
        {
            Collider col = hitCollider ?? playerInteraction?.GetTargetHitCollider();

            float multiplier = GetDamageMultiplier(col);
            float finalDamage = damage * multiplier;

            health -= finalDamage;

            Vector3 popupPos;
            if (hitPoint.HasValue)
                popupPos = hitPoint.Value;
            else if (playerInteraction != null)
                popupPos = playerInteraction.GetTargetHitPosition();
            else
                popupPos = transform.position + Vector3.up;

            if (finalDamage > 0f)
            {
                DamageNumberPool.Instance?.ShowDamage(finalDamage, popupPos);
            }

            if (torporAmount > 0f && !_isDead)
            {
                torpor = Mathf.Min(maxTorpor, torpor + torporAmount);

                if (!knockedOut && torpor >= maxTorpor)
                {
                    KnockOut();
                }
            }

            if (animator != null && finalDamage > 0)
            {
                animator.SetTrigger(animIDTakeDamage);
            }

            if (damageEffect != null && finalDamage > 0)
            {
                PlayDamageEffect(popupPos);
            }

            if (_debugDamage)
            {
                string zoneInfo = "default x1";
                if (col == null)
                    zoneInfo = "col=NULL";
                else if (col.TryGetComponent<CreatureHitZone>(out var zone))
                    zoneInfo = $"{zone.label} x{zone.damageMultiplier}";
                else
                    zoneInfo = $"col={col.name} (no zone)";

                Debug.Log($"[{gameObject.name}] Zone: {zoneInfo};  Damage: {finalDamage:F1} (base {damage:F1});  Collider passed={(hitCollider != null ? hitCollider.name : "null")}");
            }

            if (health <= 0)
            {
                Die();
            }
        }

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

        protected virtual void Die()
        {
            if (_isDead) return;
            _isDead = true;

            knockedOut = false;
            health = 0;

            // Закрываем инвентарь, если был открыт
            if (ChestUIManager.Instance != null && ReferenceEquals(ChestUIManager.Instance.CurrentSource, this))
            {
                ChestUIManager.Instance.Close();
            }

            if (!string.IsNullOrEmpty(TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UnregisterCreature(TamingInstanceId);
                TamingInstanceId = "";
            }

            if (_inventory != null)
            {
                Destroy(_inventory);
                _inventory = null;
            }

            CancelInvoke();
            CreateCorpse();
            SetBodyRagdoll();
            OnDeath?.Invoke(this);
        }

        private void SetBodyRagdoll(bool destroyAgent = false)
        {
            if (animator != null)
            {
                animator.WriteDefaultValues();
                animator.enabled = false;
            }

            if (ragdollController != null)
            {
                ragdollController.ActivateRagdoll();
                StartCoroutine(ragdollController.StopMovingRagdoll());
            }

            var agent = GetComponent<NavMeshAgent>();

            if (agent != null)
            {
                if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
                agent.enabled = false;
                if (destroyAgent)
                {
                    Destroy(agent);
                }
            }
        }

        private void CreateCorpse()
        {
            var creature = GetComponent<Creature>();
            if (creature == null)
            {
                Debug.LogError("[CreateCorpse] Не найден Creature!");
                return;
            }

            if (creature.Data == null)
            {
                Debug.LogError("[CreateCorpse] Не найден creature.Data!");
                return;
            }

            int corpseLayer = LayerMask.NameToLayer("Corpse");
            if (corpseLayer != -1) SetLayerRecursively(gameObject, corpseLayer);

            var corpse = GetComponent<Corpse>();
            if (corpse == null) return;

            corpse.enabled = true;
            string creatureId = null;

            if (creature != null && creature.Data != null)
            {
                var data = creature.Data;
                creatureId = creature.creatureId;
                corpse.InstanceId = creatureId;

                corpse.harvestDrops = data.harvestDrops;
                corpse.inventoryLootTable = data.inventoryLootTable;
                corpse.maxHarvestHits = data.maxHarvestHits;
                corpse.allowFists = data.allowFists;
                corpse.allowAxe = data.allowAxe;
                corpse.allowPickaxe = data.allowPickaxe;
                corpse.allowSword = data.allowSword;
                corpse.allowSickle = data.allowSickle;
            }

            corpse.InitializeCorpse();

            corpse.CreateCorpseInventory(
                "CreatureCorpse",
                100
            );

            corpse.PopulateInventoryFromLootTable();

            if (corpse.TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;

            if (CorpseManager.Instance != null)
            {
                CorpseManager.Instance.RegisterCorpse(
                    gameObject,
                    "world",
                    creatureId
                );
            }
        }

        public virtual void KnockOut()
        {
            if (_isDead || knockedOut) return;

            if (TamingManager.Instance == null)
            {
                Debug.LogError("[KnockOut] TamingManager.Instance == null!");
                return;
            }

            knockedOut = true;

            SetBodyRagdoll(false);

            if (TryGetComponent<Corpse>(out var corpse))
                corpse.enabled = false;

            if (TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;

            if (_inventory == null)
            {
                var chestInv = gameObject.AddComponent<ChestInventory>();
                string key = $"TamingCorpse_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
                chestInv.Initialize(100, key);
                _inventory = chestInv;
            }

            if (string.IsNullOrEmpty(TamingInstanceId))
            {
                TamingInstanceId = TamingManager.Instance.RegisterCreature(gameObject, "player_001");
            }

        }

        public virtual void KnockOutFromLoad()
        {
            knockedOut = true;
            SetBodyRagdoll(false);
            SetCreatureLayer();
            if (TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;
        }

        public virtual void TamedFromLoad()
        {
            tamed = true;
            knockedOut = false;
            SetCreatureLayer();
            if (TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;
        }

        public virtual void RecoverFromKnockout()
        {
            if (_isDead) return;
            if (!knockedOut) return;
            ExitKnockoutState();
        }

        protected virtual void ExitKnockoutState()
        {
            knockedOut = false;
            torpor = 0f;
            Debug.Log($"[{gameObject.name}] ВЫШЕЛ ИЗ НОКАУТА");

            if (!tamed && !string.IsNullOrEmpty(TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UnregisterCreature(TamingInstanceId);
                TamingInstanceId = "";
            }

            if (ragdollController != null)
                ragdollController.DeactivateRagdoll();

            if (TryGetComponent<NavMeshAgent>(out var agent))
            {
                if (!agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
                        agent.Warp(hit.position);
                    else
                        Debug.LogWarning($"[{gameObject.name}] Не удалось вернуть на NavMesh после нокаута!");
                }

                agent.enabled = true;
                if (agent.isOnNavMesh) agent.isStopped = false;
            }

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.enabled = true;
                animator.SetFloat(animIDSpeed, 0f);
                animator.SetBool(animIDIsMoving, false);
            }

            if (TryGetComponent<Creature>(out var creature))
                creature.enabled = true;

            if (TryGetComponent<Corpse>(out var corpse))
                corpse.enabled = false;

            if (TryGetComponent<RadialMenu>(out var menu))
                menu.enabled = false;

            CloseInventory();

            if (!tamed && _inventory != null)
            {
                Destroy(_inventory);
                _inventory = null;
            }
        }

        public virtual void Heal(float amount)
        {
            health = Mathf.Clamp(health + amount, 0, maxHealth);
        }

        protected void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        // ==========================================
        // === ПРИРУЧЕНИЕ ===
        // ==========================================

        public bool AddTamingProgress(float amount)
        {
            if (tamed) return true;

            tamingProgress = Mathf.Min(100f, tamingProgress + amount);
            if (tamingProgress >= 100f)
            {
                FinishTaming();
                return true;
            }
            return false;
        }

        protected virtual void FinishTaming()
        {
            tamed = true;
            tamingProgress = 100f;

            ExitKnockoutState();
            SetCreatureLayer();

            if (this is Creature c) c.SetTarget(null);

            if (!string.IsNullOrEmpty(TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UpdateSave(TamingInstanceId, "player_001");
            }

            if (TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;

            // === Уведомление ===
            NotificationManager.Instance?.ShowTopNote(
                NotificationType.TameComplete,
                $"{GetDisplayName()} приручен!"
            );

            Debug.Log($"[{gameObject.name}] Приручение завершено!");
        }

        public void AddTorpor(float amount)
        {
            torpor = Mathf.Min(maxTorpor, torpor + amount);
            if (!knockedOut && torpor >= maxTorpor)
            {
                KnockOut();
            }
        }

        public void AddFood(float amount)
        {
            food = Mathf.Min(maxFood, food + amount);
        }

        public bool ConsumeFood(float amount)
        {
            food = Mathf.Max(0f, food - amount);
            return food <= 0f;
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

        protected void SetCreatureLayer()
        {
            int creatureLayer = LayerMask.NameToLayer("Creature");
            if (creatureLayer != -1) SetLayerRecursively(gameObject, creatureLayer);
            else Debug.LogWarning("[BaseLivingEntity] Слой 'Creature' не найден!");
        }

        protected virtual void OnDestroy()
        {
            // Если этот источник был открыт — закрываем UI
            if (ChestUIManager.Instance != null && ReferenceEquals(ChestUIManager.Instance.CurrentSource, this))
            {
                ChestUIManager.Instance.Close();
            }
        }
    }
}