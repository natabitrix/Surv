# Аудит 6: Существа и AI

> Система: спавн существ, AI (дикий/прирученный), смерть, нокаут, трупы, приручение, сохранение существ.
> Файлы: `Creature.cs`, `CreatureCombat.cs` (см. аудит 2), `CreatureDeath.cs`, `CreatureKnockout.cs`, `CreatureWildAI.cs`, `CreatureTamedAI.cs`, `TamingManager.cs`, `CorpseManager.cs`.
> Статус: ✅ аудит завершён.

---

## Цепочка владения
Creature (на каждом существе)
├── Awake: playerProgress = PlayerProgress.Instance
│ ├── playerController = playerProgress.playerController
│ ├── _data = CreatureDatabase.GetCreature(creatureId)
│ ├── AddComponent<CreatureCombat/Death/Knockout/Taming/Commands/Audio/WildAI/TamedAI/Riding/Interactable>
│ ├── AddComponent<Corpse>(disabled) + RadialMenu(disabled)
│ └── health = maxHealth, food = maxFood, stamina = maxStamina
├── Update:
│ ├── torpor -= Data.torporRecoveryRate * dt ← ЛОКАЛЬНО
│ ├── _taming.Tick() ← food + save каждые 10 сек
│ ├── if (knockedOut) → return
│ ├── if (tamed) → _tamedAI.Tick() / TickRidden()
│ └── else → _wildAI.Tick()
└── UpdateAnimation() → animator.SetFloat/SetBool ← ЛОКАЛЬНО

CreatureWildAI (на диком)
├── Tick: FindWildTarget() → Chase/Attack
│ ├── Agent.SetDestination(target.position) ← ЛОКАЛЬНО
│ └── Combat.PerformAttack() → animator.SetTrigger("Attack")
└── HandleWandering → Agent.SetDestination(random) ← ЛОКАЛЬНО

CreatureTamedAI (на прирученном)
├── Tick: HandleTamedState()
│ ├── Flee / Neutral / AttackOwnerTarget / Aggressive
│ ├── FollowOwner → Agent.SetDestination(player.position) ← ЛОКАЛЬНО
│ └── WanderTamed → Agent.SetDestination(random) ← ЛОКАЛЬНО
└── LateUpdate: _isWandering = false (если дошли)

CreatureDeath (на существе)
├── Die():
│ ├── Riding.ForceDismount()
│ ├── SetDead() → _isDead = true, health = 0
│ ├── ChestUIManager.Close()
│ ├── TamingManager.UnregisterCreature() → УДАЛЯЕТ ФАЙЛ
│ ├── Destroy(inventory)
│ ├── CreateCorpse() → CorpseManager.RegisterCorpse() → ПИШЕТ ФАЙЛ
│ └── SetBodyRagdoll()
└── CreateCorpse: CorpseManager.RemoveAliveComponentsFromCreatureCorpse()

CreatureKnockout (на существе)
├── KnockOut():
│ ├── Riding.ForceDismount()
│ ├── knockedOut = true
│ ├── SetBodyRagdoll()
│ ├── AddComponent<ChestInventory>() + Initialize
│ ├── TamingManager.RegisterCreature() → ПИШЕТ ФАЙЛ
│ └── TamingInstanceId = instanceId
├── KnockOutFromLoad: knockedOut = true, SetBodyRagdoll()
└── ExitKnockoutState: knockedOut = false, torpor = 0, TamingManager.UnregisterCreature()

TamingManager (сценовый, синглтон, создаётся WorldBootstrap)
├── Initialize: creatureDatabase = GameAssets.CreatureDatabase
├── RegisterCreature(go, ownerPlayerId) → Guid.NewGuid() → SaveToDisk() ← ПИШЕТ ФАЙЛ
├── UpdateSave(instanceId, ownerPlayerId) → SaveToDisk()
├── UnregisterCreature(instanceId) → File.Delete() ← УДАЛЯЕТ ФАЙЛ
├── LoadAllTamingCreaturesAsync() → читает taming_*.save → SpawnFromData
├── SaveAll() → сохраняет все _loadedCreatures
└── OnApplicationQuit/Pause/Focus → SaveAll()

CorpseManager (сценовый, синглтон, создаётся WorldBootstrap)
├── Initialize: _corpseBodyPrefab = GameAssets.PlayerCorpsePrefab
├── RegisterCorpse(go, ownerPlayerId, creatureId) → Guid.NewGuid() → SaveCorpseToDisk() ← ПИШЕТ ФАЙЛ
├── ForgetCorpse(instanceId) → только loadedCorpses.Remove
├── UnregisterCorpse(instanceId) → File.Delete() ← УДАЛЯЕТ ФАЙЛ
├── LoadAllCorpsesAsync() → читает corpse*.save → SpawnCorpseFromData
└── RemoveAliveComponentsFromCreatureCorpse() → Destroy компонентов


**Ключевое наблюдение:** каждое существо — это `MonoBehaviour` с локальным `Update`. AI тикает локально у каждого клиента. Смерть/нокаут/приручение пишут файлы на диск. При коопе каждый клиент будет тикать свой AI, писать свои файлы и создавать свои трупы.

---

## Вопрос А. Что при одновременном действии двух игроков?

| Сценарий | Что произойдёт в текущем коде |
|---|---|
| Два клиента видят одно существо | Каждый клиент имеет **свой экземпляр** `Creature` (спавн из сцены/сейва). AI тикает у каждого независимо → **разные позиции**, разные цели, разное поведение. Рассинхрон. |
| Два игрока бьют одно существо | `CreatureCombat.TakeDamage` — локально у каждого. HP разное. Смерть может наступить у одного, но не у другого. См. аудит 2. |
| Один игрок нокаутирует, другой — добивает | `CreatureKnockout.KnockOut` → `TamingManager.RegisterCreature` → **пишет файл**. `CreatureDeath.Die` → `TamingManager.UnregisterCreature` → **удаляет файл**. Гонка: файл может остаться «висящим» или удалиться дважды. |
| Один игрок приручает, другой — видит | `tamed = true` — локально. Второй клиент не знает. `TamedFromLoad` не вызывается. |
| Два игрока видят труп | Каждый клиент создаёт **свой** труп через `CorpseManager.RegisterCorpse` → **два файла** `corpse_*.save` для одного существа. При загрузке — **два трупа**. |
| Один игрок подбирает лут из трупа, другой — тоже | `ChestInventory` трупа — локальный у каждого. Оба «подбирают» одни и те же предметы (см. аудит 1). |
| Один игрок садится на существо, другой — тоже | `CreatureRiding.Mount` — локально. См. аудит 4. |
| `TamingManager.SaveAll` на выходе | Тикает у каждого клиента. Два клиента пишут `taming_*.save` → конфликт. |
| `CorpseManager.LoadAllCorpsesAsync` при входе | Каждый клиент загружает все трупы → **дубликаты** в сцене. |
| `Creature.Update` → `torpor -= ...` | Тикает у каждого клиента. Torpor разный. Нокаут наступает в разное время. |
| `Creature.Update` → `_taming.Tick()` (save каждые 10 сек) | Тикает у каждого клиента. Два клиента пишут `taming_*.save` → конфликт. |

**Вывод:** существа и AI **полностью локальные**. При коопе каждый клиент — свой мир существ. Нет ни синхронизации позиций, ни синхронизации HP/torpor, ни единого источника правды для смерти/нокаута/приручения.

---

## Вопрос Б. Может ли клиент соврать?

| Действие | Где проверка | Можно ли соврать |
|---|---|---|
| Спавн существа | `Instantiate(prefab)` — публичный | Да, можно заспавнить любое существо в любом месте |
| Двигать существо | `Agent.SetDestination(...)` — публичный | Да, можно телепортировать через `transform.position` |
| Нанести урон | `Creature.TakeDamage(...)` — публичный (см. аудит 2) | Да |
| Убить существо | `Creature.RequestDeath()` / `Die()` — публичные | Да |
| Нокаутировать | `Creature.KnockOut()` — публичный | Да |
| Приручить | `Creature.tamed = true` — публичное поле; `TamedFromLoad()` — публичный | Да, можно «приручить» любое существо |
| Изменить torpor | `Creature.AddTorpor(...)` — публичный | Да |
| Изменить поведение | `Creature.tamedBehavior = ...` — публичное | Да |
| Зарегистрировать труп | `CorpseManager.RegisterCorpse(...)` — публичный | Да, можно создать труп из ничего |
| Зарегистрировать приручение | `TamingManager.RegisterCreature(...)` — публичный | Да |
| Сохранить существо | `TamingManager.SaveAll()` — публичный | Да, клиент может подделать файл |

**Вывод:** клиент полностью авторитетен по существам. Нет ни одной проверки: кто заспавнил, кто убил, кто приручил, кто владелец. Любой клиент может делать с любым существом что угодно.

---

## Вопрос В. Насколько критична задержка?

| Операция | Критичность | Комментарий |
|---|---|---|
| Движение существа (AI) | **Высокая** | AI должен быть серверным; клиенты видят позиции через `NetworkTransform`. Иначе рассинхрон. |
| Анимация существа | Низкая (косметика) | `NetworkAnimator` решает. |
| Атака существа | Высокая | Урон — серверный (см. аудит 2). Анимация — `ClientRpc`. |
| Смерть существа | **Высокая** | Труп + лут — мир. Серверное. |
| Нокаут | **Высокая** | Меняет поведение, открывает инвентарь. Серверное. |
| Приручение | **Высокая** | Меняет владельца, поведение. Серверное. |
| Torpor | Высокая | Нокаут = приручение. Серверное. |
| Food | Средняя | Падение food — серверное; клиент видит через репликацию. |
| Спавн/деспавн | **Высокая** | Мир. Серверное. |
| Загрузка существ | **Высокая** | При входе — серверная. Клиент не грузит сам. |
| `Creature.UpdateAnimation` | Низкая | `NetworkAnimator`. |

**Вывод:** существа и AI **критичны** — и к задержке (движение), и к авторитетности (смерть/нокаут/приручение). AI должен быть полностью серверным.

---

## Таблица разметки

| Система | Кто авторитетен | Почему | Что переделать |
|---|---|---|---|
| Спавн существа (`Creature.Awake`, `Instantiate(prefab)`) | Сервер | Клиент не решает, какие существа есть в мире | Серверный `SpawnCreature(creatureId, position, rotation)`. Клиенты получают `ClientRpc OnCreatureSpawned(netId, creatureId, pos, rot)`. |
| AI дикий (`CreatureWildAI.Tick`) | Сервер | Иначе клиенты видят разное поведение | `if (!IsServer) return;` в начале `Tick`. Позиция — через `NetworkTransform`. |
| AI прирученный (`CreatureTamedAI.Tick`) | Сервер | То же | `if (!IsServer) return;` в начале `Tick`. |
| Движение существа (`Agent.SetDestination`) | Сервер | `NavMeshAgent` — серверный | Только на сервере. Клиенты видят через `NetworkTransform`. |
| Анимация (`UpdateAnimation`, `animator.SetTrigger`) | Клиент (триггер сервером) | Косметика | `NetworkAnimator` или `ClientRpc PlayAnim(animId)`. |
| Урон (`CreatureCombat.TakeDamage`) | Сервер | См. аудит 2 | См. аудит 2. |
| Смерть (`CreatureDeath.Die`) | Сервер | Труп + лут | `Die()` → `internal`, только на сервере. `ClientRpc OnCreatureDied` для анимации. |
| Создание трупа (`CorpseManager.RegisterCorpse`) | Сервер | Пишет файл, создаёт объект мира | `internal`, только на сервере. `ClientRpc SpawnCorpse(netId, ...)`. |
| Нокаут (`CreatureKnockout.KnockOut`) | Сервер | Меняет поведение, открывает инвентарь | `KnockOut()` → `internal`, только на сервере. `ClientRpc OnCreatureKnockedOut(netId)`. |
| Приручение (`Creature.tamed = true`, `TamedFromLoad`) | Сервер | Меняет владельца | Серверный `TameCreature(netId, playerId)`. Клиент получает `ClientRpc OnCreatureTamed(netId, playerId)`. |
| Torpor (`Creature.AddTorpor`, `torpor -= ...`) | Сервер | Нокаут = приручение | `torpor` → `NetworkVariable`. `Update` — только на сервере. |
| Food (`Creature.Update` → `_taming.Tick`) | Сервер | Падение food | `food` → `NetworkVariable`. `Tick` — только на сервере. |
| `Creature.Update` (главный цикл) | Сервер | Тикает AI, torpor, food | `if (!IsServer) return;` в начале `Update`. Клиент только отображает. |
| `TamingManager.RegisterCreature` / `UpdateSave` / `UnregisterCreature` | Сервер | Пишет/удаляет файлы | `internal`, только на сервере. |
| `TamingManager.SaveAll` / `OnApplicationQuit/Pause/Focus` | Сервер/хост | Пишет файлы | Только на сервере. На клиенте — no-op. |
| `TamingManager.LoadAllTamingCreaturesAsync` | Сервер | Загружает существ | Только на сервере. Клиенты получают `ClientRpc OnCreatureSpawned`. |
| `CorpseManager.RegisterCorpse` / `UnregisterCorpse` | Сервер | Пишет/удаляет файлы | `internal`, только на сервере. |
| `CorpseManager.LoadAllCorpsesAsync` | Сервер | Загружает трупы | Только на сервере. Клиенты получают `ClientRpc SpawnCorpse`. |
| `CorpseManager.RemoveAliveComponentsFromCreatureCorpse` | Сервер | `Destroy` компонентов | Только на сервере. Клиенты получают `ClientRpc OnCreatureDied`. |
| `CreatureRiding.TickRidden` | Сервер | Прямое управление `Agent` | См. аудит 4. |
| `CreatureRiding.Mount` / `ForceDismount` | Сервер | Владелец, дистанция | См. аудит 4. |
| `CreatureInteractable` (открытие инвентаря) | Клиент (локально) + сервер (данные) | Какой инвентарь открыт — локально; данные — серверные | См. аудит 1. |
| `CreatureCommands` (радиальное меню) | Сервер | Меняет поведение | `ServerRpc ExecuteCommand(netId, actionId)`. |
| `Creature.Health` / `SetHealth` / `ReduceHealth` | Сервер | HP — серверное | `Health` → `NetworkVariable` (см. аудит 2). |
| `Creature.SetDead` / `IsDead` | Сервер | Смерть — серверная | `internal`, только на сервере. |
| `Creature.SetCreatureLayer` | Сервер (при спавне) | Слой — серверный | Только на сервере. |
| `Creature.PlayerTransform` | Сервер | AI ищет игрока | Серверный `PlayerTransform` (ближайший игрок / владелец). |

---

## Ключевые выводы

1. **AI полностью локальный.** `CreatureWildAI.Tick` и `CreatureTamedAI.Tick` тикают у каждого клиента. При коопе каждый клиент видит своё поведение. Нужно: `if (!IsServer) return;` в начале `Tick`, позиция — через `NetworkTransform`.

2. **`Creature.Update` — главный цикл, тикает у каждого.** Torpor, food, AI, анимация — всё локально. При сети: `if (!IsServer) return;` в начале `Update`. Клиент только отображает (через `NetworkVariable` / `NetworkTransform` / `NetworkAnimator`).

3. **Смерть (`CreatureDeath.Die`) — локальная.** Создаёт труп, пишет файл, удаляет сейв приручения. При коопе два клиента создадут два трупа и два файла. Нужно: серверный `Die()`, `ClientRpc OnCreatureDied`.

4. **Нокаут (`CreatureKnockout.KnockOut`) — локальный.** Пишет файл `taming_*.save`. При коопе — конфликт. Нужно: серверный `KnockOut()`, `ClientRpc OnCreatureKnockedOut`.

5. **Приручение (`tamed = true`, `TamedFromLoad`) — локальное.** Публичное поле. При коопе: серверный `TameCreature(netId, playerId)`, `ClientRpc OnCreatureTamed`.

6. **`TamingManager` — сценовый синглтон, пишет файлы.** `RegisterCreature`, `UpdateSave`, `UnregisterCreature`, `SaveAll` — всё на клиенте. При коопе: только на сервере. Клиенты получают события через `ClientRpc`.

7. **`CorpseManager` — сценовый синглтон, пишет файлы.** `RegisterCorpse`, `UnregisterCorpse`, `LoadAllCorpsesAsync` — всё на клиенте. При коопе: только на сервере. Клиенты получают `ClientRpc SpawnCorpse`.

8. **`CorpseManager.LoadAllCorpsesAsync` при входе загружает все трупы.** Два клиента загрузят дубликаты. При сети: только сервер загружает, клиенты получают `ClientRpc`.

9. **`TamingManager.LoadAllTamingCreaturesAsync` при входе загружает всех существ.** То же — дубликаты. При сети: только сервер.

10. **`Creature.PlayerTransform` — один на всех.** В `Creature.Awake` берётся из `PlayerProgress.Instance.playerController`. При N игроках — нужен ближайший игрок или владелец (для прирученных).

11. **`Creature.UpdateAnimation` — локальный.** `animator.SetFloat/SetBool` — при коопе каждый клиент видит свою скорость. `NetworkAnimator` решает.

12. **`CreatureRiding.TickRidden` — локальный.** Прямое управление `Agent.velocity`. При сети — только на сервере (см. аудит 4).

13. **`CreatureCommands` — локальные.** Радиальное меню меняет поведение локально. При сети — `ServerRpc ExecuteCommand(netId, actionId)`.

14. **`Creature.SetDead` / `IsDead` — публичные.** Клиент может «убить» существо локально. При сети — `internal`, только на сервере.

15. **`Creature.SetCreatureLayer` — публичный.** Меняет слой при спавне/смерти. При сети — только на сервере.

---

## Переделать до сети

- [ ] `Creature.Update` — `if (!IsServer) return;` в начале. Клиент только отображает.
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
- [ ] `Creature.Awake` — `if (!IsServer) return;` для `_data`-инициализации? Нет, `_data` нужен всем (для отображения). Но `AddComponent` живых компонентов — только на сервере? Нет, компоненты нужны всем (для отображения HP, инвентаря). Правило: **компоненты создаются у всех**, но **тикают только на сервере**.
