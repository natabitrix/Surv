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
        [SerializeField] private BoxCollider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        [SerializeField] private CapsuleCollider _capsuleCollider;

        [SerializeField] private ChestInventory _inventory;
        private bool _isOpen = false;

        [Header("Despawn Settings")]
        [SerializeField] private float _bagDisappearTime = 180f;

        [Header("Persistence")]
        public string InstanceId { get; set; }
        public string OwnerPlayerId { get; set; } = "world";

        private Coroutine _despawnCoroutine;
        private bool _isDespawning = false;
        private bool _hasSettled = false; // Флаг: сумка уже упала и стала "призрачной"

        [Header("Physics")]
        [SerializeField] private float _settlingThreshold = 0.1f; // Порог скорости, считаем что упала
        [SerializeField] private float _settleCheckDelay = 0.5f; // Задержка перед проверкой остановки (чтобы не сработало сразу)

        private Rigidbody _rb;

        private void Start()
        {
            if (_despawnCoroutine != null) StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = StartCoroutine(DespawnAfterTime());

            if (_inventory?.Data != null)
            {
                _inventory.Data.OnInventoryChanged += OnInventoryChanged;
            }

            _rb = GetComponent<Rigidbody>();
            
            // Убедимся, что физика включена при спавне
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.detectCollisions = true;

                // Генерируем случайное направление по X и Z
                Vector3 randomPush = new Vector3(
                    Random.Range(-100f, 100f), 
                    0f, 
                    Random.Range(-100f, 100f)
                );
                
                // Применяем силу. ForceMode.Impulse дает мгновенный толчок
                _rb.AddForce(randomPush, ForceMode.Impulse);
            }
            
            // Запускаем корутину проверки остановки
            StartCoroutine(CheckSettling());
        }

        /// <summary>
        /// Проверяет, остановилась ли сумка, чтобы отключить физику и коллайдеры
        /// </summary>
        private IEnumerator CheckSettling()
        {
            // Ждем немного, чтобы сумка успела начать падать и не сработала триггер сразу при спавне
            yield return new WaitForSeconds(_settleCheckDelay);

            while (!_hasSettled && _rb != null)
            {
                // Проверяем скорость. linearVelocity.magnitude - это текущая скорость
                if (_rb.linearVelocity.magnitude < _settlingThreshold && _rb.angularVelocity.magnitude < _settlingThreshold)
                {
                    SettleBag();
                    break;
                }
                
                yield return null; // Ждем следующего кадра
            }
        }

        /// <summary>
        /// Отключает физику и коллайдеры, фиксируя положение сумки
        /// </summary>
        /// <summary>
        /// Отключает физику и коллайдеры, фиксируя положение сумки
        /// </summary>
        private void SettleBag()
        {
            if (_hasSettled) return;
            _hasSettled = true;

            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            // Вместо отключения коллайдеров, меняем слой на тот, который не имеет коллизий
            // Предварительно создайте слой "LootGhost" в Unity и узнайте его ID
            // gameObject.layer = LayerMask.NameToLayer("LootGhost"); 
            
            // Коллайдеры остаются включенными, но благодаря настройкам Physics Matrix они ни с чем не сталкиваются
            // При этом Raycast всё равно может их находить, если вы указываете нужный LayerMask в луче

            transform.SetPositionAndRotation(pos, rot);
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