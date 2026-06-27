using UnityEngine;
using Assets.Scripts.Interactables;
using Assets.Scripts.Player;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Effects;
using Assets.Scripts.Audio;
using Assets.Scripts.Core;

namespace Assets.Scripts.Creatures
{
    [RequireComponent(typeof(Collider))]
    public class Corpse : MonoBehaviour, IInteractable, IImpactSoundProvider
    {
        public bool IsDragging { get; private set; }

        [Header("Drag Target")]
        [Tooltip("Кость, за которую тянем (Torso/Hips).")]
        [SerializeField] private Rigidbody _dragRigidbody;

        [Header("Spring Settings")]
        public float dragDistance = 0f;
        public float springForce = 500f;
        public float springDamper = 0f;
        public float maxSpringDistance = 0.3f;

        private CharacterJoint _joint;
        private Transform _anchor;
        private Rigidbody _anchorRb;
        private Creature creature;

        // ==========================================
        // === ИНВЕНТАРЬ (Открытие по E) ===
        // ==========================================
        [Header("Inventory")]
        [SerializeField] private ChestInventory _inventory;
        [SerializeField] private ChestUI _chestUI;
        private bool _isOpen = false;

        // ==========================================
        // === ДОБЫЧА РЕСУРСОВ (Удары ЛКМ) ===
        // ==========================================
        [Header("Harvesting (Добыча ударами)")]
        public bool allowHarvest = true;
        public bool allowFists = false;
        public bool allowAxe = true;
        public bool allowPickaxe = true;
        public bool allowSword = true;
        public bool allowSickle = false;

        [System.Serializable]
        public struct ResourceDrop
        {
            public Item item;
            public int totalAmount;
        }

        private LootEntry[] _lootTable;
        private ResourceDrop[] _harvestDrops;
        private int _maxHarvestHits;

        [Header("Harvest Visuals & Audio")]
        public ParticleSystem breakEffect;
        public Shatterer shatterer;
        public HitDecaler hitDecaler;
        public Animator animator;
        [SerializeField] private ImpactType _impactType = ImpactType.Metal;
        public ImpactType GetImpactType() => _impactType;

        [Header("Destruction")]
        public bool destroyAfterDepleted = true;
        public float destroyDelay = 2f; // Задержка перед уничтожением (чтобы эффекты проиграться)


        private int _harvestHits = 0;
        private int[] _remainingAmounts;
        private bool _isDepleted = false;

        // ==========================================
        // === ИНИЦИАЛИЗАЦИЯ ===
        // ==========================================
        private void Awake()
        {
            creature = GetComponent<Creature>();

            if (creature != null && creature.IsAlive())
            {
                enabled = false;
            }

            if (shatterer == null) shatterer = GetComponent<Shatterer>();
            if (hitDecaler == null) hitDecaler = GetComponent<HitDecaler>();
        }

        public void Initialize(ChestInventory inventory, ChestUI chestUI, ResourceDrop[] drops, int maxHits)
        {
            _inventory = inventory;
            _chestUI = chestUI;
            _harvestDrops = drops;
            _maxHarvestHits = maxHits;

            if (_harvestDrops != null && _harvestDrops.Length > 0)
            {
                _remainingAmounts = new int[_harvestDrops.Length];
                for (int i = 0; i < _harvestDrops.Length; i++)
                {
                    _remainingAmounts[i] = _harvestDrops[i].totalAmount;
                }
            }
        }

        // ==========================================
        // === ИНТЕРФЕЙС IInteractable ===
        // ==========================================
        public InteractType GetInteractType()
        {
            // return _inventory != null ? InteractType.Open : InteractType.Pickup;
            return _inventory != null ? InteractType.Open : InteractType.Harvest;
        }

        public ChestInventory GetInventory() => _inventory;
        public bool HasInventory() => _inventory != null;
        public bool ShouldDetachAfterInteract() => _isDepleted;

        public void Interact(InteractContext context)
        {

            if (_isDepleted && context.IsAttack) return;

            if (context.IsAttack)
            {
                HandleHarvest(context);
            }
            else
            {
                if (_inventory != null)
                {
                    OpenInventory();
                }
                else
                {
                    if (IsDragging) StopDragging(context.PlayerInteraction);
                    else StartDragging(context.PlayerInteraction);
                }
            }
        }

        // ==========================================
        // === ЛОГИКА ИНВЕНТАРЯ ===
        // ==========================================
        public void OpenInventory()
        {
            if (_isOpen) CloseInventory();
            else if (_chestUI != null)
            {
                _chestUI.OpenWith(_inventory);
                _isOpen = true;
            }
            else
            {
                Debug.LogError("[Corpse] _chestUI не назначен! Инвентарь не откроется.");
            }
        }

        public void CloseInventory()
        {
            if (_isOpen && _chestUI != null)
            {
                _chestUI.Close();
                _isOpen = false;
            }
        }

        public void SetInventory(ChestInventory inventory)
        {
            _inventory = inventory;
        }

        // ==========================================
        // === ЛОГИКА ДОБЫЧИ (Harvest) ===
        // ==========================================
        private void HandleHarvest(InteractContext context)
        {
            if (!allowHarvest) return;

            AttackAnimationType tool = context.Tool;
            bool toolAllowed = tool switch
            {
                AttackAnimationType.Fists => allowFists,
                AttackAnimationType.Axe => allowAxe,
                AttackAnimationType.Pickaxe => allowPickaxe,
                AttackAnimationType.Sword => allowSword,
                AttackAnimationType.Sickle => allowSickle,
                _ => false
            };

            Vector3 hitPos = context.PlayerInteraction.GetTargetHitPosition();
            Vector3 hitNorm = context.PlayerInteraction.GetTargetHitNormal();

            PlayBreakEffect(hitPos, hitNorm, toolAllowed);

            if (!toolAllowed)
            {
                Debug.LogWarning($"[Corpse] Добыча запрещена для инструмента {tool}! Проверь галочки в инспекторе Corpse.");
                return;
            }

            if (_harvestHits >= _maxHarvestHits) return;
            _harvestHits++;

            DistributeResources(tool, hitPos);
        }

        private void DistributeResources(AttackAnimationType tool, Vector3 hitPos)
        {
            if (_harvestDrops == null || _harvestDrops.Length == 0) return;

            for (int i = 0; i < _harvestDrops.Length; i++)
            {
                if (_harvestDrops[i].item == null || _remainingAmounts[i] <= 0) continue;

                int remaining = _remainingAmounts[i];
                int actionsLeft = _maxHarvestHits - _harvestHits + 1;

                int avg = Mathf.Max(1, Mathf.CeilToInt((float)remaining / actionsLeft));
                int maxPossibleNow = Mathf.Min(remaining, (int)(avg * 1.5f));
                int minPossibleNow = Mathf.Min(1, remaining);

                int give = Random.Range(minPossibleNow, maxPossibleNow + 1);
                if (give > remaining) give = remaining;

                // give = ApplyToolBonus(_harvestDrops[i].item, give, tool);

                if (give > 0)
                {
                    _remainingAmounts[i] -= give;
                    GiveResource(_harvestDrops[i].item, give);
                }
            }

            bool allDepleted = true;
            foreach (int amount in _remainingAmounts)
            {
                if (amount > 0) { allDepleted = false; break; }
            }

            if (allDepleted || _harvestHits >= _maxHarvestHits)
            {
                // Deplete(hitPos);
            }
        }

        // private int ApplyToolBonus(Item item, int baseAmount, AttackAnimationType tool)
        // {
        //     float mult = 1f;
        //     string name = item.itemName.ToLower();

        //     if (name.Contains("iron") || name.Contains("железо") || name.Contains("metal"))
        //         mult = tool == AttackAnimationType.Pickaxe ? 1.5f : 0.5f;
        //     else if (name.Contains("electronic") || name.Contains("электрон") || name.Contains("circuit"))
        //         mult = tool == AttackAnimationType.Pickaxe ? 1.2f : 0.8f;
        //     else if (name.Contains("meat") || name.Contains("мясо") || name.Contains("flesh"))
        //         mult = tool == AttackAnimationType.Axe ? 1.3f : 1.0f;

        //     return Mathf.Max(0, Mathf.RoundToInt(baseAmount * mult));
        // }

        private void GiveResource(Item item, int amount)
        {
            if (amount <= 0 || item == null) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var handler = player.GetComponent<ItemHandler>();
            if (handler != null)
            {
                handler.PickupItem(item, amount);
                return;
            }

            if (PlayerProgress.Instance != null)
            {
                PlayerProgress.Instance.AddItemToPlayerInventory(item, amount);
            }
        }

        private void PlayBreakEffect(Vector3 hitPos, Vector3 hitNorm, bool isBreakable)
        {
            if (animator != null) animator.SetTrigger("Hit");

            if (breakEffect != null)
            {
                var effectInstance = Instantiate(breakEffect, hitPos, transform.rotation);
                effectInstance.Play();
                Destroy(effectInstance.gameObject, effectInstance.main.duration + 1.5f);
            }

            if (isBreakable)
            {
                shatterer?.Shatter();
                hitDecaler?.SpawnHitDecal(hitPos, hitNorm);
            }
        }

        private void Deplete(Vector3 hitPos)
        {
            if (_isDepleted) return;
            _isDepleted = true;

            if (animator != null) animator.SetTrigger("BreakLast");
            shatterer?.LastBreak();

            if (destroyAfterDepleted)
            {
                Destroy(gameObject, destroyDelay);
            }
        }

        // ==========================================
        // === ЛОГИКА ПЕРЕТАСКИВАНИЯ ===
        // ==========================================
        public void StartDragging(PlayerInteraction player)
        {
            if (IsDragging || _dragRigidbody == null) return;
            IsDragging = true;

            player.RegisterDraggingCorpse(this);
            creature.ActivateRagdoll();

            var equip = player.GetComponent<PlayerEquipment>();
            Transform grabPoint = equip ? equip.corpseDragAnchor : player.transform;
            if (grabPoint == null) grabPoint = player.transform;

            _anchor = new GameObject("CorpseDragAnchor").transform;
            _anchor.SetParent(grabPoint);
            _anchor.localPosition = new Vector3(0, 0, dragDistance);
            _anchorRb = _anchor.gameObject.AddComponent<Rigidbody>();
            _anchorRb.isKinematic = true;

            _joint = _dragRigidbody.gameObject.AddComponent<CharacterJoint>();
            _joint.connectedBody = _anchorRb;
            _joint.enablePreprocessing = true;
            _joint.enableCollision = false;
        }

        public void StopDragging(PlayerInteraction player)
        {
            if (!IsDragging) return;
            IsDragging = false;

            player.UnregisterDraggingCorpse(this);

            if (_joint != null) { Destroy(_joint); _joint = null; }
            if (_anchor != null) { Destroy(_anchor.gameObject); _anchor = null; }

            if (_dragRigidbody != null)
            {
                _dragRigidbody.linearVelocity *= 0.2f;
                _dragRigidbody.angularVelocity *= 0.2f;
            }

            creature.DeactivateRagdoll();
        }

        public void InterruptByAttack(PlayerInteraction player) => StopDragging(player);
    }
}