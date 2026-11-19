# Аудит 2: Урон и смерть существа

> Система: получение урона существом, torpor, нокаут, смерть, атрибуция урона, память об атакующем.
> Файлы: `Creature.cs`, `CreatureCombat.cs`, `CreatureDeath.cs`, `CreatureKnockout.cs`, `CreatureHitZone.cs`, `PlayerSurvivalSystem.cs`, `DamageNumberPool.cs`.
> Статус: ✅ аудит завершён.

---

## Цепочка владения
PlayerInteraction.OnAttackInteractFinished ← Animation Event игрока (ближний бой)
└── OverlapBox по модели оружия
└── target.TakeDamage(damage, torpor, pi, hit, point, CombatMember.Player)
└── Creature.TakeDamage(...) ← прокси
└── CreatureCombat.TakeDamage(...) ← вся логика
├── _creature.GetDamageMultiplier(col) ← зона урона (×2 голова, ×0.5 панцирь)
├── _creature.ReduceHealth(finalDamage) ← МУТАЦИЯ HP
├── DamageNumberPool.Instance?.ShowDamage(...) ← КОСМЕТИКА
├── _creature.AddTorpor(torporAmount) ← МУТАЦИЯ TORPOR
├── animator.SetTrigger(TakeDamage) ← АНИМАЦИЯ
├── PlayDamageEffect(...) ← КОСМЕТИКА
└── if (_creature.IsHealthBelowZero()) _creature.RequestDeath()
└── Creature.Die() → CreatureDeath.Die() ← МУТАЦИЯ МИРА (труп, лут, сейв)

CreatureCombat.DealDamageToTarget ← Animation Event существа (атака)
├── верхом: OverlapBox → target.TakeDamage(..., CombatMember.Mounted)
├── AI по цели: _currentAttackTarget.Combat.NotifyAttackedBy(_creature) → target.TakeDamage(..., Tamed/Wild)
└── AI по игроку: PlayerSurvivalSystem.Instance?.TakeDamage(_creature.AttackDamage) ← СИНГЛТОН


**Ключевое наблюдение:** `CreatureCombat.TakeDamage` — это одна точка, через которую проходят все виды урона (игрок, дикий AI, прирученный AI, верхом). Это хорошо для сети: одну функцию развести на сервер проще, чем десять. Но внутри неё смешаны: мутация HP, мутация torpor, анимация, косметика (цифры, эффекты) и смерть, которая тянет за собой труп + лут + сейв.

---

## Вопрос А. Что при одновременном действии двух игроков?

| Сценарий | Что произойдёт в текущем коде |
|---|---|
| Два игрока бьют одно существо одновременно | `CreatureCombat.TakeDamage` не атомарен: `ReduceHealth` + `IsHealthBelowZero` + `RequestDeath`. Два клиента могут оба увидеть `health <= 0` и оба вызвать `Die()`. `CreatureDeath.Die()` создаёт труп + дропает лут → двойной труп / двойной лут. |
| Игрок добивает существо, которое в этот момент нокаутирует другой | `TakeDamage` проверяет `!IsAlive()` в начале, но между `ReduceHealth` и `RequestDeath` — окно. Один клиент видит «жив», второй уже «мёртв». |
| Два прирученных существа атакуют одну цель | `DealDamageToTarget` у каждого читает `_currentAttackTarget` — у каждого свой. Урон суммируется корректно (каждый вызов `TakeDamage` независим), но `NotifyAttackedBy` перезаписывает `_lastAttacker` — последний атакующий выигрывает память, первый теряется. |
| Игрок верхом + пеший игрок бьют одно существо | Верхом — `OverlapBox` перед мордой, пеший — `OverlapBox` по оружию. Оба зовут `TakeDamage`. Суммируется. Но `CombatMember.Mounted` vs `Player` — в `DamageNumberPool` разные цвета, ок. |
| Существо умирает, пока игрок его нокаутирует (torpor) | `TakeDamage` сначала `ReduceHealth`, потом `AddTorpor`. Если HP ушло в 0 — `RequestDeath()`, но `AddTorpor` уже мог сработать. Труп с torpor > 0. |
| Смерть + `RidingController.ForceDismount` | `CreatureDeath.Die()` (по инвариантам) дёргает `ForceDismount`. Если двое верхом (не должно быть, но) — не проверяется. |

**Вывод:** критическая гонка — смерть. `TakeDamage` не имеет флага «уже умирает», `Die()` не идемпотентен на уровне кода (полагается на `_isDead`, но проверка `IsHealthBelowZero` → `RequestDeath` может пройти дважды до установки `_isDead`).

---

## Вопрос Б. Может ли клиент соврать?

| Действие | Где проверка | Можно ли соврать |
|---|---|---|
| Нанести урон существу | `Creature.TakeDamage(damage, ...)` — публичный, любой код может звать с любым `damage` | Да, тривиально: `creature.TakeDamage(99999, 0, null, null, null, CombatMember.Player)` |
| Нанести урон игроку | `PlayerSurvivalSystem.Instance?.TakeDamage(_creature.AttackDamage)` — публичный singleton | Да, но это урон «от существа», клиент не инициирует напрямую... пока |
| Изменить `CombatMember` | Передаётся параметром. `CombatMember.Player` / `Tamed` / `Mounted` — клиент решает, как атрибутировать урон | Да: клиент может назваться `Mounted`, чтобы получить другой цвет цифры (косметика, но всё же) |
| Убить существо | `RequestDeath()` / `Die()` — публичные | Да, можно убить любое существо |
| Нокаутировать (torpor) | `TakeDamage(..., torporAmount, ...)` → `AddTorpor` — публичный через `Creature.AddTorpor` | Да, можно накинуть torpor |
| `NotifyAttackedBy` | Публичный, зовётся из `DealDamageToTarget` | Да, можно подставить любого атакующего |
| `LastAttacker` | Геттер с таймаутом 15 сек | Читается AI для ответной агрессии — клиент может влиять, кого AI запомнит |

**Вывод:** клиент полностью авторитетен по урону. Нет ни одной проверки дистанции, кулдауна (кулдаун есть, но клиентский `_lastAttackTime`), валидности цели (можно бить сквозь стену, если `OverlapBox` задел), зоны урона (клиент передаёт `hitCollider` — может подставить голову для ×2).

**Отдельно:** `Creature.GetDamageMultiplier(col)` читает `CreatureHitZone.damageMultiplier` с переданного клиентом коллайдера. Клиент может передать коллайдер головы, даже если попал в ногу.

---

## Вопрос В. Насколько критична задержка?

| Операция | Критичность | Комментарий |
|---|---|---|
| Нанесение урона (HP) | Высокая | В бою задержка 100–200 мс уже ощутима. Нужен клиентский «предсказанный» урон + серверная коррекция (или хотя бы мгновенный локальный отклик цифры). |
| Смерть существа | Высокая | Труп + лут — изменение мира. Клиент не должен решать. Но визуально смерть должна быть мгновенной (анимация), а лут — по серверному подтверждению. |
| Цифры урона | Низкая (косметика) | Можно клиентски, но цвет зависит от `CombatMember` — лучше с сервера, чтобы не расходилось. |
| Партиклы урона | Низкая | Клиентски. |
| Анимация `TakeDamage` | Низкая | Клиентски (триггер). |
| Torpor | Высокая | Нокаут = приручение. Клиент не должен накидывать torpor. |
| `LastAttacker` | Средняя | Влияет на AI. При сети — серверный. |
| `ForceDismount` при смерти | Высокая | Если существо умерло под игроком — сервер должен телепортировать игрока. |

**Вывод:** урон и смерть критичны к задержке, но требуют серверной валидации. Идеал: клиент шлёт `RequestAttack(targetId, hitPoint, hitColliderId)`, сервер считает урон (с учётом зоны, дистанции, кулдауна) и рассылает результат. Клиент показывает предсказанную цифру сразу, сервер корректирует.

---

## Таблица разметки

| Система | Кто авторитетен | Почему | Что переделать |
|---|---|---|---|
| Урон: расчёт | Сервер | Клиент передаёт damage/hitCollider/CombatMember — всё подделываемо | `TakeDamage` → `ServerRpc RequestAttack(targetId, hitPoint, hitZoneId)`; сервер считает damage сам |
| Урон: HP | Сервер | Прямая мутация HP | `Health` → `NetworkVariable`; `ReduceHealth` → internal |
| Смерть | Сервер | Труп + лут + сейв | `RequestDeath` → ServerRpc; `Die()` → internal; флаг `_isDying` |
| Torpor | Сервер | Нокаут = приручение | `AddTorpor` → серверный; `torpor` → NetworkVariable |
| Нокаут | Сервер | Меняет поведение | `KnockOut` → ServerRpc |
| Зона урона | Сервер | Клиент может подставить голову | Сервер сам делает Overlap по hitPoint |
| Атрибуция (CombatMember) | Сервер | Клиент не решает, кто он | Сервер по connectionId + IsRidden |
| LastAttacker | Сервер | Влияет на AI | Только серверный NotifyAttackedBy |
| Урон игроку | Сервер | HP игрока — серверное | `PlayerSurvivalSystem.TakeDamage` → серверный; Health → NetworkVariable |
| Анимация TakeDamage | Клиент (триггер сервером) | Косметика | ClientRpc или NetworkAnimator |
| Цифры урона | Клиент (косметика) | Только для глаз | ClientRpc ShowDamage(amount, pos, attacker, receiver) |
| Партиклы урона | Клиент | Косметика | ClientRpc |
| Кулдаун атаки | Сервер | Клиент может спамить | Серверный _lastAttackTime per-creature |
| Атака верхом | Сервер | OverlapBox — серверный расчёт | ServerRpc RequestMountedAttack |
| AI (поиск целей) | Сервер | Иначе клиенты видят разное | Полностью серверный AI; позиции через NetworkTransform |

---

## Ключевые выводы

1. **`CreatureCombat.TakeDamage` — «бутылочное горлышко».** Через неё проходит весь урон. Развести её на серверную часть (расчёт + HP + torpor + смерть) и клиентскую (анимация + цифры + партиклы) — самый дешёвый путь к сети. Не надо трогать 10 мест, достаточно одной функции.
2. **Смерть — самая опасная гонка.** `Die()` не идемпотентен. В синглплеере это не видно (один вызов), в коопе два клиента могут добить одновременно. Флаг `_isDying` обязателен до сети, иначе двойной лут.
3. **`CombatMember` уже есть** — это хороший задел. Но сейчас клиент его передаёт. При сети сервер должен сам определять `CombatMember` по `connectionId` + `IsRidden`. Enum оставить, но вычисление — серверное.
4. **`DamageNumberPool.ShowDamage` уже принимает `attacker`/`receiver`** — это значит, архитектура уже готова к тому, чтобы цифры показывались по серверному событию. Достаточно заменить прямой вызов из `TakeDamage` на событие `OnDamageDealt` → клиентский подписчик → `ShowDamage`.
5. **AI (`CreatureWildAI.Tick`, `CreatureTamedAI.Tick`, `ChaseAndAttack`)** — полностью серверный. Клиент не должен тикать AI, иначе рассинхрон позиций и целей. Это большой блок, но он не требует переписывания логики — только флаг `if (!IsServer) return;` в начале `Tick`.
6. **`Creature.UpdateAnimation`** — при сети заменяется на `NetworkAnimator`. Сейчас `Speed`/`IsMoving` пишутся локально из `_agent.velocity` — при сети каждый клиент увидит свою скорость. `NetworkAnimator` решает это.
7. **`PlayerSurvivalSystem.Instance?.TakeDamage`** — прямой вызов из `CreatureCombat.DealDamageToTarget`. При сети — серверный метод на игроке-цели. Синглтон `Instance` — проблема (какой игрок?), но это уже пункт «позиция/игроки», не урон.

---

## Переделать до сети

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
- [ ] `CreatureCombat.DealDamageToTarget` — верхом: `OverlapBox` на сервере; клиент — только анимация.
- [ ] `CreatureCombat.FindWildTarget` / `FindNearestEnemy` / `FindEnemyNearPlayer` — только сервер (AI).
- [ ] `CreatureCombat.PerformAttack` / `AttackFromRider` — серверный триггер анимации через `ClientRpc` / `NetworkAnimator`.
- [ ] `Creature.UpdateAnimation` — при сети заменить на `NetworkAnimator` (параметры Speed/IsMoving реплицируются).
- [ ] `Creature._wildAI.Tick` / `_tamedAI.Tick` — только на сервере (флаг `if (!IsServer) return;`).
- [ ] `CreatureCombat._isAttackingPlayer` — серверное состояние; клиент не должен влиять.
- [ ] `CreatureCombat.ChaseAndAttack` — только сервер (управляет `NavMeshAgent`).
- [ ] `CreatureCombat.UpdateCurrentEnemy` / `ClearTargets` — только сервер.