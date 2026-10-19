---
paths:
  - "Assets/**"
  - "ProjectSettings/**"
  - "Packages/**"
---

# Project settings (verified facts — check here before assuming any Unity default)

Last verified: 2026-10-18. If you change a setting below, update this file in the same change.

## Versions & packages
- Unity **6000.6.5f1**. C# API compatibility: .NET Standard 2.1 (`apiCompatibilityLevel: 6`). Language: C# 9 — no `required`, no file-scoped namespaces, no primary constructors, no raw string literals.
- URP **17.6.0**, Shader Graph 17.6.0, Input System **1.20.0**, uGUI **2.6.0** (TextMeshPro lives inside this package — `using TMPro;`), Timeline 6.6.0, AI Navigation 2.0.15 (`using Unity.AI.Navigation;` for `NavMeshSurface`, `UnityEngine.AI` for `NavMeshAgent`), Test Framework 1.8.0.
- Installed but **not used by our code**: Visual Scripting, `com.unity.ai.*`, `com.unity.pipeline`, collab-proxy. Never `using Unity.VisualScripting;` (KC had this by accident).
- **Removed `com.unity.ai.inference`** (2026-10-17): its dependency `com.unity.dt.app-ui` ships a WebGL native lib that breaks the Web build at link time (`wasm-ld: undefined symbol: _embind_register_class`). Don't re-add it unless you drop the Web build.
- **Cinemachine 6.6.0** (built-in package, Cinemachine 3 API: `using Unity.Cinemachine;`, `CinemachineCamera`, `CinemachineFollow`, `CinemachineRotationComposer`, `CinemachineConfiner3D`). Added 2026-10-17 for Level01.
- **Not installed**: DOTween, Odin, NaughtyAttributes, UniTask, Newtonsoft. Don't write code that needs them. Ask before adding any package.
- TMP Essential Resources are **imported** (`Assets/TextMesh Pro/`). If they ever go missing, re-import non-interactively with Unity MCP `eval`: `TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);`. **Never use the menu item *Window > TextMeshPro > Import TMP Essential Resources***: it opens a modal dialog that blocks the agent until a human closes it.

## Input
- Active Input Handling = **Input System Package only** (`activeInputHandler: 1`). Any `UnityEngine.Input.*` call (`Input.GetKey`, `Input.GetAxis`, `Input.mousePosition`) throws `InvalidOperationException` at runtime.
- **Project-wide actions** = `Assets/InputSystem_Actions.inputactions` (access through `InputSystem.actions`; it is enabled automatically).
- C# wrapper generation is **off** (`generateWrapperCode: 0`), so the class `InputSystem_Actions` **does not exist**. Don't reference it. See `.claude/rules/input.md`.
- Action map `Player`: Move (Vector2), Look (Vector2), Attack (LMB / Enter / pad West — the Fabricator uses it to dig), Interact (E / pad North, **plain press, no Hold interaction**), Crouch (C / pad East), Jump (Space / pad South), Previous (1), Next (2), Sprint (LeftShift / L3), Pause (Esc / pad Start), Restart (R / pad Select).
- Interact rebinds are saved as JSON overrides in PlayerPrefs key `InputBindings` (see `GameInput.RebindInteract`).
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
- Layers: built-ins + **Interactable (8)** (everything the player can target), **Portrait (9)** (dialogue portrait booth, filmed by its own cameras), **Puzzle (10)** (Fabricator diorama). Main Camera and the sun exclude Portrait + Puzzle. Tags: **none custom**. Sorting layers: default only.
  - Before code uses `LayerMask.NameToLayer("X")`, `CompareTag("X")` or a serialized `LayerMask`, create that layer/tag (Unity MCP `set_tags_layers`) and add it to the list above.

## Scenes
- Build Profiles list: `Assets/_Project/Scenes/HouseInterior.unity` (0, the game starts here), `Level01_VillagePrototype.unity` (1), `LoadingScene.unity` (2), `HouseInterior_Stub.unity` (3). `Loader.Scene` enum names match these files; new members go at the end (`HouseDoor` serializes the enum as an int).
- **HouseInterior** = the player's bedroom (Day 1 morning), built by *Tools > Day1 > Build House Interior* (`HouseInteriorBuilder`, re-runnable, overwrites the scene). Prefabs: `Prefabs/Environment/PlayerBedroom` (floor, walls, window, `BedroomDoor`, light), `Bed`, `BedsideTable`, `Prefabs/Interactables/BedsideLetter`. `BedroomDirector` runs wake-up → letter → monologue → door → `Loader.Load(Level01)`. Its own managers/HUD (no Fabricator/QTE panels, no SoundManager, plain fixed Main Camera).
- **Level01 is generated**: *Tools > Level01 > Rebuild Scene* (`Level01SceneBuilder`) recreates the whole scene, its prefabs, SO data and NavMesh from code, and wipes hand edits in the scene. Change the builders, or stop using Rebuild once the level is hand-tuned. *Tools > Level01 > Rebuild Loading + Stub Scenes* does the other two scenes and the build list.
- `Assets/Scenes/SampleScene.unity` — template scene, no longer in Build Profiles.
- `Assets/Scenes/Testing Scene.unity` — Synty PolygonPrototype greybox (stairs, ramps, ladder, crates), Main Camera, 2 directional lights, Global Volume. **Not in the build list.**
- SampleScene / Testing Scene have no EventSystem or Canvas.
- Any scene loaded by `Loader` must be in the Build Profiles scene list (Unity MCP `add_scene_to_build`), and its enum name must equal the file name exactly. Scene names with spaces can't be enum members, so rename the scene first (e.g. `TestingScene`).

## Player settings
- Web publishing: Compression **Gzip** + **Decompression Fallback on** (works on itch.io). Builds go to `Builds/` (gitignored).
- Run In Background is **off** (builds pause when unfocused).
- AI play tests: Play mode still freezes when the editor isn't the focused app. Right after entering Play mode, run Unity MCP `eval` `UnityEngine.Application.runInBackground = true;`. The Input System also ignores the keyboard while unfocused, so MCP `simulate_key` presses only land sometimes; call the game logic directly when they don't.

## Assets
- `Assets/Synty/` — third-party art (PolygonGeneric, PolygonStarter = the PolygonPrototype greybox kit; SyntyPackageHelper is editor-only). **Read-only**: never edit, move or rename anything inside it. Make a prefab variant or copy into our folders when you need changes.
- Synty materials use Synty Shader Graphs (`PolygonShader`, `Generic_Standard`, …) and are URP-compatible.
- `Assets/TutorialInfo/`, `Assets/Readme.asset` — Unity template leftovers; ignore them.
- **The Distributor** (antagonist NPC by the Fabricator) is generated by `Tools/Blender/build_distributor.py` (Blender 5.2, headless: `blender.exe -b --factory-startup -P Tools/Blender/build_distributor.py [-- <previewDir>]`). It writes `Art/Models/Distributor/` (FBX + baked head base/normal maps) and `Tools/Blender/Distributor.blend`. `CharacterBuilder.DistributorRigPrefab()` re-binds the FBX to identity-rotation bones named like the Chibi rig, so `Chibi.controller` drives him; shape keys `BrowAngry/Sneer/MouthOpen/JowlSag` are driven by `DistributorFaceVisual`. After re-running the script, rebuild his prefabs (Rebuild Scene, or `DistributorRigPrefab()` + `InteractablePrefabBuilder.Villager(...)`).

## Day 1: The Crisis (added 2026-10-18)
- Spec: `DAY1_SPEC.md`. Code in `Scripts/Day1/` (`BedroomDirector` in HouseInterior, then `Day1Director` in Level01 = enum state machine Arrival → Tutorial1 → Tutorial2 → Departure → Sari → Night → Done, starting the truck on its first Update, plus `DistributorTruck(+Visual)`, `CutsceneActor`, `FabricatorDelivery`, `BedsideLetter`, `BedroomDoor`, `NightVisual`, `Day1CameraVisual`, `EmoteLoopVisual`, `WorldState`, `Day1SO`). UI: `LetterUI`, `ScreenFadeUI`, `FabricatorHintUI`.
- **Build it into the open Level01 scene with *Tools > Day1 > Build Graybox*** (re-runnable; only replaces its own `Day1_Graybox` / `Day1` roots and UI panels, swaps in the new Fabricator, and adds `CutsceneActor` to the player and the Distributor). After *Tools > Level01 > Rebuild Scene* (which wipes the scene) run it again.
- The bedroom is no longer in Level01 (moved to HouseInterior on 2026-10-19). The truck cutscene uses `CM_Truck`, enabled by `Day1CameraVisual`.
- Builder trap: `EditorSceneManager.NewScene` unloads assets loaded before it, so load SOs/prefabs a new scene references *after* creating it. `Level01SceneBuilder.BuildPortraitBooth()` re-saves the rig prefab (fresh fileIDs for leg/shoe parts, and it resets player instances already in the scene), so spawn the player after it.
- `GameManager.State.Cutscene` = no player input, HUD hidden. `Emote` gained WakeUp, HeavySigh, GoofyFlip, GoofySetup, TiltHead, WideSmile; `CharacterBuilder.Controller()` adds missing emote states to the existing `Chibi.controller` in place.
- Task lists are swapped per story state (`TaskManager.SetObjectives`, objective type `Story` is completed by the director). `HouseDoor` has `SetLocked` / `SetEntryDialogue`. `Npc.SetOverrideDialogue` swaps what a character says. `DialogueManager.StartDialogue(null, so)` = player-only monologue.
- Fabricator: `FabricatorController.recipeArray` = per-press `Recipe` (inputs, output, returned item, puzzle, blocked text); the last entry repeats. Press 1 = 3 crops → 2 seeds with the single-liquid `Puzzle_00_Intro` (a puzzle may omit liquids; unused ones count as done). Presses 2+ = water + flower + crop → 1 seed.
- Blender (headless, run from the repo root): `Tools/Blender/build_truck.py` → `Art/Models/Truck/DistributorTruck.fbx` (chassis + 4 wheel children, wheel origins on the axles); `Tools/Blender/build_fabricator.py` → `Art/Models/Fabricator/Fabricator.fbx` (~1.3 m, separate Base/Tube/Lid/Hopper/... parts). Shared code in `day1_common.py`. Both face +Z in Unity, scale 1. `Day1PrefabBuilder` / `InteractablePrefabBuilder.Fabricator()` map the Blender material slot names to `GrayboxPalette`.
- `WorldState` (static, in memory only) records the end of Day 1 for the next scene; `HouseInterior_Stub` shows it.
- `GameDataBuilder.LoadAll()` picks up existing assets without regenerating the item/character prefabs under a hand-built scene. The Distributor's hostile lines are kept as `Distributor_Day2_*`.

