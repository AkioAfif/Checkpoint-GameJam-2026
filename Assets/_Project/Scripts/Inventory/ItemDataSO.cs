using UnityEngine;

[CreateAssetMenu()]
public class ItemDataSO : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    // visual-only model (no colliders), used for the float-up effect and icon baking
    public Transform prefab;
    // colour of this item's liquid inside the Fabricator
    public Color liquidColor = Color.white;
}
