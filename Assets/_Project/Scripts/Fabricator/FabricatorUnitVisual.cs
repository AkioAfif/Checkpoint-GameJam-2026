using System;
using System.Collections.Generic;
using UnityEngine;

// Draws the board's units: the Tech Bug, Seed Embryos, Root Sentinels (thorny ones tinted crimson), Energy Cells and Moving
// Cells (Pollen Drifters and hostile Seed Burrs). Each one "breathes" (a squash-and-stretch pulse and a slow sway, random
// phase per unit). The bug glides along its slide path cell by cell so its turns at sap routers show, Moving Cells glide
// one cell per step and show their next step as a small arrow on their edge, and captured units (or a destroyed bug) shrink away.
public class FabricatorUnitVisual : MonoBehaviour
{
    private const float UNIT_SCALE = .88f;
    private const float SHRINK_DURATION = .25f;
    private const float BREATH_FREQUENCY = .6f;
    private const float BREATH_AMPLITUDE = .05f;
    private const float SWAY_FREQUENCY = .35f;
    private const float SWAY_ANGLE_MAX = 4f;
    // the next-step arrow: size and how far out from the cell's centre, in cells
    private const float INDICATOR_SCALE = .45f;
    private const float INDICATOR_DISTANCE = .5f;

    private class UnitSprite
    {
        public FabricatorBoardUnit unit;
        public SpriteRenderer spriteRenderer;
        public SpriteRenderer indicator;
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
        float cellSize = miniGame.GetCellSize();
        bool isSliding = miniGame.TryGetSlide(out FabricatorBoardUnit slidingBug, out Vector2Int startCell, out List<Vector2Int> path, out float cellsTravelled);
        bool isCellStep = miniGame.TryGetCellStep(out bool isHostileStep, out float stepProgress);
        foreach (UnitSprite unitSprite in unitSpriteList)
        {
            FabricatorBoardUnit unit = unitSprite.unit;
            Transform unitTransform = unitSprite.spriteRenderer.transform;
            bool isThisSliding = isSliding && unit == slidingBug;
            bool isThisStepping = isCellStep && unit.type == FabricatorBoardUnit.Type.MovingCell && unit.isDangerous == isHostileStep;
            if (isThisSliding) unitTransform.position = GetPathPosition(startCell, path, cellsTravelled);
            else if (isThisStepping) unitTransform.position = Vector3.Lerp(miniGame.GetCellWorldPosition(unit.previousCell), miniGame.GetCellWorldPosition(unit.cell), stepProgress);
            else unitTransform.position = miniGame.GetCellWorldPosition(unit.cell);

            // a bug sliding into a hazard shrinks on arrival; carry that on so it doesn't pop back up once it's gone
            if (isThisSliding) unitSprite.shrinkTimer = SHRINK_DURATION * (1f - miniGame.GetHazardDeathProgress());
            else if (!unit.isAlive && !isThisStepping) unitSprite.shrinkTimer = Mathf.Max(0f, unitSprite.shrinkTimer - Time.deltaTime);
            float shrink = unitSprite.shrinkTimer / SHRINK_DURATION;
            unitSprite.spriteRenderer.enabled = shrink > 0f;

            float breath = Mathf.Sin(Time.time * BREATH_FREQUENCY * Mathf.PI * 2f + unitSprite.phase) * BREATH_AMPLITUDE;
            unitTransform.localScale = new Vector3(1f + breath, 1f - breath, 1f) * cellSize * UNIT_SCALE * shrink;
            float sway = Mathf.Sin(Time.time * SWAY_FREQUENCY * Mathf.PI * 2f + unitSprite.phase * 1.7f) * SWAY_ANGLE_MAX;
            unitTransform.rotation = Quaternion.Euler(0f, 0f, sway);

            if (unitSprite.indicator != null) RefreshIndicator(unitSprite, unitTransform.position, cellSize, isThisStepping);
        }
    }

    private void MiniGame_OnBuilt(object sender, EventArgs e)
    {
        DestroyUnits();
        unitRoot = new GameObject("Units") { layer = gameObject.layer }.transform;
        unitRoot.SetParent(transform, false);
        foreach (FabricatorBoardUnit unit in miniGame.GetBoard().GetUnitList())
        {
            bool isThornySentinel = unit.type == FabricatorBoardUnit.Type.Sentinel && unit.isDangerous;
            SpriteRenderer spriteRenderer = FabricatorSpriteFactory.Create(unit.type.ToString(), unitRoot, GetSprite(unit), skinSO.spriteMaterial, FabricatorSpriteFactory.UNIT_ORDER, isThornySentinel ? skinSO.thornySentinelColor : Color.white);
            UnitSprite unitSprite = new UnitSprite { unit = unit, spriteRenderer = spriteRenderer, phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f) };
            if (unit.type == FabricatorBoardUnit.Type.MovingCell)
                unitSprite.indicator = FabricatorSpriteFactory.Create("NextStep", unitRoot, skinSO.arrowSprite, skinSO.spriteMaterial, FabricatorSpriteFactory.UNIT_ORDER + 1, skinSO.stepIndicatorColor);
            unitSpriteList.Add(unitSprite);
        }
    }

    private void MiniGame_OnEnded(object sender, EventArgs e) => DestroyUnits();

    // on the edge the cell will step toward next; hidden while it steps, when it waits, and once it's gone
    private void RefreshIndicator(UnitSprite unitSprite, Vector3 unitPosition, float cellSize, bool isStepping)
    {
        Vector2Int step = unitSprite.unit.GetNextStep();
        bool isShown = unitSprite.unit.isAlive && !isStepping && step != Vector2Int.zero;
        unitSprite.indicator.enabled = isShown;
        if (!isShown) return;
        Vector2 direction = ((Vector2)step).normalized;
        unitSprite.indicator.transform.position = unitPosition + (Vector3)(direction * INDICATOR_DISTANCE * cellSize);
        unitSprite.indicator.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        unitSprite.indicator.transform.localScale = Vector3.one * INDICATOR_SCALE * cellSize;
    }

    private Sprite GetSprite(FabricatorBoardUnit unit)
    {
        switch (unit.type)
        {
            case FabricatorBoardUnit.Type.Bug: return skinSO.bugSprite;
            case FabricatorBoardUnit.Type.Embryo: return skinSO.embryoSprite;
            case FabricatorBoardUnit.Type.EnergyCell: return skinSO.energyCellSprite;
            case FabricatorBoardUnit.Type.MovingCell: return unit.isDangerous ? skinSO.hostileMovingCellSprite : skinSO.movingCellSprite;
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
