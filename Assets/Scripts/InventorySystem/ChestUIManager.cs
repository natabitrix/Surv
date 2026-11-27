// Assets/Scripts/InventorySystem/ChestUIManager.cs
using Assets.Scripts.Core;
using Assets.Scripts.Interactables;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.InventorySystem
{
    /// <summary>
    /// Единая точка входа для открытия/закрытия инвентарей (сундуки, трупы, существа).
    /// Владеет состоянием "какой инвентарь сейчас открыт".
    /// Устраняет рассинхрон _currentChest / _isOpen между ChestUI, Corpse, BaseLivingEntity.
    /// Живёт на 🕹️GameManagers (сценовый, один на клиента).
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
        // public IInventorySource CurrentSource { get; private set; }

        // public bool IsOpened => CurrentSource != null;

        /// <summary>
        /// Текущий открытый источник инвентаря. null — ничего не открыто.
        /// ИНВАРИАНТ (Пункт 11): это состояние ЛОКАЛЬНОГО игрока ("что открыто у меня").
        /// Один экземпляр менеджера на процесс = один клиент, поэтому реестр по игрокам не нужен.
        /// </summary>
        public IInventorySource CurrentSource { get; private set; }

        /// <summary>
        /// ПРАВКА (Пункт 11a): учитываем Unity-уничтоженный источник.
        /// Было: CurrentSource != null — после Destroy сундука флаг залипал в true,
        /// т.к. интерфейс сравнивается через object.== (reference), а не UnityEngine.Object.==.
        /// </summary>
        public bool IsOpened => IsSourceAlive(CurrentSource); //удалить: CurrentSource != null;


        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_chestUI == null)
                _chestUI = FindAnyObjectByType<ChestUI>();

            // if (_chestUI == null)
            //     Debug.LogError("[ChestUIManager] ChestUI не найден в сцене!");
        }

        /// <summary>
        /// Открыть инвентарь источника для ЛОКАЛЬНОГО игрока.
        /// Если уже открыт другой — закрывает его. Toggle — отдельный метод Toggle().
        /// </summary>
        public void Open(IInventorySource source)
        {
            // ДОБАВЛЕНО (Пункт 11a): только локальный игрок управляет своим окном
            if (!CanLocalPlayerOperate()) return;

            if (!IsSourceAlive(source) || !source.HasInventory()) //удалить: if (source == null || !source.HasInventory())
            {
                Debug.LogWarning("[ChestUIManager] Попытка открыть источник без инвентаря.");
                return;
            }

            if (_chestUI == null)
            {
                Debug.LogError("[ChestUIManager] ChestUI == null! Невозможно открыть.");
                return;
            }

            //удалить: if (CurrentSource == source)
            //удалить: {
            //удалить:     Debug.Log("[ChestUIManager.Open] Если уже открыт этот же — закрываем (toggle)");
            //удалить:     // Close();
            //удалить:     return;
            //удалить: }
            // Мёртвый блок убран: лог обещал toggle, а по факту был просто return.
            // Повторное открытие того же источника — no-op, toggle живёт в Toggle().
            if (IsSameSource(CurrentSource, source)) return;

            // Закрываем предыдущий, если есть
            if (IsSourceAlive(CurrentSource)) //удалить: if (CurrentSource != null)
            {
                CloseInternal();
            }
            else
            {
                CurrentSource = null; // страховка: источник уничтожен без ForceClose
            }

            // Открываем новый
            _chestUI.OpenWith(source.GetInventory(), source);
            CurrentSource = source;
            source.OnInventoryOpened();

            // === НОВОЕ: открыть панель инвентаря в UI ===
            if (PanelsUIController.Instance != null)
            {
                PanelsUIController.Instance.OpenOtherInventory();
            }
        }

        /// <summary>
        /// Закрыть текущий инвентарь ЛОКАЛЬНОГО игрока (Esc, клавиша, дистанция).
        /// НЕ использовать при уничтожении источника — для этого CloseForDestroyedSource().
        /// </summary>
        public void Close()
        {
            // ДОБАВЛЕНО (Пункт 11a): только локальный игрок
            if (!CanLocalPlayerOperate()) return;

            if (CurrentSource == null) return;
            CloseInternal();
        }

        /// <summary>
        /// Toggle: если открыт этот источник — закрыть, иначе открыть.
        /// </summary>
        public void Toggle(IInventorySource source)
        {
            // ПРАВКА (Пункт 11a): ReferenceEquals вместо == (интерфейс → object.==)
            if (IsSameSource(CurrentSource, source) && IsSourceAlive(source))
                Close();
            else
                Open(source);
        }

        /// <summary>
        /// Принудительно закрыть, даже если CurrentSource рассинхронизирован.
        /// Используется в OnDestroy источников.
        /// ПРАВКА (Пункт 11a): переименовано по смыслу — см. CloseForDestroyedSource.
        /// Оставлено как обёртка для совместимости со старыми вызовами.
        /// </summary>
        public void ForceCloseIfSource(IInventorySource source)
        {
            CloseForDestroyedSource(source);
        }

        /// <summary>
        /// ДОБАВЛЕНО (Пункт 11a): источник исчез из мира (OnDestroy/Die/ForceDespawn).
        /// Закрываем окно у ВСЕХ, у кого он открыт — в одном процессе это LocalPlayer.
        /// ОТЛИЧИЕ от Close(): здесь НЕ проверяем CanLocalPlayerOperate(),
        /// т.к. объект мира умирает независимо от того, кто им владеет,
        /// и окно обязано закрыться, даже если LocalPlayer временно null (смерть/респавн).
        /// </summary>
        public void CloseForDestroyedSource(IInventorySource source)
        {
            if (source == null) return;
            if (!IsSameSource(CurrentSource, source)) return;

            CloseInternal();
        }


        private void CloseInternal()
        {
            // ДОБАВЛЕНО (Пункт 11a): сбрасываем состояние ДО вызовов наружу.
            // PanelsUIController.CloseAllPanels() зовёт ChestUIManager.Close() обратно —
            // раньше рекурсию гасил только CurrentSource != null в самом конце.
            var source = CurrentSource;
            CurrentSource = null;

            // 1. Сообщаем источнику, что он закрыт (сбрасываем его _isOpen)
            if (IsSourceAlive(source)) //удалить: source?.OnInventoryClosed();
                source.OnInventoryClosed();

            // 2. Закрываем UI
            _chestUI?.Close();

            //удалить: CurrentSource = null;   // перенесено наверх

            // 4. Закрываем панели (Canvas, курсор, PanelMode)
            if (PanelsUIController.Instance != null)
            {
                PanelsUIController.Instance.CloseAllPanels();
            }
        }

        /// <summary>
        /// ДОБАВЛЕНО (Пункт 11a): жив ли источник с точки зрения Unity.
        /// IInventorySource — интерфейс, поэтому == разрешается в object.== (reference equality)
        /// и НЕ ловит уничтоженные MonoBehaviour. Приводим к UnityEngine.Object явно.
        /// </summary>
        private static bool IsSourceAlive(IInventorySource source)
        {
            if (source == null) return false;
            var asUnityObject = source as Object;
            return asUnityObject != null; // перегруженный == : destroyed → false
        }

        /// <summary>
        /// ДОБАВЛЕНО (Пункт 11a): сравнение двух источников по ссылке (без Unity-null магии).
        /// ReferenceEquals корректен: saveKey у сумок может совпадать ("LootBox"),
        /// сравнивать по ключу нельзя.
        /// </summary>
        private static bool IsSameSource(IInventorySource a, IInventorySource b)
            => ReferenceEquals(a, b);

        /// <summary>
        /// ДОБАВЛЕНО (Пункт 11a): открывать/закрывать своё окно может только локальный игрок.
        /// В синглплеере LocalPlayer один, но публичные Open()/Close() у источников
        /// может дёрнуть любой код (в т.ч. от лица болванчика) — тогда перезапишется моё окно.
        /// </summary>
        private static bool CanLocalPlayerOperate()
        {
            if (PlayerController.LocalPlayer != null) return true;
            Debug.LogWarning("[ChestUIManager] LocalPlayer == null — операция с инвентарём пропущена.");
            return false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

    }
}