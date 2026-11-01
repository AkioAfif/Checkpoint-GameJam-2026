using System.Collections.Generic;
using UnityEngine;

// A piece on the Fabricator board: the Tech Bug the player slides, a Seed Embryo to corrupt, a Root Sentinel that grows
// bramble roots onto nearby cells, an Energy Cell (a recharge to capture) or a Moving Cell that steps after every slide.
// Plain data, owned and changed by FabricatorBoard.
public class FabricatorBoardUnit
{
    public enum Type { Bug, Embryo, Sentinel, EnergyCell, MovingCell }

    public readonly Type type;
    // Root Sentinel: thorny roots. Moving Cell: hostile (Seed Burr), it kills the bug and can't be captured.
    public readonly bool isDangerous;
    public readonly int energyOnCapture;
    // board cells this Root Sentinel's roots cover (only cells that exist on the board)
    public readonly List<Vector2Int> rootCellList = new List<Vector2Int>();
    public readonly Vector2Int[] moveSequenceArray;
    public Vector2Int cell;
    // where it stood before its last Moving Cell step (visuals glide from here)
    public Vector2Int previousCell;
    public bool isAlive = true;
    // roots are pulled back while this is above 0 (counted in slides)
    public int retractTurnsLeft;
    private int stepIndex;

    public FabricatorBoardUnit(Type type, Vector2Int cell, bool isDangerous, int energyOnCapture, Vector2Int[] moveSequenceArray)
    {
        this.type = type;
        this.cell = cell;
        previousCell = cell;
        this.isDangerous = isDangerous;
        this.energyOnCapture = energyOnCapture;
        this.moveSequenceArray = moveSequenceArray != null ? moveSequenceArray : new Vector2Int[0];
    }

    public bool AreRootsOut() => type == Type.Sentinel && isAlive && retractTurnsLeft == 0;
    // a slide stops on (and captures) anything but another bug or a hostile Moving Cell
    public bool IsCapturable() => type != Type.Bug && !IsHostileMovingCell();
    public bool IsHostileMovingCell() => type == Type.MovingCell && isDangerous;

    // the step this Moving Cell takes next, one cell at most per axis
    public Vector2Int GetNextStep()
    {
        if (moveSequenceArray.Length == 0) return Vector2Int.zero;
        Vector2Int step = moveSequenceArray[stepIndex % moveSequenceArray.Length];
        return new Vector2Int(Mathf.Clamp(step.x, -1, 1), Mathf.Clamp(step.y, -1, 1));
    }

    public void AdvanceSequence()
    {
        if (moveSequenceArray.Length > 0) stepIndex = (stepIndex + 1) % moveSequenceArray.Length;
    }
}
