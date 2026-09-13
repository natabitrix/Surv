using System;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Assets.Scripts.UI
{
    public class MainMenuManager : MonoBehaviour
    {

        public GameObject MainMenuPanel;
        public GameObject SettingsPanel;

        public String GameScene;

        public float timeScale = 1f;


        [SerializeField] private SettingsPanelController _settingsController;

        private void Start()
        {

        }

        private void Update()
        {

        }

        public void SetCursorVisible(bool isCursorVisible)
        {
            Cursor.lockState = isCursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isCursorVisible;
        }

        // Ставит на паузу полностью игру
        public void SetRealPause(bool on)
        {
            if (on)
                Time.timeScale = 0;
            else
                Time.timeScale = timeScale;
        }

        public void OpenSettings()
        {
            MainMenuPanel.SetActive(false);
            SettingsPanel.SetActive(true);

            // Уведомляем контроллер настроек, что панель открыта
            if (_settingsController != null)
            {
                _settingsController.OnSettingsOpened();
            }
        }

        public void ApplySettings()
        {
            // Сначала выполняем логику сохранения
            if (_settingsController != null)
            {
                _settingsController.OnSaveButtonClicked();
            }
            MainMenuPanel.SetActive(true);
            SettingsPanel.SetActive(false);
        }

        public void CancelSettings()
        {
            // Сначала выполняем логику отмены
            if (_settingsController != null)
            {
                _settingsController.OnCancelButtonClicked();
            }

            MainMenuPanel.SetActive(true);
            SettingsPanel.SetActive(false);
        }

        public void LoadGameScene()
        {
            SetRealPause(false);
            SetCursorVisible(false);
            SceneManager.LoadScene(GameScene);
        }


        public void quitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}