using System.Collections.Generic;
using UnityEngine;

// The Fabricator puzzle's rules (ported from Sawit-Fabricator), with no Unity objects: the Tech Bug slides until something
// stops it, each slide costs 1 energy, corrupting every Seed Embryo wins, Root Sentinels' roots block cells (thorny roots
// destroy the bug), Moving Cells step after every slide (hostile ones destroy the bug), resin snares stop a slide, and gates,
// sap routers and injector glands change the board when clicked. Cells use FabricatorPuzzleSO layout coordinates centred on
// the board (x right, y up), so they match the Grid's cells.
public class FabricatorBoard
{
    public static readonly Vector2Int[] MoveDirectionArray =
    {
        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(1, -1),
        new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1),
    };
    private static readonly Vector2Int[] DefaultRootOffsetArray = { Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left };

    private readonly Dictionary<Vector2Int, FabricatorBoardTile> tileDictionary = new Dictionary<Vector2Int, FabricatorBoardTile>();
    // in placement order, which is also the order Moving Cells step in within their group
    private readonly List<FabricatorBoardUnit> unitList = new List<FabricatorBoardUnit>();
    private readonly int retractTurns;
    // a level with no embryos can't be won, only lost
    private readonly bool hasEmbryos;
    private readonly bool isEnergyLimited;
    private readonly int maxEnergy;
    private int energy;

    // energyOverride > 0 replaces the level's own starting energy (and raises the cap to it)
    public FabricatorBoard(FabricatorPuzzleSO puzzleSO, int energyOverride)
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
            }
        }
        foreach (FabricatorPuzzleSO.UnitPlacement placement in puzzleSO.unitPlacementList)
        {
            FabricatorBoardUnit unit = new FabricatorBoardUnit(placement.type, LayoutToCell(placement.layoutCell, rows), placement.isDangerous, placement.energyOnCapture, placement.moveSequenceArray);
            if (unit.type == FabricatorBoardUnit.Type.Sentinel)
            {
                bool hasOffsets = placement.rootOffsetArray != null && placement.rootOffsetArray.Length > 0;
                foreach (Vector2Int offset in hasOffsets ? placement.rootOffsetArray : DefaultRootOffsetArray)
                    if (offset != Vector2Int.zero && tileDictionary.ContainsKey(unit.cell + offset)) unit.rootCellList.Add(unit.cell + offset);
            }
            unitList.Add(unit);
        }
        hasEmbryos = GetEmbryoCount() > 0;

        int initialEnergy = energyOverride > 0 ? energyOverride : puzzleSO.initialEnergy;
        isEnergyLimited = initialEnergy > 0;
        maxEnergy = Mathf.Max(puzzleSO.maxEnergy, initialEnergy);
        energy = initialEnergy;
    }

    // layout column/row (top row first) → board cell, centred on the board
    public static Vector2Int LayoutToCell(Vector2Int layoutCell, string[] rows) => new Vector2Int(layoutCell.x - rows[layoutCell.y].Length / 2, rows.Length / 2 - layoutCell.y);

    public IEnumerable<FabricatorBoardTile> GetTiles() => tileDictionary.Values;
    public List<FabricatorBoardUnit> GetUnitList() => unitList;
    public bool TryGetTile(Vector2Int cell, out FabricatorBoardTile tile) => tileDictionary.TryGetValue(cell, out tile);
    public bool IsEnergyLimited() => isEnergyLimited;
    public int GetEnergy() => energy;
    public int GetMaxEnergy() => maxEnergy;
    public bool IsWon() => hasEmbryos && GetEmbryoCount() == 0;
    public bool IsOutOfEnergy() => isEnergyLimited && energy <= 0;

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

    public int GetEmbryoCount() => CountLiving(FabricatorBoardUnit.Type.Embryo);
    public bool HasLivingBug() => CountLiving(FabricatorBoardUnit.Type.Bug) > 0;

    // roots that are out over this cell block it; isHazardous when any of them is thorny
    public bool IsRootBlocked(Vector2Int cell, out bool isHazardous)
    {
        bool isBlocked = false;
        isHazardous = false;
        foreach (FabricatorBoardUnit unit in unitList)
        {
            if (!unit.AreRootsOut() || !unit.rootCellList.Contains(cell)) continue;
            isBlocked = true;
            isHazardous |= unit.isDangerous;
        }
        return isBlocked;
    }

    // Every cell the slide passes through, in order; the last one is where the bug ends up. Empty when it can't move that way.
    //  - board edge, wall, closed gate, root-blocked cell or another bug: stop before it. Roots come first, so a unit
    //    under roots can't be captured through them
    //  - thorny roots over walkable terrain, or a hostile Moving Cell: enter the cell and be destroyed (endsInHazard)
    //  - Seed Embryo, Root Sentinel, Energy Cell or passive Moving Cell: stop on its cell and capture it
    //  - resin snare: enter it and stop there
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
            if (TryGetUnit(next, out FabricatorBoardUnit unit))
            {
                if (unit.IsHostileMovingCell())
                {
                    path.Add(next);
                    endsInHazard = true;
                }
                else if (unit.IsCapturable()) path.Add(next);
                break;
            }
            if (!tile.IsWalkableTerrain()) break;
            path.Add(next);
            current = next;
            if (tile.type == FabricatorBoardTile.Type.Snare) break;
            if (tile.type != FabricatorBoardTile.Type.Router) continue;
            direction = tile.GetRouterDirection();
            if (!visitedRouterSet.Add((next, direction))) break;
        }
        return path;
    }

    // moves the bug to the end of the path: it dies in the hazard (no energy spent), or captures what stands there,
    // paying 1 energy and gaining what the capture restores
    public void ApplySlide(FabricatorBoardUnit bug, List<Vector2Int> path, bool endsInHazard)
    {
        Vector2Int destination = path[path.Count - 1];
        bug.previousCell = bug.cell;
        bug.cell = destination;
        if (endsInHazard)
        {
            bug.isAlive = false;
            return;
        }
        int restored = 0;
        foreach (FabricatorBoardUnit unit in unitList)
        {
            if (unit == bug || !unit.isAlive || unit.cell != destination) continue;
            unit.isAlive = false;
            restored = unit.energyOnCapture;
        }
        SetEnergy(energy - 1 + restored);
    }

    public bool HasMovingCells(bool isHostile)
    {
        foreach (FabricatorBoardUnit unit in unitList)
            if (unit.isAlive && unit.type == FabricatorBoardUnit.Type.MovingCell && unit.isDangerous == isHostile) return true;
        return false;
    }

    // One group's turn (passive or hostile cells, together): each takes the next step of its sequence, in placement order.
    // A step into a wall, closed gate, roots, another unit or the board edge is skipped (the cell waits), but the sequence
    // still advances. Into the bug: a passive cell is captured (the bug gains its energy), a hostile one takes the cell and
    // destroys the bug. Every cell's previousCell is where it started this turn.
    public void StepMovingCells(bool isHostile)
    {
        foreach (FabricatorBoardUnit unit in unitList)
        {
            if (!unit.isAlive || unit.type != FabricatorBoardUnit.Type.MovingCell || unit.isDangerous != isHostile) continue;
            unit.previousCell = unit.cell;
            Vector2Int step = unit.GetNextStep();
            unit.AdvanceSequence();
            if (step == Vector2Int.zero) continue;
            Vector2Int target = unit.cell + step;
            if (!tileDictionary.TryGetValue(target, out FabricatorBoardTile tile)) continue;
            if (TryGetUnit(target, out FabricatorBoardUnit other))
            {
                if (other.type != FabricatorBoardUnit.Type.Bug) continue;
                if (isHostile) other.isAlive = false;
                else
                {
                    unit.isAlive = false;
                    SetEnergy(energy + unit.energyOnCapture);
                }
                unit.cell = target;
                continue;
            }
            if (tile.IsWalkableTerrain() && !IsRootBlocked(target, out _)) unit.cell = target;
        }
    }

    // one finished turn: timed root retractions and the glands' own timers count down
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

    // Lose: no bug can slide anywhere, not even into a deadly cell (the player decides whether to take those), and nothing
    // on the board can be clicked (a click might open a path)
    public bool IsStuck()
    {
        foreach (FabricatorBoardUnit unit in unitList)
        {
            if (!unit.isAlive || unit.type != FabricatorBoardUnit.Type.Bug) continue;
            foreach (Vector2Int direction in MoveDirectionArray)
                if (GetSlidePath(unit.cell, direction, out _).Count > 0) return false;
        }
        foreach (FabricatorBoardTile tile in tileDictionary.Values)
            if (CanInteract(tile)) return false;
        return true;
    }

    private void SetEnergy(int value)
    {
        if (isEnergyLimited) energy = Mathf.Clamp(value, 0, maxEnergy);
    }

    private int CountLiving(FabricatorBoardUnit.Type type)
    {
        int count = 0;
        foreach (FabricatorBoardUnit unit in unitList)
            if (unit.isAlive && unit.type == type) count++;
        return count;
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
            case 'X': return FabricatorBoardTile.Type.Snare;
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
}
