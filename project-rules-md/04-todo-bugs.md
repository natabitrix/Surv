# 04. Todo & Bugs

> Живой трекер. Обновляется после каждого закрытого пункта.
> Долгосрочный план — в `05-roadmap.md`.

---

## Сетевая гигиена (до подключения NGO)

→ Полный чеклист: `network/07-refactor-checklist.md`.

## 🐛 Открытые баги

> Формат: `[ ] <кратко> — файл, репро, статус`

- [ ] **Drag из сумки (LootBag) в инвентарь** — панель закрывается, иконка висит, предмет не добавляется. Repro: открыть сумку → перетащить предмет в инвентарь. Вероятно: `ChestUI.Instance` уничтожается при `ChestUIManager.Close()`, `GetOriginalSlot()` возвращает `null`, `ReturnToOriginalSlot` не возвращает. Не связано с Шагом 1/2/3. Чинить после.
- [ ] **Крафт: ресурсы только в хотбаре → рецепт серый (неактивен)** — `CraftingSlotUI.RefreshVisual`/`TryCraft` не видят `hotbarInventoryData` или подписка на `hotbarInventoryData.OnInventoryChanged` не установлена. Закрывается в Шаге 3.4 (CraftingUI на хаб) — **проверить, не закрылся ли уже**.
- [ ] **Крафт: ресурсы разделены между main и hotbar → рецепт серый** — `HasIngredients` на каждом `InventoryData` отдельно, сумма не проверяется. Нужен `PlayerProgress.HasIngredientsTotal`/`ConsumeIngredientsTotal`. Баг 12b.
- [ ] **Атака на зажатие E** — должна повторяться, пока зажата. Сейчас `OnAttackInteractFinished` из Animation Event, повтора нет.
- [ ] **«Выбросить всё» из сумки создаёт новую сумку** — `InventoryManager.DropItemsFromChest` → `DropAllItemsToBag` всегда создаёт сумку. Нужно: если источник — `LootBag`, просто очистить инвентарь + `ForceDespawn` (не создавать новую). Недоработка.
- [ ] **`CorpseManager.RemoveAliveComponentsFromCreatureCorpse`** — проверить, что при загрузке трупа `Animator` не остаётся включённым (сейчас `Destroy(anim)`, а `HealthMaterialBlender` требует, чтобы кости были в T-позе).
- [ ] **`PlayerEquipment.Unequip` при поломке инструмента** — сейчас вызывается в `OnAttackInteractFinished` при `durability <= 0`, но `ItemHandler.DestroyItem` может не сработать (нет проверки).
- [ ] **`LootBag.CreateLootBagFromItems`** — если `items.Count == 0`, лог `Debug.LogWarning` не должен ронять создание. Проверить.
- [ ] **`InventoryUI.RefreshUI`** — при `inventoryManager.equipment == null` NRE. Добавить проверку.
- [ ] **`EntityInfoPanel`** — torpor bar скрывается, если `torpor == 0 && !knockedOut` (сейчас всегда активен, если не скрыт вручную).
- [ ] **`ChestController.OnDestroy`** — проверить, что не удаляет `Chest_*.save` при выходе в MainMenu (должно работать через `IsCorpseInventory`, но `ChestController` — не труп).
- [ ] **`Structure.Die`** — `DropAllItemsToBag` вызывает `InventoryManager.Instance`, но менеджер может быть `null` при загрузке мира. Защитить.
- [ ] **`Corpse.Awake` → `_creature` == null** — при ручном добавлении `Corpse` (игрок) `_breakEffect` берётся из инспектора, но в коде `_breakEffect = _creature.DamageEffect;` в блоке `if (_creature != null)`. Проверить, что для игрока `breakEffect` заполняется вручную.
- [ ] **`Corpse` поля `centerBone` / `interactionAnchor`** — сейчас закомментированы в `Awake`. Если они нужны для стабильного рейкаста/таскания, вернуть логику. Если нет — удалить из класса, чтобы не путали.
- [ ] **`Creature.Awake` → `AddComponent<Corpse>()`** — передать ссылки из `Creature` в `Corpse` (сейчас `Corpse.Awake` сам берёт `CorpseDragRigidbody` / `InteractionCollider` из `Creature`, но порядок `AddComponent` и `Awake` может сыграть злую шутку). Убедиться, что `_creature` в `Corpse` заполняется до первого использования.
- [ ] **`Creature.Awake` → `AddComponent<RagdollController>()`** — передать `RagdollControllerRagdollSettings` в `RagdollController.ragdollSettings`. Сейчас поле есть в `Creature`, но `RagdollController` его не получает (остаётся `null`).
- [ ] **`CreatureRiding`** — `CreatureRidingRiderAttachPoint` и `CreatureRidingDismountOffset` теперь в `Creature`, но `CreatureRiding` их не получает. Нужен `Initialize(Transform attachPoint, Vector3 dismountOffset)`.
- [ ] **`CreatureInteractable`** — `InteractionCollider` в `Creature` и `CreatureInteractable._interactionCollider` — это **два разных поля**. В `Creature.Awake` после `AddComponent<CreatureInteractable>()` нужно передать ссылку (публичный сеттер или `Initialize`).

---

## 🔧 Доработки (мелкие, «пока руки дойдут»)

*(перенесено в «Открытые баги» — см. выше)*

---

## 🚧 В работе (текущий спринт)

- [ ] **Фикс-сессия после рефакторинга Creature** — проверить, что все автодобавляемые компоненты получают нужные ссылки из `Creature` (см. «Открытые баги»). Без этого `CreatureInteractable`, `RagdollController`, `CreatureRiding`, `Corpse` могут работать некорректно.
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

- [ ] **Шаг 3.2 (`08-refactor-plan-nplayers.md`)** — `InventoryManager` + `ChestUIManager` per-player (Scoped-состояние). Или сначала закрыть `Creature`-фиксы (см. «В работе»).

---

## ✅ Закрыто (архив последних 10)

> Полная история — в git log. Здесь только чтобы помнить контекст.

- [x] **Шаг 3 (N-player): PlayerProgress per-player.** `PlayerId`, `_progressByPlayerId`, `GetPlayerProgress`, `Player_{playerId}.save`. `PlayerProgress.LoadData`/`Save` — через свой `_playerController`. `InventoryManager` — `playerId` из `_progress.PlayerId`. `PlayerSurvivalSystem.OnPlayerDeathRoutine` — свой `_progress.playerController` + `_progress.PlayerId`. `Creature` — ленивые `playerProgress`/`survivalSystem` + `Start`-корутина ожидания `GameAssets`. `PlayerController.LocalPlayer` переизбирается в `OnDestroy`. `PlayerInputHandler`/`PlayerInteraction`/`AnimationEvents` — фильтр болванчика. `PlayerController.Attack` — только у `LocalPlayer`. `CreatureCombat.DealDamageToTarget` — бьёт `_wildTargetTransform`, fallback на `LocalPlayer`. `Creature.Awake` — `AddComponent` (для `TamingManager`), `Start` — `_data`/статы.
- [x] **Шаг 3 (N-player): `PlayerSurvivalSystem`.** `TogglePlayerActive` не выключает `gameObject` (иначе корутина смерти умирает), скрывает `VisualCharacter`. `OnPlayerDied` — экран смерти только у LocalPlayer.
- [x] **Шаг 3 (события инвентаря): `InventoryEventsHub`.** Глобальный хаб, `Notify(kind)` из `PlayerProgress`/`InventoryManager`/`EngramUI.UnlockRecipe`. UI подписываются только на хаб.
- [x] **Шаг 2 (события инвентаря): `InventoryManager.Notify`.** `HandleStructurePlaced`, `DropItemFromSlot`, `DropAllItemsToBag`, `TryRepairItem`.
- [x] **Шаг 1 (события инвентаря): `InventoryData`/`EngramData` не пересоздаются.** `Clear(newSize)` / `Reset()`, `ReloadFromFile` → `OnPlayerLoaded`. `InventoryUI.OnEnable` → `RefreshUI`. Закрывает баги 3, 9, 16 (частично).
- [x] **UI на хаб:** `InventoryUI`, `HotbarUI`, `EngramUI`, `CraftingUI`/`CraftingSlotUI` — подписка на `InventoryEventsHub.OnChanged`, не на `InventoryData`. `CraftingSlotUI` — ленивые `_progress.mainInventoryData`/`hotbarInventoryData`.
- [x] **E на трупе существа не тащил тело** — `CreatureInteractable` оставался `enabled` и перехватывал `OnInteractFinished`. Фикс: `PlayerInteraction.PickPrimaryInteractTarget()` с приоритетом `Corpse`, фильтр в `ProcessHitObject`, `Destroy(CreatureInteractable)` в `CorpseManager.RemoveAliveComponentsFromCreatureCorpse`.
- [x] **Упрощение создания существ**: `Creature` добавляет все подсистемы через `AddComponent` в `Awake`. На префабе — только `Animator`, `NavMeshAgent`, `Creature`.
- [x] **Единый источник данных**: `CreatureData` — единственный источник правды для статов, звуков, `impactType`. Дублирующие поля из `Creature` удалены.
- [x] **Фикс звука удара**: `PlayerInteraction` ищет `IImpactSoundProvider` на `Creature`, а не на `Corpse`. `Corpse.GetImpactType()` читает из `CreatureData`.
- [x] **Прокси-методы Animation Events**: `SoundOnAttack`, `SoundOnTakeDamage`, `SoundOnDeath`, `SoundOnFootstep`, `DealDamageToTarget` перенесены в `Creature`.
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