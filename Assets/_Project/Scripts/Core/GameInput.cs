using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    private const string PLAYER_PREFS_BINDINGS = "InputBindings";
    private const string KEYBOARD_GROUP = "Keyboard&Mouse";
    private const string GAMEPAD_GROUP = "Gamepad";

    public static GameInput Instance { get; private set; }

    public event EventHandler OnInteractAction;
    public event EventHandler OnPauseAction;
    public event EventHandler OnRestartAction;
    // the Interact key text changed (rebind, or switched between keyboard and gamepad)
    public event EventHandler OnBindingDisplayChanged;

    private InputAction moveAction;
    private InputAction interactAction;
    private InputAction sprintAction;
    private InputAction pauseAction;
    private InputAction restartAction;
    private InputAction digAction;
    private InputAction pointAction;
    private bool isUsingGamepad;
    private bool isRebinding;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one GameInput", this);
        Instance = this;

        moveAction = InputSystem.actions.FindAction("Player/Move", true);
        interactAction = InputSystem.actions.FindAction("Player/Interact", true);
        sprintAction = InputSystem.actions.FindAction("Player/Sprint", true);
        pauseAction = InputSystem.actions.FindAction("Player/Pause", true);
        restartAction = InputSystem.actions.FindAction("Player/Restart", true);
        digAction = InputSystem.actions.FindAction("Player/Attack", true);
        pointAction = InputSystem.actions.FindAction("UI/Point", true);

        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS)) InputSystem.actions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));

        interactAction.performed += Interact_performed;
        pauseAction.performed += Pause_performed;
        restartAction.performed += Restart_performed;
        InputSystem.onActionChange += InputSystem_onActionChange;
    }

    // the actions asset outlives this object (and domain reload is off) → always unsubscribe
    private void OnDestroy()
    {
        interactAction.performed -= Interact_performed;
        pauseAction.performed -= Pause_performed;
        restartAction.performed -= Restart_performed;
        InputSystem.onActionChange -= InputSystem_onActionChange;
    }

    private void Interact_performed(InputAction.CallbackContext context) => OnInteractAction?.Invoke(this, EventArgs.Empty);
    private void Restart_performed(InputAction.CallbackContext context) => OnRestartAction?.Invoke(this, EventArgs.Empty);

    private void Pause_performed(InputAction.CallbackContext context)
    {
        if (isRebinding) return; // Esc cancels the rebind, it must not also close the pause menu
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    private void InputSystem_onActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed) return;
        InputControl control = ((InputAction)obj).activeControl;
        if (control == null) return;
        bool gamepad = control.device is Gamepad;
        if (gamepad == isUsingGamepad) return;
        isUsingGamepad = gamepad;
        OnBindingDisplayChanged?.Invoke(this, EventArgs.Empty);
    }

    public Vector2 GetMovementVectorNormalized() => moveAction.ReadValue<Vector2>().normalized;
    public bool IsSprintPressed() => sprintAction.IsPressed();
    public bool IsDigPressed() => digAction.IsPressed();
    public Vector2 GetPointerScreenPosition() => pointAction.ReadValue<Vector2>();

    public string GetInteractBindingDisplay()
    {
        return interactAction.GetBindingDisplayString(InputBinding.MaskByGroup(isUsingGamepad ? GAMEPAD_GROUP : KEYBOARD_GROUP));
    }

    // rebinds the keyboard Interact key; Esc cancels
    public void RebindInteract(Action onComplete)
    {
        int bindingIndex = interactAction.GetBindingIndex(InputBinding.MaskByGroup(KEYBOARD_GROUP));
        isRebinding = true;
        interactAction.Disable();
        interactAction.PerformInteractiveRebinding(bindingIndex)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(operation => FinishRebind(operation, onComplete))
            .OnCancel(operation => FinishRebind(operation, onComplete))
            .Start();
    }

    public void ResetBindings()
    {
        InputSystem.actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(PLAYER_PREFS_BINDINGS);
        OnBindingDisplayChanged?.Invoke(this, EventArgs.Empty);
    }

    private void FinishRebind(InputActionRebindingExtensions.RebindingOperation operation, Action onComplete)
    {
        operation.Dispose();
        interactAction.Enable();
        isRebinding = false;
        PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, InputSystem.actions.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
        OnBindingDisplayChanged?.Invoke(this, EventArgs.Empty);
        onComplete();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
