using System.Collections;
using System.Text;
using Assets.Scripts.Core;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using Assets.Scripts.Player.Data;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("UI")]
        public GameObject interactionUI;
        public TextMeshProUGUI interactionText;

        [Header("References")]
        [SerializeField] private PlayerInputHandler _input;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PanelsUIController _panelsController;
        [SerializeField] private Camera _playerCamera;

        [Header("Raycast Settings")]
        [SerializeField] private LayerMask _interactableLayers;
        [SerializeField] private float _raycastDistance = 5f;

        private PlayerMovementSettings _settings;
        private LayerMask _waterLayers;

        // === Trigger-based target (Двери, лут, сбор - по близости) ===
        private IInteractable _triggerTarget;
        private GameObject _triggerTargetGO;

        // === Raycast-based target (Radial Menu - по прицеливанию) ===
        private IInteractable _raycastTarget;
        private GameObject _raycastTargetGO;

        private Animator _playerAnimator;
        private int _animIDPickup;
        private bool _hasAnimator;

        // Radial Menu hold variables
        private Coroutine _radialMenuHoldCoroutine;
        private const float RADIAL_MENU_HOLD_DURATION = 0.5f;
        private bool _isHoldingForRadialMenu = false;

        private void Start()
        {
            _playerAnimator = _playerController?.PlayerAnimator;
            _hasAnimator = _playerAnimator != null;
            _animIDPickup = Animator.StringToHash("Pickup");
            _settings = _playerController.settings;
            _waterLayers = _settings.WaterLayers;

            if (interactionUI != null)
                interactionUI.SetActive(false);
        }

        private void OnEnable()
        {
            _input.OnInteractTriggered += HandleInteractTriggered;
            _input.OnInteractEnded += HandleInteractEnded;
        }

        private void OnDisable()
        {
            _input.OnInteractTriggered -= HandleInteractTriggered;
            _input.OnInteractEnded -= HandleInteractEnded;

            if (_radialMenuHoldCoroutine != null)
                StopCoroutine(_radialMenuHoldCoroutine);
        }

        private void Update()
        {
            // === Raycast для Radial Menu (каждый кадр) ===
            PerformRadialMenuRaycast();

            // === Обновление UI ===
            UpdateInteractionUI();

            // === Обработка атаки (ЛКМ) для Harvest ===
            if (_triggerTarget != null && _triggerTarget.GetInteractType() == InteractType.Harvest && _input.attack)
            {
                if (_hasAnimator)
                    _playerController.Attack();
                else
                    Invoke(nameof(OnAttackInteractFinished), 0.7f);
            }
        }

        // === Raycast логика ===
        private void PerformRadialMenuRaycast()
        {
            Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, _raycastDistance, _interactableLayers))
            {
                if (hit.collider.TryGetComponent(out IInteractable interactable))
                {
                    if (interactable.GetInteractType() == InteractType.RadialMenu)
                    {
                        _raycastTarget = interactable;
                        _raycastTargetGO = hit.collider.gameObject;
                        return;
                    }
                }
            }

            // Если ничего не нашли или не RadialMenu
            _raycastTarget = null;
            _raycastTargetGO = null;
        }

        // === Обновление UI (собираем все подсказки) ===
        private void UpdateInteractionUI()
        {
            StringBuilder sb = new StringBuilder();
            bool isInventoryOpened = _panelsController?.IsInventoryOpened() == true;

            // Trigger-based подсказки (Дверь, Лут и т.д.)
            if (_triggerTarget != null)
            {
                string triggerText = GetActionText(_triggerTarget.GetInteractType(), _triggerTargetGO);
                if (!string.IsNullOrEmpty(triggerText))
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append(triggerText);
                }
            }

            // Raycast-based подсказки (Radial Menu)
            if (_raycastTarget != null)
            {
                string raycastText = GetActionText(_raycastTarget.GetInteractType(), _raycastTargetGO);
                if (!string.IsNullOrEmpty(raycastText))
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append(raycastText);
                }
            }

            if (interactionUI != null)
            {
                interactionText.text = sb.ToString();
                interactionUI.SetActive(!isInventoryOpened && sb.Length > 0);
            }
        }

        // === Обработка НАЧАЛА нажатия E ===
        private void HandleInteractTriggered()
        {
            // 1. Приоритет: Trigger-based действия (Дверь, Лут и т.д.)
            if (_triggerTarget != null)
            {
                InteractType type = _triggerTarget.GetInteractType();

                if (type == InteractType.Pickup || type == InteractType.Gather ||
                    type == InteractType.Open || type == InteractType.Drink)
                {
                    ExecuteStandardInteraction(_triggerTarget);
                    return; // Выполняем и выходим
                }
            }

            // 2. Если нет trigger-действий, проверяем Raycast для Radial Menu
            if (_raycastTarget != null)
            {
                StartRadialMenuHold();
            }
        }

        // === Обработка ОТПУСКАНИЯ E ===
        private void HandleInteractEnded()
        {
            if (_isHoldingForRadialMenu)
            {
                _isHoldingForRadialMenu = false;

                if (_radialMenuHoldCoroutine != null)
                {
                    StopCoroutine(_radialMenuHoldCoroutine);
                    _radialMenuHoldCoroutine = null;
                }

                UpdateRadialMenuHoldProgress(0f);
            }
        }

        // === Запуск удержания для Radial Menu ===
        private void StartRadialMenuHold()
        {
            if (_isHoldingForRadialMenu || _raycastTarget == null) return;

            _isHoldingForRadialMenu = true;
            _radialMenuHoldCoroutine = StartCoroutine(RadialMenuHoldCoroutine());
        }

        private IEnumerator RadialMenuHoldCoroutine()
        {
            float timer = 0f;

            while (timer < RADIAL_MENU_HOLD_DURATION)
            {
                timer += Time.deltaTime;
                float progress = timer / RADIAL_MENU_HOLD_DURATION;
                UpdateRadialMenuHoldProgress(progress);

                // Проверяем, не потеряли ли цель во время удержания
                if (_raycastTarget == null)
                {
                    _isHoldingForRadialMenu = false;
                    UpdateRadialMenuHoldProgress(0f);
                    yield break;
                }

                yield return null;
            }

            // Удержание завершено
            _isHoldingForRadialMenu = false;
            var target = _raycastTarget;
            _raycastTarget = null;
            _radialMenuHoldCoroutine = null;

            if (target != null)
            {
                OnInteractFinished(target);
            }
        }

        // === Выполнение стандартного взаимодействия ===
        private void ExecuteStandardInteraction(IInteractable target)
        {
            if (_hasAnimator)
            {
                _playerAnimator.SetTrigger(_animIDPickup);
            }
            else
            {
                float delay = (target.GetInteractType() == InteractType.Open) ? 0.5f : 0.7f;
                _pendingInteractionTarget = target;
                Invoke(nameof(OnInteractFinishedNoArg), delay);
            }
        }

        private IInteractable _pendingInteractionTarget;

        public void OnInteractFinishedNoArg()
        {
            if (_pendingInteractionTarget != null)
            {
                OnInteractFinished(_pendingInteractionTarget);
                _pendingInteractionTarget = null;
            }
        }

        // === Финализация взаимодействия ===
        public void OnInteractFinished(IInteractable specificTarget = null)
        {
            IInteractable target = specificTarget ?? _triggerTarget;
            if (target == null) return;

            var context = new InteractContext
            {
                Tool = AttackAnimationType.Fists,
                IsAttack = false,
                PlayerInteraction = this
            };

            target.Interact(context);

            if (target.HasInventory() && _panelsController != null)
            {
                _panelsController.OpenOtherInventory();
            }

            if (target.GetInteractType() == InteractType.RadialMenu && _panelsController != null)
            {

                if (_triggerTargetGO.TryGetComponent(out StructureIdentity identity))
                {
                    _panelsController.OpenRadialMenu(identity.instanceId);
                }

            }

            if (target.ShouldDetachAfterInteract())
            {
                if (_triggerTarget == target)
                    ClearTriggerTarget();
            }
        }

        public void OnAttackInteractFinished()
        {
            if (_triggerTarget?.GetInteractType() != InteractType.Harvest) return;

            AttackAnimationType tool = GetEquippedToolType();
            var context = new InteractContext { Tool = tool, IsAttack = true, PlayerInteraction = this };
            _triggerTarget.Interact(context);

            if (_triggerTarget.ShouldDetachAfterInteract())
                ClearTriggerTarget();
        }

        // === Trigger события (для дверей, лута и т.д.) ===
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out IInteractable interactable))
            {
                // Игнорируем RadialMenu в триггерах — они через raycast
                if (interactable.GetInteractType() == InteractType.RadialMenu)
                    return;

                _triggerTarget = interactable;
                _triggerTargetGO = other.gameObject;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_triggerTargetGO != null && other.gameObject == _triggerTargetGO)
            {
                if (_triggerTarget?.HasInventory() == true && _panelsController != null)
                    _panelsController.CloseAllPanels();

                ClearTriggerTarget();
            }
        }

        private void ClearTriggerTarget()
        {
            _triggerTarget = null;
            _triggerTargetGO = null;
        }

        // === Утилиты ===
        private string GetActionText(InteractType type, GameObject targetGO)
        {
            bool isOpen = false;

            if (type == InteractType.Open && targetGO != null)
            {
                if (targetGO.TryGetComponent(out DoorController doorController))
                {
                    isOpen = doorController.IsVisuallyOpen();
                }
            }

            return type switch
            {
                InteractType.Open => isOpen ? "[E] Закрыть" : "[E] Открыть",
                InteractType.Pickup => "[E] Подобрать",
                InteractType.Gather => "[E] Собрать",
                InteractType.Drink => "[E] Пить",
                InteractType.Harvest => "[ЛКМ] Добывать",
                InteractType.RadialMenu => "Удерживайте [E] для меню",
                _ => "[E] Взаимодействовать"
            };
        }

        private AttackAnimationType GetEquippedToolType()
        {
            var equipment = GetComponent<PlayerEquipment>();
            if (equipment != null && equipment.IsEquipped)
            {
                var item = equipment.GetCurrentItem();
                if (item != null && (item.itemType == ItemType.Tool || item.itemType == ItemType.Weapon))
                {
                    return item.attackAnimation;
                }
            }
            return AttackAnimationType.Fists;
        }

        private void UpdateRadialMenuHoldProgress(float progress)
        {
            // Опционально: визуализация прогресса удержания
        }
    }
}