#if DEVELOPER_TOOLS || UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

// Quick save / quick load for the current scene. Kept in memory only, so it never touches the player's save and it is
// gone when the scene unloads. Restores the data layer: player pose, bag, tasks, world state, farm plots, resource nodes,
// fabricator runs and dialogue rewards. Story beats inside the Day directors are NOT rolled back.
public class DevQuickSave
{
    public enum LoadResult { Loaded, NeedsConfirm, Failed }

    private const float CONFIRM_WINDOW = 3f;

    private class Snapshot
    {
        public DateTime savedAt;
        public Vector3 playerPosition;
        public Quaternion playerRotation;
        public ItemAmount[] countArray;
        public TaskObjectiveSO[] objectiveSOArray;
        public int[] progressArray;
        public bool areRequiredComplete;
        public WorldState.Snapshot worldState;
        public List<FarmPlotSave> farmPlotList;
        public List<ResourceNodeSave> resourceNodeList;
        public FabricatorController fabricator;
        public int fabricatorRuns;
        public HashSet<DialogueSO> rewardedSet;
    }

    private struct FarmPlotSave
    {
        public FarmPlot farmPlot;
        public FarmPlot.State state;
        public CropDataSO planted;
        public CropDataSO plantable;
    }

    private struct ResourceNodeSave
    {
        public ResourceNode resourceNode;
        public bool isAvailable;
        public float respawnTimer;
    }

    private Snapshot snapshot;
    // progress since the quick save (collected items or completed objectives)
    private bool isDirty;
    private float confirmTimer;

    public DevQuickSave()
    {
        Inventory.Instance.OnItemChanged += Inventory_OnItemChanged;
        TaskManager.Instance.OnObjectiveProgress += TaskManager_OnObjectiveProgress;
    }

    public void Dispose()
    {
        if (Inventory.Instance != null) Inventory.Instance.OnItemChanged -= Inventory_OnItemChanged;
        if (TaskManager.Instance != null) TaskManager.Instance.OnObjectiveProgress -= TaskManager_OnObjectiveProgress;
    }

    public bool HasSave => snapshot != null;
    public bool IsConfirming() => confirmTimer > 0f;

    public void Tick(float unscaledDeltaTime)
    {
        if (confirmTimer > 0f) confirmTimer -= unscaledDeltaTime;
    }

    public string GetStatus()
    {
        if (snapshot == null) return "No quick save yet in this scene";
        return (isDirty ? "Saved " : "Saved, unchanged since ") + snapshot.savedAt.ToString("HH:mm:ss");
    }

    public bool TrySave(out string message)
    {
        if (!CanCapture(out message)) return false;
        snapshot = Capture();
        isDirty = false;
        confirmTimer = 0f;
        message = "Quick saved at " + snapshot.savedAt.ToString("HH:mm:ss") + " (dev slot, not your save file)";
        return true;
    }

    public LoadResult TryLoad(out string message)
    {
        if (snapshot == null)
        {
            message = "No quick save in this scene. Press Quick Save first";
            return LoadResult.Failed;
        }
        if (GameManager.Instance.GetState() != GameManager.State.Playing || GameManager.Instance.IsGamePaused())
        {
            message = "Quick Load works during free play only";
            return LoadResult.Failed;
        }
        if (IsFabricatorBusy())
        {
            message = "Leave the Fabricator before loading";
            return LoadResult.Failed;
        }
        if (isDirty && confirmTimer <= 0f)
        {
            confirmTimer = CONFIRM_WINDOW;
            message = "Progress since the quick save will be lost. Press Quick Load again to confirm";
            return LoadResult.NeedsConfirm;
        }
        confirmTimer = 0f;
        Restore(snapshot);
        message = "Quick loaded (" + snapshot.savedAt.ToString("HH:mm:ss") + ")";
        return LoadResult.Loaded;
    }

    private void Inventory_OnItemChanged(object sender, Inventory.OnItemChangedEventArgs e)
    {
        if (e.delta != 0) isDirty = true;
    }

    private void TaskManager_OnObjectiveProgress(object sender, TaskManager.OnObjectiveProgressEventArgs e) => isDirty = true;

    private bool CanCapture(out string message)
    {
        message = "";
        if (GameManager.Instance.GetState() != GameManager.State.Playing || GameManager.Instance.IsGamePaused())
        {
            message = "Quick Save works during free play only";
            return false;
        }
        if (IsFabricatorBusy())
        {
            message = "Leave the Fabricator before saving";
            return false;
        }
        if (PlantingController.Instance != null && PlantingController.Instance.IsPlanting())
        {
            message = "Finish planting before saving";
            return false;
        }
        return true;
    }

    private static bool IsFabricatorBusy()
    {
        FabricatorController fabricator = UnityEngine.Object.FindAnyObjectByType<FabricatorController>();
        return fabricator != null && fabricator.IsRunActive();
    }

    private static Snapshot Capture()
    {
        Snapshot saved = new Snapshot { savedAt = DateTime.Now };
        saved.playerPosition = Player.Instance.transform.position;
        saved.playerRotation = Player.Instance.transform.rotation;
        saved.countArray = Inventory.Instance.GetCountArray();

        TaskManager tasks = TaskManager.Instance;
        saved.objectiveSOArray = tasks.GetObjectiveSOArray();
        saved.progressArray = new int[saved.objectiveSOArray.Length];
        for (int i = 0; i < saved.progressArray.Length; i++) saved.progressArray[i] = tasks.GetProgress(i);
        saved.areRequiredComplete = tasks.AreRequiredComplete();

        saved.worldState = WorldState.Capture();

        saved.farmPlotList = new List<FarmPlotSave>();
        foreach (FarmPlot farmPlot in UnityEngine.Object.FindObjectsByType<FarmPlot>())
        {
            saved.farmPlotList.Add(new FarmPlotSave
            {
                farmPlot = farmPlot,
                state = farmPlot.GetState(),
                planted = farmPlot.GetPlantedCropDataSO(),
                plantable = farmPlot.GetPlantableCropDataSO(),
            });
        }

        saved.resourceNodeList = new List<ResourceNodeSave>();
        foreach (ResourceNode resourceNode in UnityEngine.Object.FindObjectsByType<ResourceNode>())
        {
            saved.resourceNodeList.Add(new ResourceNodeSave
            {
                resourceNode = resourceNode,
                isAvailable = resourceNode.IsAvailable(),
                respawnTimer = resourceNode.GetRespawnTimer(),
            });
        }

        saved.fabricator = UnityEngine.Object.FindAnyObjectByType<FabricatorController>();
        saved.fabricatorRuns = saved.fabricator != null ? saved.fabricator.GetCompletedRuns() : 0;
        saved.rewardedSet = DialogueManager.Instance != null ? DialogueManager.Instance.GetRewardedSet() : new HashSet<DialogueSO>();
        return saved;
    }

    // every reference below is an object of this scene session; a destroyed one is skipped (the scene reloaded)
    private void Restore(Snapshot saved)
    {
        Player.Instance.Teleport(saved.playerPosition);
        Player.Instance.transform.rotation = saved.playerRotation;

        WorldState.Restore(saved.worldState);
        Inventory.Instance.RestoreCounts(saved.countArray);
        TaskManager.Instance.RestoreProgress(saved.objectiveSOArray, saved.progressArray, saved.areRequiredComplete);

        foreach (FarmPlotSave farmPlotSave in saved.farmPlotList)
        {
            if (farmPlotSave.farmPlot != null) farmPlotSave.farmPlot.RestoreState(farmPlotSave.state, farmPlotSave.planted, farmPlotSave.plantable);
        }
        foreach (ResourceNodeSave resourceNodeSave in saved.resourceNodeList)
        {
            if (resourceNodeSave.resourceNode != null) resourceNodeSave.resourceNode.RestoreAvailability(resourceNodeSave.isAvailable, resourceNodeSave.respawnTimer);
        }
        if (saved.fabricator != null) saved.fabricator.RestoreCompletedRuns(saved.fabricatorRuns);
        if (DialogueManager.Instance != null) DialogueManager.Instance.RestoreRewardedSet(saved.rewardedSet);

        isDirty = false;
    }
}
#endif
