using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// Управляет загрузочным экраном.
    /// Показывает экран, ждет выполнения всех задач, скрывает.
    /// </summary>
    public class LoadingScreenManager : MonoBehaviour
    {
        public static LoadingScreenManager Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private GameObject _loadingCanvas;
        [SerializeField] private Image _progressBar;
        [SerializeField] private TMPro.TextMeshProUGUI _statusText;

        [Header("Настройки")]
        [Tooltip("Минимальное время показа (сек)")]
        [SerializeField] private float _minShowTime = 1f;

        [Tooltip("Время ожидания физики ragdoll (сек)")]
        [SerializeField] private float _physicsSettleTime = 2f;

        private List<LoadingTask> _tasks = new();
        private bool _isLoading = false;
        private Coroutine _loadingCoroutine;

        private class LoadingTask
        {
            public string Name;
            public IEnumerator Routine;
            public bool IsDone;

            public LoadingTask(string name, IEnumerator routine)
            {
                Name = name;
                Routine = routine;
                IsDone = false;
            }
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_loadingCanvas != null)
                _loadingCanvas.SetActive(false);
        }

        /// <summary>
        /// Показать экран загрузки (без задач).
        /// </summary>
        public void Show(string status = "Загрузка...")
        {
            if (_loadingCanvas == null) return;

            _loadingCanvas.SetActive(true);

            if (_statusText != null)
                _statusText.text = status;

            if (_progressBar != null)
                _progressBar.fillAmount = 0f;
        }

        /// <summary>
        /// Скрыть экран.
        /// </summary>
        public void Hide()
        {
            if (_loadingCanvas == null) return;
            _loadingCanvas.SetActive(false);
        }

        /// <summary>
        /// Зарегистрировать задачу загрузки.
        /// </summary>
        public void RegisterTask(string name, IEnumerator routine)
        {
            _tasks.Add(new LoadingTask(name, routine));
        }

        /// <summary>
        /// Запустить все зарегистрированные задачи.
        /// </summary>
        public void StartLoading()
        {
            if (_isLoading)
            {
                Debug.LogWarning("[LoadingScreenManager] Загрузка уже идет!");
                return;
            }

            if (_loadingCoroutine != null)
                StopCoroutine(_loadingCoroutine);

            _loadingCoroutine = StartCoroutine(LoadingRoutine());
        }

        private IEnumerator LoadingRoutine()
        {
            _isLoading = true;

            Show("Загрузка...");
            yield return null; // Даем экрану отрисоваться

            float startTime = Time.unscaledTime;

            // === Фаза 1: Выполнение задач ===
            for (int i = 0; i < _tasks.Count; i++)
            {
                var task = _tasks[i];
                float progress = (float)i / _tasks.Count;

                SetProgress(progress, task.Name);

                // Запускаем задачу как корутину
                yield return StartCoroutine(RunTask(task));
            }

            // === Фаза 2: Ожидание физики ===
            SetProgress(0.9f, "Ожидание физики...");
            yield return new WaitForSecondsRealtime(_physicsSettleTime);

            // === Фаза 3: Минимальное время показа ===
            float elapsed = Time.unscaledTime - startTime;
            if (elapsed < _minShowTime)
            {
                yield return new WaitForSecondsRealtime(_minShowTime - elapsed);
            }

            SetProgress(1f, "Готово");
            yield return new WaitForSecondsRealtime(0.2f);

            Hide();

            _tasks.Clear();
            _isLoading = false;
            _loadingCoroutine = null;

            SetCursorVisible(false);

            // Debug.Log("[LoadingScreenManager] Загрузка завершена!");
        }

        private void SetCursorVisible(bool isCursorVisible)
        {
            Cursor.lockState = isCursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isCursorVisible;
        }

        private IEnumerator RunTask(LoadingTask task)
        {
            // Debug.Log($"[LoadingScreenManager] Начинаем: {task.Name}");
            yield return StartCoroutine(task.Routine);
            task.IsDone = true;
            // Debug.Log($"[LoadingScreenManager] Завершено: {task.Name}");
        }

        private void SetProgress(float progress, string status = null)
        {
            // Debug.Log($"[LoadingScreenManager] SetProgress: {progress}, status: {status}");

            if (_progressBar != null)
                _progressBar.fillAmount = Mathf.Clamp01(progress);

            if (_statusText != null && !string.IsNullOrEmpty(status))
                _statusText.text = status;
        }
    }
}