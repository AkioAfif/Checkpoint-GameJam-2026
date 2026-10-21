using UnityEngine;

[CreateAssetMenu()]
public class CropDataSO : ScriptableObject
{
    public string cropName;
    public ItemDataSO seedItemDataSO;
    public Transform saplingPrefab;
    // what the sapling looks like by the evening
    public Transform grownPrefab;
    // the next morning: the grown palm bearing fruit, harvested for harvestItemDataSO
    public Transform ripePrefab;
    public ItemDataSO harvestItemDataSO;
    [Header("Planting QTE (higher sawit levels: more checks, faster ring, smaller window)")]
    [Min(1)] public int qteCheckCount = 2;
    // seconds for the ring to shrink from its start size onto the target circle
    public float ringShrinkDuration = 1.1f;
    // allowed distance from the target circle, in target-radius units (0.2 = ±20 %)
    public float hitWindow = .2f;
    public float perfectWindow = .06f;
}
