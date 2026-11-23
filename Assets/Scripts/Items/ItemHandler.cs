// Assets/Scripts/Items/ItemPickupHandler.cs
using UnityEngine;
using Assets.Scripts.UI;
using Assets.Scripts.Core;
using Assets.Scripts.UI.Notifications;
using Assets.Scripts.Player;

namespace Assets.Scripts.Items
{
    public class ItemHandler : MonoBehaviour
    {
        private PlayerProgress _playerProgress;

        private void Awake()
        {
            // Получаем компонент с этого же объекта (игрока)
            _playerProgress = GetComponent<PlayerProgress>();
            
            // Если по какой-то причине его нет, пробуем найти у LocalPlayer (как запасной вариант)
            if (_playerProgress == null && PlayerController.LocalPlayer != null)
            {
                _playerProgress = PlayerController.LocalPlayer.GetComponent<PlayerProgress>();
            }

            if (_playerProgress == null)
            {
                Debug.LogError("[ItemHandler] PlayerProgress not found!", this);
            }
        }
        
        public bool PickupItem(Item item, int amount = 1, float durability = -2f)
        {
            if (item == null) return false;

            int added = _playerProgress.AddItemToPlayerInventory(item, amount, durability);

            if (added > 0)
            {
                // Начисляем опыт за подбор
                if (_playerProgress != null && item != null)
                {
                    // Можно задать XP в самом Item
                    int xp = item.experienceOnPickup; // добавь это поле в Item
                    if (xp > 0)
                    {
                        _playerProgress.AddExperience(xp * amount);
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
            if (_playerProgress != null)
            {
                _playerProgress.Save("ItemHandler.PickupItem");
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

            if (_playerProgress != null)
            {
                _playerProgress.Save("ItemHandler.DestroyItem");
            }

            // return destroyed > 0;
            return true;
        }


    }
}