using UnityEngine;

[CreateAssetMenu()]
public class TaskObjectiveSO : ScriptableObject
{
    public enum Type { CollectItem, FabricateSeed, PlantCrop, EnterHouse, Story }

    public Type type;
    public string description;
    // only for CollectItem
    public ItemDataSO itemDataSO;
    [Min(1)] public int requiredCount = 1;
}
