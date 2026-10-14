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

        public virtual string GetStatusText()
        {
            if (tamed) return "Tamed";

            if (tamableKO && tamablePassive) return "Wild · Tamable (KO / Passive)";
            if (tamableKO) return "Wild · Tamable (KO)";
            if (tamablePassive) return "Wild · Tamable (Passive)";

            return "Wild";
        }

        public virtual string GetBehaviorText() => TamedBehaviorToText();

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

            // Дефолтные команды
            tamedFollowing = true;
            tamedWandering = false;
            tamedMating = false;
            tamedBehavior = TamedBehavior.AttackOwnerTarget;
            tamedFollowDistance = FollowDistance.Medium;

            ExitKnockoutState();
            SetCreatureLayer();

            // Прирученное существо следует за игроком, а не за "целью".
            // _playerTransform в Creature должен указывать на игрока.
            // if (this is Creature c) c.SetTarget(null);

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
            Heal(amount);
        }

        public bool ConsumeFood(float amount)
        {
            food = Mathf.Max(0f, food - amount);
            return food <= 0f;
        }

        // ==========================================
        // === КОМАНДЫ ПРИРУЧЕННОГО ===
        // ==========================================

        /// <summary>Включить/выключить следование. Выключает блуждание (взаимоисключающе).</summary>
        public void SetTamedFollowing(bool value)
        {
            if (!tamed) return;
            tamedFollowing = value;
            if (value) tamedWandering = false;
        }

        /// <summary>Включить/выключить блуждание. Выключает следование (взаимоисключающе).</summary>
        public void SetTamedWandering(bool value)
        {
            if (!tamed) return;
            tamedWandering = value;
            if (value)
            {
                tamedFollowing = false;
                if (this is Creature c) c.ResetWanderState();
            }
        }

        /// <summary>Включить/выключить спаривание.</summary>
        public void SetTamedMating(bool value)
        {
            if (!tamed) return;
            tamedMating = value;
        }

        /// <summary>Установить отношение к врагам.</summary>
        public void SetTamedBehavior(TamedBehavior value)
        {
            if (!tamed) return;
            tamedBehavior = value;
        }

        /// <summary>Установить дистанцию следования.</summary>
        public void SetFollowDistance(FollowDistance value)
        {
            if (!tamed) return;
            tamedFollowDistance = value;
        }


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


        // ==========================================
        // === RADIAL MENU ACTIONS ===
        // ==========================================

        /// <summary>Выполнить действие из радиального меню.</summary>
        public void ExecuteRadialAction(string actionId)
        {
            switch (actionId)
            {
                case "toggle_follow":
                    SetTamedFollowing(!tamedFollowing);
                    Debug.Log($"[{gameObject.name}] Follow = {tamedFollowing}");
                    break;

                case "toggle_wander":
                    SetTamedWandering(!tamedWandering);
                    Debug.Log($"[{gameObject.name}] Wander = {tamedWandering}");
                    break;

                case "toggle_mating":
                    SetTamedMating(!tamedMating);
                    Debug.Log($"[{gameObject.name}] Mating = {tamedMating}");
                    break;

                case "open_inventory":
                    OpenInventory();
                    Debug.Log($"[{gameObject.name}] OpenInventory");
                    break;


                case "flee":
                    SetTamedBehavior(TamedBehavior.Flee);
                    Debug.Log($"[{gameObject.name}] TamedBehavior = Flee");
                    break;

                case "passive":
                    SetTamedBehavior(TamedBehavior.Passive);
                    Debug.Log($"[{gameObject.name}] TamedBehavior = Passive");
                    break;

                case "neutral":
                    SetTamedBehavior(TamedBehavior.Neutral);
                    Debug.Log($"[{gameObject.name}] TamedBehavior = Aggressive");
                    break;

                case "attack_owner_target":
                    SetTamedBehavior(TamedBehavior.AttackOwnerTarget);
                    Debug.Log($"[{gameObject.name}] TamedBehavior = AttackOwnerTarget");
                    break;

                case "aggressive":
                    SetTamedBehavior(TamedBehavior.Aggressive);
                    Debug.Log($"[{gameObject.name}] TamedBehavior = Aggressive");
                    break;


                case "pick_up":
                    // TODO
                    Debug.Log($"[{gameObject.name}] Pick up (TODO)");
                    break;

                case "ride":
                    // TODO
                    Debug.Log($"[{gameObject.name}] Ride (TODO)");
                    break;

                case "rename":
                    // TODO
                    Debug.Log($"[{gameObject.name}] Rename (TODO)");
                    break;

                case "unclaim":
                    Unclaim();
                    break;

                default:
                    Debug.LogWarning($"[{gameObject.name}] Неизвестное действие: {actionId}");
                    break;
            }

            // Сохраняем изменения (для прирученных)
            if (tamed && !string.IsNullOrEmpty(TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UpdateSave(TamingInstanceId, "player_001");
            }
        }

        /// <summary>Текущее значение Radio-группы.</summary>
        public string GetRadialRadioValue(string radioGroupId)
        {
            switch (radioGroupId)
            {
                case "behavior":
                    return TamedBehaviorToId(tamedBehavior);
                case "follow_distance":
                    return FollowDistanceToId(tamedFollowDistance);
                default:
                    return null;
            }
        }

        /// <summary>Установить значение Radio-группы.</summary>
        public void SetRadialRadioValue(string radioGroupId, string radioValue)
        {
            switch (radioGroupId)
            {
                case "behavior":
                    tamedBehavior = IdToTamedBehavior(radioValue);
                    Debug.Log($"[{gameObject.name}] Behavior = {tamedBehavior}");
                    break;
                case "follow_distance":
                    tamedFollowDistance = IdToFollowDistance(radioValue);
                    Debug.Log($"[{gameObject.name}] FollowDistance = {tamedFollowDistance}");
                    break;
            }

            if (tamed && !string.IsNullOrEmpty(TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UpdateSave(TamingInstanceId, "player_001");
            }
        }

        /// <summary>Динамический текст Action-кнопки.</summary>
        public string GetRadialDynamicLabel(string actionId)
        {
            switch (actionId)
            {
                case "toggle_follow":
                    return tamedFollowing ? "Отключить следование" : "Включить следование";
                case "toggle_wander":
                    return tamedWandering ? "Отключить блуждание" : "Включить блуждание";
                case "toggle_mating":
                    return tamedMating ? "Отключить спаривание" : "Включить спаривание";
                default:
                    return null;
            }
        }

        // === Хелперы для enum ↔ string ===

        private static string TamedBehaviorToId(TamedBehavior value)
        {
            switch (value)
            {
                case TamedBehavior.Passive: return "passive";
                case TamedBehavior.Flee: return "flee";
                case TamedBehavior.Neutral: return "neutral";
                case TamedBehavior.AttackOwnerTarget: return "attack_owner_target";
                case TamedBehavior.Aggressive: return "aggressive";
                default: return "neutral";
            }
        }

        private static TamedBehavior IdToTamedBehavior(string id)
        {
            switch (id)
            {
                case "passive": return TamedBehavior.Passive;
                case "flee": return TamedBehavior.Flee;
                case "neutral": return TamedBehavior.Neutral;
                case "attack_owner_target": return TamedBehavior.AttackOwnerTarget;
                case "aggressive": return TamedBehavior.Aggressive;
                default: return TamedBehavior.Neutral;
            }
        }

        public string TamedBehaviorToText()
        {
            var tamedBehaviorId = TamedBehaviorToId(tamedBehavior);
            if (TryGetComponent<RadialMenu>(out var menu))
            {
                var tamedBehaviorText = menu.GetMenuButtonLabelByActionId(tamedBehaviorId);
                if (tamedBehaviorText != "") return tamedBehaviorText;
            }

            return tamedBehaviorId;
        }

        private static string FollowDistanceToId(FollowDistance value)
        {
            switch (value)
            {
                case FollowDistance.Lowest: return "lowest";
                case FollowDistance.Low: return "low";
                case FollowDistance.Medium: return "medium";
                case FollowDistance.High: return "high";
                case FollowDistance.Highest: return "highest";
                default: return "medium";
            }
        }

        private static FollowDistance IdToFollowDistance(string id)
        {
            switch (id)
            {
                case "lowest": return FollowDistance.Lowest;
                case "low": return FollowDistance.Low;
                case "medium": return FollowDistance.Medium;
                case "high": return FollowDistance.High;
                case "highest": return FollowDistance.Highest;
                default: return FollowDistance.Medium;
            }
        }

        /// <summary>Снять приручение — существо становится диким.</summary>
        public void Unclaim()
        {
            if (!tamed) return;

            tamed = false;
            tamedFollowing = false;
            tamedWandering = false;
            tamedMating = false;
            tamedBehavior = TamedBehavior.Neutral;
            tamedFollowDistance = FollowDistance.Medium;
            tamingProgress = 0f;

            // Удаляем сейв прирученного
            if (!string.IsNullOrEmpty(TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UnregisterCreature(TamingInstanceId);
                TamingInstanceId = "";
            }

            // Удаляем инвентарь приручения
            if (GetInventory() != null)
            {
                Destroy(GetInventory());
                SetInventory(null);
            }

            SetCreatureLayer();  // возвращаем слой Creature (он и так Creature, но на всякий)

            Debug.Log($"[{gameObject.name}] Unclaimed — стал диким.");
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

        /// <summary>Прямое добавление торпора (без нокаута). Используется CreatureCombat.</summary>
        public void AddTorporDirect(float amount) => torpor = Mathf.Min(maxTorpor, torpor + amount);

        /// <summary>Здоровье ниже нуля?</summary>
        public bool IsHealthBelowZero() => health <= 0;

        /// <summary>Установить dead-флаг (используется CreatureDeath).</summary>
        public void SetDead() { _isDead = true; knockedOut = false; health = 0; }


        // BaseLivingEntity
        // public virtual void TakeDamage(float damage, float torporAmount = 0f) { }
        // public virtual void TakeDamage(float damage, PlayerInteraction pi) { }
        // public virtual void TakeDamage(float damage, float torporAmount, PlayerInteraction pi) { }
        // public virtual void TakeDamage(float damage, float torporAmount, PlayerInteraction pi, Collider hit, Vector3? point) { }
    }
}