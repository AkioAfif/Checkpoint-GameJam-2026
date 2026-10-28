using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Tools > World Border > Generate Border Assets: saves the WorldBorderMeshFactory pieces as meshes + prefabs and creates the
// default BorderStyleSO looks from them and Ilkham's trees and bushes. Re-runnable: meshes are refreshed in place (prefabs keep
// their GUIDs); a style that already exists is left alone, so tuned values survive. Delete a style asset to reset it.
public static class WorldBorderAssetGenerator
{
    public const string STYLE_FOLDER = "Assets/_Project/ScriptableObjects/WorldBorder";
    public const string PREFAB_FOLDER = "Assets/_Project/Prefabs/Environment/WorldBorder";
    public const string GENERATED_MESH_FOLDER = MESH_FOLDER + "/Generated";

    private const string MESH_FOLDER = "Assets/_Project/Art/Models/WorldBorder";
    private const string MAP_MODEL_PATH = "Assets/Ilkham/scene/fix.glb";
    private const string FENCE_MODEL_PATH = "Assets/Ilkham/models/fences/fences.glb";
    private const string MODEL_FOLDER = "Assets/Ilkham/models/";

    [MenuItem("Tools/World Border/Generate Border Assets")]
    public static void Generate()
    {
        EnsureFolder(MESH_FOLDER);
        EnsureFolder(GENERATED_MESH_FOLDER);
        EnsureFolder(PREFAB_FOLDER);
        EnsureFolder(STYLE_FOLDER);

        Material ground = LoadSubAsset<Material>(MAP_MODEL_PATH, "M_Terrain");
        Material woodDark = LoadSubAsset<Material>(FENCE_MODEL_PATH, "M_Wood_Dark");
        Material wood = LoadSubAsset<Material>(FENCE_MODEL_PATH, "M_Wood");
        if (ground == null || wood == null || woodDark == null)
        {
            Debug.LogError("WorldBorderAssetGenerator: M_Terrain (fix.glb) or M_Wood/M_Wood_Dark (fences.glb) not found");
            return;
        }
        Material[] groundOnly = { ground };
        Material[] woodPair = { woodDark, wood };

        Transform rockA = SavePiece("Rock_A", WorldBorderMeshFactory.Rock("Rock_A", 11, new Vector3(1f, .7f, 1f), .18f, .6f), groundOnly, true);
        Transform rockB = SavePiece("Rock_B", WorldBorderMeshFactory.Rock("Rock_B", 23, new Vector3(1.2f, .6f, .9f), .2f, .5f), groundOnly, true);
        Transform rockC = SavePiece("Rock_C", WorldBorderMeshFactory.Rock("Rock_C", 37, new Vector3(.8f, .9f, .8f), .16f, .4f), groundOnly, true);
        // cliffs are long along z, so a style that aligns them to the strip lines them up along the edge
        Transform cliffA = SavePiece("Cliff_A", WorldBorderMeshFactory.Rock("Cliff_A", 51, new Vector3(1.3f, 1.6f, 2.6f), .22f, .35f), groundOnly, true);
        Transform cliffB = SavePiece("Cliff_B", WorldBorderMeshFactory.Rock("Cliff_B", 67, new Vector3(1.4f, 1.9f, 2.2f), .25f, .3f), groundOnly, true);
        Transform reedsA = SavePiece("Reeds_A", WorldBorderMeshFactory.Reeds("Reeds_A", 5, 16), groundOnly, false);
        Transform reedsB = SavePiece("Reeds_B", WorldBorderMeshFactory.Reeds("Reeds_B", 9, 11), groundOnly, false);
        SavePiece("Mountain_A", WorldBorderMeshFactory.Mountain("Mountain_A", 3, true), groundOnly, false);
        SavePiece("Mountain_B", WorldBorderMeshFactory.Mountain("Mountain_B", 8, false), groundOnly, false);
        SavePiece("Mountain_C", WorldBorderMeshFactory.Mountain("Mountain_C", 14, true), groundOnly, false);
        SavePiece("Mountain_D", WorldBorderMeshFactory.Mountain("Mountain_D", 21, false), groundOnly, false);
        Transform fencePost = SavePiece("FencePost", WorldBorderMeshFactory.FencePost("FencePost"), woodPair, true);
        Transform fenceSegment = SavePiece("FenceSegment", WorldBorderMeshFactory.FenceSegment("FenceSegment"), woodPair, true);
        Transform gateOpen = SavePiece("Gate_Open", WorldBorderMeshFactory.Gate("Gate_Open", true), woodPair, true);
        Transform gateClosed = SavePiece("Gate_Closed", WorldBorderMeshFactory.Gate("Gate_Closed", false), woodPair, true);

        Transform oak = LoadModel("trees/tree_1.glb");
        Transform pine = LoadModel("trees/tree_2.glb");
        Transform birch = LoadModel("trees/tree_3.glb");
        Transform roundBush = LoadModel("bushes/bush_1.glb");
        Transform uprightBush = LoadModel("bushes/bush_2.glb");
        Transform berryBush = LoadModel("bushes/bush_3.glb");

        // triangle budget: pine 248, upright bush 400, round bush 400, birch 464, oak 592, berry bush 1920 (accent only)
        CreateStyleIfMissing("BorderStyle_DenseForest", style =>
        {
            style.spacing = 3.4f;
            style.jitter = .42f;
            style.emptyChance = .06f;
            style.sinkDepth = .15f;
            style.pieceArray = new[] {
                Piece(pine, 4f, .9f, 1.25f, .8f), Piece(oak, 1.4f, .85f, 1.15f, .9f), Piece(birch, 1.2f, .85f, 1.1f, .7f),
                Piece(roundBush, 1f, .9f, 1.3f, 1f), Piece(uprightBush, .8f, .9f, 1.2f, .7f), Piece(rockA, .6f, .6f, 1.2f, .8f),
            };
        });
        CreateStyleIfMissing("BorderStyle_RockyEdge", style =>
        {
            style.spacing = 3.6f;
            style.jitter = .3f;
            style.emptyChance = .05f;
            style.sinkDepth = .45f;
            style.maxTilt = 8f;
            style.alignToStrip = true;
            style.yawJitter = 25f;
            style.pieceArray = new[] {
                Piece(cliffA, 2f, 1.3f, 2f, 1.6f), Piece(cliffB, 2f, 1.2f, 1.9f, 1.4f),
                Piece(rockA, 1f, .8f, 1.5f, .9f), Piece(rockB, 1f, .8f, 1.5f, .9f), Piece(uprightBush, .6f, .9f, 1.2f, .7f),
            };
        });
        CreateStyleIfMissing("BorderStyle_Hedge", style =>
        {
            style.spacing = 2.6f;
            style.jitter = .38f;
            style.emptyChance = .04f;
            style.sinkDepth = .1f;
            style.pieceArray = new[] {
                Piece(roundBush, 3f, 1f, 1.4f, 1.1f), Piece(uprightBush, 2.4f, 1f, 1.3f, .7f), Piece(berryBush, .35f, 1f, 1.3f, 1f),
                Piece(birch, .8f, .85f, 1.05f, .7f), Piece(oak, .5f, .85f, 1.1f, .9f), Piece(rockC, .4f, .5f, 1f, .6f),
            };
        });
        // bushes only (nothing above ~2.5 m), for ground the gameplay camera looks across
        CreateStyleIfMissing("BorderStyle_Shrubs", style =>
        {
            style.spacing = 2.4f;
            style.jitter = .4f;
            style.emptyChance = .05f;
            style.sinkDepth = .1f;
            style.pieceArray = new[] {
                Piece(roundBush, 3f, 1f, 1.4f, 1.1f), Piece(uprightBush, 1.5f, .9f, 1.1f, .7f), Piece(rockB, .8f, .5f, 1f, .7f),
                Piece(reedsB, .8f, .9f, 1.2f, .4f),
            };
        });
        // a low, dense bank: rocks set into the ground with reeds and berry bushes between them
        CreateStyleIfMissing("BorderStyle_Riverbank", style =>
        {
            style.spacing = 1.9f;
            style.jitter = .42f;
            style.emptyChance = .06f;
            style.sinkDepth = .2f;
            style.maxTilt = 8f;
            style.pieceArray = new[] {
                Piece(rockA, 2.2f, .55f, 1.2f, .7f), Piece(rockB, 1.6f, .55f, 1.1f, .7f), Piece(rockC, 1.2f, .5f, 1f, .6f),
                Piece(reedsA, 2f, .9f, 1.3f, .4f), Piece(reedsB, 1.4f, .9f, 1.2f, .4f), Piece(roundBush, .9f, .8f, 1.1f, 1f),
            };
        });
        CreateStyleIfMissing("BorderStyle_Reeds", style =>
        {
            style.spacing = 1.7f;
            style.jitter = .45f;
            style.emptyChance = .2f;
            style.sinkDepth = .05f;
            style.allowInWater = true;
            style.pieceArray = new[] { Piece(reedsA, 3f, .8f, 1.3f, .4f), Piece(reedsB, 2f, .8f, 1.2f, .4f), Piece(rockC, .6f, .3f, .7f, .5f) };
        });
        CreateStyleIfMissing("BorderStyle_BackgroundForest", style =>
        {
            // only cutscene cameras and the overview see this far out, through the haze
            style.spacing = 8.5f;
            style.jitter = .45f;
            style.emptyChance = .05f;
            style.sinkDepth = .2f;
            style.pieceArray = new[] { Piece(pine, 4f, 1.15f, 1.55f, 1f), Piece(birch, 1f, 1.05f, 1.35f, .8f), Piece(oak, .8f, 1.1f, 1.4f, 1f) };
        });
        CreateStyleIfMissing("BorderStyle_FenceLine", style =>
        {
            style.placement = BorderStyleSO.Placement.Line;
            style.spacing = WorldBorderMeshFactory.FENCE_LENGTH;
            style.pieceLength = WorldBorderMeshFactory.FENCE_LENGTH;
            style.stretchToFit = true;
            style.alignToStrip = true;
            style.sinkDepth = 0f;
            style.endCapPrefab = fencePost;
            style.pieceArray = new[] { Piece(fenceSegment, 1f, 1f, 1f, .2f) };
        });
        CreateStyleIfMissing("BorderStyle_GateOpen", style =>
        {
            style.placement = BorderStyleSO.Placement.Line;
            style.spacing = WorldBorderMeshFactory.GATE_WIDTH;
            style.pieceLength = WorldBorderMeshFactory.GATE_WIDTH;
            style.stretchToFit = true;
            style.alignToStrip = true;
            style.sinkDepth = 0f;
            style.pieceArray = new[] { Piece(gateOpen, 1f, 1f, 1f, .3f) };
        });
        CreateStyleIfMissing("BorderStyle_GateClosed", style =>
        {
            style.placement = BorderStyleSO.Placement.Line;
            style.spacing = WorldBorderMeshFactory.GATE_WIDTH;
            style.pieceLength = WorldBorderMeshFactory.GATE_WIDTH;
            style.stretchToFit = true;
            style.alignToStrip = true;
            style.sinkDepth = 0f;
            style.pieceArray = new[] { Piece(gateClosed, 1f, 1f, 1f, .3f) };
        });

        AssetDatabase.SaveAssets();
        Debug.Log("WorldBorderAssetGenerator: border meshes, prefabs and styles are up to date");
    }

    public static Transform LoadPiece(string name) => AssetDatabase.LoadAssetAtPath<Transform>(PREFAB_FOLDER + "/" + name + ".prefab");
    public static BorderStyleSO LoadStyle(string name) => AssetDatabase.LoadAssetAtPath<BorderStyleSO>(STYLE_FOLDER + "/" + name + ".asset");

    public static T LoadSubAsset<T>(string path, string name) where T : UnityEngine.Object
    {
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is T typed && asset.name == name) return typed;
        return null;
    }

    // overwrites the mesh asset in place, so every prefab and scene that uses it keeps its reference. It goes through the
    // Mesh API: EditorUtility.CopySerialized only changes the CPU copy and renderers keep drawing the old geometry
    public static Mesh SaveMesh(Mesh mesh, string path)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        existing.Clear();
        existing.name = mesh.name;
        existing.indexFormat = mesh.indexFormat;
        existing.vertices = mesh.vertices;
        existing.normals = mesh.normals;
        existing.colors = mesh.colors;
        existing.uv = mesh.uv;
        existing.tangents = mesh.tangents;
        existing.subMeshCount = mesh.subMeshCount;
        for (int i = 0; i < mesh.subMeshCount; i++) existing.SetTriangles(mesh.GetTriangles(i), i);
        existing.bounds = mesh.bounds;
        EditorUtility.SetDirty(existing);
        UnityEngine.Object.DestroyImmediate(mesh);
        return existing;
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    private static Transform SavePiece(string name, Mesh mesh, Material[] materialArray, bool castsShadows)
    {
        Mesh savedMesh = SaveMesh(mesh, MESH_FOLDER + "/" + name + ".asset");
        GameObject piece = new GameObject(name);
        piece.AddComponent<MeshFilter>().sharedMesh = savedMesh;
        MeshRenderer meshRenderer = piece.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = materialArray;
        meshRenderer.shadowCastingMode = castsShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        GameObjectUtility.SetStaticEditorFlags(piece, StaticEditorFlags.BatchingStatic);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(piece, PREFAB_FOLDER + "/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(piece);
        return prefab.transform;
    }

    private static Transform LoadModel(string relativePath)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_FOLDER + relativePath);
        if (model == null) Debug.LogError("WorldBorderAssetGenerator: missing model " + MODEL_FOLDER + relativePath);
        return model != null ? model.transform : null;
    }

    private static void CreateStyleIfMissing(string name, Action<BorderStyleSO> fill)
    {
        string path = STYLE_FOLDER + "/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<BorderStyleSO>(path) != null) return;
        BorderStyleSO style = ScriptableObject.CreateInstance<BorderStyleSO>();
        fill(style);
        AssetDatabase.CreateAsset(style, path);
    }

    private static BorderStyleSO.Piece Piece(Transform prefab, float weight, float minScale, float maxScale, float radius) =>
        new BorderStyleSO.Piece { prefab = prefab, weight = weight, scaleRange = new Vector2(minScale, maxScale), radius = radius };
}
