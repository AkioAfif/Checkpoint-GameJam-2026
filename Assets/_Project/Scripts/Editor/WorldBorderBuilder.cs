using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Bakes the invisible WorldBorder walls of the open scene. Re-runnable: it only replaces its own CollisionBoundary child.
public static class WorldBorderBuilder
{
    private const float DEFAULT_INSET = 1.5f;
    private const string DEFAULT_GROUND_NAME = "Terrain";

    [MenuItem("Tools/World Border/Bake All")]
    public static void BakeAll()
    {
        BakeColliders();
        WorldBorderVisualBuilder.BakeVisuals();
    }

    [MenuItem("Tools/World Border/Bake Colliders")]
    public static void BakeColliders()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("WorldBorderBuilder: stop Play mode first (play-mode changes are thrown away)");
            return;
        }
        WorldBorder worldBorder = Object.FindAnyObjectByType<WorldBorder>();
        if (worldBorder == null)
        {
            Debug.LogError("WorldBorderBuilder: no WorldBorder in the open scene. Add the WorldBorder prefab first.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(worldBorder.gameObject, "Bake World Border");
        if (worldBorder.GetPointArray().Length == 0) SeedPointsFromGround(worldBorder);

        Vector2[] pointArray = worldBorder.GetPointArray();
        if (pointArray.Length < 3)
        {
            Debug.LogError($"{worldBorder.name}: needs at least 3 outline points (no ground found to seed them)", worldBorder);
            return;
        }

        Transform oldBoundary = worldBorder.transform.Find(WorldBorder.COLLISION_BOUNDARY_NAME);
        if (oldBoundary != null) Undo.DestroyObjectImmediate(oldBoundary.gameObject);

        GameObject boundaryObject = new GameObject(WorldBorder.COLLISION_BOUNDARY_NAME);
        Undo.RegisterCreatedObjectUndo(boundaryObject, "Bake World Border");
        boundaryObject.transform.SetParent(worldBorder.transform, false);

        int wallLayer = LayerMask.NameToLayer("WorldBorder");
        if (wallLayer < 0)
        {
            Debug.LogError("WorldBorderBuilder: the WorldBorder layer does not exist. Add it under Tags and Layers first.", worldBorder);
            return;
        }

        int wallCount = 0;
        for (int i = 0; i < pointArray.Length; i++)
        {
            Vector2 a = pointArray[i];
            Vector2 b = pointArray[(i + 1) % pointArray.Length];
            Vector2 direction = b - a;
            float length = direction.magnitude;
            if (length < 0.01f)
            {
                Debug.LogWarning($"{worldBorder.name}: outline points {i} and {(i + 1) % pointArray.Length} are on top of each other, skipped", worldBorder);
                continue;
            }

            // each wall runs half a thickness past both ends, so neighbouring walls overlap at the corners with no gap
            float thickness = worldBorder.GetWallThickness();
            float height = worldBorder.GetWallHeight();
            Vector2 middle = (a + b) * 0.5f;
            Vector3 forward = new Vector3(direction.x, 0f, direction.y) / length;

            GameObject wall = new GameObject("Wall_" + i.ToString("00"));
            wall.layer = wallLayer;
            wall.transform.SetParent(boundaryObject.transform, false);
            wall.transform.localPosition = new Vector3(middle.x, worldBorder.GetWallBottomY() + height * 0.5f, middle.y);
            wall.transform.localRotation = Quaternion.LookRotation(forward, Vector3.up);

            BoxCollider box = wall.AddComponent<BoxCollider>();
            box.size = new Vector3(thickness, height, length + thickness);
            wallCount++;
        }

        EditorSceneManager.MarkSceneDirty(worldBorder.gameObject.scene);
        Debug.Log($"WorldBorderBuilder: baked {wallCount} wall colliders on {worldBorder.name}");
    }

    // a fresh WorldBorder gets the outline of the scene's ground, inset a little so the walls stand on the map
    private static void SeedPointsFromGround(WorldBorder worldBorder)
    {
        GameObject ground = GameObject.Find(DEFAULT_GROUND_NAME);
        if (ground == null || !ground.TryGetComponent(out Renderer groundRenderer))
        {
            Debug.LogWarning($"{worldBorder.name}: no '{DEFAULT_GROUND_NAME}' renderer to seed the outline from. Type the points in the Inspector.", worldBorder);
            return;
        }

        Bounds bounds = groundRenderer.bounds;
        float minX = bounds.min.x + DEFAULT_INSET;
        float maxX = bounds.max.x - DEFAULT_INSET;
        float minZ = bounds.min.z + DEFAULT_INSET;
        float maxZ = bounds.max.z - DEFAULT_INSET;

        // the border object sits at the origin in our scenes, so world XZ = local XZ
        Vector2[] pointArray = {
            new Vector2(minX, minZ),
            new Vector2(maxX, minZ),
            new Vector2(maxX, maxZ),
            new Vector2(minX, maxZ),
        };
        SerializedObject serializedBorder = new SerializedObject(worldBorder);
        SerializedProperty pointProperty = serializedBorder.FindProperty("pointArray");
        pointProperty.arraySize = pointArray.Length;
        for (int i = 0; i < pointArray.Length; i++) pointProperty.GetArrayElementAtIndex(i).vector2Value = pointArray[i];
        serializedBorder.ApplyModifiedPropertiesWithoutUndo();
    }
}
