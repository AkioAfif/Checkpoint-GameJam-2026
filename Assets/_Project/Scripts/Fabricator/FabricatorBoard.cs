using System.Collections.Generic;
using UnityEngine;

// The Fabricator puzzle's rules (ported from Sawit-Fabricator), with no Unity objects: the Tech Bug slides until something
// stops it, corrupting every Seed Embryo wins, Root Sentinels' roots block tiles (thorny roots destroy the bug),
// and gates, sap routers and injector glands change the board when clicked. Cells use FabricatorPuzzleSO layout coordinates
// centred on the board (x right, y up), so they match the Grid's cells.
public class FabricatorBoard
{
    public static readonly Vector2Int[] MoveDirectionArray =
    {
        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(1, -1),
        new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1),
    };
    private static readonly Vector2Int[] RootOffsetArray = { Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left };

    private readonly Dictionary<Vector2Int, FabricatorBoardTile> tileDictionary = new Dictionary<Vector2Int, FabricatorBoardTile>();
    private readonly List<FabricatorBoardUnit> unitList = new List<FabricatorBoardUnit>();
    private readonly int retractTurns;

    public FabricatorBoard(FabricatorPuzzleSO puzzleSO)
    {
        retractTurns = puzzleSO.retractTurns;
        string[] rows = puzzleSO.GetRows();
        for (int r = 0; r < rows.Length; r++)
        {
            for (int c = 0; c < rows[r].Length; c++)
            {
                char symbol = rows[r][c];
                if (symbol == '_') continue;
                Vector2Int cell = LayoutToCell(new Vector2Int(c, r), rows);
                FabricatorBoardTile tile = new FabricatorBoardTile(GetTileType(symbol), cell);
                tile.isGateOpen = symbol == 'G';
                tile.routerAngle = GetRouterAngle(symbol);
                tileDictionary[cell] = tile;
                if (TryGetUnitType(symbol, out FabricatorBoardUnit.Type unitType)) unitList.Add(new FabricatorBoardUnit(unitType, cell, symbol == 'T'));
            }
        }
        foreach (FabricatorBoardUnit unit in unitList)
        {
            if (unit.type != FabricatorBoardUnit.Type.Sentinel) continue;
            foreach (Vector2Int offset in RootOffsetArray)
                if (tileDictionary.ContainsKey(unit.cell + offset)) unit.rootCellList.Add(unit.cell + offset);
        }
    }

    // layout column/row (top row first) → board cell, centred on the board
    public static Vector2Int LayoutToCell(Vector2Int layoutCell, string[] rows) => new Vector2Int(layoutCell.x - rows[layoutCell.y].Length / 2, rows.Length / 2 - layoutCell.y);

    public IEnumerable<FabricatorBoardTile> GetTiles() => tileDictionary.Values;
    public List<FabricatorBoardUnit> GetUnitList() => unitList;
    public bool TryGetTile(Vector2Int cell, out FabricatorBoardTile tile) => tileDictionary.TryGetValue(cell, out tile);

    public bool TryGetUnit(Vector2Int cell, out FabricatorBoardUnit unit)
    {
        foreach (FabricatorBoardUnit candidate in unitList)
        {
            if (!candidate.isAlive || candidate.cell != cell) continue;
            unit = candidate;
            return true;
        }
        unit = null;
        return false;
    }

    public int GetEmbryoCount()
    {
        int count = 0;
        foreach (FabricatorBoardUnit unit in unitList)
            if (unit.isAlive && unit.type == FabricatorBoardUnit.Type.Embryo) count++;
        return count;
    }

    public bool HasLivingBug()
    {
        foreach (FabricatorBoardUnit unit in unitList)
            if (unit.isAlive && unit.type == FabricatorBoardUnit.Type.Bug) return true;
        return false;
    }

    // roots that are out over this cell block it; isHazardous when any of them is thorny
    public bool IsRootBlocked(Vector2Int cell, out bool isHazardous)
    {
        bool isBlocked = false;
        isHazardous = false;
        foreach (FabricatorBoardUnit unit in unitList)
        {
            if (!unit.AreRootsOut() || !unit.rootCellList.Contains(cell)) continue;
            isBlocked = true;
            isHazardous |= unit.isThorny;
        }
        return isBlocked;
    }

    // Every cell the slide passes through, in order; the last one is where the bug ends up. Empty when it can't move that way.
    //  - board edge, wall, closed gate, root-blocked cell or another bug: stop before it
    //  - thorny roots over walkable terrain: enter the cell and be destroyed (endsInHazard)
    //  - Seed Embryo or Root Sentinel: stop on its cell and capture it
    //  - sap router: enter it, then carry on in the router's direction
    public List<Vector2Int> GetSlidePath(Vector2Int start, Vector2Int direction, out bool endsInHazard)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        endsInHazard = false;
        Vector2Int current = start;
        // routers can point at each other: stop if the slide would enter the same router heading the same way twice
        HashSet<(Vector2Int, Vector2Int)> visitedRouterSet = new HashSet<(Vector2Int, Vector2Int)>();
        while (true)
        {
            Vector2Int next = current + direction;
            if (!tileDictionary.TryGetValue(next, out FabricatorBoardTile tile)) break;
            if (TryGetUnit(next, out FabricatorBoardUnit unit))
            {
                if (unit.IsCapturable()) path.Add(next);
                break;
            }
            if (IsRootBlocked(next, out bool isHazardous))
            {
                // thorny roots over a wall still just act as a wall
                if (isHazardous && tile.IsWalkableTerrain())
                {
                    path.Add(next);
                    endsInHazard = true;
                }
                break;
            }
            if (!tile.IsWalkableTerrain()) break;
            path.Add(next);
            current = next;
            if (tile.type != FabricatorBoardTile.Type.Router) continue;
            direction = tile.GetRouterDirection();
            if (!visitedRouterSet.Add((next, direction))) break;
        }
        return path;
    }

    // moves the bug to the end of the path: it dies in thorns, or captures what stands there
    public void ApplySlide(FabricatorBoardUnit bug, List<Vector2Int> path, bool endsInHazard)
    {
        Vector2Int destination = path[path.Count - 1];
        if (endsInHazard) bug.isAlive = false;
        else if (TryGetUnit(destination, out FabricatorBoardUnit captured)) captured.isAlive = false;
        bug.cell = destination;
    }

    // one finished slide: timed root retractions and the glands' own timers count down
    public void PassTurn()
    {
        foreach (FabricatorBoardUnit unit in unitList)
            if (unit.retractTurnsLeft > 0) unit.retractTurnsLeft--;
        foreach (FabricatorBoardTile tile in tileDictionary.Values)
        {
            if (tile.injectorTurnsLeft == 0) continue;
            tile.injectorTurnsLeft--;
            if (tile.injectorTurnsLeft == 0) tile.injectorTargetList.Clear();
        }
    }

    public bool CanInteract(FabricatorBoardTile tile)
    {
        switch (tile.type)
        {
            // a gate can't close on a unit sitting in it
            case FabricatorBoardTile.Type.Gate: return !tile.isGateOpen || !TryGetUnit(tile.cell, out _);
            case FabricatorBoardTile.Type.Router: return true;
            case FabricatorBoardTile.Type.Injector:
            case FabricatorBoardTile.Type.AreaInjector: return GetInjectorTargetList(tile).Count > 0;
            default: return false;
        }
    }

    public void Interact(FabricatorBoardTile tile)
    {
        switch (tile.type)
        {
            case FabricatorBoardTile.Type.Gate:
                tile.isGateOpen = !tile.isGateOpen;
                break;
            case FabricatorBoardTile.Type.Router:
                tile.routerAngle = (tile.routerAngle + 90) % 360;
                break;
            case FabricatorBoardTile.Type.Injector:
            case FabricatorBoardTile.Type.AreaInjector:
                // clicking again restarts the timer
                foreach (FabricatorBoardUnit target in GetInjectorTargetList(tile))
                {
                    target.retractTurnsLeft = Mathf.Max(target.retractTurnsLeft, retractTurns);
                    if (!tile.injectorTargetList.Contains(target)) tile.injectorTargetList.Add(target);
                }
                tile.injectorTurnsLeft = retractTurns;
                break;
        }
    }

    // Lose: embryos remain, no bug has a safe slide anywhere, and nothing on the board can be clicked (a click might open a path)
    public bool IsStuck()
    {
        if (GetEmbryoCount() == 0) return false;
        foreach (FabricatorBoardUnit unit in unitList)
        {
            if (!unit.isAlive || unit.type != FabricatorBoardUnit.Type.Bug) continue;
            foreach (Vector2Int direction in MoveDirectionArray)
            {
                List<Vector2Int> path = GetSlidePath(unit.cell, direction, out bool endsInHazard);
                if (path.Count > 0 && !endsInHazard) return false;
            }
        }
        foreach (FabricatorBoardTile tile in tileDictionary.Values)
            if (CanInteract(tile)) return false;
        return true;
    }

    // Root Sentinels a gland reaches: those in the 3x3 around an area gland, or all of them
    private List<FabricatorBoardUnit> GetInjectorTargetList(FabricatorBoardTile tile)
    {
        List<FabricatorBoardUnit> targetList = new List<FabricatorBoardUnit>();
        foreach (FabricatorBoardUnit unit in unitList)
        {
            if (!unit.isAlive || unit.type != FabricatorBoardUnit.Type.Sentinel) continue;
            Vector2Int offset = unit.cell - tile.cell;
            if (tile.type == FabricatorBoardTile.Type.AreaInjector && Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) > 1) continue;
            targetList.Add(unit);
        }
        return targetList;
    }

    private static FabricatorBoardTile.Type GetTileType(char symbol)
    {
        switch (symbol)
        {
            case '#': return FabricatorBoardTile.Type.Wall;
            case 'G':
            case 'g': return FabricatorBoardTile.Type.Gate;
            case '>':
            case '^':
            case '<':
            case 'v': return FabricatorBoardTile.Type.Router;
            case 'I': return FabricatorBoardTile.Type.Injector;
            case 'A': return FabricatorBoardTile.Type.AreaInjector;
            default: return FabricatorBoardTile.Type.Floor;
        }
    }

    private static int GetRouterAngle(char symbol)
    {
        switch (symbol)
        {
            case '^': return 90;
            case '<': return 180;
            case 'v': return 270;
            default: return 0;
        }
    }

    private static bool TryGetUnitType(char symbol, out FabricatorBoardUnit.Type unitType)
    {
        switch (symbol)
        {
            case 'B': unitType = FabricatorBoardUnit.Type.Bug; return true;
            case 'S': unitType = FabricatorBoardUnit.Type.Embryo; return true;
            case 'R':
            case 'T': unitType = FabricatorBoardUnit.Type.Sentinel; return true;
            default: unitType = default; return false;
        }
    }
}
