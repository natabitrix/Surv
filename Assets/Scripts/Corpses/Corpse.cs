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
    public class Corpse : MonoBehaviour, IInteractable, IImpactSoundProvider
    {
        [Header("Ragdoll")]
        public RagdollSettings ragdollSettings;

        public bool IsDragging { get; private set; }
        public bool IsHarvested { get; private set; } = false;

        [Header("Drag Target")]
        [Tooltip("Кость, за которую тянем (Torso/Hips).")]
        [SerializeField] private Rigidbody _dragRigidbody;
        [SerializeField] private Transform _centerBone;
        private SpringJoint _springJointToGrabPoint;
        private FixedJoint _fixedJointToGrabPoint;
        private CharacterJoint _characterJointToGrabPoint;

        private Creature creature;

        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;
        [Header("Interaction Anchor (для стабильного рейкаста)")]
        [Tooltip("Пустой GameObject с коллайдером, который следует за центром ragdoll. " +
                 "Если не назначен — коллайдер берётся напрямую.")]
        [SerializeField] private Transform _interactionAnchor;

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

        private void LateUpdate()
        {
            // Если мы в состоянии трупа и есть anchor
            if (enabled && _interactionAnchor != null)
            {
                UpdateInteractionAnchorPosition();
            }
        }


        private void UpdateInteractionAnchorPosition()
        {
            if (_centerBone != null)
            {
                transform.position = _centerBone.position;
                if (_interactionAnchor != null)
                {
                    _interactionAnchor.position = _centerBone.position;
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
                rb.linearDamping = ragdollSettings.linearDamp;
                rb.angularDamping = ragdollSettings.angularDamp;
                // rb.linearDamping = 0.5f; //для плеера надо меньше сделать
                // rb.angularDamping = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                // foreach (var otherPart in ragdollSettings.ragdollParts)
                // {
                //     if (part != otherPart && otherPart.TryGetComponent<Collider>(out var otherCol))
                //     {
                //         if (rb.TryGetComponent<Collider>(out var rbCol))
                //         {
                //             Physics.IgnoreCollision(rbCol, otherCol, true);
                //         }
                //     }
                // }
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
            }
        }

        public IEnumerator StopMovingRagdoll()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            DeactivateRagdoll();
        }

        public IEnumerator StopMovingCorpse(bool isPlayerCorpse = false)
        {
            yield return new WaitForSecondsRealtime(2.5f);

            Animator animator = GetComponent<Animator>();
            if (animator != null) Destroy(animator);

            DeactivateRagdoll();
        }

        public void StabilizeRagdoll(bool stabilize)
        {
            foreach (var part in ragdollSettings.ragdollParts)
            {
                if (part == null) continue;
                var rb = part.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    if (stabilize)
                    {
                        // Высокое демпфирование "замораживает" болтание частей тела
                        rb.linearDamping = 10f;
                        rb.angularDamping = 10f;
                    }
                    else
                    {
                        // Возвращаем обычные настройки физики
                        rb.linearDamping = ragdollSettings.linearDamp;
                        rb.angularDamping = ragdollSettings.angularDamp;
                    }
                }
            }
        }

        public void CreateCorpseInventory(
            string corpseName,
            int invCapacity,
            ChestUI chestUI)
        {
            // Добавляем к трупу и инициализируем компонент ChestInventory
            var chestInv = gameObject.AddComponent<ChestInventory>();
            string corpseKey = $"{corpseName}_{System.Guid.NewGuid().ToString()}";
            chestInv.Initialize(invCapacity, corpseKey);

            _inventory = chestInv;
            _chestUI = chestUI;
            _corpseName = corpseName;
        }

        /// <summary>
        /// Заполняет инвентарь трупа лутом из inventoryLootTable.
        /// Используется для существ.
        /// </summary>
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
                // Труп разобран — можно удалить запись (лут перенесен в LootBag)
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

                // ✅ Регистрируем сумку в LootBagManager
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
                // Debug.Log($"[Corpse] Труп исчез по таймеру на {transform.position}");
                Destroy(gameObject);
            }
        }

        public void StartDragging_(PlayerInteraction player)
        {
            if (IsDragging || _dragRigidbody == null) return;
            IsDragging = true;

            player.RegisterDraggingCorpse(this);
            ActivateRagdoll();

            var equip = player.GetComponent<PlayerEquipment>();
            Transform grabPoint = equip ? equip.corpseDragAnchor : player.transform;
            if (grabPoint == null) grabPoint = player.transform;

            if (_interactionCollider != null) _interactionCollider.enabled = false;

            _dragRigidbody.WakeUp();

            // FixedJoint
            // _fixedJointToGrabPoint = _dragRigidbody.gameObject.AddComponent<FixedJoint>();
            // _fixedJointToGrabPoint.connectedBody = grabPoint.gameObject.GetComponent<Rigidbody>();
            // _fixedJointToGrabPoint.enablePreprocessing = false;
            // _fixedJointToGrabPoint.enableCollision = false;

            // CharacterJoint
            // _characterJointToGrabPoint = _dragRigidbody.gameObject.AddComponent<CharacterJoint>();
            // _characterJointToGrabPoint.connectedBody = grabPoint.gameObject.GetComponent<Rigidbody>();
            // _characterJointToGrabPoint.enablePreprocessing = false;
            // _characterJointToGrabPoint.enableCollision = false;

            // SpringJoint
            _springJointToGrabPoint = _dragRigidbody.gameObject.AddComponent<SpringJoint>();
            _springJointToGrabPoint.connectedBody = grabPoint.gameObject.GetComponent<Rigidbody>();
            _springJointToGrabPoint.autoConfigureConnectedAnchor = false;
            _springJointToGrabPoint.anchor = Vector3.zero;          // Точка крепления на самом предмете (центр)
            _springJointToGrabPoint.connectedAnchor = Vector3.zero; // Точка крепления на руке (центр руки)

            _springJointToGrabPoint.spring = 10000f;                  // Сила притягивания (чем выше, тем жестче пружина)
            _springJointToGrabPoint.damper = 100f;                   // Гашение колебаний (чтобы предмет не качался бесконечно)
            _springJointToGrabPoint.tolerance = 0.01f;              // Погрешность расстояния
            _springJointToGrabPoint.minDistance = 0f;               // Минимальное расстояние между рукой и предметом
            _springJointToGrabPoint.maxDistance = 0f;               // Максимальное расстояние (0 означает, что предмет стремится ровно в точку руки)
        }

        public void StartDragging(PlayerInteraction player)
        {
            if (IsDragging || _dragRigidbody == null) return;

            // ВАЖНО: Убедимся, что физика включена именно для той кости, за которую тянем
            _dragRigidbody.isKinematic = false;
            _dragRigidbody.WakeUp();

            IsDragging = true;
            player.RegisterDraggingCorpse(this);

            // Активируем весь рагдолл
            ActivateRagdoll();

            var equip = player.GetComponent<PlayerEquipment>();
            Transform grabPoint = equip ? equip.corpseDragAnchor : player.transform;
            if (grabPoint == null) grabPoint = player.transform;

            // Получаем Rigidbody игрока (или его якоря)
            Rigidbody playerRb = grabPoint.GetComponent<Rigidbody>();
            if (playerRb == null)
            {
                // Если у якоря нет RB, ищем на самом игроке
                playerRb = player.GetComponent<Rigidbody>();
            }

            if (_interactionCollider != null) _interactionCollider.enabled = false;

            // Добавляем SpringJoint именно на ту кость, которая назначена в _dragRigidbody
            _springJointToGrabPoint = _dragRigidbody.gameObject.AddComponent<SpringJoint>();
            _springJointToGrabPoint.connectedBody = playerRb;

            // Настройки для стабильного перетаскивания за конечность:
            _springJointToGrabPoint.autoConfigureConnectedAnchor = false;

            // Anchor: точка на теле трупа. Vector3.zero означает центр самого объекта (кости ноги)
            _springJointToGrabPoint.anchor = Vector3.zero;
            _springJointToGrabPoint.connectedAnchor = Vector3.zero;

            // Параметры пружины (можно подкорректировать под вес вашей модели)
            _springJointToGrabPoint.spring = 10000f; // Чуть меньше, чем было, чтобы не рвало суставы при рывке за ногу
            _springJointToGrabPoint.damper = 200f;  // Больше демпфер, чтобы нога не болталась как макарина
            _springJointToGrabPoint.tolerance = 0.1f;
            _springJointToGrabPoint.minDistance = 0f;
            _springJointToGrabPoint.maxDistance = 0f;
        }

        public void StopDragging(PlayerInteraction player)
        {
            if (!IsDragging) return;
            IsDragging = false;

            player.UnregisterDraggingCorpse(this);

            // if (_fixedJointToGrabPoint != null)
            // {
            //     Destroy(_fixedJointToGrabPoint);
            // }

            // if (_characterJointToGrabPoint != null)
            // {
            //     Destroy(_characterJointToGrabPoint);
            // }

            if (_springJointToGrabPoint != null)
            {
                _springJointToGrabPoint.connectedBody = null;
                Destroy(_springJointToGrabPoint);
                _springJointToGrabPoint = null;
            }


            // if (_dragRigidbody != null)
            // {
            //     if (!_dragRigidbody.isKinematic)
            //     {
            //         _dragRigidbody.linearVelocity *= 0.2f;
            //         _dragRigidbody.angularVelocity *= 0.2f;
            //     }

            //     _dragRigidbody.isKinematic = true;

            // }

            // 3. "Будим" все части тела и даем им небольшой случайный толчок
            // Это нужно, чтобы ragdoll не застыл в одной позе, а красиво рассыпался
            foreach (var part in ragdollSettings.ragdollParts)
            {
                if (part == null) continue;
                var rb = part.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearDamping = ragdollSettings.linearDamp;
                    rb.angularDamping = ragdollSettings.angularDamp;
                    rb.isKinematic = false; // Важно! Включаем физику
                    rb.WakeUp();

                    // Добавляем случайный микро-импульс для естественности падения
                    rb.AddForce(Random.insideUnitSphere * 2f, ForceMode.Impulse);
                }
            }

            if (_interactionCollider != null) _interactionCollider.enabled = true;


            StartCoroutine(StopMovingRagdoll());

        }

        public void InterruptByAttack(PlayerInteraction player) => StopDragging(player);

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
            // Восстанавливаем добычу
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

            // Восстанавливаем оставшиеся количества
            if (data.remainingAmounts != null && data.remainingAmounts.Count > 0)
            {
                _remainingAmounts = data.remainingAmounts.ToArray();
            }
            else if (harvestDrops != null)
            {
                // Если не было сохранено — считаем, что все на месте
                _remainingAmounts = new int[harvestDrops.Length];
                for (int i = 0; i < harvestDrops.Length; i++)
                    _remainingAmounts[i] = harvestDrops[i].totalAmount;
            }

            _harvestHits = data.harvestHits;
            _isDepleted = data.isDepleted;
        }

        // Перегрузка DespawnAfterTime
        private IEnumerator DespawnAfterTime(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);

            if (!IsHarvested)
            {
                // Debug.Log($"[Corpse] Труп исчез по таймеру: {InstanceId}");

                // Уведомляем менеджер
                if (CorpseManager.Instance != null && !string.IsNullOrEmpty(InstanceId))
                {
                    CorpseManager.Instance.UnregisterCorpse(InstanceId);
                }

                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (CorpseManager.Instance != null && !string.IsNullOrEmpty(InstanceId))
            {
                // Не удаляем файл при выходе из игры
                if (!CorpseManager.Instance.IsQuitting)
                {
                    CorpseManager.Instance.UnregisterCorpse(InstanceId);
                    // Debug.Log($"[Corpse] OnDestroy — файл {InstanceId} удален");
                }
                // else
                // {
                //     Debug.Log($"[Corpse] OnDestroy при выходе — файл {InstanceId} сохранён");
                // }
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
        // public float massMultiplier = 0.5f;
        [Range(0, 1)] public float velocityInherit = 0.1f;
    }
}