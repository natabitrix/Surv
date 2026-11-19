# Аудит 5: Мир и строительство

> Система: чанковое сохранение мира, регистрация/удаление структур, размещение построек, разрушение структур, сундуки как структуры.
> Файлы: `WorldManager.cs`, `PlayerBuildMode.cs`, `Structure.cs`, `ChestController.cs`.
> Статус: ✅ аудит завершён.

---

## Цепочка владения
WorldManager (сценовый, синглтон, создаётся WorldBootstrap)
├── _chunks : Dictionary<Vector2Int, List<SerializableStructure>>
├── _instanceMap : Dictionary<string, GameObject> ← instanceId → GameObject
├── _dirtyChunks : HashSet<Vector2Int>
├── Update: автосохранение грязных чанков (saveInterval = 5 сек)
├── OnApplicationQuit: SaveAllChunks()
├── RegisterStructure(structure, itemId, parentId, ownerId)
│ ├── Guid.NewGuid() → instanceId
│ ├── structure.AddComponent<StructureIdentity>()
│ ├── _chunks[chunkKey].Add(new SerializableStructure(...))
│ ├── _instanceMap[instanceId] = structure
│ ├── _dirtyChunks.Add(chunkKey)
│ └── SaveDirtyChunks() ← МГНОВЕННОЕ СОХРАНЕНИЕ
├── UnregisterStructure(structure) → удаление из чанка + dirty
├── LoadWorldAsync(playerPosition) → читает файлы чанков → InstantiateLoadedChunks()
└── ClearExistingStructures() → Destroy по тегам

PlayerBuildMode (на игроке)
├── Update: CalculatePreviewPlacement() → EnsurePreviewExists() → UpdatePreviewTransform()
├── HandleInput(): ЛКМ → PlaceStructure() / ПКМ → ExitBuildMode()
├── PlaceStructure():
│ ├── Instantiate(prefab, position, rotation)
│ ├── StructureAttachment (если _baseObject != null)
│ └── WorldManager.Instance.RegisterStructure(placed, itemId, parentId)
└── StartBuildMode(blueprint, slotIndex) / ExitBuildMode()

Structure (на каждом объекте структуры)
├── Awake: playerController = PlayerController.Instance
├── Interact(context):
│ ├── isTargetInventory → OpenInventory() → ChestUIManager.Toggle(this)
│ └── IsAttack → TakeDamage(playerInteraction)
├── TakeDamage(playerInteraction):
│ ├── damage = playerController.GetAttackDamage()
│ ├── health -= damage
│ ├── DamageNumberPool.ShowDamage(...)
│ └── if (health <= 0) Die()
└── Die():
├── ChestUIManager.Close() если открыт
├── animator.SetTrigger("BreakLast")
├── InventoryManager.Instance.DropAllItemsToBag(_inventory.Data, structureId, false)
├── Destroy(gameObject, 0.5f)
└── OnDeath?.Invoke(this)

ChestController (на сундуке)
├── Awake: _inventory = GetComponent<ChestInventory>()
├── Interact(context): isTargetInventory → Open() → ChestUIManager.Toggle(this)
└── OnDestroy: если открыт — ChestUIManager.Close()


**Ключевое наблюдение:** `WorldManager` уже имеет задел под мультиплеер — поле `ownerPlayerId` в `SerializableStructure`, но `ownerId = "player_001"` хардкод. Сохранение — **мгновенное** при каждой постройке (`SaveDirtyChunks` в `RegisterStructure`), это блокер для сети: каждый клиент будет писать файлы.

---

## Вопрос А. Что при одновременном действии двух игроков?

| Сценарий | Что произойдёт в текущем коде |
|---|---|
| Два игрока строят в одном чанке одновременно | Оба вызывают `RegisterStructure`. `_chunks[chunkKey].Add(...)` — не потокобезопасно. `SaveDirtyChunks()` вызывается **дважды** (мгновенно при каждой постройке). Второй `SaveDirtyChunks` может перезаписать файл первого. Lost update. |
| Два игрока строят на одном месте | `IsPositionValid` проверяет `OverlapBox` локально у каждого клиента. Оба видят «свободно» (потому что второй объект ещё не заспавнен у первого). Оба ставят структуру → **две структуры в одной точке**. |
| Один игрок ломает структуру, другой — строит рядом | `Structure.TakeDamage` → `health -= damage` → `Die()` → `Destroy(gameObject)`. Второй игрок может поставить структуру на освободившееся место, но `WorldManager` ещё не знает об удалении (если `UnregisterStructure` не вызван). |
| Один игрок ломает структуру с лутом | `Structure.Die()` → `InventoryManager.Instance.DropAllItemsToBag(...)` — создаёт `LootBag`. Второй клиент может «подобрать» лут дважды (см. аудит 1). |
| Один игрок открывает сундук, другой — ломает его | `ChestController.OnInventoryOpened` → `_isOpen = true`. `Structure.Die()` → `ChestUIManager.Close()`. Но `ChestUIManager.CurrentSource` — глобальный (см. аудит 1). Второй игрок может «открыть» тот же сундук. |
| `WorldManager.Update` автосохранение | Тикает у каждого клиента. Два клиента пишут файлы чанков → конфликт. |
| `LoadWorldAsync` при входе | `ClearExistingStructures()` — `Destroy` по тегам. Второй клиент при загрузке удалит структуры первого (если они в одной сцене). |

**Вывод:** мир и строительство **полностью локальные**. Нет ни одного механизма синхронизации. При коопе два клиента будут строить в разных реальностях, а `WorldManager` будет писать конфликтующие файлы.

---

## Вопрос Б. Может ли клиент соврать?

| Действие | Где проверка | Можно ли соврать |
|---|---|---|
| Поставить структуру | `PlayerBuildMode.PlaceStructure` → `WorldManager.RegisterStructure` — публичный | Да, можно поставить любую структуру в любом месте (нет проверки ресурсов, дистанции, владельца) |
| Ломать структуру | `Structure.TakeDamage(playerInteraction)` — публичный, `damage = playerController.GetAttackDamage()` | Да, можно нанести любой урон (передав другой `PlayerInteraction` или подделав `Item.damage`) |
| Удалить структуру | `WorldManager.UnregisterStructure(structure)` — публичный | Да, можно удалить любую структуру |
| Открыть сундук | `ChestController.Open()` → `ChestUIManager.Toggle(this)` — публичный | Да, можно открыть любой сундук (нет проверки дистанции) |
| Забрать из сундука | `ChestUI.MoveAllToPlayer` (см. аудит 1) | Да |
| `ownerPlayerId` | Хардкод `"player_001"` в `RegisterStructure` и `Structure.Die` | Нет смысла, но и не защита |
| `instanceId` | `Guid.NewGuid()` на клиенте | Да, можно подделать, если перехватить |
| `WorldManager.Save` | Пишет локальные файлы | Да, клиент может подделать файл чанка |
| `LoadWorldAsync` | Читает локальные файлы | Да, клиент может подделать |

**Вывод:** клиент полностью авторитетен по миру. Нет ни одной проверки: ресурсов на постройку, дистанции до структуры, владельца, возможности ломать чужие структуры.

---

## Вопрос В. Насколько критична задержка?

| Операция | Критичность | Комментарий |
|---|---|---|
| Постановка структуры | Средняя | Визуально — мгновенно (превью), но сервер должен подтвердить (ресурсы, позиция). |
| Разрушение структуры | Средняя | Анимация — локально, но `health` и `Die()` — серверные. |
| Открытие сундука | Низкая | Локально, но данные — серверные (см. аудит 1). |
| Дроп лута из структуры | Высокая | Спавн `LootBag` — мир. Серверный. |
| `WorldManager.Save` | Низкая | Не критично к задержке, но должно быть серверным. |
| `LoadWorldAsync` | Высокая | При входе в мир — серверная загрузка. Клиент не должен грузить чанки сам. |
| `ClearExistingStructures` | Высокая | При загрузке — серверное. Клиент не должен удалять структуры. |

**Вывод:** строительство терпимо к задержке, но требует серверной валидации. Загрузка мира — серверная.

---

## Таблица разметки

| Система | Кто авторитетен | Почему | Что переделать |
|---|---|---|---|
| Постановка структуры (`PlayerBuildMode.PlaceStructure` → `WorldManager.RegisterStructure`) | Сервер | Клиент не решает, что построено. Нет проверки ресурсов/дистанции | `ServerRpc RequestPlaceStructure(itemId, position, rotation, parentId)`. Сервер проверяет: ресурсы, дистанция, `IsPositionValid` (серверный OverlapBox), владелец. |
| Регистрация структуры (`WorldManager.RegisterStructure`) | Сервер | Пишет в `_chunks` + `SaveDirtyChunks()` мгновенно | `RegisterStructure` → `internal`, только на сервере. `SaveDirtyChunks` — по таймеру на сервере, не мгновенно. |
| Удаление структуры (`WorldManager.UnregisterStructure`) | Сервер | Удаление без валидации = потеря | `internal`, только на сервере. Клиент шлёт `ServerRpc RequestDestroyStructure(instanceId)`. |
| Разрушение структуры (`Structure.TakeDamage`) | Сервер | `health -= damage` — клиентское. `playerController.GetAttackDamage()` — клиентское | `TakeDamage` → `internal`; `ServerRpc RequestAttackStructure(instanceId, hitPoint)`. Сервер считает урон (см. аудит 2). |
| Смерть структуры (`Structure.Die`) | Сервер | `DropAllItemsToBag`, `Destroy(gameObject)` — мир | `Die()` → `internal`, только на сервере. `ClientRpc OnStructureDestroyed` для анимации/эффектов. |
| Дроп лута (`InventoryManager.DropAllItemsToBag`) | Сервер | Спавн `LootBag` — мир | `internal`, только на сервере (см. аудит 1). |
| Открытие сундука (`ChestController.Open` → `ChestUIManager.Toggle`) | Клиент (локально) + сервер (данные) | Какой сундук открыт — локально; содержимое — серверное | `ChestUIManager.CurrentSource` — локальный. Данные (`ChestInventory.Data`) — серверные (см. аудит 1). |
| `ownerPlayerId` | Сервер | Хардкод `"player_001"` | Серверный `playerId` из `connectionId`. |
| `instanceId` (`Guid.NewGuid()`) | Сервер | Клиент не должен генерировать ID | Сервер генерирует `instanceId` и рассылает клиентам. |
| Сохранение мира (`WorldManager.SaveDirtyChunks`, `OnApplicationQuit`) | Сервер/хост | Клиент не пишет файлы мира | `Save` → no-op на клиенте. Только на сервере. |
| Загрузка мира (`WorldManager.LoadWorldAsync`, `ClearExistingStructures`) | Сервер | Клиент не грузит чанки сам | Сервер загружает чанки и рассылает `ClientRpc SpawnStructure(...)`. |
| `WorldManager.Update` (автосохранение) | Сервер | Тикает у каждого клиента | `if (!IsServer) return;` |
| `PlayerBuildMode` (превью, снапы) | Клиент | Только для глаз | Превью — локально. Серверная валидация при `RequestPlaceStructure`. |
| `PlayerBuildMode.IsPositionValid` | Клиент + сервер | Клиент для превью, сервер для финальной проверки | Клиентский `IsPositionValid` — для цвета превью. Серверный — авторитетный. |
| `StructureAttachment` | Сервер | Связь структура ↔ база | Серверное состояние; клиент получает через `ClientRpc`. |

---

## Ключевые выводы

1. **`WorldManager` — полностью локальный, пишет файлы мгновенно.** `RegisterStructure` вызывает `SaveDirtyChunks()` сразу. При коопе каждый клиент будет писать конфликтующие файлы. Нужно: `RegisterStructure` → `internal`, `SaveDirtyChunks` — по таймеру на сервере.

2. **`PlayerBuildMode` не проверяет ресурсы.** `PlaceStructure` просто `Instantiate` + `RegisterStructure`. Нет проверки, что у игрока есть предмет, нет проверки дистанции. При сети — серверный `RequestPlaceStructure` с проверкой.

3. **`PlayerBuildMode.IsPositionValid` — клиентский.** `OverlapBox` локально. При коопе два клиента могут поставить структуру в одну точку. Серверный `IsPositionValid` — авторитетный.

4. **`Structure.TakeDamage` — клиентский.** `playerController.GetAttackDamage()` — локальный. `health -= damage` — локально. При сети — серверный `RequestAttackStructure`, сервер считает урон (см. аудит 2).

5. **`Structure.Die` — клиентский.** `DropAllItemsToBag` + `Destroy(gameObject)`. При сети — серверный `Die()`, `ClientRpc` для анимации.

6. **`ownerPlayerId` — хардкод `"player_001"`.** В `RegisterStructure` и `Structure.Die`. При сети — серверный `playerId`.

7. **`instanceId` — `Guid.NewGuid()` на клиенте.** При сети — сервер генерирует и рассылает.

8. **`ClearExistingStructures` — `Destroy` по тегам при загрузке.** Второй клиент удалит структуры первого. При сети — только сервер, `ClientRpc` для клиентов.

9. **`ChestController` — публичный `Open()`.** Нет проверки дистанции/владельца. При сети — `ServerRpc RequestOpenChest(instanceId)`, сервер проверяет дистанцию.

10. **`WorldManager.Update` (автосохранение) тикает у каждого клиента.** При сети — только на сервере (`if (!IsServer) return;`).

11. **`LoadWorldAsync` — клиентский.** При сети — сервер загружает и рассылает `SpawnStructure` клиентам.

12. **`StructureAttachment` — связь структура ↔ база.** При сети — серверное состояние; клиент получает через `ClientRpc`.

---

## Переделать до сети

- [ ] `WorldManager.RegisterStructure` — `internal`, только на сервере. `SaveDirtyChunks` — не мгновенно, по таймеру.
- [ ] `WorldManager.UnregisterStructure` — `internal`, только на сервере.
- [ ] `WorldManager.Update` (автосохранение) — `if (!IsServer) return;`.
- [ ] `WorldManager.OnApplicationQuit` → `SaveAllChunks` — только на сервере.
- [ ] `WorldManager.LoadWorldAsync` / `ClearExistingStructures` — только на сервере; клиенты получают `ClientRpc SpawnStructure`.
- [ ] `WorldManager.RegisterStructure` — `ownerId` не хардкод, а серверный `playerId`.
- [ ] `WorldManager.RegisterStructure` — `instanceId` генерирует сервер, не клиент.
- [ ] `PlayerBuildMode.PlaceStructure` — `ServerRpc RequestPlaceStructure(itemId, position, rotation, parentId)`. Сервер проверяет ресурсы, дистанцию, `IsPositionValid`.
- [ ] `PlayerBuildMode.IsPositionValid` — клиентский для превью; серверный — авторитетный.
- [ ] `Structure.TakeDamage` — `internal`; `ServerRpc RequestAttackStructure(instanceId, hitPoint)`. Сервер считает урон.
- [ ] `Structure.Die` — `internal`, только на сервере. `ClientRpc OnStructureDestroyed` для анимации/эффектов.
- [ ] `Structure.Die` — `DropAllItemsToBag` только на сервере (см. аудит 1).
- [ ] `ChestController.Open` — `ServerRpc RequestOpenChest(instanceId)`. Сервер проверяет дистанцию.
- [ ] `ChestController.OnInventoryOpened/Closed` — локальное состояние клиента; данные — серверные (см. аудит 1).
- [ ] `StructureAttachment` — серверное состояние; клиент получает через `ClientRpc`.
- [ ] `WorldManager._chunks` / `_instanceMap` / `_dirtyChunks` — только на сервере. Клиент хранит только визуальные объекты.

