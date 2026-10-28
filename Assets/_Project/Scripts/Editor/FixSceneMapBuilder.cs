using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static GrayboxBuild;

// Map changes to FixScene (the CozyFarm map) that every day's scene inherits, since Day 2 and Day 3 are rebuilt from it.
// Both menus are re-runnable and skip what is already there; save FixScene, then rebuild Day 2 and Day 3.
// - Set Up Ponds: the four forest ponds fill an empty bottle like the river, and the player can't walk onto them.
// - Plant Day 3 Clearing Trees: three more forest trees on the grass across the river from the garden, the field Day 3 clears.
public static class FixSceneMapBuilder
{
    private const string PONDS_ROOT = "PondWaterSpots";
    private const string TREES_ROOT = "Day3ClearingTrees";
    private const string INTERACTABLES_PATH = "Day1_Gameplay/Interactables";
    private const string POND_MESH_FOLDER = "Assets/_Project/Art/Models/Ponds";
    private const int POND_COUNT = 4;
    // a wall round the water from the pond bed to well over the player's head
    private const float BLOCKER_DEPTH = 1f;
    private const float BLOCKER_HEIGHT = 2.5f;

    // copies of the map's own round-crowned forest trees (a bare trunk to punch, unlike the pines): (source model, where it stands, yaw)
    private static readonly (string source, Vector3 position, float yaw)[] ClearingTreeArray =
    {
        ("Forest_Tree_3_039", new Vector3(14.2f, 0f, -26f), 40f),
        ("Forest_Tree_1_028", new Vector3(16.6f, 0f, -32.6f), 160f),
        ("Forest_Tree_3_039", new Vector3(18.3f, 0f, -28.2f), 200f),
    };

    [MenuItem("Tools/FixScene/Set Up Ponds")]
    public static void SetUpPonds()
    {
        if (!IsFixSceneOpen()) return;
        Transform interactables = GameObject.Find(INTERACTABLES_PATH).transform;
        Transform existing = interactables.Find(PONDS_ROOT);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        Transform root = Root(PONDS_ROOT, interactables).transform;
        ItemDataSO water = GameDataBuilder.Load<ItemDataSO>("Items", "Water");
        ItemDataSO emptyBottle = GameDataBuilder.Load<ItemDataSO>("Items", "EmptyBottle");
        if (!AssetDatabase.IsValidFolder(POND_MESH_FOLDER)) AssetDatabase.CreateFolder("Assets/_Project/Art/Models", "Ponds");

        for (int i = 1; i <= POND_COUNT; i++)
        {
            GameObject pond = GameObject.Find($"CozyFarm/Pond_{i}_Water");
            if (pond == null)
            {
                Debug.LogError($"CozyFarm/Pond_{i}_Water is missing");
                continue;
            }
            Bounds bounds = pond.GetComponent<Renderer>().bounds;
            GameObject spot = Root($"Pond_{i}_WaterSpot", root);
            spot.transform.position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            spot.layer = LayerMask.NameToLayer("Interactable");

            MeshCollider blocker = spot.AddComponent<MeshCollider>();
            blocker.sharedMesh = BlockerMesh(pond, spot.transform, $"Pond_{i}_Blocker");
            blocker.convex = true;

            ResourceNode node = spot.AddComponent<ResourceNode>();
            SetField(node, "itemDataSO", water);
            SetField(node, "requiredItemDataSO", emptyBottle);
            SetValue(node, "isInfinite", 1f);
            SetValue(node, "respawnTime", 0f);
            SetString(node, "promptVerb", "scoop water");
            SetString(node, "blockedReason", "Need an empty bottle");
            Transform anchor = Root("PromptAnchor", spot.transform).transform;
            SetField(node, "promptAnchor", anchor);
            PondPromptAnchor pondAnchor = spot.AddComponent<PondPromptAnchor>();
            SetField(pondAnchor, "promptAnchor", anchor);
        }
        AssetDatabase.SaveAssets();
        Save();
        Debug.Log($"{POND_COUNT} ponds set up in FixScene. Rebuild Day 2 and Day 3 to carry them over.");
    }

    [MenuItem("Tools/FixScene/Plant Day 3 Clearing Trees")]
    public static void PlantClearingTrees()
    {
        if (!IsFixSceneOpen()) return;
        // under the map, so the camera's occlusion fade treats them like its own trees
        Transform map = GameObject.Find("CozyFarm").transform;
        Transform existing = map.Find(TREES_ROOT);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        Transform root = Root(TREES_ROOT, map).transform;
        foreach ((string source, Vector3 position, float yaw) in ClearingTreeArray)
        {
            GameObject model = GameObject.Find($"CozyFarm/{source}");
            if (model == null)
            {
                Debug.LogError($"CozyFarm/{source} is missing");
                continue;
            }
            // a plain copy of the mesh, material and collider, so it doesn't count as an edit of the imported map
            GameObject tree = Root($"ClearingTree_{root.childCount + 1}", root);
            tree.AddComponent<MeshFilter>().sharedMesh = model.GetComponent<MeshFilter>().sharedMesh;
            tree.AddComponent<MeshRenderer>().sharedMaterials = model.GetComponent<MeshRenderer>().sharedMaterials;
            tree.AddComponent<MeshCollider>().sharedMesh = model.GetComponent<MeshFilter>().sharedMesh;
            tree.transform.localScale = model.transform.lossyScale;
            // the copy's trunk foot stands on the ground at 'position', whatever the source mesh's pivot
            tree.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * model.transform.rotation;
            Bounds bounds = tree.GetComponent<MeshRenderer>().bounds;
            Vector3 foot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            tree.transform.position += FixSceneDay2Builder.Ground(position) - foot;
        }
        Save();
        Debug.Log("Day 3's clearing trees planted in FixScene. Rebuild Day 2 and Day 3 to carry them over.");
    }

    // the pond's surface copied down into the bed and up over the player's head; the convex collider wraps both
    private static Mesh BlockerMesh(GameObject pond, Transform spot, string name)
    {
        Mesh pondMesh = pond.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] pondVertexArray = pondMesh.vertices;
        int[] pondTriangleArray = pondMesh.triangles;
        int count = pondVertexArray.Length;
        Vector3[] vertexArray = new Vector3[count * 2];
        for (int i = 0; i < count; i++)
        {
            Vector3 local = spot.InverseTransformPoint(pond.transform.TransformPoint(pondVertexArray[i]));
            vertexArray[i] = new Vector3(local.x, -BLOCKER_DEPTH, local.z);
            vertexArray[count + i] = new Vector3(local.x, BLOCKER_HEIGHT, local.z);
        }
        int[] triangleArray = new int[pondTriangleArray.Length * 2];
        for (int i = 0; i < pondTriangleArray.Length; i++)
        {
            triangleArray[i] = pondTriangleArray[i];
            triangleArray[pondTriangleArray.Length + i] = count + pondTriangleArray[i];
        }
        string path = $"{POND_MESH_FOLDER}/{name}.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = name };
            AssetDatabase.CreateAsset(mesh, path);
        }
        mesh.Clear();
        mesh.vertices = vertexArray;
        mesh.triangles = triangleArray;
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static bool IsFixSceneOpen()
    {
        if (EditorSceneManager.GetActiveScene().path == FixSceneDay2Builder.SOURCE_SCENE_PATH) return true;
        Debug.LogError($"Open {FixSceneDay2Builder.SOURCE_SCENE_PATH} first.");
        return false;
    }

    private static void Save()
    {
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static void SetString(Object target, string field, string value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
