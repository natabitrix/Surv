using Assets.Scripts.Core;
using Assets.Scripts.InventorySystem;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        [SerializeField] private GameObject _hudRoot;

        [Header("Hotbar")]
        [SerializeField] private HotbarUI _hotbarUI;
        [SerializeField] private GameObject _hotbarRoot;

        [Header("Экраны")]
        [SerializeField] private GameObject _deathScreenRoot; // fallback
        [SerializeField] private GameObject _pauseScreenRoot;

        [Header("Ссылки на менеджеры")]
        [SerializeField] private PauseManager _pauseManager;

        [Header("Прочее")]
        [SerializeField] private bool _autoSubscribe = true;

        private bool _isSubscribed = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (_autoSubscribe) TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Start()
        {
            if (_autoSubscribe && !_isSubscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_isSubscribed) return;
            var survival = PlayerSurvivalSystem.Instance;
            if (survival == null) return;

            survival.OnPlayerDied += HandlePlayerDied;
            survival.OnPlayerRespawned += HandlePlayerRespawned;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed) return;
            var survival = PlayerSurvivalSystem.Instance;
            if (survival != null)
            {
                survival.OnPlayerDied -= HandlePlayerDied;
                survival.OnPlayerRespawned -= HandlePlayerRespawned;
            }
            _isSubscribed = false;
        }

        // ==========================================
        // === ОБРАБОТЧИКИ ===
        // ==========================================

        private void HandlePlayerDied()
        {
            Debug.Log("[UIManager] Игрок умер.");

            // Если пауза открыта — закрываем
            if (_pauseManager != null && _pauseManager.IsPauseOpened())
                _pauseManager.ResumeFromPause();

            SetActiveSafe(_hudRoot, false);
            SetActiveSafe(_hotbarRoot, false);

            if (DeathScreenManager.Instance != null)
                DeathScreenManager.Instance.Show();
            else
                SetActiveSafe(_deathScreenRoot, true);
        }

        private void HandlePlayerRespawned()
        {
            Debug.Log("[UIManager] Игрок возродился.");

            if (DeathScreenManager.Instance != null)
                DeathScreenManager.Instance.Hide();
            else
                SetActiveSafe(_deathScreenRoot, false);

            SetActiveSafe(_hudRoot, true);
            SetActiveSafe(_hotbarRoot, true);

            if (_hotbarUI != null)
                _hotbarUI.RefreshUI();
        }

        // ==========================================
        // === ПУБЛИЧНЫЕ МЕТОДЫ ===
        // ==========================================

        public void SetPauseVisible(bool visible)
        {
            SetActiveSafe(_pauseScreenRoot, visible);
            SetActiveSafe(_hotbarRoot, !visible && !IsDeathScreenVisible());
        }

        public void SetHudVisible(bool visible) => SetActiveSafe(_hudRoot, visible);
        public void SetHotbarVisible(bool visible) => SetActiveSafe(_hotbarRoot, visible);

        public void RefreshAll()
        {
            if (_hotbarUI != null) _hotbarUI.RefreshUI();
        }

        // ==========================================
        // === ВСПОМОГАТЕЛЬНОЕ ===
        // ==========================================

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }

        private bool IsDeathScreenVisible()
        {
            if (DeathScreenManager.Instance != null)
                return DeathScreenManager.Instance.IsDeathScreenOpened();
            return _deathScreenRoot != null && _deathScreenRoot.activeSelf;
        }
    }
}