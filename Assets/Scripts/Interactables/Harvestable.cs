// Assets/Scripts/Interactables/Harvestable.cs
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class Harvestable : MonoBehaviour, IInteractable
    {
        [System.Serializable]
        public struct ResourceDrop
        {
            public Item item;
            public int baseTotalAmount; // Общее количество этого ресурса при полной добыче
        }

        [Header("Resource Drops")]
        public ResourceDrop[] drops;

        [Header("Harvest Settings")]
        public int maxHits = 3;
        public bool allowFistHarvest = false; // Можно ли кулаками? (обычно false для дерева/камня)

        [Header("Visual Feedback (Optional)")]
        public Animator animator;
        public ParticleSystem breakEffect;

        private int _currentHits = 0;
        private bool _isDestroyed = false;

        // === IInteractable implementation ===

        public InteractType GetInteractType() => InteractType.Harvest;

        public bool HasInventory() => false;
        public ChestInventory GetInventory() => null;

        public bool ShouldDetachAfterInteract() => _isDestroyed;

        public void Interact(InteractContext context)
        {
            // Только если атака (ЛКМ), иначе игнорируем
            if (!context.IsAttack)
            {
                Debug.Log("Harvestable requires attack (ЛКМ), not interact (E).");
                return;
            }

            // Проверка: можно ли кулаками?
            if (context.Tool == AttackAnimationType.Fists && !allowFistHarvest)
            {
                Debug.Log("Нужен инструмент для добычи!");
                return;
            }

            if (_isDestroyed || _currentHits >= maxHits) return;

            _currentHits++;

            // Выдаём все ресурсы
            foreach (var drop in drops)
            {
                if (drop.item == null || drop.baseTotalAmount <= 0) continue;

                int amountPerHit = Mathf.CeilToInt((float)drop.baseTotalAmount / maxHits);
                int amount = CalculateAmountWithTool(drop.item, amountPerHit, context.Tool);

                if (amount > 0)
                {
                    GiveResource(drop.item, amount);
                }
            }

            PlayHitFeedback();

            if (_currentHits >= maxHits)
            {
                _isDestroyed = true;
                PlayBreakEffect();
                Destroy(gameObject, 1.0f); // задержка для визуала
            }
        }

        // === Логика инструментов ===

        private int CalculateAmountWithTool(Item item, int baseAmount, AttackAnimationType tool)
        {
            float multiplier = 1f;

            // Пример: дерево
            if (item.itemName.Contains("Wood") || item.itemName.Contains("Log") || item.itemName.Contains("Дерево"))
            {
                switch (tool)
                {
                    case AttackAnimationType.Axe: multiplier = 1.5f; break;
                    case AttackAnimationType.Pickaxe: multiplier = 0.3f; break;
                    case AttackAnimationType.Fists: multiplier = 0.0f; break; // нельзя
                }
            }
            // Пример: солома, трава, листья
            else if (item.itemName.Contains("Straw") || item.itemName.Contains("Hay") || item.itemName.Contains("Солома"))
            {
                switch (tool)
                {
                    case AttackAnimationType.Pickaxe: multiplier = 1.4f; break;
                    case AttackAnimationType.Axe: multiplier = 0.7f; break;
                    case AttackAnimationType.Fists: multiplier = 1.0f; break; // можно руками
                }
            }
            // Пример: камень (только киркой)
            else if (item.itemName.Contains("Stone") || item.itemName.Contains("Камень"))
            {
                switch (tool)
                {
                    case AttackAnimationType.Pickaxe: multiplier = 1.6f; break;
                    default: multiplier = 0.0f; break; // только кирка
                }
            }

            return Mathf.Max(0, Mathf.RoundToInt(baseAmount * multiplier));
        }

        // === Вспомогательные методы ===

        private void GiveResource(Item item, int amount)
        {
            if (amount <= 0) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var pickupHandler = player.GetComponent<ItemHandler>();
            if (pickupHandler != null)
            {
                pickupHandler.PickupItem(item, amount);
            }
        }

        private void PlayHitFeedback()
        {
            if (animator != null)
                animator.SetTrigger("Hit");
        }

        private void PlayBreakEffect()
        {
            if (breakEffect != null)
                breakEffect.Play();
            if (animator != null)
                animator.SetTrigger("Break");
        }
    }
}