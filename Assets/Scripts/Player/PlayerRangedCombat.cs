// Assets/Scripts/Player/PlayerRangedCombat.cs
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

        [Tooltip("Дистанция, на которую мысленно 'проецируется' точка прицеливания вдоль взгляда камеры. " +
                 "Нужна только для вычисления направления — сама стрела летит с реальной скоростью и гравитацией.")]
        [SerializeField] private float _aimDistance = 100f;

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

            // 3. Точка спавна
            Vector3 spawnPos = ResolveSpawnPosition();

            // 4. Направление: из точки спавна к точке прицеливания (камера + forward * distance)
            Vector3 direction = ResolveShotDirection(spawnPos);

            // 5. Из пула
            if (ArrowPool.Instance == null)
            {
                Debug.LogError("[PlayerRangedCombat] ArrowPool.Instance == null!");
                return;
            }

            Arrow arrow = ArrowPool.Instance.GetArrow(spawnPos, Quaternion.identity);

            // 6. Урон = лук + стрела
            float totalDamage = bow.damage + bow.projectileItem.damage;

            // 6.1. Torpor = torpor лука + torpor стрелы (транквилизаторная стрела)
            float totalTorpor = bow.torporDamage + bow.projectileItem.torporDamage;

            arrow.SetOwnerColliders(GetComponentsInChildren<Collider>());

            // 7. Запуск
            uint ownerId = 0; // Для мультиплеера будет из NetworkIdentity
            arrow.Launch(
                arrowItem: bow.projectileItem,
                damage: totalDamage,
                torpor: totalTorpor,
                direction: direction,
                speed: bow.projectileSpeed,
                gravityScale: bow.projectileGravityScale,
                ownerId: ownerId
            );
        }

        private Vector3 ResolveSpawnPosition()
        {
            if (_arrowSpawnPoint != null)
                return _arrowSpawnPoint.position;

            if (_playerCamera != null)
                return _playerCamera.transform.position + _playerCamera.transform.forward * 0.5f;

            return transform.position + transform.forward;
        }

        /// <summary>
        /// Считает направление так, чтобы стрела летела из spawnPos в точку под прицелом.
        /// Это устраняет эффект «параллакса», когда точка спавна смещена от центра экрана.
        /// </summary>
        private Vector3 ResolveShotDirection(Vector3 spawnPos)
        {
            if (_playerCamera != null)
            {
                // Точка, куда смотрит камера (центр экрана)
                Vector3 aimPoint = _playerCamera.transform.position
                                   + _playerCamera.transform.forward * _aimDistance;

                // Направление от точки спавна к цели — компенсирует смещение ArrowSpawnPoint
                Vector3 dir = (aimPoint - spawnPos).normalized;

                if (dir.sqrMagnitude > 0.0001f)
                    return dir;
            }

            // Fallback — вперёд от точки спавна
            if (_arrowSpawnPoint != null)
                return _arrowSpawnPoint.forward;

            return transform.forward;
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