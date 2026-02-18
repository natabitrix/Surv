using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class ButtonScaleEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Настройки анимации")]
        public float pressedScale = 1.1f;
        public float animationDuration = 0.1f;

        private Vector3 _originalScale;
        private bool _isPressed = false;
        private Button _button; // ← добавили ссылку на Button

        void Start()
        {
            _originalScale = transform.localScale;
            _button = GetComponent<Button>(); // ← получаем компонент Button
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Анимация только если кнопка активна
            if (_button != null && !_button.interactable)
                return;

            _isPressed = true;
            StartCoroutine(AnimateScale(pressedScale));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Анимация только если кнопка активна (и была нажата)
            if (_button != null && !_button.interactable)
                return;

            _isPressed = false;
            StartCoroutine(AnimateScale(_originalScale.x));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isPressed)
            {
                // Даже при выходе — проверяем interactable (на всякий случай)
                if (_button != null && !_button.interactable)
                    return;

                _isPressed = false;
                StartCoroutine(AnimateScale(_originalScale.x));
            }
        }

        IEnumerator AnimateScale(float targetScale)
        {
            float elapsedTime = 0f;
            float startScale = transform.localScale.x;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = elapsedTime / animationDuration;
                float currentScale = Mathf.Lerp(startScale, targetScale, t);
                transform.localScale = Vector3.one * currentScale;
                yield return null;
            }

            transform.localScale = Vector3.one * targetScale;
        }
    }
}