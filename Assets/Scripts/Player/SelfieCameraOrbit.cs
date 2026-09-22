using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Player
{
    public class SelfieCameraOrbit : MonoBehaviour
    {
        public Transform playerCameraRoot;
        public float sensitivity = 2f;

        [Header("Zoom")]
        public float minDistance = 0.5f;
        public float maxDistance = 20f;
        [SerializeField] private float _defaultDistance = 5f;
        [SerializeField] private float _zoomStep = 0.5f;

        private float _distance;
        private float _yaw = 0f;
        private float _pitch = 10f;

        private PlayerInputHandler _inputHandler;
        private PlayerController _playerController;

        private void Awake()
        {
            _inputHandler = FindAnyObjectByType<PlayerInputHandler>();
            _playerController = FindAnyObjectByType<PlayerController>();
            _distance = _defaultDistance;
        }

        private void LateUpdate()
        {
            if (playerCameraRoot == null) return;
            if (CameraManager.Instance == null || !CameraManager.Instance.IsSelfie) return;

            if (_playerController != null && _playerController.LockCameraOnEsc)
                return;

            // Вращение мышью
            Vector2 mouseDelta = Mouse.current?.delta.ReadValue() ?? Vector2.zero;
            if (mouseDelta != Vector2.zero)
            {
                _yaw += mouseDelta.x * sensitivity * 0.1f;
                _pitch -= mouseDelta.y * sensitivity * 0.1f;
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);
            }

            // Зум колесом
            if (_inputHandler != null && Mathf.Abs(_inputHandler.mouseScrollDelta) > 0.01f)
            {
                _distance -= _inputHandler.mouseScrollDelta * _zoomStep;
                _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
                _inputHandler.mouseScrollDelta = 0f; // обнуляем — CameraManager не трогал
            }

            // Позиционирование
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = playerCameraRoot.position - rotation * Vector3.forward * _distance;
            transform.LookAt(playerCameraRoot);
        }
    }
}