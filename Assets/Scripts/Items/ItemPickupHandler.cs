// Assets/Scripts/Items/ItemPickupHandler.cs
using UnityEngine;
using Assets.Scripts.UI;
using Assets.Scripts.Core;

namespace Assets.Scripts.Items
{
    public class ItemPickupHandler : MonoBehaviour
    {
        // public PlayerInventory playerInventory;

        public bool PickupItem(Item item, int amount = 1)
        {
            if (item == null) return false;

            // bool added = playerInventory.AddItem(item, amount);
            int added = PlayerProgress.Instance.AddItemToPlayerInventory(item, amount);

            if (added > 0)
            {
                // Начисляем опыт за подбор
                if (PlayerProgress.Instance != null && item != null)
                {
                    // Можно задать XP в самом Item
                    int xp = item.experienceOnPickup; // добавь это поле в Item
                    if (xp > 0)
                    {
                        PlayerProgress.Instance.AddExperience(xp * amount);
                    }
                    // else
                    // {
                    //     // Или базовое значение: 1 XP за предмет
                    //     PlayerProgress.Instance.AddExperience(amount);
                    // }
                }

                // Уведомления
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.Show(
                        $"Добавлено: {amount}x {item.itemName}",
                        item.icon
                    );
                }
            }
            else
            {
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.Show("Инвентарь полон", null);
                }
            }
            if (PlayerProgress.Instance != null)
            {
                PlayerProgress.Instance.Save();
                Debug.Log("[ItemPickupHandler] PickupItem Save!");
            }

            return added > 0;
        }
    }
}