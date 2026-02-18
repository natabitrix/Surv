using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class TestLocalization : MonoBehaviour
{
    void Start()
    {
        if (LocalizationSettings.SelectedLocale != null)
            Debug.Log($"✅ Локализация работает. Текущий язык: {LocalizationSettings.SelectedLocale}");
        else
            Debug.LogError("❌ LocalizationSettings не настроен или не загружен.");
    }
}