using UnityEditor;
using UnityEngine;

// Tools > FixScene > Create World Border Layout: writes the CozyFarm map's border layout and hooks it to the open scene's
// WorldBorderDressing. Re-runnable: it rewrites the asset in place (same GUID), so tune the layout here.
// The map (ground x -70..60, z -50..50; walls 1.5 m inside): village west, forest east, the river along the north and down the
// middle. The gameplay camera sits 10.5 m north of the player looking south, so from it:
//   south = far edge, dense forest (seen most)      east = left, rocky forest edge with a closed gate on the road
//   west  = right, village hedge with an open gate  north = near edge, low riverbank only (the camera stands over it)
public static class FixSceneBorderLayoutBuilder
{
    private const string LAYOUT_NAME = "FixScene_WorldBorderLayout";
    private const string LAYOUT_PATH = WorldBorderAssetGenerator.STYLE_FOLDER + "/" + LAYOUT_NAME + ".asset";
    private const string MAP_MODEL_PATH = "Assets/Ilkham/scene/fix.glb";

    private const float WEST_WALL = -68.5f;
    private const float EAST_WALL = 58.5f;
    private const float SOUTH_WALL = -48.5f;
    private const float NORTH_WALL = 48.5f;
    // strips run past the corners so the corner squares are covered from both sides
    private const float RUN_OVER = 20f;

    [MenuItem("Tools/FixScene/Create World Border Layout")]
    public static void CreateLayout()
    {
        BorderStyleSO denseForest = Style("BorderStyle_DenseForest");
        BorderStyleSO rockyEdge = Style("BorderStyle_RockyEdge");
        BorderStyleSO hedge = Style("BorderStyle_Hedge");
        BorderStyleSO shrubs = Style("BorderStyle_Shrubs");
        BorderStyleSO riverbank = Style("BorderStyle_Riverbank");
        BorderStyleSO reeds = Style("BorderStyle_Reeds");
        BorderStyleSO background = Style("BorderStyle_BackgroundForest");
        BorderStyleSO gateOpen = Style("BorderStyle_GateOpen");
        BorderStyleSO gateClosed = Style("BorderStyle_GateClosed");
        if (denseForest == null || rockyEdge == null || hedge == null || shrubs == null || riverbank == null || reeds == null || background == null || gateOpen == null || gateClosed == null)
        {
            Debug.LogError("FixSceneBorderLayoutBuilder: border styles missing. Run Tools > World Border > Generate Border Assets first.");
            return;
        }

        WorldBorderLayoutSO layout = ScriptableObject.CreateInstance<WorldBorderLayoutSO>();
        layout.name = LAYOUT_NAME;
        layout.groundObjectName = "Terrain";
        layout.groundMaterial = WorldBorderAssetGenerator.LoadSubAsset<Material>(MAP_MODEL_PATH, "M_Terrain");
        // the ground mesh's own (linear) vertex colours at its west/south and east/north edges
        layout.farGrassColor = new Color(.2f, .4f, .09f).gamma;
        layout.hillColor = new Color(.13f, .33f, .07f).gamma;
        layout.roadMaterial = WorldBorderAssetGenerator.LoadSubAsset<Material>(MAP_MODEL_PATH, "M_Path");
        layout.roadEdgeMaterial = WorldBorderAssetGenerator.LoadSubAsset<Material>(MAP_MODEL_PATH, "M_Path_Edge");
        layout.riverObjectName = "River_Water";
        layout.riverColumnCount = 11;
        layout.riverExtension = 60f;
        layout.riverMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/RiverWater.mat");
        layout.riverBankStyleSO = reeds;

        float west = WEST_WALL;
        float east = EAST_WALL;
        float south = SOUTH_WALL;
        float north = NORTH_WALL;
        layout.stripArray = new[] {
            // south: the far edge on screen, the deepest forest
            Strip("South_Forest", denseForest, west - RUN_OVER, south, east + RUN_OVER, south, .6f, 13f, 101),
            Strip("South_Background", background, west - RUN_OVER, south, east + RUN_OVER, south, 13f, 34f, 102),
            // east: rocks and low cliffs along the wall, forest behind, a closed gate where the forest road leaves
            Strip("East_Rocks", rockyEdge, east, south - RUN_OVER, east, north + RUN_OVER, .6f, 4.5f, 201),
            Strip("East_Forest", denseForest, east, south - RUN_OVER, east, north + RUN_OVER, 4.5f, 15f, 202),
            Strip("East_Background", background, east, south - RUN_OVER, east, north + RUN_OVER, 15f, 34f, 203),
            Strip("East_Gate", gateClosed, 59.6f, -6.75f, 59.6f, -1.15f, 0f, 0f, 204),
            // west: village hedge and outskirts woods, an open gate where the truck comes in (it runs toward -z so the
            // leaves swing outward, to -x)
            Strip("West_Hedge", hedge, west, south - RUN_OVER, west, north + RUN_OVER, .6f, 6.5f, 301),
            Strip("West_Forest", denseForest, west, south - RUN_OVER, west, north + RUN_OVER, 6.5f, 15f, 302),
            Strip("West_Background", background, west, south - RUN_OVER, west, north + RUN_OVER, 15f, 34f, 303),
            Strip("West_Gate", gateOpen, -69.7f, 18.8f, -69.7f, 13.2f, 0f, 0f, 304),
            // north: the camera stands over this edge when the player is up there (at most 10.5 m past the wall) and sees
            // only the first ~5 m of it, so a dense rocky bank there, low shrubs under the camera, trees only behind it
            Strip("North_Bank", riverbank, west - RUN_OVER, north, east + RUN_OVER, north, .6f, 5f, 401),
            Strip("North_Shrubs", shrubs,west - RUN_OVER, north, east + RUN_OVER, north, 5f, 11.5f, 402),
            Strip("North_Forest", denseForest, west - RUN_OVER, north, east + RUN_OVER, north, 11.5f, 22f, 403),
            Strip("North_Background", background, west - RUN_OVER, north, east + RUN_OVER, north, 22f, 38f, 404),
        };
        layout.roadArray = new[] {
            new WorldBorderLayoutSO.Road { label = "West_Road", start = new Vector2(-70f, 15.95f), direction = Vector2.left, width = 3.2f, length = 45f },
            new WorldBorderLayoutSO.Road { label = "East_Road", start = new Vector2(60f, -3.95f), direction = Vector2.right, width = 3f, length = 32f },
        };

        layout.mountainPrefabArray = new[] {
            WorldBorderAssetGenerator.LoadPiece("Mountain_A"), WorldBorderAssetGenerator.LoadPiece("Mountain_B"),
            WorldBorderAssetGenerator.LoadPiece("Mountain_C"), WorldBorderAssetGenerator.LoadPiece("Mountain_D"),
        };
        layout.mountainCount = 22;
        layout.mountainDistanceRange = new Vector2(22f, 40f);
        layout.mountainWidthRange = new Vector2(28f, 45f);
        layout.mountainHeightRange = new Vector2(20f, 38f);
        layout.hazeColor = new Color(.8f, .88f, .93f); // the Main Camera's background
        // the map itself stays clear; only the far hills and mountains soften
        layout.hazeStart = 90f;
        layout.hazeEnd = 260f;

        WorldBorderAssetGenerator.EnsureFolder(WorldBorderAssetGenerator.STYLE_FOLDER);
        WorldBorderLayoutSO existing = AssetDatabase.LoadAssetAtPath<WorldBorderLayoutSO>(LAYOUT_PATH);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(layout, LAYOUT_PATH);
            existing = layout;
        }
        else
        {
            EditorUtility.CopySerialized(layout, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(layout);
        }
        AssetDatabase.SaveAssets();

        WorldBorderDressing dressing = Object.FindAnyObjectByType<WorldBorderDressing>();
        if (dressing == null)
        {
            Debug.LogWarning("FixSceneBorderLayoutBuilder: layout saved; no WorldBorderDressing in the open scene to assign it to");
            return;
        }
        SerializedObject serializedDressing = new SerializedObject(dressing);
        serializedDressing.FindProperty("worldBorderLayoutSO").objectReferenceValue = existing;
        serializedDressing.ApplyModifiedProperties();
        Debug.Log($"FixSceneBorderLayoutBuilder: layout saved to {LAYOUT_PATH} and assigned to {dressing.name}");
    }

    private static BorderStyleSO Style(string name) => WorldBorderAssetGenerator.LoadStyle(name);

    private static WorldBorderLayoutSO.Strip Strip(string label, BorderStyleSO style, float startX, float startZ, float endX, float endZ, float nearOffset, float farOffset, int seed) =>
        new WorldBorderLayoutSO.Strip {
            label = label, borderStyleSO = style, start = new Vector2(startX, startZ), end = new Vector2(endX, endZ),
            nearOffset = nearOffset, farOffset = farOffset, seed = seed,
        };
}
