using UnityEngine;
using Assets.Scripts.Interactables;
using Assets.Scripts.Player;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Effects;
using Assets.Scripts.Audio;
using Assets.Scripts.Core;
using System.Collections;

namespace Assets.Scripts.Creatures
{
    public class Corpse : MonoBehaviour, IInteractable, IImpactSoundProvider
    {
        public bool IsDragging { get; private set; }
        public bool IsHarvested { get; private set; } = false;

        [Header("Drag Target")]
        [Tooltip("Кость, за которую тянем (Torso/Hips).")]
        [SerializeField] private Rigidbody _dragRigidbody;

        [Header("Spring Settings")]
        public float dragDistance = 0f;
        public float springForce = 500f;
        public float springDamper = 0f;
        public float maxSpringDistance = 0.3f;

        private CharacterJoint _jointToGrabPoint;
        private CharacterJoint _jointToParentObj;
        private Transform _anchor;
        private Rigidbody _anchorRb;
        private Rigidbody _parentObjRb;
        private Creature creature;

        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        [Header("Inventory")]
        [SerializeField] private ChestInventory _inventory;
        [SerializeField] private ChestUI _chestUI;
        private bool _isOpen = false;

        [Header("Harvesting")]
        public bool allowHarvest = true;
        public bool allowFists = false;
        public bool allowAxe = true;
        public bool allowPickaxe = true;
        public bool allowSword = true;
        public bool allowSickle = false;

        [Tooltip("Предметы, которые рандомно попадут в инвентарь трупа (мясо, шкуры, детали)")]
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

        [Tooltip("Ресурсы, которые выпадают при разбивании тела (железо, электроника, кости)")]
        public ResourceDrop[] harvestDrops;
        [Tooltip("Сколько ударов нужно, чтобы полностью разобрать тело")]
        public int maxHarvestHits = 5;

        [Header("Ragdoll")]
        public RagdollSettings ragdollSettings;
        private PlayerController _playerController = null;
        private Transform playerTransform;

        [Header("Despawn Settings")]
        [SerializeField] private float _corpseDisappearTime = 300f;
        [SerializeField] private GameObject _lootBagPrefab;

        private string _corpseName = "";
        private float _creationTime = 0f;
        private int _harvestHits = 0;
        private int[] _remainingAmounts;
        private bool _isDepleted = false;
        private Coroutine _despawnCoroutine;

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

        private void Start()
        {
            _creationTime = Time.time;

            if (_despawnCoroutine != null)
                StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = StartCoroutine(DespawnAfterTime());

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

            if (TryGetComponent<PlayerController>(out var pc))
            {
                _playerController = pc;
                playerTransform = pc.transform;
            }
        }

        public InteractType GetInteractType() => InteractType.OpenTargetInventory;
        public InteractType GetInteractType2() => InteractType.Interact;
        public ChestInventory GetInventory() => _inventory;
        public bool HasInventory() => _inventory != null;
        public bool ShouldDetachAfterInteract() => _isDepleted;

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

        public void ActivateRagdoll()
        {

            foreach (var part in ragdollSettings.ragdollParts)
            {
                if (part == null) continue;
                var rb = part.GetComponent<Rigidbody>();
                if (rb == null) continue;

                rb.isKinematic = false;
                rb.useGravity = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                // rb.linearDamping = 5f;
                rb.linearDamping = ragdollSettings.linearDamp;
                // rb.angularDamping = 5f;
                rb.angularDamping = ragdollSettings.angularDamp;
                // rb.mass *= ragdollSettings.massMultiplier;

                // rb.maxAngularVelocity = 5f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                foreach (var otherPart in ragdollSettings.ragdollParts)
                {
                    if (part != otherPart && otherPart.TryGetComponent<Collider>(out var otherCol))
                    {
                        Physics.IgnoreCollision(rb.GetComponent<Collider>(), otherCol, true);
                    }
                }

                if (playerTransform != null)
                {
                    Debug.Log("ActivateRagdoll playerTransform: " + playerTransform);
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

                    if (rb.linearVelocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
                    {
                        rb.Sleep();
                    }
                }

                if (playerTransform != null)
                {
                    Debug.Log("DeactivateRagdoll playerTransform: " + playerTransform);
                    if (playerTransform.TryGetComponent<Collider>(out var playerCol))
                    {
                        Physics.IgnoreCollision(rb.GetComponent<Collider>(), playerCol, false);
                    }
                }
            }
        }

        public IEnumerator StopMovingRagdoll()
        {
            yield return new WaitForSeconds(5.5f);
            DeactivateRagdoll();
        }

        public IEnumerator StopMovingCorpse(bool isPlayerCorpse = false)
        {
            yield return new WaitForSeconds(2.5f);

            Animator animator = GetComponent<Animator>();
            if (animator != null) Destroy(animator);

            DeactivateRagdoll();

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

        private void CopyPlayerItemsToCorpse(InventoryData mainInventory, InventoryData hotbarInventory)
        {
            if (_inventory?.Data == null) return;

            // Копируем вещи из основного инвентаря
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

            // Копируем вещи из хотбара
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


        public void OpenInventory()
        {

            // Если уже открыт - сначала закрываем
            if (_isOpen)
            {
                CloseInventory();
            }

            // Теперь открываем (даже если был закрыт)
            if (_chestUI != null)
            {
                _chestUI.OpenWith(_inventory, this);
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

        public void CreateCorpseInventory(string corpseName, int invCapacity, InventoryData mainInventory, InventoryData hotbarInventory, ChestUI chestUI)
        {

            // Добавляем к трупу и инициализируем компонент ChestInventory
            var chestInv = gameObject.AddComponent<ChestInventory>();
            string corpseKey = $"{corpseName}_{System.Guid.NewGuid().ToString()}";
            chestInv.Initialize(invCapacity, corpseKey);

            _inventory = chestInv;
            _chestUI = chestUI;
            _corpseName = corpseName;

            if (mainInventory != null)
            {
                // Копируем в труп все вещи игрока
                CopyPlayerItemsToCorpse(mainInventory, hotbarInventory);
            }
            else
            {
                // Заполняем инвентарь существа
                PopulateInventory(chestInv);
            }

            // Инициализируем труп для добычи ресурсов с него
            InitializeCorpse();
        }

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

            // Создаём сумку
            GameObject bagGO = Instantiate(_lootBagPrefab, transform.position, Quaternion.identity);
            bagGO.name = $"LootBag_{_corpseName}";

            // Создаём инвентарь для сумки
            ChestInventory bagInventory = bagGO.AddComponent<ChestInventory>();

            // Копируем содержимое из инвентаря корпуса
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
                lootBag.Initialize(bagInventory, _chestUI, _corpseDisappearTime, lootBag);
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
                Debug.Log($"[Corpse] Труп исчез по таймеру на {transform.position}");
                Destroy(gameObject);
            }
        }

        public void StartDragging(PlayerInteraction player)
        {
            if (IsDragging || _dragRigidbody == null) return;
            IsDragging = true;

            player.RegisterDraggingCorpse(this);
            ActivateRagdoll();

            var equip = player.GetComponent<PlayerEquipment>();
            Transform grabPoint = equip ? equip.corpseDragAnchor : player.transform;
            if (grabPoint == null) grabPoint = player.transform;

            if (_interactionCollider != null) _interactionCollider.enabled = false;

            // Помещаем точку перетаскивания _anchor в точку для перетаскивания персонажа grabPoint
            _anchor = new GameObject("CorpseDragAnchor").transform;
            _anchor.SetParent(grabPoint);
            _anchor.localPosition = new Vector3(0, 0, dragDistance);
            // Добавляем точке перетаскивания _anchor Rigidbody
            _anchorRb = _anchor.gameObject.AddComponent<Rigidbody>();
            _anchorRb.isKinematic = true;

            // Получаем у главного объекта Rigidbody
            _parentObjRb = gameObject.GetComponent<Rigidbody>();
            _parentObjRb.isKinematic = false;
            _dragRigidbody.isKinematic = false;
            // Debug.Log("StartDragging _parentObjRb: " + _parentObjRb);

            // Добавляем соединение c _anchor
            _jointToGrabPoint = _parentObjRb.gameObject.AddComponent<CharacterJoint>();
            _jointToGrabPoint.connectedBody = _anchorRb;
            _jointToGrabPoint.enablePreprocessing = false;
            _jointToGrabPoint.enableCollision = false;

            // Добавляем соединение c _parentObjRb 
            _jointToParentObj = _dragRigidbody.gameObject.AddComponent<CharacterJoint>();
            _jointToParentObj.connectedBody = _parentObjRb;
            _jointToParentObj.enablePreprocessing = false;
            _jointToParentObj.enableCollision = false;

        }

        public void StopDragging(PlayerInteraction player)
        {
            if (!IsDragging) return;
            IsDragging = false;

            player.UnregisterDraggingCorpse(this);

            if (_jointToParentObj != null) { Destroy(_jointToParentObj); _jointToParentObj = null; }
            if (_jointToGrabPoint != null) { Destroy(_jointToGrabPoint); _jointToGrabPoint = null; }
            if (_anchor != null) { Destroy(_anchor.gameObject); _anchor = null; }

            if (_dragRigidbody != null)
            {
                _dragRigidbody.linearVelocity *= 0.2f;
                _dragRigidbody.angularVelocity *= 0.2f;
            }

            if (_parentObjRb != null)
            {
                _parentObjRb.isKinematic = true;
            }
            if (_dragRigidbody != null)
            {
                _dragRigidbody.isKinematic = true;
            }

            if (_interactionCollider != null) _interactionCollider.enabled = true;

            StartCoroutine(StopMovingRagdoll());

        }

        public void InterruptByAttack(PlayerInteraction player) => StopDragging(player);
    }

    [System.Serializable]
    public class RagdollSettings
    {
        public Transform[] ragdollParts;
        [Header("Физика после смерти")]
        public float linearDamp = 0.5f;
        public float angularDamp = 2f;
        // public float massMultiplier = 0.5f;
        [Range(0, 1)] public float velocityInherit = 0.1f;
    }
}