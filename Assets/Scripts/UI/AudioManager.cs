using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("Настройки")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    private static AudioManager _instance;
    public static AudioManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadSavedVolume();
        ApplyVolumeToAllSources();
    }

    private void LoadSavedVolume()
    {
        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterVolume = Mathf.Clamp01(masterVolume);
        }
    }

    // Вызывается слайдером при перетаскивании (Предпросмотр)
    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeToAllSources();
        // ВАЖНО: Здесь мы НЕ сохраняем в PlayerPrefs, чтобы не засорять память промежуточными значениями
    }

    // Вызывается кнопкой "Сохранить" в меню настроек
    public void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
        Debug.Log($"[AudioManager] Настройки громкости сохранены: {masterVolume}");
    }

    // Вызывается кнопкой "Отменить" в меню настроек
    public void ResetVolumeToSaved()
    {
        LoadSavedVolume(); // Загружаем последнее сохраненное значение из памяти
        ApplyVolumeToAllSources(); // Применяем его немедленно
        Debug.Log($"[AudioManager] Громкость сброшена к сохраненной: {masterVolume}");
    }

    private void ApplyVolumeToAllSources()
    {
        // AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        AudioSource[] sources = FindObjectsByType<AudioSource>();
        foreach (var source in sources)
        {
            source.volume = masterVolume;
        }
    }
}