using UnityEngine;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Core;
using System.Collections;

namespace Assets.Scripts.Loot
{
    /// <summary>
    /// Сумка с вещами. Появляется при разборе трупа, выбрасывании вещей,
    /// разрушении сундука и т.д. Исчезает по таймеру или когда вещи забрали.
    /// </summary>
    public class LootBag : MonoBehaviour, IInteractable, IInventorySource
    {
        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        [SerializeField] private ChestInventory _inventory;
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
            // Если сумка была открыта — закрываем UI
            if (ChestUIManager.Instance != null
                && ReferenceEquals(ChestUIManager.Instance.CurrentSource, this))
            {
                ChestUIManager.Instance.Close();
            }

            if (_inventory?.Data != null)
            {
                _inventory.Data.OnInventoryChanged -= OnInventoryChanged;
            }

            // Только чистим in-memory, файл НЕ трогаем.
            // Файл удаляется явно через UnregisterLootBag в ForceDespawn.
            if (LootBagManager.Instance != null && !string.IsNullOrEmpty(InstanceId))
            {
                LootBagManager.Instance.ForgetLootBag(InstanceId);
            }
        }

        private void OnInventoryChanged()
        {
            if (IsInventoryEmpty())
            {
                // Debug.Log($"[LootBag] Инвентарь пуст! Исчезаем.");
                ForceDespawn();
            }
        }

        /// <summary>
        /// Инициализация сумки. ChestUI больше не нужен — открытие через ChestUIManager.
        /// </summary>
        public void Initialize(ChestInventory inventory, float disappearTime, IInteractable source = null)
        {
            _inventory = inventory;
            _bagDisappearTime = disappearTime;
        }

        public void SetPersistenceData(string instanceId, string ownerPlayerId)
        {
            InstanceId = instanceId;
            OwnerPlayerId = ownerPlayerId;
        }

        public void SetRemainingTime(float remainingTime)
        {
            _bagDisappearTime = remainingTime;
        }

        // === IInteractable ===
        public InteractType GetInteractType() => InteractType.OpenTargetInventory;
        public InteractType GetInteractType2() => InteractType.None;
        public bool ShouldDetachAfterInteract() => false;

        public void Interact(InteractContext context)
        {
            if (context.isTargetInventory)
            {
                OpenInventory();
            }
        }

        // === IInventorySource ===
        public ChestInventory GetInventory() => _inventory;
        public bool HasInventory() => _inventory != null;

        public void OnInventoryOpened()
        {
            _isOpen = true;
        }

        public void OnInventoryClosed()
        {
            _isOpen = false;
        }

        // === Открытие/закрытие через менеджер ===
        public void OpenInventory()
        {
            if (ChestUIManager.Instance == null)
            {
                Debug.LogError("[LootBag] ChestUIManager не найден!");
                return;
            }

            if (_inventory == null)
            {
                Debug.LogError("[LootBag] _inventory == null!");
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
                    ForceDespawn();
                    yield break;
                }

                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }

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

            if (ChestUIManager.Instance != null
                && ReferenceEquals(ChestUIManager.Instance.CurrentSource, this))
            {
                ChestUIManager.Instance.Close();
            }

            // Удаляем с диска — это окончательный despawn.
            if (LootBagManager.Instance != null && !string.IsNullOrEmpty(InstanceId))
            {
                LootBagManager.Instance.UnregisterLootBag(InstanceId);
            }

            Destroy(gameObject);
        }

    }
}