// Assets/Scripts/Core/InventoryEventsHub.cs
using System;
using UnityEngine;

namespace Assets.Scripts.Core
{
    public enum InventoryChangeKind
    {
        None = 0,
        MainInventory,      // изменился mainInventoryData
        HotbarInventory,    // изменился hotbarInventoryData
        Engrams,            // изменились энграммы (изучение, очки)
        Equipment,          // экипировка/снятие
        BuildMode,          // вход/выход из build mode
        Stats,              // уровень/опыт/статы
        Survival,           // health/stamina/food/water
        All                 // всё сразу (респавн/загрузка)
    }

    /// <summary>
    /// Глобальный хаб событий инвентаря/прогресса/экипировки.
    /// Живёт на [GameAssets] (DontDestroyOnLoad), один на клиента.
    /// UI подписывается только на него, а не на каждый InventoryData/PlayerProgress.
    /// </summary>
    public class InventoryEventsHub : MonoBehaviour
    {
        public static InventoryEventsHub Instance { get; private set; }

        public event Action<InventoryChangeKind> OnChanged;

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

        public void Notify(InventoryChangeKind kind)
        {
            if (kind == InventoryChangeKind.None) return;
            Debug.Log($"[InventoryEventsHub] Notify: {kind}");  // ВРЕМЕННО
            OnChanged?.Invoke(kind);
        }
    }
}