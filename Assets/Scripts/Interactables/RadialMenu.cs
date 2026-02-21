using Assets.Scripts.Core;
using Assets.Scripts.InventorySystem;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    public class RadialMenu : MonoBehaviour, IInteractable // ← реализует IInteractable напрямую
    {
        public InteractType GetInteractType() => InteractType.RadialMenu;
        public ChestInventory GetInventory() => null;
        public bool HasInventory() => false;
        public bool ShouldDetachAfterInteract() => false;
        public void Interact(InteractContext context) { }
    }
}