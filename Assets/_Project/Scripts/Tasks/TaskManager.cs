using System;
using UnityEngine;

// Today's checklist. Every objective counts from the moment it is shown; the house unlocks when all but "go home" are done.
public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    public event EventHandler<OnObjectiveProgressEventArgs> OnObjectiveProgress;
    public class OnObjectiveProgressEventArgs : EventArgs { public int index; public bool isComplete; }
    public event EventHandler OnRequiredTasksCompleted;
    // the whole list was swapped (a new story beat)
    public event EventHandler OnObjectivesChanged;

    [SerializeField] private TaskObjectiveSO[] objectiveSOArray;

    private int[] progressArray;
    private bool areRequiredComplete;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one TaskManager", this);
        Instance = this;
        progressArray = new int[objectiveSOArray.Length];
    }

    private void Start()
    {
        Inventory.Instance.OnItemChanged += Inventory_OnItemChanged;
        FabricatorController.OnAnySeedFabricated += FabricatorController_OnAnySeedFabricated;
        FarmPlot.OnAnyPlanted += FarmPlot_OnAnyPlanted;
        FarmPlot.OnAnyHarvested += FarmPlot_OnAnyHarvested;
        SellCrate.OnAnySold += SellCrate_OnAnySold;
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null) Inventory.Instance.OnItemChanged -= Inventory_OnItemChanged;
        FabricatorController.OnAnySeedFabricated -= FabricatorController_OnAnySeedFabricated;
        FarmPlot.OnAnyPlanted -= FarmPlot_OnAnyPlanted;
        FarmPlot.OnAnyHarvested -= FarmPlot_OnAnyHarvested;
        SellCrate.OnAnySold -= SellCrate_OnAnySold;
    }

    private void Inventory_OnItemChanged(object sender, Inventory.OnItemChangedEventArgs e)
    {
        if (e.delta > 0) AddProgress(TaskObjectiveSO.Type.CollectItem, e.itemDataSO, e.delta);
    }

    // "make N seeds" counts seeds, not presses: a run of several batches (or a combo) counts every seed it made
    private void FabricatorController_OnAnySeedFabricated(object sender, FabricatorController.OnAnySeedFabricatedEventArgs e) => AddProgress(TaskObjectiveSO.Type.FabricateSeed, e.seedItemDataSO, e.seedCount);
    private void FarmPlot_OnAnyPlanted(object sender, FarmPlot.OnAnyPlantedEventArgs e) => AddProgress(TaskObjectiveSO.Type.PlantCrop, e.cropDataSO.seedItemDataSO, 1);

    private void FarmPlot_OnAnyHarvested(object sender, FarmPlot.OnAnyHarvestedEventArgs e)
    {
        if (e.isFruit) AddProgress(TaskObjectiveSO.Type.HarvestFruit, e.itemDataSO, 1);
    }

    private void SellCrate_OnAnySold(object sender, SellCrate.OnAnySoldEventArgs e)
    {
        foreach (ItemAmount sold in e.soldItemList) AddProgress(TaskObjectiveSO.Type.SellItem, sold.itemDataSO, sold.amount);
    }

    public void CompleteEnterHouse() => AddProgress(TaskObjectiveSO.Type.EnterHouse, null, 1);
    public void CompleteStory() => AddProgress(TaskObjectiveSO.Type.Story, null, 1);

    // replaces the list; items already in the bag count toward "collect" objectives
    public void SetObjectives(TaskObjectiveSO[] newObjectiveSOArray)
    {
        objectiveSOArray = newObjectiveSOArray;
        progressArray = new int[objectiveSOArray.Length];
        areRequiredComplete = false;
        for (int i = 0; i < objectiveSOArray.Length; i++)
        {
            if (objectiveSOArray[i].type != TaskObjectiveSO.Type.CollectItem) continue;
            progressArray[i] = Mathf.Min(Inventory.Instance.GetCount(objectiveSOArray[i].itemDataSO), objectiveSOArray[i].requiredCount);
        }
        OnObjectivesChanged?.Invoke(this, EventArgs.Empty);
        CheckRequiredComplete();
    }

    public TaskObjectiveSO[] GetObjectiveSOArray() => objectiveSOArray;
    public int GetProgress(int index) => progressArray[index];
    public bool IsComplete(int index) => progressArray[index] >= objectiveSOArray[index].requiredCount;
    public bool AreRequiredTasksComplete() => areRequiredComplete;

    // the first unfinished objective, highlighted in the UI as "do this next"
    public int GetCurrentIndex()
    {
        for (int i = 0; i < objectiveSOArray.Length; i++)
            if (!IsComplete(i)) return i;
        return -1;
    }

    private void AddProgress(TaskObjectiveSO.Type type, ItemDataSO itemDataSO, int amount)
    {
        for (int i = 0; i < objectiveSOArray.Length; i++)
        {
            TaskObjectiveSO objectiveSO = objectiveSOArray[i];
            if (objectiveSO.type != type || IsComplete(i)) continue;
            // an objective naming an item only counts that item ("press Lv.2 seeds", "sell the sawit")
            if (objectiveSO.itemDataSO != null && objectiveSO.itemDataSO != itemDataSO) continue;
            progressArray[i] = Mathf.Min(progressArray[i] + amount, objectiveSO.requiredCount);
            OnObjectiveProgress?.Invoke(this, new OnObjectiveProgressEventArgs { index = i, isComplete = IsComplete(i) });
        }
        CheckRequiredComplete();
    }

    private void CheckRequiredComplete()
    {
        if (areRequiredComplete) return;
        for (int i = 0; i < objectiveSOArray.Length; i++)
            if (objectiveSOArray[i].type != TaskObjectiveSO.Type.EnterHouse && !IsComplete(i)) return;
        areRequiredComplete = true;
        OnRequiredTasksCompleted?.Invoke(this, EventArgs.Empty);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
