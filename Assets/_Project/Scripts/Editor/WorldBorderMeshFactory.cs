using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Low-poly, flat-shaded meshes for the world border, built from code because the art kit has no rocks, reeds, gates or
// mountains: rocks, cliffs, reed clumps, mountains, fence pieces and gates. Colours are vertex colours, which the map's
// glTF ground material (M_Terrain) reads; the wooden pieces have two submeshes for M_Wood_Dark (posts) and M_Wood (boards).
public static class WorldBorderMeshFactory
{
    public const int POST_SUBMESH = 0;
    public const int BOARD_SUBMESH = 1;
    public const float FENCE_LENGTH = 2.4f;
    public const float GATE_WIDTH = 5.6f;

    // vertex colours are read as linear, so the colours as you'd pick them are converted
    private static readonly Color ROCK_COLOR = new Color(.612f, .592f, .557f).linear; // M_Rock
    private static readonly Color MOSS_COLOR = new Color(.42f, .56f, .26f).linear;
    private static readonly Color REED_BASE_COLOR = new Color(.33f, .5f, .2f).linear;
    private static readonly Color REED_TIP_COLOR = new Color(.66f, .7f, .36f).linear;
    private static readonly Color CATTAIL_COLOR = new Color(.45f, .3f, .17f).linear;
    private static readonly Color MOUNTAIN_FOREST_COLOR = new Color(.3f, .48f, .3f).linear;
    private static readonly Color MOUNTAIN_ROCK_COLOR = new Color(.5f, .56f, .62f).linear;
    private static readonly Color MOUNTAIN_SNOW_COLOR = new Color(.92f, .94f, .97f).linear;

    private class FlatMesh
    {
        private readonly List<Vector3> vertexList = new List<Vector3>();
        private readonly List<Color> colorList = new List<Color>();
        private readonly List<int>[] triangleListArray;

        public FlatMesh(int submeshCount)
        {
            triangleListArray = new List<int>[submeshCount];
            for (int i = 0; i < submeshCount; i++) triangleListArray[i] = new List<int>();
        }

        // every triangle gets its own vertices (flat shading); the front face is the side `outward` points to
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward, Color colorA, Color colorB, Color colorC, int submesh = 0)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f)
            {
                (b, c) = (c, b);
                (colorB, colorC) = (colorC, colorB);
            }
            int index = vertexList.Count;
            vertexList.Add(a);
            vertexList.Add(b);
            vertexList.Add(c);
            colorList.Add(colorA);
            colorList.Add(colorB);
            colorList.Add(colorC);
            triangleListArray[submesh].Add(index);
            triangleListArray[submesh].Add(index + 1);
            triangleListArray[submesh].Add(index + 2);
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward, Color color, int submesh = 0) => Triangle(a, b, c, outward, color, color, color, submesh);

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, Color color, int submesh = 0)
        {
            Triangle(a, b, c, outward, color, submesh);
            Triangle(a, c, d, outward, color, submesh);
        }

        public void Box(Vector3 center, Vector3 size, Quaternion rotation, Color color, int submesh = 0)
        {
            Vector3[] axisArray = { Vector3.right, Vector3.up, Vector3.forward };
            Vector3 half = size * .5f;
            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 normal = axisArray[axis] * sign;
                    Vector3 u = axisArray[(axis + 1) % 3];
                    Vector3 v = axisArray[(axis + 2) % 3];
                    Vector3 Corner(float cu, float cv) => center + rotation * Vector3.Scale(normal + u * cu + v * cv, half);
                    Quad(Corner(-1f, -1f), Corner(1f, -1f), Corner(1f, 1f), Corner(-1f, 1f), rotation * normal, color, submesh);
                }
            }
        }

        public Mesh ToMesh(string name)
        {
            Mesh mesh = new Mesh { name = name };
            if (vertexList.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertexList);
            mesh.SetColors(colorList);
            mesh.subMeshCount = triangleListArray.Length;
            for (int i = 0; i < triangleListArray.Length; i++) mesh.SetTriangles(triangleListArray[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    // radius ~1 at scale 1, flat bottom sunk a little below y 0
    public static Mesh Rock(string name, int seed, Vector3 stretch, float noise, float mossiness)
    {
        System.Random random = new System.Random(seed);
        List<Vector3> pointList = Icosphere(out List<int[]> faceList);
        for (int i = 0; i < pointList.Count; i++)
        {
            float radius = 1f + Signed(random) * noise;
            Vector3 point = Vector3.Scale(pointList[i] * radius, stretch);
            point.y = Mathf.Max(point.y, -.3f * stretch.y);
            pointList[i] = point;
        }

        FlatMesh flatMesh = new FlatMesh(1);
        foreach (int[] face in faceList)
        {
            Vector3 a = pointList[face[0]], b = pointList[face[1]], c = pointList[face[2]];
            Vector3 centroid = (a + b + c) / 3f;
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            if (Vector3.Dot(normal, centroid) < 0f) normal = -normal;

            Color color = ROCK_COLOR * (.86f + (float)random.NextDouble() * .18f);
            if (normal.y > .5f) color = Color.Lerp(color, MOSS_COLOR, mossiness * Mathf.InverseLerp(.5f, .95f, normal.y));
            color.a = 1f;
            flatMesh.Triangle(a, b, c, centroid, color);
        }
        return flatMesh.ToMesh(name);
    }

    // a clump of reed blades within ~0.45 m, ~1.4 m tall at scale 1, some with cattail heads
    public static Mesh Reeds(string name, int seed, int bladeCount)
    {
        System.Random random = new System.Random(seed);
        FlatMesh flatMesh = new FlatMesh(1);
        for (int i = 0; i < bladeCount; i++)
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float distance = Mathf.Sqrt((float)random.NextDouble()) * .45f;
            Vector3 root = new Vector3(Mathf.Cos(angle) * distance, -.1f, Mathf.Sin(angle) * distance);
            float height = .7f + (float)random.NextDouble() * .7f;
            float leanAngle = (float)random.NextDouble() * Mathf.PI * 2f;
            Vector3 lean = new Vector3(Mathf.Cos(leanAngle), 0f, Mathf.Sin(leanAngle)) * height * (.08f + (float)random.NextDouble() * .2f);
            float yaw = (float)random.NextDouble() * Mathf.PI;
            Vector3 side = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw)) * .035f;

            Vector3 left = root - side;
            Vector3 right = root + side;
            Vector3 tip = root + lean + Vector3.up * height;
            Vector3 normal = Vector3.Cross(right - left, tip - left);
            // both sides, so the blade shows from every angle
            flatMesh.Triangle(left, right, tip, normal, REED_BASE_COLOR, REED_BASE_COLOR, REED_TIP_COLOR);
            flatMesh.Triangle(left, right, tip, -normal, REED_BASE_COLOR, REED_BASE_COLOR, REED_TIP_COLOR);

            if (random.NextDouble() < .3)
            {
                Vector3 head = Vector3.Lerp(root, tip, .82f);
                flatMesh.Box(head, new Vector3(.07f, .2f, .07f), Quaternion.FromToRotation(Vector3.up, (tip - root).normalized), CATTAIL_COLOR);
            }
        }
        return flatMesh.ToMesh(name);
    }

    // radius 1 and height 1 at scale 1: forested foot, bare slate shoulders, optional snow cap
    public static Mesh Mountain(string name, int seed, bool hasSnow)
    {
        System.Random random = new System.Random(seed);
        int sideCount = 9 + random.Next(4);
        float[] ringHeightArray = { -.05f, .32f, .62f, .86f };
        float[] ringRadiusArray = { 1f, .72f, .44f, .2f };
        Vector2 peakShift = new Vector2(Signed(random), Signed(random)) * .1f;

        Vector3[][] ringArray = new Vector3[ringHeightArray.Length][];
        for (int ring = 0; ring < ringHeightArray.Length; ring++)
        {
            ringArray[ring] = new Vector3[sideCount];
            float drift = Mathf.InverseLerp(0f, 1f, ringHeightArray[ring]);
            for (int k = 0; k < sideCount; k++)
            {
                float angle = (k + Signed(random) * .25f) / sideCount * Mathf.PI * 2f;
                float radius = ringRadiusArray[ring] * (.86f + (float)random.NextDouble() * .26f);
                float height = ringHeightArray[ring] + (ring == 0 ? 0f : Signed(random) * .04f);
                ringArray[ring][k] = new Vector3(Mathf.Cos(angle) * radius + peakShift.x * drift, height, Mathf.Sin(angle) * radius + peakShift.y * drift);
            }
        }
        Vector3 peak = new Vector3(peakShift.x, 1f, peakShift.y);

        FlatMesh flatMesh = new FlatMesh(1);
        for (int ring = 0; ring < ringArray.Length; ring++)
        {
            for (int k = 0; k < sideCount; k++)
            {
                Vector3 a = ringArray[ring][k];
                Vector3 b = ringArray[ring][(k + 1) % sideCount];
                bool isTop = ring == ringArray.Length - 1;
                Vector3 c = isTop ? peak : ringArray[ring + 1][(k + 1) % sideCount];
                Vector3 d = isTop ? peak : ringArray[ring + 1][k];

                AddMountainTriangle(flatMesh, a, b, c, random, hasSnow);
                if (!isTop) AddMountainTriangle(flatMesh, a, c, d, random, hasSnow);
            }
        }
        return flatMesh.ToMesh(name);
    }

    // one post, 1.3 m tall, at the origin
    public static Mesh FencePost(string name)
    {
        FlatMesh flatMesh = new FlatMesh(2);
        AddPost(flatMesh, Vector3.zero, .16f, 1.3f);
        return flatMesh.ToMesh(name);
    }

    // a post at z 0 and two rails running FENCE_LENGTH along +z (the next segment's post closes it)
    public static Mesh FenceSegment(string name)
    {
        FlatMesh flatMesh = new FlatMesh(2);
        AddPost(flatMesh, Vector3.zero, .16f, 1.3f);
        foreach (float railHeight in new[] { .45f, .92f })
            flatMesh.Box(new Vector3(0f, railHeight, FENCE_LENGTH * .5f), new Vector3(.07f, .11f, FENCE_LENGTH), Quaternion.identity, Color.white, BOARD_SUBMESH);
        return flatMesh.ToMesh(name);
    }

    // posts at z 0 and z GATE_WIDTH, no beam overhead (the truck drives through); open leaves swing toward +x
    public static Mesh Gate(string name, bool isOpen)
    {
        FlatMesh flatMesh = new FlatMesh(2);
        AddPost(flatMesh, Vector3.zero, .26f, 2.2f);
        AddPost(flatMesh, new Vector3(0f, 0f, GATE_WIDTH), .26f, 2.2f);

        float swing = isOpen ? 78f : 0f;
        AddGateLeaf(flatMesh, new Vector3(0f, 0f, .16f), Quaternion.Euler(0f, swing, 0f));
        AddGateLeaf(flatMesh, new Vector3(0f, 0f, GATE_WIDTH - .16f), Quaternion.Euler(0f, 180f - swing, 0f));
        return flatMesh.ToMesh(name);
    }

    private static void AddMountainTriangle(FlatMesh flatMesh, Vector3 a, Vector3 b, Vector3 c, System.Random random, bool hasSnow)
    {
        Vector3 centroid = (a + b + c) / 3f;
        float height = centroid.y;
        Color color;
        if (height < .3f) color = MOUNTAIN_FOREST_COLOR;
        else if (height < .45f) color = Color.Lerp(MOUNTAIN_FOREST_COLOR, MOUNTAIN_ROCK_COLOR, (float)random.NextDouble());
        else if (hasSnow && height > .8f) color = MOUNTAIN_SNOW_COLOR;
        else color = MOUNTAIN_ROCK_COLOR;
        color *= .92f + (float)random.NextDouble() * .12f;
        color.a = 1f;
        flatMesh.Triangle(a, b, c, new Vector3(centroid.x, .3f, centroid.z), color);
    }

    private static void AddPost(FlatMesh flatMesh, Vector3 foot, float thickness, float height)
    {
        // sunk 0.1 m so it stands on uneven ground; a low pyramid cap keeps it from looking like a crate leg
        flatMesh.Box(foot + new Vector3(0f, height * .5f - .1f, 0f), new Vector3(thickness, height, thickness), Quaternion.identity, Color.white, POST_SUBMESH);
        float top = height - .1f;
        float half = thickness * .5f;
        Vector3 apex = foot + new Vector3(0f, top + thickness * .6f, 0f);
        Vector3[] cornerArray = {
            foot + new Vector3(-half, top, -half), foot + new Vector3(half, top, -half),
            foot + new Vector3(half, top, half), foot + new Vector3(-half, top, half),
        };
        for (int i = 0; i < 4; i++)
        {
            Vector3 a = cornerArray[i];
            Vector3 b = cornerArray[(i + 1) % 4];
            flatMesh.Triangle(a, b, apex, (a + b) * .5f - foot - new Vector3(0f, top, 0f) + Vector3.up * .1f, Color.white, POST_SUBMESH);
        }
    }

    // a leaf built along its own +z from the hinge: two rails, five pickets and a diagonal brace
    private static void AddGateLeaf(FlatMesh flatMesh, Vector3 hinge, Quaternion rotation)
    {
        float width = GATE_WIDTH * .5f - .22f;
        void Board(Vector3 localCenter, Vector3 size, Quaternion localRotation) =>
            flatMesh.Box(hinge + rotation * localCenter, size, rotation * localRotation, Color.white, BOARD_SUBMESH);

        Board(new Vector3(0f, 1.15f, width * .5f), new Vector3(.08f, .13f, width), Quaternion.identity);
        Board(new Vector3(0f, .35f, width * .5f), new Vector3(.08f, .13f, width), Quaternion.identity);
        for (int i = 0; i < 5; i++)
            Board(new Vector3(.03f, .76f, width * (i + .5f) / 5f), new Vector3(.05f, 1.05f, .13f), Quaternion.identity);

        Vector3 braceStart = new Vector3(-.03f, .35f, .1f);
        Vector3 braceEnd = new Vector3(-.03f, 1.15f, width - .1f);
        Vector3 brace = braceEnd - braceStart;
        Board((braceStart + braceEnd) * .5f, new Vector3(.05f, .1f, brace.magnitude), Quaternion.FromToRotation(Vector3.forward, brace.normalized));
    }

    private static float Signed(System.Random random) => (float)random.NextDouble() * 2f - 1f;

    // a once-subdivided icosahedron (42 points, 80 faces) on the unit sphere
    private static List<Vector3> Icosphere(out List<int[]> faceList)
    {
        float t = (1f + Mathf.Sqrt(5f)) * .5f;
        List<Vector3> pointList = new List<Vector3> {
            new Vector3(-1f, t, 0f), new Vector3(1f, t, 0f), new Vector3(-1f, -t, 0f), new Vector3(1f, -t, 0f),
            new Vector3(0f, -1f, t), new Vector3(0f, 1f, t), new Vector3(0f, -1f, -t), new Vector3(0f, 1f, -t),
            new Vector3(t, 0f, -1f), new Vector3(t, 0f, 1f), new Vector3(-t, 0f, -1f), new Vector3(-t, 0f, 1f),
        };
        for (int i = 0; i < pointList.Count; i++) pointList[i] = pointList[i].normalized;
        int[][] baseFaceArray = {
            new[] { 0, 11, 5 }, new[] { 0, 5, 1 }, new[] { 0, 1, 7 }, new[] { 0, 7, 10 }, new[] { 0, 10, 11 },
            new[] { 1, 5, 9 }, new[] { 5, 11, 4 }, new[] { 11, 10, 2 }, new[] { 10, 7, 6 }, new[] { 7, 1, 8 },
            new[] { 3, 9, 4 }, new[] { 3, 4, 2 }, new[] { 3, 2, 6 }, new[] { 3, 6, 8 }, new[] { 3, 8, 9 },
            new[] { 4, 9, 5 }, new[] { 2, 4, 11 }, new[] { 6, 2, 10 }, new[] { 8, 6, 7 }, new[] { 9, 8, 1 },
        };

        Dictionary<long, int> midpointDictionary = new Dictionary<long, int>();
        int Midpoint(int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (midpointDictionary.TryGetValue(key, out int index)) return index;
            pointList.Add(((pointList[a] + pointList[b]) * .5f).normalized);
            midpointDictionary[key] = pointList.Count - 1;
            return pointList.Count - 1;
        }

        faceList = new List<int[]>();
        foreach (int[] face in baseFaceArray)
        {
            int ab = Midpoint(face[0], face[1]);
            int bc = Midpoint(face[1], face[2]);
            int ca = Midpoint(face[2], face[0]);
            faceList.Add(new[] { face[0], ab, ca });
            faceList.Add(new[] { face[1], bc, ab });
            faceList.Add(new[] { face[2], ca, bc });
            faceList.Add(new[] { ab, bc, ca });
        }
        return pointList;
    }
}
