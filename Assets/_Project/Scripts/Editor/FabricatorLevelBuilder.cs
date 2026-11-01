using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// The Fabricator's levels: Levels 1–12 of the Sawit-Fabricator project, converted from its LevelData assets (tile rows top
// row first, unit cells as layout column/row, root offsets and Moving Cell steps y up). Each was checked solvable by a
// brute-force solver of the same rules, with its own energy and with 15. Content lives here only to seed the assets;
// re-running Build rewrites them, keeping their GUIDs.
public static class FabricatorLevelBuilder
{
    public static FabricatorLevelListSO Build()
    {
        FabricatorLevelListSO levelListSO = GameDataBuilder.Asset<FabricatorLevelListSO>("Puzzles", "FabricatorLevelList");
        levelListSO.puzzleSOList = new List<FabricatorPuzzleSO>
        {
            Level("Level_01_Movement", "Movement", 0, 10,
                new[] { "#########", "#.......#", "#.......#", "#####.###", "####.####", "###.#####", "##.######", "#.......#", "#########" },
                Bug(1, 1),
                Embryo(7, 1, 1),
                Embryo(1, 7, 1),
                Embryo(7, 7, 1)),
            Level("Level_02_StopTile", "Stop Tile", 0, 10,
                new[] { "#########", "#.......#", "#.......#", "#.......#", "#...X...#", "#.......#", "#.......#", "#.......#", "#########" },
                Bug(7, 7),
                Embryo(1, 1, 1),
                Embryo(7, 1, 1),
                Embryo(2, 4, 1)),
            Level("Level_03_Energy", "Energy", 4, 10,
                new[] { "############", "#..........#", "#.########.#", "#.......##.#", "#######.#..#", "#######.#.##", "#.......#.##", "#.#######..#", "#.......##.#", "#######.##.#", "#..........#", "############" },
                Bug(10, 1),
                Embryo(10, 10, 1),
                EnergyCell(7, 3, 2),
                EnergyCell(1, 6, 2),
                EnergyCell(7, 8, 2)),
            Level("Level_04_BlockingVines", "Blocking Vines", 4, 10,
                new[] { "##################", "#.....#......#####", "#.....#......#...#", "#.........##.....#", "#....##....#.....#", "#....##..........#", "##################" },
                Sentinel(3, 3, false, V(0, 1), V(0, 2), V(0, -1), V(0, -2)),
                Bug(1, 3),
                Sentinel(5, 1, false, V(0, -1), V(0, -2), V(0, -3), V(0, -4)),
                Sentinel(10, 3, false, V(0, 1), V(0, 2)),
                Embryo(11, 1, 1),
                EnergyCell(9, 3, 3),
                Sentinel(16, 3, false, V(0, -1), V(-1, -1), V(-2, -1), V(-3, -1)),
                Sentinel(13, 3, false, V(-1, 1)),
                EnergyCell(5, 3, 3),
                Embryo(16, 5, 1),
                Embryo(15, 3, 1)),
            Level("Level_05_PatrolingSentinels", "Patroling Sentinels", 5, 10,
                new[] { "###################", "##.#.#...##.#######", "#....#.....X.....##", "##.#.#.#.#..#######", "####.#.#....#######", "####..X..##.......#", "###################" },
                Bug(1, 2),
                MovingCell(3, 2, true, 0, V(1, 0), V(-1, 0)),
                MovingCell(8, 4, true, 0, V(0, 1), V(0, 1), V(1, 0), V(1, 0), V(0, -1), V(0, -1), V(-1, 0), V(-1, 0)),
                EnergyCell(8, 5, 3),
                Embryo(6, 1, 1),
                Embryo(4, 5, 1),
                EnergyCell(11, 5, 3),
                EnergyCell(11, 1, 3),
                Embryo(16, 2, 1),
                Embryo(17, 5, 1)),
            Level("Level_06_Drifters", "Drifters", 4, 10,
                new[] { "###################", "######............#", "#..###..X#...#..###", "#..###..........###", "#............#.####", "#....X..X.........#", "###################" },
                Bug(2, 2),
                MovingCell(2, 4, true, 0, V(0, 1), V(0, -1)),
                MovingCell(10, 2, false, 5, V(0, 1), V(-1, 0), V(-1, 0), V(0, -1), V(0, -1), V(1, 0), V(1, 0), V(0, 1)),
                Embryo(6, 2, 1),
                MovingCell(14, 3, true, 0, V(0, -1), V(0, -1), V(-1, 0), V(-1, 0), V(0, 1), V(0, 1), V(1, 0), V(1, 0)),
                MovingCell(15, 3, true, 0, V(0, 1), V(0, 1), V(0, -1), V(0, -1)),
                Embryo(17, 1, 1),
                Embryo(17, 5, 1)),
            Level("Level_07_StoppingRule", "Stopping Rule", 5, 10,
                new[] { "############", "#XX#########", "#X.........#", "#.###.####.#", "#.#........#", "#.###.######", "#.#..##..###", "#...X...X..#", "##########.#", "###..#..##.#", "#..X..X....#", "############" },
                Bug(1, 10),
                MovingCell(4, 10, true, 0, V(0, 1), V(0, -1)),
                MovingCell(7, 10, true, 0, V(0, 1), V(0, -1)),
                EnergyCell(6, 9, 3),
                EnergyCell(10, 7, 1),
                MovingCell(3, 7, false, 5, V(0, 1), V(0, -1)),
                MovingCell(7, 6, false, 5, V(0, -1), V(0, 1)),
                Embryo(3, 4, 1),
                MovingCell(5, 2, true, 0, V(0, 1), V(0, -1)),
                MovingCell(5, 5, true, 0, V(0, 1), V(0, -1)),
                EnergyCell(10, 4, 3),
                // Sawit e92b5a1 (2026-10-29): a second embryo on the top-left snare
                Embryo(1, 1, 1)),
            Level("Level_08_BiblicalGreed", "Biblical Greed", 5, 10,
                new[] { "############", "#..........#", "#.##...###.#", "#.##X....#.#", "#.#....###.#", "#.....X###.#", "###.......X#", "##########.#", "#...######.#", "#..........#", "#...########", "############" },
                Bug(4, 9),
                EnergyCell(1, 8, 3),
                EnergyCell(2, 8, 3),
                EnergyCell(3, 8, 3),
                EnergyCell(1, 9, 3),
                EnergyCell(2, 9, 3),
                EnergyCell(3, 9, 3),
                EnergyCell(1, 10, 3),
                EnergyCell(2, 10, 3),
                EnergyCell(3, 10, 3),
                MovingCell(4, 6, true, 0, V(1, 0), V(1, 0), V(1, 0), V(1, 0), V(1, 0)),
                EnergyCell(10, 1, 1),
                Embryo(4, 2, 1),
                Embryo(8, 3, 1)),
            Level("Level_09_Doors", "Doors", 5, 10,
                new[] { "#########", "#...G...#", "###.##..#", "###.##.##", "###.##G##", "###G##..#", "#....#.##", "#....#.##", "#########" },
                Bug(6, 7),
                EnergyCell(7, 5, 3),
                MovingCell(6, 2, true, 0, V(0, 1), V(1, 0), V(0, -1), V(-1, 0)),
                EnergyCell(1, 1, 3),
                Embryo(1, 7, 1),
                MovingCell(3, 6, true, 0, V(1, 0), V(0, -1), V(-1, 0), V(0, 1))),
            Level("Level_10_MuhammadRigel", "Muhammad Rigel", 5, 10,
                new[] { "###############", "#......########", "#.......G.....#", "#......###.##.#", "#......#.####G#", "#......#......#", "#......#..X...#", "#......#......#", "###############" },
                Bug(1, 7),
                Sentinel(1, 4, false, V(1, 0), V(2, 0), V(3, 0)),
                Sentinel(4, 7, false, V(0, 1), V(0, 2), V(0, 3)),
                Embryo(3, 2, 1),
                MovingCell(10, 3, true, 0, V(0, 1), V(0, -1)),
                Embryo(9, 6, 1),
                Sentinel(1, 1, true, V(1, 0), V(2, 0), V(3, 0), V(4, 0), V(4, -1)),
                Sentinel(6, 5, true, V(0, 1), V(0, 2), V(0, 3)),
                EnergyCell(7, 2, 3),
                Embryo(11, 5, 1),
                MovingCell(12, 7, false, 5, V(1, 0), V(0, 1), V(-1, 0), V(0, -1))),
            Level("Level_11_Ilhama", "Ilhama", 5, 10,
                new[] { "###############", "#######......##", "#.....G..#....#", "#........#....#", "#.............#", "####....G.....#", "####....#.....#", "#.....#.......#", "###############" },
                Bug(1, 7),
                Embryo(11, 3, 1),
                Sentinel(8, 1, false, V(1, 0), V(2, 0), V(2, -1), V(2, -2), V(2, -3), V(3, -3), V(4, -3)),
                EnergyCell(1, 2, 3)),
            Level("Level_12_Aiko", "Aiko", 5, 10,
                new[] { "############", "#..........#", "#..........#", "#..........#", "#..........#", "#......G...#", "#..........#", "#....#.....#", "#.......G..#", "###........#", "#.....#....#", "#..........#", "#.##..#.#..#", "#.##..#.#..#", "############" },
                Bug(1, 13),
                Embryo(1, 5, 1),
                Embryo(8, 5, 1),
                Embryo(7, 2, 1),
                Embryo(4, 11, 1),
                EnergyCell(4, 2, 3))
        };
        EditorUtility.SetDirty(levelListSO);
        AssetDatabase.SaveAssets();
        return levelListSO;
    }

    private static FabricatorPuzzleSO Level(string file, string puzzleName, int initialEnergy, int maxEnergy, string[] rowArray, params FabricatorPuzzleSO.UnitPlacement[] placementArray)
    {
        FabricatorPuzzleSO puzzleSO = GameDataBuilder.Asset<FabricatorPuzzleSO>("Puzzles", file);
        puzzleSO.puzzleName = puzzleName;
        puzzleSO.layout = string.Join("\n", rowArray);
        puzzleSO.unitPlacementList = new List<FabricatorPuzzleSO.UnitPlacement>(placementArray);
        puzzleSO.initialEnergy = initialEnergy;
        puzzleSO.maxEnergy = maxEnergy;
        puzzleSO.retractTurns = 2;
        puzzleSO.parMoves = 0;
        EditorUtility.SetDirty(puzzleSO);
        return puzzleSO;
    }

    private static FabricatorPuzzleSO.UnitPlacement Bug(int column, int row) => Unit(FabricatorBoardUnit.Type.Bug, column, row, false, 0);
    private static FabricatorPuzzleSO.UnitPlacement Embryo(int column, int row, int energy) => Unit(FabricatorBoardUnit.Type.Embryo, column, row, false, energy);
    private static FabricatorPuzzleSO.UnitPlacement EnergyCell(int column, int row, int energy) => Unit(FabricatorBoardUnit.Type.EnergyCell, column, row, false, energy);

    private static FabricatorPuzzleSO.UnitPlacement Sentinel(int column, int row, bool isThorny, params Vector2Int[] rootOffsetArray)
    {
        FabricatorPuzzleSO.UnitPlacement placement = Unit(FabricatorBoardUnit.Type.Sentinel, column, row, isThorny, 0);
        placement.rootOffsetArray = rootOffsetArray;
        return placement;
    }

    private static FabricatorPuzzleSO.UnitPlacement MovingCell(int column, int row, bool isHostile, int energy, params Vector2Int[] moveSequenceArray)
    {
        FabricatorPuzzleSO.UnitPlacement placement = Unit(FabricatorBoardUnit.Type.MovingCell, column, row, isHostile, energy);
        placement.moveSequenceArray = moveSequenceArray;
        return placement;
    }

    private static FabricatorPuzzleSO.UnitPlacement Unit(FabricatorBoardUnit.Type type, int column, int row, bool isDangerous, int energy)
    {
        return new FabricatorPuzzleSO.UnitPlacement { type = type, layoutCell = new Vector2Int(column, row), isDangerous = isDangerous, energyOnCapture = energy };
    }

    private static Vector2Int V(int x, int y) => new Vector2Int(x, y);
}
