// Assets/Scripts/Building/SnapPoint.cs
using UnityEngine;

namespace Assets.Scripts.Building
{
    /// <summary>
    /// Точка крепления для размещения структур. Размещается как дочерний объект на префабах.
    /// НЕ делает размещаемую структуру своим дочерним объектом — только предоставляет позицию/поворот.
    /// </summary>
    [System.Serializable]
    public class SnapPoint : MonoBehaviour
    {
        public enum Direction { North, East, South, West, Top }
        public enum AttachmentType { Wall, Ceiling, Door, DoorFrame, Gate, GateFrame, Foundation, Any }

        public Direction direction;
        public AttachmentType attachmentType;
        public float snapRadius = 0.3f; // Радиус активации точки

        // Визуальная отладка в редакторе
        private void OnDrawGizmosSelected()
        {
            Color gizmoColor = Color.yellow;
            if(attachmentType == AttachmentType.Wall) gizmoColor = Color.cyan;
            if(attachmentType == AttachmentType.Ceiling) gizmoColor = Color.azure;
            if(attachmentType == AttachmentType.Foundation) gizmoColor = Color.crimson;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, snapRadius);
        }
    }

}