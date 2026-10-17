using System;
using UnityEngine;

// A run of N timing checks: a ring shrinks toward the target circle; press [Interact] when they overlap.
public class QTEManager : MonoBehaviour
{
    public const float RING_START_SCALE = 3f;
    private const float PAUSE_BETWEEN_CHECKS = .45f;

    public static QTEManager Instance { get; private set; }

    public event EventHandler<OnCheckStartedEventArgs> OnCheckStarted;
    public class OnCheckStartedEventArgs : EventArgs { public int index; public int count; }
    public event EventHandler<OnCheckResolvedEventArgs> OnCheckResolved;
    public class OnCheckResolvedEventArgs : EventArgs { public int index; public Result result; }
    public event EventHandler<OnQTEFinishedEventArgs> OnQTEFinished;
    public class OnQTEFinishedEventArgs : EventArgs { public bool isSuccess; public int perfectCount; }

    public enum Result { Miss, Hit, Perfect }
    private enum State { Idle, Shrinking, Pause }

    private State state;
    private CropDataSO cropDataSO;
    private int checkIndex;
    private int perfectCount;
    private bool hasMissed;
    private float timer;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one QTEManager", this);
        Instance = this;
    }

    private void Start()
    {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    private void Update()
    {
        switch (state)
        {
            case State.Shrinking:
                timer += Time.deltaTime;
                if (GetRingScale() < 1f - cropDataSO.hitWindow) Resolve(Result.Miss); // ring passed the target: too late
                break;
            case State.Pause:
                timer -= Time.deltaTime;
                if (timer <= 0f) NextCheck();
                break;
        }
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (state != State.Shrinking) return;
        Resolve(EvaluatePress(GetRingScale(), cropDataSO.hitWindow, cropDataSO.perfectWindow));
    }

    public void StartQTE(CropDataSO cropDataSO)
    {
        this.cropDataSO = cropDataSO;
        checkIndex = -1;
        perfectCount = 0;
        hasMissed = false;
        NextCheck();
    }

    // 1 = ring exactly on the target circle
    public float GetRingScale()
    {
        float shrinkPerSecond = (RING_START_SCALE - 1f) / cropDataSO.ringShrinkDuration;
        return RING_START_SCALE - timer * shrinkPerSecond;
    }

    public bool IsRunning() => state != State.Idle;
    public float GetHitWindow() => cropDataSO.hitWindow;
    public float GetPerfectWindow() => cropDataSO.perfectWindow;

    public static Result EvaluatePress(float ringScale, float hitWindow, float perfectWindow)
    {
        float distance = Mathf.Abs(ringScale - 1f);
        if (distance <= perfectWindow) return Result.Perfect;
        return distance <= hitWindow ? Result.Hit : Result.Miss;
    }

    private void NextCheck()
    {
        checkIndex++;
        if (checkIndex >= cropDataSO.qteCheckCount)
        {
            state = State.Idle;
            OnQTEFinished?.Invoke(this, new OnQTEFinishedEventArgs { isSuccess = !hasMissed, perfectCount = perfectCount });
            return;
        }
        timer = 0f;
        state = State.Shrinking;
        OnCheckStarted?.Invoke(this, new OnCheckStartedEventArgs { index = checkIndex, count = cropDataSO.qteCheckCount });
    }

    private void Resolve(Result result)
    {
        if (result == Result.Miss) hasMissed = true;
        if (result == Result.Perfect) perfectCount++;
        state = State.Pause;
        timer = PAUSE_BETWEEN_CHECKS;
        OnCheckResolved?.Invoke(this, new OnCheckResolvedEventArgs { index = checkIndex, result = result });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
