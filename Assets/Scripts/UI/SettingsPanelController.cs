using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Ссылки")]
    public Slider volumeSlider;
    
    private float _volumeBeforeOpen; // Значение громкости до открытия настроек

    // Вызывать при открытии панели настроек (например, из PauseManager.OpenSettings)
    public void OnSettingsOpened()
    {
        if (AudioManager.Instance != null)
        {
            // Запоминаем текущую громкость (которая может быть изменена в игре)
            _volumeBeforeOpen = AudioManager.Instance.masterVolume;
            
            // Синхронизируем ползунок с текущей громкостью
            if (volumeSlider != null)
            {
                volumeSlider.value = _volumeBeforeOpen;
            }
        }
    }

    // Привязать к кнопке "Сохранить"
    public void OnSaveButtonClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SaveVolumeSettings();
        }
        // Здесь можно добавить закрытие панели или возврат в главное меню паузы
    }

    // Привязать к кнопке "Отменить"
    public void OnCancelButtonClicked()
    {
        if (AudioManager.Instance != null)
        {
            // Восстанавливаем значение, которое было до открытия настроек
            AudioManager.Instance.ResetVolumeToSaved();
            
            // Возвращаем ползунок визуально в правильное положение
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioManager.Instance.masterVolume;
            }
        }
    }
}