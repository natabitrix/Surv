// Assets/Scripts/UI/NotificationManager.cs
using System.Collections;
using Assets.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        public GameObject notificationLeftPanel;
        public GameObject notificationLeftPrefab;

        public GameObject noteTopUI;
        public TMP_Text noteTopUIText;  
        // public GameObject noteTopUIIconObj;


        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void SetNoteText(string text, GameObject noteUI)
        {
            var textComponent = noteUI.GetComponentInChildren<TextMeshProUGUI>();

            if (textComponent != null)
                textComponent.text = text;
            else
                Debug.LogWarning("TextMeshProUGUI not found in notification Manager UI!");
        }

        private void SetNoteIcon(Sprite icon, GameObject noteUI)
        {
            var imageComponent = noteUI.GetComponentInChildren<Image>();

            if (imageComponent != null)
            {
                if (icon != null)
                {
                    imageComponent.sprite = icon;
                    imageComponent.enabled = true;
                }
                else
                {
                    imageComponent.enabled = false;
                }
            }
        }

        // метод для показа уведомлений слева
        public void Show(string text, Sprite icon = null, float duration = 3f)
        {
            if (notificationLeftPrefab == null)
            {
                Debug.LogError("notificationLeftPrefab is not assigned in NotificationManager!");
                return;
            }

            GameObject notificationGO = Instantiate(notificationLeftPrefab, notificationLeftPanel.transform, false);

            SetNoteText(text, notificationGO);
            SetNoteIcon(icon, notificationGO);

            // Автоуничтожение через duration
            StartCoroutine(DestroyAfterDelay(notificationGO, duration));
        }


        // метод для показа уведомлений вверху с кнопкой закрытия
        public void ShowTopNote(string text, bool autoHide = true, float duration = 5f)
        {
            noteTopUI.SetActive(true);

            SetNoteText(text, noteTopUI);

            // Автоскрытие через duration
            if (autoHide)
                StartCoroutine(HideAfterDelay(noteTopUI, duration));
        }

        public void HideTopNote()
        {
            noteTopUI.SetActive(false);
        }



        private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
                Destroy(obj);
        }

        private IEnumerator HideAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
                obj.SetActive(false);
        }



    }
}