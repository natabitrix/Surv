using Assets.Scripts.InventorySystem;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class DoorController : MonoBehaviour, IInteractable // ← теперь реализует IInteractable напрямую
    {
        public float openAngle = 90f;
        public float smoothness = 5f;
        public bool HasInventory() => false;
        public ChestInventory GetInventory() => null;
        public bool ShouldDetachAfterInteract() => false;

        public InteractType GetInteractType() => InteractType.Open;

        private Collider _targetCollider;
        private Transform _doorHinge;
        private float _closedAngle;
        private float _currentAngle;
        private float _targetAngle;
        private float _angleVelocity;

        // Состояние двери
        private bool _isOpen = false;

        private void Start()
        {
            _doorHinge = transform.parent;
            _targetCollider = GetComponent<Collider>();
            _closedAngle = _doorHinge.localEulerAngles.y;
            _currentAngle = _closedAngle;
            _targetAngle = _closedAngle;
        }

        private void Update()
        {
            _currentAngle = Mathf.SmoothDampAngle(_currentAngle, _targetAngle, ref _angleVelocity, 1f / smoothness);
            _doorHinge.localEulerAngles = new Vector3(0f, _currentAngle, 0f);

            // УНИВЕРСАЛЬНАЯ проверка для триггера (работает с любым знаком openAngle)
            float closedAngle = _closedAngle;
            float openAngleAbs = _closedAngle + openAngle;
            
            float minAngle = Mathf.Min(closedAngle, openAngleAbs);
            float maxAngle = Mathf.Max(closedAngle, openAngleAbs);
            
            // Активируем триггер, когда дверь находится МЕЖДУ закрытым и открытым состоянием
            bool isMoving = _currentAngle > minAngle + 1f && _currentAngle < maxAngle - 1f;
            _targetCollider.isTrigger = isMoving;

            // Обновляем кэшированное состояние для внешних систем
            _isOpen = IsVisuallyOpen();
        }

        public bool IsVisuallyOpen(float thresholdDegrees = 2f)
        {
            float targetOpenAngle = _closedAngle + openAngle;
            
            // Используем DeltaAngle для корректной работы с углами в диапазоне [0, 360)
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(_currentAngle, targetOpenAngle));
            
            return angleDiff < thresholdDegrees;
        }


        // Исправляем метод Interact — убираем мгновенное переключение состояния
        public void Interact(InteractContext context)
        {
            // Переключаем ЦЕЛЕВОЙ угол, но не состояние
             if (IsVisuallyOpen()) // Движемся к открытому?
            {
                _targetAngle = _closedAngle; // закрыть
            }
            else
            {
                _targetAngle = _closedAngle + openAngle; // открыть
            }
        }


    }
}