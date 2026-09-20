using Assets.Scripts.Core;
using Assets.Scripts.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// Управляет экраном смерти.
    /// Показывается по событию PlayerSurvivalSystem.OnPlayerDied
    /// (с задержкой 2.5 сек, чтобы ragdoll успел упасть).
    /// Скрывается по OnPlayerRespawned.
    ///
    /// ВАЖНО: по умолчанию НЕ морозит мир (как в ARK).
    /// Флаг deathScreenFreezesWorld в GameSettings — если нужно вернуть паузу.
    /// </summary>
    public class DeathScreenManager : MonoBehaviour
    {
        public static DeathScreenManager Instance { get; private set; }

        [Header("UI")]
        [Tooltip("Корневой Canvas экрана смерти (DeathCanvas).")]
        [SerializeField] private GameObject _deathCanvas;

        [Tooltip("Опционально: затемняющий оверлей внутри Canvas.")]
        [SerializeField] private GameObject _fullOverlay;

        [Header("Ссылки")]
        [SerializeField] private PlayerController _playerController;

        [Header("Сцены")]
        [SerializeField] private string _mainMenuScene = "MainMenu";

        private bool _isDeathScreenOpened = false;
        public bool IsDeathScreenOpened() => _isDeathScreenOpened;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (_deathCanvas != null) _deathCanvas.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show()
        {
            if (_isDeathScreenOpened) return;
            _isDeathScreenOpened = true;

            if (_deathCanvas != null) _deathCanvas.SetActive(true);
            if (_fullOverlay != null) _fullOverlay.SetActive(true);

            SetRealPause(true);
            SetCursorVisible(true);

            if (_playerController != null)
                _playerController.LockCameraOnEsc = true;
        }

        public void Hide()
        {
            if (!_isDeathScreenOpened) return;
            _isDeathScreenOpened = false;

            if (_deathCanvas != null) _deathCanvas.SetActive(false);
            if (_fullOverlay != null) _fullOverlay.SetActive(false);

            SetRealPause(false);
            SetCursorVisible(false);

            if (_playerController != null)
                _playerController.LockCameraOnEsc = false;
        }

        // === КНОПКИ ===

        public void OnRespawnButtonClick()
        {
            // PlayerSurvivalSystem.Respawn() вызовет OnPlayerRespawned →
            // UIManager.HandlePlayerRespawned → DeathScreenManager.Hide()
            if (PlayerSurvivalSystem.Instance != null)
                PlayerSurvivalSystem.Instance.Respawn();
        }

        public void OnQuitToMainMenuButtonClick()
        {
            SetRealPause(false);
            SetCursorVisible(false);

            if (PlayerProgress.Instance != null)
                PlayerProgress.Instance.Save("DeathScreen.QuitToMainMenu");

            SceneManager.LoadScene(_mainMenuScene);
        }

        public void OnQuitGameButtonClick()
        {
            SetRealPause(false);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // === ВСПОМОГАТЕЛЬНОЕ ===

        private void SetRealPause(bool on)
        {
            // В мультиплеере — никогда.
            // В синглплеере — только если deathScreenFreezesWorld = true в GameSettings.
            if (!SessionMode.DeathScreenFreezesWorld) return;

            Time.timeScale = on ? SessionMode.SingleplayerPauseTimeScale : 1f;
        }

        private void SetCursorVisible(bool visible)
        {
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = visible;
        }
    }
}