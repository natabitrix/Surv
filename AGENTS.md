# Agent instructions

## Language

Always answer in Russian, using Cyrillic letters. This applies unconditionally:

- every reply, in every turn, regardless of the language the user writes in;
- explanations, summaries, plans, progress notes and error reports;
- code comments added or changed by me — follow the existing style of the file being edited.

Exceptions (keep them in their original language):

- code, identifiers, file paths, shell commands, commit-message type prefixes (`fix:`, `feat:`);
- quotes from existing files, logs and compiler output.

If a reply must contain a term without an established Russian translation, write the Russian explanation first and the original term in parentheses.

# Unity Agent Instructions

## Core workflow

- Keep changes focused, use the project's existing patterns, and avoid unnecessary complexity or token-heavy investigation.
- Read applicable `AGENTS.md` files and inspect only the files relevant to the task. Check `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, and existing code/config when the Unity version or setup matters.
- Identify the active project, render pipeline, input system, and relevant packages before making assumptions.
- For Unity APIs, packages, workflows, or best practices that may have changed, check the latest relevant official Unity documentation (Manual, Scripting API, package docs, or release notes). Match guidance to the project's Unity version; use targeted research to conserve tokens, and skip browsing for trivial, stable changes.
- For large or risky changes, make a short plan first. For small, clear changes, proceed directly.
- Prefer direct file edits for source code and text-based configuration. Use Unity CLI/Pipeline commands for Unity Editor operations.

<!-- ## Unity CLI and Pipeline

- Before Editor work, run `unity command` to discover commands supported by the connected Pipeline version.
- Then run `unity command editor_status`; proceed with Editor operations only when it reports `status: ready`.
- Run `unity command ...` with permission to inspect the host Unity process. Sandboxed process discovery can misclassify the live Pipeline descriptor as stale, remove it, and make the server appear unreachable.
- If discovery fails, inspect `unity pipeline list`. A project with the Pipeline package installed but no PID, server port, or reachable server may indicate process-validation or server-startup trouble.
- Prefer discovered commands over assumed command names or parameters. Confirm the connected Pipeline controls the intended project.
- Do not launch another Unity instance against the same project while its Editor is open unless the workflow explicitly supports it. -->

## Code and project changes

- Follow the project's existing C# style, naming, architecture, Unity version, and package APIs. Avoid broad refactors or new dependencies unless needed.
- Keep scripts focused and handle object lifetimes, events, coroutines, and subscriptions safely. Avoid costly per-frame work and repeated allocations in hot paths when relevant; optimize based on need, not habit.
- Preserve serialized field names, Inspector assignments, scene/prefab links, `.meta` files, and GUID references. Avoid unnecessary asset moves or renames.
- Use Pipeline for scene, prefab, and other Editor-state operations when supported. If a needed operation is not available, use the safest suitable method and verify the result in Unity.
- Do not change render pipelines, input systems, package versions, or project-wide settings unless the task requires it.
- Do not delete generated or user data, including `Library` or `UserSettings`, as routine cleanup.

## Git and safety

- When Git is available, inspect `git status` before editing and review the final diff.
- Preserve unrelated user changes. Never reset, discard, overwrite, commit, or push changes unless explicitly requested.
- Use dry-run and confirmation parameters for destructive Pipeline commands when available. Preserve existing scenes and assets unless the task requires changing or deleting them.
- Keep generated verification artifacts in the project workspace, preferably `Temp/`, unless another location is requested. Do not expose secrets or credentials.

## Verification

- After Editor or project changes, wait for compilation and domain reloads to finish, then confirm `editor_status` is ready.
- Check Unity console errors with `unity command get_console_logs --severity Error --limit 100`.
- Run focused tests or checks relevant to the change when available. Use Edit Mode tests for suitable editor/logic checks and Play Mode tests for runtime behavior. Do not run unrelated expensive checks for a small change.
- For build, platform, or performance tasks, run the relevant build/check or profiling workflow if available. Do not claim a build or test passed unless it was run and passed.
- When visual verification matters, capture the Game view through Pipeline, for example:
  `unity command screenshot --view game --output <absolute-workspace-path>.png --width 1280 --height 720`
- Inspect required screenshots and report compilation failures, console errors, screenshot failures, test failures, and unexpected visual results honestly.
- If Pipeline or compilation fails, gather status and logs, diagnose the likely cause, and retry only after a useful correction. Do not repeatedly issue commands or make broad project changes blindly.

## Completion report

- Briefly report what changed and which relevant checks ran and passed. When official documentation materially guided the solution, identify the source briefly.
- State important failures, limitations, or unverified behavior. Keep the report concise and do not claim success beyond the evidence.

## The project description can be found in the files:
[00-project-passport.md](./project-rules-md/00-project-passport.md)
[01-project-overview.md](./project-rules-md/01-project-overview.md)
[02-game-design.md](./project-rules-md/02-game-design.md)
[03-current-state.md](./project-rules-md/03-current-state.md)
[04-todo-bugs.md](./project-rules-md/04-todo-bugs.md)
[05-roadmap.md](./project-rules-md/05-roadmap.md)

[00-network-overview.md](./project-rules-md/network/00-network-overview.md)
[01-audit-inventory.md](./project-rules-md/network/01-audit-inventory.md)
[02-audit-damage.md](./project-rules-md/network/02-audit-damage.md)
[03-audit-players.md](./project-rules-md/network/03-audit-players.md)
[04-audit-position.md](./project-rules-md/network/04-audit-position.md)
[05-audit-world.md](./project-rules-md/network/05-audit-world.md)
[06-audit-creatures.md](./project-rules-md/network/06-audit-creatures.md)
[07-refactor-checklist.md](./project-rules-md/network/07-refactor-checklist.md)
[08-refactor-plan-nplayers.md](./project-rules-md/network/08-refactor-plan-nplayers.md)

[Scene_GameWorld_Hierarchy_AI_Context.md](./project-rules-md/Scene_GameWorld_Hierarchy_AI_Context.md)
[Scene_MainMenu_Hierarchy_AI_Context.md](./project-rules-md/Scene_MainMenu_Hierarchy_AI_Context.md)

[scripts_list.txt](./project-rules-md/scripts_list.txt)


