using System.Collections.Generic;
using UnityEngine;

// A piece on the Fabricator board: the Tech Bug the player slides, a Seed Embryo to corrupt,
// or a Root Sentinel that grows bramble roots onto its neighbours. Plain data, owned and changed by FabricatorBoard.
public class FabricatorBoardUnit
{
    public enum Type { Bug, Embryo, Sentinel }

    public readonly Type type;
    public readonly bool isThorny;
    // board cells this Root Sentinel's roots cover (only cells that exist on the board)
    public readonly List<Vector2Int> rootCellList = new List<Vector2Int>();
    public Vector2Int cell;
    public bool isAlive = true;
    // roots are pulled back while this is above 0 (counted in slides)
    public int retractTurnsLeft;

    public FabricatorBoardUnit(Type type, Vector2Int cell, bool isThorny)
    {
        this.type = type;
        this.cell = cell;
        this.isThorny = isThorny;
    }

    public bool AreRootsOut() => type == Type.Sentinel && isAlive && retractTurnsLeft == 0;
    // the bug stops on (and captures) anything that isn't another bug
    public bool IsCapturable() => type != Type.Bug;
}
