# 04. Todo & Bugs

> Живой трекер. Обновляется после каждого закрытого пункта.
> Долгосрочный план — в `05-roadmap.md`.

---

## 🐛 Открытые баги

> Формат: `[ ] <кратко> — файл, репро, статус`

*(пока пусто — заполнить при первой фикс-сессии)*

---

## 🔧 Доработки (мелкие, «пока руки дойдут»)

- [ ] **`CorpseManager.RemoveAliveComponentsFromCreatureCorpse`** — проверить, что при загрузке трупа `Animator` не остаётся включённым (сейчас `Destroy(anim)`, а `HealthMaterialBlender` требует, чтобы кости были в T-позе).
- [ ] **`PlayerEquipment.Unequip` при поломке инструмента** — сейчас вызывается в `OnAttackInteractFinished` при `durability <= 0`, но `ItemHandler.DestroyItem` может не сработать (нет проверки).
- [ ] **`LootBag.CreateLootBagFromItems`** — если `items.Count == 0`, лог `Debug.LogWarning` не должен ронять создание. Проверить.
- [ ] **`InventoryUI.RefreshUI`** — при `inventoryManager.equipment == null` NRE. Добавить проверку.
- [ ] **`EntityInfoPanel`** — torpor bar скрывается, если `torpor == 0 && !knockedOut` (сейчас всегда активен, если не скрыт вручную).
- [ ] **`ChestController.OnDestroy`** — проверить, что не удаляет `Chest_*.save` при выходе в MainMenu (должно работать через `IsCorpseInventory`, но `ChestController` — не труп).
- [ ] **`Structure.Die`** — `DropAllItemsToBag` вызывает `InventoryManager.Instance`, но менеджер может быть `null` при загрузке мира. Защитить.
- [ ] **`CreatureKnockout.KnockOut`** — проверить, что при повторном нокауте не создаётся второй `ChestInventory` (сейчас `if (_creature.Interactable.GetInventory() == null)`).
- [ ] **`TamingManager.SpawnFromData`** — `creature.Interactable` может быть `null` до `Awake`, проверить порядок.
- [ ] **`PlayerInteraction.ProcessHitObject`** — при попадании в `Corpse` не должен добавлять `Creature` в `_allTargets`.

---

## 🚧 В работе (текущий спринт)

- [ ] **`SettingsController`** — единая система настроек (MainMenu + Pause).
  - Префаб UI + скрипт.
  - Хранение в JSON (`persistentDataPath/settings.json`).
  - Сюда переедет `splitStackKey` (сейчас захардкожен `LeftShift` в `InventorySlotUI`).
  - Громкость (уже есть `AudioManager.masterVolume`, но UI — только в `SettingsPanelController` в Pause, в MainMenu — нет).
  - Качество графики, разрешение, язык (частично есть в `LanguageDropdownController`).

---

## 🧊 Отложено

- [ ] **«Кто ударил игрока»** — для `AttackOwnerTarget` (сейчас ищет «враг рядом с игроком»).
- [ ] **Пищевая цепочка** — дикие бьют диких.
- [ ] **`Creature.cs`** — рефакторинг (файл раздут ~600 строк; AI/боевка/приручение/анимация смешаны — часть уже вынесена в компоненты, но `Update` ещё толстый).
- [ ] **`FindWildTarget` каждый кадр** — `OverlapSphere` в `Update`. Оптимизация: раз в 0.2 сек.
- [ ] **`LootBagManager.CreateLootBagFromItems`** — `bagInventory.Initialize(items.Count, ...)`. Если `items.Count > 12` (стандартный размер сумки), часть предметов не влезет. Проверить/увеличить.
- [ ] **UI заголовок чужого инвентаря** — показать `OwnerPlayerId` / имя существа в `PanelsUIController`.
- [ ] **`EntityInfoPanel`** — показывать владельца для `LootBag` и `Corpse`.

---

## 📋 Ближайшее (следующая сессия)

*(перезаполняется перед каждым чатом)*

---

## ✅ Закрыто (архив последних 10)

> Полная история — в git log. Здесь только чтобы помнить контекст.

- [x] Радиальное меню для структур, трупов, сундуков.
- [x] `ChestUIManager` — единая точка Open/Close.
- [x] Сброс слотов в `ChestUI.Close`.
- [x] `ForceResetInteract` при закрытии радиального меню.
- [x] Сортировка канвасов (RadialMenu = 1, HUD = 0).
- [x] Riding (mount/dismount, TickRidden, ForceDismount).
- [x] Инфо-панель существ (EntityInfoPanelManager).
- [x] Damage numbers (pool).
- [x] CreatureHitZone (крит/броня).
- [x] PlayerDamageFeedback (тряска + звук + вспышка).
- [x] Оптимизация рендера: починены LOD-переходы, отключены тени на дальних LOD'ах (скрипт TreeOptimizer.cs), нагрузка на GPU снижена с 10.8ms до 4.0ms.