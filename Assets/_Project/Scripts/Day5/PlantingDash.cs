using System;
using System.Collections.Generic;
using UnityEngine;

// Day 5's "Plough Dash": after another bite of sawit the MC plants the empty plantation by dashing through it. Hold Space
// to charge, release to dash straight ahead; every empty plantation plot the MC passes over is planted with a Lv.2 seed
// from the bag. Dashes are limited, so lining up the rows matters; when they run out with beds left, they refill
// (OnDashesRefilled) so the field can always be finished. [Interact] on a plot still plants it the slow way.
public class PlantingDash : MonoBehaviour
{
    public event EventHandler<OnChargeChangedEventArgs> OnChargeChanged;
    public class OnChargeChangedEventArgs : EventArgs { public float chargeNormalized; }
    public event EventHandler OnDashesChanged;
    public event EventHandler OnDashesRefilled;
    public event EventHandler OnDashed;
    public event EventHandler<OnPlotPlantedEventArgs> OnPlotPlanted;
    public class OnPlotPlantedEventArgs : EventArgs { public Vector3 position; }
    public event EventHandler OnFinished;

    [SerializeField] private FarmPlot[] plantationPlotArray;
    [SerializeField] private ItemDataSO seedItemDataSO;
    [SerializeField] private CropDataSO cropDataSO;
    [SerializeField] private LayerMask interactableLayerMask;
    [Tooltip("Seconds of holding for a full charge.")]
    [SerializeField] private float chargeDuration = 1f;
    [SerializeField] private float minDashDistance = 4f;
    [SerializeField] private float maxDashDistance = 20f;
    [SerializeField] private float dashDuration = .35f;
    [Tooltip("How close to a plot's centre the MC passes to plant it.")]
    [SerializeField] private float plantRadius = 1.1f;
    [SerializeField] private int dashesPerBunch = 4;
    [SerializeField] private int minDashes = 6;

    private bool isActive;
    private bool isCharging;
    private float charge;
    private int dashesLeft;
    private int dashesPerRefill;
    private readonly HashSet<FarmPlot> plotSet = new HashSet<FarmPlot>();
    private readonly Collider[] hitArray = new Collider[16];
    // raised every frame while charging
    private readonly OnChargeChangedEventArgs chargeChangedEventArgs = new OnChargeChangedEventArgs();

    private void Awake()
    {
        foreach (FarmPlot plot in plantationPlotArray) plotSet.Add(plot);
    }

    private void Start()
    {
        FarmPlot.OnAnyPlanted += FarmPlot_OnAnyPlanted;
    }

    private void Update()
    {
        if (!isActive) return;
        Player player = Player.Instance;
        if (player.IsDashing())
        {
            PlantUnder(player.transform.position);
            return;
        }
        bool isPressed = GameManager.Instance.IsGamePlaying() && GameInput.Instance.IsJumpPressed();
        if (isPressed)
        {
            isCharging = true;
            SetCharge(Mathf.Min(1f, charge + Time.deltaTime / chargeDuration));
        }
        else if (isCharging)
        {
            isCharging = false;
            float distance = Mathf.Lerp(minDashDistance, maxDashDistance, charge);
            player.Dash(player.transform.forward, distance / dashDuration, dashDuration);
            SetCharge(0f);
            dashesLeft--;
            OnDashed?.Invoke(this, EventArgs.Empty);
            if (dashesLeft <= 0 && !IsFieldPlanted())
            {
                dashesLeft = dashesPerRefill;
                OnDashesRefilled?.Invoke(this, EventArgs.Empty);
            }
            OnDashesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnDestroy()
    {
        FarmPlot.OnAnyPlanted -= FarmPlot_OnAnyPlanted;
    }

    // planted by dash or by hand
    private void FarmPlot_OnAnyPlanted(object sender, FarmPlot.OnAnyPlantedEventArgs e) => CheckFinished();

    // more bunches eaten, more dashes
    public void Begin(int bunchesEaten)
    {
        dashesPerRefill = Mathf.Max(minDashes, bunchesEaten * dashesPerBunch);
        dashesLeft = dashesPerRefill;
        isActive = true;
        OnDashesChanged?.Invoke(this, EventArgs.Empty);
        // a plot planted by hand before the dash counts too
        CheckFinished();
    }

    public bool IsActive() => isActive;
    public int GetDashesLeft() => dashesLeft;

    private void PlantUnder(Vector3 position)
    {
        int count = Physics.OverlapSphereNonAlloc(position, plantRadius, hitArray, interactableLayerMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            FarmPlot plot = hitArray[i].GetComponentInParent<FarmPlot>();
            if (plot == null || !plotSet.Contains(plot) || plot.GetState() != FarmPlot.State.Empty) continue;
            if (!Inventory.Instance.TryRemoveItem(seedItemDataSO)) return;
            plot.Plant(cropDataSO);
            OnPlotPlanted?.Invoke(this, new OnPlotPlantedEventArgs { position = plot.transform.position });
        }
    }

    private void CheckFinished()
    {
        if (!isActive || !IsFieldPlanted()) return;
        isActive = false;
        SetCharge(0f);
        OnFinished?.Invoke(this, EventArgs.Empty);
    }

    private bool IsFieldPlanted()
    {
        foreach (FarmPlot plot in plantationPlotArray)
            if (plot.GetState() == FarmPlot.State.Empty) return false;
        return true;
    }

    private void SetCharge(float charge)
    {
        this.charge = charge;
        chargeChangedEventArgs.chargeNormalized = charge;
        OnChargeChanged?.Invoke(this, chargeChangedEventArgs);
    }
}
