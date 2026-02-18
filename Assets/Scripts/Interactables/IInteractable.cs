using Assets.Scripts.InventorySystem;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    public interface IInteractable
    {
        void Interact(InteractContext context);
        InteractType GetInteractType();
        bool HasInventory();
        bool ShouldDetachAfterInteract();
        ChestInventory GetInventory();

    }

}