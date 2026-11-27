# Чеклист «переделать до сети»

> Сводный список. Собирается из аудитов (`0X-audit-*.md`).
> Приоритет 1 — блокеры для коопа. Приоритет 2 — важно, но не блокер первого коопа. Приоритет 3 — можно после первого коопа.
> Формат: `- [ ]` — не сделано, `- [x]` — сделано, `- [~]` — отложено.
>
> **Обновлено после N-player рефакторинга (Шаги 1–3) и пунктов 2, 4, 5, 6, 7, 9, 10.**
> Закрытые пункты — `[x]` с пометкой что именно сделано. Отложенные — `[~]` с причиной.

---

## Приоритет 1 — блокеры коопа

### Инвентарь (из `01-audit-inventory.md`)

- [x] `InventoryData.AddItemAnywhere` — убрать `PlayerProgress.Instance.Save()` (ответственность вызывающего). **Сделано:** `Save` убран, `Notify` — на уровне `PlayerProgress`/`InventoryManager`.
- [x] `InventoryData.RemoveItemFromSlot` / `ClearSlot` — `internal`, доступ через `InventoryManager`. **Сделано (Пункт 2):** методы `internal`.
- [ ] `PlayerProgress.AddItemToPlayerInventory` — сделать `[ServerRpc]`-совместимым (пока — `internal` + флаг `SessionMode.IsMultiplayer`).
- [x] `InventorySlot.currentDurability` — `private set` + сеттер в `InventoryData`. **Сделано (Пункт 4):** `public get; internal set;`.
- [x] `PlayerEquipment.Equip` — валидировать `slotIndex` ↔ `item`. **Сделано (Пункт 5):** проверка слота перед экипировкой, `slotIndex == -1` (Unequip) — без проверки.
- [x] `PlayerProgress.TrySpendEngramPoints` — валидировать `cost` по рецепту. **Сделано (Пункт 6):** сигнатура `TrySpendEngramPoints(Recipe)`, `cost` из `recipe.engramPointsCost`.
- [x] `PlayerProgress.AllocateStatPoint` — защита от повторного вызова в одном кадре. **Сделано (Пункт 7):** cooldown 0.2 сек (`_lastAllocateTime`).
- [~] `NotificationManager.Instance.Show(...)` из `InventoryData` / `InventoryManager` — заменить на события (данные не должны звать UI). **Отложено (Пункт 8):** не блокер коопа, `NotificationManager` локальный. На сервере — не критично.
- [x] `ChestInventory.Save()` / `PlayerProgress.Save()` — no-op при `SessionMode.IsMultiplayer && !IsServer`. **Сделано (Пункт 9):** `SessionMode.IsServer` + проверка в начале `Save`.
- [x] `LootBagManager.Instance.CreateLootBagFromItems` — вызывать только на сервере. **Сделано (Пункт 10):** `if (SessionMode.IsMultiplayer && !SessionMode.IsServer) return null;`.
- [x] `InventoryUI` / `HotbarUI` — подписать на `InventoryData.OnInventoryChanged`, а не на прямые вызовы `RefreshAllUIs` из `InventoryManager`. **Сделано:** подписаны на `InventoryEventsHub.OnChanged` (глобальный хаб).
- [ ] `ChestUIManager.CurrentSource` — при сети должен быть локальным состоянием клиента (какой сундук открыт у меня), но данные — серверные. **Сейчас:** `CurrentSource` один на клиента, не per-player. **Следующий пункт (11).**

### Урон и смерть (из `02-audit-damage.md`)

- [ ] `Creature.TakeDamage` — сделать `internal`, вход только через серверный `RequestAttack`.
- [ ] `Creature.ReduceHealth` — `internal`; `Health` setter → серверный.
- [ ] `Creature.RequestDeath` / `Creature.Die` — `RequestDeath` → ServerRpc; `Die()` → `internal`.
- [ ] `CreatureDeath.Die` — добавить флаг `_isDying` (идемпотентность); сейчас двойной вызов возможен.
- [ ] `CreatureCombat.TakeDamage` — разделить на: (1) серверный расчёт (damage + зона + HP), (2) клиентские эффекты (анимация, цифры, партиклы) через события.
- [ ] `CreatureCombat.TakeDamage` — убрать `PlayDamageEffect` (Instantiate) из общей функции; вынести в клиентский обработчик.
- [ ] `DamageNumberPool.Instance.ShowDamage` — вызывать из клиентского обработчика события, а не из `TakeDamage`.
- [ ] `Creature.GetDamageMultiplier(col)` — при сети клиент не передаёт `col`; сервер сам ищет зону по `hitPoint`.
- [ ] `Creature.AddTorpor` — `internal`; `torpor` → `NetworkVariable` (или серверный сеттер + репликация).
- [ ] `CreatureKnockout.KnockOut` — `internal`; вход через `ServerRpc`.
- [ ] `CreatureCombat.NotifyAttackedBy` — `internal`; только сервер.
- [ ] `CreatureCombat.LastAttacker` — серверный геттер; клиенту не нужен.
- [ ] `CreatureCombat._lastAttackTime` — серверный; клиентский кулдаун только для анимации.
- [ ] `PlayerSurvivalSystem.TakeDamage` — `internal`; вход через серверный метод; `Health` → `NetworkVariable`.
- [x] `CreatureCombat.DealDamageToTarget` — верхом: `OverlapBox` на сервере; клиент — только анимация. **Частично:** бьёт `_wildTargetTransform` (не `LocalPlayer`), fallback на `LocalPlayer`. Для сети — `_wildTargetTransform` должен быть `NetworkObject`.
- [ ] `CreatureCombat.FindWildTarget` / `FindNearestEnemy` / `FindEnemyNearPlayer` — только сервер (AI).
- [ ] `CreatureCombat.PerformAttack` / `AttackFromRider` — серверный триггер анимации через `ClientRpc` / `NetworkAnimator`.
- [ ] `Creature.UpdateAnimation` — при сети заменить на `NetworkAnimator` (параметры Speed/IsMoving реплицируются).
- [ ] `Creature._wildAI.Tick` / `_tamedAI.Tick` — только на сервере (флаг `if (!IsServer) return;`).
- [ ] `CreatureCombat._isAttackingPlayer` — серверное состояние; клиент не должен влиять.
- [ ] `CreatureCombat.ChaseAndAttack` — только сервер (управляет `NavMeshAgent`).
- [ ] `CreatureCombat.UpdateCurrentEnemy` / `ClearTargets` — только сервер.

### Игроки и сессия (из `03-audit-players.md`)

- [x] `PlayerController.Awake` — убрать `Destroy(gameObject)` для дубликата; `Instance` → `LocalPlayer` (только свой) + `AllPlayers` (список). **Сделано:** `LocalPlayer` + `AllPlayers`, `Destroy` убран, `LocalPlayer` переизбирается в `OnDestroy`.
- [x] `PlayerSurvivalSystem.Awake` — убрать `Destroy(gameObject)`; `Instance` → per-player. **Сделано:** `_progress = GetComponent<PlayerProgress>()`, `Destroy` убран.
- [x] `PlayerProgress.Instance` — per-player: `GetPlayerProgress(playerId)`. Сейв — `Player_{playerId}.save`. **Сделано:** `_progressByPlayerId`, `GetPlayerProgress`, `Player_{playerId}.save`.
- [x] `InventoryManager.Instance` — per-player: `GetInventoryManager(playerId)`. **Сделано:** `Instance` убран, компонент на игроке, `_progress` через `GetComponent`.
- [x] `PlayerProgress.RegisterPlayerController` — регистрация по `playerId`, не по singleton. **Сделано:** `_playerController` — свой, `RegisterPlayerController(this)`.
- [x] `PlayerProgress.RegisterInventoryManager` — то же. **Сделано:** `RegisterInventoryManager(this)`.
- [x] `PlayerSurvivalSystem.OnPlayerDeathRoutine` — `ReloadFromFile` только для умершего игрока (серверный). **Сделано:** `_progress.playerController` (свой), `_progress.PlayerId`.
- [x] `PlayerProgress.Save` — no-op на клиенте; per-player файл на сервере. **Сделано:** per-player файл + no-op на клиенте (Пункт 9).
- [ ] `GameTime.Now` — серверное время; синхронизация часов с сервером.
- [ ] `GameTimeUpdater` — только на сервере.
- [ ] `SessionMode.IsMultiplayer` — серверный флаг; `GameSettings.isMultiplayer` — только дефолт.
- [x] `GameAssets.CreateGlobalManagers` — развести: глобальные (GameController, GameTime) на сервере; per-player (PlayerProgress, InventoryManager) на каждого. **Сделано:** `PlayerProgress` не создаётся в `GameAssets` (компонент на игроке).
- [ ] `PlayerController.transform` — client-side prediction + `NetworkTransform` + серверная валидация скорости.
- [ ] `PlayerController.Attack` — `ServerRpc RequestAttack(targetId, hitPoint)`; сервер считает урон.
- [ ] `PlayerSurvivalSystem.TakeDamage` — `internal`, вход через серверный метод; `Health` → `NetworkVariable`.
- [ ] `PlayerSurvivalSystem.Update` — проверка смерти только на сервере.
- [x] `PlayerInputHandler` / `PlayerInput` — только на локальном игроке (не на сервере). **Сделано:** болванчик отфильтрован (`IsLocalPlayer()`, `PlayerInput.enabled = false`).
- [ ] `PlayerProgress.AllocateStatPoint` / `TrySpendEngramPoints` — серверные (см. аудит 1). **Частично:** `UpdateEngramAvailability` вызывается, но `ServerRpc` — нет.

### Позиция и движение (из `04-audit-position.md`)

- [ ] `PlayerController.transform` — client-side prediction + `NetworkTransform` (клиент-авторитарный).
- [ ] `PlayerController.Move` / `Swim` / `OnLadder` — локально, но сервер валидирует `maxSpeed`, `Grounded`, `_isInWater`, `_onLadder`.
- [ ] `PlayerController._verticalVelocity` — предсказание; сервер проверяет, что игрок не «летает».
- [ ] `PlayerController.SetMounted` — `IsMounted` реплицируется (другие клиенты видят игрока в седле).
- [ ] `RidingController.Mount` — `ServerRpc RequestMount(creatureId)`; сервер проверяет: приручено, владелец, дистанция.
- [ ] `RidingController.Dismount` — `ServerRpc RequestDismount`; сервер находит позицию через `NavMesh.SamplePosition` и телепортирует.
- [ ] `RidingController.OnApplicationQuit` / `OnDestroy` — `ServerRpc ForceDismount`; сервер телепортирует игрока.
- [ ] `CreatureRiding.TickRidden` — только сервер; клиент шлёт `ServerRpc SendRideInput(move, look, sprint)`.
- [x] `PlayerProgress.LateUpdate` (`_lastKnownPlayerPosition`) — только на сервере. **Сделано:** per-player `_lastKnownPlayerPosition` на `PlayerProgress` (своём).
- [ ] `PlayerProgress.Save` — per-player файл; на клиенте no-op (см. аудит 3). **Сделано (Пункт 9):** no-op на клиенте.
- [ ] `GameTime.Now` — серверное время; синхронизация часов с клиентом.
- [ ] `GameTimeUpdater` — только на сервере (см. аудит 3).
- [ ] `PlayerController.transform.position` — серверная валидация телепорта (максимальное смещение за тик).
- [ ] `RidingController._currentMount` — серверное состояние; клиент только отображает.

### Мир и строительство (из `05-audit-world.md`)

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

### Существа и AI (из `06-audit-creatures.md`)

- [ ] `Creature.Update` — `if (!IsServer) return;` в начале. Клиент только отображает. **Частично:** есть `if (_data == null || _taming == null) return;`, но `!IsServer` — нет.
- [ ] `Creature.UpdateAnimation` — заменить на `NetworkAnimator` (параметры Speed/IsMoving/Attack/TakeDamage/Death).
- [ ] `Creature.torpor` — `NetworkVariable<float>`; `torpor -= ...` — только на сервере.
- [ ] `Creature.food` — `NetworkVariable<float>`; `_taming.Tick()` — только на сервере.
- [ ] `Creature.Health` — `NetworkVariable<float>` (см. аудит 2).
- [ ] `Creature.SetHealth` / `SetFood` / `SetStamina` / `ReduceHealth` / `SetDead` — `internal`; только на сервере.
- [ ] `Creature.tamed` / `knockedOut` — `NetworkVariable<bool>`; изменение — только на сервере.
- [ ] `Creature.tamingProgress` — `NetworkVariable<float>`.
- [ ] `Creature.tamedBehavior` / `tamedFollowDistance` / `isFollowing` / `isWandering` / `isMating` — `NetworkVariable`; изменение — через `ServerRpc ExecuteCommand`.
- [ ] `Creature.AddTorpor` / `AddFood` / `AddTamingProgress` / `ConsumeFood` / `Unclaim` — `internal`; только на сервере.
- [ ] `Creature.KnockOut` / `KnockOutFromLoad` / `RecoverFromKnockout` — `internal`; только на сервере.
- [ ] `Creature.Die` / `RequestDeath` — `internal`; только на сервере.
- [ ] `Creature.PlayerTransform` — серверный (ближайший игрок / владелец).
- [ ] `Creature.SetCreatureLayer` — только на сервере.
- [ ] `CreatureWildAI.Tick` — `if (!IsServer) return;` в начале.
- [ ] `CreatureTamedAI.Tick` — `if (!IsServer) return;` в начале.
- [ ] `CreatureWildAI.LateUpdate` / `CreatureTamedAI.LateUpdate` — `if (!IsServer) return;` (или убрать, если не нужно).
- [ ] `Agent.SetDestination` / `Agent.isStopped` / `Agent.speed` — только на сервере (внутри AI).
- [ ] `CreatureDeath.Die` — `internal`; только на сервере. `ClientRpc OnCreatureDied(netId)` для анимации.
- [ ] `CreatureDeath.CreateCorpse` — только на сервере. `CorpseManager.RegisterCorpse` — `internal`.
- [ ] `CreatureDeath.SetBodyRagdoll` — только на сервере (или `ClientRpc` для визуала).
- [ ] `CreatureKnockout.KnockOut` — `internal`; только на сервере. `ClientRpc OnCreatureKnockedOut(netId)`.
- [ ] `CreatureKnockout.KnockOutFromLoad` / `ExitKnockoutState` — только на сервере.
- [ ] `CreatureKnockout.SetBodyRagdoll` — только на сервере (или `ClientRpc`).
- [ ] `TamingManager.RegisterCreature` / `UpdateSave` / `UnregisterCreature` — `internal`; только на сервере.
- [ ] `TamingManager.SaveAll` / `OnApplicationQuit/Pause/Focus` — только на сервере.
- [ ] `TamingManager.LoadAllTamingCreaturesAsync` — только на сервере. Клиенты получают `ClientRpc OnCreatureSpawned`.
- [ ] `TamingManager.SpawnFromData` — только на сервере.
- [ ] `CorpseManager.RegisterCorpse` / `UnregisterCorpse` — `internal`; только на сервере.
- [ ] `CorpseManager.LoadAllCorpsesAsync` — только на сервере. Клиенты получают `ClientRpc SpawnCorpse`.
- [ ] `CorpseManager.SpawnCorpseFromData` — только на сервере.
- [ ] `CorpseManager.RemoveAliveComponentsFromCreatureCorpse` — только на сервере.
- [ ] `CorpseManager.ForgetCorpse` — только на сервере (или `ClientRpc` для визуала).
- [ ] `CreatureCommands.ExecuteRadialAction` / `SetRadialRadioValue` — `ServerRpc ExecuteCommand(netId, actionId)`.
- [ ] `CreatureInteractable` — открытие инвентаря: локально у клиента, данные — серверные (см. аудит 1).
- [ ] `CreatureRiding.TickRidden` / `Mount` / `ForceDismount` — только на сервере (см. аудит 4).
- [ ] Спавн существ — серверный `SpawnCreature(creatureId, position, rotation)`. Клиенты получают `ClientRpc OnCreatureSpawned(netId, ...)`.

---

## Приоритет 2 — важно, но не блокер первого коопа

> Эти системы не мешают «двум клиентам увидеть друг друга», но нужны для полноценного коопа. Делать после Приоритета 1.

- [ ] Синхронизация `LootBag` (спавн/подбор/деспавн) — серверная.
- [ ] Синхронизация `Corpse` (harvest, drag, despawn) — серверная.
- [ ] Синхронизация `ChestInventory` (открытие/закрытие/содержимое) — серверная.
- [ ] Синхронизация `RadialMenu` (открытие, действия) — серверная.
- [ ] Синхронизация `EntityInfoPanel` (какая цель у какого игрока) — локальная.
- [ ] Синхронизация `DamageNumberPool` (цифры урона) — `ClientRpc`.
- [ ] Синхронизация `NotificationManager` (уведомления) — локальная.
- [ ] Синхронизация погоды/времени суток (если будет) — серверная.

---

## Приоритет 3 — можно после первого коопа

- [ ] Сетевой слой (NGO + Relay) — подключение.
- [ ] Steam-лобби.
- [ ] Dedicated server.
- [ ] PvP-режим.
- [ ] Античит (серверная валидация скорости, дистанции, кулдаунов).
- [ ] Синхронизация `TamingManager` (прогресс приручения в реальном времени).
- [ ] Синхронизация `WorldManager` (чанки, загрузка/выгрузка по расстоянию).
- [ ] Синхронизация `CreatureRiding` (верхом, mounted combat).
- [ ] Синхронизация `PlayerBuildMode` (превью у других игроков).

---

## Как пользоваться

1. Перед началом работы над системой — открыть её аудит (`0X-audit-*.md`) и этот чеклист.
2. Делать пункты сверху вниз (Приоритет 1 → 2 → 3).
3. После закрытия пункта — `- [x]` + коммит на русском + обновить `../03-current-state.md`.
4. Если пункт оказался не нужен — зачеркнуть `~~...~~` и написать причину.
5. Новые пункты — добавлять в конец соответствующего блока (не менять порядок выше).