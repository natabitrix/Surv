// Assets/Scripts/Player/PlayerInteraction.cs
using System.Text;
using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using Assets.Scripts.Player.Data;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using Assets.Scripts.Utils;
using Unity.VisualScripting;

namespace Assets.Scripts.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("UI")]
        public GameObject interactionUI;
        public GameObject aim;
        public TextMeshProUGUI interactionText;

        [Header("References")]
        [SerializeField] private PlayerInputHandler _input;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PanelsUIController _panelsController;
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private Transform _playerInteractPoint;

        [Header("Raycast Settings")]
        [SerializeField] private LayerMask _interactableLayers;
        [SerializeField] private float _playerInteractionRadius = 1f;

        [Header("Hold Settings")]
        [SerializeField] private float _holdThreshold = 0.5f; // Время удержания для меню

        private PlayerMovementSettings _settings;
        private LayerMask _waterLayers;

        // === Цели (теперь поддерживаем несколько) ===
        private List<IInteractable> _allTargets = new List<IInteractable>();
        private GameObject _targetGO;

        // === Логика удержания ===
        private bool _isInteractHeld = false;
        private float _interactionHoldTimer = 0f;
        private bool _radialMenuOpenedThisHold = false;

        private Animator _playerAnimator;
        private Transform _playerHead;
        private int _animIDPickup;
        private bool _hasAnimator;
        private IInteractable _pendingInteractionTarget;

        private void Start()
        {
            _playerAnimator = _playerController?.PlayerAnimator;
            _playerHead = _playerController?.Head;
            _hasAnimator = _playerAnimator != null;
            _animIDPickup = Animator.StringToHash("Pickup");
            _settings = _playerController.settings;
            _waterLayers = _settings.WaterLayers;

            if (interactionUI != null)
                interactionUI.SetActive(false);
        }

        private void OnEnable()
        {
            _input.OnInteractPressed += HandleInteractStarted;    // Старт таймера (1 раз)
            _input.OnInteractTriggered += HandleInteractHeld;     // Повтор действия (еда)
            _input.OnInteractStopPressed += HandleInteractEnded;  // Финал (меню или действие)
        }

        private void OnDisable()
        {
            _input.OnInteractPressed -= HandleInteractStarted;
            _input.OnInteractTriggered -= HandleInteractHeld;
            _input.OnInteractStopPressed -= HandleInteractEnded;
            HandleMenuClosed();
        }

        private void Update()
        {
            PerformInteractionRaycast();
            UpdateInteractionUI();
            HandleHoldLogic();

            // Обработка атаки (ЛКМ) для Harvest
            if (_targetGO != null && _input.attack)
            {
                // Ищем Harvest среди всех целей
                foreach (var target in _allTargets)
                {
                    if (target.GetInteractType() == InteractType.Harvest)
                    {
                        if (_hasAnimator)
                            _playerController.Attack();
                        else
                            Invoke(nameof(OnAttackInteractFinished), 0.7f);
                        break;
                    }
                }
            }
        }

        // === 1. ЛОГИКА УДЕРЖАНИЯ (Решение Проблемы 1) ===
        private void HandleHoldLogic()
        {
            if (_isInteractHeld)
            {
                _interactionHoldTimer += Time.deltaTime;

                // Если держим дольше порога и меню еще не открыто
                if (_interactionHoldTimer >= _holdThreshold && !_radialMenuOpenedThisHold)
                {
                    OpenRadialMenuIfAvailable();
                    _radialMenuOpenedThisHold = true;
                }
            }
        }


        // === 1. СТАРТ (Сброс таймера) ===
        private void HandleInteractStarted()
        {
            _isInteractHeld = true;
            _interactionHoldTimer = 0f;
            _radialMenuOpenedThisHold = false;
        }

        // === 2. УДЕРЖАНИЕ (Повтор действия, например еда) ===
        private void HandleInteractHeld()
        {
            // // Выполняем действие только если меню еще НЕ открылось
            // if (!_radialMenuOpenedThisHold && _triggerTarget != null)
            // {
            // }
        }

        private void HandleInteractEnded()
        {

            _isInteractHeld = false;

            // Если меню НЕ открылось за время удержания -> выполняем обычное действие
            if (!_radialMenuOpenedThisHold)
            {
                ExecuteStandardInteractionIfAvailable();
            }

            // Закрываем меню при отпускании
            HandleMenuClosed();

            _interactionHoldTimer = 0f;
            _radialMenuOpenedThisHold = false;
        }

        private void OpenRadialMenuIfAvailable()
        {
            // Ищем цель с типом RadialMenu среди всех найденных
            foreach (var target in _allTargets)
            {
                if (target.GetInteractType() == InteractType.RadialMenu)
                {
                    HandleMenuOpened(_targetGO);
                    return;
                }
            }
        }

        private void ExecuteStandardInteractionIfAvailable()
        {
            // Ищем первую цель НЕ меню и выполняем
            foreach (var target in _allTargets)
            {
                if (target.GetInteractType() != InteractType.RadialMenu)
                {
                    ExecuteStandardInteraction(target);
                    return;
                }
            }
        }

        // === 2. ПОИСК ВСЕХ ЦЕЛЕЙ (Решение Проблемы 2) ===
        private void PerformInteractionRaycast()
        {
            _allTargets.Clear();
            _targetGO = null;

            float baseRadius = _playerInteractionRadius;
            float maxDistance = 2.0f;
            float angleMultiplier = 0.02f;

            Vector3 headPos = _playerHead.position;
            Vector3 headDir = _playerHead.forward;

            float pitchAngle = Mathf.Asin(headDir.y) * Mathf.Rad2Deg;
            float absAngle = Mathf.Abs(pitchAngle);

            float currentInteractionDistance = baseRadius + (absAngle * angleMultiplier);
            currentInteractionDistance = Mathf.Min(currentInteractionDistance, maxDistance);

            Ray ray = new Ray(headPos, headDir);

            if (Physics.Raycast(ray, out RaycastHit hit, currentInteractionDistance, _interactableLayers))
            {
                float distance = (headPos - hit.point).magnitude;
                if (distance <= currentInteractionDistance)
                {
                    // 1. Ищем на самом коллайдере
                    FindInteractablesOnObject(hit.collider.gameObject);

                    // 2. Если не нашли, ищем в родителях
                    if (_allTargets.Count == 0)
                    {
                        Transform parent = hit.collider.transform.parent;
                        int depth = 0;
                        while (parent != null && depth < 3)
                        {
                            FindInteractablesOnObject(parent.gameObject);
                            if (_allTargets.Count > 0)
                            {
                                _targetGO = parent.gameObject;
                                return;
                            }
                            parent = parent.parent;
                            depth++;
                        }
                    }

                    // 3. Если не нашли, ищем в детях
                    if (_allTargets.Count == 0)
                    {
                        var childInteractables = hit.collider.gameObject.GetComponentsInChildren<IInteractable>();
                        foreach (var child in childInteractables)
                        {
                            if (!_allTargets.Contains(child))
                                _allTargets.Add(child);
                        }
                        if (_allTargets.Count > 0)
                            _targetGO = hit.collider.gameObject;
                    }

                    if (_allTargets.Count > 0 && _targetGO == null)
                        _targetGO = hit.collider.gameObject;

                    return;
                }
            }
        }

        // Поиск всех компонентов IInteractable на объекте
        private void FindInteractablesOnObject(GameObject obj)
        {
            var interactables = obj.GetComponents<IInteractable>();
            foreach (var interactable in interactables)
            {
                if (!_allTargets.Contains(interactable))
                    _allTargets.Add(interactable);
            }
        }

        // === 3. ОБНОВЛЕНИЕ UI (Две надписи) ===
        private void UpdateInteractionUI()
        {
            StringBuilder sb = new StringBuilder();
            bool isInventoryOpened = _panelsController?.IsInventoryOpened() == true;
            bool isMenuAlreadyOpen = _panelsController?.IsRadialMenuOpened() == true;

            foreach (var target in _allTargets)
            {
                InteractType type = target.GetInteractType();

                // 1. Надпись для действия (Открыть, Подобрать)
                if (type != InteractType.RadialMenu)
                {
                    string actionText = GetActionText(type, _targetGO);
                    if (!string.IsNullOrEmpty(actionText))
                    {
                        if (sb.Length > 0) sb.AppendLine();
                        sb.Append(actionText);
                    }
                }

                // 2. Надпись для Меню
                if (type == InteractType.RadialMenu && !isMenuAlreadyOpen)
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append("Удерживайте [E] для меню");
                }
            }

            if (interactionUI != null)
            {
                interactionText.text = sb.ToString();
                interactionUI.SetActive(!isInventoryOpened && sb.Length > 0);
            }
        }

        // === Остальные методы ===

        private void HandleMenuOpened(GameObject targetGO)
        {
            if (_panelsController != null)
            {
                _panelsController.OpenRadialMenu(targetGO);
            }
        }

        private void HandleMenuClosed()
        {
            if (_panelsController != null)
            {
                _panelsController.CloseRadialMenu();
            }
        }

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

        public void OnInteractFinishedNoArg()
        {
            if (_pendingInteractionTarget != null)
            {
                OnInteractFinished(_pendingInteractionTarget);
                _pendingInteractionTarget = null;
            }
        }

        public void OnInteractFinished(IInteractable specificTarget = null)
        {
            IInteractable target = specificTarget ?? (_allTargets.Count > 0 ? _allTargets[0] : null);
            if (target == null) return;

            var context = new InteractContext
            {
                Tool = AttackAnimationType.Fists,
                IsAttack = false,
                PlayerInteraction = this
            };

            target.Interact(context);

            if (target.HasInventory() && _panelsController != null)
                _panelsController.OpenOtherInventory();

            if (target.ShouldDetachAfterInteract())
            {
                ClearTriggerTarget();
            }
        }

        public void OnAttackInteractFinished()
        {
            foreach (var target in _allTargets)
            {
                if (target.GetInteractType() == InteractType.Harvest)
                {
                    AttackAnimationType tool = GetEquippedToolType();
                    var context = new InteractContext { Tool = tool, IsAttack = true, PlayerInteraction = this };
                    target.Interact(context);

                    if (target.ShouldDetachAfterInteract())
                        ClearTriggerTarget();
                    return;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            FindInteractablesOnObject(other.gameObject);
            if (_allTargets.Count > 0)
                _targetGO = other.gameObject;
        }

        private void OnTriggerExit(Collider other)
        {
            if (_targetGO != null && other.gameObject == _targetGO)
            {
                foreach (var target in _allTargets)
                {
                    if (target?.HasInventory() == true && _panelsController != null)
                        _panelsController.CloseAllPanels();
                }
                ClearTriggerTarget();
            }
        }

        private void ClearTriggerTarget()
        {
            _allTargets.Clear();
            _targetGO = null;
        }

        private string GetActionText(InteractType type, GameObject targetGO)
        {
            bool isOpen = false;
            if (type == InteractType.Open && targetGO != null)
            {
                if (targetGO.TryGetComponent(out DoorController doorController))
                    isOpen = doorController.IsVisuallyOpen();
            }

            return type switch
            {
                InteractType.Open => isOpen ? "[E] Закрыть" : "[E] Открыть",
                InteractType.Pickup => "[E] Подобрать",
                InteractType.Gather => "[E] Собрать",
                InteractType.Drink => "[E] Пить",
                InteractType.Harvest => "[ЛКМ] Добывать",
                InteractType.RadialMenu => "", // Скрыто, обрабатывается отдельно
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
                    return item.attackAnimation;
            }
            return AttackAnimationType.Fists;
        }
    }
}