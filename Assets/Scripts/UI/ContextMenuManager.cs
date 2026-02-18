using Assets.Scripts.InventorySystem;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// Управляет показом и скрытием контекстного меню.
    /// Требует ручного создания Canvas в редакторе.
    /// </summary>
    public class ContextMenuManager : MonoBehaviour
    {
        public static ContextMenuManager Instance { get; private set; }

        // === НАЗНАЧАТЬ В ИНСПЕКТОРЕ ===
        [Header("Ссылки на UI (создаются вручную в редакторе)")]
        public Canvas contextMenuCanvas;     // Основной Canvas меню
        public RectTransform Panel; // Панель фона (Panel)
        public RectTransform contentPanel;    // Контейнер для кнопок (обычно пустой GameObject внутри Panel)

        [Header("Префабы")]
        public GameObject menuItemButtonPrefab; // Префаб кнопки из папки Prefabs

        private Item _currentItem;
        private System.Action _onUseCallback;
        private System.Action _onDropCallback;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public static void Show(Item item, System.Action onUse, System.Action onDrop, Vector2 position)
        {
            if (Instance == null) return;
            Instance._currentItem = item;
            Instance._onUseCallback = onUse;
            Instance._onDropCallback = onDrop;
            Instance._menuPosition = position;
            Instance.BuildAndShow();
        }

        private Vector2 _menuPosition = Vector2.zero;

        void BuildAndShow()
        {
            // Активируем Canvas, если он неактивен
            if (!contextMenuCanvas.gameObject.activeSelf)
            {
                contextMenuCanvas.gameObject.SetActive(true);
            }

            // Очистка
            for (int i = contentPanel.childCount - 1; i >= 0; i--)
            {
                Destroy(contentPanel.GetChild(i).gameObject);
            }

            // Добавление кнопок
            if (_currentItem != null)
            {
                AddButton("Использовать", OnUseClicked);
                AddButton("Выбросить", OnDropClicked);
            }

            // Позиционирование
            PositionMenu();
        }

        void AddButton(string text, System.Action onClick)
        {
            if (menuItemButtonPrefab == null)
            {
                Debug.LogError("menuItemButtonPrefab is not assigned!");
                return;
            }

            var buttonObj = Instantiate(menuItemButtonPrefab, contentPanel);
            var buttonComp = buttonObj.GetComponent<ContextMenuItemButton>();
            if (buttonComp != null)
            {
                buttonComp.Initialize(text, onClick);
            }
            else
            {
                Debug.LogWarning("ContextMenuItemButton component not found on prefab!");
            }
        }

        void PositionMenu()
        {
            if (Panel == null || contextMenuCanvas == null) return;

            Camera cam = contextMenuCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : contextMenuCanvas.worldCamera;
            Vector2 localPoint;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                contextMenuCanvas.transform as RectTransform,
                _menuPosition,
                cam,
                out localPoint))
            {
                // Смещение: вниз и вправо от курсора
                // localPoint += new Vector2(10, -Panel.sizeDelta.y - 10);
                localPoint += new Vector2(100, 0);
                Panel.anchoredPosition = localPoint;
            }



        }

        void OnUseClicked()
        {
            Debug.Log("Use clicked");
            _onUseCallback?.Invoke();
            Hide();
        }

        // Пример дополнительной кнопки (раскомментируй при необходимости)
        void OnDropClicked()
        {
            Debug.Log("Drop clicked");
            _onDropCallback?.Invoke();
            Hide();
        }

        /// <summary>
        /// Скрыть контекстное меню.
        /// </summary>
        public void Hide()
        {
            if (contextMenuCanvas != null)
            {
                contextMenuCanvas.gameObject.SetActive(false);
            }
            _currentItem = null;
            _onUseCallback = null;
        }
    }
}