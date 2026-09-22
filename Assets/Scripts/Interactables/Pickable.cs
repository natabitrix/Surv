using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    // Помещает подбираемый объект в инвентарь игрока и после удаляет его со сцены
    public class Pickable : MonoBehaviour, IInteractable // ← реализует IInteractable напрямую
    {
        [Header("Item to Pickup")]
        public Item item;

        [Header("Amount to Pickup")]
        public int amount;

        public InteractType GetInteractType() => InteractType.Pickup;
        public ChestInventory GetInventory() => null;
        public PlayerController playerController; // Назначь в инспекторе

        private bool _isPickedUp = false;
        public bool HasInventory() => false;

        public void Interact(InteractContext context)
        {
            if (_isPickedUp) return;

            var handler = context.PlayerInteraction?.GetComponent<ItemHandler>();
            if (handler == null)
            {
                Debug.LogError("[Pickable] No ItemHandler in context!");
                return;
            }

            if (handler.PickupItem(item, amount))
            {
                _isPickedUp = true;

                if (TryGetComponent<IPickupCallback>(out var callback))
                    callback.OnPickedUp();
                else
                    Destroy(gameObject);
            }
        }

        public bool ShouldDetachAfterInteract()
        {
            return _isPickedUp; // после подбора — отключаемся
        }
    }
}