using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Tools > World Border > Bake Visuals: builds the open scene's WorldBorderDressing layout into its VisualBorder child.
// - strips of border styles past the walls (forest, rocky edge, hedge, riverbank, gates)
// - a vertex-coloured ground skirt past the ground mesh's edge, rising into hills, with the roads painted on
// - the river carried on past the map's edge (same UVs, its own RiverFlowVisual so the water flows with the river)
// - a ring of distant mountains and the haze
// Re-runnable: it only replaces VisualBorder. Nothing it makes has a collider (the walls keep the player in), and a
// NavMeshModifier keeps all of it out of NavMesh bakes so villagers never path off the map.
public static class WorldBorderVisualBuilder
{
    private const float RAY_TOP = 150f;
    private const float RIVER_BED_HEIGHT = -1.6f;
    private const float RIVER_END_TOLERANCE = 3f;
    // the skirt starts this far above the ground mesh's edge: any lower and the edge throws a thin shadow line onto it
    private const float SEAM_LIFT = .03f;

    private class Corridor
    {
        public Vector2 start;
        public Vector2 direction;
        public float halfWidth;
        public float length;
        public bool isRiver;

        public float GetDistance(Vector2 point, out float along)
        {
            Vector2 offset = point - start;
            along = Vector2.Dot(offset, direction);
            return (offset - direction * Mathf.Clamp(along, 0f, length)).magnitude;
        }
    }

    private class PlacementStrip
    {
        public string label;
        public BorderStyleSO borderStyleSO;
        public Vector2 start;
        public Vector2 end;
        public Vector2 outward;
        public float nearOffset;
        public float farOffset;
        public int seed;
    }

    private class Bake
    {
        public WorldBorderLayoutSO layout;
        public Transform root;
        public MeshCollider groundCollider;
        public int[] groundTriangleArray;
        public Color[] groundColorArray;
        public Rect groundRect;
        public int borderLayer;
        public string sceneName;
        public readonly List<Corridor> corridorList = new List<Corridor>();
        public readonly List<PlacementStrip> bankStripList = new List<PlacementStrip>();
        public readonly RaycastHit[] hitArray = new RaycastHit[16];
        public readonly Collider[] overlapArray = new Collider[16];
    }

    [MenuItem("Tools/World Border/Bake Visuals")]
    public static void BakeVisuals()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("WorldBorderVisualBuilder: stop Play mode first (play-mode changes are thrown away)");
            return;
        }
        WorldBorderDressing dressing = Object.FindAnyObjectByType<WorldBorderDressing>();
        if (dressing == null)
        {
            Debug.LogError("WorldBorderVisualBuilder: no WorldBorderDressing in the open scene (it sits on the WorldBorder prefab)");
            return;
        }
        WorldBorderLayoutSO layout = dressing.GetWorldBorderLayoutSO();
        if (layout == null)
        {
            Debug.LogError($"{dressing.name}: WorldBorderDressing has no layout. Assign a WorldBorderLayoutSO.", dressing);
            return;
        }
        GameObject ground = GameObject.Find(layout.groundObjectName);
        if (ground == null || !ground.TryGetComponent(out MeshCollider groundCollider) || groundCollider.sharedMesh == null)
        {
            Debug.LogError($"{dressing.name}: ground '{layout.groundObjectName}' with a MeshCollider not found", dressing);
            return;
        }

        Physics.SyncTransforms();
        Bake bake = new Bake {
            layout = layout,
            groundCollider = groundCollider,
            groundTriangleArray = groundCollider.sharedMesh.triangles,
            groundColorArray = groundCollider.sharedMesh.colors,
            borderLayer = LayerMask.NameToLayer("WorldBorder"),
            sceneName = dressing.gameObject.scene.name,
        };
        Bounds groundBounds = groundCollider.bounds;
        bake.groundRect = Rect.MinMaxRect(groundBounds.min.x, groundBounds.min.z, groundBounds.max.x, groundBounds.max.z);

        Transform oldRoot = dressing.transform.Find(WorldBorderDressing.VISUAL_ROOT_NAME);
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot.gameObject);
        bake.root = new GameObject(WorldBorderDressing.VISUAL_ROOT_NAME).transform;
        Undo.RegisterCreatedObjectUndo(bake.root.gameObject, "Bake World Border Visuals");
        bake.root.SetParent(dressing.transform, false);
        // everything below is built in world space
        bake.root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        NavMeshModifier navMeshModifier = bake.root.gameObject.AddComponent<NavMeshModifier>();
        navMeshModifier.ignoreFromBuild = true;
        WorldBorderAssetGenerator.EnsureFolder(WorldBorderAssetGenerator.GENERATED_MESH_FOLDER);

        foreach (WorldBorderLayoutSO.Road road in layout.roadArray)
            bake.corridorList.Add(new Corridor { start = road.start, direction = road.direction.normalized, halfWidth = road.width * .5f, length = road.length });
        BuildRiverExtensions(bake);
        BuildGroundSkirt(bake);
        BuildRoads(bake);

        int pieceCount = 0;
        Transform stripRoot = CreateChild(bake.root, "Strips");
        Vector2 center = bake.groundRect.center;
        foreach (WorldBorderLayoutSO.Strip strip in layout.stripArray)
        {
            if (strip.borderStyleSO == null)
            {
                Debug.LogWarning($"{dressing.name}: strip '{strip.label}' has no border style, skipped", dressing);
                continue;
            }
            Vector2 along = (strip.end - strip.start).normalized;
            Vector2 outward = new Vector2(-along.y, along.x);
            if (Vector2.Dot(outward, (strip.start + strip.end) * .5f - center) < 0f) outward = -outward;
            pieceCount += PlaceStrip(bake, new PlacementStrip {
                label = strip.label, borderStyleSO = strip.borderStyleSO, start = strip.start, end = strip.end,
                outward = outward, nearOffset = strip.nearOffset, farOffset = strip.farOffset, seed = strip.seed,
            }, CreateChild(stripRoot, strip.label));
        }
        foreach (PlacementStrip bankStrip in bake.bankStripList) pieceCount += PlaceStrip(bake, bankStrip, CreateChild(stripRoot, bankStrip.label));

        int mountainCount = PlaceMountains(bake, CreateChild(bake.root, "Mountains"));
        if (layout.applyHaze) ApplyHaze(layout);

        EditorSceneManager.MarkSceneDirty(dressing.gameObject.scene);
        Debug.Log($"WorldBorderVisualBuilder: {pieceCount} border pieces, {mountainCount} mountains, {bake.corridorList.Count} road/river corridors on {dressing.name}");
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        Transform child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    // ---------- strips ----------

    private static int PlaceStrip(Bake bake, PlacementStrip strip, Transform parent)
    {
        BorderStyleSO style = strip.borderStyleSO;
        Vector2 line = strip.end - strip.start;
        float length = line.magnitude;
        if (length < .01f) return 0;
        Vector2 along = line / length;
        float stripYaw = Mathf.Atan2(along.x, along.y) * Mathf.Rad2Deg;

        float totalWeight = 0f;
        foreach (BorderStyleSO.Piece piece in style.pieceArray) if (piece.prefab != null) totalWeight += piece.weight;
        if (totalWeight <= 0f)
        {
            Debug.LogWarning($"WorldBorderVisualBuilder: style {style.name} has no pieces, strip '{strip.label}' skipped", style);
            return 0;
        }

        System.Random random = new System.Random(strip.seed);
        int placedCount = 0;
        if (style.placement == BorderStyleSO.Placement.Line)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(length / style.spacing));
            float step = length / count;
            float stretch = style.stretchToFit ? step / style.pieceLength : 1f;
            for (int k = 0; k < count; k++)
            {
                BorderStyleSO.Piece piece = PickPiece(style, totalWeight, random);
                Vector2 point = strip.start + along * (k * step) + strip.outward * strip.nearOffset;
                float scale = Mathf.Lerp(piece.scaleRange.x, piece.scaleRange.y, (float)random.NextDouble());
                Spawn(piece.prefab, parent, point, GetSurfaceHeight(bake, point) - style.sinkDepth, stripYaw, new Vector3(scale, scale, scale * stretch), Quaternion.identity);
                placedCount++;
            }
            if (style.endCapPrefab != null)
            {
                Vector2 endPoint = strip.end + strip.outward * strip.nearOffset;
                Spawn(style.endCapPrefab, parent, endPoint, GetSurfaceHeight(bake, endPoint) - style.sinkDepth, stripYaw, Vector3.one, Quaternion.identity);
                placedCount++;
            }
            return placedCount;
        }

        // scatter: one piece per grid cell, jittered inside it, so the band is even with no clumps and no gaps
        float depth = Mathf.Max(.01f, strip.farOffset - strip.nearOffset);
        int columnCount = Mathf.Max(1, Mathf.CeilToInt(length / style.spacing));
        int rowCount = Mathf.Max(1, Mathf.CeilToInt(depth / style.spacing));
        float cellLength = length / columnCount;
        float cellDepth = depth / rowCount;
        for (int column = 0; column < columnCount; column++)
        {
            for (int row = 0; row < rowCount; row++)
            {
                bool isEmpty = random.NextDouble() < style.emptyChance;
                float u = (column + .5f + Signed(random) * style.jitter) * cellLength;
                float v = strip.nearOffset + (row + .5f + Signed(random) * style.jitter) * cellDepth;
                BorderStyleSO.Piece piece = PickPiece(style, totalWeight, random);
                float scale = Mathf.Lerp(piece.scaleRange.x, piece.scaleRange.y, (float)random.NextDouble());
                float yaw = style.alignToStrip ? stripYaw + Signed(random) * style.yawJitter : (float)random.NextDouble() * 360f;
                Quaternion tilt = Quaternion.AngleAxis(Signed(random) * style.maxTilt, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f) * Vector3.right);
                if (isEmpty) continue;

                // the footprint stays past the near edge, so nothing pokes back through the wall into the playable area
                float radius = piece.radius * scale;
                v = Mathf.Max(v, strip.nearOffset + radius);
                Vector2 point = strip.start + along * u + strip.outward * v;
                if (!TryFindGround(bake, point, radius, style.allowInWater, out float height)) continue;
                Spawn(piece.prefab, parent, point, height - style.sinkDepth * scale, yaw, Vector3.one * scale, tilt);
                placedCount++;
            }
        }
        return placedCount;
    }

    private static BorderStyleSO.Piece PickPiece(BorderStyleSO style, float totalWeight, System.Random random)
    {
        float roll = (float)random.NextDouble() * totalWeight;
        BorderStyleSO.Piece lastPiece = default;
        foreach (BorderStyleSO.Piece piece in style.pieceArray)
        {
            if (piece.prefab == null) continue;
            lastPiece = piece;
            roll -= piece.weight;
            if (roll <= 0f) return piece;
        }
        return lastPiece;
    }

    private static void Spawn(Transform prefab, Transform parent, Vector2 point, float height, float yaw, Vector3 scale, Quaternion tilt)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, parent);
        // keep the model's own import rotation/scale (glTF roots may carry one)
        instance.transform.SetPositionAndRotation(new Vector3(point.x, height, point.y), tilt * Quaternion.Euler(0f, yaw, 0f) * prefab.localRotation);
        instance.transform.localScale = Vector3.Scale(scale, prefab.localScale);
        foreach (Transform part in instance.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(part.gameObject, StaticEditorFlags.BatchingStatic);
    }

    // fences and gates are placed on purpose, so they stand on whatever is there (the road, for a gate). The lowest hit is
    // the ground: anything parked above it (the truck waits just past the west gate) must not lift the piece onto its roof
    private static float GetSurfaceHeight(Bake bake, Vector2 point)
    {
        Ray ray = new Ray(new Vector3(point.x, RAY_TOP, point.y), Vector3.down);
        int hitCount = Physics.RaycastNonAlloc(ray, bake.hitArray, RAY_TOP * 2f, ~0, QueryTriggerInteraction.Ignore);
        float height = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
            if (bake.hitArray[i].collider.gameObject.layer != bake.borderLayer) height = Mathf.Min(height, bake.hitArray[i].point.y);
        return height < float.MaxValue ? height : GetSkirtHeight(bake, point, out _);
    }

    // where a piece may stand: on the bare ground mesh (not a path, the river, a house or an existing tree) or out on the skirt
    private static bool TryFindGround(Bake bake, Vector2 point, float radius, bool allowInWater, out float height)
    {
        if (!bake.groundRect.Contains(point)) return TryFindSkirtGround(bake, point, radius, allowInWater, out height);

        height = 0f;
        Ray ray = new Ray(new Vector3(point.x, RAY_TOP, point.y), Vector3.down);
        int hitCount = Physics.RaycastNonAlloc(ray, bake.hitArray, RAY_TOP * 2f, ~0, QueryTriggerInteraction.Ignore);
        RaycastHit nearest = default;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = bake.hitArray[i];
            if (hit.collider.gameObject.layer == bake.borderLayer || hit.distance >= nearestDistance) continue;
            nearest = hit;
            nearestDistance = hit.distance;
        }
        if (nearestDistance == float.MaxValue) return TryFindSkirtGround(bake, point, radius, allowInWater, out height);

        if (nearest.collider != bake.groundCollider)
        {
            bool isWater = nearest.collider.GetComponentInParent<River>() != null;
            if (!(allowInWater && isWater)) return false;
        }
        height = nearest.point.y;
        if (radius <= 0f) return true;

        Vector3 foot = nearest.point;
        int overlapCount = Physics.OverlapCapsuleNonAlloc(foot + Vector3.up * .4f, foot + Vector3.up * 2.2f, radius * .8f, bake.overlapArray, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider other = bake.overlapArray[i];
            if (other == bake.groundCollider || other.gameObject.layer == bake.borderLayer || IsBlockedSurface(bake, other.name)) continue;
            return false;
        }
        return true;
    }

    private static bool TryFindSkirtGround(Bake bake, Vector2 point, float radius, bool allowInWater, out float height)
    {
        height = GetSkirtHeight(bake, point, out _);
        foreach (Corridor corridor in bake.corridorList)
        {
            float distance = corridor.GetDistance(point, out float along);
            if (along < -1f || along > corridor.length + radius) continue;
            if (corridor.isRiver)
            {
                if (allowInWater ? distance < corridor.halfWidth - 1.6f : distance < corridor.halfWidth + .8f + radius * .5f) return false;
            }
            else if (distance < corridor.halfWidth + .6f + radius) return false;
        }
        return true;
    }

    private static bool IsBlockedSurface(Bake bake, string objectName)
    {
        foreach (string blockedName in bake.layout.blockedSurfaceNameArray)
            if (!string.IsNullOrEmpty(blockedName) && objectName.Contains(blockedName)) return true;
        return false;
    }

    // ---------- ground skirt ----------

    private static void BuildGroundSkirt(Bake bake)
    {
        WorldBorderLayoutSO layout = bake.layout;
        Rect rect = bake.groundRect;
        float cell = Mathf.Max(.5f, layout.skirtCellSize);
        int marginCellCount = Mathf.CeilToInt(layout.skirtWidth / cell);
        float originX = rect.xMin - marginCellCount * cell;
        float originZ = rect.yMin - marginCellCount * cell;
        int columnCount = Mathf.CeilToInt(rect.width / cell) + marginCellCount * 2;
        int rowCount = Mathf.CeilToInt(rect.height / cell) + marginCellCount * 2;
        // cells fully under the ground mesh (one cell in from its edge) are left out; the outer ring tucks under its edge
        Rect hiddenRect = Rect.MinMaxRect(rect.xMin + cell, rect.yMin + cell, rect.xMax - cell, rect.yMax - cell);

        Vector3[] vertexArray = new Vector3[(columnCount + 1) * (rowCount + 1)];
        Color[] colorArray = new Color[vertexArray.Length];
        for (int row = 0; row <= rowCount; row++)
        {
            for (int column = 0; column <= columnCount; column++)
            {
                Vector2 point = new Vector2(originX + column * cell, originZ + row * cell);
                int index = row * (columnCount + 1) + column;
                vertexArray[index] = new Vector3(point.x, GetSkirtHeight(bake, point, out Color color), point.y);
                colorArray[index] = color;
            }
        }

        List<int> triangleList = new List<int>();
        for (int row = 0; row < rowCount; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                Vector2 cellMin = new Vector2(originX + column * cell, originZ + row * cell);
                if (hiddenRect.Contains(cellMin) && hiddenRect.Contains(cellMin + new Vector2(cell, cell))) continue;
                int a = row * (columnCount + 1) + column;
                int b = a + 1;
                int c = a + columnCount + 1;
                int d = c + 1;
                // clockwise seen from above, so the faces point up
                triangleList.Add(a); triangleList.Add(c); triangleList.Add(b);
                triangleList.Add(b); triangleList.Add(c); triangleList.Add(d);
            }
        }

        Mesh mesh = new Mesh { name = bake.sceneName + "_GroundSkirt", indexFormat = IndexFormat.UInt32 };
        mesh.vertices = vertexArray;
        mesh.colors = colorArray;
        mesh.SetTriangles(triangleList, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Mesh savedMesh = WorldBorderAssetGenerator.SaveMesh(mesh, WorldBorderAssetGenerator.GENERATED_MESH_FOLDER + "/" + mesh.name + ".asset");

        GameObject skirt = new GameObject("GroundSkirt");
        skirt.transform.SetParent(bake.root, false);
        skirt.AddComponent<MeshFilter>().sharedMesh = savedMesh;
        MeshRenderer skirtRenderer = skirt.AddComponent<MeshRenderer>();
        skirtRenderer.sharedMaterial = layout.groundMaterial;
        // the hills would throw long shadows across the edge of the map
        skirtRenderer.shadowCastingMode = ShadowCastingMode.Off;
        GameObjectUtility.SetStaticEditorFlags(skirt, StaticEditorFlags.BatchingStatic);
    }

    // height and colour of the skirt at a point: the ground mesh's own edge height and colour at the seam,
    // easing into hills further out, carved into a valley along rivers and kept flat and painted along roads
    private static float GetSkirtHeight(Bake bake, Vector2 point, out Color color)
    {
        WorldBorderLayoutSO layout = bake.layout;
        Rect rect = bake.groundRect;
        Vector2 onRect = new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax));
        float outside = Vector2.Distance(point, onRect);

        if (outside <= 0f)
        {
            // under the ground mesh: a hair above its edge (SEAM_LIFT), tucked below it a metre in
            float inside = Mathf.Min(Mathf.Min(point.x - rect.xMin, rect.xMax - point.x), Mathf.Min(point.y - rect.yMin, rect.yMax - point.y));
            Vector2 samplePoint = Vector2.MoveTowards(point, rect.center, .05f);
            float offset = SEAM_LIFT - (SEAM_LIFT + .08f) * Smooth(0f, 1f, inside);
            if (TrySampleGround(bake, samplePoint, out float groundHeight, out color)) return groundHeight + offset;
            color = layout.farGrassColor.linear;
            return offset;
        }

        Vector2 edgePoint = Vector2.MoveTowards(onRect, rect.center, .05f);
        if (!TrySampleGround(bake, edgePoint, out float edgeHeight, out Color edgeColor))
        {
            edgeHeight = 0f;
            edgeColor = layout.farGrassColor.linear;
        }

        float hillMask = Smooth(layout.hillStartDistance, layout.hillStartDistance + 30f, outside);
        float noise = Mathf.PerlinNoise(point.x * .025f + 31.7f, point.y * .025f + 11.3f);
        float bumps = (Mathf.PerlinNoise(point.x * .11f + 5.1f, point.y * .11f + 7.9f) - .5f) * .5f;
        float flatten = 1f;
        foreach (Corridor corridor in bake.corridorList)
        {
            float distance = corridor.GetDistance(point, out float along);
            if (along < 0f) continue;
            flatten = Mathf.Min(flatten, Smooth(corridor.halfWidth + 2f, corridor.halfWidth + 16f, distance));
        }

        float height = Mathf.Lerp(edgeHeight, 0f, Smooth(0f, 10f, outside)) + SEAM_LIFT * (1f - Smooth(4f, 10f, outside));
        height += bumps * Smooth(2f, 10f, outside) * flatten;
        height += layout.hillHeight * hillMask * (.35f + .65f * noise) * flatten;
        // the ground's edge colour fades into the far grass slowly, so the seam can't be seen
        color = Color.Lerp(edgeColor, layout.farGrassColor.linear, Smooth(0f, 18f, outside));
        color = Color.Lerp(color, layout.hillColor.linear, hillMask * (.4f + .6f * noise));

        foreach (Corridor corridor in bake.corridorList)
        {
            if (!corridor.isRiver) continue;
            float distance = corridor.GetDistance(point, out float along);
            if (along < 0f) continue;
            float bank = Smooth(corridor.halfWidth - 1.2f, corridor.halfWidth + 2.5f, distance);
            height = Mathf.Lerp(Mathf.Min(height, RIVER_BED_HEIGHT), height, bank);
            color = Color.Lerp(layout.bankColor.linear, color, Smooth(corridor.halfWidth - .5f, corridor.halfWidth + 1.2f, distance));
        }
        color.a = 1f;
        return height;
    }

    private static bool TrySampleGround(Bake bake, Vector2 point, out float height, out Color color)
    {
        Ray ray = new Ray(new Vector3(point.x, RAY_TOP, point.y), Vector3.down);
        if (!bake.groundCollider.Raycast(ray, out RaycastHit hit, RAY_TOP * 2f))
        {
            height = 0f;
            color = bake.layout.farGrassColor.linear;
            return false;
        }
        height = hit.point.y;
        color = bake.layout.farGrassColor.linear;
        int triangle = hit.triangleIndex * 3;
        if (bake.groundColorArray.Length > 0 && triangle >= 0 && triangle + 2 < bake.groundTriangleArray.Length)
        {
            Vector3 weight = hit.barycentricCoordinate;
            color = bake.groundColorArray[bake.groundTriangleArray[triangle]] * weight.x
                + bake.groundColorArray[bake.groundTriangleArray[triangle + 1]] * weight.y
                + bake.groundColorArray[bake.groundTriangleArray[triangle + 2]] * weight.z;
        }
        return true;
    }

    // ---------- roads ----------

    // each road carries on past the edge as a ribbon in the map's own path materials (centre + darker edges), lying on the
    // skirt and narrowing to nothing over its last 10 m. It starts 1.5 m inside the map, just under the map's own path end.
    private static void BuildRoads(Bake bake)
    {
        WorldBorderLayoutSO layout = bake.layout;
        if (layout.roadArray.Length == 0 || layout.roadMaterial == null) return;
        Transform roadRoot = CreateChild(bake.root, "Roads");
        const float overlap = 1.5f;
        const float edgeWidth = .35f;
        foreach (WorldBorderLayoutSO.Road road in layout.roadArray)
        {
            Vector2 direction = road.direction.normalized;
            Vector2 side = new Vector2(-direction.y, direction.x);
            int stepCount = Mathf.Max(1, Mathf.CeilToInt(road.length + overlap));
            float stepLength = (road.length + overlap) / stepCount;

            // four vertices across each row: outer left, inner left, inner right, outer right
            Vector3[] vertexArray = new Vector3[(stepCount + 1) * 4];
            for (int step = 0; step <= stepCount; step++)
            {
                float along = -overlap + step * stepLength;
                float half = road.width * .5f * (1f - Smooth(road.length - 10f, road.length, along)) + .05f;
                float edge = Mathf.Min(edgeWidth, half * .3f);
                Vector2 center = road.start + direction * along;
                float[] acrossArray = { -half, -half + edge, half - edge, half };
                for (int k = 0; k < 4; k++)
                {
                    Vector2 point = center + side * acrossArray[k];
                    float height = bake.groundRect.Contains(point) && TrySampleGround(bake, point, out float groundHeight, out _) ? groundHeight : GetSkirtHeight(bake, point, out _);
                    vertexArray[step * 4 + k] = new Vector3(point.x, height + .03f, point.y);
                }
            }

            List<int> centerList = new List<int>();
            List<int> edgeList = new List<int>();
            Vector3 acrossDirection = new Vector3(side.x, 0f, side.y);
            Vector3 alongDirection = new Vector3(direction.x, 0f, direction.y);
            bool isFlipped = Vector3.Cross(acrossDirection, alongDirection).y < 0f;
            for (int step = 0; step < stepCount; step++)
            {
                for (int k = 0; k < 3; k++)
                {
                    int a = step * 4 + k;
                    int b = a + 1;
                    int c = a + 4;
                    int d = c + 1;
                    List<int> list = k == 1 ? centerList : edgeList;
                    if (isFlipped) { list.Add(a); list.Add(c); list.Add(b); list.Add(b); list.Add(c); list.Add(d); }
                    else { list.Add(a); list.Add(b); list.Add(c); list.Add(b); list.Add(d); list.Add(c); }
                }
            }

            Mesh mesh = new Mesh { name = bake.sceneName + "_Road_" + road.label, vertices = vertexArray, subMeshCount = 2 };
            mesh.SetTriangles(centerList, 0);
            mesh.SetTriangles(edgeList, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Mesh savedMesh = WorldBorderAssetGenerator.SaveMesh(mesh, WorldBorderAssetGenerator.GENERATED_MESH_FOLDER + "/" + mesh.name + ".asset");

            GameObject roadObject = new GameObject("Road_" + road.label);
            roadObject.transform.SetParent(roadRoot, false);
            roadObject.AddComponent<MeshFilter>().sharedMesh = savedMesh;
            MeshRenderer roadRenderer = roadObject.AddComponent<MeshRenderer>();
            roadRenderer.sharedMaterials = new[] { layout.roadMaterial, layout.roadEdgeMaterial != null ? layout.roadEdgeMaterial : layout.roadMaterial };
            roadRenderer.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(roadObject, StaticEditorFlags.BatchingStatic);
        }
    }

    // ---------- river ----------

    private static void BuildRiverExtensions(Bake bake)
    {
        WorldBorderLayoutSO layout = bake.layout;
        if (string.IsNullOrEmpty(layout.riverObjectName)) return;
        GameObject riverObject = GameObject.Find(layout.riverObjectName);
        if (riverObject == null || !riverObject.TryGetComponent(out MeshFilter riverFilter) || riverFilter.sharedMesh == null)
        {
            Debug.LogWarning($"WorldBorderVisualBuilder: river '{layout.riverObjectName}' not found, no river extension");
            return;
        }

        Mesh riverMesh = riverFilter.sharedMesh;
        int columnCount = layout.riverColumnCount;
        int rowCount = columnCount > 1 ? riverMesh.vertexCount / columnCount : 0;
        if (rowCount < 2 || rowCount * columnCount != riverMesh.vertexCount)
        {
            Debug.LogWarning($"WorldBorderVisualBuilder: '{layout.riverObjectName}' is not a strip of {columnCount} columns, no river extension");
            return;
        }

        Vector3[] vertexArray = riverMesh.vertices;
        for (int i = 0; i < vertexArray.Length; i++) vertexArray[i] = riverObject.transform.TransformPoint(vertexArray[i]);
        Vector2[] uvArray = riverMesh.uv;
        River river = riverObject.GetComponent<River>();
        RiverFlowVisual riverFlowVisual = riverObject.GetComponent<RiverFlowVisual>();
        float metresPerRepeat = riverFlowVisual != null ? riverFlowVisual.GetMetresPerRepeat() : 8f;

        Transform extensionRoot = CreateChild(bake.root, "RiverExtensions");
        ExtendRiverEnd(bake, extensionRoot, "Start", vertexArray, uvArray, columnCount, 0, 1, metresPerRepeat, river);
        ExtendRiverEnd(bake, extensionRoot, "End", vertexArray, uvArray, columnCount, rowCount - 1, rowCount - 2, metresPerRepeat, river);
    }

    private static void ExtendRiverEnd(Bake bake, Transform parent, string label, Vector3[] vertexArray, Vector2[] uvArray, int columnCount,
        int endRow, int innerRow, float metresPerRepeat, River river)
    {
        Vector3 endCenter = RowCenter(vertexArray, endRow, columnCount);
        Vector3 innerCenter = RowCenter(vertexArray, innerRow, columnCount);
        Vector2 endPoint = new Vector2(endCenter.x, endCenter.z);
        Rect rect = bake.groundRect;
        float edgeDistance = Mathf.Min(Mathf.Min(endPoint.x - rect.xMin, rect.xMax - endPoint.x), Mathf.Min(endPoint.y - rect.yMin, rect.yMax - endPoint.y));
        if (edgeDistance > RIVER_END_TOLERANCE) return; // this end stays inside the map

        Vector2 direction = new Vector2(endCenter.x - innerCenter.x, endCenter.z - innerCenter.z).normalized;
        float vEnd = uvArray[endRow * columnCount].y;
        float vSign = Mathf.Sign(vEnd - uvArray[innerRow * columnCount].y);
        int stepCount = Mathf.Max(1, Mathf.CeilToInt(bake.layout.riverExtension));

        // rows one metre apart carrying on from the river's last row, v continuing at the same metres per repeat
        Vector3[] extensionVertexArray = new Vector3[(stepCount + 1) * columnCount];
        Vector2[] extensionUvArray = new Vector2[extensionVertexArray.Length];
        Vector3 offsetStep = new Vector3(direction.x, 0f, direction.y);
        for (int step = 0; step <= stepCount; step++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                int index = step * columnCount + column;
                extensionVertexArray[index] = vertexArray[endRow * columnCount + column] + offsetStep * step;
                extensionUvArray[index] = new Vector2(uvArray[endRow * columnCount + column].x, vEnd + vSign * step / metresPerRepeat);
            }
        }
        List<int> triangleList = new List<int>();
        Vector3 across = extensionVertexArray[1] - extensionVertexArray[0];
        bool isFlipped = Vector3.Cross(across, offsetStep).y < 0f;
        for (int step = 0; step < stepCount; step++)
        {
            for (int column = 0; column < columnCount - 1; column++)
            {
                int a = step * columnCount + column;
                int b = a + 1;
                int c = a + columnCount;
                int d = c + 1;
                if (isFlipped)
                {
                    triangleList.Add(a); triangleList.Add(c); triangleList.Add(b);
                    triangleList.Add(b); triangleList.Add(c); triangleList.Add(d);
                }
                else
                {
                    triangleList.Add(a); triangleList.Add(b); triangleList.Add(c);
                    triangleList.Add(b); triangleList.Add(d); triangleList.Add(c);
                }
            }
        }

        Mesh mesh = new Mesh { name = bake.sceneName + "_RiverExtension_" + label };
        mesh.vertices = extensionVertexArray;
        mesh.uv = extensionUvArray;
        mesh.SetTriangles(triangleList, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        Mesh savedMesh = WorldBorderAssetGenerator.SaveMesh(mesh, WorldBorderAssetGenerator.GENERATED_MESH_FOLDER + "/" + mesh.name + ".asset");

        GameObject extension = new GameObject("RiverExtension_" + label);
        extension.transform.SetParent(parent, false);
        extension.AddComponent<MeshFilter>().sharedMesh = savedMesh;
        MeshRenderer extensionRenderer = extension.AddComponent<MeshRenderer>();
        extensionRenderer.sharedMaterial = bake.layout.riverMaterial;
        extensionRenderer.shadowCastingMode = ShadowCastingMode.Off;
        if (river != null)
        {
            // scrolls with the river (both start at offset 0 and move at its current speed), so the seam never shows
            RiverFlowVisual flowVisual = extension.AddComponent<RiverFlowVisual>();
            SerializedObject serializedFlow = new SerializedObject(flowVisual);
            serializedFlow.FindProperty("river").objectReferenceValue = river;
            serializedFlow.FindProperty("metresPerRepeat").floatValue = metresPerRepeat;
            serializedFlow.ApplyModifiedPropertiesWithoutUndo();
        }

        float halfWidth = (vertexArray[endRow * columnCount] - vertexArray[endRow * columnCount + columnCount - 1]).magnitude * .5f;
        float length = stepCount;
        bake.corridorList.Add(new Corridor { start = endPoint, direction = direction, halfWidth = halfWidth, length = length, isRiver = true });

        if (bake.layout.riverBankStyleSO == null) return;
        Vector2 side = new Vector2(-direction.y, direction.x);
        for (int sign = -1; sign <= 1; sign += 2)
        {
            bake.bankStripList.Add(new PlacementStrip {
                label = "RiverBank_" + label + (sign < 0 ? "_Left" : "_Right"),
                borderStyleSO = bake.layout.riverBankStyleSO,
                start = endPoint - direction * 1.5f,
                end = endPoint + direction * length,
                outward = side * sign,
                nearOffset = halfWidth - 1.2f,
                farOffset = halfWidth + 3.5f,
                seed = 900 + endRow + sign,
            });
        }
    }

    private static Vector3 RowCenter(Vector3[] vertexArray, int row, int columnCount)
    {
        Vector3 sum = Vector3.zero;
        for (int column = 0; column < columnCount; column++) sum += vertexArray[row * columnCount + column];
        return sum / columnCount;
    }

    // ---------- mountains and haze ----------

    private static int PlaceMountains(Bake bake, Transform parent)
    {
        WorldBorderLayoutSO layout = bake.layout;
        int prefabCount = 0;
        foreach (Transform prefab in layout.mountainPrefabArray) if (prefab != null) prefabCount++;
        if (prefabCount == 0 || layout.mountainCount <= 0) return 0;

        System.Random random = new System.Random(layout.mountainSeed);
        Rect rect = bake.groundRect;
        Vector2 center = rect.center;
        int placedCount = 0;
        for (int i = 0; i < layout.mountainCount; i++)
        {
            float angle = (i + Signed(random) * .3f) / layout.mountainCount * Mathf.PI * 2f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            // from the centre to the ground's edge along this direction
            float toEdgeX = Mathf.Abs(direction.x) > .001f ? rect.width * .5f / Mathf.Abs(direction.x) : float.MaxValue;
            float toEdgeZ = Mathf.Abs(direction.y) > .001f ? rect.height * .5f / Mathf.Abs(direction.y) : float.MaxValue;
            float radius = Mathf.Lerp(layout.mountainWidthRange.x, layout.mountainWidthRange.y, (float)random.NextDouble());
            float height = Mathf.Lerp(layout.mountainHeightRange.x, layout.mountainHeightRange.y, (float)random.NextDouble());
            // the gap is measured from the ground's edge to the mountain's foot, so no slope reaches into the map
            float gap = Mathf.Lerp(layout.mountainDistanceRange.x, layout.mountainDistanceRange.y, (float)random.NextDouble());
            Vector2 point = center + direction * (Mathf.Min(toEdgeX, toEdgeZ) + gap + radius);

            Transform prefab = layout.mountainPrefabArray[random.Next(layout.mountainPrefabArray.Length)];
            float yaw = (float)random.NextDouble() * 360f;
            if (prefab == null) continue;
            Spawn(prefab, parent, point, -2f, yaw, new Vector3(radius, height, radius), Quaternion.identity);
            placedCount++;
        }
        return placedCount;
    }

    private static void ApplyHaze(WorldBorderLayoutSO layout)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = layout.hazeColor;
        RenderSettings.fogStartDistance = layout.hazeStart;
        RenderSettings.fogEndDistance = layout.hazeEnd;
    }

    private static float Smooth(float from, float to, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, value));
    private static float Signed(System.Random random) => (float)random.NextDouble() * 2f - 1f;
}
