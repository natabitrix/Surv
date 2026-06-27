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
using Assets.Scripts.Creatures;
using Assets.Scripts.Audio;

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
        // public Transform toolAttachPoint;


        [Header("Raycast Settings")]
        [SerializeField] private LayerMask _interactableLayers;
        [SerializeField] private float _playerInteractionRadius = 1f;

        [Header("Contact Settings")]
        [SerializeField] private LayerMask _harvestableLayers;

        [Header("Hold Settings")]
        [SerializeField] private float _holdThreshold = 0.5f; // Время удержания для меню

        [Header("Combat")]
        [SerializeField] private LayerMask _damageLayers; // 2. Маска для существ (отдельно от интерактаблов)
        [SerializeField] private float _equippedItemContactRadius = 2f;

        [Header("Audio")]
        public AudioClip SwingAudioClip;
        [Range(0, 1)] public float SwingAudioVolume = 0.5f;
        public AudioClip TakeDamageAudioClip;
        [Range(0, 1)] public float TakeDamageAudioVolume = 0.5f;
        public AudioClip DeathAudioClip;
        [Range(0, 1)] public float DeathAudioVolume = 0.5f;

        private PlayerMovementSettings _settings;
        private LayerMask _waterLayers;

        // === Цели (теперь поддерживаем несколько) ===
        private List<IInteractable> _allTargets = new List<IInteractable>();
        private GameObject _targetGO;
        private Vector3 _targetHitPosition;
        private Vector3 _targetHitNormal;

        public Vector3 GetTargetHitPosition() => _targetHitPosition;
        public Vector3 GetTargetHitNormal() => _targetHitNormal;

        // === Логика удержания ===
        private bool _isInteractHeld = false;
        private float _interactionHoldTimer = 0f;
        private bool _radialMenuOpenedThisHold = false;

        private Animator _playerAnimator;
        private Transform _playerHead;
        private int _animIDPickup;
        private bool _hasAnimator;
        private IInteractable _pendingInteractionTarget;
        private BaseLivingEntity _hitCreature;
        private Corpse _currentlyDraggingCorpse;

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
            _input.OnTargetInventoryPressed += HandleInteractEnded;
        }

        private void OnDisable()
        {
            _input.OnInteractPressed -= HandleInteractStarted;
            _input.OnInteractTriggered -= HandleInteractHeld;
            _input.OnInteractStopPressed -= HandleInteractEnded;
            _input.OnTargetInventoryPressed -= HandleInteractEnded;

            HandleMenuClosed();
        }

        private void Update()
        {
            PerformInteractionRaycast();
            UpdateInteractionUI();
            HandleHoldLogic();
            // HandleTargetInventory();
            // Закрытие панелей по F или Cancel (Esc)
            // if (_input.targetInventory)
            // {
            //     Debug.Log("_input.targetInventory");
            //     TryClosePanels();
            //     _input.ResetTargetInventory();
            // }
        }

        // === ЛОГИКА УДЕРЖАНИЯ 
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

        // === СТАРТ (Сброс таймера) ===
        private void HandleInteractStarted()
        {
            _isInteractHeld = true;
            _interactionHoldTimer = 0f;
            _radialMenuOpenedThisHold = false;
        }

        // === УДЕРЖАНИЕ (Повтор действия, например еда) ===
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

            // 🆕 ПРОВЕРКА: если тащим тело — отпускаем его, НЕЗАВИСИМО от рейкаста
            if (_currentlyDraggingCorpse != null)
            {
                _currentlyDraggingCorpse.StopDragging(this);
                // Не вызываем ExecuteStandardInteraction — действие уже выполнено
                HandleMenuClosed();
                _interactionHoldTimer = 0f;
                _radialMenuOpenedThisHold = false;
                return; // ✅ Выходим, чтобы не сработала логика рейкаста
            }

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


        // === ПОИСК ВСЕХ ЦЕЛЕЙ
        // === 2. ПОИСК ВСЕХ ЦЕЛЕЙ (Raycast из головы игрока) ===
        private void PerformInteractionRaycast()
        {
            // Сбрасываем цели в начале каждого кадра
            _allTargets.Clear();
            _targetGO = null;
            _hitCreature = null;

            // === Расчёт динамической дистанции взаимодействия ===
            // Чем выше смотрит игрок, тем дальше радиус (для взаимодействия с объектами на уровне глаз)
            float baseRadius = _playerInteractionRadius;
            float maxDistance = 2.0f;
            float angleMultiplier = 0.02f;

            Vector3 headPos = _playerHead.position;
            Vector3 headDir = _playerHead.forward;

            // Угол наклона головы по вертикали
            float pitchAngle = Mathf.Asin(headDir.y) * Mathf.Rad2Deg;
            float absAngle = Mathf.Abs(pitchAngle);

            // Финальная дистанция с ограничением по максимуму
            float currentInteractionDistance = baseRadius + (absAngle * angleMultiplier);
            currentInteractionDistance = Mathf.Min(currentInteractionDistance, maxDistance);

            Ray ray = new Ray(headPos, headDir);
            Debug.DrawRay(headPos, headDir * currentInteractionDistance, Color.red); // Визуализация для отладки

            // === 1. Raycast для существ (_damageLayers) ===
            // Отдельный луч для боя — не смешиваем с интерактивными объектами
            if (Physics.Raycast(ray, out RaycastHit hitCreature, 2.0f, _damageLayers))
            {
                TryFindCreature(hitCreature.collider); // ✅ Используем хелпер
            }

            // === 2. Raycast для интерактивных объектов (_interactableLayers) ===
            if (Physics.Raycast(ray, out RaycastHit hitGO, currentInteractionDistance, _interactableLayers))
            {
                float distance = (headPos - hitGO.point).magnitude;
                if (distance <= currentInteractionDistance)
                {
                    ProcessHitObject(hitGO.collider.gameObject); // ✅ Используем хелпер
                    return; // Нашли цель по лучу — дальше не ищем
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
                InteractType type2 = target.GetInteractType2();

                // Проверка активности компонента, если он является MonoBehaviour
                // Это скроет надпись и для Corpse, и для RadialMenu, если они disabled
                bool isActive = true;
                if (target is MonoBehaviour mb)
                {
                    isActive = mb.enabled && mb.gameObject.activeInHierarchy;
                }

                // Если компонент не активен, пропускаем его отображение в UI
                if (!isActive) continue;

                // 1. Надпись для действия (Открыть, Подобрать, Тащить тело)
                if (type != InteractType.RadialMenu)
                {
                    string actionText = GetActionText(type, _targetGO);
                    if (!string.IsNullOrEmpty(actionText))
                    {
                        if (sb.Length > 0) sb.AppendLine();
                        sb.Append(actionText);
                    }
                }

                // 2. Надпись для второго действия (Открыть инвентарь)
                if (type2 != InteractType.None && type2 != InteractType.RadialMenu)
                {
                    string actionText2 = GetActionText(type2, _targetGO);
                    if (!string.IsNullOrEmpty(actionText2))
                    {
                        if (sb.Length > 0) sb.AppendLine();
                        sb.Append(actionText2);
                    }
                }

                // 3. Надпись для Меню
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
        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ (убирают дублирование) ===

        /// <summary>
        /// Обрабатывает один попадание: ищет IInteractable и BaseLivingEntity.
        /// Заполняет _allTargets, _targetGO, _hitCreature.
        /// </summary>
        private void ProcessHitObject(GameObject hitObject)
        {
            // === БЛОК 1: Поиск IInteractable (ресурсы, двери, контейнеры) ===

            // 1. Проверяем сам объект
            FindInteractablesOnObject(hitObject);

            // 2. Если не нашли — ищем в родителях (макс. 3 уровня вверх)
            if (_allTargets.Count == 0)
            {
                Transform parent = hitObject.transform.parent;
                int depth = 0;
                while (parent != null && depth < 3)
                {
                    FindInteractablesOnObject(parent.gameObject);
                    if (_allTargets.Count > 0)
                    {
                        _targetGO = parent.gameObject;
                        break; // ✅ Важно: break, а не return!
                    }
                    parent = parent.parent;
                    depth++;
                }
            }

            // 3. Если всё ещё не нашли — ищем в детях (на случай вложенных коллайдеров)
            if (_allTargets.Count == 0)
            {
                var childInteractables = hitObject.GetComponentsInChildren<IInteractable>();
                foreach (var child in childInteractables)
                {
                    if (!_allTargets.Contains(child))
                        _allTargets.Add(child);
                }
                if (_allTargets.Count > 0)
                    _targetGO = hitObject;
            }

            // Фоллбэк: если цели есть, но _targetGO ещё не установлен
            if (_allTargets.Count > 0 && _targetGO == null)
                _targetGO = hitObject;


            // === БЛОК 2: Поиск BaseLivingEntity (враги, животные) ===

            // Ищем только если ещё не нашли существо (приоритет первому попаданию)
            if (_hitCreature == null)
            {
                // Пробуем найти сразу на объекте
                var creature = hitObject.GetComponent<BaseLivingEntity>();

                // Если не нашли — ищем в детях (хитбоксы часто висят отдельно от корня)
                if (creature == null)
                {
                    creature = hitObject.GetComponentInChildren<BaseLivingEntity>();
                }

                // Если не нашли — ищем в родителях (макс. 3 уровня вверх)
                if (creature == null)
                {
                    Transform parent = hitObject.transform.parent;
                    int depth = 0;
                    while (parent != null && depth < 3)
                    {
                        creature = parent.gameObject.GetComponent<BaseLivingEntity>();
                        if (creature != null)
                        {
                            break;
                        }
                        parent = parent.parent;
                        depth++;
                    }
                }

                // Сохраняем, если нашли
                if (creature != null)
                {
                    _hitCreature = creature;
                    // Debug.Log($"[ProcessHit] Найден враг: {creature.name}");
                }

            }
        }

        /// <summary>
        /// Быстрая проверка только на BaseLivingEntity (для raycast-атаки).
        /// Используется в PerformInteractionRaycast для оптимизации.
        /// </summary>
        private void TryFindCreature(Collider collider)
        {
            if (_hitCreature != null) return; // Уже есть цель

            var creature = collider.GetComponent<BaseLivingEntity>() ??
                          collider.GetComponentInChildren<BaseLivingEntity>();

            if (creature != null)
                _hitCreature = creature;

            // Debug.Log("_hitCreature: " + _hitCreature);
        }

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

        /// <summary>
        /// Запускает процесс взаимодействия с целью. 
        /// Если есть аниматор — проигрывает анимацию. 
        /// Если нет — устанавливает задержку (0.5с для Open, 0.7с для остальных) 
        /// и планирует завершение через Invoke.
        /// </summary>
        private void ExecuteStandardInteraction(IInteractable target)
        {
            if (_hasAnimator)
            {
                _playerAnimator.SetTrigger(_animIDPickup);
            }
            else
            {
                float delay = 0.5f;
                _pendingInteractionTarget = target;
                Invoke(nameof(OnInteractFinishedNoArg), delay);
            }
        }

        private void HandleTargetInventory(IInteractable target)
        {
            if (_hasAnimator)
            {
                _playerAnimator.SetTrigger(_animIDPickup);
            }
            else
            {
                float delay = 0.5f;
                _pendingInteractionTarget = target;
                Invoke(nameof(OnTargetInventoryNoArg), delay);
            }
        }

        /// <summary>
        /// Обертка для Invoke (так как Invoke не поддерживает параметры). 
        /// Вызывает основной метод завершения с сохраненной целью и очищает кэш.
        /// </summary>
        public void OnInteractFinishedNoArg()
        {
            if (_pendingInteractionTarget != null)
            {
                OnInteractFinished(_pendingInteractionTarget);
                _pendingInteractionTarget = null;
            }
        }
        public void OnTargetInventoryNoArg()
        {
            if (_pendingInteractionTarget != null)
            {
                OnTargetInventory(_pendingInteractionTarget);
                _pendingInteractionTarget = null;
            }
        }

        /// <summary>
        /// Финальное выполнение взаимодействия: создает контекст, вызывает Interact у цели, 
        /// открывает инвентарь (если есть) и удаляет цель из триггера (если нужно от detachment).
        /// Может быть вызван напрямую с конкретной целью или без параметров (берет первую из списка).
        /// </summary>
        public void OnInteractFinished(IInteractable specificTarget = null)
        {
            IInteractable target = specificTarget ?? (_allTargets.Count > 0 ? _allTargets[0] : null);
            if (target == null) return;

            bool isTargetInventory = target.GetInteractType() == InteractType.OpenTargetInventory;

            var context = new InteractContext
            {
                Tool = AttackAnimationType.Fists,
                IsAttack = false,
                isTargetInventory = false,
                // isTargetInventory = isTargetInventory,
                PlayerInteraction = this
            };

            target.Interact(context);

            if (isTargetInventory && target.HasInventory() && _panelsController != null)
                _panelsController.OpenOtherInventory();

            if (target.ShouldDetachAfterInteract())
                ClearTriggerTarget();
        }

        public void OnTargetInventory(IInteractable specificTarget = null)
        {
            IInteractable target = specificTarget ?? (_allTargets.Count > 0 ? _allTargets[0] : null);
            if (target == null) return;

            bool isTargetInventory = target.GetInteractType() == InteractType.OpenTargetInventory;

            var context = new InteractContext
            {
                Tool = AttackAnimationType.Fists,
                IsAttack = false,
                isTargetInventory = false,
                // isTargetInventory = isTargetInventory,
                PlayerInteraction = this
            };

            target.Interact(context);

            if (isTargetInventory && target.HasInventory() && _panelsController != null)
                _panelsController.OpenOtherInventory();

            if (target.ShouldDetachAfterInteract())
                ClearTriggerTarget();
        }

        private void HandleTargetInventory_()
        {
            // 1. ЛОГИКА ЗАКРЫТИЯ (Если панель уже открыта — закрываем её)
            if (_panelsController.IsInventoryOpened())
            {
                _panelsController.CloseAllPanels();

                // Синхронизируем состояние объектов (сундуки/трупы)
                foreach (var target in _allTargets)
                {
                    if (target is ChestController chest) chest.Close();
                    if (target is Corpse corpse) corpse.CloseInventory();
                }
                return;
            }

            // 2. Если открыто другое меню (пауза, радиальное) — игнорируем F
            if (_panelsController.IsPanelOpened()) return;

            // 3. Ищем цель, которая реагирует на F
            foreach (var target in _allTargets)
            {
                bool isFTarget = target.GetInteractType() == InteractType.OpenTargetInventory ||
                                 target.GetInteractType2() == InteractType.OpenTargetInventory;

                if (target.HasInventory() && isFTarget)
                {
                    // Создаем контекст с явным указанием "открыть инвентарь"
                    var context = new InteractContext
                    {
                        Tool = AttackAnimationType.Fists,
                        IsAttack = false,
                        isTargetInventory = true, // Критически важно для Corpse!
                        PlayerInteraction = this
                    };

                    // Вызываем взаимодействие напрямую
                    target.Interact(context);

                    // Открываем UI панель
                    if (_panelsController != null)
                        _panelsController.OpenOtherInventory();

                    return; // Обрабатываем только первую подходящую цель
                }
            }
        }

        /// <summary>
        /// Вызывается в конце анимации атаки (через Animation Event).
        /// Запускает проверку попадания в зависимости от экипировки.
        /// </summary>
        public void OnAttackInteractFinished()
        {
            Item equippedTool = GetEquippedTool();
            AttackAnimationType weaponType = equippedTool?.attackAnimation ?? AttackAnimationType.Fists;

            bool hitSomething = false;
            Vector3? hitPosition = null;
            ImpactType? hitImpactType = null;

            // === Проверка попаданий ===
            if (equippedTool != null)
            {
                // Оружие/инструмент: проверяем пересечение модели
                GetEquippedItemContact(); // заполняет _allTargets
            }
            else
            {
                // Кулаки: проверяем сферу вокруг кистей
                GetHandContact(); //  заполняет _allTargets
            }

            // === Собираем информацию для звуков из найденных целей ===
            if (_allTargets.Count > 0)
            {
                foreach (var target in _allTargets)
                {
                    var go = target is MonoBehaviour mb ? mb.gameObject : null;
                    if (go != null)
                    {
                        var impactProvider = go.GetComponent<IImpactSoundProvider>()
                            ?? go.GetComponentInChildren<IImpactSoundProvider>();

                        if (impactProvider != null)
                        {
                            hitImpactType = impactProvider.GetImpactType();
                            hitPosition = go.transform.position;
                            hitSomething = true;
                            break; // Берём первую цель со звуком
                        }
                    }
                }
            }

            // === Нанесение урона существам ===
            if (_hitCreature != null)
            {
                float damage = _playerController.GetAttackDamage();
                // Debug.Log($"[PlayerInteraction] Атака по {_hitCreature.gameObject.name}, урон: {damage}");
                _hitCreature.TakeDamage(damage, this);

                // Получаем тип воздействия от существа
                if (_hitCreature is IImpactSoundProvider provider)
                {
                    hitImpactType = provider.GetImpactType();
                }
                hitPosition ??= _hitCreature.transform.position;
                hitSomething = true;
                _hitCreature = null;
            }


            //  === ПРОВЕРКА: если тащим тело и нажали атаку — бросаем его ===
            if (_currentlyDraggingCorpse != null)
            {
                _currentlyDraggingCorpse.InterruptByAttack(this);
                CombatAudioManager.Instance?.PlayMissSound(AttackAnimationType.Fists, transform.position);
                // Не return! Пусть дальше сработает логика атаки по другим целям
            }

            // === Воспроизведение звука удара ===
            if (hitSomething && hitPosition.HasValue)
            {
                var impactType = hitImpactType ?? ImpactType.Air;
                CombatAudioManager.Instance?.PlayHitSound(weaponType, impactType, hitPosition.Value);
            }
            else
            {
                // Промах — звук воздуха
                CombatAudioManager.Instance?.PlayMissSound(weaponType, transform.position);
            }

            // === Обработка ресурсов (Harvest) ===
            foreach (var target in _allTargets)
            {
                // Debug.Log("target.GetInteractType(): " + target.GetInteractType());
                if (target.GetInteractType() == InteractType.Harvest || target is Corpse)
                {
                    var context = new InteractContext
                    {
                        Tool = weaponType,
                        IsAttack = true,
                        PlayerInteraction = this
                    };
                    target.Interact(context);

                    if (target.ShouldDetachAfterInteract())
                        ClearTriggerTarget();

                    // Прерываем после первого Harvest — обычно атакуют один ресурс за раз
                    return;
                }
            }
        }
        // === Вспомогательные методы (добавить в класс) ===

        /// <summary>
        /// Проверяет пересечение модели экипированного предмета с миром.
        /// Используется для добычи ресурсов (Harvest) и melee-атаки оружием.
        /// </summary>

        private void GetEquippedItemContact()
        {
            Item equippedItem = GetEquippedTool();
            if (equippedItem == null) return;

            var equipment = GetComponent<PlayerEquipment>();
            if (equipment == null || equipment.toolAttachPoint == null) return;

            Transform toolParent = equipment.toolAttachPoint;
            if (toolParent.childCount == 0) return;
            Transform itemModel = toolParent.GetChild(0);

            var meshFilter = itemModel.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            Bounds localBounds = meshFilter.sharedMesh.bounds;

            // 🔹 1. Мировой центр коробки = центр геометрии меша, а не пивот/рука
            Vector3 boxCenterWorld = itemModel.TransformPoint(localBounds.center);

            // 🔹 2. Стабильные полуразмеры в мировых единицах
            Vector3 boxExtents = Vector3.Scale(localBounds.extents, itemModel.lossyScale) * _equippedItemContactRadius;

            itemModel.transform.GetPositionAndRotation(out Vector3 placementPosition, out Quaternion placementRotation);

            // 🔹 3. Detекция теперь точно совпадает с визуалом лезвия
            Collider[] hits = Physics.OverlapBox(
                boxCenterWorld,      // ← смещённый центр
                boxExtents,          // ← стабильные полуразмеры
                placementRotation,
                _damageLayers | _harvestableLayers
            );

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Collider>(out var targetCollider))
                    _targetHitPosition = targetCollider.ClosestPoint(boxCenterWorld);
                else
                    _targetHitPosition = placementPosition; // или boxCenterWorld

                ProcessHitObject(hit.gameObject);
            }

            _targetHitNormal = _playerController.transform.forward;
        }

#if UNITY_EDITOR
        // private void OnDrawGizmos()
        // {
        //     if (!Application.isPlaying) return;

        //     Item equippedItem = GetEquippedTool();
        //     if (equippedItem == null) return;

        //     var equipment = GetComponent<PlayerEquipment>();
        //     if (equipment == null || equipment.toolAttachPoint == null) return;

        //     Transform toolParent = equipment.toolAttachPoint;
        //     if (toolParent.childCount == 0) return;
        //     Transform itemModel = toolParent.GetChild(0);

        //     var meshFilter = itemModel.GetComponent<MeshFilter>();
        //     if (meshFilter == null || meshFilter.sharedMesh == null) return;

        //     Bounds localBounds = meshFilter.sharedMesh.bounds;
        //     Vector3 boxCenterWorld = itemModel.TransformPoint(localBounds.center);
        //     Vector3 boxExtents = Vector3.Scale(localBounds.extents, itemModel.lossyScale) * _equippedItemContactRadius;

        //     itemModel.transform.GetPositionAndRotation(out Vector3 _, out Quaternion rot);

        //     // 🔹 Рисуем короб ровно там, где работает OverlapBox
        //     Matrix4x4 oldMatrix = Gizmos.matrix;
        //     Gizmos.matrix = Matrix4x4.TRS(boxCenterWorld, rot, Vector3.one);

        //     Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
        //     Gizmos.DrawCube(Vector3.zero, boxExtents * 2f);

        //     Gizmos.color = Color.white;
        //     Gizmos.DrawWireCube(Vector3.zero, boxExtents * 2f);
        //     Gizmos.matrix = oldMatrix;

        //     // 🔹 Визуально покажем разницу: пивот (жёлтый) vs центр удара (голубой)
        //     Gizmos.color = Color.yellow;
        //     Gizmos.DrawSphere(itemModel.position, 0.05f); // пивот/рука

        //     Gizmos.color = Color.cyan;
        //     Gizmos.DrawSphere(boxCenterWorld, 0.07f); // центр лезвия
        //     Gizmos.DrawLine(itemModel.position, boxCenterWorld);
        // }
#endif




        /// <summary>
        /// Проверяет пересечение сферы вокруг кисти с миром.
        /// Используется для melee-атаки кулаками.
        /// </summary>
        private void GetHandContact()
        {
            Transform rightHand = _playerAnimator?.GetBoneTransform(HumanBodyBones.RightHand);
            Transform leftHand = _playerAnimator?.GetBoneTransform(HumanBodyBones.LeftHand);

            if (rightHand == null && leftHand == null) return;

            float hitRadius = 0.3f;
            int layerMask = _damageLayers | _harvestableLayers | _interactableLayers;

            HashSet<Collider> processedHits = new HashSet<Collider>();

            if (rightHand != null)
            {
                Collider[] hits = Physics.OverlapSphere(rightHand.position, hitRadius, layerMask);
                foreach (var hit in hits)
                {
                    if (processedHits.Add(hit))
                    {
                        ProcessHitObject(hit.gameObject); // ← ВАЖНО: заполняет _allTargets и _hitCreature
                    }
                }
            }

            if (leftHand != null)
            {
                Collider[] hits = Physics.OverlapSphere(leftHand.position, hitRadius, layerMask);
                foreach (var hit in hits)
                {
                    if (processedHits.Add(hit))
                    {
                        ProcessHitObject(hit.gameObject); // ← ВАЖНО: заполняет _allTargets и _hitCreature
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            FindInteractablesOnObject(other.gameObject);
            if (_allTargets.Count > 0)
            {
                _targetGO = other.gameObject;
            }
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
            // 🆕 Проверка тела (луч попадёт в кость, но Corpse висит на корне)
            if (targetGO != null)
            {
                var corpse = targetGO.GetComponent<Corpse>() ?? targetGO.GetComponentInParent<Corpse>();
                if (corpse != null && corpse.enabled)
                {
                    return corpse.IsDragging ? "[E] Отпустить тело" : "[E] Тащить тело";
                }
            }

            if (type == InteractType.Interact && targetGO != null)
            {
                if (targetGO.TryGetComponent(out DoorController doorController))
                {
                    return doorController.IsVisuallyOpen() ? "[E] Закрыть" : "[E] Открыть";
                }

            }

            return type switch
            {
                InteractType.None => "", // Явная обработка None
                InteractType.OpenTargetInventory => "[F] Открыть",
                InteractType.Interact => "[E] Использовать",
                InteractType.Pickup => "[E] Подобрать",
                InteractType.Gather => "[E] Собрать",
                InteractType.Drink => "[E] Пить",
                InteractType.Harvest => "[ЛКМ] Добывать",
                InteractType.RadialMenu => "", // Скрыто, обрабатывается отдельно
                _ => "[E] Взаимодействовать"
            };
        }


        private void PlaySound(AudioClip audioClip, float audioClipVolume)
        {
            if (audioClip != null)
            {
                // Получаем глобальную громкость
                float globalVolume = 1f;
                if (AudioManager.Instance != null)
                {
                    globalVolume = AudioManager.Instance.masterVolume;
                }

                // Итоговая громкость = Глобальная * Настройка существа
                float finalVolume = globalVolume * audioClipVolume;

                // Создаем источник вручную с правильной громкостью
                GameObject soundObj = new GameObject($"{audioClip.name}");
                soundObj.transform.position = transform.position;

                AudioSource source = soundObj.AddComponent<AudioSource>();
                source.clip = audioClip;
                source.volume = finalVolume;
                source.spatialBlend = 1f;
                source.Play();

                Destroy(soundObj, audioClip.length + 0.1f);
            }
        }

        private Item GetEquippedTool()
        {
            var equipment = GetComponent<PlayerEquipment>();
            if (equipment != null && equipment.IsEquipped)
            {
                var item = equipment.GetCurrentItem();
                if (item != null && (item.itemType == ItemType.Tool || item.itemType == ItemType.Weapon))
                    return item;
            }
            return null;
        }

        private AttackAnimationType GetEquippedToolType()
        {
            var item = GetEquippedTool();

            if (item != null)
                return item.attackAnimation;

            return AttackAnimationType.Fists;
        }

        // === Методы для управления перетаскиванием тела ===
        public void RegisterDraggingCorpse(Corpse corpse)
        {
            _currentlyDraggingCorpse = corpse;
        }

        public void UnregisterDraggingCorpse(Corpse corpse)
        {
            if (_currentlyDraggingCorpse == corpse)
                _currentlyDraggingCorpse = null;
        }

        // Проверка: тащим ли мы сейчас тело?
        public bool IsDraggingCorpse() => _currentlyDraggingCorpse != null;

        public void TryClosePanels()
        {
            if (_panelsController != null && _panelsController.IsPanelOpened())
            {
                Debug.Log("PanelOpened");
                // Если открыт инвентарь цели или радиальное меню - закрываем
                if (_panelsController.IsInventoryOpened() || _panelsController.IsRadialMenuOpened())
                {
                    _panelsController.CloseAllPanels();
                }
            }
        }
    }
}