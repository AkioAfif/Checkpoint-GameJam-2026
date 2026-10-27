using System;
using System.Collections.Generic;
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
    public event EventHandler OnToggleTasksAction;
    // the Interact key text changed (rebind, or switched between keyboard and gamepad)
    public event EventHandler OnBindingDisplayChanged;

    private InputAction moveAction;
    private InputAction interactAction;
    private InputAction sprintAction;
    private InputAction pauseAction;
    private InputAction restartAction;
    private InputAction toggleTasksAction;
    private InputAction digAction;
    private InputAction pointAction;
    private bool isUsingGamepad;
    private bool isRebinding;
    // dev tools menu open: gameplay sees no input at all (the menu's own clicks must not dig, interact or pause)
    private bool isInputLocked;
    private readonly Dictionary<KeyCode, Key> keyLookup = new Dictionary<KeyCode, Key>();

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one GameInput", this);
        Instance = this;

        moveAction = InputSystem.actions.FindAction("Player/Move", true);
        interactAction = InputSystem.actions.FindAction("Player/Interact", true);
        sprintAction = InputSystem.actions.FindAction("Player/Sprint", true);
        pauseAction = InputSystem.actions.FindAction("Player/Pause", true);
        restartAction = InputSystem.actions.FindAction("Player/Restart", true);
        toggleTasksAction = InputSystem.actions.FindAction("Player/ToggleTasks", true);
        digAction = InputSystem.actions.FindAction("Player/Attack", true);
        pointAction = InputSystem.actions.FindAction("UI/Point", true);

        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS)) InputSystem.actions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));

        interactAction.performed += Interact_performed;
        pauseAction.performed += Pause_performed;
        restartAction.performed += Restart_performed;
        toggleTasksAction.performed += ToggleTasks_performed;
        InputSystem.onActionChange += InputSystem_onActionChange;
    }

    // the actions asset outlives this object (and domain reload is off) → always unsubscribe
    private void OnDestroy()
    {
        interactAction.performed -= Interact_performed;
        pauseAction.performed -= Pause_performed;
        restartAction.performed -= Restart_performed;
        toggleTasksAction.performed -= ToggleTasks_performed;
        InputSystem.onActionChange -= InputSystem_onActionChange;
    }

    private void Interact_performed(InputAction.CallbackContext context)
    {
        if (!isInputLocked) OnInteractAction?.Invoke(this, EventArgs.Empty);
    }

    private void Restart_performed(InputAction.CallbackContext context)
    {
        if (!isInputLocked) OnRestartAction?.Invoke(this, EventArgs.Empty);
    }

    private void ToggleTasks_performed(InputAction.CallbackContext context)
    {
        if (!isInputLocked) OnToggleTasksAction?.Invoke(this, EventArgs.Empty);
    }

    private void Pause_performed(InputAction.CallbackContext context)
    {
        if (isRebinding || isInputLocked) return; // Esc cancels the rebind, it must not also close the pause menu
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

    public Vector2 GetMovementVectorNormalized() => isInputLocked ? Vector2.zero : moveAction.ReadValue<Vector2>().normalized;
    public bool IsSprintPressed() => !isInputLocked && sprintAction.IsPressed();
    public bool IsDigPressed() => !isInputLocked && digAction.IsPressed();
    public bool IsInteractPressed() => !isInputLocked && interactAction.IsPressed();
    public Vector2 GetPointerScreenPosition() => pointAction.ReadValue<Vector2>();

    // developer tools only: while locked, every gameplay getter and event above reads as "nothing pressed"
    public void SetInputLocked(bool isLocked) => isInputLocked = isLocked;

    // developer tools shortcuts. Works while input is locked, so the menu can always be opened and closed.
    public bool WasKeyPressedThisFrame(KeyCode keyCode)
    {
        if (Keyboard.current == null) return false;
        if (!keyLookup.TryGetValue(keyCode, out Key key))
        {
            Enum.TryParse(keyCode.ToString(), out key); // KeyCode.F5 and Key.F5 share names; Key.None when there is no match
            keyLookup[keyCode] = key;
        }
        return key != Key.None && Keyboard.current[key].wasPressedThisFrame;
    }

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
