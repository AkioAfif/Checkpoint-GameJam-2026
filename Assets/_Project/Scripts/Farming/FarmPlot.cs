using System;
using UnityEngine;

// One farm tile: starts with a basic crop to harvest, then accepts any seed in the bag (planted through a QTE) that grows by
// the evening. The next morning a grown palm is ripe: harvesting its fruit clears the tile for a new seed. A plot that stood
// in yesterday's garden (WorldState) starts as it was left there instead of in its start state.
public class FarmPlot : MonoBehaviour, IInteractable
{
    public static event EventHandler<OnAnyPlantedEventArgs> OnAnyPlanted;
    public class OnAnyPlantedEventArgs : EventArgs { public CropDataSO cropDataSO; }
    public static event EventHandler<OnAnyHarvestedEventArgs> OnAnyHarvested;
    // isFruit: a ripe palm's fruit (not the starting crop)
    public class OnAnyHarvestedEventArgs : EventArgs { public ItemDataSO itemDataSO; public bool isFruit; }

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    // isInstant: the plot's starting state or the morning's (no harvest float-up, no growing pop)
    public class OnStateChangedEventArgs : EventArgs { public State state; public bool isInstant; }
    public enum State { Crop, Empty, Planted, Grown, Ripe }

    [SerializeField] private State startState = State.Crop;
    [SerializeField] private ItemDataSO cropItemDataSO;
    [Tooltip("The seed offered first when several kinds are in the bag (a day can change it).")]
    [SerializeField] private CropDataSO plantableCropDataSO;
    [SerializeField] private string harvestVerb = "collect";
    [SerializeField] private string ripeVerb = "harvest";
    [SerializeField] private string plantVerb = "plant";
    [SerializeField] private string noSeedReason = "Need a Sawit seed";
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private Transform standPoint;

    private State state;
    private CropDataSO plantedCropDataSO;

    private void Start()
    {
        if (WorldState.TryGetGardenPlot(transform.position, out WorldState.GardenPlot gardenPlot)) WakeUp(gardenPlot);
        else SetState(startState, true);
    }

    public bool IsSelectable() => (state == State.Crop || state == State.Empty || state == State.Ripe) && !PlantingController.Instance.IsPlanting();

    public bool CanInteract(Player player) => state == State.Crop || state == State.Ripe || PlantingController.Instance.HasAnySeed();

    public void Interact(Player player)
    {
        switch (state)
        {
            case State.Crop:
                Inventory.Instance.AddItem(cropItemDataSO);
                SetState(State.Empty);
                OnAnyHarvested?.Invoke(this, new OnAnyHarvestedEventArgs { itemDataSO = cropItemDataSO, isFruit = false });
                break;
            case State.Ripe:
                Inventory.Instance.AddItem(plantedCropDataSO.harvestItemDataSO);
                SetState(State.Empty);
                OnAnyHarvested?.Invoke(this, new OnAnyHarvestedEventArgs { itemDataSO = plantedCropDataSO.harvestItemDataSO, isFruit = true });
                break;
            case State.Empty:
                PlantingController.Instance.ChooseSeed(this, plantableCropDataSO);
                break;
        }
    }

    // called by PlantingController when every QTE check succeeded
    public void Plant(CropDataSO cropDataSO)
    {
        plantedCropDataSO = cropDataSO;
        SetState(State.Planted);
        OnAnyPlanted?.Invoke(this, new OnAnyPlantedEventArgs { cropDataSO = cropDataSO });
    }

    public void Grow()
    {
        if (state == State.Planted) SetState(State.Grown);
    }

    // a palm planted on an earlier day, bearing fruit this morning
    public void SetRipe(CropDataSO cropDataSO)
    {
        plantedCropDataSO = cropDataSO;
        SetState(State.Ripe, true);
    }

    // the seed a later day plants here
    public void SetPlantableCrop(CropDataSO cropDataSO) => plantableCropDataSO = cropDataSO;

    public string GetPromptVerb() => state == State.Crop ? harvestVerb : state == State.Ripe ? ripeVerb : plantVerb;
    public string GetBlockedReason() => noSeedReason;
    public Transform GetPromptAnchor() => promptAnchor;
    public Transform GetStandPoint() => standPoint;
    public State GetState() => state;
    public ItemDataSO GetCropItemDataSO() => cropItemDataSO;
    public CropDataSO GetPlantedCropDataSO() => plantedCropDataSO;

    // overnight: whatever grew here is ripe this morning (it keeps its own crop); empty and crop plots stay so
    private void WakeUp(WorldState.GardenPlot gardenPlot)
    {
        bool hasPalm = gardenPlot.state == State.Planted || gardenPlot.state == State.Grown || gardenPlot.state == State.Ripe;
        if (hasPalm && gardenPlot.cropDataSO != null) SetRipe(gardenPlot.cropDataSO);
        else SetState(hasPalm ? State.Empty : gardenPlot.state, true);
    }

    private void SetState(State state, bool isInstant = false)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state, isInstant = isInstant });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStaticData()
    {
        OnAnyPlanted = null;
        OnAnyHarvested = null;
    }
}
