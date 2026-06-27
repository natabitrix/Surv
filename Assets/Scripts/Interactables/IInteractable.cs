// Assets/Scripts/Interactables/Interactable.cs
using Assets.Scripts.InventorySystem;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    public interface IInteractable
    {
        void Interact(InteractContext context);
        InteractType GetInteractType();
        InteractType GetInteractType2() => InteractType.None;
        bool HasInventory();
        bool ShouldDetachAfterInteract();
        ChestInventory GetInventory();

    }

}