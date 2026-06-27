using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class ChestController : MonoBehaviour, IInteractable
    {
        public Animator chestAnim;
        public bool HasInventory() => true;
        public bool ShouldDetachAfterInteract() => false;
        public InteractType GetInteractType() => InteractType.OpenTargetInventory; // E - Открыть
        // public InteractType GetInteractType2() => InteractType.OpenTargetInventory; // F - Открыть
        [SerializeField] private ChestUI _chestUI;
        [SerializeField] private PlayerInputHandler _input;
        private bool _isOpen = false;

        private void Update()
        {
            // Обработка нажатия клавиши openInventory (tab) для закрытия инвентаря
            if (_input.openInventory && _isOpen)
            {
                if (chestAnim != null) chestAnim.SetTrigger("close"); // закрыть
                _isOpen = false;
            }
        }

        public void Close()
        {
            if (_isOpen)
            {
                if (chestAnim != null) chestAnim.SetTrigger("close");
                _isOpen = false;
                _chestUI.Close();
            }
        }

        public void Interact(InteractContext context)
        {
            if (_isOpen)
            {
                Close();
            }
            else
            {
                Debug.Log("chestAnim: " + chestAnim);
                if (chestAnim != null) chestAnim.SetTrigger("open");
                var chestInventory = GetComponent<ChestInventory>();
                _chestUI.OpenWith(chestInventory);
                _isOpen = true;
            }
        }


        public ChestInventory GetInventory()
        {
            return null;
        }
    }
}