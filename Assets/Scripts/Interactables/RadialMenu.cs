using Assets.Scripts.Core;
using Assets.Scripts.Creatures;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    public class RadialMenu : MonoBehaviour, IInteractable // ← реализует IInteractable напрямую
    {
        [Header("Target Item")]
        public Item item;
        public Corpse corpse;
        public Creature creature;
        public InteractType GetInteractType() => InteractType.RadialMenu;
        public ChestInventory GetInventory() => null;
        public bool HasInventory() => false;
        public bool ShouldDetachAfterInteract() => false;
        public void Interact(InteractContext context) { }
    }
}