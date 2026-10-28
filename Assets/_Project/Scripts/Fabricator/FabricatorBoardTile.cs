using System.Collections.Generic;
using UnityEngine;

// One cell of the Fabricator board. Plain data, owned and changed by FabricatorBoard.
public class FabricatorBoardTile
{
    public enum Type { Floor, Wall, Gate, Router, Injector, AreaInjector }

    public readonly Type type;
    public readonly Vector2Int cell;
    public bool isGateOpen;
    // 0 = right, 90 = up, 180 = left, 270 = down
    public int routerAngle;
    // injector glands: slides left on their own retraction, and the Root Sentinels they pulled back
    public int injectorTurnsLeft;
    public readonly List<FabricatorBoardUnit> injectorTargetList = new List<FabricatorBoardUnit>();

    public FabricatorBoardTile(Type type, Vector2Int cell)
    {
        this.type = type;
        this.cell = cell;
    }

    public bool IsInjector() => type == Type.Injector || type == Type.AreaInjector;

    // terrain only: ignores units and roots (roots over a wall still act as a wall)
    public bool IsWalkableTerrain() => type != Type.Wall && (type != Type.Gate || isGateOpen);

    public Vector2Int GetRouterDirection()
    {
        float radians = routerAngle * Mathf.Deg2Rad;
        return new Vector2Int(Mathf.RoundToInt(Mathf.Cos(radians)), Mathf.RoundToInt(Mathf.Sin(radians)));
    }

    // the gland's effect runs until its timer ends or every Root Sentinel it pulled back is captured
    public bool IsInjectorActive()
    {
        if (injectorTurnsLeft == 0) return false;
        foreach (FabricatorBoardUnit target in injectorTargetList)
            if (target.isAlive) return true;
        return false;
    }
}
