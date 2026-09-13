using UnityEngine;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Core;
using System.Collections;
using Assets.Scripts.UI;

namespace Assets.Scripts.Creatures
{
    /// <summary>
    /// LootBag — сумка, остающаяся после разбора трупа.
    /// Содержит лут из инвентаря трупа и исчезает через время.
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

        private Coroutine _despawnCoroutine;

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
            // ✅ Отписываемся при уничтожении
            if (_inventory?.Data != null)
            {
                _inventory.Data.OnInventoryChanged -= OnInventoryChanged;
            }
        }

        private void OnInventoryChanged()
        {
            // ✅ Проверяем, не стал ли инвентарь пустым
            if (IsInventoryEmpty())
            {
                Debug.Log($"[LootBag] Инвентарь стал пустым! Исчезаем немедленно.");
                ForceDespawn();
            }
        }

        /// <summary>
        /// Инициализирует сумку с инвентарём из трупа
        /// </summary>
        public void Initialize(ChestInventory corpseInventory, ChestUI chestUI, float corpseDisappearTime, IInteractable source = null)
        {
            _inventory = corpseInventory;
            _chestUI = chestUI;
            _bagDisappearTime = corpseDisappearTime * 0.6f;
            _source = source ?? this; // ← если source не передан, используем this
        }

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
            if (_isOpen)
            {
                CloseInventory();
            }

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
                // Проверяем каждую секунду, пуст ли инвентарь
                if (IsInventoryEmpty())
                {
                    Debug.Log($"[LootBag] Сумка исчезла — весь лут забран на {transform.position}");
                    ForceDespawn();
                    yield break;
                }

                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }

            // Если таймер истёк, исчезаем всё равно
            Debug.Log($"[LootBag] Сумка исчезла по таймеру на {transform.position}");
            ForceDespawn();
        }

        private bool _isDespawning = false;

        public void ForceDespawn()
        {
            // ✅ Защита от повторного вызова
            if (_isDespawning) return;
            _isDespawning = true;

            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }

            // Отписываемся от событий
            if (_inventory?.Data != null)
            {
                _inventory.Data.OnInventoryChanged -= OnInventoryChanged;
            }

            CloseInventory();
            
            // Закрываем панель в PanelsUIController
            var panelsController = FindAnyObjectByType<PanelsUIController>();
            if (panelsController != null)
            {
                panelsController.CloseAllPanels();
            }

            Destroy(gameObject);
            
        }
    }
}