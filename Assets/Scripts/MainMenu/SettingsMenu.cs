using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts.MainMenu
{
    public class SettingsMenu : MonoBehaviour
    {
        public GameObject settingsPanel; // Панель настроек
        public Slider volumeSlider; // Слайдер громкости
        public TMP_Dropdown graphicsDropdown; // Выбор графики

        private const string VolumeKey = "GameVolume"; // Ключ для сохранения громкости

        void Start()
        {
            // Инициализация настроек
            InitializeVolume();
            InitializeGraphicsDropdown();
        }

        private void InitializeVolume()
        {
            // Загрузка сохраненной громкости (по умолчанию 1.0)
            float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1.0f);
            AudioListener.volume = savedVolume;
            volumeSlider.value = savedVolume;
        }

        private void InitializeGraphicsDropdown()
        {
            // Очистка старых опций
            graphicsDropdown.ClearOptions();

            // Получение списка уровней графики
            string[] qualityLevels = QualitySettings.names;

            // Добавление уровней графики в Dropdown
            graphicsDropdown.AddOptions(new System.Collections.Generic.List<string>(qualityLevels));

            // Установка текущего уровня графики
            graphicsDropdown.value = QualitySettings.GetQualityLevel();
            graphicsDropdown.RefreshShownValue();
        }

        public void SetVolume(float volume)
        {
            // Установка громкости
            AudioListener.volume = volume;

            // Сохранение громкости в PlayerPrefs
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save(); // Сохранение изменений
        }

        public void SetGraphics(int qualityIndex)
        {
            // Установка выбранного уровня графики
            QualitySettings.SetQualityLevel(qualityIndex);

            // Сохранение уровня графики (опционально)
            PlayerPrefs.SetInt("GraphicsQuality", qualityIndex);
            PlayerPrefs.Save();
        }

        public void OpenSettings()
        {
            settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            settingsPanel.SetActive(false);
        }

        public void ResetSettings()
        {
            // Сброс громкости
            float defaultVolume = 1.0f;
            AudioListener.volume = defaultVolume;
            volumeSlider.value = defaultVolume;
            PlayerPrefs.SetFloat(VolumeKey, defaultVolume);

            // Сброс уровня графики
            int defaultQuality = QualitySettings.names.Length - 1; // Самый высокий уровень
            QualitySettings.SetQualityLevel(defaultQuality);
            graphicsDropdown.value = defaultQuality;
            PlayerPrefs.SetInt("GraphicsQuality", defaultQuality);

            PlayerPrefs.Save();
        }

    }
}