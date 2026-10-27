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
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null) Inventory.Instance.OnItemChanged -= Inventory_OnItemChanged;
        FabricatorController.OnAnySeedFabricated -= FabricatorController_OnAnySeedFabricated;
        FarmPlot.OnAnyPlanted -= FarmPlot_OnAnyPlanted;
    }

    private void Inventory_OnItemChanged(object sender, Inventory.OnItemChangedEventArgs e)
    {
        if (e.delta > 0) AddProgress(TaskObjectiveSO.Type.CollectItem, e.itemDataSO, e.delta);
    }

    private void FabricatorController_OnAnySeedFabricated(object sender, EventArgs e) => AddProgress(TaskObjectiveSO.Type.FabricateSeed, null, 1);
    private void FarmPlot_OnAnyPlanted(object sender, EventArgs e) => AddProgress(TaskObjectiveSO.Type.PlantCrop, null, 1);

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

    // developer tools: sets an objective to done and announces it like a normal completion
    public void CompleteObjective(int index)
    {
        progressArray[index] = objectiveSOArray[index].requiredCount;
        OnObjectiveProgress?.Invoke(this, new OnObjectiveProgressEventArgs { index = index, isComplete = true });
        CheckRequiredComplete();
    }

    // developer tools: sets an objective back to not-done (collect objectives recount the bag)
    public void ResetObjective(int index)
    {
        TaskObjectiveSO objectiveSO = objectiveSOArray[index];
        progressArray[index] = objectiveSO.type == TaskObjectiveSO.Type.CollectItem
            ? Mathf.Min(Inventory.Instance.GetCount(objectiveSO.itemDataSO), objectiveSO.requiredCount)
            : 0;
        OnObjectiveProgress?.Invoke(this, new OnObjectiveProgressEventArgs { index = index, isComplete = IsComplete(index) });
        if (objectiveSO.type == TaskObjectiveSO.Type.EnterHouse) return;
        areRequiredComplete = false;
        CheckRequiredComplete();
    }

    // developer tools: restores a saved list and its progress silently (no completion events fire again)
    public void RestoreProgress(TaskObjectiveSO[] savedObjectiveSOArray, int[] savedProgressArray, bool savedAreRequiredComplete)
    {
        objectiveSOArray = savedObjectiveSOArray;
        progressArray = (int[])savedProgressArray.Clone();
        areRequiredComplete = savedAreRequiredComplete;
        OnObjectivesChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool AreRequiredComplete() => areRequiredComplete;

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
            if (type == TaskObjectiveSO.Type.CollectItem && objectiveSO.itemDataSO != itemDataSO) continue;
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
