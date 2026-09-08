using UnityEngine;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Core;
using System.Collections;

namespace Assets.Scripts.Creatures
{
    /// <summary>
    /// PlayerCorpse — труп игрока после его смерти.
    /// Содержит вещи игрока и выпадает дроп при разборе.
    /// Другие игроки могут открыть инвентарь и взять вещи.
    /// </summary>
    public class PlayerCorpse : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        [SerializeField] private ChestInventory _inventory;
        [SerializeField] private ChestUI _chestUI;
        private bool _isOpen = false;

        [Header("Despawn Settings")]
        [SerializeField] private float _corpseDisappearTime = 300f;
        [SerializeField] private GameObject _lootBagPrefab;

        // Ресурсы, которые падают при разборе трупа игрока (мясо, кожа и т.д.)
        [Header("Player Corpse Resources")]
        [SerializeField] private Item _meatDropItem;
        [SerializeField] private int _meatDropAmount = 5;
        [SerializeField] private Item _skinDropItem;
        [SerializeField] private int _skinDropAmount = 3;

        private InventoryData _mainInventoryData;
        private InventoryData _hotbarInventoryData;
        private Coroutine _despawnCoroutine;
        private bool _isHarvested = false;

        private void Start()
        {
            // Добавляем коллайдер если его нет
            if (_interactionCollider == null)
            {
                var col = gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(1f, 1.8f, 1f);
                col.center = new Vector3(0, 0.9f, 0);
                col.isTrigger = true;
                _interactionCollider = col;
            }

            if (_despawnCoroutine != null)
                StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = StartCoroutine(DespawnAfterTime());
        }

        /// <summary>
        /// Инициализирует труп игрока с его вещами
        /// </summary>
        public void Initialize(InventoryData mainInventory, InventoryData hotbarInventory, ChestUI chestUI)
        {
            _mainInventoryData = mainInventory;
            _hotbarInventoryData = hotbarInventory;
            _chestUI = chestUI;

            // Создаём инвентарь трупа и копируем туда все вещи игрока
            _inventory = gameObject.AddComponent<ChestInventory>();
            string corpseKey = $"PlayerCorpse_{System.Guid.NewGuid().ToString()}";
            _inventory.Initialize(30, corpseKey); // Больше слотов для всех вещей

            CopyPlayerItemsToCorpse();

            // Добавляем визуальное представление трупа (можно использовать игрока как визуал или спрайт)
            var visualObj = new GameObject("CorpseVisual");
            visualObj.transform.SetParent(transform);
            visualObj.transform.localPosition = Vector3.zero;

            // Простой визуал
            var renderer = visualObj.AddComponent<SpriteRenderer>();
            renderer.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }

        private void CopyPlayerItemsToCorpse()
        {
            if (_inventory?.Data == null) return;

            // Копируем вещи из основного инвентаря
            if (_mainInventoryData?.slots != null)
            {
                foreach (var slot in _mainInventoryData.slots)
                {
                    if (!slot.IsEmpty && slot.item != null)
                    {
                        _inventory.Data.AddItemAnywhere(slot.item, slot.count, slot.currentDurability);
                    }
                }
            }

            // Копируем вещи из хотбара
            if (_hotbarInventoryData?.slots != null)
            {
                foreach (var slot in _hotbarInventoryData.slots)
                {
                    if (!slot.IsEmpty && slot.item != null)
                    {
                        _inventory.Data.AddItemAnywhere(slot.item, slot.count, slot.currentDurability);
                    }
                }
            }
        }

        public InteractType GetInteractType() => InteractType.OpenTargetInventory;
        public InteractType GetInteractType2() => InteractType.Interact;

        public ChestInventory GetInventory() => _inventory;
        public bool HasInventory() => _inventory != null;
        public bool ShouldDetachAfterInteract() => false;

        public void Interact(InteractContext context)
        {
            if (_isHarvested) return;

            if (context.isTargetInventory)
            {
                OpenInventory();
            }
            else if (context.IsAttack)
            {
                HandleHarvest();
            }
        }

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
                Debug.LogError("[PlayerCorpse] _chestUI не назначен!");
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

        private void HandleHarvest()
        {
            if (_isHarvested) return;
            _isHarvested = true;

            // Выпадают ресурсы при разборе
            if (_meatDropItem != null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    var handler = player.GetComponent<ItemHandler>();
                    if (handler != null)
                    {
                        handler.PickupItem(_meatDropItem, _meatDropAmount);
                    }
                }
            }

            if (_skinDropItem != null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    var handler = player.GetComponent<ItemHandler>();
                    if (handler != null)
                    {
                        handler.PickupItem(_skinDropItem, _skinDropAmount);
                    }
                }
            }

            // Создаём сумку с оставшимися вещами
            if (HasLootInInventory() && _lootBagPrefab != null)
            {
                CreateLootBag();
            }

            // Отключаем таймер исчезновения
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }

            // Удаляем труп
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
                Debug.LogWarning("[PlayerCorpse] LootBag префаб не назначен!");
                return;
            }

            GameObject bagGO = Instantiate(_lootBagPrefab, transform.position, Quaternion.identity);
            var lootBag = bagGO.GetComponent<LootBag>();
            if (lootBag != null)
            {
                lootBag.Initialize(_inventory, _chestUI, _corpseDisappearTime);
            }
            else
            {
                Debug.LogWarning("[PlayerCorpse] На префабе сумки нет компонента LootBag!");
                Destroy(bagGO);
            }
        }

        private IEnumerator DespawnAfterTime()
        {
            yield return new WaitForSeconds(_corpseDisappearTime);

            if (!_isHarvested)
            {
                Debug.Log($"[PlayerCorpse] Труп игрока исчез по таймеру на {transform.position}");
                CloseInventory();
                Destroy(gameObject);
            }
        }
    }
}