using UnityEngine;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Core;
using System.Collections;
using Assets.Scripts.UI;

namespace Assets.Scripts.Loot
{
    /// <summary>
    /// Сумка с вещами. Появляется при разборе трупа, выбрасывании вещей,
    /// разрушении сундука и т.д. Исчезает по таймеру или когда вещи забрали.
    /// </summary>
    public class LootBag : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        [SerializeField] private ChestInventory _inventory;
        [SerializeField] private ChestUI _chestUI;
        private IInteractable _source;
        private bool _isOpen = false;

        [Header("Despawn Settings")]
        [SerializeField] private float _bagDisappearTime = 180f;

        [Header("Persistence")]
        public string InstanceId { get; set; }
        public string OwnerPlayerId { get; set; } = "world";

        private Coroutine _despawnCoroutine;
        private bool _isDespawning = false;

        private void Start()
        {
            if (_despawnCoroutine != null) StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = StartCoroutine(DespawnAfterTime());

            if (_inventory?.Data != null)
            {
                _inventory.Data.OnInventoryChanged += OnInventoryChanged;
            }
        }

        private void OnDestroy()
        {
            if (_inventory?.Data != null)
            {
                _inventory.Data.OnInventoryChanged -= OnInventoryChanged;
            }

            // ✅ Уведомляем менеджер
            if (LootBagManager.Instance != null && !string.IsNullOrEmpty(InstanceId))
            {
                if (!LootBagManager.Instance.IsQuitting)
                {
                    LootBagManager.Instance.UnregisterLootBag(InstanceId);
                }
            }
        }

        private void OnInventoryChanged()
        {
            if (IsInventoryEmpty())
            {
                Debug.Log($"[LootBag] Инвентарь пуст! Исчезаем.");
                ForceDespawn();
            }
        }

        /// <summary>
        /// Инициализация через источник (труп, сундук и т.д.)
        /// </summary>
        public void Initialize(ChestInventory inventory, ChestUI chestUI, float disappearTime, IInteractable source = null)
        {
            _inventory = inventory;
            _chestUI = chestUI;
            _bagDisappearTime = disappearTime;
            _source = source ?? this;
        }

        /// <summary>
        /// Устанавливает данные из сохранения.
        /// </summary>
        public void SetPersistenceData(string instanceId, string ownerPlayerId)
        {
            InstanceId = instanceId;
            OwnerPlayerId = ownerPlayerId;
        }

        /// <summary>
        /// Устанавливает оставшееся время жизни.
        /// </summary>
        public void SetRemainingTime(float remainingTime)
        {
            _bagDisappearTime = remainingTime;
        }

        // === IInteractable ===

        public InteractType GetInteractType() => InteractType.OpenTargetInventory;
        public InteractType GetInteractType2() => InteractType.None;
        public ChestInventory GetInventory() => _inventory;
        public bool HasInventory() => _inventory != null;
        public bool ShouldDetachAfterInteract() => false;

        public void Interact(InteractContext context)
        {
            if (context.isTargetInventory)
            {
                OpenInventory();
            }
        }

        public void OpenInventory()
        {
            if (_isOpen) CloseInventory();

            if (_chestUI != null)
            {
                _chestUI.OpenWith(_inventory, _source);
                _isOpen = true;
            }
            else
            {
                Debug.LogError("[LootBag] _chestUI не назначен!");
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

        private bool IsInventoryEmpty()
        {
            if (_inventory?.Data?.slots == null) return true;

            foreach (var slot in _inventory.Data.slots)
            {
                if (!slot.IsEmpty && slot.item != null)
                    return false;
            }

            return true;
        }

        private IEnumerator DespawnAfterTime()
        {
            float elapsed = 0f;

            while (elapsed < _bagDisappearTime)
            {
                if (IsInventoryEmpty())
                {
                    Debug.Log($"[LootBag] Сумка исчезла — весь лут забран.");
                    ForceDespawn();
                    yield break;
                }

                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }

            Debug.Log($"[LootBag] Сумка исчезла по таймеру.");
            ForceDespawn();
        }

        public void ForceDespawn()
        {
            if (_isDespawning) return;
            _isDespawning = true;

            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }

            if (_inventory?.Data != null)
            {
                _inventory.Data.OnInventoryChanged -= OnInventoryChanged;
            }

            CloseInventory();

            var panelsController = FindAnyObjectByType<PanelsUIController>();
            if (panelsController != null)
            {
                panelsController.CloseAllPanels();
            }

            Destroy(gameObject);
        }
    }
}