using System;
using UnityEngine;

// Films the puzzle board flat-on (orthographic, like the Sawit-Fabricator game) while it is open, and fits the board,
// frame included, into the part of the screen the HUD leaves free. Re-fitted every frame, so window resizes keep it framed.
[RequireComponent(typeof(Camera))]
public class FabricatorPuzzleCamera : MonoBehaviour
{
    private const float CAMERA_DISTANCE = 10f;

    [SerializeField] private FabricatorController fabricator;
    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private Camera worldCamera;
    [Tooltip("Where the board goes on screen, in viewport units (0..1): clear of the task list (top left), the hotbar (bottom) and the puzzle panel (right).")]
    [SerializeField] private Rect boardViewportRect = new Rect(.03f, .13f, .69f, .73f);
    [Tooltip("Extra space around the board, in cells.")]
    [SerializeField] private float paddingCells = 1f;

    private Camera puzzleCamera;
    private Vector3 boardCentre;
    private Vector2 boardSize;

    private void Awake()
    {
        puzzleCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        fabricator.OnStateChanged += Fabricator_OnStateChanged;
        miniGame.OnBuilt += MiniGame_OnBuilt;
        SetActive(false);
    }

    private void LateUpdate()
    {
        if (!puzzleCamera.enabled || boardSize == Vector2.zero) return;
        float aspect = puzzleCamera.aspect;
        float size = Mathf.Max(boardSize.y * .5f / boardViewportRect.height, boardSize.x * .5f / (aspect * boardViewportRect.width));
        puzzleCamera.orthographicSize = size;
        // the board's centre sits in the middle of the free rect, not the middle of the screen
        Vector2 shift = new Vector2((.5f - boardViewportRect.center.x) * 2f * aspect * size, (.5f - boardViewportRect.center.y) * 2f * size);
        transform.SetPositionAndRotation(boardCentre + new Vector3(shift.x, shift.y, -CAMERA_DISTANCE), Quaternion.identity);
    }

    private void Fabricator_OnStateChanged(object sender, FabricatorController.OnStateChangedEventArgs e)
    {
        SetActive(e.state == FabricatorController.State.Puzzle || e.state == FabricatorController.State.Solved);
    }

    private void MiniGame_OnBuilt(object sender, EventArgs e)
    {
        Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue), max = new Vector2Int(int.MinValue, int.MinValue);
        foreach (FabricatorBoardTile tile in miniGame.GetBoard().GetTiles())
        {
            min = Vector2Int.Min(min, tile.cell);
            max = Vector2Int.Max(max, tile.cell);
        }
        boardCentre = (miniGame.GetCellWorldPosition(min) + miniGame.GetCellWorldPosition(max)) * .5f;
        float cellSize = miniGame.GetCellSize();
        boardSize = new Vector2(max.x - min.x + 1, max.y - min.y + 1) * cellSize + Vector2.one * paddingCells * 2f * cellSize;
    }

    private void SetActive(bool isActive)
    {
        puzzleCamera.enabled = isActive;
        worldCamera.enabled = !isActive; // no need to render the village behind a full-screen puzzle
    }
}
