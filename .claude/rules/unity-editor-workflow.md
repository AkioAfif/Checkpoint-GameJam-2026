---
paths:
  - "Assets/**"
  - "ProjectSettings/**"
  - "Packages/**"
---

# Unity editor workflow (Unity MCP: `mcp__unity-editor-mcp__*`)

## After every C# change — definition of done
1. `recompile` → wait with `recompile_status` until it finishes.
2. `console` filtered to errors/warnings. **Zero compile errors and zero new warnings from our scripts** before you report done. Fix and repeat.
3. If the change adds `[SerializeField]` references, assign them in the scene/prefab (`set_serialized_field`) or tell the user exactly which fields to drag in.
4. For gameplay changes, when the scene is set up: `editor_play` → check `console` for runtime exceptions (NullReference, MissingReference, InvalidOperation) → `editor_stop`.
5. If the Unity MCP server isn't connected, say so and list what the user must verify manually. Never claim "it compiles" without checking.

## Scenes, prefabs, assets
- **Never hand-edit `.unity`, `.prefab`, `.asset`, `.mat`, `.controller` YAML or `.meta` files** with Write/Edit/sed. Use the Unity MCP tools (`create_gameobject`, `add_component`, `set_component_properties`, `set_serialized_field`, `create_prefab`, `instantiate_prefab`, `create_asset`, `set_parent`, `save_scene`, …). Exception: the `.inputactions` JSON (see `input.md`).
- Every asset has a `.meta` holding its GUID, and references break when the GUID changes. Move or rename with `move_asset` / `rename_asset`. In the shell, move the file **and** its `.meta` together. Never delete a `.meta` while keeping the asset.
- New `.cs` files may be created with Write. Unity generates the `.meta` on its next refresh/recompile.
- After scene/prefab modifications, call `save_scene` (or `save_prefab_contents`). Unsaved scene changes are lost.
- Don't modify anything while in Play mode. Those changes are discarded on stop.
- Don't touch `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.slnx` (all generated).
- Layers, tags and the build scene list are changed via `set_tags_layers` / `add_scene_to_build`, then recorded in `project-settings.md`.
- Gameplay objects are **prefabs** (`Assets/Prefabs/<Feature>/`). Scene instances only override position/serialized refs. Logic + Visual + Sound components sit on the same prefab (visual parts as children).

## Prefab structure convention (KC)
```
Checkpoint (prefab root)   ← Checkpoint.cs, Collider (isTrigger), CheckpointSound.cs + AudioSource
├── Visual                 ← model from Synty, Animator, CheckpointVisual.cs
├── RespawnPoint           ← empty Transform referenced by Checkpoint.cs
└── ProgressBarUI          ← World Space Canvas + ProgressBarUI.cs + LookAtCamera
```
- The root holds logic and colliders. The `Visual` child holds meshes/animator, so art can be swapped without touching logic.
- Synty meshes are used through prefab instances or variants under our folders. Never modify the Synty originals.

## Git
- Commit only when the user asks. Every new/moved asset is committed **with its `.meta`**.
- Don't commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/` (already in `.gitignore`).
