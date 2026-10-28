using System;
using System.Collections.Generic;
using UnityEngine;

// While an injector gland's effect runs, a faint pulsing mist hangs over the cells it reaches: the 3x3 around an area gland,
// otherwise each retracted Root Sentinel and its root cells. Mist fades in and out instead of popping.
public class FabricatorMistVisual : MonoBehaviour
{
    private const float FADE_SPEED = 1.5f;
    private const float PULSE_SPEED = 1.4f;
    // bigger than a cell so neighbouring puffs merge into one cloud
    private const float MIST_SCALE = 1.35f;

    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private FabricatorBoardSkinSO skinSO;

    private Transform mistRoot;
    private readonly Dictionary<Vector2Int, SpriteRenderer> mistDictionary = new Dictionary<Vector2Int, SpriteRenderer>();
    private readonly HashSet<Vector2Int> wantedCellSet = new HashSet<Vector2Int>();
    private readonly List<Vector2Int> fadedCellList = new List<Vector2Int>();

    private void Start()
    {
        miniGame.OnBuilt += MiniGame_OnBuilt;
        miniGame.OnEnded += MiniGame_OnEnded;
    }

    private void Update()
    {
        if (mistRoot == null) return;
        CollectWantedCells();
        foreach (Vector2Int cell in wantedCellSet)
        {
            if (mistDictionary.ContainsKey(cell)) continue;
            SpriteRenderer mist = FabricatorSpriteFactory.Create("Mist", mistRoot, skinSO.mistSprite, skinSO.spriteMaterial, FabricatorSpriteFactory.MIST_ORDER, WithAlpha(skinSO.mistColor, 0f));
            mist.transform.position = miniGame.GetCellWorldPosition(cell);
            mist.transform.localScale = Vector3.one * miniGame.GetCellSize() * MIST_SCALE;
            mistDictionary[cell] = mist;
        }

        fadedCellList.Clear();
        foreach (KeyValuePair<Vector2Int, SpriteRenderer> pair in mistDictionary)
        {
            // the pulse phase follows the cell, so neighbouring puffs don't breathe in unison
            float pulse = .75f + .25f * Mathf.Sin(Time.time * PULSE_SPEED + pair.Key.x * 1.3f + pair.Key.y * 2.1f);
            float targetAlpha = wantedCellSet.Contains(pair.Key) ? skinSO.mistAlpha * pulse : 0f;
            float alpha = Mathf.MoveTowards(pair.Value.color.a, targetAlpha, FADE_SPEED * Time.deltaTime);
            pair.Value.color = WithAlpha(skinSO.mistColor, alpha);
            if (alpha <= 0f && targetAlpha <= 0f) fadedCellList.Add(pair.Key);
        }
        foreach (Vector2Int cell in fadedCellList)
        {
            Destroy(mistDictionary[cell].gameObject);
            mistDictionary.Remove(cell);
        }
    }

    private void MiniGame_OnBuilt(object sender, EventArgs e)
    {
        DestroyMist();
        mistRoot = new GameObject("Mist") { layer = gameObject.layer }.transform;
        mistRoot.SetParent(transform, false);
    }

    private void MiniGame_OnEnded(object sender, EventArgs e) => DestroyMist();

    private void CollectWantedCells()
    {
        wantedCellSet.Clear();
        FabricatorBoard board = miniGame.GetBoard();
        foreach (FabricatorBoardTile tile in board.GetTiles())
        {
            if (!tile.IsInjector() || !tile.IsInjectorActive()) continue;
            if (tile.type == FabricatorBoardTile.Type.AreaInjector)
            {
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        if (board.TryGetTile(tile.cell + new Vector2Int(x, y), out _)) wantedCellSet.Add(tile.cell + new Vector2Int(x, y));
                continue;
            }
            foreach (FabricatorBoardUnit target in tile.injectorTargetList)
            {
                if (!target.isAlive) continue;
                wantedCellSet.Add(target.cell);
                wantedCellSet.UnionWith(target.rootCellList);
            }
        }
    }

    private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

    private void DestroyMist()
    {
        mistDictionary.Clear();
        if (mistRoot != null) Destroy(mistRoot.gameObject);
        mistRoot = null;
    }
}
