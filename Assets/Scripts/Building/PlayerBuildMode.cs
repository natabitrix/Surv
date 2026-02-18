// Assets/Scripts/Building/PlayerBuildMode.cs
using System;
using System.Net.Http.Headers;
using Assets.Scripts.Core;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.Building
{

    public class PlayerBuildMode : MonoBehaviour
    {
        [Header("Grid & Layers")]
        public float foundationGridSize = 2f;
        public LayerMask groundLayer;
        public LayerMask structureLayer;
        public float maxDistance = 10f;
        [SerializeField] private Material previewMaterial;

        [Header("Visual Feedback")]
        public Color validColor = new Color(0f, 1f, 0f, 0.4f);
        public Color invalidColor = new Color(1f, 0f, 0f, 0.4f);

        [Header("Placement Parameters")]
        public float wallThickness = 0.1f;
        public float ceilingThickness = 0.1f;

        // === КЭШИРОВАНИЕ ВЫСОТ ===
        private float _cachedFoundationHeight = 0.7f;
        private float _cachedWallHeight = 2f;

        private Item _currentBlueprint;
        private GameObject _previewInstance;
        private bool _isActive = false;
        private bool _isPositionValid = false;
        private bool _isRotationEnabled = false;

        private Vector3 _previewPosition = Vector3.zero;
        private Quaternion _previewRotation = Quaternion.identity;
        private SnapPoint _activeSnapPoint = null; // Текущая активная точка крепления
        private GameObject _baseObject = null;     // Объект, на котором находится точка
        private PlayerController _playerController = null;

        public event System.Action OnBuildActive;
        public event System.Action OnStructurePlaced;
        public event System.Action OnBuildExit;
        public int ActiveBuildSlotIndex { get; private set; } = -1;

        private enum PlacementState { Positioning, Rotating, Completed }
        private PlacementState _placementState = PlacementState.Positioning;
        private Vector3 _fixedPosition = Vector3.zero;
        private float _currentRotationY = 0f;
        private const float ROTATION_SPEED = 90f;
        private float _rotationSensitivity = 0.3f;

        // Добавьте в начало класса PlayerBuildMode (рядом с другими полями)
        private Vector3 _debugClosestGroundPoint = Vector3.zero;

        private enum StructureType
        {
            Foundation, Wall, Ceiling, Door, DoorFrame, Gate, GateFrame
        }

        void Update()
        {
            if (!_isActive || _currentBlueprint == null || _currentBlueprint.placeablePrefab == null)
                return;

            if (TryGetComponent<PlayerController>(out var pc)) _playerController = pc;

            if (_playerController != null && _playerController.LockCameraOnEsc) return;

            StructureType buildType = GetStructureType(_currentBlueprint);
            CalculatePreviewPlacement(buildType);
            EnsurePreviewExists();
            UpdatePreviewTransform();
            _isPositionValid = IsPositionValid(buildType);
            UpdatePreviewColor(_isPositionValid);
            HandleInput();

            // Ручное вращение превью мышью (только для фундамента/ворот)
            if (_placementState == PlacementState.Rotating && IsRotationModeStructure())
            {
                float mouseXDelta = Input.GetAxis("Mouse X");
                _currentRotationY += mouseXDelta * _rotationSensitivity * 100f;
                _currentRotationY = Mathf.Repeat(_currentRotationY, 360f);

                _previewPosition = _fixedPosition;
                _previewRotation = Quaternion.Euler(0f, _currentRotationY, 0f);
                _isPositionValid = IsPositionValid(GetStructureType(_currentBlueprint));
                UpdatePreviewColor(_isPositionValid);
            }

        }

        private StructureType GetStructureType(Item blueprint)
        {
            GameObject prefab = blueprint.placeablePrefab;
            if (prefab.CompareTag("Foundation")) return StructureType.Foundation;
            if (prefab.CompareTag("Wall")) return StructureType.Wall;
            if (prefab.CompareTag("Ceiling")) return StructureType.Ceiling;
            if (prefab.CompareTag("Door")) return StructureType.Door;
            if (prefab.CompareTag("DoorFrame")) return StructureType.DoorFrame;
            if (prefab.CompareTag("Gate")) return StructureType.Gate;
            if (prefab.CompareTag("GateFrame")) return StructureType.GateFrame;
            return StructureType.Foundation;
        }

        private void CalculatePreviewPlacement(StructureType type)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Ray viewRay = new(Camera.main.transform.position, Camera.main.transform.forward);

            _activeSnapPoint = null;
            _baseObject = null;

            switch (type)
            {
                case StructureType.Foundation:
                case StructureType.GateFrame:
                    if (_placementState == PlacementState.Rotating) return;

                    // === 1. СНАП К ТОЧКЕ КРЕПЛЕНИЯ ПОД КУРСОРОМ (сохраняем как есть) ===
                    if (FindSnapPointUnderCursor(ray, SnapPoint.AttachmentType.Foundation, out _activeSnapPoint, out _baseObject))
                    {
                        _previewPosition = _activeSnapPoint.transform.position;
                        _previewRotation = _activeSnapPoint.transform.rotation;
                        _isRotationEnabled = false;
                        break; // Точка крепления имеет приоритет
                    }

                    // === РЕЙКАСТ В НАПРАВЛЕНИИ ВЗГЛЯДА КАМЕРЫ ===
                    bool hitGroundInView = Physics.Raycast(viewRay, out RaycastHit viewHit, maxDistance, groundLayer);

                    if (hitGroundInView)
                    {
                        // Потенциальная позиция на земле в направлении взгляда
                        Vector3 potentialPosition = new Vector3(
                            viewHit.point.x,
                            viewHit.point.y + _cachedFoundationHeight * 0.5f,
                            viewHit.point.z
                        );
                        Quaternion potentialRotation = Quaternion.identity;

                        // === АВТОМАТИЧЕСКОЕ СНАПЛЕНИЕ К БЛИЖАЙШЕМУ ФУНДАМЕНТУ) ===
                        if (_previewInstance != null)
                        {
                            Renderer previewRenderer = _previewInstance.GetComponent<Renderer>();
                            if (previewRenderer == null) previewRenderer = _previewInstance.GetComponentInChildren<Renderer>();
                            if (previewRenderer != null)
                            {
                                Bounds potentialBounds = previewRenderer.bounds;
                                Vector3 bufferedExtents = potentialBounds.extents * 0.95f;
                                Collider[] nearbyColliders = Physics.OverlapBox(potentialPosition, bufferedExtents, potentialRotation, structureLayer);

                                GameObject nearestFoundationCandidate = null;
                                float minDistanceToCandidate = float.MaxValue;

                                foreach (var nearbyCollider in nearbyColliders)
                                {
                                    if (nearbyCollider.gameObject == _previewInstance) continue;
                                    if (nearbyCollider.CompareTag("Foundation") || nearbyCollider.CompareTag("GateFrame"))
                                    {
                                        SnapPoint[] candidateSnapPoints = nearbyCollider.GetComponentsInChildren<SnapPoint>();
                                        foreach (var sp in candidateSnapPoints)
                                        {
                                            if (sp.attachmentType != SnapPoint.AttachmentType.Foundation &&
                                                sp.attachmentType != SnapPoint.AttachmentType.Any)
                                                continue;

                                            float dist = Vector3.Distance(potentialPosition, sp.transform.position);
                                            if (dist < foundationGridSize * 0.75f && dist < minDistanceToCandidate)
                                            {
                                                minDistanceToCandidate = dist;
                                                nearestFoundationCandidate = nearbyCollider.gameObject;
                                                _activeSnapPoint = sp;
                                            }
                                        }
                                    }
                                }

                                // Если найдена подходящая точка крепления — используем её
                                if (_activeSnapPoint != null && nearestFoundationCandidate != null)
                                {
                                    _previewPosition = _activeSnapPoint.transform.position;
                                    _previewRotation = _activeSnapPoint.transform.rotation;
                                    _baseObject = nearestFoundationCandidate;
                                    _isRotationEnabled = false;
                                    break;
                                }
                            }
                        }

                        // === СНАП К ЗЕМЛЕ В НАПРАВЛЕНИИ ВЗГЛЯДА ===
                        _previewPosition = potentialPosition;
                        _previewRotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
                        _isRotationEnabled = true;
                    }
                    else
                    {
                        // === НЕТ ЗЕМЛИ В ПРЕДЕЛАХ maxDistance — ПРЕВЬЮ В ВОЗДУХЕ ===
                        _previewPosition = Camera.main.transform.position + Camera.main.transform.forward * 10f;
                        _previewPosition.y -= 0.3f; // чуть ниже уровня глаз для видимости
                        _previewRotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
                        _isRotationEnabled = true;
                    }
                    break;

                case StructureType.Wall:
                case StructureType.DoorFrame:
                    // Ищем точку крепления типа Wall на структурах в пределах луча
                    if (FindSnapPointUnderCursor(ray, SnapPoint.AttachmentType.Wall, out _activeSnapPoint, out _baseObject))
                    {
                        // Просто копируем трансформ точки крепления!
                        _previewPosition = _activeSnapPoint.transform.position;
                        _previewRotation = _activeSnapPoint.transform.rotation;
                    }
                    else
                    {
                        _previewPosition = Camera.main.transform.position + Camera.main.transform.forward * 10f;
                        _previewPosition.y += _cachedFoundationHeight; // выше высоты фундамента
                        _previewRotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
                    }
                    break;

                case StructureType.Ceiling:
                    // Ищем точку крепления типа Ceiling на стенах/фундаментах
                    if (FindSnapPointUnderCursor(ray, SnapPoint.AttachmentType.Ceiling, out _activeSnapPoint, out _baseObject))
                    {
                        _previewPosition = _activeSnapPoint.transform.position;
                        _previewRotation = _activeSnapPoint.transform.rotation;
                    }
                    else
                    {
                        _previewPosition = Camera.main.transform.position + Camera.main.transform.forward * 10f;
                        _previewPosition.y += _cachedWallHeight; // выше высоты стены
                        _previewRotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
                    }
                    break;

                case StructureType.Door:
                    // Ищем точку крепления типа Door
                    if (FindSnapPointUnderCursor(ray, SnapPoint.AttachmentType.Door, out _activeSnapPoint, out _baseObject))
                    {
                        _previewPosition = _activeSnapPoint.transform.position;
                        _previewRotation = _activeSnapPoint.transform.rotation;
                    }
                    else
                    {
                        _previewPosition = Camera.main.transform.position + Camera.main.transform.forward * 10f;
                        _previewPosition.y += _cachedFoundationHeight;  // выше высоты фундамента
                        _previewRotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
                    }

                    break;
            }

            // Защита от NaN
            if (float.IsNaN(_previewPosition.x) || float.IsNaN(_previewPosition.y) || float.IsNaN(_previewPosition.z))
            {
                _previewPosition = Camera.main.transform.position + Camera.main.transform.forward * 10f;
                _previewPosition.y = Camera.main.transform.position.y;
            }
        }

        /// <summary>
        /// Ищет ближайшую подходящую точку крепления под курсором мыши.
        /// </summary>
        private bool FindSnapPointUnderCursor(Ray ray, SnapPoint.AttachmentType requiredType,
                                             out SnapPoint snapPoint, out GameObject baseObject)
        {
            snapPoint = null;
            baseObject = null;

            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, structureLayer))
                return false;

            SnapPoint[] snapPoints = hit.collider.gameObject.GetComponentsInChildren<SnapPoint>();
            if (snapPoints.Length == 0) return false;

            // Находим ближайшую точку крепления подходящего типа
            SnapPoint closest = null;
            float minDistance = float.MaxValue;

            foreach (var sp in snapPoints)
            {
                if (sp.attachmentType != requiredType && sp.attachmentType != SnapPoint.AttachmentType.Any)
                    continue;

                // ✅ Ищем БЛИЖАЙШУЮ точку без жёсткой проверки радиуса
                float distance = Vector3.Distance(hit.point, sp.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = sp;
                }
            }

            // ✅ Дополнительная проверка: точка должна быть в пределах разумного расстояния
            if (closest != null && minDistance < foundationGridSize) // < 1.5 для сетки 2.0
            {
                snapPoint = closest;
                baseObject = hit.collider.gameObject;
                return true;
            }

            return false;
        }

        private bool IsPositionValid(StructureType type)
        {
            if (_previewInstance == null) return false;

            switch (type)
            {
                case StructureType.Foundation:
                case StructureType.GateFrame:

                    // === ПРОВЕРКА РЕНДЕРА ===
                    if (_previewInstance == null) return false;
                    Renderer previewRenderer = _previewInstance.GetComponent<Renderer>();
                    if (previewRenderer == null)
                        previewRenderer = _previewInstance.GetComponentInChildren<Renderer>();
                    if (previewRenderer == null) return false;

                    // === 1. НАХОДИМ БЛИЖАЙШУЮ ПОВЕРХНОСТЬ (куб, скала, террейн) ===
                    Vector3 closestGroundPoint = Vector3.zero;
                    float minDistance = float.MaxValue;

                    Vector3[] checkOffsets = new Vector3[]
                    {
                        Vector3.zero,                      // центр
                        new Vector3(-1f, 0f, -1f),         // левый-задний
                        new Vector3(1f, 0f, -1f),          // правый-задний
                        new Vector3(-1f, 0f, 1f),          // левый-передний
                        new Vector3(1f, 0f, 1f)            // правый-передний
                    };

                    float halfSize = foundationGridSize * 0.5f * 0.95f;
                    const float MAX_AIR_GAP_UNDER = 0f; // допустимый зазор в воздухе
                    const float MIN_HEIGHT_OVER_GROUND = 0.1f; // минимальная высота над землей

                    foreach (var offset in checkOffsets)
                    {
                        // Мировая позиция точки НА НИЖНЕЙ ГРАНИ фундамента
                        Vector3 localOffset = new Vector3(offset.x * halfSize, _cachedFoundationHeight * 0.5f, offset.z * halfSize);
                        Vector3 worldCheckPos = _previewPosition + _previewRotation * localOffset;
                        float checkDistance = _cachedFoundationHeight + MAX_AIR_GAP_UNDER - MIN_HEIGHT_OVER_GROUND;
                        Vector3 start = worldCheckPos + Vector3.down * MIN_HEIGHT_OVER_GROUND;

                        // Ищем БЛИЖАЙШУЮ поверхность внизу
                        if (Physics.Raycast(
                            start,  // старт (верх фундамента)
                            Vector3.down,
                            out RaycastHit hit,
                            checkDistance, // стоп (низ фундамента)
                            groundLayer,
                            QueryTriggerInteraction.Collide))
                        {
                            float distance = (start - hit.point).magnitude;

                            // Запоминаем БЛИЖАЙШУЮ точку
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closestGroundPoint = hit.point;
                                _debugClosestGroundPoint = hit.point; // Сохраняем для отладки
                                // Debug.Log($"[CLOSEST] Found ground at Y={hit.point.y:F2} (distance={distance:F2}) | object={hit.collider.gameObject.name}");
                            }
                        }

                        // // Рисуем точку проверки вниз
                        // drawPoint(start, Color.cyan);
                        // // Рисуем луч вниз от точки проверки (жёлтый)
                        // Debug.DrawRay(start, Vector3.down * checkDistance, Color.yellow);
                    }

                    // if (_debugClosestGroundPoint != Vector3.zero)
                    // {
                    //     drawPoint(_debugClosestGroundPoint, Color.green);
                    // }

                    // Если не нашли НИКАКОЙ поверхности в пределах высоты фундамента → невалидно
                    if (minDistance == float.MaxValue)
                    {
                        // Debug.Log("Foundation INVALID: no ground within foundation height");
                        return false;
                    }

                    // === ПРОВЕРКА КОЛЛИЗИЙ ===
                    // Получаем текущую позицию и вращение превью (уже выставлены в CalculatePreviewPlacement)
                    Vector3 placementPosition = _previewInstance.transform.position;
                    Quaternion placementRotation = _previewInstance.transform.rotation;
                    Bounds previewBounds = previewRenderer.bounds;

                    // Уменьшаем extents, чтобы создать буфер при проверке с поворотом
                    Vector3 bufferedExtents = previewBounds.extents * 0.5f;

                    // Проверяем пересечение с уже существующими структурами
                    // Используем OverlapBox с позицией, *буферизованными* размерами и вращением превью
                    Collider[] overlappingColliders = Physics.OverlapBox(
                        placementPosition,              // Мировая позиция превью
                        bufferedExtents,                // Половины размеров бокса в ЛОКАЛЬНЫХ осях объекта (буферизованные!)
                        placementRotation,              // Мировое вращение превью
                        structureLayer                  // Проверяем только против слоя структур
                    );

                    bool foundInvalidCollision = false;
                    string firstInvalidColliderName = ""; // Для отладки

                    foreach (var collider in overlappingColliders)
                    {
                        // Игнорируем сам объект превью, если он случайно попал в проверку
                        if (collider.gameObject == _previewInstance)
                        {
                            // Debug.Log("IsPositionValid: Ignoring self-collision check.");
                            continue;
                        }

                        // Также игнорируем базовый объект, на котором находится точка привязки (если она есть)
                        // Это важно, когда мы привязываемся к существующему фундаменту
                        if (collider.gameObject == _baseObject)
                        {
                            // Debug.Log($"IsPositionValid: Ignoring base object collision with {_baseObject.name}.");
                            continue;
                        }

                        // Если найден другой объект структуры (фундамент, стена и т.д.), помечаем это как недопустимое пересечение
                        // и выходим из цикла, так как одной коллизии достаточно для невалидной позиции.
                        foundInvalidCollision = true;
                        firstInvalidColliderName = collider.gameObject.name; // Запоминаем имя для лога
                        // Debug.Log($"IsPositionValid: Found FIRST invalid overlapping structure collision with {collider.gameObject.name}");
                        break; // Выходим из цикла, как только нашли первую "настоящую" коллизию
                    }

                    // Проверяем, было ли найдено недопустимое пересечение после фильтрации
                    if (foundInvalidCollision)
                    {
                        // Debug.Log($"IsPositionValid: Position is INVALID due to collision with {firstInvalidColliderName}.");
                        return false; // Найдено недопустимое пересечение
                    }

                    // Debug.Log("IsPositionValid: No collisions found, position is valid.");
                    // Если пересечений (кроме себя и базового объекта) не найдено, позиция действительна
                    return true;

                case StructureType.Wall:
                case StructureType.DoorFrame:
                    // Валидация: должна быть активная точка крепления
                    if (_activeSnapPoint == null) return false;

                    // Проверка коллизий с другими стенами
                    float halfWidth = wallThickness * 0.6f;
                    float halfHeight = _cachedWallHeight * 0.5f;

                    Collider[] wallHits = Physics.OverlapBox(
                        _previewPosition,
                        new Vector3(halfWidth, halfHeight, halfWidth),
                        _previewRotation,
                        structureLayer
                    );

                    foreach (var h in wallHits)
                    {
                        if (h.gameObject == _previewInstance || h.gameObject == _baseObject) continue;
                        if (h.CompareTag("Wall") || h.CompareTag("DoorFrame")) return false;
                    }
                    return true;

                case StructureType.Ceiling:
                    // Валидация: должна быть активная точка крепления
                    if (_activeSnapPoint == null) return false;

                    // Проверка коллизий с другими потолками
                    float halfCeilingWidth = foundationGridSize * 0.5f * 0.7f; // буфер 30%
                    float halfCeilingHeight = ceilingThickness * 0.5f * 0.7f;

                    Collider[] ceilingHits = Physics.OverlapBox(
                        _previewPosition,
                        new Vector3(halfCeilingWidth, halfCeilingHeight, halfCeilingWidth),
                        _previewRotation,
                        structureLayer
                    );

                    foreach (var h in ceilingHits)
                    {
                        if (h.gameObject == _previewInstance || h.gameObject == _baseObject) continue;
                        if (h.CompareTag("Ceiling")) return false; // Коллизия с другим потолком
                    }

                    // if (!HasFoundationInCeilingGrid(_previewPosition))
                    //     return false;

                    return true;

                case StructureType.Door:
                    if (_activeSnapPoint == null) return false;

                    // if (_baseObject == null || !_baseObject.CompareTag("DoorFrame"))
                    //     return false;

                    // GameObject doorHinge = FindChildWithTag(_baseObject, "DoorHinge");
                    // if (doorHinge == null) return false;
                    // if (FindChildWithTag(doorHinge, "Door") != null) return false;

                    return true;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Проверяет наличие фундамента в том же квадрате сетки под потолком.
        /// Игнорирует высоту — ищет фундамент по X/Z в пределах квадрата foundationGridSize.
        /// </summary>
        private bool HasFoundationInCeilingGrid(Vector3 ceilingPosition)
        {
            // 1. Округляем позицию потолка до сетки фундамента
            float gridX = Mathf.Round(ceilingPosition.x / foundationGridSize) * foundationGridSize;
            float gridZ = Mathf.Round(ceilingPosition.z / foundationGridSize) * foundationGridSize;

            // 2. Центр поиска — в центре квадрата сетки, по Y — на уровне потолка
            Vector3 searchCenter = new Vector3(gridX, ceilingPosition.y, gridZ);

            // 3. Размеры бокса для поиска:
            //    - По X/Z: 45% от размера сетки (чтобы покрыть весь квадрат)
            //    - По Y: большой размер (200 единиц) для поиска фундамента на ЛЮБОЙ высоте ниже потолка
            Vector3 searchExtents = new Vector3(
                foundationGridSize * 0.45f,
                100f,  // Ищем вверх/вниз на 100 единиц — достаточно для любого здания
                foundationGridSize * 0.45f
            );

            // 4. Выполняем поиск по всей вертикали в пределах квадрата сетки
            Collider[] hits = Physics.OverlapBox(
                searchCenter,
                searchExtents,
                Quaternion.identity,  // Без поворота — фундамент всегда axis-aligned
                structureLayer
            );

            // 5. Проверяем найденные коллайдеры
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Foundation"))
                {
                    // Дополнительная проверка: фундамент должен быть в том же квадрате сетки
                    float fx = Mathf.Round(hit.transform.position.x / foundationGridSize) * foundationGridSize;
                    float fz = Mathf.Round(hit.transform.position.z / foundationGridSize) * foundationGridSize;

                    if (Mathf.Approximately(fx, gridX) && Mathf.Approximately(fz, gridZ))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void drawPoint(Vector3 point, Color color)
        {
            Debug.DrawRay(point, Vector3.up * 0.1f, color);
            Debug.DrawRay(point, Vector3.down * 0.1f, color);
            Debug.DrawRay(point, Vector3.right * 0.1f, color);
            Debug.DrawRay(point, Vector3.left * 0.1f, color);
            Debug.DrawRay(point, Vector3.forward * 0.1f, color);
            Debug.DrawRay(point, Vector3.back * 0.1f, color);
        }

        // === УПРАВЛЕНИЕ ПРЕВЬЮ ===
        private void EnsurePreviewExists()
        {
            if (_previewInstance != null) return;
            if (_currentBlueprint?.placeablePrefab == null) return;

            _previewInstance = Instantiate(_currentBlueprint.placeablePrefab);

            //Удаляем все ненужные компоненты и дочерние элементы из превью
            // Destroy(_previewInstance.GetComponent<DoorController>());

            foreach (var dc in _previewInstance.GetComponentsInChildren<DoorController>())
                Destroy(dc);

            foreach (var col in _previewInstance.GetComponentsInChildren<Collider>())
                Destroy(col);

            foreach (var snapPoint in _previewInstance.GetComponentsInChildren<SnapPoint>())
                Destroy(snapPoint);

            foreach (Transform child in _previewInstance.transform)
            {
                if(_previewInstance.CompareTag("Door")) continue;
                Destroy(child.gameObject);
            }
                


            ApplyPreviewMaterial(_previewInstance);
            _previewInstance.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        private void UpdatePreviewTransform()
        {
            if (_previewInstance == null) return;
            _previewInstance.transform.position = _previewPosition;
            _previewInstance.transform.rotation = _previewRotation;
        }

        private void ApplyPreviewMaterial(GameObject target)
        {
            Material baseMat = previewMaterial != null ? previewMaterial : new Material(Shader.Find("Standard"));
            if (baseMat == null) return;

            foreach (var renderer in target.GetComponentsInChildren<Renderer>())
            {
                Material instanceMat = new Material(baseMat);
                instanceMat.color = validColor;
                renderer.material = instanceMat;
            }
        }

        private void UpdatePreviewColor(bool isValid)
        {
            if (_previewInstance == null) return;

            Color color = isValid ? validColor : invalidColor;
            foreach (var renderer in _previewInstance.GetComponentsInChildren<Renderer>())
            {
                if (renderer.material != null)
                    renderer.material.color = color;
            }
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(1))
            {
                ExitBuildMode();
                return;
            }

            // Фундамент/ворота вращаем первым кликом
            if (_placementState == PlacementState.Positioning &&
                Input.GetMouseButtonDown(0) &&
                _isPositionValid &&
                _previewInstance != null &&
                IsRotationModeStructure())
            {
                _fixedPosition = _previewPosition;
                _currentRotationY = _previewRotation.eulerAngles.y;
                _placementState = PlacementState.Rotating;
                return;
            }

            // Устанавливаем вторым кликом после поворота 
            if (_placementState == PlacementState.Rotating && Input.GetMouseButtonDown(0))
            {
                PlaceStructure();
                _placementState = PlacementState.Positioning;
                return;
            }

            // Структуры одним кликом
            if (_placementState == PlacementState.Positioning &&
                Input.GetMouseButtonDown(0) &&
                _isPositionValid &&
                _previewInstance != null &&
                !IsRotationModeStructure())
            {
                PlaceStructure();
            }
        }

        private bool IsRotationModeStructure()
        {
            return _currentBlueprint?.placeablePrefab != null &&
                   _isRotationEnabled == true &&
                   (_currentBlueprint.placeablePrefab.CompareTag("Foundation") ||
                    _currentBlueprint.placeablePrefab.CompareTag("GateFrame"));
        }

        public void StartBuildMode(Item blueprint, int slotIndex)
        {
            if (blueprint?.placeablePrefab == null)
            {
                Debug.LogError("Blueprint has no preview model!");
                return;
            }

            _currentBlueprint = blueprint;
            ActiveBuildSlotIndex = slotIndex;
            _isActive = true;
            _placementState = PlacementState.Positioning;
            EnsurePreviewExists();
            OnBuildActive?.Invoke();
        }

        public void ExitBuildMode()
        {
            _isActive = false;
            _currentBlueprint = null;
            _baseObject = null;
            _activeSnapPoint = null;
            _placementState = PlacementState.Positioning;

            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }

            ActiveBuildSlotIndex = -1;
            OnBuildExit?.Invoke();
        }

        private void PlaceStructure()
        {
            if (_previewInstance == null || !_isPositionValid) return;

            GameObject placed = Instantiate(
                _currentBlueprint.placeablePrefab,
                _previewInstance.transform.position,
                _previewInstance.transform.rotation
            );

            // Двери остаются дочерними объектами (как в оригинале)
            // if (_baseObject != null && _baseObject.CompareTag("DoorFrame") && placed.CompareTag("Door"))
            // {
            //     GameObject doorHinge = FindChildWithTag(_baseObject, "DoorHinge");
            //     if (doorHinge != null)
            //     {
            //         placed.transform.SetParent(doorHinge.transform, true);
            //     }
            // }

            // Стены/потолки НЕ становятся дочерними объектами — только сохраняем ссылку для логики разрушения
            // if (_baseObject != null && (_currentBlueprint.placeablePrefab.CompareTag("Wall") ||
            //                             _currentBlueprint.placeablePrefab.CompareTag("DoorFrame") ||
            // 
            if (_baseObject != null)
            {
                StructureAttachment attachment = placed.AddComponent<StructureAttachment>();
                attachment.attachedTo = _baseObject;
                attachment.snapPoint = _activeSnapPoint;
            }

            // === СОХРАНЕНИЕ ===
            string parentId = null;
            // if (_baseObject != null && _baseObject.CompareTag("DoorFrame") && placed.CompareTag("Door"))
            // {
            //     var identity = _baseObject.GetComponent<StructureIdentity>();
            //     parentId = identity?.instanceId;
            // }
            WorldManager.Instance.RegisterStructure(placed, _currentBlueprint.Id, parentId);


            OnStructurePlaced?.Invoke();
        }



        public static GameObject FindChildWithTag(GameObject parent, string tag)
        {
            Transform t = parent.transform;
            foreach (Transform child in t)
            {
                if (child.CompareTag(tag))
                    return child.gameObject;
            }
            return null;
        }

        public bool IsActive() => _isActive;
        public Item GetCurrentItem() => _currentBlueprint;
    }

    /// <summary>
    /// Компонент для сохранения связи между структурой и её базой (для логики разрушения).
    /// Структура НЕ является дочерним объектом — только хранит ссылку.
    /// </summary>
    public class StructureAttachment : MonoBehaviour
    {
        public GameObject attachedTo;      // Фундамент/стена, к которой прикреплена структура
        public SnapPoint snapPoint;        // Использованная точка крепления
        public bool isDestroyed = false;   // Флаг разрушения для оптимизации
    }
}