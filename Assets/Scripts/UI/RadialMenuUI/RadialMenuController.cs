// Assets/Scripts/UI/RadialMenu/RadialMenuController.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.RadialMenuUI
{
    /// <summary>
    /// Управляет отображением радиального меню: стек уровней, построение кнопок.
    /// </summary>
    public class RadialMenuController : MonoBehaviour
    {
        public static RadialMenuController Instance { get; private set; }

        [Header("UI")]
        [Tooltip("Корневой GameObject радиального меню (для включения/выключения канваса).")]
        [SerializeField] private GameObject _menuRoot;
        [Tooltip("Контейнер, в который создаются кнопки.")]
        [SerializeField] private Transform _buttonsContainer;

        [Tooltip("Префаб кнопки (RadialMenuButton).")]
        [SerializeField] private RadialMenuButton _buttonPrefab;

        [Tooltip("Заголовок меню (имя существа).")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Tooltip("Кнопка 'Назад'. Скрывается на корневом уровне.")]
        [SerializeField] private GameObject _backButton;

        [Header("Layout")]
        [Tooltip("Радиус, на котором располагаются кнопки.")]
        [SerializeField] private float _radius = 220f;

        [Tooltip("Начальный угол (градусы). 0 = верх.")]
        [SerializeField] private float _startAngleDeg = 0f;

        [Tooltip("Максимум кнопок на круг. Если больше — уменьшается радиус.")]
        [SerializeField] private int _maxButtonsPerRing = 12;

        [Tooltip("Отступ между кольцами, если кнопок больше 12.")]
        [SerializeField] private float _ringStep = 80f;

        private IRadialMenuContext _context;
        private readonly List<RadialMenuEntry[]> _stack = new();
        private readonly List<RadialMenuButton> _spawnedButtons = new();
        private const int MAX_DEPTH = 3;   // 0, 1, 2 — три уровня

        private RadialMenuEntry[] CurrentEntries =>
            _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Открыть меню для контекста.
        /// </summary>
        public void Open(IRadialMenuContext context)
        {
            if (context == null)
            {
                Debug.LogError("[RadialMenuController] context == null!");
                return;
            }

            if (_menuRoot != null) _menuRoot.SetActive(true);

            _context = context;

            var config = context.GetMenuConfig();
            if (config == null || config.rootEntries == null || config.rootEntries.Length == 0)
            {
                Debug.LogWarning($"[RadialMenuController] Пустой RadialMenuConfig для {context.GetMenuTitle()}");
                return;
            }

            _stack.Clear();
            _stack.Add(config.rootEntries);

            UpdateTitle(config);
            Rebuild();
        }

        /// <summary>
        /// Закрыть меню.
        /// </summary>
        public void Close()
        {
            _stack.Clear();
            _context = null;
            ClearButtons();
            if (_menuRoot != null) _menuRoot.SetActive(false);
        }

        /// <summary>
        /// Вернуться на уровень выше. Если корень — ничего.
        /// </summary>
        public void GoBack()
        {
            if (_stack.Count <= 1) return;
            _stack.RemoveAt(_stack.Count - 1);
            Rebuild();
        }

        public bool CanGoBack() => _stack.Count > 1;

        // ==========================================
        // === REBUILD ===
        // ==========================================

        private void Rebuild()
        {
            ClearButtons();

            var entries = CurrentEntries;
            if (entries == null) return;

            // Фильтруем недоступные
            var visible = new List<RadialMenuEntry>();
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                if (entry.hideIfUnavailable && !_context.IsActionAvailable(entry.actionId)) continue;
                visible.Add(entry);
            }

            // Кнопка "Назад" — добавляем как первую (сверху), если не корень
            if (CanGoBack())
            {
                var backEntry = new RadialMenuEntry
                {
                    label = "Назад",
                    type = RadialMenuEntryType.Action,
                    actionId = "__back__",
                };
                visible.Insert(0, backEntry);
            }

            // Показываем BackButton в UI (если он есть отдельно)
            if (_backButton != null) _backButton.SetActive(CanGoBack());

            // Создаём кнопки
            int count = visible.Count;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var entry = visible[i];
                var btn = Instantiate(_buttonPrefab, _buttonsContainer);
                _spawnedButtons.Add(btn);

                // Label: динамический для Action с useDynamicLabel
                string label = entry.label;
                if (entry.type == RadialMenuEntryType.Action && entry.useDynamicLabel)
                {
                    label = _context.GetDynamicLabel(entry.actionId) ?? entry.label;
                }
                else if (entry.type == RadialMenuEntryType.Submenu)
                {
                    label = $"[{entry.label}]";
                }

                btn.Setup(entry, label, OnButtonClicked);

                // Позиция
                PositionButton(btn, i, count);
            }
        }

        private void PositionButton(RadialMenuButton btn, int index, int total)
        {
            var rt = btn.transform as RectTransform;
            if (rt == null) return;

            // Раскладываем по кругу. Если больше 12 — второе кольцо.
            int ringIndex = 0;
            int inRing = total;
            if (total > _maxButtonsPerRing)
            {
                ringIndex = index / _maxButtonsPerRing;
                inRing = Mathf.Min(_maxButtonsPerRing, total - ringIndex * _maxButtonsPerRing);
                index = index % _maxButtonsPerRing;
            }

            float step = 360f / inRing;
            float angle = _startAngleDeg + step * index;
            float rad = angle * Mathf.Deg2Rad;

            float r = _radius + ringIndex * _ringStep;

            // 0° = верх → x = sin, y = cos
            float x = Mathf.Sin(rad) * r;
            float y = Mathf.Cos(rad) * r;

            rt.anchoredPosition = new Vector2(x, y);
        }

        private void ClearButtons()
        {
            foreach (var btn in _spawnedButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            _spawnedButtons.Clear();
        }

        private void UpdateTitle(RadialMenuConfig config)
        {
            if (_titleText == null) return;

            if (!string.IsNullOrEmpty(config.titleOverride))
                _titleText.text = config.titleOverride;
            else
                _titleText.text = _context.GetMenuTitle();
        }

        // ==========================================
        // === CLICK ===
        // ==========================================

        private void OnButtonClicked(RadialMenuEntry entry)
        {
            if (entry == null) return;

            switch (entry.type)
            {
                case RadialMenuEntryType.Submenu:
                    if (_stack.Count >= MAX_DEPTH)
                    {
                        Debug.LogWarning($"[RadialMenuController] Достигнут максимум вложенности ({MAX_DEPTH}). Submenu '{entry.label}' не открыт.");
                        break;
                    }
                    if (entry.children != null && entry.children.Length > 0)
                    {
                        _stack.Add(entry.children);
                        Rebuild();
                    }
                    break;

                case RadialMenuEntryType.Radio:
                    // Radio-кнопка в подменю: устанавливает значение и возвращает на уровень выше.
                    _context.SetRadioValue(entry.radioGroupId, entry.radioValue);

                    // Визуальная обратная связь — потом.
                    // Автовозврат:
                    GoBack();
                    break;

                case RadialMenuEntryType.Action:
                    if (entry.actionId == "__back__")
                    {
                        GoBack();
                    }
                    else
                    {
                        _context.ExecuteAction(entry.actionId);

                        // Обновить labels (могли измениться после toggle)
                        Rebuild();
                    }
                    break;
            }
        }
    }
}