using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class ButtonEffects : MonoBehaviour
    {
        [Header("Настройки")]
        public Image buttonIcon;
        public TMP_Text buttonText;

        public float opacityInteractable = 1.0f;
        public float opacityNonInteractable = 0.5f;

        private Button _button;

        private Color _buttonIconColor;
        private Color _buttonTextColor;

        void Start()
        {
            _button = GetComponent<Button>(); // ← получаем компонент Button

            if (_button == null)
                return;

            if (buttonIcon != null)
            {
                _buttonIconColor = buttonIcon.color;
            }
            if (buttonText != null)
            {
                _buttonTextColor = buttonText.color;
            }
        }

        void Update()
        {
            if (_button == null)
                return;

            if (_button.interactable)
            {
                _buttonIconColor.a = opacityInteractable;
                _buttonTextColor.a = opacityInteractable;
            }
            else
            {
                _buttonIconColor.a = opacityNonInteractable;
                _buttonTextColor.a = opacityNonInteractable;
            }

            if (buttonIcon != null)
            {
                buttonIcon.color = _buttonIconColor;
            }

            if (buttonText != null)
            {
                buttonText.color = _buttonTextColor;
            }
        }


    }
}