# Checkpoint GameJam 2026

Instructions for every AI coding agent (Claude Code, Codex, …). This is the single shared instruction file. `CLAUDE.md` is gitignored and personal only.

Unity 6000.6.5f1 · URP 17.6 · Input System 1.20 (legacy input disabled) · uGUI + TextMeshPro · C# 9.
Art: Synty packs in `Assets/Synty/` (read-only, gitignored). Code style: Code Monkey's *Kitchen Chaos*, with its flaws fixed. Small single-purpose components talk through C# events, data lives in ScriptableObjects, and input goes through one `GameInput`.
Game jam: build the smallest thing that works, no speculative systems.

## Rules folder — read before you write
Detailed rules live in `.claude/rules/` (shared by all agents despite the folder name). Claude Code auto-loads a rule when it reads or edits a matching file, but not for a file it is about to create. Other agents never auto-load them. So **every agent: open the files for your task before creating or changing anything**:

| Task | Read first |
|---|---|
| Any C# script | `csharp-architecture.md`, `csharp-pitfalls.md`, `code-templates.md` |
| Player, movement, anything reading input | + `input.md` |
| UI script, Canvas, menus, HUD | + `ui.md` |
| New item/level/audio data | + `scriptable-objects.md` |
| Scenes, prefabs, assets, layers/tags, build list | `unity-editor-workflow.md`, `project-settings.md` |
| Materials, shaders, lighting, post-processing | `rendering-urp.md` |

`project-settings.md` holds verified project facts (packages, input actions, layers, scenes, play mode settings). Trust it over Unity defaults, and update it whenever you change a setting.

## Never-break rules (always apply)
1. **Domain reload is OFF.** Every static field, static event and `Instance` gets reset in `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`. Listeners of singletons/static events unsubscribe in `OnDestroy`.
2. **No `UnityEngine.Input`.** It throws. All input goes through `GameInput` (project-wide actions). EventSystem uses `InputSystemUIInputModule`.
3. **No `using UnityEditor`** in runtime scripts (it breaks the build). No unused `using`s.
4. **No `?.` / `??` / `is null` on Unity objects.** Use `!= null`. (`event?.Invoke` is fine.)
5. One class per file, file name = class name. `[SerializeField] private` fields. Unity 6 APIs (`linearVelocity`, no `FindObjectOfType`).
6. Logic never touches visuals/audio/UI. It raises `EventHandler` events, and `XVisual` / `XSound` / `XUI` components listen.
7. Layers, tags and scenes referenced by code must exist first (none custom exist yet).
8. Don't hand-edit `.unity` / `.prefab` / `.asset` / `.meta` YAML. Use the Unity MCP tools.
9. **Done = recompiled with zero console errors** (Unity MCP `recompile` → `console`). If you have no Unity MCP access, say so and tell the user to check the Unity Console. Never claim it compiles without checking.
10. Ask before adding packages, renaming serialized fields on existing prefabs, or touching `Assets/Synty/`.
