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

            // Находим игрока и его ItemPickupHandler
            // var player = GameObject.FindGameObjectWithTag("Player");
            // if (player == null)
            // {
            //     Debug.LogError("Player not found!");
            //     return;
            // }
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>(); // Найти автоматически, если не назначен
            }

            ItemHandler pickupHandler = playerController.GetComponent<ItemHandler>();

            if (pickupHandler != null && pickupHandler.PickupItem(item, amount))
            {
                Debug.Log($"Picked up {amount}x {item.itemName}");
                _isPickedUp = true;
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Inventory is full!");
            }
        }
        public bool ShouldDetachAfterInteract()
        {
            return _isPickedUp; // после подбора — отключаемся
        }
    }
}