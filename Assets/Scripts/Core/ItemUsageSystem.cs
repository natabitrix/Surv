// Assets/Scripts/Core/ItemUsageSystem.cs
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.UI;
using UnityEngine;

namespace Assets.Scripts.Core
{
    public class ItemUsageSystem : MonoBehaviour
    {
        private PlayerSurvivalSystem _survival => PlayerSurvivalSystem.Instance;

        public void UseItem(Item item, int count)
        {
            switch (item.itemType)
            {
                case ItemType.Weapon:
                case ItemType.Tool:
                    EquipWeapon(item);
                    break;
                case ItemType.Food:
                    Consume(item, count);
                    break;
                case ItemType.Armor:
                    EquipArmor(item);
                    break;
                case ItemType.Placeable:
                    PrepareToPlace(item);
                    break;
                case ItemType.Resource:
                    UseResource(item, count);
                    break;
                default:
                    Debug.Log($"Item {item.itemName} has no defined action.");
                    break;
            }
        }

        private void EquipWeapon(Item item)
        {
            Debug.Log($"Equipping weapon: {item.itemName}");
            // Реализуй логику экипировки оружия
        }

        private void Consume(Item item, int count)
        {
            Debug.Log($"Consuming: {item.itemName} x{count}");
            // Реализуй восстановление HP/HP/насыщения
            if (item.foodRecoveryAmount > 0)
            {
                _survival.AddFood(item.foodRecoveryAmount);
            }
            if (item.healthRecoveryAmount > 0)
            {
                _survival.RecoveryHealth(item.healthRecoveryAmount);
            }
        }

        private void EquipArmor(Item item)
        {
            Debug.Log($"Equipping armor: {item.itemName}");
            // Реализуй экипировку брони
        }

        private void PrepareToPlace(Item item)
        {
            Debug.Log($"Ready to place: {item.itemName}");
            // Подготовь к постройке (например, отображение preview)
        }

        private void UseResource(Item item, int count)
        {
            Debug.Log($"Using resource: {item.itemName} x{count}");
            // Использование ресурса (например, крафт)
        }
    }
}