// Assets/Scripts/Creatures/CreatureHitZone.cs
using UnityEngine;

namespace Assets.Scripts.Creatures
{
    /// <summary>
    /// Маркер зоны урона на кости существа (голова, панцирь и т.п.).
    /// Ловится при попадании через OverlapBox / OverlapSphere / Raycast из PlayerInteraction.
    /// </summary>
    public class CreatureHitZone : MonoBehaviour
    {
        [Tooltip("Множитель урона. >1 — крит, <1 — броня, 1 — дефолт.")]
        public float damageMultiplier = 1f;

        [Tooltip("Имя для отладки/UI ('Голова', 'Панцирь').")]
        public string label = "Zone";

        [Tooltip("Ссылка на существо. Заполняется автоматически из родителя на Awake.")]
        public BaseLivingEntity owner;

        private void Awake()
        {
            if (owner == null)
                owner = GetComponentInParent<BaseLivingEntity>();
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;

            // Цвет: красный — крит (>1), синий — броня (<1), серый — дефолт (1)
            if (damageMultiplier > 1f)
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            else if (damageMultiplier < 1f)
                Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.4f);
            else
                Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.4f);

            // Рисуем bounds коллайдера
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is SphereCollider sc)
            {
                Gizmos.DrawWireSphere(sc.center, sc.radius);
                Gizmos.DrawSphere(sc.center, sc.radius);
            }
            else if (col is BoxCollider bc)
            {
                Gizmos.DrawWireCube(bc.center, bc.size);
                Gizmos.DrawCube(bc.center, bc.size);
            }
            else if (col is CapsuleCollider cc)
            {
                Gizmos.DrawWireCube(cc.center, new Vector3(cc.radius * 2, cc.height, cc.radius * 2));
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}