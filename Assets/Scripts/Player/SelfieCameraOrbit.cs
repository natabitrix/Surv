using UnityEngine;

namespace Assets.Scripts.Player
{
    public class SelfieCameraOrbit : MonoBehaviour
    {

        public Transform playerCameraRoot;
        public float sensitivity = 2f;

        [Header("Zoom")]
        public float minDistance = 1f;
        public float maxDistance = 20f;
        private float _distance = 10f;

        private float _yaw = 0f;
        private float _pitch = 10f;

        void LateUpdate()
        {
            if (playerCameraRoot == null) return;

            var player = GameObject.FindGameObjectWithTag("Player");

            // Получаем состояние блокировки из PlayerController
            if (player.TryGetComponent<PlayerController>(out var playerController))
            {
                if (playerController.LockCameraOnEsc)
                    return; // не двигаем камеру
            }

            // Вращение мышью
            _yaw += Input.GetAxis("Mouse X") * sensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            _pitch = Mathf.Clamp(_pitch, -89f, 89f); // почти полная сфера

            // Zoom
            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0)
            {
                _distance -= scroll * 0.5f;
                _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
            }

            // Позиционирование по сфере
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = playerCameraRoot.position - rotation * Vector3.forward * _distance;
            transform.LookAt(playerCameraRoot);
        }
    }
}