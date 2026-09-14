using System;
using Assets.Scripts.Core;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Assets.Scripts.UI
{
    public class DeathScreenManager : MonoBehaviour
    {
        public GameObject DeathSceenCanvas;
        // public GameObject PausePanel;

        public String GameScene;
        // public String MainMenuScene;

        public float timeScale = 1f;

        private bool _isDeathSceenOpened = false;


        // [SerializeField] private PlayerInputHandler _input;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PanelsUIController _panelsController;

        public bool IsDeathSceenOpened() => _isDeathSceenOpened;


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

        public void Show()
        {

            if (_panelsController != null)
            {
                _panelsController.CloseRadialMenu();
                _panelsController.CloseAllPanels();
            }

            DeathSceenCanvas.SetActive(true);
            LockCamera(true);
            _isDeathSceenOpened = true;
            // SetRealPause(true);
            SetCursorVisible(true);
        }

        // public void ResumeFromPause()
        // {
        //     DeathSceenCanvas.SetActive(false);
        //     SetCursorVisible(false);
        //     LockCamera(false);
        //     _isDeathSceenOpened = false;
        //     SetRealPause(false);
        // }


        public void LoadGameScene()
        {
            SetRealPause(false);
            _isDeathSceenOpened = false;
            SetCursorVisible(false);
            // Перечитываем файл перед загрузкой сцены
            PlayerProgress.Instance?.ReloadFromFile();
            SceneManager.LoadScene(GameScene);

            if (PlayerSurvivalSystem.Instance != null)
            {
                PlayerSurvivalSystem.Instance.Respawn();
            }
        }


    }
}