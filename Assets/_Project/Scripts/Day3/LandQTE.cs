using System;
using UnityEngine;

// The two Interact challenges of Day 3's new land: hold [Interact] for a while (eating), or mash it to fill a meter
// several times (each fill is one blow on the ground). There is no fail state; letting go only drains the meter.
public class LandQTE : MonoBehaviour
{
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }
    public event EventHandler<OnProgressChangedEventArgs> OnProgressChanged;
    public class OnProgressChangedEventArgs : EventArgs { public float progressNormalized; }
    public event EventHandler<OnFilledEventArgs> OnFilled;
    public class OnFilledEventArgs : EventArgs { public int index; public bool isLast; }

    public enum State { Idle, Hold, Mash }

    [SerializeField] private float holdDrainSpeed = 2f;
    [Tooltip("Meter lost per second while the player stops mashing.")]
    [SerializeField] private float mashDrainSpeed = .35f;

    private State state;
    private float progress;
    private float holdDuration;
    private int pressesPerFill;
    private int fillCount;
    private int fillIndex;
    // below 1 the meter can never fill: an effort that's doomed (pulling a stump alone)
    private float maxProgress = 1f;
    // a hold only counts once the key that closed the last dialogue line has been let go
    private bool isReleased;
    private readonly OnProgressChangedEventArgs progressChangedEventArgs = new OnProgressChangedEventArgs();

    private void Start()
    {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    private void Update()
    {
        switch (state)
        {
            case State.Hold:
                bool isPressed = GameInput.Instance.IsInteractPressed();
                if (!isPressed) isReleased = true;
                bool isHolding = isPressed && isReleased;
                SetProgress(Mathf.Clamp01(progress + (isHolding ? Time.deltaTime / holdDuration : -holdDrainSpeed * Time.deltaTime)));
                if (progress >= 1f) SetState(State.Idle);
                break;
            case State.Mash:
                if (progress > 0f) SetProgress(Mathf.Max(0f, progress - mashDrainSpeed * Time.deltaTime));
                break;
        }
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (state != State.Mash || GameManager.Instance.IsGamePaused()) return;
        SetProgress(Mathf.Min(progress + 1f / pressesPerFill, maxProgress));
        if (progress < 1f) return;

        bool isLast = fillIndex == fillCount - 1;
        OnFilled?.Invoke(this, new OnFilledEventArgs { index = fillIndex, isLast = isLast });
        fillIndex++;
        SetProgress(0f);
        if (isLast) SetState(State.Idle);
    }

    public void StartHold(float holdDuration)
    {
        this.holdDuration = holdDuration;
        isReleased = false;
        SetProgress(0f);
        SetState(State.Hold);
    }

    public void StartMash(int pressesPerFill, int fillCount, float maxProgress = 1f)
    {
        this.pressesPerFill = pressesPerFill;
        this.fillCount = fillCount;
        this.maxProgress = maxProgress;
        fillIndex = 0;
        SetProgress(0f);
        SetState(State.Mash);
    }

    // gives up on the current challenge without finishing it
    public void Stop()
    {
        SetProgress(0f);
        SetState(State.Idle);
    }

    public State GetState() => state;
    public float GetProgress() => progress;

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }

    private void SetProgress(float progressNormalized)
    {
        progress = progressNormalized;
        progressChangedEventArgs.progressNormalized = progressNormalized;
        OnProgressChanged?.Invoke(this, progressChangedEventArgs);
    }
}
