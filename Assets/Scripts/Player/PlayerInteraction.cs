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

        [Header("Raycast Settings")]
        [SerializeField] private LayerMask _interactableLayers;
        // [SerializeField] private float _playerInteractionRadius = 1f;

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
        private int _animIDOpenInventory;
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
            _animIDOpenInventory = Animator.StringToHash("OpenInventory");
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
            _input.OnTargetInventoryPressed += HandleTargetInventory;
        }

        private void OnDisable()
        {
            _input.OnInteractPressed -= HandleInteractStarted;
            _input.OnInteractTriggered -= HandleInteractHeld;
            _input.OnInteractStopPressed -= HandleInteractEnded;
            _input.OnTargetInventoryPressed -= HandleTargetInventory;

            HandleMenuClosed();
        }

        private void Update()
        {
            PerformInteractionRaycast();
            UpdateInteractionUI();
            HandleHoldLogic();
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

            // ПРОВЕРКА: если тащим тело — отпускаем его, НЕЗАВИСИМО от рейкаста
            if (_currentlyDraggingCorpse != null)
            {
                _currentlyDraggingCorpse.StopDragging(this);

                HandleMenuClosed();
                _interactionHoldTimer = 0f;
                _radialMenuOpenedThisHold = false;
                return; // Выходим, чтобы не сработала логика рейкаста
            }

            // Если меню НЕ открылось за время удержания -> выполняем обычное действие
            if (!_radialMenuOpenedThisHold)
            {
                // Ищем первую цель НЕ меню и выполняем
                foreach (var target in _allTargets)
                {
                    InteractType type = target.GetInteractType();
                    InteractType type2 = target.GetInteractType2();
                    if ((type != InteractType.RadialMenu && type != InteractType.OpenTargetInventory) || type2 == InteractType.Interact)
                    {
                        _playerAnimator.SetTrigger(_animIDPickup);
                        return;
                    }
                }
            }

            // Закрываем меню при отпускании
            HandleMenuClosed();

            _interactionHoldTimer = 0f;
            _radialMenuOpenedThisHold = false;
        }

        private void HandleTargetInventory()
        {
            // Ищем первую цель
            foreach (var target in _allTargets)
            {
                InteractType type = target.GetInteractType();
                InteractType type2 = target.GetInteractType2();
                if (type == InteractType.OpenTargetInventory || type2 == InteractType.OpenTargetInventory)
                {
                    if (_hasAnimator)
                    {
                        _playerAnimator.SetTrigger(_animIDOpenInventory);
                    }
                    else
                    {
                        float delay = 0.5f;
                        _pendingInteractionTarget = target;
                        Invoke(nameof(OnOpenInventoryFinishedNoArg), delay);
                    }
                    return;
                }
            }
        }

        public void OnOpenInventoryFinishedNoArg()
        {
            if (_pendingInteractionTarget != null)
            {
                OnOpenInventoryFinished(_pendingInteractionTarget);
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
                isTargetInventory = false,
                PlayerInteraction = this
            };

            target.Interact(context);

            if (target.ShouldDetachAfterInteract())
                ClearTriggerTarget();
        }

        public void OnOpenInventoryFinished(IInteractable specificTarget = null)
        {

            IInteractable target = specificTarget ?? (_allTargets.Count > 0 ? _allTargets[0] : null);

            if (target == null) return;

            // Если панель уже открыта — закрываем её
            if (target.HasInventory() && _panelsController != null && _panelsController.IsInventoryOpened())
            {
                _panelsController.CloseAllPanels();
                if (target is ChestController chest) chest.Close();
                if (target is Corpse corpse) corpse.CloseInventory();
                return;
            }

            var context = new InteractContext
            {
                Tool = AttackAnimationType.Fists,
                IsAttack = false,
                isTargetInventory = true,
                PlayerInteraction = this
            };

            target.Interact(context);

            if (target.HasInventory() && _panelsController != null)
                _panelsController.OpenOtherInventory();

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

        /// <summary>
        /// Проверяет, находится ли объект в поле зрения камеры и не перекрыт ли он стеной.
        /// </summary>

        private bool IsVisibleByCamera(GameObject target, float maxDistance)
        {
            if (target == null || _playerCamera == null) return false;

            // 1. Проверка дистанции
            float dist = Vector3.Distance(_playerHead.position, target.transform.position);
            if (dist > maxDistance) return false;

            // 2. Проверка поля зрения и позиции относительно камеры
            Vector3 viewportPos = _playerCamera.WorldToViewportPoint(target.transform.position);

            // Если z <= 0, объект находится позади камеры
            if (viewportPos.z <= 0) return false;

            // Проверяем, попадает ли объект в границы экрана
            float edgeMargin = 0.05f;
            if (viewportPos.x < -edgeMargin || viewportPos.x > 1 + edgeMargin ||
                viewportPos.y < -edgeMargin || viewportPos.y > 1 + edgeMargin)
            {
                // Если центр объекта за пределами экрана, проверяем Bounds через Frustum
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_playerCamera);
                Collider col = target.GetComponent<Collider>();

                if (col != null && !GeometryUtility.TestPlanesAABB(planes, col.bounds))
                {
                    return false;
                }
                else if (col == null)
                {
                    return false;
                }
            }

            // 3. Проверка на препятствия (LineCast)
            // ВАЖНО: Игнорируем попадание в самого себя и его детей
            Vector3 dir = target.transform.position - _playerHead.position;
            if (Physics.Linecast(_playerHead.position, target.transform.position, out RaycastHit hit, _interactableLayers))
            {
                bool isPartOfTarget = false;

                // Проверяем прямую иерархию: является ли попавший объект частью цели
                Transform t = hit.collider.transform;
                while (t != null)
                {
                    if (t.gameObject == target)
                    {
                        isPartOfTarget = true;
                        break;
                    }
                    t = t.parent;
                }

                // Проверяем обратную иерархию: является ли цель частью попавшего объекта
                if (!isPartOfTarget)
                {
                    t = target.transform;
                    while (t != null)
                    {
                        if (t.gameObject == hit.collider.gameObject)
                        {
                            isPartOfTarget = true;
                            break;
                        }
                        t = t.parent;
                    }
                }

                if (!isPartOfTarget) return false;
            }

            return true;
        }

        private void PerformInteractionRaycast()
        {
            _allTargets.Clear();
            _targetGO = null;
            _hitCreature = null;

            float currentInteractionDistance = 2.0f;
            Vector3 headPos = _playerHead.position;
            Vector3 headDir = _playerHead.forward;

            // === 1. Raycast для существ (оставляем точным для боя) ===
            if (Physics.Raycast(headPos, headDir, out RaycastHit hitCreature, 2.0f, _damageLayers))
            {
                TryFindCreature(hitCreature.collider);
            }

            // === 2. Поиск интерактивных объектов через OverlapSphere + Сортировка ===
            Collider[] nearbyColliders = Physics.OverlapSphere(headPos, currentInteractionDistance, _interactableLayers);

            IInteractable bestTarget = null;
            GameObject bestTargetGO = null;
            float bestScore = -1f;

            foreach (var col in nearbyColliders)
            {
                // 🔑 ШАГ 1: Ищем КОРНЕВОЙ объект взаимодействия ВВЕРХ по иерархии
                Transform current = col.transform;
                GameObject candidateRoot = null;
                IInteractable foundInteractable = null;

                // Поднимаемся до 5 уровней вверх
                for (int i = 0; i < 5 && current != null; i++)
                {
                    // ✅ ПРИОРИТЕТ 1: Сначала ищем CORPSE (независимо от состояния BaseLivingEntity)
                    if (current.TryGetComponent<Corpse>(out var corpse))
                    {
                        // Проверяем явный коллайдер или используем любой, если не назначен
                        bool isCorrectCollider = corpse.InteractionCollider == null ||
                                               corpse.InteractionCollider == col;

                        if (isCorrectCollider && corpse.enabled)
                        {
                            candidateRoot = current.gameObject;
                            foundInteractable = corpse;
                            break; // Нашли труп — дальше не ищем, он важнее живого существа
                        }
                    }

                    // ✅ ПРИОРИТЕТ 2: Только если нет трупа, проверяем BaseLivingEntity
                    if (current.TryGetComponent<BaseLivingEntity>(out var livingEntity))
                    {
                        bool isCorrectCollider = livingEntity.InteractionCollider == null ||
                                               livingEntity.InteractionCollider == col;

                        // Для живых существ ПРОВЕРЯЕМ IsAlive()
                        if (isCorrectCollider && livingEntity.IsAlive())
                        {
                            candidateRoot = current.gameObject;
                            foundInteractable = livingEntity;
                            break;
                        }
                    }

                    // ✅ ПРИОРИТЕТ 3: Обычные интерактаблы (сундуки, двери)
                    if (foundInteractable == null)
                    {
                        var interactable = current.GetComponent<IInteractable>();
                        if (interactable != null && !(interactable is Corpse) && !(interactable is BaseLivingEntity))
                        {
                            candidateRoot = current.gameObject;
                            foundInteractable = interactable;
                            break;
                        }
                    }

                    current = current.parent;
                }

                // Если не нашли корневого объекта — пропускаем этот коллайдер
                if (candidateRoot == null || foundInteractable == null) continue;

                // 🔑 ШАГ 2: Проверка видимости ДЛЯ КОРНЕВОГО ОБЪЕКТА
                if (!IsVisibleByCamera(candidateRoot, currentInteractionDistance)) continue;

                // 🔑 ШАГ 3: Расчет приоритета
                Vector3 toCol = col.transform.position - headPos;
                float angle = Vector3.Angle(headDir, toCol.normalized);
                float dist = Vector3.Distance(headPos, candidateRoot.transform.position);

                if (dist > currentInteractionDistance) continue;

                float score = (1f - (angle / 90f)) * 0.7f + (1f - (dist / currentInteractionDistance)) * 0.3f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTargetGO = candidateRoot;
                    bestTarget = foundInteractable;
                }
            }

            // Добавляем в список ТОЛЬКО лучшую цель
            if (bestTarget != null)
            {
                ProcessHitObject(bestTargetGO);
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


        /// <summary>
        /// Обрабатывает попадание в объект. 
        /// Так как вызывается из PerformInteractionRaycast с уже найденной лучшей целью,
        /// поиск IInteractable упрощен до проверки самого объекта.
        /// </summary>

        private void ProcessHitObject(GameObject hitObject)
        {
            if (hitObject == null) return;

            // === БЛОК 1: Поиск IInteractable с ПРИОРИТЕТОМ CORPSE ===

            // Сначала проверяем, есть ли на объекте Corpse
            var corpse = hitObject.GetComponent<Corpse>();
            bool hasCorpse = corpse != null && corpse.enabled;

            // Ищем все интерактаблы
            var interactables = hitObject.GetComponents<IInteractable>();
            foreach (var interactable in interactables)
            {
                // 🔑 КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: Если есть Corpse, игнорируем BaseLivingEntity
                if (hasCorpse && interactable is BaseLivingEntity)
                {
                    continue; // Пропускаем живое существо, т.к. труп важнее
                }

                if (!_allTargets.Contains(interactable))
                    _allTargets.Add(interactable);
            }

            // Фоллбэк: поиск в детях (только если нет Corpse на корне)
            if (!hasCorpse && _allTargets.Count == 0)
            {
                var childInteractables = hitObject.GetComponentsInChildren<IInteractable>();
                foreach (var child in childInteractables)
                {
                    // Та же фильтрация для детей
                    var childCorpse = child as Corpse;
                    var childBLE = child as BaseLivingEntity;

                    if (childCorpse != null || childBLE == null)
                    {
                        if (!_allTargets.Contains(child))
                            _allTargets.Add(child);
                    }
                }
            }

            if (_allTargets.Count > 0 && _targetGO == null)
                _targetGO = hitObject;


            // === БЛОК 2: Поиск BaseLivingEntity (только для боя/урона) ===
            // Здесь оставляем поиск существа, но только если НЕТ трупа
            // Или если труп есть, но нам нужно нанести урон самому трупу (harvest)
            if (_hitCreature == null)
            {
                // Если есть Corpse, считаем его целью для harvest, а не BaseLivingEntity
                if (hasCorpse)
                {
                    // Можно установить corpse как _hitCreature для системы урона, 
                    // если она поддерживает IImpactSoundProvider
                    // Но обычно _hitCreature используется только для TakeDamage живых
                }
                else
                {
                    _hitCreature = hitObject.GetComponent<BaseLivingEntity>() ??
                                  hitObject.GetComponentInChildren<BaseLivingEntity>();

                    if (_hitCreature == null)
                    {
                        Transform parent = hitObject.transform.parent;
                        int depth = 0;
                        while (parent != null && depth < 5)
                        {
                            _hitCreature = parent.GetComponent<BaseLivingEntity>();
                            if (_hitCreature != null) break;
                            parent = parent.parent;
                            depth++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Быстрая проверка только на BaseLivingEntit y (для raycast-атаки).
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
            // Проверка тела (луч попадёт в кость, но Corpse висит на корне)
            if (targetGO != null)
            {

                if (type == InteractType.Interact)
                {
                    if (targetGO.TryGetComponent(out DoorController doorController))
                    {
                        return doorController.IsVisuallyOpen() ? "[E] Закрыть" : "[E] Открыть";
                    }

                    var corpse = targetGO.GetComponent<Corpse>() ?? targetGO.GetComponentInParent<Corpse>();
                    if (corpse != null && corpse.enabled)
                    {
                        return corpse.IsDragging ? "[E] Отпустить тело" : "[E] Тащить тело";
                    }
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
                // Если открыт инвентарь цели или радиальное меню - закрываем
                if (_panelsController.IsInventoryOpened() || _panelsController.IsRadialMenuOpened())
                {
                    _panelsController.CloseAllPanels();
                }
            }
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


    }
}