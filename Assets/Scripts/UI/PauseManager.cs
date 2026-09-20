using Assets.Scripts.Core;
using Assets.Scripts.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.UI
{
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }

        [Header("UI")]
        public GameObject PauseCanvas;
        public GameObject PausePanel;
        public GameObject SettingsPanel;

        [Header("Сцены")]
        public string GameScene;
        public string MainMenuScene;

        [SerializeField] private PlayerInputHandler _input;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PanelsUIController _panelsController;
        [SerializeField] private SettingsPanelController _settingsController;

        private bool _isPauseOpened = false;
        public bool IsPauseOpened() => _isPauseOpened;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // Не открываем паузу поверх экрана смерти
            if (DeathScreenManager.Instance != null && DeathScreenManager.Instance.IsDeathScreenOpened())
                return;

            if (_input.cancel)
            {
                bool anyPanelsOpened = false;

                if (_panelsController != null)
                {
                    if (_panelsController.IsRadialMenuOpened())
                    {
                        anyPanelsOpened = true;
                        _panelsController.CloseRadialMenu();
                    }
                    if (_panelsController.IsInventoryOpened())
                    {
                        anyPanelsOpened = true;
                        _panelsController.CloseAllPanels();
                    }
                }

                if (!anyPanelsOpened)
                {
                    if (_isPauseOpened) ResumeFromPause();
                    else SetPause();
                }

                _input.ResetCancel();
            }
        }

        public void SetPause()
        {
            if (_isPauseOpened) return;
            _isPauseOpened = true;

            PauseCanvas.SetActive(true);
            PausePanel.SetActive(true);
            SettingsPanel.SetActive(false);

            LockCamera(true);
            SetCursorVisible(true);
            SetRealPause(true);
        }

        public void ResumeFromPause()
        {
            if (!_isPauseOpened) return;
            _isPauseOpened = false;

            PauseCanvas.SetActive(false);
            PausePanel.SetActive(false);
            SettingsPanel.SetActive(false);

            SetCursorVisible(false);
            LockCamera(false);
            SetRealPause(false);
        }

        // === КНОПКИ ===

        /// <summary>
        /// Кнопка «Умереть» на паузе (для теста).
        /// </summary>
        public void OnDieButtonClick()
        {
            // 1. Скрываем паузу
            ForceHidePause();

            // 2. Снимаем паузу с timeScale, иначе DieManually может «зависнуть»
            SetRealPause(false);

            // 3. Флаг смерти — PlayerSurvivalSystem.Update его поймает
            if (PlayerSurvivalSystem.Instance != null)
                PlayerSurvivalSystem.Instance.DieManually = true;
        }

        private void ForceHidePause()
        {
            _isPauseOpened = false;
            if (PauseCanvas != null) PauseCanvas.SetActive(false);
            if (PausePanel != null) PausePanel.SetActive(false);
            if (SettingsPanel != null) SettingsPanel.SetActive(false);
        }

        // === НАСТРОЙКИ ===

        public void OpenSettings()
        {
            PausePanel.SetActive(false);
            SettingsPanel.SetActive(true);
            _settingsController?.OnSettingsOpened();
        }

        public void ApplySettings()
        {
            _settingsController?.OnSaveButtonClicked();
            PausePanel.SetActive(true);
            SettingsPanel.SetActive(false);
        }

        public void CancelSettings()
        {
            _settingsController?.OnCancelButtonClicked();
            PausePanel.SetActive(true);
            SettingsPanel.SetActive(false);
        }

        // === ВЫХОД ===

        public void LoadMainMenuScene()
        {
            if (PlayerProgress.Instance != null)
                PlayerProgress.Instance.Save("ReturnToMainMenu");

            SetRealPause(false);
            SceneManager.LoadScene(MainMenuScene);
        }

        public void quitGame()
        {
            SetRealPause(false);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // === ВСПОМОГАТЕЛЬНОЕ ===

        private void LockCamera(bool isLock)
        {
            if (_playerController != null)
                _playerController.LockCameraOnEsc = isLock;
        }

        private void SetCursorVisible(bool visible)
        {
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = visible;
        }

        private void SetRealPause(bool on)
        {
            // В мультиплеере пауза НЕ морозит мир.
            if (on && !SessionMode.ShouldPauseTimeScale) return;
            Time.timeScale = on ? SessionMode.SingleplayerPauseTimeScale : 1f;
        }
    }
}