// Assets/Scripts/UI/AimUI.cs
using UnityEngine;
using Assets.Scripts.Player;

namespace Assets.Scripts.UI
{
    public class AimUI : MonoBehaviour
    {
        [SerializeField] private GameObject _crosshairRoot;
        [SerializeField] private PanelsUIController _panelsController;
        [SerializeField] private PauseManager _pauseManager;

        private bool _subscribed;
        private bool _lastVisible;

        private void Update()
        {
            // Ленивая подписка на событие PlayerAimingSystem
            if (!_subscribed && PlayerAimingSystem.Instance != null)
            {
                PlayerAimingSystem.Instance.OnCrosshairVisibilityChanged += OnCrosshairVisibilityChanged;
                _subscribed = true;
            }

            // Пересчитываем видимость каждый кадр — это дёшево (несколько проверок bool)
            RefreshVisibility();
        }

        private void OnDisable()
        {
            if (_subscribed && PlayerAimingSystem.Instance != null)
                PlayerAimingSystem.Instance.OnCrosshairVisibilityChanged -= OnCrosshairVisibilityChanged;

            _subscribed = false;
        }

        // Событие — просто повод, а не единственный источник правды
        private void OnCrosshairVisibilityChanged(bool _)
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            bool shouldShow = PlayerAimingSystem.Instance != null
                              && PlayerAimingSystem.Instance.IsCrosshairVisible;

            if (shouldShow)
            {
                // Скрываем прицел, если открыта панель (инвентарь / радиальное меню)
                if (_panelsController != null && _panelsController.IsPanelOpened())
                    shouldShow = false;

                // Скрываем при паузе
                if (_pauseManager != null && _pauseManager.IsPauseOpened())
                    shouldShow = false;

                // Скрываем на экране смерти
                if (DeathScreenManager.Instance != null
                    && DeathScreenManager.Instance.IsDeathScreenOpened())
                    shouldShow = false;
            }

            if (shouldShow == _lastVisible) return;
            _lastVisible = shouldShow;

            if (_crosshairRoot != null)
                _crosshairRoot.SetActive(shouldShow);
        }
    }
}