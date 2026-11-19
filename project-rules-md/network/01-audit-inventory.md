# Аудит 1: Инвентарь игрока

> Система: инвентарь игрока (hotbar + main), сундуки, трупы, сумки.
> Файлы: `InventoryData.cs`, `InventoryManager.cs`, `ChestInventory.cs`, `ChestUIManager.cs`, `PlayerProgress.cs`, `PlayerEquipment.cs`.
> Статус: ✅ аудит завершён.

---

## Цепочка владения
PlayerProgress (глобальный, DontDestroyOnLoad)
├── hotbarInventoryData : InventoryData (10)
├── mainInventoryData : InventoryData (100)
└── AddItemToPlayerInventory(item, amount, durability) ← «входная точка» пополнения
├── hotbarSlotMap[item.Id] → закреплённый слот хотбара
└── mainInventoryData.AddItemAnywhere(...) ← но AddItemAnywhere сам зовёт PlayerProgress.Instance.Save()

InventoryManager (сценовый)
├── DropItemFromSlot / DropAllItemsToBag → LootBagManager.Instance.CreateLootBagFromItems(...)
├── MoveAllToChest / MoveAllToPlayer
├── TryRepairItem → PlayerProgress.Instance.mainInventoryData
└── EquipSavedEquippedItem / SaveEquippedItem

ChestUIManager (сценовый) — CurrentSource (IInventorySource), Open/Close/Toggle
ChestInventory (на сундуках/трупах/существах)
└── Data : InventoryData; Save()/Load() для не-corpse; OnDestroy → Save()

InventoryData (чистые данные, НЕ MonoBehaviour)
├── AddItemAnywhere → PlayerProgress.Instance.Save(...) ← ЛОВУШКА
├── RemoveItemFromSlot / ClearSlot / NotifyChanged
├── TransferAllTo / ConsumeIngredients / ConsumeRepairIngredients
└── ToSerializable / FromSerializable


**Главная особенность:** данные инвентаря — это чистый C# (`InventoryData`), но `AddItemAnywhere` сам вызывает `PlayerProgress.Instance.Save(...)` и `NotifyChanged()`. То есть мутация данных неотделима от сохранения на диск и от UI-уведомления.

---

## Вопрос А. Что при одновременном действии двух игроков?

| Сценарий | Что произойдёт в текущем коде |
|---|---|
| Два игрока дропают предмет в один сундук одновременно | `ChestInventory.Data` — один объект. `AddItemAnywhere` не атомарен: два вызова подряд в одном кадре дадут корректную сумму, но если между ними `Save()` — возможна перезапись файла. В коопе с разными клиентами — классический lost update. |
| Один игрок дропает, другой подбирает с земли | `DropItemFromSlot` создаёт `LootBag` через `LootBagManager.Instance` (singleton). Подбор идёт через `PlayerInteraction` → в `InventoryData.AddItemAnywhere`. Два клиента могут «подобрать» один и тот же стак. |
| Два игрока чинят один предмет в общем сундуке | `TryRepairItem` читает `slot.currentDurability`, потребляет ресурсы, пишет `maxDurability`. Между чтением и записью — окно для дюпа/потери ресурсов. |
| Один игрок перекладывает из сундука, другой — в тот же сундук | `TransferAllTo` итерируется по `sourceData.slots` и вызывает `target.AddItemAnywhere` в цикле. Не транзакционно. |
| Один игрок экипирует предмет, другой в это время дропает его из общего сундука | `PlayerEquipment.Equip` не связан с источником слота. Если предмет «экипирован», а слот очищен — `GetCurrentEquippedSlot()` вернёт слот, где уже `null`. |

**Вывод:** данные инвентаря не рассчитаны на конкурентный доступ. Все операции read-modify-write.

---

## Вопрос Б. Может ли клиент соврать?

Сейчас клиент = единственный авторитет. Все проверки — «доверяющие»:

| Действие | Где проверка | Можно ли соврать |
|---|---|---|
| Добавить предмет | `PlayerProgress.AddItemToPlayerInventory(item, amount)` — публичный метод, любой код может звать с любым `Item` и `amount` | Да, тривиально: `PlayerProgress.Instance.AddItemToPlayerInventory(item, 9999)` |
| Изменить прочность | `InventorySlot.currentDurability` — публичное поле (`InventorySlot` не показан, но `slot.currentDurability = ...` пишется напрямую в `ChestUI`, `TryRepairItem`, `HandleStructurePlaced`) | Да, прямое присваивание |
| Очистить слот | `InventoryData.ClearSlot(index)` — публичный, без контекста «кто и почему» | Да |
| Забрать из сундука | `ChestUI.MoveAllToPlayer()` → `chestUI.MoveAllToPlayer()` → `target.AddItemAnywhere` | Да, нет проверки дистанции/владельца |
| Экипировать | `PlayerEquipment.Equip(item, slotIndex)` — публичный, не проверяет, что `slotIndex` соответствует слоту, где реально лежит `item` | Да: можно экипировать предмет, которого нет в инвентаре (просто передать `Item` из базы) |
| Скрафтить | `InventoryData.ConsumeIngredients(recipe)` вызывается из UI (не показан) | Да, если UI-код вызывает напрямую, минуя проверку уровня/энграмм |
| Изучить энграмму | `PlayerProgress.TrySpendEngramPoints(cost)` — публичный, не проверяет, что `cost` соответствует реальной энграмме | Да, можно передать `cost = 0` |
| Распределить стат | `PlayerProgress.AllocateStatPoint(stat)` — проверяет только `StatPointsAvailable > 0` | Частично: нельзя накрутить очки, но можно накликать, если UI не заблокирован (в коде нет блокировки от повторного вызова в одном кадре) |
| Добавить XP | `PlayerProgress.AddExperience(amount)` — публичный | Да |

**Вывод:** клиент полностью авторитетен. Для сети нужен серверный слой, который валидирует: наличие предмета, дистанцию до сундука, владельца слота, уровень для крафта, реальный `cost` энграммы.

---

## Вопрос В. Насколько критична задержка?

| Операция | Критичность задержки | Комментарий |
|---|---|---|
| Показ предмета в UI (иконка/стак) | Низкая | Можно клиентски, но с серверной коррекцией |
| Перекладывание предметов drag&drop | Средняя | Нужен «оптимистичный» локальный показ + серверный откат при отказе |
| Скрафт | Средняя | Время крафта пока нет (мгновенно), при сети — серверное подтверждение обязательно |
| Дроп предмета | Средняя | Спавн `LootBag` — серверный, клиент только просит |
| Экипировка | Низкая | Локально можно мгновенно, сервер проверяет владение |
| Ремонт | Низкая | Редко, можно ждать подтверждения |
| Смерть/потеря инвентаря | Высокая | Сервер решает, что выпало и куда |

**Вывод:** большинство операций инвентаря терпимы к задержке, но требуют серверной валидации перед применением.

---

## Таблица разметки

| Система | Кто авторитетен | Почему | Что переделать |
|---|---|---|---|
| Инвентарь: добавление | Сервер | Клиент не решает, сколько у него предметов | `PlayerProgress.AddItemToPlayerInventory` → ServerRpc; убрать `Save()` из `AddItemAnywhere` |
| Инвентарь: удаление/очистка | Сервер | Удаление без валидации = дюп | `RemoveItemFromSlot` / `ClearSlot` → private; клиент шлёт Request |
| Инвентарь: перенос | Сервер + клиентский оптимизм | Конкурентный доступ к сундукам | Серверный `MoveItem(from, fromSlot, to, toSlot, count)` |
| Инвентарь: дроп | Сервер | Спавн LootBag — мир | `CreateLootBagFromItems` только на сервере |
| Инвентарь: экипировка | Клиент + валидация | Визуал локально, владение — сервер | `Equip` → локально + `ServerRpc EquipRequest(slotIndex)` |
| Инвентарь: ремонт | Сервер | Потребление ресурсов + прочность | `RequestRepair(slotIndex, owner)` на сервере |
| Крафт | Сервер | `TrySpendEngramPoints(cost)` без валидации | `RequestCraft(recipeId)` на сервере |
| Сохранение на диск | Сервер/хост | Клиент не пишет в файлы мира | `Save` → no-op на клиенте; убрать `Save()` из `AddItemAnywhere` |
| Прочность | Сервер | Публичное поле | `currentDurability` → `private set` + серверный сеттер |
| Уведомления UI | Клиент (косметика) | Только для глаз | Вынести `NotificationManager` из `InventoryData` |

---

## Переделать до сети

- [ ] `InventoryData.AddItemAnywhere` — убрать `PlayerProgress.Instance.Save()` (ответственность вызывающего).
- [ ] `InventoryData.RemoveItemFromSlot` / `ClearSlot` — `internal`, доступ через `InventoryManager`.
- [ ] `PlayerProgress.AddItemToPlayerInventory` — сделать `[ServerRpc]`-совместимым (пока — `internal` + флаг `SessionMode.IsMultiplayer`).
- [ ] `InventorySlot.currentDurability` — `private set` + сеттер в `InventoryData`.
- [ ] `PlayerEquipment.Equip` — валидировать `slotIndex` ↔ `item`.
- [ ] `PlayerProgress.TrySpendEngramPoints` — валидировать `cost` по рецепту.
- [ ] `PlayerProgress.AllocateStatPoint` — защита от повторного вызова в одном кадре (флаг `_isAllocating`).
- [ ] `NotificationManager.Instance.Show(...)` из `InventoryData` / `InventoryManager` — заменить на события.
- [ ] `ChestInventory.Save()` / `PlayerProgress.Save()` — no-op при `SessionMode.IsMultiplayer && !IsServer`.
- [ ] `LootBagManager.Instance.CreateLootBagFromItems` — вызывать только на сервере (флаг `SessionMode.IsMultiplayer`).
- [ ] `InventoryUI` / `HotbarUI` — подписать на `InventoryData.OnInventoryChanged`, а не на прямые вызовы `RefreshAllUIs` из `InventoryManager`.
- [ ] `ChestUIManager.CurrentSource` — при сети должен быть локальным состоянием клиента (какой сундук открыт у меня), но данные — серверные.