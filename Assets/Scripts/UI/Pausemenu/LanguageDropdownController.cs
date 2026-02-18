// Assets/Scripts/Pausemenu/LanguageDropdownController.cs
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Assets.Scripts.UI.Pausemenu
{
    public class LanguageDropdownController : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;

        private void Start()
        {
            if (dropdown == null)
                dropdown = GetComponent<TMP_Dropdown>();

            // Заполняем опции из LanguageManager
            var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            var languages = LanguageManager.Instance.availableLanguages;

            foreach (var lang in languages)
            {
                options.Add(new TMP_Dropdown.OptionData(lang.displayName));
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(options);

            // Устанавливаем текущий язык
            string currentCode = LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en";
            for (int i = 0; i < languages.Count; i++)
            {
                if (languages[i].localeCode == currentCode)
                {
                    dropdown.value = i;
                    break;
                }
            }

            // Подписываемся на изменение
            dropdown.onValueChanged.AddListener(OnLanguageSelected);
        }

        private void OnLanguageSelected(int index)
        {
            string selectedCode = LanguageManager.Instance.availableLanguages[index].localeCode;
            LanguageManager.Instance.SetLanguage(selectedCode);
        }
    }
}
