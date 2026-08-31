// Assets/Scripts/Items/ItemPickupHandler.cs
using UnityEngine;
using Assets.Scripts.UI;
using Assets.Scripts.Core;

namespace Assets.Scripts.Items
{
    public class ItemHandler : MonoBehaviour
    {
        public bool PickupItem(Item item, int amount = 1, float durability = -2f)
        {
            if (item == null) return false;

            int added = PlayerProgress.Instance.AddItemToPlayerInventory(item, amount, durability);

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
                PlayerProgress.Instance.Save("ItemHandler.PickupItem");
            }

            return added > 0;
        }


        public bool DestroyItem(GameObject obj)
        {
            if (obj == null) return false;

            if (WorldManager.Instance != null)
            {
                WorldManager.Instance.UnregisterStructure(obj);
            }
            
            Destroy(obj);

            if (PlayerProgress.Instance != null)
            {
                PlayerProgress.Instance.Save("ItemHandler.DestroyItem");
            }

            // return destroyed > 0;
            return true;
        }


    }
}