using Assets.Scripts.Core;
using Assets.Scripts.InventorySystem;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    public class Drinkable : MonoBehaviour, IInteractable // ← реализует IInteractable напрямую
    {

        public InteractType GetInteractType() => InteractType.Drink;
        public ChestInventory GetInventory() => null;
        public bool HasInventory() => false;
        public bool ShouldDetachAfterInteract() => false;

        private PlayerSurvivalSystem _survival => PlayerSurvivalSystem.Instance;

        public void Interact(InteractContext context)
        {
            _survival.AddWater();
        }
    }
}