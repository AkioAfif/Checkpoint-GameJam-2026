using UnityEngine;

// One Where's-My-Water layout, written as text rows (top row first, all rows the same width):
// R = rock (undiggable), # = dirt (dig it), . = empty, W/F/C = start pocket of liquid 0/1/2, M = mould (target).
[CreateAssetMenu()]
public class FabricatorPuzzleSO : ScriptableObject
{
    public string puzzleName;
    [TextArea(20, 24)] public string layout;
    // at most 4 per start-pocket cell
    public int particlesPerLiquid = 24;
    // share of each liquid that must reach the mould
    [Range(.1f, 1f)] public float requiredFraction = .5f;

    public string[] GetRows() => layout.Replace("\r", "").Trim().Split('\n');
}
