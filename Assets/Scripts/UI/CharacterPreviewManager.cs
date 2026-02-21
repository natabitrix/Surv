// Assets/Scripts/UI/CharacterPreviewManager.cs
using System.Collections;
using Assets.Scripts.Player;
using UnityEngine;
using UnityEngine.InputSystem; // Подключаем новый инпут

namespace Assets.Scripts.UI
{
    public class CharacterPreviewManager : MonoBehaviour
    {
        [Header("References")]
        public GameObject previewCamera;
        public GameObject playerModel; // текущая модель персонажа
        public RectTransform previewImageRect; // RawImage RectTransform для проверки попадания

        [Header("Rotation Settings")]
        public float horizontalSensitivity = 0.5f; // Чувствительность по горизонтали
        public float verticalSensitivity = 0.5f;   // Чувствительность по вертикали

        private Transform previewParent;
        private GameObject previewInstance;
        private Transform rotationTarget;
        
        private bool isRotating = false;
        private Vector2 rotationAngles = new Vector2(0f, 180f); // X — вертикаль, Y — горизонталь

        [SerializeField] private PlayerInputHandler _inputHandler;

        private void Awake()
        {
            if (_inputHandler == null)
            {
                _inputHandler = FindFirstObjectByType<PlayerInputHandler>();
            }
        }

        // ========================
        // SUBSCRIPTION TO INPUT EVENTS
        // ========================
        private void OnEnable()
        {
            if (_inputHandler != null)
            {
                // Подписываемся на событие начала "огня" (ЛКМ)
                _inputHandler.OnFireTriggered += StartRotationCheck;
                // Подписываемся на событие окончания "огня"
                _inputHandler.OnFireEnded += StopRotation;
            }
        }

        private void OnDisable()
        {
            if (_inputHandler != null)
            {
                _inputHandler.OnFireTriggered -= StartRotationCheck;
                _inputHandler.OnFireEnded -= StopRotation;
            }
            // Сбрасываем вращение при закрытии панели
            isRotating = false;
        }

        // Вызывается событием при нажатии ЛКМ
        private void StartRotationCheck()
        {
            // Проверяем, находится ли курсор над областью превью ТОЛЬКО в момент нажатия
            if (IsMouseOverPreview())
            {
                isRotating = true;
            }
        }

        // Вызывается событием при отпускании ЛКМ
        private void StopRotation()
        {
            isRotating = false;
        }

        // ========================
        // PREVIEW MANAGEMENT
        // ========================

        public void OpenPreview()
        {
            if (!previewCamera) return;
            if (previewCamera.gameObject.activeSelf) return;

            previewCamera.SetActive(true);

            if (previewParent == null)
            {
                var parentObj = new GameObject("CharacterPreviewParent");
                parentObj.transform.SetParent(transform, false);
                parentObj.layer = LayerMask.NameToLayer("PreviewCharacter");
                previewParent = parentObj.transform;
            }

            if (previewInstance == null && playerModel != null)
            {
                previewInstance = new GameObject("PreviewPivot")
                {
                    layer = LayerMask.NameToLayer("PreviewCharacter")
                };

                GameObject modelInstance = Instantiate(playerModel, previewInstance.transform);
                modelInstance.layer = LayerMask.NameToLayer("PreviewCharacter");
                SetLayerRecursively(modelInstance, LayerMask.NameToLayer("PreviewCharacter"));

                modelInstance.transform.localPosition = Vector3.down * 0.5f;

                previewInstance.transform.SetParent(previewParent, false);
                previewInstance.transform.localPosition = Vector3.zero;

                var animator = modelInstance.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.SetFloat(Animator.StringToHash("Speed"), 0);
                }

                rotationTarget = previewInstance.transform;
                ResetRotation();
            }
        }

        public void RefreshPreview()
        {
            if (previewCamera != null && previewCamera.activeSelf)
            {
                ClosePreview();
                OpenPreview();
            }
        }

        public void ClosePreview()
        {
            if (previewCamera) previewCamera.SetActive(false);
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
                rotationTarget = null;
            }
            isRotating = false;
        }

        // ========================
        // ROTATION LOGIC (UPDATE)
        // ========================

        void LateUpdate()
        {
            if (rotationTarget == null || !isRotating) return;

            // Читаем дельту мыши из НОВОЙ системы ввода
            // Mouse.current.delta возвращает вектор смещения за кадр
            Vector2 mouseDelta = Mouse.current?.delta.ReadValue() ?? Vector2.zero;

            if (mouseDelta != Vector2.zero)
            {
                // Вращаем: Mouse X влияет на Y угол (горизонт), Mouse Y на X угол (вертикаль)
                rotationAngles.y += mouseDelta.x * horizontalSensitivity;
                rotationAngles.x -= mouseDelta.y * verticalSensitivity;

                // Ограничение по вертикали (чтобы не перевернуть модель вверх ногами)
                rotationAngles.x = Mathf.Clamp(rotationAngles.x, -80f, 80f);
            }

            rotationTarget.localEulerAngles = new Vector3(rotationAngles.x, rotationAngles.y, 0f);
        }

        public void ResetRotation()
        {
            rotationAngles = new Vector2(0f, 180f);
            if (rotationTarget != null)
            {
                rotationTarget.localEulerAngles = new Vector3(rotationAngles.x, rotationAngles.y, 0f);
            }
            isRotating = false;
        }

        // ========================
        // UTILITIES
        // ========================

        bool IsMouseOverPreview()
        {
            if (previewImageRect == null) return false;
            
            // Используем RectTransformUtility для проверки попадания точки в прямоугольник UI
            return RectTransformUtility.RectangleContainsScreenPoint(previewImageRect, Mouse.current.position.ReadValue());
        }

        void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}