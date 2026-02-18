// Assets/Scripts/UI/ContextMenuItemButton.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts.UI
{
    public class ContextMenuItemButton : MonoBehaviour
    {
        public TextMeshProUGUI label;
        private System.Action _onClick;

        public void Initialize(string text, System.Action onClick)
        {
            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>();

            if (label != null)
                label.text = text;

            _onClick = onClick;

            // Подключаем клик
            var button = GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClick?.Invoke());
            }
        }
    }
}