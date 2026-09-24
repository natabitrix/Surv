// Assets/Scripts/UI/EntityInfoPanelManager.cs
using Assets.Scripts.Creatures;
using UnityEngine;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// Следит за целью под прицелом камеры и показывает/скрывает инфо-панель.
    /// Панель держится ещё N секунд после того, как цель ушла из прицела,
    /// чтобы не мельтешить при малейшем движении.
    /// </summary>
    public class EntityInfoPanelManager : MonoBehaviour
    {
        public static EntityInfoPanelManager Instance { get; private set; }

        [Header("References")]
        [Tooltip("Панель, которую показываем/скрываем.")]
        [SerializeField] private EntityInfoPanel _panel;

        [Tooltip("Камера, из центра которой пускаем Raycast. Если null — берётся Camera.main.")]
        [SerializeField] private Camera _camera;

        [Header("Raycast")]
        [Tooltip("Слои, которые считаем целями.")]
        [SerializeField] private LayerMask _targetLayers = ~0;

        [Tooltip("Максимальная дистанция прицеливания.")]
        [SerializeField] private float _maxDistance = 100f;

        [Header("Timing")]
        [Tooltip("Сколько секунд цель должна быть под прицелом, прежде чем показать панель. " +
                 "0 = мгновенно. 0.2 — против мельтешения.")]
        [SerializeField] private float _holdTime = 0.2f;

        [Tooltip("Сколько секунд панель остаётся видимой ПОСЛЕ того, " +
                 "как цель ушла из прицела.")]
        [SerializeField] private float _lingerTime = 2f;

        // === Состояние ===
        private BaseLivingEntity _currentTarget;   // цель, которую сейчас отслеживаем
        private float _targetEnterTime;             // когда цель появилась под прицелом
        private float _lastSeenTime;                // когда цель в последний раз была под прицелом

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (_panel == null) return;

            Camera cam = _camera != null ? _camera : Camera.main;
            if (cam == null) { _panel.Hide(); _currentTarget = null; return; }

            // Ray из центра экрана
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

            BaseLivingEntity found = null;

            if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _targetLayers, QueryTriggerInteraction.Collide))
            {
                found = hit.collider.GetComponentInParent<BaseLivingEntity>();
                if (found != null && !found.IsAlive() && !found.knockedOut)
                    found = null;
            }

            // === Смена цели ===
            if (found != _currentTarget)
            {
                _currentTarget = found;
                _targetEnterTime = Time.time;
            }

            // === Если цель есть — обновляем время последнего «видел» ===
            if (_currentTarget != null)
            {
                _lastSeenTime = Time.time;
            }

            // === Если цели нет — проверяем, сколько уже прошло ===
            if (_currentTarget == null)
            {
                _panel.Hide();
                return;
            }

            // === Задержка перед показом ===
            if (Time.time - _targetEnterTime < _holdTime)
            {
                _panel.Hide();
                return;
            }

            // === Панель живёт, если цель «видели» недавно ===
            float timeSinceLastSeen = Time.time - _lastSeenTime;

            if (timeSinceLastSeen <= _lingerTime)
            {
                _panel.Show(_currentTarget);
            }
            else
            {
                // Истекло — скрываем и забываем цель
                _panel.Hide();
                _currentTarget = null;
            }
        }
    }
}