using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > FixScene > Build River Flow: gives the imported River_Water (fix.glb, no UVs) a copy of its mesh with UVs that
// run along the river, a tileable flow-streak texture, a ripple normal map (used twice: the main ripples and a second, finer
// detail layer) and a URP Lit material, then swaps them in. RiverFlowVisual scrolls the textures at River's current speed and
// RiverWaveVisual swells the surface (all added and wired here). Re-runnable: it overwrites its own assets. The MeshCollider
// keeps the original, flat mesh (same shape).
public static class RiverFlowBuilder
{
    private const string WATER_NAME = "River_Water";
    // the glb river is a strip: rows of this many vertices straight across the water, one row per metre downstream
    private const int ROW_VERTEX_COUNT = 11;
    private const int TEXTURE_SIZE = 256;
    // metres of river one texture repeat covers, along and across
    private const float TILE_LENGTH = 8f;
    private const float TILES_ACROSS = 2f;
    private const string MESH_PATH = "Assets/_Project/Art/Models/River/River_Water_Flow.asset";
    private const string TEXTURE_PATH = "Assets/_Project/Art/Textures/RiverFlow.png";
    private const string RIPPLE_PATH = "Assets/_Project/Art/Textures/RiverRipples.png";
    // how steep the ripple normals are drawn from their height noise
    private const float RIPPLE_STRENGTH = 1.6f;
    private const string MATERIAL_PATH = "Assets/_Project/Art/Materials/RiverWater.mat";

    private static readonly Color DEEP = new Color(.13f, .55f, .7f);
    private static readonly Color SHALLOW = new Color(.27f, .78f, .87f);
    private static readonly Color FOAM = new Color(.9f, .97f, 1f);

    [MenuItem("Tools/FixScene/Build River Flow")]
    public static void Build()
    {
        GameObject water = GameObject.Find(WATER_NAME);
        if (water == null)
        {
            Debug.LogError($"No '{WATER_NAME}' in the open scene.");
            return;
        }
        MeshFilter meshFilter = water.GetComponent<MeshFilter>();
        Mesh sourceMesh = water.TryGetComponent(out MeshCollider meshCollider) && meshCollider.sharedMesh != null ? meshCollider.sharedMesh : meshFilter.sharedMesh;
        if (sourceMesh.vertexCount % ROW_VERTEX_COUNT != 0)
        {
            Debug.LogError($"{WATER_NAME} is not a strip of {ROW_VERTEX_COUNT}-vertex rows ({sourceMesh.vertexCount} vertices).");
            return;
        }

        Undo.RecordObject(meshFilter, "Build River Flow");
        meshFilter.sharedMesh = SaveMesh(FlowMesh(sourceMesh));
        MeshRenderer meshRenderer = water.GetComponent<MeshRenderer>();
        Undo.RecordObject(meshRenderer, "Build River Flow");
        meshRenderer.sharedMaterial = SaveMaterial(SaveTexture(), SaveRippleTexture());
        // the current's speed lives on River; the visual scrolls to match it
        if (!water.TryGetComponent(out River river)) river = Undo.AddComponent<River>(water);
        if (!water.TryGetComponent(out RiverFlowVisual flowVisual)) flowVisual = Undo.AddComponent<RiverFlowVisual>(water);
        SerializedObject serializedFlowVisual = new SerializedObject(flowVisual);
        serializedFlowVisual.FindProperty("river").objectReferenceValue = river;
        serializedFlowVisual.FindProperty("metresPerRepeat").floatValue = TILE_LENGTH;
        serializedFlowVisual.ApplyModifiedProperties();
        if (!water.TryGetComponent(out RiverWaveVisual waveVisual)) waveVisual = Undo.AddComponent<RiverWaveVisual>(water);
        SerializedObject serializedWaveVisual = new SerializedObject(waveVisual);
        serializedWaveVisual.FindProperty("river").objectReferenceValue = river;
        serializedWaveVisual.ApplyModifiedProperties();
        SerializedObject serializedRiver = new SerializedObject(river);
        SerializedProperty screenFade = serializedRiver.FindProperty("screenFade");
        if (screenFade.objectReferenceValue == null) screenFade.objectReferenceValue = Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include);
        serializedRiver.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(water.scene);
        Debug.Log($"{WATER_NAME} now flows ({sourceMesh.vertexCount / ROW_VERTEX_COUNT} rows).");
    }

    // u: 0 → TILES_ACROSS from bank to bank; v: metres downstream along the row centres, in texture repeats
    private static Mesh FlowMesh(Mesh source)
    {
        Vector3[] vertexArray = source.vertices;
        int rowCount = vertexArray.Length / ROW_VERTEX_COUNT;
        Vector2[] uvArray = new Vector2[vertexArray.Length];
        float distance = 0f;
        Vector3 previousCentre = RowCentre(vertexArray, 0);
        for (int row = 0; row < rowCount; row++)
        {
            Vector3 centre = RowCentre(vertexArray, row);
            distance += Vector3.Distance(previousCentre, centre);
            previousCentre = centre;
            for (int i = 0; i < ROW_VERTEX_COUNT; i++)
                uvArray[row * ROW_VERTEX_COUNT + i] = new Vector2((float)i / (ROW_VERTEX_COUNT - 1) * TILES_ACROSS, distance / TILE_LENGTH);
        }
        Mesh mesh = Object.Instantiate(source);
        mesh.name = "River_Water_Flow";
        mesh.uv = uvArray;
        mesh.RecalculateTangents();
        return mesh;
    }

    private static Vector3 RowCentre(Vector3[] vertexArray, int row) => (vertexArray[row * ROW_VERTEX_COUNT] + vertexArray[row * ROW_VERTEX_COUNT + ROW_VERTEX_COUNT - 1]) * .5f;

    private static Mesh SaveMesh(Mesh mesh)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(MESH_PATH));
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, MESH_PATH);
            return mesh;
        }
        EditorUtility.CopySerialized(mesh, existing);
        Object.DestroyImmediate(mesh);
        AssetDatabase.SaveAssets();
        return existing;
    }

    // long current streaks (stretched downstream) with flecks of foam on their crests; tiles both ways
    private static Texture2D SaveTexture()
    {
        Texture2D texture = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGB24, false);
        for (int y = 0; y < TEXTURE_SIZE; y++)
        {
            for (int x = 0; x < TEXTURE_SIZE; x++)
            {
                float u = (float)x / TEXTURE_SIZE;
                float v = (float)y / TEXTURE_SIZE;
                float streak = TileNoise(u, v, 14, 3, 1) * .65f + TileNoise(u, v, 28, 5, 2) * .35f;
                float foam = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.68f, .82f, TileNoise(u, v, 22, 6, 3) * .6f + streak * .4f));
                Color color = Color.Lerp(DEEP, SHALLOW, streak);
                texture.SetPixel(x, y, Color.Lerp(color, FOAM, foam * .75f));
            }
        }
        return SavePng(texture, TEXTURE_PATH, false);
    }

    // small, choppy river ripples: crests a little wider across the flow than along it, two scales mixed so nothing lines up
    private static Texture2D SaveRippleTexture()
    {
        float[,] heightArray = new float[TEXTURE_SIZE, TEXTURE_SIZE];
        for (int y = 0; y < TEXTURE_SIZE; y++)
        {
            for (int x = 0; x < TEXTURE_SIZE; x++)
            {
                float u = (float)x / TEXTURE_SIZE;
                float v = (float)y / TEXTURE_SIZE;
                heightArray[x, y] = TileNoise(u, v, 8, 18, 4) * .65f + TileNoise(u, v, 15, 32, 5) * .35f;
            }
        }
        Texture2D texture = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGB24, false);
        for (int y = 0; y < TEXTURE_SIZE; y++)
        {
            for (int x = 0; x < TEXTURE_SIZE; x++)
            {
                // slopes from the wrapped neighbours, so the normal map tiles too
                float dx = heightArray[(x + 1) % TEXTURE_SIZE, y] - heightArray[(x + TEXTURE_SIZE - 1) % TEXTURE_SIZE, y];
                float dy = heightArray[x, (y + 1) % TEXTURE_SIZE] - heightArray[x, (y + TEXTURE_SIZE - 1) % TEXTURE_SIZE];
                Vector3 normal = new Vector3(-dx * RIPPLE_STRENGTH * TEXTURE_SIZE / 32f, -dy * RIPPLE_STRENGTH * TEXTURE_SIZE / 32f, 1f).normalized;
                texture.SetPixel(x, y, new Color(normal.x * .5f + .5f, normal.y * .5f + .5f, normal.z * .5f + .5f));
            }
        }
        return SavePng(texture, RIPPLE_PATH, true);
    }

    private static Texture2D SavePng(Texture2D texture, string path, bool isNormalMap)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (isNormalMap) importer.textureType = TextureImporterType.NormalMap;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Material SaveMaterial(Texture2D texture, Texture2D rippleTexture)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, MATERIAL_PATH);
        }
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", .82f);
        material.SetFloat("_Metallic", 0f);
        // the main ripples ride the current with the streaks (they share _BaseMap's tiling and offset)
        material.SetTexture("_BumpMap", rippleTexture);
        material.SetFloat("_BumpScale", .45f);
        material.EnableKeyword("_NORMALMAP");
        // a finer second layer at its own scale and pace (RiverFlowVisual scrolls it), so the ripples keep changing shape;
        // the detail albedo stays URP's neutral grey
        material.SetTexture("_DetailNormalMap", rippleTexture);
        material.SetFloat("_DetailNormalMapScale", .3f);
        material.SetTextureScale("_DetailAlbedoMap", new Vector2(1.7f, 2.3f));
        material.EnableKeyword("_DETAIL_MULX2");
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    // smooth value noise on a cellsX × cellsY grid that wraps at the edges (0..1 in, 0..1 out)
    private static float TileNoise(float u, float v, int cellsX, int cellsY, int seed)
    {
        float x = u * cellsX;
        float y = v * cellsY;
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float tx = Mathf.SmoothStep(0f, 1f, x - x0);
        float ty = Mathf.SmoothStep(0f, 1f, y - y0);
        float a = Mathf.Lerp(Hash(x0, y0, cellsX, cellsY, seed), Hash(x0 + 1, y0, cellsX, cellsY, seed), tx);
        float b = Mathf.Lerp(Hash(x0, y0 + 1, cellsX, cellsY, seed), Hash(x0 + 1, y0 + 1, cellsX, cellsY, seed), tx);
        return Mathf.Lerp(a, b, ty);
    }

    private static float Hash(int x, int y, int cellsX, int cellsY, int seed)
    {
        x = ((x % cellsX) + cellsX) % cellsX;
        y = ((y % cellsY) + cellsY) % cellsY;
        uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
        h = (h ^ (h >> 13)) * 1274126177u;
        return (h ^ (h >> 16)) / (float)uint.MaxValue;
    }
}
