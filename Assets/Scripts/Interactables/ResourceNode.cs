// Assets/Scripts/Interactables/ResourceNode.cs
using Assets.Scripts.Effects;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class ResourceNode : MonoBehaviour, IInteractable
    {
        // === Типы взаимодействия ===
        [Header("Gather - Сбор руками по E; Harvest - Добыча инструментом по ЛКМ")]
        public bool allowGather = true;   // Сбор руками по E
        public bool allowHarvest = true;  // Добыча инструментом по ЛКМ

        [Header("Allowed Tools for Harvest (only matters if allowHarvest = true)")]
        public bool allowFists = true;
        public bool allowAxe = true;
        public bool allowPickaxe = true;
        public bool allowSickle = false;
        // private float destroyTime = 0.5f;
        public bool pitchResourceOnLastHit = false;

        // === Ресурсы ===
        [System.Serializable]
        public struct ResourceDrop
        {
            public Item item;
            public int totalAmount; // Общее количество при полной добыче/сборе
        }

        [Header("Resource Drops")]
        public ResourceDrop[] drops;

        [Header("Limits")]
        public int maxGatherActions = 3; // По умолчанию 3, не 0
        public int maxHarvestHits = 3;   // По умолчанию 3, не 0

        [Header("Visual Feedback (Optional)")]
        public Animator animator;
        public ParticleSystem breakEffect;
        public bool destroyAfterDepleted = true;

        public Shatterer shatterer; // новое поле

        // === Внутреннее состояние ===
        private int _gatherCount = 0;
        private int _harvestHits = 0;
        private int[] _remainingAmounts;
        private bool _isDepleted = false;

        public ChestInventory GetInventory() => null;

        void Awake()
        {
            // Если shatterer не назначен в инспекторе, ищем его на том же GameObject
            if (shatterer == null)
            {
                shatterer = GetComponent<Shatterer>();
            }

            if (drops.Length > 0)
            {
                _remainingAmounts = new int[drops.Length];
                for (int i = 0; i < drops.Length; i++)
                {
                    _remainingAmounts[i] = drops[i].totalAmount;
                }
            }

            // Защита от 0 действий
            if (maxGatherActions <= 0) maxGatherActions = 1;
            if (maxHarvestHits <= 0) maxHarvestHits = 1;


            if (shatterer != null)
            {
                shatterer.SetPitchOnLastHit(pitchResourceOnLastHit);
            }

        }

        public InteractType GetInteractType()
        {
            return allowGather ? InteractType.Gather : InteractType.Harvest;
        }

        public bool HasInventory() => false;
        public bool ShouldDetachAfterInteract() => _isDepleted;

        public void Interact(InteractContext context)
        {
            if (_isDepleted) return;

            if (context.IsAttack)
            {
                if (!allowHarvest) return;

                AttackAnimationType tool = context.Tool;
                bool toolAllowed = tool switch
                {
                    AttackAnimationType.Fists => allowFists,
                    AttackAnimationType.Axe => allowAxe,
                    AttackAnimationType.Pickaxe => allowPickaxe,
                    AttackAnimationType.Sickle => allowSickle,
                    _ => false
                };

                if (!toolAllowed)
                {
                    Debug.Log($"Нельзя добывать этим инструментом: {tool}");
                    return;
                }

                if (_harvestHits >= maxHarvestHits) return;
                _harvestHits++;

                DistributeResources(isHarvest: true, tool: tool);
                PlayBreakEffect();
                PlayHitFeedback();

                // ❌ Deplete() вызывается внутри DistributeResources
            }
            else
            {
                if (!allowGather) return;
                if (_gatherCount >= maxGatherActions) return;

                _gatherCount++;
                DistributeResources(isHarvest: false, tool: AttackAnimationType.Fists);
                // ❌ Deplete() вызывается внутри DistributeResources
            }
        }

        void DistributeResources(bool isHarvest, AttackAnimationType tool)
        {
            int currentAction = isHarvest ? _harvestHits : _gatherCount;
            int maxActions = isHarvest ? maxHarvestHits : maxGatherActions;

            // bool gaveSomething = false;

            for (int i = 0; i < drops.Length; i++)
            {
                if (drops[i].item == null || _remainingAmounts[i] <= 0) continue;

                int remaining = _remainingAmounts[i];
                int actionsLeftIncludingThis = maxActions - currentAction + 1; // включая текущий

                // Среднее количество на оставшиеся действия
                int avg = Mathf.Max(1, Mathf.CeilToInt((float)remaining / actionsLeftIncludingThis));
                // Максимум — не больше чем на 50% выше среднего
                int maxPossibleNow = Mathf.Min(remaining, (int)(avg * 1.5f));
                int minPossibleNow = Mathf.Min(1, remaining);

                if (maxPossibleNow < minPossibleNow) maxPossibleNow = minPossibleNow;

                int give = Random.Range(minPossibleNow, maxPossibleNow + 1);
                // give = 1; //for tests

                if (give > remaining) give = remaining;

                if (isHarvest)
                {
                    give = ApplyToolBonus(drops[i].item, give, tool);
                }

                if (give > 0)
                {
                    _remainingAmounts[i] -= give;
                    GiveResource(drops[i].item, give);
                    // gaveSomething = true;
                }
            }

            // Проверка: всё ли собрано ИЛИ достигнут лимит действий
            bool allDepleted = true;
            foreach (int amount in _remainingAmounts)
            {
                if (amount > 0)
                {
                    allDepleted = false;
                    break;
                }
            }

            int currentActions = isHarvest ? _harvestHits : _gatherCount;
            if (allDepleted || currentActions >= maxActions)
            {
                Deplete();
            }
        }

        int ApplyToolBonus(Item item, int baseAmount, AttackAnimationType tool)
        {
            float mult = 1f;
            string name = item.itemName.ToLower();

            if (name.Contains("wood") || name.Contains("дерево"))
            {
                mult = tool == AttackAnimationType.Axe ? 1.5f : 0.6f;
            }
            else if (name.Contains("stone") || name.Contains("камень"))
            {
                mult = tool == AttackAnimationType.Pickaxe ? 1.6f : 0.2f;
            }
            else if (name.Contains("stick") || name.Contains("thatch") || name.Contains("ветка") || name.Contains("солома"))
            {
                if (tool == AttackAnimationType.Pickaxe)
                    mult = 1.4f;
                else if (tool == AttackAnimationType.Fists)
                    mult = 1.0f;
                else
                    mult = 0.7f;
            }

            return Mathf.Max(0, Mathf.RoundToInt(baseAmount * mult));
        }

        void GiveResource(Item item, int amount)
        {
            if (amount <= 0 || item == null) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            player?.GetComponent<ItemPickupHandler>()?.PickupItem(item, amount);
        }

        void PlayHitFeedback()
        {
            if (animator != null)
                animator.SetTrigger("Hit");
        }

        // В момент ударов
        void PlayBreakEffect()
        {
            if (animator != null)
                animator.SetTrigger("Break");

            if (breakEffect != null)
            {
                // Отделяем от родителя, чтобы не удалился вместе с ним
                var effectInstance = Instantiate(breakEffect, transform.position, transform.rotation);
                effectInstance.Play();

                // Уничтожим частицы после завершения
                var main = effectInstance.main;
                Destroy(effectInstance.gameObject, main.duration + 1.5f);
            }

            // вызов физических осколков
            shatterer?.Shatter();
        }

        // На последнем ударе
        void PlayLastBreakEffect()
        {
            if (animator != null)
                animator.SetTrigger("BreakLast");

            if (breakEffect != null)
            {
                // Отделяем от родителя, чтобы не удалился вместе с ним
                var effectInstance = Instantiate(breakEffect, transform.position, transform.rotation);
                effectInstance.Play();

                // Уничтожим частицы после завершения
                var main = effectInstance.main;
                Destroy(effectInstance.gameObject, main.duration + 1.5f);
            }

            // вызов физических осколков
            shatterer?.LastBreak();
            shatterer?.Shatter();
            // Invoke(nameof(shatterer.Shatter), destroyTime);
        }


        void Deplete()
        {
            if (_isDepleted) return;
            _isDepleted = true;
            PlayLastBreakEffect();
            if (destroyAfterDepleted)
            {
                // Destroy(gameObject, destroyTime);
                Destroy(gameObject);
            }
        }
    }
}