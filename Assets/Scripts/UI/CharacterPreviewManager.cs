namespace Assets.Scripts.UI
{
    using System.Collections;
    using Assets.Scripts.Player;
    using UnityEngine;

    public class CharacterPreviewManager : MonoBehaviour
    {
        [Header("References")]
        public GameObject previewCamera;

        public GameObject playerModel; // текущая модель персонажа
        public RectTransform previewImageRect; // RawImage RectTransform

        [Header("Rotation Settings")]
        public float horizontalSensitivity = 1.5f;
        public float verticalSensitivity = 1.5f;

        private Transform previewParent;
        private GameObject previewInstance;
        private Transform rotationTarget;
        private bool isRotating = false;
        private Vector2 rotationAngles = new Vector2(0f, 180f); // X — вертикаль, Y — горизонталь

        // ========================
        // PREVIEW MANAGEMENT
        // ========================

        public void OpenPreview()
        {
            if (!previewCamera) return;
            if (previewCamera.gameObject.activeSelf) return;

            previewCamera.SetActive(true);

            // Создаём previewParent, если ещё не создан
            if (previewParent == null)
            {
                var parentObj = new GameObject("CharacterPreviewParent");
                parentObj.transform.SetParent(transform, false); // делаем дочерним текущему объекту (например, UI-панели)
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
        }

        // ========================
        // ROTATION LOGIC
        // ========================

        void LateUpdate()
        {
            if (rotationTarget == null) return;

            // Начать вращение: ЛКМ + курсор над превью
            if (Input.GetMouseButtonDown(0) && IsMouseOverPreview())
            {
                isRotating = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isRotating = false;
            }

            if (isRotating && Input.GetMouseButton(0))
            {
                rotationAngles.y -= Input.GetAxis("Mouse X") * horizontalSensitivity * 100f * Time.deltaTime;
                rotationAngles.x -= Input.GetAxis("Mouse Y") * verticalSensitivity * 100f * Time.deltaTime;

                // Опционально: ограничение по вертикали
                // rotationAngles.x = Mathf.Clamp(rotationAngles.x, -60f, 80f);
            }

            rotationTarget.localEulerAngles = new Vector3(rotationAngles.x, rotationAngles.y, 0f);
        }

        public void ResetRotation()
        {
            rotationAngles = new Vector2(0f, 180f); // или (0, 0), если модель смотрит вперёд
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
            return previewImageRect != null &&
                   RectTransformUtility.RectangleContainsScreenPoint(previewImageRect, Input.mousePosition);
        }

        void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}