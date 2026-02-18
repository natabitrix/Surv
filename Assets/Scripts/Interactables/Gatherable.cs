// Assets/Scripts/Interactables/Gatherable.cs
using Assets.Scripts.InventorySystem;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class Gatherable : MonoBehaviour, IInteractable
    {
        [Header("Item to Gather")]
        public Item item;

        [Header("Gather Settings")]
        public int maxGatherAmount = 3; // Сколько всего можно собрать
        public int amountPerGather = 1; // Сколько даётся за одно нажатие

        [Header("Optional: Visual")]
        public GameObject emptyVersion; // Необязательно: модель после опустошения

        private int _currentGathered = 0;
        private bool _isDepleted = false;

        // === IInteractable implementation ===

        public InteractType GetInteractType() => InteractType.Gather;

        public bool HasInventory() => false;
        public ChestInventory GetInventory() => null;

        public bool ShouldDetachAfterInteract() => _isDepleted;

        public void Interact(InteractContext context)
        {
            // Игнорируем атаку (ЛКМ) — только E
            if (context.IsAttack)
                return;

            if (_isDepleted || _currentGathered >= maxGatherAmount)
            {
                _isDepleted = true;
                return;
            }

            int toGive = Mathf.Min(amountPerGather, maxGatherAmount - _currentGathered);
            _currentGathered += toGive;

            GiveResource(toGive);

            // Обновляем визуал (опционально)
            if (_currentGathered >= maxGatherAmount)
            {
                _isDepleted = true;
                if (emptyVersion != null)
                {
                    // Можно показать "обломки" или пустой куст
                    Instantiate(emptyVersion, transform.position, transform.rotation);
                }
                Destroy(gameObject);
            }
        }

        private void GiveResource(int amount)
        {
            if (item == null || amount <= 0) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var pickupHandler = player.GetComponent<ItemPickupHandler>();
            if (pickupHandler != null)
            {
                pickupHandler.PickupItem(item, amount);
            }
        }
    }
}