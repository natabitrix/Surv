using UnityEngine;
using Assets.Scripts.Interactables;
using Assets.Scripts.Player;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Effects;
using Assets.Scripts.Audio;
using Assets.Scripts.Core;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Creatures;
using Assets.Scripts.Loot;

namespace Assets.Scripts.Corpses
{
    public class Corpse : MonoBehaviour, IInteractable, IImpactSoundProvider, IInventorySource
    {
        [Header("Ragdoll")]
        [SerializeField] private RagdollController _ragdollController;

        public bool IsDragging { get; private set; }
        public bool IsHarvested { get; private set; } = false;

        [Header("Drag Target")]
        [SerializeField] private Rigidbody _dragRigidbody;
        [SerializeField] private Transform _centerBone;
        private SpringJoint _springJointToGrabPoint;

        private Creature creature;

        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        [Header("Interaction Anchor (для стабильного рейкаста)")]
        [SerializeField] private Transform _interactionAnchor;

        [Header("Inventory")]
        [SerializeField] private ChestInventory _inventory;
        private bool _isOpen = false;

        [Header("Harvesting")]
        public bool allowHarvest = true;
        public bool allowFists = false;
        public bool allowAxe = true;
        public bool allowPickaxe = true;
        public bool allowSword = true;
        public bool allowSickle = false;

        public LootEntry[] inventoryLootTable;

        [System.Serializable]
        public struct ResourceDrop
        {
            public Item item;
            public int totalAmount;
        }

        [Header("Harvest Visuals & Audio")]
        public ParticleSystem breakEffect;
        public Shatterer shatterer;
        public HitDecaler hitDecaler;
        public Animator animator;
        [SerializeField] private ImpactType _impactType = ImpactType.Metal;
        public ImpactType GetImpactType() => _impactType;

        public ResourceDrop[] harvestDrops;
        public int maxHarvestHits = 5;

        [Header("Despawn Settings")]
        [SerializeField] private float _corpseDisappearTime = 300f;
        [SerializeField] private GameObject _lootBagPrefab;

        [Header("Persistence")]
        public string InstanceId { get; set; }
        public string OwnerPlayerId { get; set; } = "player_001";
        public string CorpseId { get; set; } = "Unknown";

        private float _remainingLifetime;
        private string _corpseName = "";
        private float _creationTime = 0f;
        private int _harvestHits = 0;
        private int[] _remainingAmounts;
        private bool _isDepleted = false;
        private Coroutine _despawnCoroutine;

        public RagdollController RagdollController => _ragdollController;

        private void Awake()
        {
            if (_ragdollController == null)
                _ragdollController = GetComponent<RagdollController>();

            creature = GetComponent<Creature>();

            if (creature != null && creature.IsAlive())
            {
                enabled = false;
            }

            if (shatterer == null) shatterer = GetComponent<Shatterer>();
            if (hitDecaler == null) hitDecaler = GetComponent<HitDecaler>();
        }

        private void Start()
        {
            _creationTime = Time.time;

            if (_despawnCoroutine != null)
                StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = StartCoroutine(DespawnAfterTime());
        }

        private void LateUpdate()
        {
            if (enabled && _interactionAnchor != null)
            {
                UpdateInteractionAnchorPosition();
            }
        }

        private void UpdateInteractionAnchorPosition()
        {
            if (_centerBone != null)
            {
                if (_interactionAnchor != null)
                {
                    _interactionAnchor.position = _centerBone.position;
                }
                if (IsDragging)
                {
                    transform.position = _centerBone.position;
                }
            }
        }

        public void InitializeCorpse()
        {
            if (harvestDrops != null && harvestDrops.Length > 0)
            {
                _remainingAmounts = new int[harvestDrops.Length];
                for (int i = 0; i < harvestDrops.Length; i++)
                {
                    _remainingAmounts[i] = harvestDrops[i].totalAmount;
                }
            }
        }

        public InteractType GetInteractType() => InteractType.OpenTargetInventory;
        public InteractType GetInteractType2() => InteractType.Interact;

        // === IInventorySource ===
        public ChestInventory GetInventory() => _inventory;
        public bool HasInventory() => _inventory != null;
        public bool ShouldDetachAfterInteract() => _isDepleted;

        public void OnInventoryOpened()
        {
            _isOpen = true;
        }

        public void OnInventoryClosed()
        {
            _isOpen = false;
        }

        public void Interact(InteractContext context)
        {
            if (!enabled || (_isDepleted && context.IsAttack)) return;

            if (IsDragging) StopDragging(context.PlayerInteraction);

            if (context.IsAttack)
            {
                HandleHarvest(context);
            }
            else
            {
                if (context.isTargetInventory)
                {
                    OpenInventory();
                }
                else
                {
                    StartDragging(context.PlayerInteraction);
                }
            }
        }

        // ==========================================
        // === RAGDOLL ===
        // ==========================================

        public void ActivateRagdoll()
        {
            _ragdollController?.ActivateRagdoll();
        }

        public IEnumerator ActivateRagdollCoroutine(float delay = 1f)
        {
            yield return new WaitForSecondsRealtime(delay);
            _ragdollController?.ActivateRagdoll();
            StartCoroutine(StopMovingRagdoll());
        }

        public void DeactivateRagdoll()
        {
            _ragdollController?.DeactivateRagdoll();
        }

        public IEnumerator StopMovingRagdoll()
        {
            if (_ragdollController != null)
                yield return _ragdollController.StopMovingRagdoll();
        }

        public IEnumerator StopMovingCorpse(bool isPlayerCorpse = false)
        {
            yield return new WaitForSecondsRealtime(2.5f);

            Animator anim = GetComponent<Animator>();
            if (anim != null) Destroy(anim);

            DeactivateRagdoll();
        }

        public void StabilizeRagdoll(bool stabilize)
        {
            _ragdollController?.StabilizeRagdoll(stabilize);
        }

        // ==========================================
        // === ИНВЕНТАРЬ ===
        // ==========================================

        public void CreateCorpseInventory(string corpseName, int invCapacity)
        {
            var chestInv = gameObject.AddComponent<ChestInventory>();
            string corpseKey = $"{corpseName}_{System.Guid.NewGuid().ToString()}";
            chestInv.Initialize(invCapacity, corpseKey);

            _inventory = chestInv;
            _corpseName = corpseName;
        }

        public void PopulateInventoryFromLootTable()
        {
            if (_inventory?.Data == null)
            {
                Debug.LogWarning("[Corpse] _inventory или _inventory.Data равны null!");
                return;
            }

            if (inventoryLootTable == null || inventoryLootTable.Length == 0)
            {
                Debug.Log($"[Corpse] inventoryLootTable пуст — инвентарь останется пустым.");
                return;
            }

            foreach (var entry in inventoryLootTable)
            {
                if (entry.item == null) continue;

                float roll = Random.value;

                if (roll <= entry.dropChance)
                {
                    int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                    _inventory.Data.AddItemAnywhere(entry.item, amount);
                }
            }
        }

        public void CopyPlayerItemsToCorpse(InventoryData mainInventory, InventoryData hotbarInventory)
        {
            if (_inventory?.Data == null) return;

            if (mainInventory?.slots != null)
            {
                foreach (var slot in mainInventory.slots)
                {
                    if (!slot.IsEmpty && slot.item != null)
                    {
                        _inventory.Data.AddItemAnywhere(slot.item, slot.count, slot.currentDurability);
                    }
                }
            }

            if (hotbarInventory?.slots != null)
            {
                foreach (var slot in hotbarInventory.slots)
                {
                    if (!slot.IsEmpty && slot.item != null)
                    {
                        _inventory.Data.AddItemAnywhere(slot.item, slot.count, slot.currentDurability);
                    }
                }
            }
        }

        public void LoadFromCorpseData(SerializableInventory inventoryData, ItemDatabase itemDatabase)
        {
            if (_inventory?.Data == null) return;

            if (inventoryData != null && itemDatabase != null)
            {
                _inventory.Data.FromSerializable(inventoryData, itemDatabase.ItemLookup);
            }
        }

        public void OpenInventory()
        {
            if (ChestUIManager.Instance == null)
            {
                Debug.LogError("[Corpse] ChestUIManager не найден!");
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

        // ==========================================
        // === HARVEST ===
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
                Debug.LogWarning($"[Corpse] Добыча запрещена для инструмента {tool}!");
                return;
            }

            if (_harvestHits >= maxHarvestHits) return;
            _harvestHits++;

            DistributeResources(tool, hitPos);
        }

        private void DistributeResources(AttackAnimationType tool, Vector3 hitPos)
        {
            if (harvestDrops == null || harvestDrops.Length == 0) return;

            for (int i = 0; i < harvestDrops.Length; i++)
            {
                if (harvestDrops[i].item == null || _remainingAmounts[i] <= 0) continue;

                int remaining = _remainingAmounts[i];
                int actionsLeft = maxHarvestHits - _harvestHits + 1;

                int avg = Mathf.Max(1, Mathf.CeilToInt((float)remaining / actionsLeft));
                int maxPossibleNow = Mathf.Min(remaining, (int)(avg * 1.5f));
                int minPossibleNow = Mathf.Min(1, remaining);

                int give = Random.Range(minPossibleNow, maxPossibleNow + 1);
                if (give > remaining) give = remaining;

                if (give > 0)
                {
                    _remainingAmounts[i] -= give;
                    GiveResource(harvestDrops[i].item, give);
                }
            }

            bool allDepleted = true;
            foreach (int amount in _remainingAmounts)
            {
                if (amount > 0) { allDepleted = false; break; }
            }

            if (allDepleted || _harvestHits >= maxHarvestHits)
            {
                OnHarvestComplete(hitPos);
            }
        }

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

        private void OnHarvestComplete(Vector3 hitPos)
        {
            if (_isDepleted) return;
            _isDepleted = true;
            IsHarvested = true;

            if (animator != null) animator.SetTrigger("BreakLast");
            shatterer?.LastBreak();

            bool hasLoot = HasLootInInventory();

            if (hasLoot && _lootBagPrefab != null)
            {
                CreateLootBag();
            }

            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }

            if (CorpseManager.Instance != null && !string.IsNullOrEmpty(InstanceId))
            {
                CorpseManager.Instance.UnregisterCorpse(InstanceId);
            }

            Destroy(gameObject, 0.5f);
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

        private void CreateLootBag()
        {
            if (_lootBagPrefab == null)
            {
                Debug.LogWarning("[Corpse] LootBag префаб не назначен!");
                return;
            }

            GameObject bagGO = Instantiate(_lootBagPrefab, transform.position, Quaternion.identity);
            bagGO.name = $"LootBag_{_corpseName}";

            ChestInventory bagInventory = bagGO.AddComponent<ChestInventory>();

            if (_inventory?.Data?.slots != null)
            {
                string saveKey = $"LootBag_{_corpseName}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
                bagInventory.Initialize(_inventory.Data.slots.Count, saveKey);
                foreach (var slot in _inventory.Data.slots)
                {
                    if (!slot.IsEmpty && slot.item != null)
                    {
                        bagInventory.Data.AddItemAnywhere(slot.item, slot.count);
                    }
                }
            }

            var lootBag = bagGO.GetComponent<LootBag>();
            if (lootBag != null)
            {
                lootBag.Initialize(bagInventory, _corpseDisappearTime, lootBag);

                if (LootBagManager.Instance != null)
                {
                    LootBagManager.Instance.RegisterLootBag(bagGO, "world");
                }
            }
            else
            {
                Debug.LogWarning("[Corpse] На префабе сумки нет компонента LootBag!");
                Destroy(bagGO);
            }
        }

        private IEnumerator DespawnAfterTime()
        {
            yield return new WaitForSeconds(_corpseDisappearTime);

            if (!IsHarvested)
            {
                Destroy(gameObject);
            }
        }

        // ==========================================
        // === DRAG ===
        // ==========================================

        private Transform _dragGrabPoint;
        private float _dragBodyVelocity = 100f;

        private void FixedUpdate()
        {
            if (!IsDragging || _dragRigidbody == null || _dragGrabPoint == null) return;

            _dragRigidbody.linearVelocity = (_dragGrabPoint.position - _dragRigidbody.position) * _dragBodyVelocity;
        }

        public void StartDragging(PlayerInteraction player)
        {
            if (IsDragging || _dragRigidbody == null) return;

            _ragdollController?.ResetVelocities();

            _dragRigidbody.isKinematic = false;
            _dragRigidbody.WakeUp();

            IsDragging = true;
            player.RegisterDraggingCorpse(this);
            ActivateRagdoll();

            _ragdollController?.ResetVelocities();

            var equip = player.GetComponent<PlayerEquipment>();
            Transform grabPoint = equip ? equip.corpseDragAnchor : player.transform;
            if (grabPoint == null) grabPoint = player.transform;

            _dragGrabPoint = grabPoint;

            Rigidbody playerRb = grabPoint.GetComponent<Rigidbody>();
            if (playerRb == null)
            {
                playerRb = player.GetComponent<Rigidbody>();
            }

            if (_interactionCollider != null) _interactionCollider.enabled = false;
        }

        public void StopDragging(PlayerInteraction player)
        {
            if (!IsDragging) return;
            IsDragging = false;

            player.UnregisterDraggingCorpse(this);

            _ragdollController?.ResetVelocities();
            _ragdollController?.WakeAllRigidbodies();

            if (_interactionCollider != null) _interactionCollider.enabled = true;

            StartCoroutine(StopMovingRagdoll());
        }

        public void InterruptByAttack(PlayerInteraction player) => StopDragging(player);

        // ==========================================
        // === ДАННЫЕ / ПЕРСИСТЕНТНОСТЬ ===
        // ==========================================

        public int GetHarvestHits() => _harvestHits;
        public int GetInventoryCapacity() => _inventory?.Data?.size ?? 100;

        public void StartDespawnTimer(float lifetime)
        {
            _remainingLifetime = lifetime;
            if (_despawnCoroutine != null) StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = StartCoroutine(DespawnAfterTime(lifetime));
        }

        public void SaveHarvestData(CorpseSaveData data)
        {
            data.harvestDrops.Clear();
            data.remainingAmounts.Clear();

            if (harvestDrops != null)
            {
                foreach (var drop in harvestDrops)
                {
                    data.harvestDrops.Add(new ResourceDropData
                    {
                        itemId = drop.item?.Id ?? "",
                        totalAmount = drop.totalAmount
                    });
                }
            }

            if (_remainingAmounts != null)
            {
                data.remainingAmounts.AddRange(_remainingAmounts);
            }

            data.harvestHits = _harvestHits;
            data.isDepleted = _isDepleted;
        }

        public void LoadHarvestData(CorpseSaveData data)
        {
            if (data.harvestDrops != null && data.harvestDrops.Count > 0)
            {
                var itemDb = PlayerProgress.Instance?.itemDatabase?.ItemLookup;
                var newDrops = new List<ResourceDrop>();

                foreach (var dropData in data.harvestDrops)
                {
                    if (itemDb != null && itemDb.TryGetValue(dropData.itemId, out var item))
                    {
                        newDrops.Add(new ResourceDrop
                        {
                            item = item,
                            totalAmount = dropData.totalAmount
                        });
                    }
                }

                harvestDrops = newDrops.ToArray();
            }

            if (data.remainingAmounts != null && data.remainingAmounts.Count > 0)
            {
                _remainingAmounts = data.remainingAmounts.ToArray();
            }
            else if (harvestDrops != null)
            {
                _remainingAmounts = new int[harvestDrops.Length];
                for (int i = 0; i < harvestDrops.Length; i++)
                    _remainingAmounts[i] = harvestDrops[i].totalAmount;
            }

            _harvestHits = data.harvestHits;
            _isDepleted = data.isDepleted;
        }

        private IEnumerator DespawnAfterTime(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);

            if (!IsHarvested)
            {
                if (CorpseManager.Instance != null && !string.IsNullOrEmpty(InstanceId))
                {
                    CorpseManager.Instance.UnregisterCorpse(InstanceId);
                }

                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            // Если этот труп был открыт — закрываем UI
            if (ChestUIManager.Instance != null && ReferenceEquals(ChestUIManager.Instance.CurrentSource, this))
            {
                ChestUIManager.Instance.Close();
            }

            if (CorpseManager.Instance != null && !string.IsNullOrEmpty(InstanceId))
            {
                if (!CorpseManager.Instance.IsQuitting)
                {
                    CorpseManager.Instance.UnregisterCorpse(InstanceId);
                }
            }
        }
    }

    [System.Serializable]
    public class RagdollSettings
    {
        public Transform[] ragdollParts;
        [Header("Физика после смерти")]
        public float linearDamp = 0.5f;
        public float angularDamp = 2f;
        [Range(0, 1)] public float velocityInherit = 0.1f;
    }
}