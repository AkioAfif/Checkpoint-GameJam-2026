---
paths:
  - "Assets/_Project/Scripts/Core/GameInput.cs"
  - "Assets/**/*.inputactions"
  - "Assets/_Project/Scripts/Player/**/*.cs"
---

# Input (Input System 1.20, project-wide actions)

## Facts
- Only `GameInput.cs` may write `using UnityEngine.InputSystem;`. Everything else talks to `GameInput.Instance`.
- Source of truth: `Assets/InputSystem_Actions.inputactions`, registered as **project-wide actions** → `InputSystem.actions`, which is **already enabled** at startup. Don't call `Enable()` on it again, and don't `Dispose()` it.
- There's **no generated C# wrapper** (`InputSystem_Actions` class doesn't exist). Look up actions by `"Map/Action"` string with `throwIfNotFound: true`, so a typo fails loudly in `Awake` instead of silently returning null.
- Existing actions — Player: `Move` (Vector2), `Look` (Vector2), `Attack`, `Interact`, `Crouch`, `Jump`, `Previous`, `Next`, `Sprint`, `Pause` (Esc / pad Start), `Restart` (R / pad Select). UI: `Navigate`, `Submit`, `Cancel`, `Point`, `Click`, `ScrollWheel`, ….
- New actions: add them in the Input Actions editor, or edit the JSON carefully (each action and binding needs a unique new GUID `id`). Then update the action list here and in `project-settings.md`.
- `Interact` is a plain press (the template's Hold interaction was removed — QTE timing and dialogue need instant presses).
- `Interact` binds to `<Keyboard>/e`, `Jump` to Space, `Attack` to LMB/Enter, `Sprint` to LeftShift.

## GameInput template
```csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public event EventHandler OnJumpAction;
    public event EventHandler OnInteractAction;
    public event EventHandler OnPauseAction;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction interactAction;
    private InputAction sprintAction;
    private InputAction pauseAction;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one GameInput", this);
        Instance = this;

        moveAction = InputSystem.actions.FindAction("Player/Move", true);
        jumpAction = InputSystem.actions.FindAction("Player/Jump", true);
        interactAction = InputSystem.actions.FindAction("Player/Interact", true);
        sprintAction = InputSystem.actions.FindAction("Player/Sprint", true);
        pauseAction = InputSystem.actions.FindAction("Player/Pause", true); // add the action first, see above

        jumpAction.performed += Jump_performed;
        interactAction.performed += Interact_performed;
        pauseAction.performed += Pause_performed;
    }

    // the actions asset outlives this object (and domain reload is off) → always unsubscribe
    private void OnDestroy()
    {
        jumpAction.performed -= Jump_performed;
        interactAction.performed -= Interact_performed;
        pauseAction.performed -= Pause_performed;
    }

    private void Jump_performed(InputAction.CallbackContext context) => OnJumpAction?.Invoke(this, EventArgs.Empty);
    private void Interact_performed(InputAction.CallbackContext context) => OnInteractAction?.Invoke(this, EventArgs.Empty);
    private void Pause_performed(InputAction.CallbackContext context) => OnPauseAction?.Invoke(this, EventArgs.Empty);

    public Vector2 GetMovementVectorNormalized() => moveAction.ReadValue<Vector2>().normalized;
    public bool IsSprintPressed() => sprintAction.IsPressed();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
```

## Rules
- Discrete presses (jump, interact, pause) → `performed` callback → C# event. Continuous input (move, look, sprint held) → polling getter, read in `Update`.
- Lambdas can't be unsubscribed, so never subscribe one to `performed`. Always use named methods.
- Movement on the XZ plane: `Vector3 moveDir = new Vector3(input.x, 0f, input.y);`. Make it camera-relative only if the camera rotates.
- Gameplay handlers check `GameManager.Instance.IsGamePlaying()` first.
- Rebinding exists for Interact only (`GameInput.RebindInteract`, pause menu). It uses `PerformInteractiveRebinding` + `SaveBindingOverridesAsJson()` in PlayerPrefs. UI shows keys via `GameInput.GetInteractBindingDisplay()` and refreshes on `OnBindingDisplayChanged` (rebind or keyboard↔gamepad switch).
- UI navigation uses the `UI` map through `InputSystemUIInputModule` on the EventSystem. Call `button.Select()` on panel show so gamepad works.
