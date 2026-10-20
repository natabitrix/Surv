// Assets/Scripts/Interactables/RadialMenu.cs
using Assets.Scripts.Core;
using Assets.Scripts.Corpses;
using Assets.Scripts.Creatures;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Structures;
using Assets.Scripts.UI.RadialMenuUI;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    /// <summary>
    /// Маркер радиального меню на объекте.
    /// - Для существ: IRadialMenuContext (IInteractable — на Creature).
    /// - Для структур: IRadialMenuContext + IInteractable (возвращает InteractType.RadialMenu).
    ///
    /// Конфиги берутся из GameAssets.RadialMenuGlobalConfig по состоянию.
    /// Особенности существа (canBePickedUp, canBeRidden) — из CreatureData.
    /// </summary>
    public class RadialMenu : MonoBehaviour, IRadialMenuContext, IInteractable
    {
        [Header("Targets")]
        public Item item;
        public Corpse corpse;
        public Creature creature;
        public Structure structure;
        public ChestController chest;

        // [Header("Structure")]
        // [Tooltip("Заголовок меню для структур (если не существ/трупов).")]
        // public string structureTitleOverride;

        // private Creature _living;

        private void Awake()
        {
            // _living = GetComponent<Creature>();

            if (creature == null) creature = GetComponent<Creature>();
            if (corpse == null) corpse = GetComponent<Corpse>();
            if (structure == null) structure = GetComponent<Structure>();
            if (chest == null) chest = GetComponent<ChestController>();
        }

        // ==========================================
        // === IRadialMenuContext ===
        // ==========================================

        public string GetMenuTitle()
        {
            if (creature != null) return creature.GetDisplayName();
            // if (corpse != null && corpse.enabled) return corpse.gameObject.name;
            if (corpse != null && corpse.enabled) return corpse.GetDisplayName();
            if (structure != null && structure.enabled) return structure.GetDisplayName();
            if (item != null) return item.itemName;
            return gameObject.name;
        }

        public RadialMenuConfig GetMenuConfig()
        {
            var global = GameAssets.Instance?.RadialMenuGlobalConfig;
            if (global == null)
            {
                Debug.LogWarning("[RadialMenu] GameAssets.RadialMenuGlobalConfig == null!");
                return null;
            }

            // === Существо ===
            if (creature != null)
            {
                if (creature.knockedOut) return global.knockedOutMenu;
                if (creature.tamed) return global.tamedMenu;
            }

            // === Труп ===
            if (corpse != null && corpse.enabled) return global.corpseMenu;

            // === Сундук ===
            if (chest != null) return global.chestMenu;

            // === Структура ===
            if (structure != null) return global.structureMenu;

            return null;
        }

        public bool IsActionAvailable(string actionId)
        {
            switch (actionId)
            {
                case "pick_up":
                    return creature != null && creature.Data != null && creature.Data.canBePickedUp;
                case "ride":
                    return creature != null && creature.Data != null && creature.Data.canBeRidden;
                case "open_inventory":
                    return (corpse != null && corpse.HasInventory()) || (creature != null && creature.Interactable.HasInventory()) || (structure != null && structure.HasInventory());
                case "unclaim":
                    return creature != null && creature.tamed;
                default:
                    return true;
            }
        }


        public string GetMenuButtonLabelByActionId(string actionId)
        {
            RadialMenuConfig config = GetMenuConfig();
            if (config == null || config.rootEntries == null || config.rootEntries.Length == 0)
            {
                return "";
            }

            foreach (var entry in config.rootEntries)
            {
                if (entry.actionId == actionId) return entry.label;

                foreach (var child in entry.children)
                {
                    if (child.actionId == actionId) return child.label;

                    foreach (var childchild in child.children)
                    {
                        if (childchild.actionId == actionId) return childchild.label;
                    }
                }
            }

            return "";
        }

        public void ExecuteAction(string actionId)
        {
            if (creature != null && creature.enabled)
            {
                creature.ExecuteRadialAction(actionId);
                return;
            }

            if (corpse != null && corpse.enabled)
            {
                corpse.ExecuteRadialAction(actionId);
                return;
            }

            if (structure != null && structure.enabled)
            {
                structure.ExecuteRadialAction(actionId);
                return;
            }

            if (chest != null)
            {
                structure.ExecuteRadialAction(actionId);
                return;
            }



            Debug.Log($"[RadialMenu] Action: {actionId} (нет Creature — структура/предмет)");
        }


        public string GetRadioValue(string radioGroupId)
        {
            if (creature != null) return creature.GetRadialRadioValue(radioGroupId);
            return null;
        }

        public void SetRadioValue(string radioGroupId, string radioValue)
        {
            if (creature != null) creature.SetRadialRadioValue(radioGroupId, radioValue);
        }

        public string GetDynamicLabel(string actionId)
        {
            if (creature != null) return creature.GetRadialDynamicLabel(actionId);
            return null;
        }

        // ==========================================
        // === IInteractable ===
        // ==========================================

        public InteractType GetInteractType() => InteractType.RadialMenu;
        public InteractType GetInteractType2() => InteractType.None;
        public void Interact(InteractContext context) { }
        public ChestInventory GetInventory() => null;
        public bool HasInventory() => false;
        public bool ShouldDetachAfterInteract() => false;
    }
}