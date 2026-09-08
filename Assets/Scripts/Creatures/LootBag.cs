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
        public void Initialize(ChestInventory corpseInventory, ChestUI chestUI, float corpseDisappearTime)
        {
            _inventory = corpseInventory;
            _chestUI = chestUI;
            _bagDisappearTime = corpseDisappearTime * 0.6f;
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
            if (_isOpen) CloseInventory();
            else if (_chestUI != null)
            {
                _chestUI.OpenWith(_inventory);
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

        private IEnumerator DespawnAfterTime()
        {
            yield return new WaitForSeconds(_bagDisappearTime);

            Debug.Log($"[LootBag] Сумка исчезла по таймеру на {transform.position}");
            
            CloseInventory();
            Destroy(gameObject);
        }

        public void ForceDespawn()
        {
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }

            CloseInventory();
            Destroy(gameObject);
        }
    }
}