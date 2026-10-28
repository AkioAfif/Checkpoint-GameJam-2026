using System;
using System.Collections.Generic;
using UnityEngine;

// The puzzle inside the tube (the Sawit-Fabricator rules, see FabricatorBoard): click the Tech Bug, click an arrow next to it,
// and it slides until something stops it. Corrupting every Seed Embryo solves it; losing the bug in thorns or getting stuck loses
// ([R] rebuilds it, Esc leaves). The bug is picked automatically when there is only one.
public class FabricatorMiniGame : MonoBehaviour
{
    private const float HAZARD_DEATH_DURATION = .25f;

    public event EventHandler OnBuilt;
    // something on the board changed: selection, a gate/router/gland click, a finished slide
    public event EventHandler OnBoardChanged;
    public event EventHandler OnSlideStarted;
    public event EventHandler OnSolved;
    public event EventHandler OnLost;
    public event EventHandler OnEnded;

    public enum State { Off, Playing, Sliding, Solved, Lost }

    // a direction the selected bug can slide in; the arrow sits on the neighbouring cell
    public struct Move
    {
        public Vector2Int direction;
        public bool endsInHazard;
    }

    [SerializeField] private Grid grid;
    [SerializeField] private Camera puzzleCamera;
    [Tooltip("Cells per second while the bug slides.")]
    [SerializeField] private float slideSpeed = 8f;

    private FabricatorPuzzleSO puzzleSO;
    private FabricatorBoard board;
    private State state;
    private FabricatorBoardUnit selectedBug;
    private readonly List<Move> moveList = new List<Move>();
    private FabricatorBoardUnit slidingBug;
    private Vector2Int slideStartCell;
    private List<Vector2Int> slidePath;
    private bool slideEndsInHazard;
    private float slideTimer;
    private int moveCount;
    private bool hasPlayerMoved;
    private bool isLostInThorns;

    private void Start()
    {
        GameInput.Instance.OnAttackAction += GameInput_OnAttackAction;
    }

    private void Update()
    {
        if (state != State.Sliding) return;
        slideTimer += Time.deltaTime;
        if (slideTimer >= GetSlideDuration()) FinishSlide();
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
            if (board.IsStuck()) SetState(State.Lost);
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
        if (state == State.Playing || state == State.Sliding || state == State.Lost) Build();
    }

    public void End()
    {
        SetState(State.Off);
        OnEnded?.Invoke(this, EventArgs.Empty);
    }

    public bool IsRunning() => state != State.Off;
    public bool IsPlaying() => state == State.Playing;
    public bool IsLost() => state == State.Lost;
    // lost because the last bug slid into thorny roots (not because it got stuck)
    public bool IsLostInThorns() => isLostInThorns;
    public FabricatorBoard GetBoard() => board;
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
        cellsTravelled = state == State.Sliding ? Mathf.Min(slideTimer * slideSpeed, slidePath.Count) : 0f;
        return state == State.Sliding;
    }

    // 0..1 once a bug sliding into thorns has reached them
    public float GetHazardDeathProgress()
    {
        if (state != State.Sliding || !slideEndsInHazard) return 0f;
        return Mathf.Clamp01((slideTimer - slidePath.Count / slideSpeed) / HAZARD_DEATH_DURATION);
    }

    // the board cell under the mouse, on the board's plane (the puzzle camera sways, so this goes through a ray)
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

    // tutorial ghost cursor: shown until the player's first slide
    public bool IsHintActive() => IsRunning() && state != State.Solved && !hasPlayerMoved && puzzleSO.hasHint;

    // screen position along the hint path, t = 0..1
    public Vector2 GetHintScreenPoint(float t)
    {
        string[] rows = puzzleSO.GetRows();
        Vector3 from = GetCellWorldPosition(FabricatorBoard.LayoutToCell(puzzleSO.hintFromCell, rows));
        Vector3 to = GetCellWorldPosition(FabricatorBoard.LayoutToCell(puzzleSO.hintToCell, rows));
        return puzzleCamera.WorldToScreenPoint(Vector3.Lerp(from, to, t));
    }

    // solved within the puzzle's par
    public bool IsCombo() => state == State.Solved && puzzleSO.parMoves > 0 && moveCount <= puzzleSO.parMoves;

    private void Build()
    {
        board = new FabricatorBoard(puzzleSO);
        moveCount = 0;
        hasPlayerMoved = false;
        isLostInThorns = false;
        slidingBug = null;
        selectedBug = null;
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
        slideTimer = 0f;
        selectedBug = null;
        RefreshMoves();
        SetState(State.Sliding);
        OnSlideStarted?.Invoke(this, EventArgs.Empty);
    }

    private void FinishSlide()
    {
        board.ApplySlide(slidingBug, slidePath, slideEndsInHazard);
        moveCount++;
        hasPlayerMoved = true;
        // win before the turn passes, so the last capture wins even if roots grow back this turn
        if (board.GetEmbryoCount() == 0)
        {
            ChangeBoard();
            SetState(State.Solved);
            return;
        }
        if (!board.HasLivingBug())
        {
            isLostInThorns = true;
            ChangeBoard();
            SetState(State.Lost);
            return;
        }
        board.PassTurn();
        SelectOnlyBug();
        SetState(State.Playing);
        ChangeBoard();
        if (board.IsStuck()) SetState(State.Lost);
    }

    private float GetSlideDuration() => slidePath.Count / slideSpeed + (slideEndsInHazard ? HAZARD_DEATH_DURATION : 0f);

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

    // only offer directions where the bug would actually move (moves into thorns are offered too, as a warning)
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

    private void SetState(State state)
    {
        this.state = state;
        if (state == State.Solved) OnSolved?.Invoke(this, EventArgs.Empty);
        else if (state == State.Lost) OnLost?.Invoke(this, EventArgs.Empty);
    }
}
