// Assets/Scripts/Interactables/Types/InteractContext.cs
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    public struct InteractContext
    {
        // public ToolType Tool;
        public AttackAnimationType Tool;
        public bool IsAttack;
        public bool isTargetInventory;
        public PlayerInteraction PlayerInteraction;
    }
}