using UnityEditor;
using UnityEngine;
using static GrayboxBuild;

// Meshes: Shell, Rug, Furniture (desk + stool), Decor, Door, Bed, BedsideTable.
// Dresses the bedroom prefabs (PlayerBedroom, Bed, BedsideTable) in the Blender "Farmhand's Den" model
// (Tools/Blender/build_bedroom_concepts.py → Art/Models/Bedroom/Bedroom.fbx). It edits the prefabs in place: the graybox
// colliders, BedroomDoor and light stay, only their meshes are swapped, so HouseInterior keeps every reference. Re-runnable.
public static class BedroomModelBuilder
{
    private const string FBX_PATH = "Assets/_Project/Art/Models/Bedroom/Bedroom.fbx";
    private const string ENVIRONMENT = "Assets/_Project/Prefabs/Environment";
    private const string MODEL_PREFIX = "Model_";

    [MenuItem("Tools/Day1/Apply Bedroom Model")]
    public static void Apply()
    {
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(FBX_PATH);
        if (importer.importAnimation || importer.importCameras || importer.importLights)
        {
            importer.importAnimation = importer.importCameras = importer.importLights = false;
            importer.SaveAndReimport();
        }
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_PATH);

        Edit("PlayerBedroom", root =>
        {
            BedroomDoor door = root.GetComponentInChildren<BedroomDoor>();
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                if (!renderer.transform.IsChildOf(door.transform)) StripMesh(renderer);
            AddModel(fbx, "Shell", root.transform);
            AddModel(fbx, "Rug", root.transform);
            AddModel(fbx, "Furniture", root.transform).AddComponent<MeshCollider>();
            AddModel(fbx, "Decor", root.transform).AddComponent<MeshCollider>();

            Transform visual = door.transform.Find("Visual");
            for (int i = visual.childCount - 1; i >= 0; i--) Object.DestroyImmediate(visual.GetChild(i).gameObject);
            AddModel(fbx, "Door", visual);
            SetArray(door.GetComponent<InteractableOutlineVisual>(), "outlineGameObjectArray", AddOutlineHulls(visual, GrayboxPalette.Outline, .05f));
        });
        Edit("Bed", root => SwapModel(fbx, "Bed", root));
        Edit("BedsideTable", root => SwapModel(fbx, "BedsideTable", root));
        AssetDatabase.SaveAssets();
        Debug.Log("Bedroom model applied to PlayerBedroom, Bed and BedsideTable.");
    }

    private static void Edit(string prefabName, System.Action<GameObject> edit)
    {
        string path = $"{ENVIRONMENT}/{prefabName}.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        // a re-run starts from the graybox colliders again
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            if (root.transform.GetChild(i).name.StartsWith(MODEL_PREFIX)) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        edit(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // furniture: graybox parts are mesh-only children, the root keeps its collider
    private static void SwapModel(GameObject fbx, string part, GameObject root)
    {
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true)) Object.DestroyImmediate(renderer.gameObject);
        AddModel(fbx, part, root.transform);
    }

    private static void StripMesh(MeshRenderer renderer)
    {
        GameObject go = renderer.gameObject;
        Object.DestroyImmediate(renderer);
        Object.DestroyImmediate(go.GetComponent<MeshFilter>());
    }

    private static GameObject AddModel(GameObject fbx, string part, Transform parent)
    {
        Transform source = fbx.transform.Find(part);
        GameObject go = Root(MODEL_PREFIX + part, parent);
        go.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
        Material[] slots = source.GetComponent<MeshRenderer>().sharedMaterials;
        Material[] materials = new Material[slots.Length];
        for (int i = 0; i < slots.Length; i++) materials[i] = SlotMaterial(slots[i].name);
        go.AddComponent<MeshRenderer>().sharedMaterials = materials;
        return go;
    }

    // slots are named after their sRGB colour ("Bedroom_C9A06A"; Blender may add ".001")
    private static Material SlotMaterial(string slot)
    {
        string key = slot.Split('.')[0];
        if (key.StartsWith("Bedroom_") && ColorUtility.TryParseHtmlString("#" + key.Substring(8), out Color color)) return GrayboxPalette.Lit(key, color);
        Debug.LogError($"Bedroom FBX has an unknown material slot '{slot}'");
        return GrayboxPalette.DarkGray;
    }
}
