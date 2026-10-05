// Assets/Scripts/InventorySystem/ChestUIManager.cs
using Assets.Scripts.Core;
using Assets.Scripts.Interactables;
using Assets.Scripts.UI;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    /// <summary>
    /// Единая точка входа для открытия/закрытия инвентарей (сундуки, трупы, существа).
    /// Владеет состоянием "какой инвентарь сейчас открыт".
    /// Устраняет рассинхрон _currentChest / _isOpen между ChestUI, Corpse, BaseLivingEntity.
    /// Живёт на ===CoreManagers=== (DontDestroyOnLoad).
    /// </summary>
    public class ChestUIManager : MonoBehaviour
    {
        public static ChestUIManager Instance { get; private set; }

        [Header("UI")]
        [Tooltip("Ссылка на ChestUI. Если не назначена — ищется автоматически.")]
        [SerializeField] private ChestUI _chestUI;

        /// <summary>
        /// Текущий открытый источник инвентаря. null — ничего не открыто.
        /// </summary>
        public IInventorySource CurrentSource { get; private set; }

        public bool IsOpened => CurrentSource != null;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_chestUI == null)
                _chestUI = FindAnyObjectByType<ChestUI>();

            if (_chestUI == null)
                Debug.LogError("[ChestUIManager] ChestUI не найден в сцене!");
        }

        /// <summary>
        /// Открыть инвентарь источника. Если уже открыт другой — закрывает его.
        /// </summary>
        public void Open(IInventorySource source)
        {
            if (source == null || !source.HasInventory())
            {
                Debug.LogWarning("[ChestUIManager] Попытка открыть источник без инвентаря.");
                return;
            }

            if (_chestUI == null)
            {
                Debug.LogError("[ChestUIManager] ChestUI == null! Невозможно открыть.");
                return;
            }

            // Если уже открыт этот же — закрываем (toggle)
            if (CurrentSource == source)
            {
                Close();
                return;
            }

            // Закрываем предыдущий, если есть
            if (CurrentSource != null)
            {
                CloseInternal();
            }

            // Открываем новый
            _chestUI.OpenWith(source.GetInventory(), source);
            CurrentSource = source;
            source.OnInventoryOpened();

        }

        /// <summary>
        /// Закрыть текущий инвентарь.
        /// </summary>
        public void Close()
        {
            if (CurrentSource == null) return;
            CloseInternal();
        }

        /// <summary>
        /// Toggle: если открыт этот источник — закрыть, иначе открыть.
        /// </summary>
        public void Toggle(IInventorySource source)
        {
            if (CurrentSource == source)
                Close();
            else
                Open(source);
        }

        /// <summary>
        /// Принудительно закрыть, даже если CurrentSource рассинхронизирован.
        /// Используется в OnDestroy источников.
        /// </summary>
        public void ForceCloseIfSource(IInventorySource source)
        {
            if (CurrentSource == source)
            {
                CloseInternal();
            }
        }

        private void CloseInternal()
        {
            var source = CurrentSource;

            // 1. Сообщаем источнику, что он закрыт (сбрасываем его _isOpen)
            source?.OnInventoryClosed();

            // 2. Закрываем UI
            _chestUI?.Close();

            // 3. Сбрасываем состояние
            CurrentSource = null;

            // 4. Закрываем панели (Canvas, курсор, PanelMode)
            if (PanelsUIController.Instance != null)
            {
                PanelsUIController.Instance.CloseAllPanels();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}