using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class ChestController : MonoBehaviour, IInteractable, IInventorySource
    {
        [Header("Animation")]
        public Animator chestAnim;

        [Header("Inventory")]
        [SerializeField] private ChestInventory _inventory;

        private bool _isOpen = false;

        private void Awake()
        {
            if (_inventory == null)
                _inventory = GetComponent<ChestInventory>();
        }

        // === IInteractable ===
        public InteractType GetInteractType() => InteractType.OpenTargetInventory;
        public InteractType GetInteractType2() => InteractType.None;
        public bool ShouldDetachAfterInteract() => false;

        public void Interact(InteractContext context)
        {
            if (!context.isTargetInventory) return;
            Open();
        }

        // === IInventorySource ===
        public ChestInventory GetInventory() => _inventory;
        public bool HasInventory() => _inventory != null;

        public void OnInventoryOpened()
        {
            _isOpen = true;
            if (chestAnim != null) chestAnim.SetTrigger("open");
        }

        public void OnInventoryClosed()
        {
            _isOpen = false;
            if (chestAnim != null) chestAnim.SetTrigger("close");
        }

        // === Управление через менеджер ===
        public void Open()
        {
            if (ChestUIManager.Instance == null)
            {
                Debug.LogError("[ChestController] ChestUIManager не найден!");
                return;
            }

            if (_inventory == null)
            {
                Debug.LogError($"[ChestController] _inventory == null на {gameObject.name}!");
                return;
            }

            ChestUIManager.Instance.Toggle(this);
        }

        public void Close()
        {
            if (_isOpen && ChestUIManager.Instance != null)
            {
                ChestUIManager.Instance.Close();
            }
        }

        private void OnDestroy()
        {
            // Если этот сундук был открыт — закрываем UI
            if (ChestUIManager.Instance != null && ReferenceEquals(ChestUIManager.Instance.CurrentSource, this))
            {
                ChestUIManager.Instance.Close();
            }
        }
    }
}