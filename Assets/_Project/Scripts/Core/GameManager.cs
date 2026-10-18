using System;
using UnityEngine;

// Which mode the level is in. Gameplay systems request a mode; everyone else listens to OnStateChanged.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }
    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnpaused;

    public enum State { Playing, Dialogue, Popup, Fabricating, Planting, EnteringHouse, Cutscene }

    private State state;
    private bool isGamePaused;
    private int stateChangedFrame = -1;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one GameManager", this);
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        SetState(State.Playing);
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnPauseAction -= GameInput_OnPauseAction;
    }

    // Esc only pauses free roaming; the Fabricator uses it to leave
    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        if (state == State.Playing) TogglePauseGame();
    }

    public void TogglePauseGame()
    {
        isGamePaused = !isGamePaused;
        RefreshTimeScale();
        if (isGamePaused) OnGamePaused?.Invoke(this, EventArgs.Empty);
        else OnGameUnpaused?.Invoke(this, EventArgs.Empty);
    }

    public void SetState(State state)
    {
        this.state = state;
        stateChangedFrame = Time.frameCount;
        RefreshTimeScale();
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }

    public State GetState() => state;
    public bool IsGamePlaying() => state == State.Playing && !isGamePaused && !IsStateChangedThisFrame();
    // one [Interact] press must not both close a dialogue/popup and start the next interaction
    public bool IsStateChangedThisFrame() => stateChangedFrame == Time.frameCount;
    public bool IsGamePaused() => isGamePaused;

    // the discovery popup freezes the world like the pause menu does
    private void RefreshTimeScale() => Time.timeScale = isGamePaused || state == State.Popup ? 0f : 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
