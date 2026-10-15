// Assets/Scripts/Creatures/Components/CreatureCommands.cs
using UnityEngine;
using Assets.Scripts.Creatures.Taming;

namespace Assets.Scripts.Creatures.Components
{
    /// <summary>
    /// Команды прирученного + обработка радиального меню.
    /// </summary>
    public class CreatureCommands : MonoBehaviour
    {
        private Creature _creature;

        private void Awake()
        {
            _creature = GetComponent<Creature>();
            if (_creature == null)
                Debug.LogError($"[CreatureCommands] Нет Creature на {gameObject.name}!");
        }

        // ==========================================
        // === КОМАНДЫ ===
        // ==========================================

        public void SetTamedFollowing(bool value)
        {
            if (!_creature.tamed) return;
            _creature.tamedFollowing = value;
            if (value) _creature.tamedWandering = false;
        }

        public void SetTamedWandering(bool value)
        {
            if (!_creature.tamed) return;
            _creature.tamedWandering = value;
            if (value)
            {
                _creature.tamedFollowing = false;
                _creature.TamedAI.ResetWanderState();
            }
        }

        public void SetTamedMating(bool value)
        {
            if (!_creature.tamed) return;
            _creature.tamedMating = value;
        }

        public void SetTamedBehavior(TamedBehavior value)
        {
            if (!_creature.tamed) return;
            _creature.tamedBehavior = value;
        }

        public void SetFollowDistance(FollowDistance value)
        {
            if (!_creature.tamed) return;
            _creature.tamedFollowDistance = value;
        }

        // ==========================================
        // === RADIAL MENU ===
        // ==========================================

        public void ExecuteRadialAction(string actionId)
        {
            switch (actionId)
            {
                case "toggle_follow":
                    SetTamedFollowing(!_creature.tamedFollowing);
                    Debug.Log($"[{gameObject.name}] Follow = {_creature.tamedFollowing}");
                    break;

                case "toggle_wander":
                    SetTamedWandering(!_creature.tamedWandering);
                    Debug.Log($"[{gameObject.name}] Wander = {_creature.tamedWandering}");
                    break;

                case "toggle_mating":
                    SetTamedMating(!_creature.tamedMating);
                    Debug.Log($"[{gameObject.name}] Mating = {_creature.tamedMating}");
                    break;

                case "open_inventory":
                    _creature.OpenInventory();
                    Debug.Log($"[{gameObject.name}] OpenInventory");
                    break;

                case "flee":
                    SetTamedBehavior(TamedBehavior.Flee);
                    Debug.Log($"[{gameObject.name}] TamedBehavior = Flee");
                    break;

                case "passive":
                    SetTamedBehavior(TamedBehavior.Passive);
                    Debug.Log($"[{gameObject.name}] TamedBehavior = Passive");
                    break;

                case "neutral":
                    SetTamedBehavior(TamedBehavior.Neutral);
                    Debug.Log($"[{gameObject.name}] TamedBehavior = Neutral");
                    break;

                case "attack_owner_target":
                    SetTamedBehavior(TamedBehavior.AttackOwnerTarget);
                    Debug.Log($"[{gameObject.name}] TamedBehavior = AttackOwnerTarget");
                    break;

                case "aggressive":
                    SetTamedBehavior(TamedBehavior.Aggressive);
                    Debug.Log($"[{gameObject.name}] TamedBehavior = Aggressive");
                    break;

                case "pick_up":
                    Debug.Log($"[{gameObject.name}] Pick up (TODO)");
                    break;

                case "ride":
                    Debug.Log($"[{gameObject.name}] Ride (TODO)");
                    break;

                case "rename":
                    Debug.Log($"[{gameObject.name}] Rename (TODO)");
                    break;

                case "unclaim":
                    _creature.Unclaim();
                    break;

                default:
                    Debug.LogWarning($"[{gameObject.name}] Неизвестное действие: {actionId}");
                    break;
            }

            // Сохраняем изменения
            if (_creature.tamed &&
                !string.IsNullOrEmpty(_creature.TamingInstanceId) &&
                TamingManager.Instance != null)
            {
                TamingManager.Instance.UpdateSave(_creature.TamingInstanceId, "player_001");
            }
        }

        public string GetRadialRadioValue(string radioGroupId)
        {
            switch (radioGroupId)
            {
                case "behavior":
                    return TamedBehaviorToId(_creature.tamedBehavior);
                case "follow_distance":
                    return FollowDistanceToId(_creature.tamedFollowDistance);
                default:
                    return null;
            }
        }

        public void SetRadialRadioValue(string radioGroupId, string radioValue)
        {
            switch (radioGroupId)
            {
                case "behavior":
                    _creature.tamedBehavior = IdToTamedBehavior(radioValue);
                    Debug.Log($"[{gameObject.name}] Behavior = {_creature.tamedBehavior}");
                    break;
                case "follow_distance":
                    _creature.tamedFollowDistance = IdToFollowDistance(radioValue);
                    Debug.Log($"[{gameObject.name}] FollowDistance = {_creature.tamedFollowDistance}");
                    break;
            }

            if (_creature.tamed &&
                !string.IsNullOrEmpty(_creature.TamingInstanceId) &&
                TamingManager.Instance != null)
            {
                TamingManager.Instance.UpdateSave(_creature.TamingInstanceId, "player_001");
            }
        }

        public string GetRadialDynamicLabel(string actionId)
        {
            switch (actionId)
            {
                case "toggle_follow":
                    return _creature.tamedFollowing ? "Отключить следование" : "Включить следование";
                case "toggle_wander":
                    return _creature.tamedWandering ? "Отключить блуждание" : "Включить блуждание";
                case "toggle_mating":
                    return _creature.tamedMating ? "Отключить спаривание" : "Включить спаривание";
                default:
                    return null;
            }
        }

        // ==========================================
        // === ХЕЛПЕРЫ enum ↔ string ===
        // ==========================================

        public static string TamedBehaviorToId(TamedBehavior value)
        {
            switch (value)
            {
                case TamedBehavior.Passive: return "passive";
                case TamedBehavior.Flee: return "flee";
                case TamedBehavior.Neutral: return "neutral";
                case TamedBehavior.AttackOwnerTarget: return "attack_owner_target";
                case TamedBehavior.Aggressive: return "aggressive";
                default: return "neutral";
            }
        }

        public static TamedBehavior IdToTamedBehavior(string id)
        {
            switch (id)
            {
                case "passive": return TamedBehavior.Passive;
                case "flee": return TamedBehavior.Flee;
                case "neutral": return TamedBehavior.Neutral;
                case "attack_owner_target": return TamedBehavior.AttackOwnerTarget;
                case "aggressive": return TamedBehavior.Aggressive;
                default: return TamedBehavior.Neutral;
            }
        }

        public static string FollowDistanceToId(FollowDistance value)
        {
            switch (value)
            {
                case FollowDistance.Lowest: return "lowest";
                case FollowDistance.Low: return "low";
                case FollowDistance.Medium: return "medium";
                case FollowDistance.High: return "high";
                case FollowDistance.Highest: return "highest";
                default: return "medium";
            }
        }

        public static FollowDistance IdToFollowDistance(string id)
        {
            switch (id)
            {
                case "lowest": return FollowDistance.Lowest;
                case "low": return FollowDistance.Low;
                case "medium": return FollowDistance.Medium;
                case "high": return FollowDistance.High;
                case "highest": return FollowDistance.Highest;
                default: return FollowDistance.Medium;
            }
        }

        public string TamedBehaviorToText()
        {
            var id = TamedBehaviorToId(_creature.tamedBehavior);

            if (_creature.TryGetComponent<Interactables.RadialMenu>(out var menu))
            {
                var text = menu.GetMenuButtonLabelByActionId(id);
                if (text != "") return text;
            }
            return id;
        }
    }
}