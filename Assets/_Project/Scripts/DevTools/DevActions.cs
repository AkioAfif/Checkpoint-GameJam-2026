#if DEVELOPER_TOOLS || UNITY_EDITOR
using UnityEngine;

// Gameplay shortcuts. Each action checks its preconditions, then calls the same methods normal play uses.
public class DevActions
{
    private readonly DevToolsSettingsSO settings;

    public DevActions(DevToolsSettingsSO settings)
    {
        this.settings = settings;
    }

    // ----- Fabricator -----

    public bool IsFabricatorActive()
    {
        FabricatorController fabricator = UnityEngine.Object.FindAnyObjectByType<FabricatorController>();
        return fabricator != null && fabricator.IsRunActive();
    }

    public string GetFabricatorStatus()
    {
        if (UnityEngine.Object.FindAnyObjectByType<FabricatorController>() == null) return "No Fabricator in this scene";
        return IsFabricatorActive() ? "Fabricator run in progress" : "No Fabricator run in progress";
    }

    // finishes the run through the normal completion path: output once, inputs used once per batch
    public bool TrySkipFabricator(out string message)
    {
        FabricatorController fabricator = UnityEngine.Object.FindAnyObjectByType<FabricatorController>();
        if (fabricator == null)
        {
            message = "No Fabricator in this scene";
            return false;
        }
        if (!fabricator.IsRunActive())
        {
            message = "No Fabricator run is active";
            return false;
        }
        if (!fabricator.TrySkipRun())
        {
            message = "Not enough items for this run, so it was not skipped";
            return false;
        }
        message = "Fabricator run completed (output granted once)";
        return true;
    }

    // Reset Current Interaction: leaves the Fabricator without a reward, like Esc does
    public bool TryAbortFabricator(out string message)
    {
        FabricatorController fabricator = UnityEngine.Object.FindAnyObjectByType<FabricatorController>();
        if (fabricator == null || !fabricator.TryAbortRun())
        {
            message = "No Fabricator run to reset";
            return false;
        }
        message = "Fabricator run reset (no reward, no items used)";
        return true;
    }

    // ----- Objectives -----

    public bool HasObjectives() => TaskManager.Instance.GetObjectiveSOArray().Length > 0;

    public string GetObjectiveStatus()
    {
        int index = TaskManager.Instance.GetCurrentIndex();
        if (index < 0) return "All objectives done";
        return "Current: " + TaskManager.Instance.GetObjectiveSOArray()[index].description;
    }

    // completes the first open objective. Collect objectives get the missing items through the bag, so the bag and the list agree.
    public bool TryCompleteObjective(out string message)
    {
        TaskManager tasks = TaskManager.Instance;
        int index = tasks.GetCurrentIndex();
        if (index < 0)
        {
            message = "All objectives are already done";
            return false;
        }
        TaskObjectiveSO objectiveSO = tasks.GetObjectiveSOArray()[index];
        if (objectiveSO.type == TaskObjectiveSO.Type.CollectItem)
        {
            int missing = objectiveSO.requiredCount - Inventory.Instance.GetCount(objectiveSO.itemDataSO);
            if (missing > 0) Inventory.Instance.AddItem(objectiveSO.itemDataSO, missing);
        }
        tasks.CompleteObjective(index);
        message = "Objective completed: " + objectiveSO.description;
        return true;
    }

    // resets the current objective (or the last one when everything is done) to not-done
    public bool TryResetObjective(out string message)
    {
        TaskManager tasks = TaskManager.Instance;
        TaskObjectiveSO[] objectiveSOArray = tasks.GetObjectiveSOArray();
        if (objectiveSOArray.Length == 0)
        {
            message = "No objectives in this scene";
            return false;
        }
        int index = tasks.GetCurrentIndex();
        if (index < 0) index = objectiveSOArray.Length - 1;
        tasks.ResetObjective(index);
        message = "Objective reset: " + objectiveSOArray[index].description;
        return true;
    }

    // ----- Player -----

    public bool TryResetPlayer(bool hasSpawnPose, Vector3 spawnPosition, Quaternion spawnRotation, out string message)
    {
        if (!hasSpawnPose)
        {
            message = "No spawn point yet: wait until free play starts";
            return false;
        }
        if (GameManager.Instance.GetState() != GameManager.State.Playing)
        {
            message = "Reset Player works during free play only";
            return false;
        }
        Player.Instance.Teleport(spawnPosition);
        Player.Instance.transform.rotation = spawnRotation;
        message = "Player moved back to the scene spawn";
        return true;
    }

    // ----- Inventory -----

    public string AddTestItem(ItemDataSO itemDataSO)
    {
        Inventory.Instance.AddItem(itemDataSO, 1);
        return "Added 1 " + itemDataSO.itemName;
    }

    // sets the bag to the configured baseline; collect objectives recount the bag so they don't keep stale progress
    public string ResetTestInventory()
    {
        Inventory.Instance.RestoreCounts(settings.testInventoryArray);
        TaskObjectiveSO[] objectiveSOArray = TaskManager.Instance.GetObjectiveSOArray();
        for (int i = 0; i < objectiveSOArray.Length; i++)
        {
            if (objectiveSOArray[i].type == TaskObjectiveSO.Type.CollectItem) TaskManager.Instance.ResetObjective(i);
        }
        return "Test inventory reset to the baseline";
    }
}
#endif
