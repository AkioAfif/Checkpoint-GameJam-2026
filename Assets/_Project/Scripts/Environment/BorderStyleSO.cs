using System;
using UnityEngine;

// One look for a stretch of world border (dense forest, rocky edge, hedge, fence line, gate...).
// Strips in a WorldBorderLayoutSO point at these, so every scene shares the same looks.
[CreateAssetMenu(menuName = "World Border/Border Style")]
public class BorderStyleSO : ScriptableObject
{
    public enum Placement { Scatter, Line }

    [Serializable]
    public struct Piece
    {
        public Transform prefab;
        [Min(0f)] public float weight;
        public Vector2 scaleRange;
        [Tooltip("Footprint radius at scale 1. Keeps the piece past the strip's near edge and off existing scene objects.")]
        public float radius;
    }

    public Placement placement = Placement.Scatter;
    public Piece[] pieceArray = new Piece[0];
    [Tooltip("Scatter: grid cell size (one piece per cell). Line: distance between pieces.")]
    public float spacing = 3f;
    [Tooltip("Scatter only: random offset inside each cell, as a share of the spacing.")]
    [Range(0f, .5f)] public float jitter = .4f;
    [Tooltip("Scatter only: share of cells left empty.")]
    [Range(0f, 1f)] public float emptyChance;
    [Tooltip("How far each piece sinks into the ground (m at scale 1), so bases never float on slopes.")]
    public float sinkDepth = .1f;
    [Tooltip("Random lean in degrees (rocks).")]
    public float maxTilt;
    [Tooltip("Face along the strip (Line always does). Scatter adds ±yawJitter.")]
    public bool alignToStrip;
    public float yawJitter;
    [Tooltip("Line only: stretch each piece along the strip so the line has no gaps (fences, gates).")]
    public bool stretchToFit;
    [Tooltip("Line only: the length one piece covers at scale 1 (fence segment, gate).")]
    public float pieceLength = 2.4f;
    [Tooltip("Line only: placed once at the far end of the strip (the last fence post).")]
    public Transform endCapPrefab;
    [Tooltip("May stand in river water (reeds).")]
    public bool allowInWater;
}
