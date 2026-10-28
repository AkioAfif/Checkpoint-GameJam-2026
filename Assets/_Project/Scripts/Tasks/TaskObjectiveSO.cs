using UnityEngine;

[CreateAssetMenu()]
public class TaskObjectiveSO : ScriptableObject
{
    public enum Type { CollectItem, FabricateSeed, PlantCrop, EnterHouse, Story }

    public Type type;
    public string description;
    // CollectItem: the item to collect. FabricateSeed: only seeds of this type count (empty = any seed)
    public ItemDataSO itemDataSO;
    [Min(1)] public int requiredCount = 1;
}
