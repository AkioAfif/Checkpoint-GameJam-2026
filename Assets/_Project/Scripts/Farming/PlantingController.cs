using System;
using UnityEngine;

// Planting sequence: step to the plot → crouch + camera zoom → QTE checks → result → stand up.
public class PlantingController : MonoBehaviour
{
    public static PlantingController Instance { get; private set; }

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }
    public event EventHandler<OnPlantingFinishedEventArgs> OnPlantingFinished;
    public class OnPlantingFinishedEventArgs : EventArgs { public bool isSuccess; public FarmPlot farmPlot; }

    public enum State { Idle, Crouching, QTE, Result, StandingUp }

    [SerializeField] private float crouchDuration = .9f;
    [SerializeField] private float resultDuration = 1.4f;
    [SerializeField] private float standUpDuration = .4f;

    private State state;
    private float timer;
    private FarmPlot farmPlot;
    private CropDataSO cropDataSO;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one PlantingController", this);
        Instance = this;
    }

    private void Start()
    {
        QTEManager.Instance.OnQTEFinished += QTEManager_OnQTEFinished;
    }

    private void Update()
    {
        if (state == State.Idle || state == State.QTE) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        switch (state)
        {
            case State.Crouching:
                SetState(State.QTE, 0f);
                QTEManager.Instance.StartQTE(cropDataSO);
                break;
            case State.Result:
                SetState(State.StandingUp, standUpDuration);
                break;
            case State.StandingUp:
                SetState(State.Idle, 0f);
                GameManager.Instance.SetState(GameManager.State.Playing);
                break;
        }
    }

    private void OnDestroy()
    {
        if (QTEManager.Instance != null) QTEManager.Instance.OnQTEFinished -= QTEManager_OnQTEFinished;
    }

    private void QTEManager_OnQTEFinished(object sender, QTEManager.OnQTEFinishedEventArgs e)
    {
        // a failed plant keeps the seed, so the task can never soft-lock
        if (e.isSuccess && Inventory.Instance.TryRemoveItem(cropDataSO.seedItemDataSO)) farmPlot.Plant(cropDataSO);
        SetState(State.Result, resultDuration);
        OnPlantingFinished?.Invoke(this, new OnPlantingFinishedEventArgs { isSuccess = e.isSuccess, farmPlot = farmPlot });
    }

    public void StartPlanting(FarmPlot farmPlot, CropDataSO cropDataSO)
    {
        if (state != State.Idle) return;
        this.farmPlot = farmPlot;
        this.cropDataSO = cropDataSO;
        Player.Instance.Teleport(farmPlot.GetStandPoint().position);
        Player.Instance.FaceTowards(farmPlot.transform.position);
        GameManager.Instance.SetState(GameManager.State.Planting);
        SetState(State.Crouching, crouchDuration);
    }

    public bool IsPlanting() => state != State.Idle;
    public bool IsCrouched() => state == State.Crouching || state == State.QTE;

    private void SetState(State state, float duration)
    {
        this.state = state;
        timer = duration;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
