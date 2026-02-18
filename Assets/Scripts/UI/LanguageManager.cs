// Assets/Scripts/Localization/LanguageManager.cs
using UnityEngine;
using UnityEngine.Localization.Settings;
using System.Collections.Generic;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    [System.Serializable]
    public struct LanguageOption
    {
        public string displayName; // "Русский", "English"
        public string localeCode;  // "ru", "en"
    }

    public List<LanguageOption> availableLanguages = new()
    {
        new LanguageOption { displayName = "English", localeCode = "en" },
        new LanguageOption { displayName = "Русский", localeCode = "ru" }
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAndApplySavedLanguage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetLanguage(string localeCode)
    {
        var locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
            PlayerPrefs.SetString("SelectedLanguage", localeCode);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.LogWarning($"Locale '{localeCode}' not found in Available Locales!");
        }
    }

    private void LoadAndApplySavedLanguage()
    {
        string savedCode = PlayerPrefs.GetString("SelectedLanguage", "en");
        SetLanguage(savedCode);
    }

    public string GetCurrentLanguageDisplayName()
    {
        string currentCode = LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en";
        foreach (var lang in availableLanguages)
        {
            if (lang.localeCode == currentCode)
                return lang.displayName;
        }
        return "Unknown";
    }
}