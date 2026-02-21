using System;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Assets.Scripts.UI.Pausemenu
{
    public class PauseManager : MonoBehaviour
    {
        public GameObject PauseCanvas;
        public GameObject PausePanel;
        public GameObject SettingsPanel;

        public String GameScene;
        public String MainMenuScene;

        // public Button EngramsPanelButton;

        public float timeScale = 1f;

        public bool IsMainMenu = false;
        public bool IsPauseOpen = false;

        [SerializeField] private PlayerInputHandler _input;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PanelsUIController _panelsController;


        private void Start()
        {
            // Для разработки
            SetPause();
        }

        private void Update()
        {
            if (_input.cancel)
            {
                if (_panelsController != null && _panelsController.IsInventoryOpened())
                {
                    _panelsController.CloseAllPanels();
                }
                else
                {
                    SetPause();
                }

                _input.ResetCancel();
            }

            // Для разработки
            // if (Input.GetMouseButtonDown(0))
            // {
            //     ResumeFromPause();
            // }
        }

        public void SetStart()
        {


        }
        public void LockCamera(bool isLock)
        {
            if (_playerController != null)
            {
                _playerController.LockCameraOnEsc = isLock;
            }
            else
            {
                Debug.Log("_playerController not found");
            }
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

        public void SetPause()
        {
            PauseCanvas.SetActive(true);
            PausePanel.SetActive(true);
            SettingsPanel.SetActive(false);
            LockCamera(true);
            IsPauseOpen = true;
            SetRealPause(true);
            SetCursorVisible(true);
        }

        public void ResumeFromPause()
        {
            PauseCanvas.SetActive(false);
            PausePanel.SetActive(false);
            SettingsPanel.SetActive(false);
            SetCursorVisible(false);
            LockCamera(false);
            IsPauseOpen = false;
            SetRealPause(false);
        }

        public void OpenSettings()
        {
            PausePanel.SetActive(false);
            SettingsPanel.SetActive(true);
        }

        public void ApplySettings()
        {
            PausePanel.SetActive(true);
            SettingsPanel.SetActive(false);
        }

        public void CancelSettings()
        {
            PausePanel.SetActive(true);
            SettingsPanel.SetActive(false);
        }

        public void LoadGameScene()
        {
            SetRealPause(false);
            IsPauseOpen = false;
            SetCursorVisible(false);
            SceneManager.LoadScene(GameScene);
        }

        public void LoadMainMenuScene()
        {
            SceneManager.LoadScene(MainMenuScene);
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