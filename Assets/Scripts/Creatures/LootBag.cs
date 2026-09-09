using UnityEngine;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Core;
using System.Collections;

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
        private IInteractable _source; // ← новое поле
        private bool _isOpen = false;

        [Header("Despawn Settings")]
        [SerializeField] private float _bagDisappearTime = 180f;

        private Coroutine _despawnCoroutine;

        private void Start()
        {
            if (_despawnCoroutine != null)
                StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = StartCoroutine(DespawnAfterTime());
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

        public void OpenInventory_()
        {
            if (_isOpen) CloseInventory();
            else if (_chestUI != null)
            {
                _chestUI.OpenWith(_inventory, _source);
                _isOpen = true;
            }
            else
            {
                Debug.LogError("[LootBag] _chestUI не назначен!");
            }
        }

        public void CloseInventory_()
        {
            if (_isOpen && _chestUI != null)
            {
                _chestUI.Close();
                _isOpen = false;
            }
            // if (IsInventoryEmpty())
            // {
            //     ForceDespawn();
            // }
        }

public void OpenInventory()
{
    Debug.Log($"[LootBag] ===== OPEN INVENTORY CALLED =====");
    Debug.Log($"[LootBag] _isOpen: {_isOpen}");
    Debug.Log($"[LootBag] _inventory: {(_inventory != null ? _inventory.name : "null")}");
    Debug.Log($"[LootBag] _source: {(_source != null ? _source.GetType().Name : "null")}");
    
    // ✅ Если уже открыт - сначала закрываем
    if (_isOpen)
    {
        Debug.Log($"[LootBag] Уже открыт, вызываем CloseInventory()");
        CloseInventory();
        // ✅ После закрытия продолжаем открывать заново!
    }
    
    // ✅ Теперь открываем (даже если был закрыт)
    if (_chestUI != null)
    {
        Debug.Log($"[LootBag] Вызываем _chestUI.OpenWith() с source = {(_source != null ? _source.GetType().Name : "null")}");
        _chestUI.OpenWith(_inventory, _source);
        _isOpen = true;
        Debug.Log($"[LootBag] Инвентарь открыт, _isOpen = true");
    }
    else
    {
        Debug.LogError("[LootBag] _chestUI не назначен!");
    }
    
    Debug.Log($"[LootBag] ===== OPEN INVENTORY FINISHED =====");
}

public void CloseInventory()
{
    Debug.Log($"[LootBag] ===== CLOSE INVENTORY CALLED =====");
    Debug.Log($"[LootBag] _isOpen: {_isOpen}");
    
    if (_isOpen && _chestUI != null)
    {
        Debug.Log($"[LootBag] Вызываем _chestUI.Close()");
        _chestUI.Close();
        _isOpen = false;
        Debug.Log($"[LootBag] Инвентарь закрыт, _isOpen = false");
    }
    Debug.Log($"[LootBag] ===== CLOSE INVENTORY FINISHED =====");
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
            // yield return new WaitForSeconds(_bagDisappearTime);

            // Debug.Log($"[LootBag] Сумка исчезла по таймеру на {transform.position}");

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

        public void ForceDespawn()
        {
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }

            // CloseInventory();
            Destroy(gameObject);
        }
    }
}