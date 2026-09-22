using Assets.Scripts.Combat;
using Assets.Scripts.Core;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.UI;
using UnityEngine;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Отвечает за стрельбу из дальнего оружия (лук, арбалет).
    /// Вызывается из Animation Event "OnArrowReleased" на клипе выстрела.
    /// </summary>
    public class PlayerRangedCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerEquipment _equipment;

        [Tooltip("Точка спавна стрелы (на модели лука). Заполняется из PlayerEquipment при экипировке.")]
        [SerializeField] private Transform _arrowSpawnPoint;

        [Tooltip("Fallback: если точки спавна нет — используем эту камеру.")]
        [SerializeField] private Camera _playerCamera;

        public void SetArrowSpawnPoint(Transform point)
        {
            _arrowSpawnPoint = point;
        }

        /// <summary>
        /// Вызывается из Animation Event "OnArrowReleased".
        /// </summary>
        public void SpawnArrow()
        {
            // 1. Проверяем экипировку
            if (_equipment == null || !_equipment.IsEquipped) return;

            Item bow = _equipment.GetCurrentItem();
            if (bow == null || !bow.isRanged) return;

            if (bow.projectileItem == null)
            {
                Debug.LogWarning($"[PlayerRangedCombat] У лука '{bow.itemName}' не назначен projectileItem!");
                return;
            }

            // 2. Забираем стрелу из инвентаря
            if (!TryConsumeArrow(bow.projectileItem))
            {
                NotificationManager.Instance?.Show("Нет стрел!", null);
                return;
            }

            // 3. Точка спавна и направление
            Vector3 spawnPos;
            Vector3 direction;

            if (_arrowSpawnPoint != null)
            {
                spawnPos = _arrowSpawnPoint.position;
                direction = _arrowSpawnPoint.forward;
            }
            else if (_playerCamera != null)
            {
                spawnPos = _playerCamera.transform.position + _playerCamera.transform.forward * 0.5f;
                direction = _playerCamera.transform.forward;
            }
            else
            {
                spawnPos = transform.position + transform.forward;
                direction = transform.forward;
            }

            // 4. Из пула
            if (ArrowPool.Instance == null)
            {
                Debug.LogError("[PlayerRangedCombat] ArrowPool.Instance == null!");
                return;
            }

            Arrow arrow = ArrowPool.Instance.GetArrow(spawnPos, Quaternion.identity);

            // 5. Урон = лук + стрела
            float totalDamage = bow.damage + bow.projectileItem.damage;

            arrow.SetOwnerColliders(GetComponentsInChildren<Collider>());

            // 6. Запуск
            uint ownerId = 0; // Для мультиплеера будет из NetworkIdentity
            arrow.Launch(
                arrowItem: bow.projectileItem,
                damage: totalDamage,
                direction: direction,
                speed: bow.projectileSpeed,
                gravityScale: bow.projectileGravityScale,
                ownerId: ownerId
            );
        }

        private bool TryConsumeArrow(Item arrowItem)
        {
            var progress = PlayerProgress.Instance;
            if (progress == null) return false;

            if (TryConsumeFrom(progress.hotbarInventoryData, arrowItem)) return true;
            if (TryConsumeFrom(progress.mainInventoryData, arrowItem)) return true;

            return false;
        }

        private bool TryConsumeFrom(InventoryData data, Item arrowItem)
        {
            if (data?.slots == null) return false;

            for (int i = 0; i < data.slots.Count; i++)
            {
                var slot = data.slots[i];
                if (!slot.IsEmpty && slot.item == arrowItem && slot.count > 0)
                {
                    data.RemoveItemFromSlot(i, 1);
                    data.NotifyChanged();
                    return true;
                }
            }
            return false;
        }
    }
}