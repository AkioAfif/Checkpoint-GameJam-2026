using UnityEditor;
using UnityEngine;

// Small helpers shared by the level/prefab builders.
public static class GrayboxBuild
{
    public const string PREFABS = "Assets/_Project/Prefabs";

    public static GameObject Root(string name, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    // places a bottom-pivot mesh so that its bounds centre lands on 'centre'
    public static GameObject PartCentered(Transform parent, string name, Mesh mesh, Material material, Vector3 centre, Vector3 euler = default)
    {
        Quaternion rotation = Quaternion.Euler(euler);
        return Part(parent, name, mesh, material, centre - rotation * mesh.bounds.center, euler);
    }

    public static GameObject Part(Transform parent, string name, Mesh mesh, Material material, Vector3 localPosition, Vector3 euler = default)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localEulerAngles = euler;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    public static BoxCollider Box(GameObject go, Vector3 centre, Vector3 size, bool isTrigger = false)
    {
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = centre;
        box.size = size;
        box.isTrigger = isTrigger;
        return box;
    }

    public static void SetLayerRecursively(GameObject go, int layer)
    {
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }

    // adds an inverted-hull outline child to every mesh under 'visual'; returns the hull objects (disabled)
    public static GameObject[] AddOutlineHulls(Transform visual, Material outlineMaterial, float thickness = .05f)
    {
        MeshFilter[] filters = visual.GetComponentsInChildren<MeshFilter>(true);
        GameObject[] hulls = new GameObject[filters.Length];
        for (int i = 0; i < filters.Length; i++)
        {
            Mesh hullMesh = GrayboxMeshFactory.Outline(filters[i].sharedMesh, thickness);
            GameObject hull = Part(filters[i].transform, "Outline", hullMesh, outlineMaterial, Vector3.zero);
            hull.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hull.SetActive(false);
            hulls[i] = hull;
        }
        // shortcut: skinned hulls follow the bones but not blend shapes, fine while the face keys stay subtle
        foreach (SkinnedMeshRenderer skinned in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            GameObject hull = Root("Outline", skinned.transform);
            SkinnedMeshRenderer hullRenderer = hull.AddComponent<SkinnedMeshRenderer>();
            hullRenderer.sharedMesh = GrayboxMeshFactory.Outline(skinned.sharedMesh, thickness);
            hullRenderer.sharedMaterial = outlineMaterial;
            hullRenderer.bones = skinned.bones;
            hullRenderer.rootBone = skinned.rootBone;
            hullRenderer.localBounds = skinned.localBounds;
            hullRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hull.SetActive(false);
            ArrayUtility.Add(ref hulls, hull);
        }
        return hulls;
    }

    // sets a private [SerializeField] through SerializedObject (the only way to wire them from an editor script)
    public static void SetField(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Debug.LogError($"{target.GetType().Name} has no field '{field}'"); return; }
        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetArray(Object target, string field, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Debug.LogError($"{target.GetType().Name} has no field '{field}'"); return; }
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetValue(Object target, string field, float value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Debug.LogError($"{target.GetType().Name} has no field '{field}'"); return; }
        if (property.propertyType == SerializedPropertyType.Integer) property.intValue = (int)value;
        else if (property.propertyType == SerializedPropertyType.Boolean) property.boolValue = value != 0f;
        else property.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static GameObject SavePrefab(GameObject go, string folder, string name)
    {
        string dir = $"{PREFABS}/{folder}";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(PREFABS, folder);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/{name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, float yaw = 0f)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        return go;
    }
}
