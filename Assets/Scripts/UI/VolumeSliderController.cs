using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VolumeSliderController : MonoBehaviour
{
    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        
        // Инициализация значения слайдера текущей громкостью
        if (AudioManager.Instance != null)
        {
            _slider.value = AudioManager.Instance.masterVolume;
        }

        // Подписка на событие изменения значения
        // Используем AddListener для чистой подписки без дублирования
        _slider.onValueChanged.RemoveAllListeners();
        _slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(value);
        }
    }
    
    // Важно: Этот метод гарантирует, что логика UI не конфликтует с InputSystem игры,
    // так как событие вызывается только при взаимодействии именно с этим UI элементом.
}