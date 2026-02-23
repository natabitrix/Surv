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

        // private bool _isMainMenu = false;
        private bool _isPauseOpened = false;
        

        [SerializeField] private PlayerInputHandler _input;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PanelsUIController _panelsController;

         public bool IsPauseOpened() => _isPauseOpened;


        private void Start()
        {
            // Для разработки
            SetPause();
        }

        private void Update()
        {
            if (_input.cancel)
            {
                bool anyPanelsOpened = false;
                
                if (_panelsController != null)
                {
                    if (_panelsController.IsRadialMenuOpened())
                    {
                        anyPanelsOpened = true;
                        Debug.Log("IsRadialMenuOpened");
                        _panelsController.CloseRadialMenu();
                    }
                    if (_panelsController.IsInventoryOpened())
                    {
                        anyPanelsOpened = true;
                        Debug.Log("IsInventoryOpened");
                        _panelsController.CloseAllPanels();
                    }
                }

                if (!anyPanelsOpened)
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
            _isPauseOpened = true;
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
            _isPauseOpened = false;
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
            _isPauseOpened = false;
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