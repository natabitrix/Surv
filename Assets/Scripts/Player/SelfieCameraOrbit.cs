// Assets/Scripts/Player/SelfieCameraOrbit.cs
using UnityEngine;
using UnityEngine.InputSystem; // Подключаем новый Input System
using Assets.Scripts.Player; // Для доступа к PlayerInputHandler

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

        // Ссылка на обработчик ввода, чтобы проверять режим селфи
        private PlayerInputHandler _inputHandler;

        void Awake()
        {
            // Пытаемся найти обработчик ввода автоматически
            _inputHandler = FindFirstObjectByType<PlayerInputHandler>();
        }

        void LateUpdate()
        {
            if (playerCameraRoot == null) return;

            // 1. Проверка: активен ли режим селфи-камеры?
            // Если игрок не включил селфи-режим, выходим сразу.
            if (_inputHandler == null || !_inputHandler.selfieCamera)
            {
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");

            // 2. Проверка: не заблокирована ли камера (например, пауза или меню)?
            if (player.TryGetComponent<PlayerController>(out var playerController))
            {
                if (playerController.LockCameraOnEsc)
                    return; 
            }

            // === ВРАЩЕНИЕ МЫШЬЮ (Новая система) ===
            // Mouse.current.delta возвращает смещение в пикселях за кадр
            Vector2 mouseDelta = Mouse.current?.delta.ReadValue() ?? Vector2.zero;

            if (mouseDelta != Vector2.zero)
            {
                // Умножаем на чувствительность. 
                // В новой системе не нужно умножать на Time.deltaTime, так как delta уже "за кадр".
                // Коэффициент 0.1f подобран эмпирически, так как delta мыши обычно больше, чем GetAxis (0..1).
                // Можете настроить sensitivity в инспекторе под себя.
                _yaw += mouseDelta.x * sensitivity * 0.1f;
                _pitch -= mouseDelta.y * sensitivity * 0.1f;
                
                // Ограничение по вертикали (чтобы не перевернуть камеру)
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);
            }

            // === ЗУМ КОЛЕСИКОМ (Новая система) ===
            // Mouse.current.scroll возвращает вектор прокрутки (x, y)
            // float scrollY = Mouse.current?.scroll.ReadValue().y ?? 0f;

            if (_inputHandler.mouseScrollDelta != 0f)
            {
                // scroll.y обычно равен 1 или -1 за шаг колеса.
                // Умножаем на коэффициент скорости зума (0.5f как в старом коде)
                _distance -= _inputHandler.mouseScrollDelta * 0.5f; 
                _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
            }

            // === ПОЗИЦИОНИРОВАНИЕ ===
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            
            // Камера располагается сзади объекта (вектор forward * расстояние)
            transform.position = playerCameraRoot.position - rotation * Vector3.forward * _distance;
            
            // Камера всегда смотрит на цель
            transform.LookAt(playerCameraRoot);
        }
    }
}