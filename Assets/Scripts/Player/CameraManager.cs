using UnityEngine;
using Unity.Cinemachine;

namespace Assets.Scripts.Player
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        public enum CameraMode
        {
            ThirdPerson,
            FirstPerson,
            Selfie
        }

        [Header("Cameras")]
        [SerializeField] private CinemachineCamera _thirdPersonVcam;
        [SerializeField] private CinemachineCamera _firstPersonVcam;
        [SerializeField] private CinemachineCamera _selfieVcam;

        [Header("Priorities")]
        [SerializeField] private int _inactivePriority = 0;
        [SerializeField] private int _activePriority = 100;
        [SerializeField] private int _selfiePriority = 150;

        public CameraMode CurrentMode => _currentMode;
        public bool IsSelfie => _currentMode == CameraMode.Selfie;
        public bool IsFirstPerson => _currentMode == CameraMode.FirstPerson;
        public bool IsThirdPerson => _currentMode == CameraMode.ThirdPerson;

        public event System.Action<CameraMode> OnCameraModeChanged;

        private CameraMode _currentMode = CameraMode.ThirdPerson;
        private CameraMode _modeBeforeSelfie = CameraMode.ThirdPerson;
        private PlayerInputHandler _input;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _input = GetComponent<PlayerInputHandler>();
            if (_input == null)
            {
                Debug.LogError("[CameraManager] PlayerInputHandler не найден!");
                enabled = false;
                return;
            }

            // Стартовый режим — TPS
            ApplyMode(CameraMode.ThirdPerson, force: true);
        }

        private void Update()
        {
            // Если открыто меню/пауза — не трогаем камеру
            if (TryGetComponent<PlayerController>(out var pc) && pc.LockCameraOnEsc)
                return;

            // K — toggle Selfie
            if (_input.selfieCamera)
            {
                _input.selfieCamera = false; // сброс флага, чтобы не срабатывало многократно
                ToggleSelfie();
                return;
            }

            // В Selfie колесо уходит в SelfieCameraOrbit — не трогаем
            if (IsSelfie) return;

            // Колесо: >0 → FPS, <0 → TPS
            float scroll = _input.mouseScrollDelta;
            if (Mathf.Abs(scroll) < 0.01f) return;

            _input.mouseScrollDelta = 0f;

            if (scroll > 0f && _currentMode != CameraMode.FirstPerson)
                ApplyMode(CameraMode.FirstPerson);
            else if (scroll < 0f && _currentMode != CameraMode.ThirdPerson)
                ApplyMode(CameraMode.ThirdPerson);
        }

        private void ToggleSelfie()
        {
            if (IsSelfie)
            {
                // Выходим из селфи → возвращаемся в предыдущий режим
                ApplyMode(_modeBeforeSelfie);
            }
            else
            {
                // Входим в селфи → запоминаем текущий
                _modeBeforeSelfie = _currentMode;
                ApplyMode(CameraMode.Selfie);
            }
        }

        private void ApplyMode(CameraMode mode, bool force = false)
        {
            if (!force && _currentMode == mode) return;

            // Все неактивные — inactive
            _thirdPersonVcam.Priority = _inactivePriority;
            _firstPersonVcam.Priority = _inactivePriority;
            _selfieVcam.Priority = _inactivePriority;

            // Активируем нужную
            switch (mode)
            {
                case CameraMode.ThirdPerson:
                    _thirdPersonVcam.Priority = _activePriority;
                    break;
                case CameraMode.FirstPerson:
                    _firstPersonVcam.Priority = _activePriority;
                    break;
                case CameraMode.Selfie:
                    _selfieVcam.Priority = _selfiePriority;
                    break;
            }

            _currentMode = mode;
            OnCameraModeChanged?.Invoke(mode);
        }
    }
}