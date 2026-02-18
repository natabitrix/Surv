using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        public void StartGame()
        {
            // Загружает сцену с именем "GameScene" (замени на имя твоей игровой сцены)
            SceneManager.LoadScene("Game");
        }

        public void QuitGame()
        {
            // Выход из игры
            Application.Quit();

            // Для тестирования в редакторе
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
        
    }
}

