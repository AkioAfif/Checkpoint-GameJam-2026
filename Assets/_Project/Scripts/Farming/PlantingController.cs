using System;
using System.Collections.Generic;
using UnityEngine;

// Planting sequence: pick a seed (only when the bag has more than one kind) → step to the plot → crouch + camera zoom →
// QTE checks → result → stand up. The seed is only used up when the QTE succeeds.
public class PlantingController : MonoBehaviour
{
    public static PlantingController Instance { get; private set; }

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }
    public event EventHandler<OnPlantingFinishedEventArgs> OnPlantingFinished;
    public event EventHandler OnSeedChoiceChanged;
    public class OnPlantingFinishedEventArgs : EventArgs { public bool isSuccess; public FarmPlot farmPlot; }

    public enum State { Idle, Crouching, QTE, Result, StandingUp, Choosing }

    [SerializeField] private float crouchDuration = .9f;
    [SerializeField] private float resultDuration = 1.4f;
    [SerializeField] private float standUpDuration = .4f;
    [Tooltip("Every crop a seed can be planted for.")]
    [SerializeField] private CropListSO cropListSO;

    private State state;
    private float timer;
    private FarmPlot farmPlot;
    private CropDataSO cropDataSO;
    // the seeds in the bag, the plot's own crop first
    private readonly List<CropDataSO> seedChoiceList = new List<CropDataSO>();
    private int seedChoiceIndex;
    // the Interact press that opened the choice must not also confirm it
    private int choosingStartFrame;
    // A/D change the seed once per press, not every frame they are held
    private int heldSeedStep;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one PlantingController", this);
        Instance = this;
    }

    private void Start()
    {
        QTEManager.Instance.OnQTEFinished += QTEManager_OnQTEFinished;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
    }

    private void Update()
    {
        if (state == State.Choosing)
        {
            float horizontal = GameInput.Instance.GetMovementVectorNormalized().x;
            int step = horizontal > .5f ? 1 : horizontal < -.5f ? -1 : 0;
            if (step != 0 && step != heldSeedStep) ChangeSeed(step);
            heldSeedStep = step;
            return;
        }
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
        if (GameInput.Instance == null) return;
        GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
        GameInput.Instance.OnPauseAction -= GameInput_OnPauseAction;
    }

    private void QTEManager_OnQTEFinished(object sender, QTEManager.OnQTEFinishedEventArgs e)
    {
        // a failed plant keeps the seed, so the task can never soft-lock
        if (e.isSuccess && Inventory.Instance.TryRemoveItem(cropDataSO.seedItemDataSO)) farmPlot.Plant(cropDataSO);
        SetState(State.Result, resultDuration);
        OnPlantingFinished?.Invoke(this, new OnPlantingFinishedEventArgs { isSuccess = e.isSuccess, farmPlot = farmPlot });
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (state == State.Choosing && Time.frameCount != choosingStartFrame) ConfirmSeed();
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        if (state == State.Choosing) CancelSeed();
    }

    public bool HasAnySeed()
    {
        foreach (CropDataSO crop in cropListSO.cropDataSOList)
            if (HasSeed(crop)) return true;
        return false;
    }

    // one kind of seed in the bag: straight to planting; several: the player picks (SeedChoiceUI)
    public void ChooseSeed(FarmPlot farmPlot, CropDataSO preferredCropDataSO)
    {
        if (state != State.Idle) return;
        seedChoiceList.Clear();
        seedChoiceIndex = 0;
        if (preferredCropDataSO != null && HasSeed(preferredCropDataSO)) seedChoiceList.Add(preferredCropDataSO);
        foreach (CropDataSO crop in cropListSO.cropDataSOList)
            if (crop != preferredCropDataSO && HasSeed(crop)) seedChoiceList.Add(crop);
        if (seedChoiceList.Count == 0) return;
        if (seedChoiceList.Count == 1)
        {
            StartPlanting(farmPlot, seedChoiceList[0]);
            return;
        }
        this.farmPlot = farmPlot;
        heldSeedStep = 0;
        choosingStartFrame = Time.frameCount;
        GameManager.Instance.SetState(GameManager.State.Planting);
        SetState(State.Choosing, 0f);
        OnSeedChoiceChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ChangeSeed(int delta)
    {
        if (state != State.Choosing) return;
        seedChoiceIndex = (seedChoiceIndex + delta + seedChoiceList.Count) % seedChoiceList.Count;
        OnSeedChoiceChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ConfirmSeed()
    {
        if (state != State.Choosing) return;
        StartPlanting(farmPlot, GetChosenCropDataSO());
    }

    // nothing is used up; control comes back next frame (Esc must not also open the pause menu)
    public void CancelSeed()
    {
        if (state != State.Choosing) return;
        SetState(State.StandingUp, 0f);
    }

    public CropDataSO GetChosenCropDataSO() => seedChoiceList[seedChoiceIndex];
    public int GetSeedChoiceIndex() => seedChoiceIndex;
    public int GetSeedChoiceCount() => seedChoiceList.Count;

    private static bool HasSeed(CropDataSO crop) => Inventory.Instance.GetCount(crop.seedItemDataSO) > 0;

    private void StartPlanting(FarmPlot farmPlot, CropDataSO cropDataSO)
    {
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
