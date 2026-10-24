using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// The puzzle inside the tube: dirt/rock tiles on a flat 2D-physics slice, liquid blobs that fall when you dig.
public class FabricatorMiniGame : MonoBehaviour
{
    public const int LIQUID_COUNT = 3;
    private const string LIQUID_CHARS = "WFC";
    private const float COUNT_INTERVAL = .15f;

    public event EventHandler OnBuilt;
    public event EventHandler OnDug;
    public event EventHandler OnSolved;
    public event EventHandler OnEnded;

    [Tooltip("The liquid behind W, F and C, in that order.")]
    [SerializeField] private ItemDataSO[] liquidItemDataSOArray;
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase dirtTile;
    [SerializeField] private TileBase rockTile;
    [SerializeField] private Transform particleParent;
    [SerializeField] private PhysicsMaterial2D particleMaterial;
    [SerializeField] private Camera puzzleCamera;
    [SerializeField] private float particleRadius = .12f;
    [SerializeField] private float digRadius = .55f;
    [Tooltip("All three liquids finishing within this many seconds of each other is a combo.")]
    [SerializeField] private float comboWindow = 1.5f;

    private FabricatorPuzzleSO puzzleSO;
    private readonly List<Rigidbody2D>[] particleListArray = { new List<Rigidbody2D>(), new List<Rigidbody2D>(), new List<Rigidbody2D>() };
    private readonly int[] inMouldCountArray = new int[LIQUID_COUNT];
    private readonly bool[] isLiquidUsedArray = new bool[LIQUID_COUNT];
    // when each liquid first reached its target (-1 = not yet)
    private readonly float[] doneTimeArray = new float[LIQUID_COUNT];
    private Rect mouldRect;
    private bool isRunning;
    private bool isSolved;
    private bool hasPlayerDug;
    private float countTimer;
    private int puzzleLayer;

    private void Awake()
    {
        puzzleLayer = gameObject.layer;
    }

    private void Update()
    {
        if (!isRunning || isSolved) return;
        if (GameInput.Instance.IsDigPressed()) TryDig(GameInput.Instance.GetPointerScreenPosition());

        countTimer -= Time.deltaTime;
        if (countTimer > 0f) return;
        countTimer = COUNT_INTERVAL;
        CountMould();
        for (int i = 0; i < LIQUID_COUNT; i++)
            if (doneTimeArray[i] < 0f && GetLiquidProgress(i) >= 1f) doneTimeArray[i] = Time.time;
        if (IsEveryLiquidDone())
        {
            isSolved = true;
            OnSolved?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Begin(FabricatorPuzzleSO puzzleSO)
    {
        this.puzzleSO = puzzleSO;
        Build();
        isRunning = true;
    }

    public void Restart()
    {
        if (isRunning && !isSolved) Build();
    }

    public void End()
    {
        isRunning = false;
        ClearParticles();
        tilemap.ClearAllTiles();
        OnEnded?.Invoke(this, EventArgs.Empty);
    }

    public bool IsRunning() => isRunning;
    public ItemDataSO GetLiquidItemDataSO(int liquidIndex) => liquidItemDataSOArray[liquidIndex];
    public List<Rigidbody2D> GetParticleList(int liquidIndex) => particleListArray[liquidIndex];
    public float GetParticleRadius() => particleRadius;
    public Tilemap GetTilemap() => tilemap;
    public bool IsDirt(Vector3Int cell) => tilemap.GetTile(cell) == dirtTile;
    public bool IsRock(Vector3Int cell) => tilemap.GetTile(cell) == rockTile;
    public Rect GetMouldRect() => mouldRect;

    // tutorial ghost cursor: shown until the player digs for the first time
    public bool IsHintActive() => isRunning && !isSolved && !hasPlayerDug && puzzleSO.hasHint;

    // screen position along the hint path, t = 0..1
    public Vector2 GetHintScreenPoint(float t)
    {
        Vector3 from = grid.GetCellCenterWorld(LayoutToCell(puzzleSO.hintFromCell));
        Vector3 to = grid.GetCellCenterWorld(LayoutToCell(puzzleSO.hintToCell));
        return puzzleCamera.WorldToScreenPoint(Vector3.Lerp(from, to, t));
    }

    // a puzzle may use only some of the liquids (the tutorial has just one)
    public bool IsLiquidUsed(int liquidIndex) => isLiquidUsedArray[liquidIndex];

    // every liquid in play reached the mould at nearly the same moment (one-liquid puzzles never combo)
    public bool IsCombo()
    {
        float first = float.MaxValue, last = float.MinValue;
        for (int i = 0; i < LIQUID_COUNT; i++)
        {
            if (!isLiquidUsedArray[i] || doneTimeArray[i] < 0f) return false;
            first = Mathf.Min(first, doneTimeArray[i]);
            last = Mathf.Max(last, doneTimeArray[i]);
        }
        return last - first <= comboWindow;
    }

    // 0..1 progress of one liquid toward its required amount
    public float GetLiquidProgress(int liquidIndex)
    {
        if (!isLiquidUsedArray[liquidIndex]) return 1f;
        float required = puzzleSO.particlesPerLiquid * puzzleSO.requiredFraction;
        return Mathf.Clamp01(inMouldCountArray[liquidIndex] / required);
    }

    private void Build()
    {
        ClearParticles();
        tilemap.ClearAllTiles();
        isSolved = false;
        hasPlayerDug = false;
        Array.Clear(inMouldCountArray, 0, LIQUID_COUNT);
        for (int i = 0; i < LIQUID_COUNT; i++) doneTimeArray[i] = -1f;

        string[] rows = puzzleSO.GetRows();
        List<Vector3Int>[] sourceCellListArray = { new List<Vector3Int>(), new List<Vector3Int>(), new List<Vector3Int>() };
        Vector3Int mouldMin = new Vector3Int(int.MaxValue, int.MaxValue, 0), mouldMax = new Vector3Int(int.MinValue, int.MinValue, 0);
        for (int r = 0; r < rows.Length; r++)
        {
            for (int c = 0; c < rows[r].Length; c++)
            {
                Vector3Int cell = LayoutToCell(new Vector2Int(c, r), rows); // centred, top row first
                char symbol = rows[r][c];
                if (symbol == '#') tilemap.SetTile(cell, dirtTile);
                else if (symbol == 'R') tilemap.SetTile(cell, rockTile);
                else if (symbol == 'M')
                {
                    mouldMin = Vector3Int.Min(mouldMin, cell);
                    mouldMax = Vector3Int.Max(mouldMax, cell);
                }
                int liquidIndex = LIQUID_CHARS.IndexOf(symbol);
                if (liquidIndex >= 0) sourceCellListArray[liquidIndex].Add(cell);
            }
        }
        Vector3 min = grid.CellToWorld(mouldMin), max = grid.CellToWorld(mouldMax + Vector3Int.one);
        mouldRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);

        for (int i = 0; i < LIQUID_COUNT; i++)
        {
            int cellCount = sourceCellListArray[i].Count;
            isLiquidUsedArray[i] = cellCount > 0;
            for (int p = 0; p < puzzleSO.particlesPerLiquid && cellCount > 0; p++)
                SpawnParticle(i, sourceCellListArray[i][p % cellCount], p / cellCount);
        }
        OnBuilt?.Invoke(this, EventArgs.Empty);
    }

    private Vector3Int LayoutToCell(Vector2Int layoutCell) => LayoutToCell(layoutCell, puzzleSO.GetRows());

    private static Vector3Int LayoutToCell(Vector2Int layoutCell, string[] rows) => new Vector3Int(layoutCell.x - rows[layoutCell.y].Length / 2, rows.Length / 2 - layoutCell.y, 0);

    // up to 4 blobs per cell on a 2x2 sub-grid, so freshly spawned blobs never overlap and explode apart
    private void SpawnParticle(int liquidIndex, Vector3Int cell, int slot)
    {
        Vector3 cellCentre = grid.GetCellCenterWorld(cell);
        Vector2 offset = new Vector2(slot % 2 == 0 ? -1f : 1f, (slot / 2) % 2 == 0 ? -1f : 1f) * grid.cellSize.x * .25f;
        GameObject particle = new GameObject("Liquid" + liquidIndex) { layer = puzzleLayer };
        particle.transform.SetParent(particleParent, false);
        particle.transform.position = cellCentre + (Vector3)offset;
        Rigidbody2D body = particle.AddComponent<Rigidbody2D>();
        body.gravityScale = 1.6f;
        body.linearDamping = .15f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D circle = particle.AddComponent<CircleCollider2D>();
        circle.radius = particleRadius;
        circle.sharedMaterial = particleMaterial;
        particleListArray[liquidIndex].Add(body);
    }

    private void TryDig(Vector2 screenPosition)
    {
        Ray ray = puzzleCamera.ScreenPointToRay(screenPosition);
        Plane plane = new Plane(Vector3.back, grid.transform.position);
        if (!plane.Raycast(ray, out float distance)) return;
        Vector3 point = ray.GetPoint(distance);
        Vector3Int centre = grid.WorldToCell(point);
        int reach = Mathf.CeilToInt(digRadius / grid.cellSize.x);
        bool hasDug = false;
        for (int x = -reach; x <= reach; x++)
        {
            for (int y = -reach; y <= reach; y++)
            {
                Vector3Int cell = centre + new Vector3Int(x, y, 0);
                if (!IsDirt(cell) || Vector2.Distance(grid.GetCellCenterWorld(cell), point) > digRadius) continue;
                tilemap.SetTile(cell, null);
                hasDug = true;
            }
        }
        if (!hasDug) return;
        hasPlayerDug = true;
        OnDug?.Invoke(this, EventArgs.Empty);
    }

    private void CountMould()
    {
        for (int i = 0; i < LIQUID_COUNT; i++)
        {
            inMouldCountArray[i] = 0;
            foreach (Rigidbody2D body in particleListArray[i])
                if (mouldRect.Contains(body.position)) inMouldCountArray[i]++;
        }
    }

    private bool IsEveryLiquidDone()
    {
        for (int i = 0; i < LIQUID_COUNT; i++)
            if (GetLiquidProgress(i) < 1f) return false;
        return true;
    }

    private void ClearParticles()
    {
        foreach (List<Rigidbody2D> particleList in particleListArray)
        {
            foreach (Rigidbody2D body in particleList) Destroy(body.gameObject);
            particleList.Clear();
        }
    }
}
