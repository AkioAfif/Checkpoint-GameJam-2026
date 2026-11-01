using System;
using System.Collections.Generic;
using UnityEngine;

// One Fabricator level (the Sawit-Fabricator rules). The tiles are text rows (top row first, all rows the same width):
// . = seed matrix floor, # = lignin wall, _ = no tile (acts as the board edge), X = resin snare (a slide stops on it),
// G / g = seed-coat gate open / closed, > ^ < v = sap router (its starting direction; clicking rotates it),
// I = injector gland (every Root Sentinel), A = area injector gland (Root Sentinels in the 3x3 around it).
// Units stand on those tiles and are listed separately.
[CreateAssetMenu()]
public class FabricatorPuzzleSO : ScriptableObject
{
    [Serializable]
    public class UnitPlacement
    {
        public FabricatorBoardUnit.Type type;
        [Tooltip("Layout column (x) and row (y, top row = 0).")]
        public Vector2Int layoutCell;
        [Tooltip("Root Sentinel: thorny roots. Moving Cell: hostile (a Seed Burr instead of a Pollen Drifter).")]
        public bool isDangerous;
        [Tooltip("Energy the Tech Bug gains for capturing this unit.")]
        [Min(0)] public int energyOnCapture;
        [Tooltip("Root Sentinel: the cells its roots cover, relative to it (y up). Empty = its 4 neighbours.")]
        public Vector2Int[] rootOffsetArray;
        [Tooltip("Moving Cell: one step per Tech Bug slide, repeated in order (y up; (0,0) waits).")]
        public Vector2Int[] moveSequenceArray;
    }

    public string puzzleName;
    [TextArea(10, 16)] public string layout;
    public List<UnitPlacement> unitPlacementList = new List<UnitPlacement>();
    [Tooltip("Energy the Tech Bug starts with; each slide costs 1. 0 = unlimited.")]
    [Min(0)] public int initialEnergy;
    [Tooltip("Recharges from captures can't go above this.")]
    [Min(1)] public int maxEnergy = 10;
    [Tooltip("Slides an injector gland keeps the roots pulled back for.")]
    [Min(1)] public int retractTurns = 2;
    [Tooltip("Solving in this many slides or fewer is a combo (extra seed). 0 = no combo.")]
    [Min(0)] public int parMoves;

    public string[] GetRows() => layout.Replace("\r", "").Trim().Split('\n');
}
