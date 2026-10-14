// Assets/Scripts/Creatures/Components/CreatureTaming.cs
using UnityEngine;
using Assets.Scripts.UI.Notifications;
using Assets.Scripts.Creatures.Taming;

namespace Assets.Scripts.Creatures.Components
{
    /// <summary>
    /// Приручение существа: прогресс, torpor, еда, сохранение.
    /// </summary>
    public class CreatureTaming : MonoBehaviour
    {
        private Creature _creature;

        private float _tamingSaveTimer = 0f;
        private const float TamingSaveInterval = 10f;

        private void Awake()
        {
            _creature = GetComponent<Creature>();
            if (_creature == null)
                Debug.LogError($"[CreatureTaming] Нет Creature на {gameObject.name}!");
        }

        // ==========================================
        // === TICK ===
        // ==========================================

        /// <summary>Тик еды + периодическое сохранение. Вызывается из Creature.Update.</summary>
        public void Tick()
        {
            if (_creature == null) return;
            if (_creature.Data == null) return;

            // Food tick
            if (_creature.knockedOut || _creature.tamed)
            {
                float drain = _creature.knockedOut
                    ? _creature.Data.foodDrainRateKnockedOut
                    : _creature.Data.foodDrainRateTamed;

                TickFood(drain);

                if (ShouldEat()) TryEatFood();
            }

            // Периодическое сохранение
            if ((_creature.knockedOut || _creature.tamed) &&
                !string.IsNullOrEmpty(_creature.TamingInstanceId))
            {
                _tamingSaveTimer += Time.deltaTime;
                if (_tamingSaveTimer >= TamingSaveInterval)
                {
                    _tamingSaveTimer = 0f;
                    TamingManager.Instance?.UpdateSave(_creature.TamingInstanceId, "player_001");
                }
            }
        }

        // ==========================================
        // === ПРИРУЧЕНИЕ ===
        // ==========================================

        public bool AddTamingProgress(float amount)
        {
            if (_creature.tamed) return true;

            _creature.tamingProgress = Mathf.Min(100f, _creature.tamingProgress + amount);
            if (_creature.tamingProgress >= 100f)
            {
                FinishTaming();
                return true;
            }
            return false;
        }

        public void FinishTaming()
        {
            _creature.tamed = true;
            _creature.tamingProgress = 100f;

            // Дефолтные команды
            _creature.tamedFollowing = true;
            _creature.tamedWandering = false;
            _creature.tamedMating = false;
            _creature.tamedBehavior = TamedBehavior.AttackOwnerTarget;
            _creature.tamedFollowDistance = FollowDistance.Medium;

            _creature.RecoverFromKnockout();
            _creature.SetCreatureLayer();

            if (!string.IsNullOrEmpty(_creature.TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UpdateSave(_creature.TamingInstanceId, "player_001");
            }

            if (_creature.TryGetComponent<Interactables.RadialMenu>(out var menu))
                menu.enabled = true;

            NotificationManager.Instance?.ShowTopNote(
                NotificationType.TameComplete,
                $"{_creature.GetDisplayName()} приручен!"
            );

            Debug.Log($"[{_creature.gameObject.name}] Приручение завершено!");
        }

        public void Unclaim()
        {
            if (!_creature.tamed) return;

            _creature.tamed = false;
            _creature.tamedFollowing = false;
            _creature.tamedWandering = false;
            _creature.tamedMating = false;
            _creature.tamedBehavior = TamedBehavior.Neutral;
            _creature.tamedFollowDistance = FollowDistance.Medium;
            _creature.tamingProgress = 0f;

            if (!string.IsNullOrEmpty(_creature.TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UnregisterCreature(_creature.TamingInstanceId);
                _creature.TamingInstanceId = "";
            }

            var inv = _creature.GetInventory();
            if (inv != null)
            {
                Destroy(inv);
                _creature.SetInventory(null);
            }

            _creature.SetCreatureLayer();

            Debug.Log($"[{_creature.gameObject.name}] Unclaimed — стал диким.");
        }

        // ==========================================
        // === TORPOR ===
        // ==========================================

        public void AddTorpor(float amount)
        {
            _creature.torpor = Mathf.Min(_creature.maxTorpor, _creature.torpor + amount);

            if (!_creature.knockedOut && _creature.torpor >= _creature.maxTorpor)
            {
                _creature.KnockOut();
            }
        }

        // ==========================================
        // === FOOD ===
        // ==========================================

        public void AddFood(float amount)
        {
            _creature.food = Mathf.Min(_creature.maxFood, _creature.food + amount);
            _creature.Heal(amount);
        }

        public bool ConsumeFood(float amount)
        {
            _creature.food = Mathf.Max(0f, _creature.food - amount);
            return _creature.food <= 0f;
        }

        private void TickFood(float drainRate)
        {
            _creature.food = Mathf.Max(0f, _creature.food - drainRate * Time.deltaTime);

            if (_creature.food <= 0f && _creature.Data.starvationDamagePerSecond > 0f)
            {
                _creature.Health = Mathf.Max(0f,
                    _creature.Health - _creature.Data.starvationDamagePerSecond * Time.deltaTime);

                if (_creature.Health <= 0f) _creature.Die();
            }
        }

        private bool ShouldEat()
        {
            if (_creature.Data.maxFood <= 0f) return false;
            return _creature.food <= _creature.Data.maxFood * _creature.Data.foodEatThresholdPercent;
        }

        private bool TryEatFood()
        {
            var inv = _creature.GetInventory();
            if (inv?.Data == null) return false;
            if (_creature.Data.tamingFoodPreferences == null ||
                _creature.Data.tamingFoodPreferences.Length == 0) return false;

            var inventoryData = inv.Data;

            foreach (var pref in _creature.Data.tamingFoodPreferences)
            {
                if (pref.food == null) continue;

                var slot = inventoryData.slots.Find(s => !s.IsEmpty && s.item == pref.food);
                if (slot != null)
                {
                    var eaten = slot.item;
                    inventoryData.RemoveItemFromSlot(inventoryData.slots.IndexOf(slot), 1);

                    float recovery = eaten.foodRecoveryAmount > 0f ? eaten.foodRecoveryAmount : 10f;
                    AddFood(recovery);

                    if (!_creature.tamed)
                    {
                        bool finished = AddTamingProgress(pref.progressAmount);

                        if (finished)
                            Debug.Log($"[{_creature.gameObject.name}] Съел {eaten.itemName}, food: {_creature.food:F1}, progress: 100% (ПРИРУЧЕН)");
                        else
                            Debug.Log($"[{_creature.gameObject.name}] Съел {eaten.itemName}, food: {_creature.food:F1}, progress: {_creature.tamingProgress:F1}%");
                    }

                    if (!string.IsNullOrEmpty(_creature.TamingInstanceId))
                    {
                        TamingManager.Instance?.UpdateSave(_creature.TamingInstanceId, "player_001");
                    }

                    return true;
                }
            }
            return false;
        }
    }
}