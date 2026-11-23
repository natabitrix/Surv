using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Player
{
    public class SelfieCameraOrbit : MonoBehaviour
    {
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
        private CameraManager _cameraManager;
        private Transform _playerCameraRoot;

        private void Awake()
        {
            _inputHandler = FindAnyObjectByType<PlayerInputHandler>();

            _playerController = PlayerController.LocalPlayer;

            // if (_playerController == null)
            //     Debug.LogError($"[SelfieCameraOrbit] Нет PlayerController.LocalPlayer!");

            // _cameraManager = _playerController.GetComponent<CameraManager>();

            // if (_cameraManager == null)
            //     Debug.LogError($"[SelfieCameraOrbit] Нет CameraManager!");

            // _playerCameraRoot = _cameraManager.PlayerCameraRoot;

            // _distance = _defaultDistance;
        }

        private void Start()
        {
            // В Start все Awake уже отработали, LocalPlayer должен быть назначен
            _playerController = PlayerController.LocalPlayer;

            if (_playerController == null)
            {
                Debug.LogError($"[SelfieCameraOrbit] Нет PlayerController.LocalPlayer! Отключаю скрипт.", this);
                enabled = false;
                return;
            }

            _cameraManager = _playerController.GetComponent<CameraManager>();

            if (_cameraManager == null)
            {
                Debug.LogError($"[SelfieCameraOrbit] Нет CameraManager на игроке {_playerController.name}!", this);
                enabled = false;
                return;
            }

            _playerCameraRoot = _cameraManager.PlayerCameraRoot;

            _distance = _defaultDistance;
        }

        private void LateUpdate()
        {
            if (_playerCameraRoot == null || _cameraManager == null) return;
            
            if (!_cameraManager.IsSelfie) return;

            if (_playerController != null && _playerController.LockCamera)
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
            transform.position = _playerCameraRoot.position - rotation * Vector3.forward * _distance;
            transform.LookAt(_playerCameraRoot);
        }
    }
}