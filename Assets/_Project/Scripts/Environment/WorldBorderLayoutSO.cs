using System;
using UnityEngine;

// Where one scene's border dressing goes: strips of BorderStyleSO looks along the edges, roads that lead off the map,
// the ground skirt and hills past the map's edge, the distant mountains and the haze.
// Coordinates are XZ in the WorldBorder object's space (our scenes keep it at the origin).
[CreateAssetMenu(menuName = "World Border/Layout")]
public class WorldBorderLayoutSO : ScriptableObject
{
    [Serializable]
    public class Strip
    {
        public string label;
        public BorderStyleSO borderStyleSO;
        public Vector2 start;
        public Vector2 end;
        [Tooltip("Band across the strip, in metres outward from the line (away from the playable area).")]
        public float nearOffset = .6f;
        public float farOffset = 8f;
        public int seed;
    }

    [Serializable]
    public class Road
    {
        public string label;
        [Tooltip("Where the road leaves the ground mesh.")]
        public Vector2 start;
        public Vector2 direction = Vector2.left;
        public float width = 3.2f;
        public float length = 40f;
    }

    [Header("Ground skirt")]
    [Tooltip("The playable ground mesh (needs a MeshCollider). The skirt starts at its edge.")]
    public string groundObjectName = "Terrain";
    [Tooltip("A vertex-coloured material, like the ground's.")]
    public Material groundMaterial;
    public float skirtWidth = 70f;
    public float skirtCellSize = 2.5f;
    [Tooltip("Colours as picked (converted to linear vertex colours). Right at the edge the skirt takes the ground's own colour.")]
    public Color farGrassColor = new Color(.48f, .66f, .33f);
    public Color hillColor = new Color(.42f, .62f, .3f);
    public Color bankColor = new Color(.7f, .62f, .45f);
    public float hillHeight = 7f;
    [Tooltip("Metres past the ground's edge before the hills rise. Keep it beyond what the gameplay camera sees.")]
    public float hillStartDistance = 16f;

    [Header("Roads")]
    [Tooltip("The map's own path materials, so a road carries on past the edge unchanged.")]
    public Material roadMaterial;
    public Material roadEdgeMaterial;

    [Header("River")]
    [Tooltip("A strip mesh river whose ends reach the ground's edge get extended past it. Empty = no river.")]
    public string riverObjectName = "River_Water";
    public int riverColumnCount = 11;
    public float riverExtension = 60f;
    public Material riverMaterial;
    [Tooltip("Dressing along both banks of each river extension.")]
    public BorderStyleSO riverBankStyleSO;

    [Header("Strips and roads")]
    public Strip[] stripArray = new Strip[0];
    public Road[] roadArray = new Road[0];
    [Tooltip("Ground hits on objects whose name contains one of these are kept clear (unless the style allows water).")]
    public string[] blockedSurfaceNameArray = { "Path", "River", "Bridge" };

    [Header("Mountains")]
    public Transform[] mountainPrefabArray = new Transform[0];
    public int mountainCount = 18;
    [Tooltip("Metres past the ground's edge.")]
    public Vector2 mountainDistanceRange = new Vector2(50f, 85f);
    public Vector2 mountainWidthRange = new Vector2(40f, 70f);
    public Vector2 mountainHeightRange = new Vector2(22f, 46f);
    public int mountainSeed = 7;

    [Header("Haze")]
    public bool applyHaze = true;
    public Color hazeColor = new Color(.8f, .88f, .93f);
    public float hazeStart = 60f;
    public float hazeEnd = 190f;
}
