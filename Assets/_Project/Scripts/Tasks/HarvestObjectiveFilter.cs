using System.Collections.Generic;
using UnityEngine;

// A morning's "harvest, then sell" checklist without the objectives the garden and the bag can't serve today, so a day
// can't get stuck when yesterday's palms were all picked already. Shared by the yard day and Day 5.
public static class HarvestObjectiveFilter
{
    public static TaskObjectiveSO[] Filter(TaskObjectiveSO[] harvestObjectiveArray)
    {
        FarmPlot[] farmPlotArray = Object.FindObjectsByType<FarmPlot>();
        List<TaskObjectiveSO> objectiveSOList = new List<TaskObjectiveSO>();
        foreach (TaskObjectiveSO objectiveSO in harvestObjectiveArray)
        {
            if (objectiveSO.type == TaskObjectiveSO.Type.HarvestFruit && !HasRipeFruit(farmPlotArray, objectiveSO.itemDataSO)) continue;
            if (objectiveSO.type == TaskObjectiveSO.Type.SellItem && !HasRipeFruit(farmPlotArray, objectiveSO.itemDataSO) && !CanSellFromBag(objectiveSO.itemDataSO)) continue;
            objectiveSOList.Add(objectiveSO);
        }
        return objectiveSOList.ToArray();
    }

    // the bag already holds something to sell (null = anything the crate buys)
    private static bool CanSellFromBag(ItemDataSO itemDataSO)
    {
        if (itemDataSO != null) return Inventory.Instance.GetCount(itemDataSO) > 0;
        SellCrate crate = Object.FindAnyObjectByType<SellCrate>();
        return crate != null && crate.CanInteract(Player.Instance);
    }

    // a ripe palm bearing this fruit (null = any fruit)
    private static bool HasRipeFruit(FarmPlot[] farmPlotArray, ItemDataSO fruitItemDataSO)
    {
        foreach (FarmPlot farmPlot in farmPlotArray)
        {
            if (farmPlot.GetState() != FarmPlot.State.Ripe) continue;
            if (fruitItemDataSO == null || farmPlot.GetPlantedCropDataSO().harvestItemDataSO == fruitItemDataSO) return true;
        }
        return false;
    }
}
