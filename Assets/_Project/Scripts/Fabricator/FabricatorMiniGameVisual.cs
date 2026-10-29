using System;
using System.Collections.Generic;
using UnityEngine;

// Draws the Fabricator board with the Sawit-Fabricator sprites: the 9-sliced machine frame, the tiles (gates, sap routers and
// injector glands show their current state), the move arrows around the selected bug, and the hover/selection outlines.
// Units, roots and mist have their own visuals.
public class FabricatorMiniGameVisual : MonoBehaviour
{
    private const float GLYPH_SCALE = .6f;
    // the move_arrow art has padding around it, so this draws it at about half a cell
    private const float ARROW_SCALE = .8f;

    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private FabricatorBoardSkinSO skinSO;

    private Transform boardRoot;
    private readonly Dictionary<FabricatorBoardTile, SpriteRenderer> glyphDictionary = new Dictionary<FabricatorBoardTile, SpriteRenderer>();
    private readonly List<SpriteRenderer> arrowList = new List<SpriteRenderer>();
    private SpriteRenderer hoverOutline;
    private SpriteRenderer selectionOutline;
    private Sprite outlineSprite;

    private void Awake()
    {
        outlineSprite = CreateOutlineSprite();
    }

    private void Start()
    {
        miniGame.OnBuilt += MiniGame_OnBuilt;
        miniGame.OnBoardChanged += MiniGame_OnBoardChanged;
        miniGame.OnSlideStarted += MiniGame_OnBoardChanged;
        miniGame.OnSolved += MiniGame_OnBoardChanged;
        miniGame.OnLost += MiniGame_OnBoardChanged;
        miniGame.OnEnded += MiniGame_OnEnded;
    }

    private void Update()
    {
        if (boardRoot == null) return;
        Vector2Int cell = default;
        bool isHovering = miniGame.IsPlaying() && miniGame.TryGetPointerCell(out cell) && IsClickable(cell);
        hoverOutline.enabled = isHovering;
        if (isHovering) hoverOutline.transform.position = miniGame.GetCellWorldPosition(cell);
        RefreshArrowHover();
    }

    private void MiniGame_OnBuilt(object sender, EventArgs e)
    {
        DestroyBoard();
        boardRoot = new GameObject("Board") { layer = gameObject.layer }.transform;
        boardRoot.SetParent(transform, false);
        float cellSize = miniGame.GetCellSize();
        BuildFrame(cellSize);
        foreach (FabricatorBoardTile tile in miniGame.GetBoard().GetTiles()) BuildTile(tile, cellSize);
        hoverOutline = CreateCellSprite("HoverOutline", outlineSprite, FabricatorSpriteFactory.HIGHLIGHT_ORDER, skinSO.hoverColor, cellSize);
        selectionOutline = CreateCellSprite("SelectionOutline", outlineSprite, FabricatorSpriteFactory.HIGHLIGHT_ORDER, skinSO.selectedColor, cellSize);
        Refresh();
    }

    private void MiniGame_OnBoardChanged(object sender, EventArgs e)
    {
        if (boardRoot != null) Refresh();
    }

    private void MiniGame_OnEnded(object sender, EventArgs e) => DestroyBoard();

    private void Refresh()
    {
        foreach (KeyValuePair<FabricatorBoardTile, SpriteRenderer> pair in glyphDictionary) RefreshGlyph(pair.Key, pair.Value);

        bool hasSelection = miniGame.TryGetSelectedBug(out FabricatorBoardUnit bug);
        selectionOutline.enabled = hasSelection;
        if (hasSelection) selectionOutline.transform.position = miniGame.GetCellWorldPosition(bug.cell);

        foreach (SpriteRenderer arrow in arrowList) Destroy(arrow.gameObject);
        arrowList.Clear();
        if (!hasSelection) return;
        foreach (FabricatorMiniGame.Move move in miniGame.GetMoveList())
        {
            SpriteRenderer arrow = CreateCellSprite("Arrow", skinSO.arrowSprite, FabricatorSpriteFactory.ARROW_ORDER, GetArrowColor(move, false), miniGame.GetCellSize() * ARROW_SCALE);
            arrow.transform.position = miniGame.GetCellWorldPosition(bug.cell + move.direction);
            arrow.transform.rotation = Quaternion.Euler(0f, 0f, FabricatorSpriteFactory.GetAngle(move.direction));
            arrowList.Add(arrow);
        }
    }

    private void RefreshArrowHover()
    {
        bool hasPointerCell = miniGame.TryGetPointerCell(out Vector2Int pointerCell);
        if (!miniGame.TryGetSelectedBug(out FabricatorBoardUnit bug)) return;
        List<FabricatorMiniGame.Move> moveList = miniGame.GetMoveList();
        for (int i = 0; i < arrowList.Count && i < moveList.Count; i++)
            arrowList[i].color = GetArrowColor(moveList[i], hasPointerCell && bug.cell + moveList[i].direction == pointerCell);
    }

    private void BuildFrame(float cellSize)
    {
        Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue), max = new Vector2Int(int.MinValue, int.MinValue);
        foreach (FabricatorBoardTile tile in miniGame.GetBoard().GetTiles())
        {
            min = Vector2Int.Min(min, tile.cell);
            max = Vector2Int.Max(max, tile.cell);
        }
        SpriteRenderer frame = FabricatorSpriteFactory.Create("Frame", boardRoot, skinSO.frameSprite, skinSO.spriteMaterial, FabricatorSpriteFactory.FRAME_ORDER, Color.white);
        frame.drawMode = SpriteDrawMode.Sliced;
        // the sprite's 9-slice border lies outside the board, so its inner edge hugs the tiles
        Vector4 border = skinSO.frameSprite.border / skinSO.frameSprite.pixelsPerUnit;
        frame.size = new Vector2(max.x - min.x + 1 + border.x + border.z, max.y - min.y + 1 + border.y + border.w);
        frame.transform.localScale = Vector3.one * cellSize;
        frame.transform.position = (miniGame.GetCellWorldPosition(min) + miniGame.GetCellWorldPosition(max)) * .5f;
    }

    private void BuildTile(FabricatorBoardTile tile, float cellSize)
    {
        bool isOffset = (tile.cell.x + tile.cell.y) % 2 != 0;
        Color floorColor = isOffset ? skinSO.floorOffsetColor : skinSO.floorColor;
        SpriteRenderer floor = CreateCellSprite("Tile", GetTileSprite(tile), FabricatorSpriteFactory.TILE_ORDER, tile.type == FabricatorBoardTile.Type.Floor ? floorColor : Color.white, cellSize);
        floor.transform.position = miniGame.GetCellWorldPosition(tile.cell);

        float glyphScale = cellSize;
        switch (tile.type)
        {
            case FabricatorBoardTile.Type.Router:
                SpriteRenderer node = CreateCellSprite("RouterNode", skinSO.routerNodeSprite, FabricatorSpriteFactory.TILE_GLYPH_ORDER, Color.white, cellSize);
                node.transform.position = floor.transform.position;
                glyphScale = cellSize * GLYPH_SCALE;
                break;
            case FabricatorBoardTile.Type.Gate:
            case FabricatorBoardTile.Type.Injector:
            case FabricatorBoardTile.Type.AreaInjector:
                break;
            default:
                return;
        }
        // drawn just above the router node
        SpriteRenderer glyph = CreateCellSprite("Glyph", null, FabricatorSpriteFactory.TILE_GLYPH_ORDER + (tile.type == FabricatorBoardTile.Type.Router ? 1 : 0), Color.white, glyphScale);
        glyph.transform.position = floor.transform.position;
        glyphDictionary[tile] = glyph;
    }

    private void RefreshGlyph(FabricatorBoardTile tile, SpriteRenderer glyph)
    {
        switch (tile.type)
        {
            case FabricatorBoardTile.Type.Gate:
                glyph.sprite = tile.isGateOpen ? skinSO.gateOpenSprite : skinSO.gateClosedSprite;
                break;
            case FabricatorBoardTile.Type.Router:
                glyph.sprite = skinSO.routerArrowSprite;
                glyph.transform.rotation = Quaternion.Euler(0f, 0f, tile.routerAngle);
                break;
            default:
                glyph.sprite = tile.IsInjectorActive() ? skinSO.injectorReleasedSprite : skinSO.injectorIdleSprite;
                glyph.color = tile.type == FabricatorBoardTile.Type.AreaInjector ? skinSO.areaInjectorColor : Color.white;
                break;
        }
    }

    // walls and resin snares are whole-tile art; everything else stands on the matrix floor
    private Sprite GetTileSprite(FabricatorBoardTile tile)
    {
        switch (tile.type)
        {
            case FabricatorBoardTile.Type.Wall: return skinSO.wallSprite;
            case FabricatorBoardTile.Type.Snare: return skinSO.snareSprite;
            default: return skinSO.floorSprite;
        }
    }

    // what a click on this cell would do something with: the bug, an arrow, or a clickable tile
    private bool IsClickable(Vector2Int cell)
    {
        FabricatorBoard board = miniGame.GetBoard();
        if (board.TryGetUnit(cell, out FabricatorBoardUnit unit) && unit.type == FabricatorBoardUnit.Type.Bug) return true;
        if (miniGame.TryGetSelectedBug(out _) && miniGame.TryGetMoveTo(cell, out _)) return true;
        return board.TryGetTile(cell, out FabricatorBoardTile tile) && board.CanInteract(tile);
    }

    private Color GetArrowColor(FabricatorMiniGame.Move move, bool isHovered)
    {
        if (isHovered) return skinSO.hoverColor;
        return move.endsInHazard ? skinSO.dangerArrowColor : skinSO.arrowColor;
    }

    private SpriteRenderer CreateCellSprite(string name, Sprite sprite, int sortingOrder, Color color, float scale)
    {
        SpriteRenderer spriteRenderer = FabricatorSpriteFactory.Create(name, boardRoot, sprite, skinSO.spriteMaterial, sortingOrder, color);
        spriteRenderer.transform.localScale = Vector3.one * scale;
        return spriteRenderer;
    }

    private void DestroyBoard()
    {
        glyphDictionary.Clear();
        arrowList.Clear();
        if (boardRoot != null) Destroy(boardRoot.gameObject);
        boardRoot = null;
    }

    // a hollow 1-unit square for the hover and selection outlines
    private static Sprite CreateOutlineSprite()
    {
        const int size = 32;
        const int border = 3;
        Color32[] pixelArray = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < border || y < border || x >= size - border || y >= size - border;
                pixelArray[y * size + x] = isBorder ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        }
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixelArray);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
    }
}
