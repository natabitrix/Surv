// Assets/Scripts/UI/Tooltip/Tooltip.cs
namespace Assets.Scripts.UI.Tooltip
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using System.Collections.Generic;
    using Assets.Scripts.Items;

    [RequireComponent(typeof(CanvasGroup))]
    public class Tooltip : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The parent object for the title and icon. Can be hidden if both are absent.")]
        [SerializeField] private GameObject header;
        [Tooltip("The TextMeshPro component for the title text.")]
        [SerializeField] public TextMeshProUGUI titleField;
        [Tooltip("The TextMeshPro component for the main content text.")]
        [SerializeField] public TextMeshProUGUI contentField;
        [Tooltip("The Image component for the icon.")]
        [SerializeField] private Image iconField;

        [SerializeField] public GameObject bigPanel;

        [Header("Right Panel")]
        [SerializeField] public GameObject rightPanel;
        [SerializeField] public GameObject rightPanelContent;
        [SerializeField] public GameObject rightPanelListPrefab;
        [Tooltip("Text field for the right panel's title.")]
        [SerializeField] public TextMeshProUGUI rightPanelTitleField; // <-- НОВОЕ ПОЛЕ

        [SerializeField] public GameObject smallPanel;
        [SerializeField] public TextMeshProUGUI smallPanelContentField;

        public void SetText(string content, string title = "", Sprite icon = null, Color? titleColor = null, Color? iconColor = null)
        {
            // small tooltip
            if (title == "" && icon == null)
            {
                smallPanel.SetActive(true);
                bigPanel.SetActive(false);

                if (smallPanelContentField != null)
                {
                    smallPanelContentField.text = content ?? string.Empty;
                }
            }
            // big tooltip width title, text and icon
            else
            {
                bigPanel.SetActive(true);
                smallPanel.SetActive(false);

                if (contentField != null)
                {
                    contentField.text = content ?? string.Empty;
                }
            }

            // Set Title (null-safe)
            bool hasTitle = !string.IsNullOrEmpty(title);
            if (titleField != null)
            {
                titleField.gameObject.SetActive(hasTitle);
                if (hasTitle)
                {
                    titleField.text = title;
                    titleField.color = titleColor ?? Color.white;
                }
            }

            // Set Icon (null-safe)
            bool hasIcon = (icon != null);
            if (iconField != null)
            {
                iconField.gameObject.SetActive(hasIcon);
                if (hasIcon)
                {
                    iconField.sprite = icon;
                    iconField.color = iconColor ?? Color.white;
                }
            }

            // Conditionally hide the entire header area
            if (header != null)
            {
                header.SetActive(hasTitle || hasIcon);
            }
        }

        // ОБНОВЛЕННЫЙ МЕТОД
        public void SetRightPanel(List<(int amount, Item item)> rightPanelList, string rightPanelTitle = "")
        {
            // Сначала скрываем панель, чтобы избежать мерцания
            if (rightPanel != null)
                rightPanel.SetActive(false);

            // Проверяем, есть ли что показывать
            if (rightPanelList == null || rightPanelList.Count == 0)
            {
                // Если список пуст, просто выходим, панель уже скрыта
                return;
            }

            // Проверка на наличие всех необходимых ссылок
            if (rightPanel == null)
            {
                Debug.LogError("rightPanel is not assigned in Tooltip!");
                return;
            }

            if (rightPanelContent == null)
            {
                Debug.LogError("rightPanelContent is not assigned in Tooltip!");
                return;
            }

            if (rightPanelListPrefab == null)
            {
                Debug.LogError("rightPanelListPrefab is not assigned in Tooltip!");
                return;
            }

            // Устанавливаем заголовок правой панели
            if (rightPanelTitleField != null)
            {
                bool hasTitle = !string.IsNullOrEmpty(rightPanelTitle);
                rightPanelTitleField.gameObject.SetActive(hasTitle);
                if (hasTitle)
                {
                    rightPanelTitleField.text = rightPanelTitle;
                }
            }

            // Очищаем старые элементы
            foreach (Transform child in rightPanelContent.transform)
            {
                Destroy(child.gameObject);
            }

            // Создаем новые элементы
            foreach (var l in rightPanelList)
            {
                string text = $"{l.amount}x {l.item.itemName}";
                GameObject listGO = Instantiate(rightPanelListPrefab, rightPanelContent.transform, false);
                SetListText(text, listGO);
                SetListIcon(l.item.icon, listGO);
            }

            // Активируем панель
            rightPanel.SetActive(true);
        }

        private void SetListIcon(Sprite icon, GameObject noteUI)
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

        private void SetListText(string text, GameObject noteUI)
        {
            var textComponent = noteUI.GetComponentInChildren<TextMeshProUGUI>();

            if (textComponent != null)
                textComponent.text = text;
            else
                Debug.LogWarning("TextMeshProUGUI not found in notification Manager UI!");
        }
    }
}