using UnityEngine;
using Assets.Scripts.Core;
using Assets.Scripts.Audio;
using Assets.Scripts.Player;
using Assets.Scripts.Effects;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Corpses;
using Assets.Scripts.UI;
using Assets.Scripts.Loot;
using Assets.Scripts.Items;
using Assets.Scripts.Creatures.Components;

namespace Assets.Scripts.Structures
{
    public class Structure : MonoBehaviour, IInteractable, IImpactSoundProvider, IInventorySource
    {
        [Header("Database")]
        [Tooltip("Ссылка на базу данных существ")]
        // public ItemDatabase itemDatabase;
        public Item item;

        [Tooltip("ID структуры")]
        public string structureId => item.Id;

        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        public virtual string GetDisplayName() => item.itemName;

        // ==========================================
        // === ИНВЕНТАРЬ ===
        // ==========================================
        [SerializeField] private ChestInventory _inventory;
        private bool _isOpen = false;

        // Ссылки на системы
        protected PlayerController playerController;
        protected PlayerProgress playerProgress;
        protected PlayerSurvivalSystem survivalSystem;

        public float health;
        public float maxHealth = 5000f;

        [Header("Destroy Visuals & Audio")]
        public ParticleSystem breakEffect;
        public Shatterer shatterer;
        public HitDecaler hitDecaler;
        public Animator animator;
        [SerializeField] private ImpactType _impactType = ImpactType.Wood;
        public ImpactType GetImpactType() => _impactType;

        // События
        public System.Action<Structure> OnDeath;
 
        protected virtual void Awake()
        {
            playerProgress = PlayerProgress.Instance;
            survivalSystem = PlayerSurvivalSystem.Instance;
            playerController = PlayerController.Instance;

            // if (playerProgress != null && playerProgress.playerController != null)
            //     playerController = playerProgress.playerController;

            // var assets = GameAssets.Instance;
            // if (assets != null)
            //     itemDatabase = assets.ItemDatabase;

            animator = GetComponent<Animator>();

            InitializeStats();
        }

        // ==========================================
        // === ИНТЕРФЕЙС IInteractable ===
        // ==========================================
        public InteractType GetInteractType() => HasInventory() ? InteractType.OpenTargetInventory : InteractType.None;
        public InteractType GetInteractType2() => InteractType.None;

        public void Interact(InteractContext context)
        {
            if (context.isTargetInventory)
            {
                OpenInventory();
            }

            if (context.IsAttack)
            {
                TakeDamage(context.PlayerInteraction);
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
                Debug.LogError("[Structure] ChestUIManager не найден!");
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
        protected void InitializeStats()
        {
            health = maxHealth;
        }

        // ==========================================
        // === УРОН ===
        // ==========================================

        // public virtual void TakeDamage(float damage, PlayerInteraction playerInteraction, Collider hitCollider = null, Vector3? hitPoint = null)
        public virtual void TakeDamage(PlayerInteraction playerInteraction)
        {
            float damage = playerController.GetAttackDamage();

            health -= damage;

            Vector3 popupPos;
            if (playerInteraction != null)
                popupPos = playerInteraction.GetTargetHitPosition();
            else
                popupPos = transform.position + Vector3.up;

            if (damage > 0f)
            {
                DamageNumberPool.Instance?.ShowDamage(damage, popupPos, CombatMember.Player, CombatMember.Tamed);
            }

            if (breakEffect != null && damage > 0)
            {
                Vector3 hitNorm = playerInteraction.GetTargetHitNormal();
                PlayBreakEffect(popupPos, hitNorm);
            }

            if (health <= 0)
            {
                Die();
            }
        }

        private void PlayBreakEffect(Vector3 hitPos, Vector3 hitNorm)
        {
            if (breakEffect != null)
            {
                var effectInstance = Instantiate(breakEffect, hitPos, transform.rotation);
                effectInstance.Play();
                Destroy(effectInstance.gameObject, effectInstance.main.duration + 1.5f);
            }

            shatterer?.Shatter();
            hitDecaler?.SpawnHitDecal(hitPos, hitNorm);
        }

        protected virtual void Die()
        {
            if (_isDead) return;
            _isDead = true;

            health = 0;

            // Закрываем инвентарь, если был открыт
            if (ChestUIManager.Instance != null && ReferenceEquals(ChestUIManager.Instance.CurrentSource, this))
            {
                ChestUIManager.Instance.Close();
            }

            if (animator != null) animator.SetTrigger("BreakLast");
            shatterer?.LastBreak();

            bool hasLoot = HasLootInInventory();

            if (hasLoot && _inventory.Data != null)
            {
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.DropAllItemsToBag(
                        _inventory.Data,
                        structureId,
                        false
                    );
                }
            }

            Destroy(gameObject, 0.5f);

            // if (_inventory != null)
            // {
            //     Destroy(_inventory);
            //     _inventory = null;
            // }

            CancelInvoke();
            OnDeath?.Invoke(this);
        }

        public virtual void Heal(float amount)
        {
            health = Mathf.Clamp(health + amount, 0, maxHealth);
        }

        private bool HasLootInInventory()
        {
            if (_inventory?.Data?.slots == null) return false;

            foreach (var slot in _inventory.Data.slots)
            {
                if (!slot.IsEmpty && slot.item != null)
                    return true;
            }

            return false;
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
            playerController.TryGetComponent(out ItemHandler itemHandler);

            switch (actionId)
            {
                case "pick_up":
                    if (itemHandler)
                    {
                        itemHandler.PickupItem(item);
                        itemHandler.DestroyItem(gameObject);
                    }
                    Debug.Log($"[{gameObject.name}] pick_up");
                    break;

                case "destroy":
                    if (itemHandler)
                    {
                        itemHandler.DestroyItem(gameObject);
                    }
                    Debug.Log($"[{gameObject.name}] destroy");
                    break;

                case "open_inventory":
                    OpenInventory();
                    break;

                case "rename":
                    // TODO
                    Debug.Log($"[{gameObject.name}] Rename (TODO)");
                    break;

                default:
                    Debug.LogWarning($"[{gameObject.name}] Неизвестное действие: {actionId}");
                    break;
            }

            // TODO Сохраняем изменения для rename

        }




        protected bool _isDead = false;
        public bool IsDead() => _isDead;
        public float GetHealth() => health;
        public float GetMaxHealth() => maxHealth;
        public bool IsAlive() => !_isDead && health > 0;
        public void SetHealth(float value) => health = Mathf.Clamp(value, 0f, maxHealth);


    }
}