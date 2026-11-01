using UnityEngine;

[CreateAssetMenu()]
public class TaskObjectiveSO : ScriptableObject
{
    // HarvestFruit = a ripe palm harvested; SellItem = items sold at the ICAN crate
    public enum Type { CollectItem, FabricateSeed, PlantCrop, EnterHouse, Story, HarvestFruit, SellItem }

    public Type type;
    public string description;
    // CollectItem: the item to collect. FabricateSeed / PlantCrop: only that seed counts; HarvestFruit / SellItem: only that
    // item counts (empty = any)
    public ItemDataSO itemDataSO;
    [Min(1)] public int requiredCount = 1;
}
