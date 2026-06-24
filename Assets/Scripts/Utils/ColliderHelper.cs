
// Assets/Scripts/Utils/ColliderHelper.cs
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public static class ColliderHelper
    {
        /// <summary>
        /// Создаёт или обновляет BoxCollider, размер которого соответствует крайним точкам всех рендереров объекта и его детей.
        /// </summary>
        public static BoxCollider FitBoxColliderToRenderers(GameObject target)
        {
            if (target == null) return null;

            // Собираем все рендереры в иерархии (включая неактивные)
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[ColliderHelper] Не найдено рендереров у объекта {target.name}. Коллайдер не создан.");
                return null;
            }

            // 1. Объединяем мировые границы всех рендереров
            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            // 2. Переводим центр в локальное пространство
            Vector3 localCenter = target.transform.InverseTransformPoint(worldBounds.center);

            // 3. Корректируем размер под локальный масштаб (учитывает lossyScale родителя)
            Vector3 worldSize = worldBounds.size;
            Vector3 localSize = new Vector3(
                worldSize.x / target.transform.lossyScale.x,
                worldSize.y / target.transform.lossyScale.y,
                worldSize.z / target.transform.lossyScale.z
            );

            // 4. Добавляем или получаем существующий BoxCollider
            BoxCollider box = target.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = target.AddComponent<BoxCollider>();
            }

            // 5. Применяем параметры
            box.center = localCenter;
            box.size = localSize;

            return box;
        }
    }
}