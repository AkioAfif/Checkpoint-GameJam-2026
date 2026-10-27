#if DEVELOPER_TOOLS || UNITY_EDITOR
using System;
using UnityEngine;

// Developer playtesting tools for the current scene. DevToolsBootstrap adds it to every scene.
// Owns the shortcuts and the facade the menu calls; each feature lives in its own class.
public class DevTools : MonoBehaviour
{
    public event EventHandler<OnMessageEventArgs> OnMessage;
    public class OnMessageEventArgs : EventArgs { public string text; public bool isError; }

    private DevToolsSettingsSO settings;
    private DevCinematicControl cinematic;
    private DevQuickSave quickSave;
    private DevActions actions;
    private DevToolsMenuUI menu;
    private bool isReady;
    private bool hasSpawnPose;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private void Start()
    {
        // only gameplay scenes have these; loading screens and menus drop the tools
        if (GameInput.Instance == null || GameManager.Instance == null || Player.Instance == null || Inventory.Instance == null || TaskManager.Instance == null)
        {
            Destroy(gameObject);
            return;
        }
        isReady = true;
        settings = DevToolsSettingsSO.Load();
        cinematic = new DevCinematicControl(settings);
        quickSave = new DevQuickSave();
        actions = new DevActions(settings);
        menu = new DevToolsMenuUI(this, settings);
        EnsureEventSystem();
    }

    private void Update()
    {
        if (!isReady) return;
        // the scene's spawn: where the player stands the first time free play starts (after the scene's own setup teleports)
        if (!hasSpawnPose && GameManager.Instance.GetState() == GameManager.State.Playing)
        {
            hasSpawnPose = true;
            spawnPosition = Player.Instance.transform.position;
            spawnRotation = Player.Instance.transform.rotation;
        }
        quickSave.Tick(Time.unscaledDeltaTime);
        cinematic.Tick(Time.unscaledDeltaTime);
        menu.Tick(Time.unscaledDeltaTime);
        HandleShortcuts();
    }

    private void LateUpdate()
    {
        if (isReady) cinematic.ApplyTimeScale();
    }

    private void OnDestroy()
    {
        if (!isReady) return;
        quickSave.Dispose();
        cinematic.Restore();
        if (GameInput.Instance != null) GameInput.Instance.SetInputLocked(false);
    }

    private void HandleShortcuts()
    {
        GameInput input = GameInput.Instance;
        if (input.WasKeyPressedThisFrame(settings.toggleMenuKey)) ToggleMenu();
        if (input.WasKeyPressedThisFrame(settings.quickSaveKey)) QuickSave();
        if (input.WasKeyPressedThisFrame(settings.quickLoadKey)) QuickLoad();
        if (input.WasKeyPressedThisFrame(settings.skipKey)) SkipCinematic();
        if (input.WasKeyPressedThisFrame(settings.fastForwardKey)) FastForward();
        if (input.WasKeyPressedThisFrame(settings.skipFabricatorKey)) SkipFabricator();
    }

    // ----- menu -----

    public void ToggleMenu()
    {
        bool isOpen = !menu.IsOpen();
        menu.SetOpen(isOpen);
        // while the menu is open, gameplay sees no input (its clicks must not dig, interact or pause)
        GameInput.Instance.SetInputLocked(isOpen);
    }

    // ----- actions (each one reports its result as a message) -----

    public void SkipCinematic()
    {
        if (!cinematic.IsCinematic() && !IsDialogueActive())
        {
            Notify("Nothing to skip: no cutscene or dialogue is playing");
            return;
        }
        cinematic.StartSkip();
        Notify("Skipping cutscene and dialogue");
    }

    public void FastForward()
    {
        Notify("Fast-forward " + cinematic.CycleFastForward() + "x (cutscenes and dialogue only)");
    }

    public void QuickSave()
    {
        bool isSaved = quickSave.TrySave(out string message);
        Notify(message, !isSaved);
    }

    public void QuickLoad()
    {
        DevQuickSave.LoadResult result = quickSave.TryLoad(out string message);
        Notify(message, result == DevQuickSave.LoadResult.Failed);
    }

    public void SkipFabricator()
    {
        bool isSkipped = actions.TrySkipFabricator(out string message);
        Notify(message, !isSkipped);
    }

    public void AbortFabricator()
    {
        bool isAborted = actions.TryAbortFabricator(out string message);
        Notify(message, !isAborted);
    }

    public void CompleteObjective()
    {
        bool isDone = actions.TryCompleteObjective(out string message);
        Notify(message, !isDone);
    }

    public void ResetObjective()
    {
        bool isReset = actions.TryResetObjective(out string message);
        Notify(message, !isReset);
    }

    public void ResetPlayer()
    {
        bool isReset = actions.TryResetPlayer(hasSpawnPose, spawnPosition, spawnRotation, out string message);
        Notify(message, !isReset);
    }

    public void AddTestItem(ItemDataSO itemDataSO)
    {
        Notify(actions.AddTestItem(itemDataSO));
    }

    public void ResetTestInventory()
    {
        Notify(actions.ResetTestInventory());
    }

    // ----- status for the menu -----

    public bool IsCinematicPlaying() => cinematic.IsCinematic();
    public bool IsDialogueActive() => DialogueManager.Instance != null && DialogueManager.Instance.IsActive();
    public bool IsSkipArmed() => cinematic.IsSkipArmed();
    public float GetFastForwardSpeed() => cinematic.GetFastForwardSpeed();
    public bool IsFabricatorActive() => actions.IsFabricatorActive();
    public string GetFabricatorStatus() => actions.GetFabricatorStatus();
    public bool HasQuickSave() => quickSave.HasSave;
    public bool IsQuickLoadConfirming() => quickSave.IsConfirming();
    public string GetQuickSaveStatus() => quickSave.GetStatus();
    public bool IsFreePlay() => GameManager.Instance.GetState() == GameManager.State.Playing;
    public bool HasSpawnPose() => hasSpawnPose;
    public string GetObjectiveStatus() => actions.GetObjectiveStatus();
    public bool HasObjectives() => actions.HasObjectives();
    public DevToolsSettingsSO GetSettings() => settings;

    // ----- helpers -----

    private void Notify(string text, bool isError = false)
    {
        OnMessage?.Invoke(this, new OnMessageEventArgs { text = text, isError = isError });
    }

    // a scene with no EventSystem gets one with the Input System UI module (the legacy module throws in this project)
    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}
#endif
