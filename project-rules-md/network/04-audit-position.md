# Аудит 4: Позиция и движение

> Система: движение игрока, поворот, прыжок/гравитация, плавание, лестница, оседлание, движение верхом, сохранение позиции, время.
> Файлы: `PlayerController.cs`, `RidingController.cs`, `GameTimeUpdater.cs`, `PlayerProgress.cs`, `CreatureRiding.cs` (частично).
> Статус: ✅ аудит завершён.

---

## Цепочка владения
PlayerController (сценовый, синглтон)
├── Update: Move() / Swim() / OnLadder()
│ └── _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + ...)
│ └── CharacterController.Move(...) ← ЛОКАЛЬНОЕ движение
├── LateUpdate: CameraRotation() / HeadRotation()
│ ├── _cinemachineTargetYaw += _input.look.x
│ ├── _cinemachineTargetPitch += _input.look.y
│ ├── transform.rotation = Quaternion.Euler(0, _cinemachineTargetYaw, 0) ← ЛОКАЛЬНЫЙ поворот
│ └── CinemachineCameraTarget.transform.rotation = ...
├── JumpAndGravity() → _verticalVelocity += Gravity * Time.deltaTime
├── GroundedCheck() → Physics.CheckSphere(...)
└── SetMounted(bool) → _controller.enabled = !mounted

RidingController (на игроке)
├── Mount(CreatureRiding mount) → transform.SetParent(mount.RiderAttachPoint)
│ ├── _playerController.SetMounted(true)
│ └── компенсация lossyScale
├── Dismount(CreatureRiding mount) → transform.SetParent(_savedParent)
│ ├── transform.position = FindDismountPosition(mount)
│ └── NavMesh.SamplePosition(...)
├── OnApplicationQuit / OnDestroy → ForceDismountIfMounted()
└── CurrentMount : CreatureRiding

CreatureRiding (на существе)
├── Mount(PlayerController) → Agent.updateRotation = false
├── TickRidden(move, look, sprint, ...) → Agent.velocity = ... ← прямое управление
└── ForceDismount()

PlayerProgress (глобальный)
├── LateUpdate: _lastKnownPlayerPosition = _playerController.transform.position
└── Save(): сохраняет playerPositionX/Y/Z, playerRotationX/Y/Z

GameTimeUpdater (глобальный)
├── Update: GameTime.AddTime(Time.unscaledDeltaTime) ← только при timeScale > 0 и focused
└── OnApplicationPause/Focus/Quit → GameTime.Save()


**Ключевое наблюдение:** движение полностью клиентское — `CharacterController.Move` локально. Позиция сохраняется в `PlayerProgress.Save()` в один файл. При коопе каждый клиент будет двигать своего игрока и писать в один файл.

---

## Вопрос А. Что при одновременном действии двух игроков?

| Сценарий | Что произойдёт в текущем коде |
|---|---|
| Два игрока двигаются | Оба `PlayerController` — но второй удалён при `Awake` (см. аудит 3). Даже если убрать `Destroy`, `PlayerProgress.LateUpdate` кэширует позицию последнего контроллера. При сохранении — позиция одного игрока. |
| Два игрока в одном месте | `CharacterController` не сталкиваются с другими `CharacterController` (только с коллайдерами мира). Игроки проходят друг сквозь друга. |
| Один игрок верхом, другой — пеший | `RidingController.Mount` переприцепляет игрока к `RiderAttachPoint`. `SetMounted(true)` отключает `CharacterController`. Второй игрок этого не видит (нет репликации `IsMounted`). |
| Один игрок спешивается, другой — садится на то же существо | `CreatureRiding` — один на существо. Два `Mount` подряд: второй перезапишет `_currentMount`. Первый игрок останется «в воздухе» (его `transform` всё ещё прицеплен к `RiderAttachPoint`, но `CreatureRiding` уже указывает на второго). |
| Один игрок умер, другой — двигается | `PlayerSurvivalSystem.TogglePlayerActive(pc, false)` отключает `PlayerController` только у умершего. Но `PlayerController.Instance` — один, и если он указывает на мёртвого, второй игрок не сможет двигаться (если бы он был). |
| `GameTimeUpdater` тикает у каждого клиента | `GameTime.AddTime` — статический, глобальный. Два клиента копят разное время (каждый в своём процессе). При сохранении — рассинхрон. |
| Один игрок телепортируется (читы) | `PlayerController.transform.position = ...` — публичное. `CharacterController.Move` не проверяет скорость. Сервер не валидирует. |

**Вывод:** движение полностью локальное, без валидации. При коопе каждый клиент авторитетен по своей позиции, но нет механизма синхронизации и нет валидации (телепорт, скорость, прохождение сквозь стены).

---

## Вопрос Б. Может ли клиент соврать?

| Действие | Где проверка | Можно ли соврать |
|---|---|---|
| Двигать себя | `CharacterController.Move(...)` — публичный, локальный | Да, клиент полностью авторитетен: `transform.position = ...` напрямую |
| Телепортироваться | `transform.position = ...` — публичное | Да, тривиально |
| Летать | `_verticalVelocity` — приватное, но `CharacterController.Move` не проверяет гравитацию | Да, можно вызвать `Move(Vector3.up * 100)` |
| Проходить сквозь стены | `CharacterController` сталкивается с коллайдерами, но `transform.position = ...` — нет | Да, прямой установкой позиции |
| Садиться на существо | `RidingController.Mount(mount)` — публичный | Да, можно «сесть» на любое существо (нет проверки дистанции/владельца) |
| Спешиваться | `RidingController.Dismount(mount)` — публичный | Да |
| Сохранять позицию | `PlayerProgress.Save()` — пишет `_playerController.transform.position` | Да, клиент может подделать файл |
| `_lastKnownPlayerPosition` | `LateUpdate` кэширует локальную позицию | Да, при сохранении пишется локальная |
| `GameTime.Now` | См. аудит 3 — клиентское время | Да |
| `Mount` без `RiderAttachPoint` | Проверка есть (`RiderAttachPoint == null` → error) | Нет, но это не защита от читов |

**Вывод:** клиент полностью авторитетен по позиции и оседланию. Нет ни одной проверки: дистанции до существа, владельца существа, скорости движения, телепорта, прохождения сквозь стены.

---

## Вопрос В. Насколько критична задержка?

| Операция | Критичность | Комментарий |
|---|---|---|
| Движение игрока | Высокая | 100 мс задержки = «резиновый» персонаж. Нужен client-side prediction + server reconciliation. |
| Поворот камеры | Низкая | Локально, не реплицируется (только для наблюдателей) |
| Прыжок / гравитация | Высокая | `_verticalVelocity` — локально. При сети — предсказание + коррекция. |
| Оседлание (Mount) | Средняя | Визуально — мгновенно, но сервер должен подтвердить (владелец, дистанция). |
| Спешивание (Dismount) | Средняя | Позиция спешивания — серверная (NavMesh.SamplePosition). |
| `TickRidden` (движение верхом) | Высокая | Прямое управление `Agent.velocity` — серверное. Клиент шлёт ввод, сервер двигает. |
| `GameTime` | Средняя | Должно быть серверным; клиент синхронизирует часы. |
| `_lastKnownPlayerPosition` | Низкая | Только для сохранения. Серверная позиция — авторитет. |

**Вывод:** движение критично к задержке. Идеал: client-side prediction (клиент двигает локально, предсказывает) + server reconciliation (сервер присылает коррекцию). Оседлание — серверное подтверждение.

---

## Таблица разметки

| Система | Кто авторитетен | Почему | Что переделать |
|---|---|---|---|
| Движение игрока (`PlayerController.Move`, `CharacterController.Move`) | Клиент + валидация сервера | Client-side prediction для отзывчивости; сервер проверяет скорость/телепорт | `NetworkTransform` (клиент-авторитарный) + серверная проверка `maxSpeed`. `CharacterController` — локально. При рассинхроне — `ServerRpc ReconcilePosition`. |
| Поворот игрока (`transform.rotation = Quaternion.Euler(0, _cinemachineTargetYaw, 0)`) | Клиент | Только для своего игрока; наблюдатели видят через `NetworkTransform` | Не реплицируется напрямую; `NetworkTransform` шлёт rotation. |
| Прыжок / гравитация (`JumpAndGravity`, `_verticalVelocity`) | Клиент + валидация сервера | Предсказание для отзывчивости; сервер проверяет `Grounded` | `_verticalVelocity` — локально; сервер проверяет, что игрок не «летает». |
| Плавание (`Swim`) | Клиент + валидация сервера | Предсказание; сервер проверяет `_isInWater` | `_isInWater` — локально (OverlapSphere); сервер валидирует. |
| Лестница (`OnLadder`) | Клиент + валидация сервера | Предсказание; сервер проверяет тег `Ladder` | `_onLadder` — локально; сервер валидирует. |
| Оседлание (`RidingController.Mount`) | Сервер | Владелец существа, дистанция — серверные | `ServerRpc RequestMount(creatureId)`. Сервер проверяет: существо приручено, игрок владелец, дистанция ≤ X. |
| Спешивание (`RidingController.Dismount`) | Сервер | Позиция спешивания — `NavMesh.SamplePosition` | `ServerRpc RequestDismount`. Сервер находит позицию и телепортирует игрока. |
| Движение верхом (`CreatureRiding.TickRidden`) | Сервер | Прямое управление `Agent.velocity` | Клиент шлёт `ServerRpc SendRideInput(move, look, sprint)`. Сервер двигает `Agent`. |
| `_lastKnownPlayerPosition` (`PlayerProgress.LateUpdate`) | Сервер | Только для сохранения; серверная позиция — авторитет | `_lastKnownPlayerPosition` — серверная. `LateUpdate` — только на сервере. |
| `GameTime` (`GameTime.Now`, `GameTimeUpdater`) | Сервер | Клиентское время рассинхронизировано | `GameTime.Now` → серверное время. `GameTimeUpdater` — только на сервере. Синхронизация часов с клиентом. |
| Сохранение позиции (`PlayerProgress.Save`) | Сервер | Один файл `Player.save` | Per-player файл. На клиенте — no-op. |
| `ForceDismount` при выходе (`RidingController.OnApplicationQuit/OnDestroy`) | Сервер | Смерть/выход — серверное | `ServerRpc ForceDismount` при выходе; сервер телепортирует игрока. |

---

## Ключевые выводы

1. **Движение полностью локальное, без валидации.** `CharacterController.Move` + `transform.position = ...` — клиент может телепортироваться, летать, проходить сквозь стены. При коопе нужен `NetworkTransform` (клиент-авторитарный) + серверная проверка `maxSpeed` и `Grounded`.

2. **Client-side prediction обязателен.** 100 мс задержки = «резиновый» персонаж. Идеал: клиент двигает локально, сервер присылает коррекцию (`ServerRpc ReconcilePosition`). NGO имеет встроенный `NetworkTransform` с интерполяцией, но prediction нужно писать самим.

3. **Оседлание — серверное.** `RidingController.Mount` публичный, без проверки дистанции/владельца. При сети: `ServerRpc RequestMount(creatureId)`, сервер проверяет: существо приручено, игрок владелец, дистанция ≤ X.

4. **`TickRidden` — серверный.** Прямое управление `Agent.velocity` — это серверная операция. Клиент шлёт ввод, сервер двигает существо.

5. **`_lastKnownPlayerPosition` — серверная.** Сейчас кэшируется локально в `LateUpdate`. При сети — только на сервере.

6. **`GameTime` — серверное.** `GameTimeUpdater` тикает у каждого клиента, `GameTime.Now` в мультиплеере — клиентское UTC. Рассинхрон. Нужна синхронизация часов с сервером.

7. **`ForceDismount` при выходе** — при сети: `ServerRpc ForceDismount` при выходе; сервер телепортирует игрока, чтобы он не остался «в воздухе».

8. **`PlayerProgress.Save` пишет один файл** — при N игроках per-player файл. См. аудит 3.

9. **`PlayerController.SetMounted` отключает `CharacterController`** — при сети это локальная операция, но `IsMounted` должен реплицироваться, чтобы другие клиенты видели игрока в седле.

10. **`RidingController` не проверяет, что игрок — владелец существа.** Любой может сесть на любое прирученное существо. При сети — серверная проверка `ownerPlayerId`.

---

## Переделать до сети

- [ ] `PlayerController.transform` — client-side prediction + `NetworkTransform` (клиент-авторитарный).
- [ ] `PlayerController.Move` / `Swim` / `OnLadder` — локально, но сервер валидирует `maxSpeed`, `Grounded`, `_isInWater`, `_onLadder`.
- [ ] `PlayerController._verticalVelocity` — предсказание; сервер проверяет, что игрок не «летает».
- [ ] `PlayerController.SetMounted` — `IsMounted` реплицируется (другие клиенты видят игрока в седле).
- [ ] `RidingController.Mount` — `ServerRpc RequestMount(creatureId)`; сервер проверяет: приручено, владелец, дистанция.
- [ ] `RidingController.Dismount` — `ServerRpc RequestDismount`; сервер находит позицию через `NavMesh.SamplePosition` и телепортирует.
- [ ] `RidingController.OnApplicationQuit` / `OnDestroy` — `ServerRpc ForceDismount`; сервер телепортирует игрока.
- [ ] `CreatureRiding.TickRidden` — только сервер; клиент шлёт `ServerRpc SendRideInput(move, look, sprint)`.
- [ ] `PlayerProgress.LateUpdate` (`_lastKnownPlayerPosition`) — только на сервере.
- [ ] `PlayerProgress.Save` — per-player файл; на клиенте no-op (см. аудит 3).
- [ ] `GameTime.Now` — серверное время; синхронизация часов с клиентом.
- [ ] `GameTimeUpdater` — только на сервере (см. аудит 3).
- [ ] `PlayerController.transform.position` — серверная валидация телепорта (максимальное смещение за тик).
- [ ] `RidingController._currentMount` — серверное состояние; клиент только отображает.
