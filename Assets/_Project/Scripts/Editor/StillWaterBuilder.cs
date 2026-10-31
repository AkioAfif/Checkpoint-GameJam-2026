using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > FixScene > Build Still Water: the ponds and the well get the river's look without its current. Every renderer in the
// open scene with the glb's flat M_Water slot gets a copy of its mesh with flat top-down UVs in metres (the glb has none) and
// StillWater.mat, a copy of RiverWater.mat (run Build River Flow first), plus a StillWaterVisual on that slot. Re-runnable:
// it also finds the slots it already swapped. Run it in FixScene and FixScene_Day2/3/4 (or rebuild those days after FixScene).
public static class StillWaterBuilder
{
    private const string GLB_WATER_MATERIAL_NAME = "M_Water";
    private const string RIVER_MATERIAL_PATH = "Assets/_Project/Art/Materials/RiverWater.mat";
    private const string MATERIAL_PATH = "Assets/_Project/Art/Materials/StillWater.mat";
    private const string MESH_FOLDER = "Assets/_Project/Art/Models/StillWater";
    // metres one texture repeat covers, the same as the river's (RiverFlowBuilder: 8 m along, about 4 m across)
    private const float METRES_PER_REPEAT_U = 4f;
    private const float METRES_PER_REPEAT_V = 8f;

    [MenuItem("Tools/FixScene/Build Still Water")]
    public static void Build()
    {
        Material material = SaveMaterial();
        if (material == null) return;
        int count = 0;
        foreach (MeshRenderer meshRenderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
        {
            Material[] materialArray = meshRenderer.sharedMaterials;
            int waterIndex = System.Array.FindIndex(materialArray, m => m != null && (m.name == GLB_WATER_MATERIAL_NAME || m == material));
            if (waterIndex < 0) continue;

            MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
            Undo.RecordObject(meshFilter, "Build Still Water");
            meshFilter.sharedMesh = SaveMesh(meshFilter.sharedMesh, meshRenderer.transform);
            Undo.RecordObject(meshRenderer, "Build Still Water");
            materialArray[waterIndex] = material;
            meshRenderer.sharedMaterials = materialArray;
            if (!meshRenderer.TryGetComponent(out StillWaterVisual visual)) visual = Undo.AddComponent<StillWaterVisual>(meshRenderer.gameObject);
            SerializedObject serializedVisual = new SerializedObject(visual);
            serializedVisual.FindProperty("materialIndex").intValue = waterIndex;
            serializedVisual.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(meshRenderer.gameObject.scene);
            count++;
        }
        Debug.Log($"Still water on {count} renderers.");
    }

    // shortcut: an existing mesh asset is reused as is; delete Art/Models/StillWater to rebuild after the glb's water changes
    private static Mesh SaveMesh(Mesh source, Transform owner)
    {
        string baseName = source.name.EndsWith("_Still") ? source.name : source.name + "_Still";
        string path = $"{MESH_FOLDER}/{baseName}.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;

        Vector3[] vertexArray = source.vertices;
        Vector2[] uvArray = new Vector2[vertexArray.Length];
        for (int i = 0; i < vertexArray.Length; i++)
        {
            // world metres, so every pond and the well share the river's texture scale
            Vector3 world = owner.TransformPoint(vertexArray[i]);
            uvArray[i] = new Vector2(world.x / METRES_PER_REPEAT_U, world.z / METRES_PER_REPEAT_V);
        }
        Mesh mesh = Object.Instantiate(source);
        mesh.name = baseName;
        mesh.uv = uvArray;
        mesh.RecalculateTangents();
        Directory.CreateDirectory(MESH_FOLDER);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static Material SaveMaterial()
    {
        Material riverMaterial = AssetDatabase.LoadAssetAtPath<Material>(RIVER_MATERIAL_PATH);
        if (riverMaterial == null)
        {
            Debug.LogError($"No {RIVER_MATERIAL_PATH}: run Tools > FixScene > Build River Flow first.");
            return null;
        }
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
        if (material == null)
        {
            material = new Material(riverMaterial);
            AssetDatabase.CreateAsset(material, MATERIAL_PATH);
        }
        else
        {
            material.CopyPropertiesFromMaterial(riverMaterial);
        }
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }
}
