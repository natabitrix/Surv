using Assets.Scripts.Core;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    public class Drinkable : MonoBehaviour, IInteractable // ← реализует IInteractable напрямую
    {

        public InteractType GetInteractType() => InteractType.Drink;
        public ChestInventory GetInventory() => null;
        public bool HasInventory() => false;
        public bool ShouldDetachAfterInteract() => false;
        private PlayerController _localPlayer = PlayerController.LocalPlayer;

        public void Interact(InteractContext context)
        {
            if (_localPlayer != null)
            {
                var survival = _localPlayer.GetComponent<PlayerSurvivalSystem>();

                if (survival != null) survival.AddWater();
            }

        }
    }
}