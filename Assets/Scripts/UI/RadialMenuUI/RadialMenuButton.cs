// Assets/Scripts/UI/RadialMenu/RadialMenuButton.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.RadialMenuUI
{
    /// <summary>
    /// Одна кнопка в радиальном меню. Хранит RadialMenuEntry и шлёт клик в RadialMenuController.
    /// </summary>
    public class RadialMenuButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;

        public RadialMenuEntry Entry { get; private set; }

        private System.Action<RadialMenuEntry> _onClick;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>
        /// Настроить кнопку.
        /// </summary>
        public void Setup(RadialMenuEntry entry, string displayLabel, System.Action<RadialMenuEntry> onClick)
        {
            Entry = entry;
            _onClick = onClick;

            if (_label != null) _label.text = displayLabel;

            if (_icon != null)
            {
                if (entry.icon != null)
                {
                    _icon.sprite = entry.icon;
                    _icon.enabled = true;
                }
                else
                {
                    _icon.enabled = false;
                }
            }
        }

        private void HandleClick()
        {
            if (Entry == null) return;
            _onClick?.Invoke(Entry);
        }
    }
}