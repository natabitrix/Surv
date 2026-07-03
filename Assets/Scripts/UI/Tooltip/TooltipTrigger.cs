namespace Assets.Scripts.UI.Tooltip
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// Attach to any UI element to show a tooltip on hover.
    /// Also provides a static helper method for adding tooltips from code.
    /// Automatically ensures a TooltipManager exists in the scene.
    /// </summary>
    [ExecuteAlways]
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Fields
        [Header("Tooltip Content")]
        [SerializeField] private string title;
        [TextArea(3, 10)]
        [SerializeField] private string content;
        [SerializeField] private Sprite icon;

        [Header("Custom Styles")]
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color iconColor = Color.white;

        [Header("Settings")]
        [SerializeField, Min(0f)] private float hoverDelay = 0.5f;
        #endregion

        #region Public Properties
        public string Title { get => title; set => title = value; }
        public string Content { get => content; set => content = value; }
        public Sprite Icon { get => icon; set => icon = value; }
        public Color TitleColor { get => titleColor; set => titleColor = value; }
        public Color IconColor { get => iconColor; set => iconColor = value; }
        public float HoverDelay { get => hoverDelay; set => hoverDelay = value; }
        #endregion

        #region Editor and Lifecycle
        private void Reset()
        {
            EnsureManagerExists();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                EnsureManagerExists();
            }
        }
        #endregion

        public void SetContent(string content, string title = "", Sprite icon = null)
        {
            this.content = content;
            this.title = title;
            this.icon = icon;

            // Если вы хотите сразу обновить отображение при наведении — это не обязательно,
            // потому что при следующем OnPointerEnter будет использовано новое содержимое.
        }

        #region Public Static API
        /// <summary>
        /// The main public entry point to add a tooltip to any UI object from code.
        /// </summary>
        /// <returns>The created or existing TooltipTrigger component for further customization.</returns>
        public static TooltipTrigger AddTooltip(
            GameObject target,
            string content,
            string title = "",
            Sprite icon = null
        )
        {
            if (target == null)
            {
                Debug.LogError("Easy Tooltip Error: Target GameObject is null.");
                return null;
            }

            EnsureManagerExists();

            var existing = target.GetComponent<TooltipTrigger>();
            if (existing != null)
            {
                existing.SetContent(content, title, icon);
                return existing;
            }

            TooltipTrigger trigger = target.AddComponent<TooltipTrigger>();
            trigger.Content = content;
            trigger.Title = title;
            trigger.Icon = icon;

            return trigger;
        }
        #endregion

        #region Interface Implementations
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (TooltipManager.Instance != null)
            {
                TooltipManager.Instance.ShowTooltip(content, title, icon, titleColor, iconColor, hoverDelay);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (TooltipManager.Instance != null)
            {
                TooltipManager.Instance.HideTooltip();
            }
        }
        #endregion

        #region Private Helper Methods
        private static void EnsureManagerExists()
        {
            if (TooltipManager.Instance != null || FindAnyObjectByType<TooltipManager>() != null)
            {
                return;
            }
        }
        #endregion
    }
}