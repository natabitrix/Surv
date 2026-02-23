using Assets.Scripts.InventorySystem;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class DoorController : MonoBehaviour, IInteractable
    {
        [Header("Настройки двери")]
        public float openAngle = 90f;
        public float smoothness = 5f;

        public bool HasInventory() => false;
        public ChestInventory GetInventory() => null;
        public bool ShouldDetachAfterInteract() => false;
        public InteractType GetInteractType() => InteractType.Open;

        private Collider _targetCollider;
        private float _currentAngle;
        private float _targetAngle;
        private float _angleVelocity;
        private bool _isOpen = false;

        // Запоминаем начальную ротацию, чтобы дверь не сбивала ориентацию в мире
        private Quaternion _startRotation;

        private void Start()
        {
            _targetCollider = GetComponent<Collider>();
            _startRotation = transform.rotation;
            
            _currentAngle = 0f;
            _targetAngle = 0f;
        }

        private void Update()
        {
            // 1. Плавно меняем угол (от 0 до openAngle)
            _currentAngle = Mathf.SmoothDampAngle(_currentAngle, _targetAngle, ref _angleVelocity, 1f / smoothness);

            // 2. Вращаем дверь вокруг её локального пивота (который теперь в петле)
            // Мы умножаем кватернионы, чтобы сохранить глобальную ориентацию проема
            transform.rotation = _startRotation * Quaternion.Euler(0f, _currentAngle, 0f);

            // 3. Логика триггера (чтобы игрок не застревал в полотне двери)
            // Дверь становится триггером, только если она движется
            if (_targetCollider != null)
            {
                bool isMoving = Mathf.Abs(_currentAngle - _targetAngle) > 1f;
                _targetCollider.isTrigger = isMoving;
            }

            _isOpen = IsVisuallyOpen();
        }

        public bool IsVisuallyOpen(float thresholdDegrees = 2f)
        {
            float angleDiff = Mathf.Abs(_currentAngle - openAngle);
            return angleDiff < thresholdDegrees;
        }

        public void Interact(InteractContext context)
        {
            if (IsVisuallyOpen())
            {
                _targetAngle = 0f; // Закрыть
            }
            else
            {
                _targetAngle = openAngle; // Открыть
            }
        }
    }
}