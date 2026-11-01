using System;
using UnityEngine;

// Films the puzzle board flat-on (orthographic, like the Sawit-Fabricator game) while it is open, and fits the board,
// frame included, into the part of the screen the HUD leaves free. Re-fitted every frame, so window resizes keep it framed.
// When the screen allows, a cell is drawn at a whole multiple of the pixel art's size and lined up on the pixel grid, so
// every art pixel is a crisp square; a smaller screen gets an exact half or quarter of it, and only then the plain fit.
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
    [Tooltip("The board art's pixels per cell (the Sawit pixel set: 64). 0 = no pixel snapping.")]
    [SerializeField] private int artPixelsPerCell = 64;
    [Tooltip("Space a pixel-snapped board may shrink its padding to, in cells: at least the frame's border (0.375).")]
    [SerializeField] private float minPaddingCells = .5f;

    private Camera puzzleCamera;
    private Vector3 boardCentre;
    private Vector2 boardCells;
    private Vector2 boardCorner;

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
        if (!puzzleCamera.enabled || boardCells == Vector2.zero) return;
        float aspect = puzzleCamera.aspect;
        float cellSize = miniGame.GetCellSize();
        float size = GetFitSize(paddingCells, aspect) * cellSize;
        float pixelsPerUnit = 0f;
        if (artPixelsPerCell > 0)
        {
            // the largest whole multiple of the art's size that fits with the full padding, or with the frame only; a small
            // screen gets an exact half or quarter (every 2nd/4th art pixel, still even squares) rather than an uneven fit
            float fitPixelsPerCell = puzzleCamera.pixelHeight / (2f * GetFitSize(paddingCells, aspect));
            float tightPixelsPerCell = puzzleCamera.pixelHeight / (2f * GetFitSize(minPaddingCells, aspect));
            float pixelsPerCell = Mathf.Floor(fitPixelsPerCell / artPixelsPerCell) * artPixelsPerCell;
            for (int divisor = 1; pixelsPerCell == 0f && divisor <= 4; divisor *= 2)
                if (tightPixelsPerCell >= (float)artPixelsPerCell / divisor) pixelsPerCell = (float)artPixelsPerCell / divisor;
            if (pixelsPerCell > 0f)
            {
                pixelsPerUnit = pixelsPerCell / cellSize;
                size = puzzleCamera.pixelHeight / (2f * pixelsPerUnit);
            }
        }
        puzzleCamera.orthographicSize = size;
        // the board's centre sits in the middle of the free rect, not the middle of the screen
        Vector2 shift = new Vector2((.5f - boardViewportRect.center.x) * 2f * aspect * size, (.5f - boardViewportRect.center.y) * 2f * size);
        Vector2 position = (Vector2)boardCentre + shift;
        if (pixelsPerUnit > 0f) position = new Vector2(SnapToPixels(position.x, boardCorner.x, puzzleCamera.pixelWidth, pixelsPerUnit), SnapToPixels(position.y, boardCorner.y, puzzleCamera.pixelHeight, pixelsPerUnit));
        transform.SetPositionAndRotation(new Vector3(position.x, position.y, boardCentre.z - CAMERA_DISTANCE), Quaternion.identity);
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
        boardCells = new Vector2(max.x - min.x + 1, max.y - min.y + 1);
        boardCorner = (Vector2)miniGame.GetCellWorldPosition(min) - Vector2.one * miniGame.GetCellSize() * .5f;
    }

    // half the view height, in cells, that fits the board plus this much padding into the free rect
    private float GetFitSize(float padding, float aspect)
    {
        Vector2 cells = boardCells + Vector2.one * padding * 2f;
        return Mathf.Max(cells.y * .5f / boardViewportRect.height, cells.x * .5f / (aspect * boardViewportRect.width));
    }

    // moves the camera so the board's cell edges fall on screen pixel edges
    private static float SnapToPixels(float cameraPosition, float boardEdge, int screenPixels, float pixelsPerUnit)
    {
        float pixelsFromEdge = Mathf.Round((cameraPosition - boardEdge) * pixelsPerUnit - screenPixels * .5f);
        return boardEdge + (pixelsFromEdge + screenPixels * .5f) / pixelsPerUnit;
    }

    private void SetActive(bool isActive)
    {
        puzzleCamera.enabled = isActive;
        worldCamera.enabled = !isActive; // no need to render the village behind a full-screen puzzle
    }
}
