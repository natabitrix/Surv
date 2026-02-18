using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;
using Assets.Scripts.Player.Data;
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
        [SerializeField] private PlayerPanelsUIController _panelsController;
        
        private PlayerMovementSettings _settings;
        private LayerMask _waterLayers;

        private IInteractable _currentTarget;
        private GameObject _currentTargetGO;
        private Animator _playerAnimator;

        private int _animIDPickup;
        private bool _hasAnimator;
        private bool _isInWater;

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
            _input.OnInteractTriggered += HandleInteract;
        }

        private void OnDisable()
        {
            _input.OnInteractTriggered -= HandleInteract;
        }

        private void Update()
        {
            // Обновление UI
            if (_currentTarget != null)
            {
                string actionText = GetActionText(_currentTarget.GetInteractType());

                bool isInventoryOpened = _panelsController?.IsInventoryOpened() == true;

                if (interactionUI != null)
                {
                    interactionText.text = actionText;
                    interactionUI.SetActive(!isInventoryOpened);
                }

                // Отдельная обработка атаки (ЛКМ) для Harvest
                if (_currentTarget.GetInteractType() == InteractType.Harvest && _input.attack)
                {
                    if (_hasAnimator)
                    {
                        _playerController.Attack(); // Предполагается, что Attack() запускает анимацию с событием
                    }
                    else
                    {
                        Invoke(nameof(OnAttackInteractFinished), 0.7f);
                    }
                }
            }
            else
            {
                if (interactionUI != null)
                    interactionUI.SetActive(false);
            }
        }

        // Вызывается при нажатии/удержании клавиши Interact (E)
        private void HandleInteract()
        {
            if (_currentTarget == null) return;

            InteractType type = _currentTarget.GetInteractType();

            // Только действия, привязанные к клавише E
            if (
                type == InteractType.Pickup || 
                type == InteractType.Gather || 
                type == InteractType.Open || 
                type == InteractType.Drink
            )
            {
                if (_hasAnimator)
                {
                    _playerAnimator.SetTrigger(_animIDPickup);
                    // Анимация должна содержать Animation Event → вызов OnInteractFinished()
                }
                else
                {
                    float delay = (type == InteractType.Open) ? 0.5f : 0.7f;
                    Invoke(nameof(OnInteractFinished), delay);
                }
            }
            // Harvest НЕ обрабатывается здесь — он на attack!
        }

        // Вызывается из Animation Event или Invoke после анимации подбора/открытия
        public void OnInteractFinished()
        {
            if (_currentTarget == null) return;

            var context = new InteractContext
            {
                Tool = AttackAnimationType.Fists,
                IsAttack = false,
                PlayerInteraction = this
            };

            _currentTarget.Interact(context);

            // Открытие чужого инвентаря (если есть)
            if (_currentTarget.HasInventory() && _panelsController != null)
            {
                _panelsController.OpenOtherInventory();
            }

            if (_currentTarget.ShouldDetachAfterInteract())
            {
                ClearCurrentTarget();
            }
        }

        // Вызывается из Animation Event или Invoke после атаки по Harvest-объекту
        public void OnAttackInteractFinished()
        {
            if (_currentTarget?.GetInteractType() != InteractType.Harvest) return;

            AttackAnimationType tool = GetEquippedToolType();

            var context = new InteractContext
            {
                Tool = tool,
                IsAttack = true,
                PlayerInteraction = this
            };

            _currentTarget.Interact(context);

            if (_currentTarget.ShouldDetachAfterInteract())
            {
                ClearCurrentTarget();
            }
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

        private string GetActionText(InteractType type)
        {
            bool isOpen = false;
            if(type == InteractType.Open && _currentTargetGO != null)
            {
                if (_currentTargetGO.TryGetComponent(out DoorController doorController))
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
                _ => "[E] Взаимодействовать"
            };
        }

        // === Триггеры взаимодействия ===
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out IInteractable interactable))
            {
                _currentTarget = interactable;
                _currentTargetGO = other.gameObject;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_currentTargetGO != null && other.gameObject == _currentTargetGO)
            {
                if (_currentTarget?.HasInventory() == true && _panelsController != null)
                {
                    _panelsController.CloseAllPanels();
                }

                ClearCurrentTarget();

                if (interactionUI != null)
                    interactionUI.SetActive(false);
            }
        }

        private void ClearCurrentTarget()
        {
            _currentTarget = null;
            _currentTargetGO = null;
        }
    }
}