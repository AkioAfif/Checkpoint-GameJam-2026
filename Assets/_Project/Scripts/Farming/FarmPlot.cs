using System;
using UnityEngine;

// One farm tile: starts with a basic crop to harvest, then accepts a fabricated seed (planted through a QTE).
public class FarmPlot : MonoBehaviour, IInteractable
{
    public static event EventHandler OnAnyPlanted;
    public static event EventHandler OnAnyHarvested;

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }
    public enum State { Crop, Empty, Planted }

    [SerializeField] private State startState = State.Crop;
    [SerializeField] private ItemDataSO cropItemDataSO;
    [SerializeField] private CropDataSO plantableCropDataSO;
    [SerializeField] private string harvestVerb = "collect";
    [SerializeField] private string plantVerb = "plant";
    [SerializeField] private string noSeedReason = "Need a Sawit seed";
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private Transform standPoint;

    private State state;
    private CropDataSO plantedCropDataSO;

    private void Start()
    {
        SetState(startState);
    }

    public bool IsSelectable() => state != State.Planted && !PlantingController.Instance.IsPlanting();

    public bool CanInteract(Player player) => state == State.Crop || Inventory.Instance.GetCount(plantableCropDataSO.seedItemDataSO) > 0;

    public void Interact(Player player)
    {
        switch (state)
        {
            case State.Crop:
                Inventory.Instance.AddItem(cropItemDataSO);
                SetState(State.Empty);
                OnAnyHarvested?.Invoke(this, EventArgs.Empty);
                break;
            case State.Empty:
                PlantingController.Instance.StartPlanting(this, plantableCropDataSO);
                break;
        }
    }

    // called by PlantingController when every QTE check succeeded
    public void Plant(CropDataSO cropDataSO)
    {
        plantedCropDataSO = cropDataSO;
        SetState(State.Planted);
        OnAnyPlanted?.Invoke(this, EventArgs.Empty);
    }

    public string GetPromptVerb() => state == State.Crop ? harvestVerb : plantVerb;
    public string GetBlockedReason() => noSeedReason;
    public Transform GetPromptAnchor() => promptAnchor;
    public Transform GetStandPoint() => standPoint;
    public State GetState() => state;
    public ItemDataSO GetCropItemDataSO() => cropItemDataSO;
    public CropDataSO GetPlantedCropDataSO() => plantedCropDataSO;

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStaticData()
    {
        OnAnyPlanted = null;
        OnAnyHarvested = null;
    }
}
