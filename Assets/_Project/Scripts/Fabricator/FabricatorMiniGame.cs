using System;
using System.Collections.Generic;
using UnityEngine;

// The puzzle inside the Fabricator (the Sawit-Fabricator rules, see FabricatorBoard): click the Tech Bug, click an arrow next
// to it, and it slides until something stops it. Each slide costs 1 energy. After a slide the Moving Cells take their turn
// (passive ones, then hostile ones). Corrupting every Seed Embryo solves it; losing the bug, running out of energy or getting
// stuck loses ([R] rebuilds it, Esc leaves). The bug is picked automatically when there is only one.
public class FabricatorMiniGame : MonoBehaviour
{
    private const float HAZARD_DEATH_DURATION = .25f;

    public event EventHandler OnBuilt;
    // something on the board changed: selection, a gate/router/gland click, a finished slide or Moving Cell step, energy
    public event EventHandler OnBoardChanged;
    public event EventHandler OnSlideStarted;
    public event EventHandler OnSolved;
    public event EventHandler OnLost;
    public event EventHandler OnEnded;

    public enum State { Off, Playing, Sliding, MovingCells, Solved, Lost }
    public enum LoseReason { Destroyed, OutOfEnergy, Stuck }

    // a direction the selected bug can slide in; the arrow sits on the neighbouring cell
    public struct Move
    {
        public Vector2Int direction;
        public bool endsInHazard;
    }

    [SerializeField] private Grid grid;
    [SerializeField] private Camera puzzleCamera;
    [Tooltip("Cells per second while the bug slides; a Moving Cell's step takes one cell's worth.")]
    [SerializeField] private float slideSpeed = 8f;
    [Tooltip("Testing: every level starts with this much energy (its cap is raised to match). 0 = each level's own energy.")]
    [SerializeField] private int testInitialEnergy = 15;

    private FabricatorPuzzleSO puzzleSO;
    private FabricatorBoard board;
    private State state;
    private FabricatorBoardUnit selectedBug;
    private readonly List<Move> moveList = new List<Move>();
    private FabricatorBoardUnit slidingBug;
    private Vector2Int slideStartCell;
    private List<Vector2Int> slidePath;
    private bool slideEndsInHazard;
    // the slide's or the Moving Cell step's time so far
    private float timer;
    // which Moving Cells are stepping (passive first, then hostile)
    private bool isHostileStep;
    private int moveCount;
    private LoseReason loseReason;

    private void Start()
    {
        GameInput.Instance.OnAttackAction += GameInput_OnAttackAction;
    }

    private void Update()
    {
        if (state != State.Sliding && state != State.MovingCells) return;
        timer += Time.deltaTime;
        if (state == State.Sliding && timer >= GetSlideDuration()) FinishSlide();
        else if (state == State.MovingCells && timer >= GetCellStepDuration())
        {
            if (isHostileStep) EndTurn();
            else StepHostileCells();
        }
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnAttackAction -= GameInput_OnAttackAction;
    }

    private void GameInput_OnAttackAction(object sender, EventArgs e)
    {
        if (state != State.Playing || !TryGetPointerCell(out Vector2Int cell)) return;

        // the bug: pick it, or drop it again (that frees its neighbours for clicks)
        if (board.TryGetUnit(cell, out FabricatorBoardUnit unit) && unit.type == FabricatorBoardUnit.Type.Bug)
        {
            selectedBug = unit == selectedBug ? null : unit;
            ChangeBoard();
            return;
        }
        // an arrow wins over the tile under it, like the arrows drawn in front of the board
        if (TryGetMoveTo(cell, out Move move))
        {
            StartSlide(move);
            return;
        }
        if (board.TryGetTile(cell, out FabricatorBoardTile tile) && board.CanInteract(tile))
        {
            board.Interact(tile);
            ChangeBoard();
            if (board.IsStuck()) Lose(LoseReason.Stuck);
            return;
        }
        selectedBug = null;
        ChangeBoard();
    }

    public void Begin(FabricatorPuzzleSO puzzleSO)
    {
        this.puzzleSO = puzzleSO;
        Build();
    }

    public void Restart()
    {
        if (state != State.Off && state != State.Solved) Build();
    }

    public void End()
    {
        SetState(State.Off);
        OnEnded?.Invoke(this, EventArgs.Empty);
    }

    public bool IsRunning() => state != State.Off;
    public bool IsPlaying() => state == State.Playing;
    public LoseReason GetLoseReason() => loseReason;
    public FabricatorBoard GetBoard() => board;
    public FabricatorPuzzleSO GetPuzzleSO() => puzzleSO;
    public List<Move> GetMoveList() => moveList;
    public int GetMoveCount() => moveCount;
    public int GetParMoves() => puzzleSO.parMoves;
    public int GetEmbryoCount() => board.GetEmbryoCount();
    public float GetCellSize() => grid.cellSize.x;
    public Vector3 GetCellWorldPosition(Vector2Int cell) => grid.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));

    public bool TryGetSelectedBug(out FabricatorBoardUnit bug)
    {
        bug = selectedBug;
        return bug != null;
    }

    public bool TryGetSlide(out FabricatorBoardUnit bug, out Vector2Int startCell, out List<Vector2Int> path, out float cellsTravelled)
    {
        bug = slidingBug;
        startCell = slideStartCell;
        path = slidePath;
        cellsTravelled = state == State.Sliding ? Mathf.Min(timer * slideSpeed, slidePath.Count) : 0f;
        return state == State.Sliding;
    }

    // 0..1 once a bug sliding into thorns or a hostile Moving Cell has reached it
    public float GetHazardDeathProgress()
    {
        if (state != State.Sliding || !slideEndsInHazard) return 0f;
        return Mathf.Clamp01((timer - slidePath.Count / slideSpeed) / HAZARD_DEATH_DURATION);
    }

    // while one group of Moving Cells steps: which group, and how far along (0..1) the step is
    public bool TryGetCellStep(out bool isHostile, out float progress)
    {
        isHostile = isHostileStep;
        progress = Mathf.Clamp01(timer / GetCellStepDuration());
        return state == State.MovingCells;
    }

    // the board cell under the mouse, on the board's plane (the puzzle camera may move, so this goes through a ray)
    public bool TryGetPointerCell(out Vector2Int cell)
    {
        cell = default;
        if (board == null) return false;
        Ray ray = puzzleCamera.ScreenPointToRay(GameInput.Instance.GetPointerScreenPosition());
        Plane plane = new Plane(Vector3.back, grid.transform.position);
        if (!plane.Raycast(ray, out float distance)) return false;
        Vector3Int gridCell = grid.WorldToCell(ray.GetPoint(distance));
        cell = new Vector2Int(gridCell.x, gridCell.y);
        return board.TryGetTile(cell, out _);
    }

    public bool TryGetMoveTo(Vector2Int cell, out Move move)
    {
        foreach (Move candidate in moveList)
        {
            if (selectedBug.cell + candidate.direction != cell) continue;
            move = candidate;
            return true;
        }
        move = default;
        return false;
    }

    // solved within the puzzle's par
    public bool IsCombo() => state == State.Solved && puzzleSO.parMoves > 0 && moveCount <= puzzleSO.parMoves;

    private void Build()
    {
        board = new FabricatorBoard(puzzleSO, testInitialEnergy);
        moveCount = 0;
        slidingBug = null;
        SelectOnlyBug();
        RefreshMoves();
        SetState(State.Playing);
        OnBuilt?.Invoke(this, EventArgs.Empty);
    }

    private void StartSlide(Move move)
    {
        slidingBug = selectedBug;
        slideStartCell = selectedBug.cell;
        slidePath = board.GetSlidePath(selectedBug.cell, move.direction, out slideEndsInHazard);
        timer = 0f;
        selectedBug = null;
        RefreshMoves();
        SetState(State.Sliding);
        OnSlideStarted?.Invoke(this, EventArgs.Empty);
    }

    private void FinishSlide()
    {
        board.ApplySlide(slidingBug, slidePath, slideEndsInHazard);
        moveCount++;
        // win before the Moving Cells and the energy check, so the last capture wins even on the last energy
        if (board.IsWon())
        {
            ChangeBoard();
            SetState(State.Solved);
            return;
        }
        if (board.HasLivingBug() && board.HasMovingCells(false)) StartCellStep(false);
        else StepHostileCells();
    }

    private void StepHostileCells()
    {
        if (board.HasLivingBug() && board.HasMovingCells(true)) StartCellStep(true);
        else EndTurn();
    }

    private void StartCellStep(bool isHostile)
    {
        isHostileStep = isHostile;
        board.StepMovingCells(isHostile);
        timer = 0f;
        SetState(State.MovingCells);
        ChangeBoard();
    }

    private void EndTurn()
    {
        if (!board.HasLivingBug())
        {
            ChangeBoard();
            Lose(LoseReason.Destroyed);
            return;
        }
        board.PassTurn();
        SelectOnlyBug();
        SetState(State.Playing);
        ChangeBoard();
        if (board.IsOutOfEnergy()) Lose(LoseReason.OutOfEnergy);
        else if (board.IsStuck()) Lose(LoseReason.Stuck);
    }

    private float GetSlideDuration() => slidePath.Count / slideSpeed + (slideEndsInHazard ? HAZARD_DEATH_DURATION : 0f);
    private float GetCellStepDuration() => 1f / slideSpeed;

    private void SelectOnlyBug()
    {
        selectedBug = null;
        foreach (FabricatorBoardUnit unit in board.GetUnitList())
        {
            if (!unit.isAlive || unit.type != FabricatorBoardUnit.Type.Bug) continue;
            if (selectedBug != null)
            {
                selectedBug = null;
                return;
            }
            selectedBug = unit;
        }
    }

    // only offer directions where the bug would actually move (deadly moves are offered too, as a warning)
    private void RefreshMoves()
    {
        moveList.Clear();
        if (selectedBug == null) return;
        foreach (Vector2Int direction in FabricatorBoard.MoveDirectionArray)
        {
            List<Vector2Int> path = board.GetSlidePath(selectedBug.cell, direction, out bool endsInHazard);
            if (path.Count > 0) moveList.Add(new Move { direction = direction, endsInHazard = endsInHazard });
        }
    }

    private void ChangeBoard()
    {
        RefreshMoves();
        OnBoardChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Lose(LoseReason loseReason)
    {
        this.loseReason = loseReason;
        selectedBug = null;
        RefreshMoves();
        SetState(State.Lost);
    }

    private void SetState(State state)
    {
        this.state = state;
        if (state == State.Solved) OnSolved?.Invoke(this, EventArgs.Empty);
        else if (state == State.Lost) OnLost?.Invoke(this, EventArgs.Empty);
    }
}
