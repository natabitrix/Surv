using UnityEngine;
// using Cinemachine;
using Unity.Cinemachine;

namespace Assets.Scripts.Player
{
    public class CameraManager : MonoBehaviour
    {
        public enum CameraMode
        {
            ThirdPerson,
            FirstPerson,
            Selfie
        }

        [Header("Cameras")]
        [SerializeField] private CinemachineCamera _thirdPersonVcam; // Priority: 10 or 11
        [SerializeField] private CinemachineCamera _firstPersonVcam; // Priority: 10 or 11
        [SerializeField] private CinemachineCamera _selfieVcam;      // Priority: всегда 12

        [Header("Selfie Zoom")]
        public float minDistance = 1f;
        public float maxDistance = 10f;
        // [SerializeField] private float _defaultSelfieDistance = 3f;

        private CameraMode _currentMode = CameraMode.ThirdPerson;
        private float _currentSelfieDistance;
        private PlayerInputHandler _input;

        private const int MAIN_PRIORITY = 10;
        private const int ACTIVE_PRIORITY = 11;
        private const int SELFIE_PRIORITY = 12;

        void Start()
        {
            _input = GetComponent<PlayerInputHandler>();
            if (_input == null)
            {
                Debug.LogError("CameraManager: PlayerInputEvents not found!");
                enabled = false;
                return;
            }

            SwitchToMode(CameraMode.ThirdPerson);
        }

        void Update()
        {
            if (_input.selfieCamera)
            {
                if (_currentMode != CameraMode.Selfie)
                {
                    SwitchToMode(CameraMode.Selfie);
                }
                else
                {
                    HandleSelfieZoom();
                }
            }
            else
            {
                // Возвращаемся к последнему основному режиму (TPS/FPS)
                if (_currentMode == CameraMode.Selfie)
                {
                    // Восстанавливаем предыдущий режим: если был Third → Third, иначе First
                    CameraMode fallback = (_currentModeBeforeSelfie == CameraMode.FirstPerson)
                        ? CameraMode.FirstPerson
                        : CameraMode.ThirdPerson;
                    SwitchToMode(fallback);
                }
                else
                {
                    // Обрабатываем переключение между TPS и FPS колёсиком
                    HandleMainCameraSwitch();
                }
            }
        }

        private CameraMode _currentModeBeforeSelfie = CameraMode.ThirdPerson;

        private void SwitchToMode(CameraMode mode)
        {
            if (_currentMode == mode) return;

            // Запоминаем основной режим перед входом в селфи
            if (mode == CameraMode.Selfie)
            {
                _currentModeBeforeSelfie = _currentMode;
            }

            // Сначала сбросим все приоритеты
            _thirdPersonVcam.Priority = MAIN_PRIORITY;
            _firstPersonVcam.Priority = MAIN_PRIORITY;
            _selfieVcam.Priority = MAIN_PRIORITY;

            // Активируем нужную камеру
            switch (mode)
            {
                case CameraMode.ThirdPerson:
                    _thirdPersonVcam.Priority = ACTIVE_PRIORITY;
                    break;
                case CameraMode.FirstPerson:
                    _firstPersonVcam.Priority = ACTIVE_PRIORITY;
                    break;
                case CameraMode.Selfie:
                    _selfieVcam.Priority = SELFIE_PRIORITY;
                    break;
            }

            _currentMode = mode;

        }

        private void HandleMainCameraSwitch()
        {
            // Получаем состояние блокировки из PlayerController
            if (TryGetComponent<PlayerController>(out var playerController))
            {
                if (playerController.LockCameraOnEsc)
                    return; // не двигаем камеру
            }
            float scroll = _input.mouseScrollDelta;
            _input.mouseScrollDelta = 0f;

            if (Mathf.Abs(scroll) < 0.01f) return;

            // Переключаем между Third и First
            CameraMode newMode = _currentMode == CameraMode.ThirdPerson
                ? CameraMode.FirstPerson
                : CameraMode.ThirdPerson;

            SwitchToMode(newMode);
        }

        private void HandleSelfieZoom()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (scroll == 0f) return;

            _currentSelfieDistance -= scroll * 0.5f;
            _currentSelfieDistance = Mathf.Clamp(_currentSelfieDistance, minDistance, maxDistance);
        }

    }
}