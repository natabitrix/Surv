// Assets/Scripts/Interactables/RadialMenu.cs
using Assets.Scripts.Core;
using Assets.Scripts.Corpses;
using Assets.Scripts.Creatures;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.UI.RadialMenuUI;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    /// <summary>
    /// Маркер радиального меню на объекте.
    /// - Для существ: IRadialMenuContext (IInteractable — на BaseLivingEntity).
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

        [Header("Structure")]
        [Tooltip("Заголовок меню для структур (если не существ/трупов).")]
        public string structureTitleOverride;

        private BaseLivingEntity _living;
        private bool _hasLiving;

        private void Awake()
        {
            _living = GetComponent<BaseLivingEntity>();
            _hasLiving = _living != null;

            if (creature == null) creature = GetComponent<Creature>();
            if (corpse == null) corpse = GetComponent<Corpse>();
        }

        // ==========================================
        // === IRadialMenuContext ===
        // ==========================================

        public string GetMenuTitle()
        {
            if (_living != null) return _living.GetDisplayName();
            if (corpse != null && corpse.enabled) return corpse.gameObject.name;
            if (item != null) return item.itemName;
            if (!string.IsNullOrEmpty(structureTitleOverride)) return structureTitleOverride;
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
            if (_living != null)
            {
                if (_living.knockedOut) return global.knockedOutMenu;
                if (_living.tamed) return global.tamedMenu;
            }

            // === Труп ===
            if (corpse != null && corpse.enabled) return global.corpseMenu;

            // === Сундук ===
            if (GetComponent<ChestController>() != null) return global.chestMenu;

            // === Структура ===
            return global.structureMenu;
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
                    return _living != null && _living.HasInventory();
                case "unclaim":
                    return _living != null && _living.tamed;
                default:
                    return true;
            }
        }

        public void ExecuteAction(string actionId)
        {
            if (_living != null)
            {
                _living.ExecuteRadialAction(actionId);
                return;
            }

            Debug.Log($"[RadialMenu] Action: {actionId} (нет BaseLivingEntity — структура/предмет)");
        }

        public string GetRadioValue(string radioGroupId)
        {
            if (_living != null) return _living.GetRadialRadioValue(radioGroupId);
            return null;
        }

        public void SetRadioValue(string radioGroupId, string radioValue)
        {
            if (_living != null) _living.SetRadialRadioValue(radioGroupId, radioValue);
        }

        public string GetDynamicLabel(string actionId)
        {
            if (_living != null) return _living.GetRadialDynamicLabel(actionId);
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