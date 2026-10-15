---
paths:
  - "Assets/**"
  - "ProjectSettings/**"
  - "Packages/**"
---

# Project settings (verified facts — check here before assuming any Unity default)

Last verified: 2026-10-14. If you change a setting below, update this file in the same change.

## Versions & packages
- Unity **6000.6.5f1**. C# API compatibility: .NET Standard 2.1 (`apiCompatibilityLevel: 6`). Language: C# 9 — no `required`, no file-scoped namespaces, no primary constructors, no raw string literals.
- URP **17.6.0**, Shader Graph 17.6.0, Input System **1.20.0**, uGUI **2.6.0** (TextMeshPro lives inside this package — `using TMPro;`), Timeline 6.6.0, AI Navigation 2.0.15 (`using Unity.AI.Navigation;` for `NavMeshSurface`, `UnityEngine.AI` for `NavMeshAgent`), Test Framework 1.8.0.
- Installed but **not used by our code**: Visual Scripting, `com.unity.ai.*`, `com.unity.pipeline`, collab-proxy. Never `using Unity.VisualScripting;` (KC had this by accident).
- **Not installed**: Cinemachine, DOTween, Odin, NaughtyAttributes, UniTask, Newtonsoft. Don't write code that needs them. Ask before adding any package.
- TMP Essential Resources are **not imported yet** (`Assets/TextMesh Pro/` is missing). Before the first TMP text, import them non-interactively with Unity MCP `eval`: `TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);` (essentials, no examples, no dialog). **Never use the menu item *Window > TextMeshPro > Import TMP Essential Resources***: it opens a modal dialog that blocks the agent until a human closes it.

## Input
- Active Input Handling = **Input System Package only** (`activeInputHandler: 1`). Any `UnityEngine.Input.*` call (`Input.GetKey`, `Input.GetAxis`, `Input.mousePosition`) throws `InvalidOperationException` at runtime.
- **Project-wide actions** = `Assets/InputSystem_Actions.inputactions` (access through `InputSystem.actions`; it is enabled automatically).
- C# wrapper generation is **off** (`generateWrapperCode: 0`), so the class `InputSystem_Actions` **does not exist**. Don't reference it. See `.claude/rules/input.md`.
- Action map `Player`: Move (Vector2), Look (Vector2), Attack, Interact (E / pad North), Crouch (C / pad East), Jump (Space / pad South), Previous (1), Next (2), Sprint (LeftShift / L3).
- Action map `UI`: Navigate, Submit, Cancel, Point, Click, RightClick, MiddleClick, ScrollWheel, TrackedDevice*.
- Control schemes: Keyboard&Mouse, Gamepad, Touch, Joystick, XR.

## Play mode — IMPORTANT
- *Enter Play Mode Options* enabled with **Domain Reload OFF and Scene Reload OFF** (`m_EnterPlayModeOptions: 3`).
  - Static fields, static events and `Instance` singletons **keep their values from the previous Play session**.
  - Every class with static state must reset it with `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`. See `csharp-pitfalls.md`.

## Rendering
- Two quality tiers: `Mobile` (index 0) → `Mobile_RPAsset`, `PC` (index 1, editor default) → `PC_RPAsset`. Renderers: `PC_Renderer`, `Mobile_Renderer` in `Assets/Settings/`.
- Color space: Linear. Post-processing goes through a `Global Volume` (already in both scenes) using `DefaultVolumeProfile` / `SampleSceneProfile`.

## Physics & time
- Gravity (0, -9.81, 0). Fixed timestep 0.02. `Physics.autoSyncTransforms = false`.
- Layers: **only the Unity built-ins** (Default, TransparentFX, Ignore Raycast, Water, UI). Tags: **none custom**. Sorting layers: default only.
  - Before code uses `LayerMask.NameToLayer("X")`, `CompareTag("X")` or a serialized `LayerMask`, create that layer/tag (Unity MCP `set_tags_layers`) and add it to the list above.

## Scenes
- `Assets/Scenes/SampleScene.unity` — the **only** scene in Build Profiles.
- `Assets/Scenes/Testing Scene.unity` — Synty PolygonPrototype greybox (stairs, ramps, ladder, crates), Main Camera, 2 directional lights, Global Volume. **Not in the build list.**
- Neither scene has an EventSystem or Canvas yet.
- Any scene loaded by `Loader` must be in the Build Profiles scene list (Unity MCP `add_scene_to_build`), and its enum name must equal the file name exactly. Scene names with spaces can't be enum members, so rename the scene first (e.g. `TestingScene`).

## Assets
- `Assets/Synty/` — third-party art (PolygonGeneric, PolygonStarter = the PolygonPrototype greybox kit; SyntyPackageHelper is editor-only). **Read-only**: never edit, move or rename anything inside it. Make a prefab variant or copy into our folders when you need changes.
- Synty materials use Synty Shader Graphs (`PolygonShader`, `Generic_Standard`, …) and are URP-compatible.
- `Assets/TutorialInfo/`, `Assets/Readme.asset` — Unity template leftovers; ignore them.
