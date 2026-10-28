using UnityEngine;

// One grid puzzle (the Sawit-Fabricator rules), written as text rows (top row first, all rows the same width):
// . = seed matrix floor, # = lignin wall, _ = no tile (acts as the board edge),
// B = Tech Bug, S = Seed Embryo, R = Root Sentinel, T = thorny Root Sentinel (all stand on floor),
// G / g = seed-coat gate open / closed, > ^ < v = sap router (its starting direction; clicking rotates it),
// I = injector gland (every Root Sentinel), A = area injector gland (Root Sentinels in the 3x3 around it).
[CreateAssetMenu()]
public class FabricatorPuzzleSO : ScriptableObject
{
    public string puzzleName;
    [TextArea(10, 14)] public string layout;
    [Tooltip("Slides an injector gland keeps the roots pulled back for.")]
    [Min(1)] public int retractTurns = 2;
    [Tooltip("Solving in this many slides or fewer is a combo (extra seed). 0 = no combo.")]
    [Min(0)] public int parMoves;

    // optional tutorial hint: a ghost cursor sweeps between two layout cells (x = column, y = row) until the first slide
    public bool hasHint;
    public Vector2Int hintFromCell;
    public Vector2Int hintToCell;

    public string[] GetRows() => layout.Replace("\r", "").Trim().Split('\n');
}
