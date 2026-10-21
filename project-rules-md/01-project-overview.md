# 01. Project Overview

## Что это

ARK Survival Evolved clone на Unity 6.5. Синглплеер сейчас, мультиплеер — в планах.
Стиль: сурвайвал, крафт, строительство, приручение динозавров, PvE/PvP.

**Версия Unity:** 6000.5.0f1 (Unity 6.5)
**Рендер:** URP
**Язык:** C# (.NET Standard 2.1)

## Стек

| Пакет | Назначение |
|---|---|
| Input System | Все контролы (`MyInputActions.inputactions`) |
| Addressables | Загрузка ассетов (задел) |
| TextMeshPro | UI + world text |
| Localization | Мультиязычность |
| Cinemachine | Камеры (TPS/FPS/Selfie) |
| Newtonsoft.Json | Сериализация сейвов |

## Структура проекта

Assets/
├── Animations/
├── Art/
├── Audio/
├── Data/ ← ScriptableObjects (ItemDatabase, CreatureDatabase, RecipeDatabase, GameSettings)
├── InputSystem/
├── Localization/
├── Resources/ ← LevelTable.json, Configs/StatConfig.json
├── Scenes/ ← MainMenu, GameWorld
├── Scripts/ ← весь C#
├── Settings/
├── Shaders/
└── TextMesh Pro/


## Ссылки на документацию

**Unity:**
- [Unity 6 Manual](https://docs.unity3d.com/6000.0/Documentation/Manual/)
- [Scripting API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/)
- [URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@16.0/manual/)
- [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/)
- [Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/)
- [Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.4/manual/)

**ARK-референс:**
- [ARK Wiki](https://ark.wiki.gg/wiki/ARK_Survival_Evolved_Wiki)
- [ARK Dev Kit](https://devkit.ark.wiki.gg/)

**Паттерны:**
- [Game Programming Patterns](https://gameprogrammingpatterns.com/)
- [Unity Best Practices](https://resources.unity.com/games/unity-best-practices-guide-2022-lp)

---

## Структура сцен (верхний уровень)

**MainMenu:**
- GameAssets (глобальные менеджеры)
- MainMenuManager
- EventSystem
- Camera
- Canvas_MainMenu

**GameWorld:**
- GameAssets (глобальные менеджеры)
- WorldManagers (сценовые мир-менеджеры)
- GameManagers (сценовые UI-менеджеры)
- InventoryManager
- PanelsController
- ---Player---
- Cameras
- UICanvases
- EventSystem
- NavMeshBacker
- _Env, Interactables, NPC_1, Robot_George, __CreatureSpawner__

**Архитектурное решение:**
- Глобальные менеджеры (`GameController`, `PlayerProgress`, `GameTimeUpdater`, `LoadingScreenManager`, `AudioManager`, `LanguageManager`) живут на `GameAssets` в обеих сценах, `DontDestroyOnLoad`.
- Сценовые мир-менеджеры (`CorpseManager`, `LootBagManager`, `TamingManager`, `WorldManager`) — на `WorldManagers`, создаются `WorldBootstrap`'ом через `AddComponent`.
- Сценовые UI-менеджеры (`HUDManager`, `UIManager`, `PauseManager`, `DeathScreenManager`, `NotificationManager`, `TooltipManager`, `ContextMenuManager`, `CharacterPreviewManager`, `CombatAudioManager`, `ArrowPool`, `AimUI`, `EntityInfoPanelManager`, `DamageNumberPool`) — на `GameManagers`.
- `ChestUIManager` — на `InventoryManager` (рядом с `InventoryManager`).
- `PanelsUIController` + `RadialMenuController` — на `PanelsController`.
- Менеджеры инвентаря/UI, создаваемые в рантайме (`PlayerController`, `PlayerSurvivalSystem`, `InventoryManager`, `ChestUIManager`) — на `---Player---` / `InventoryManager`.
- **Полная иерархия (с вложенностью и компонентами) — по скриншоту:** `Screenshots/scene-hierarchy.png` (обновляется по необходимости, не входит в `.md`).

**Порядок загрузки GameWorld:**
`WorldBootstrap.Start` → ждёт `GameAssets` → создаёт 4 сценовых менеджера → ждёт `PlayerProgress.playerController` → `LoadingScreenManager.RegisterTask(...)` × 4 → `StartLoading()`.
## Правила работы в чате

**Главное:** не пиши сразу весь код. Обсуждаем → идём по пунктам → после каждого пункта тестируем → чиним баги → только потом дальше.

1. **Не пиши сразу весь код.** Сначала обсуждаем подход, потом идём по пунктам.
2. **После каждого пункта — тест.** Подтвердили → идём к следующему. Есть баги — чиним до перехода.
3. **В конце каждого сообщения — итоги:** что и где исправить (список файлов + изменений).
4. **Если пришли логи/описание бага — не строй предположения сразу.** Задавай вопросы: в какой ситуации, скриншоты, логи, актуальные файлы.
5. **После закрытия пункта — коммит на русском** (краткий) + обновление `03-current-state.md` и `04-todo-bugs.md`.
6. **Файлы, которые ассистент должен прикреплять к новому чату:** все 5 файлов в `.continue/rules/` + актуальные `.cs` файлы по теме.