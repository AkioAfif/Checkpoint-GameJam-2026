using System;
using System.Collections.Generic;
using UnityEngine;

// Draws the board's units: the Tech Bug, Seed Embryos and Root Sentinels (thorny ones tinted crimson). Each one "breathes"
// (a squash-and-stretch pulse and a slow sway, random phase per unit), the bug glides along its slide path cell by cell
// so its turns at sap routers show, and captured units (or a bug lost in thorns) shrink away.
public class FabricatorUnitVisual : MonoBehaviour
{
    private const float UNIT_SCALE = .88f;
    private const float SHRINK_DURATION = .25f;
    private const float BREATH_FREQUENCY = .6f;
    private const float BREATH_AMPLITUDE = .05f;
    private const float SWAY_FREQUENCY = .35f;
    private const float SWAY_ANGLE_MAX = 4f;

    private class UnitSprite
    {
        public FabricatorBoardUnit unit;
        public SpriteRenderer spriteRenderer;
        public float phase;
        // counts down once the unit is gone
        public float shrinkTimer = SHRINK_DURATION;
    }

    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private FabricatorBoardSkinSO skinSO;

    private Transform unitRoot;
    private readonly List<UnitSprite> unitSpriteList = new List<UnitSprite>();

    private void Start()
    {
        miniGame.OnBuilt += MiniGame_OnBuilt;
        miniGame.OnEnded += MiniGame_OnEnded;
    }

    private void Update()
    {
        if (unitRoot == null) return;
        float baseScale = miniGame.GetCellSize() * UNIT_SCALE;
        bool isSliding = miniGame.TryGetSlide(out FabricatorBoardUnit slidingBug, out Vector2Int startCell, out List<Vector2Int> path, out float cellsTravelled);
        foreach (UnitSprite unitSprite in unitSpriteList)
        {
            Transform unitTransform = unitSprite.spriteRenderer.transform;
            bool isThisSliding = isSliding && unitSprite.unit == slidingBug;
            unitTransform.position = isThisSliding ? GetPathPosition(startCell, path, cellsTravelled) : miniGame.GetCellWorldPosition(unitSprite.unit.cell);

            float shrink = 1f;
            if (isThisSliding) shrink = 1f - miniGame.GetHazardDeathProgress();
            else if (!unitSprite.unit.isAlive)
            {
                // a bug only dies in thorns, and it already shrank on its way in
                if (unitSprite.unit.type == FabricatorBoardUnit.Type.Bug) unitSprite.shrinkTimer = 0f;
                unitSprite.shrinkTimer = Mathf.Max(0f, unitSprite.shrinkTimer - Time.deltaTime);
                shrink = unitSprite.shrinkTimer / SHRINK_DURATION;
            }
            unitSprite.spriteRenderer.enabled = shrink > 0f;

            float breath = Mathf.Sin(Time.time * BREATH_FREQUENCY * Mathf.PI * 2f + unitSprite.phase) * BREATH_AMPLITUDE;
            unitTransform.localScale = new Vector3(1f + breath, 1f - breath, 1f) * baseScale * shrink;
            float sway = Mathf.Sin(Time.time * SWAY_FREQUENCY * Mathf.PI * 2f + unitSprite.phase * 1.7f) * SWAY_ANGLE_MAX;
            unitTransform.rotation = Quaternion.Euler(0f, 0f, sway);
        }
    }

    private void MiniGame_OnBuilt(object sender, EventArgs e)
    {
        DestroyUnits();
        unitRoot = new GameObject("Units") { layer = gameObject.layer }.transform;
        unitRoot.SetParent(transform, false);
        foreach (FabricatorBoardUnit unit in miniGame.GetBoard().GetUnitList())
        {
            SpriteRenderer spriteRenderer = FabricatorSpriteFactory.Create(unit.type.ToString(), unitRoot, GetSprite(unit), skinSO.spriteMaterial, FabricatorSpriteFactory.UNIT_ORDER, unit.isThorny ? skinSO.thornySentinelColor : Color.white);
            unitSpriteList.Add(new UnitSprite { unit = unit, spriteRenderer = spriteRenderer, phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f) });
        }
    }

    private void MiniGame_OnEnded(object sender, EventArgs e) => DestroyUnits();

    private Sprite GetSprite(FabricatorBoardUnit unit)
    {
        switch (unit.type)
        {
            case FabricatorBoardUnit.Type.Bug: return skinSO.bugSprite;
            case FabricatorBoardUnit.Type.Embryo: return skinSO.embryoSprite;
            default: return skinSO.sentinelSprite;
        }
    }

    private Vector3 GetPathPosition(Vector2Int startCell, List<Vector2Int> path, float cellsTravelled)
    {
        int index = Mathf.Min(Mathf.FloorToInt(cellsTravelled), path.Count - 1);
        Vector3 from = miniGame.GetCellWorldPosition(index == 0 ? startCell : path[index - 1]);
        Vector3 to = miniGame.GetCellWorldPosition(path[index]);
        return Vector3.Lerp(from, to, cellsTravelled - index);
    }

    private void DestroyUnits()
    {
        unitSpriteList.Clear();
        if (unitRoot != null) Destroy(unitRoot.gameObject);
        unitRoot = null;
    }
}
