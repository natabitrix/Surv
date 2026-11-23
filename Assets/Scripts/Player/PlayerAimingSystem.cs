// Assets/Scripts/Player/PlayerAimingSystem.cs
using UnityEngine;
using Unity.Cinemachine;
using Assets.Scripts.Items;
using Assets.Scripts.UI;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Отвечает за:
    /// 1. Прицеливание по ПКМ — плавный зум FOV активной камеры.
    /// 2. Видимость прицела UI — прицел виден всегда, когда экипировано
    ///    дальнобойное оружие и активна не-Selfie камера.
    /// </summary>
    public class PlayerAimingSystem : MonoBehaviour
    {
        [Header("References")]
        private PlayerInputHandler _input;
        private PlayerEquipment _equipment;
        private CameraManager _cameraManager;
        [SerializeField] private PanelsUIController _panelsUIController;

        [Header("Cameras (назначить те же, что в CameraManager)")]
        [SerializeField] private CinemachineCamera _thirdPersonVcam;
        [SerializeField] private CinemachineCamera _firstPersonVcam;
        [SerializeField] private CinemachineCamera _selfieVcam;

        // === Зум (ПКМ) ===
        public bool IsAiming { get; private set; }
        public event System.Action<bool> OnAimingChanged;

        // === Прицел UI ===
        public bool IsCrosshairVisible { get; private set; }
        public event System.Action<bool> OnCrosshairVisibilityChanged;

        private float _baseFov = 60f;
        private float _targetFov = 60f;
        private float _currentFov = 60f;
        private float _blendTime = 0.15f;
        private bool _fovInitialized = false;

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            if (_input == null)
                Debug.LogError($"[PlayerAimingSystem] Нет PlayerInputHandler на {gameObject.name}!");

            _equipment = GetComponent<PlayerEquipment>();
            if (_equipment == null)
                Debug.LogError($"[PlayerAimingSystem] Нет PlayerEquipment на {gameObject.name}!");

            _cameraManager = GetComponent<CameraManager>();
            if (_cameraManager == null)
                Debug.LogError($"[PlayerAimingSystem] Нет CameraManager на {gameObject.name}!");

        }

        private void Update()
        {
            UpdateCrosshairVisibility();
            UpdateAiming();
            UpdateFovBlend();
        }

        // === Прицел UI: виден, если экипировано дальнобойное оружие и не Selfie ===
        private void UpdateCrosshairVisibility()
        {
            bool shouldShow = false;

            bool isSelfie = _cameraManager != null && _cameraManager.IsSelfie;

            if (!isSelfie && _equipment != null && _equipment.IsEquipped)
            {
                Item item = _equipment.GetCurrentItem();
                if (item != null && item.isRanged)
                    shouldShow = true;
            }

            if (shouldShow != IsCrosshairVisible)
            {
                IsCrosshairVisible = shouldShow;
                OnCrosshairVisibilityChanged?.Invoke(IsCrosshairVisible);
            }
        }

        // === Зум по ПКМ ===
        private void UpdateAiming()
        {
            bool wantAim = _input.rightClick;

            // Не прицеливаемся в Selfie
            if (_cameraManager != null && _cameraManager.IsSelfie)
                wantAim = false;

            if (_panelsUIController.IsPanelOpened())
                wantAim = false;

            // Не прицеливаемся, если не экипировано дальнобойное оружие
            if (wantAim)
            {
                if (_equipment == null || !_equipment.IsEquipped)
                {
                    wantAim = false;
                }
                else
                {
                    Item item = _equipment.GetCurrentItem();
                    if (item == null || !item.isRanged)
                        wantAim = false;
                }
            }

            if (wantAim != IsAiming)
            {
                IsAiming = wantAim;
                OnAimingChanged?.Invoke(IsAiming);
                RecalculateTargetFov();
            }
        }

        private void UpdateFovBlend()
        {
            if (_currentFov != _targetFov)
            {
                float step = (_baseFov / _blendTime) * Time.deltaTime;
                _currentFov = Mathf.MoveTowards(_currentFov, _targetFov, step);
                ApplyFovToActiveCamera();
            }
        }

        private void RecalculateTargetFov()
        {
            var cam = GetActiveCamera();
            if (cam == null) return;

            if (!_fovInitialized)
            {
                _baseFov = cam.Lens.FieldOfView;
                _currentFov = _baseFov;
                _fovInitialized = true;
            }

            if (IsAiming && _equipment != null && _equipment.IsEquipped)
            {
                Item item = _equipment.GetCurrentItem();
                if (item != null && item.aimFov > 0f)
                {
                    _targetFov = item.aimFov;
                    _blendTime = Mathf.Max(0.01f, item.aimBlendTime);
                }
                else
                {
                    _targetFov = _baseFov;
                }
            }
            else
            {
                _targetFov = _baseFov;
            }
        }

        private CinemachineCamera GetActiveCamera()
        {
            if (_cameraManager == null) return null;

            switch (_cameraManager.CurrentMode)
            {
                case CameraManager.CameraMode.FirstPerson: return _firstPersonVcam;
                case CameraManager.CameraMode.ThirdPerson: return _thirdPersonVcam;
                case CameraManager.CameraMode.Selfie: return _selfieVcam;
            }
            return null;
        }

        private void ApplyFovToActiveCamera()
        {
            var cam = GetActiveCamera();
            if (cam == null) return;

            var lens = cam.Lens;
            lens.FieldOfView = _currentFov;
            cam.Lens = lens;
        }
    }
}