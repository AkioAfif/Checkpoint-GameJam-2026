using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

// Bakes soft, bevelled graybox meshes as .asset files. Every mesh has its pivot at the bottom centre.
public static class GrayboxMeshFactory
{
    private const string FOLDER = "Assets/_Project/Art/Models/Generated";

    public static Mesh RoundedBox(Vector3 size, float radius)
    {
        radius = Mathf.Min(radius, size.x * .5f, size.y * .5f, size.z * .5f);
        return GetOrCreate($"RBox_{F(size.x)}x{F(size.y)}x{F(size.z)}_r{F(radius)}", () => BuildRoundedBox(size, radius));
    }

    public static Mesh Cylinder(float radius, float height, float bevel, int segments = 20)
    {
        bevel = Mathf.Min(bevel, radius, height * .5f);
        return GetOrCreate($"Cyl_{F(radius)}x{F(height)}_b{F(bevel)}_s{segments}", () => Lathe(BevelCylinderProfile(radius, radius, height, bevel), segments));
    }

    // tapered cylinder: radiusTop < radiusBottom gives a soft cone / tree trunk / pot
    public static Mesh Taper(float radiusBottom, float radiusTop, float height, float bevel, int segments = 20)
    {
        return GetOrCreate($"Tap_{F(radiusBottom)}_{F(radiusTop)}x{F(height)}_b{F(bevel)}_s{segments}", () => Lathe(BevelCylinderProfile(radiusBottom, radiusTop, height, bevel), segments));
    }

    // squashed sphere (blob) of the given radius and height, used for heads, bushes, foliage, seeds
    public static Mesh Blob(float radius, float height, int segments = 16, int rings = 8)
    {
        return GetOrCreate($"Blob_{F(radius)}x{F(height)}_s{segments}_r{rings}", () =>
        {
            List<Vector2> points = new List<Vector2>();
            List<Vector2> normals = new List<Vector2>();
            for (int i = 0; i <= rings; i++)
            {
                float a = Mathf.Lerp(-90f, 90f, (float)i / rings) * Mathf.Deg2Rad;
                points.Add(new Vector2(Mathf.Cos(a) * radius, (Mathf.Sin(a) + 1f) * height * .5f));
                normals.Add(new Vector2(Mathf.Cos(a) / radius, Mathf.Sin(a) / (height * .5f)).normalized);
            }
            return Lathe(new Profile(points, normals), segments);
        });
    }

    // donut lying flat, used for the Fabricator rings and the bottle neck
    public static Mesh Torus(float radius, float thickness, int segments = 24, int sides = 8)
    {
        return GetOrCreate($"Torus_{F(radius)}_{F(thickness)}_s{segments}", () =>
        {
            List<Vector2> points = new List<Vector2>();
            List<Vector2> normals = new List<Vector2>();
            for (int i = 0; i <= sides; i++)
            {
                float a = (-90f - 360f * i / sides) * Mathf.Deg2Rad;
                Vector2 n = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                points.Add(new Vector2(radius, thickness) + n * thickness);
                normals.Add(n);
            }
            return Lathe(new Profile(points, normals), segments);
        });
    }

    // inverted-hull outline: same mesh pushed out along its normals (drawn back-faces-only by the outline material)
    public static Mesh Outline(Mesh source, float thickness)
    {
        return GetOrCreate($"{source.name}_outline{F(thickness)}", () =>
        {
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            for (int i = 0; i < vertices.Length; i++) vertices[i] += normals[i] * thickness;
            // bone data is empty for static meshes; skinned sources keep deforming with their rig
            Mesh mesh = new Mesh { vertices = vertices, normals = normals, triangles = source.triangles, boneWeights = source.boneWeights, bindposes = source.bindposes };
            mesh.RecalculateBounds();
            return mesh;
        });
    }

    private static Mesh BuildRoundedBox(Vector3 size, float r)
    {
        Vector3 h = size * .5f;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        // 6 faces: (normal axis, u axis, v axis)
        Vector3[,] faces =
        {
            { Vector3.right, Vector3.forward, Vector3.up }, { Vector3.left, Vector3.forward, Vector3.up },
            { Vector3.up, Vector3.right, Vector3.forward }, { Vector3.down, Vector3.right, Vector3.forward },
            { Vector3.forward, Vector3.right, Vector3.up }, { Vector3.back, Vector3.right, Vector3.up },
        };
        for (int f = 0; f < 6; f++)
        {
            Vector3 n = faces[f, 0], u = faces[f, 1], v = faces[f, 2];
            float[] uCoords = BevelCoords(Vector3.Scale(u, h).magnitude, r);
            float[] vCoords = BevelCoords(Vector3.Scale(v, h).magnitude, r);
            float nDist = Vector3.Scale(n, h).magnitude;
            int start = vertices.Count;
            for (int j = 0; j < vCoords.Length; j++)
            {
                for (int i = 0; i < uCoords.Length; i++)
                {
                    Vector3 p = n * nDist + u * uCoords[i] + v * vCoords[j];
                    Vector3 inner = new Vector3(Mathf.Clamp(p.x, -h.x + r, h.x - r), Mathf.Clamp(p.y, -h.y + r, h.y - r), Mathf.Clamp(p.z, -h.z + r, h.z - r));
                    Vector3 normal = (p - inner).normalized;
                    vertices.Add(inner + normal * r + Vector3.up * h.y);
                    normals.Add(normal);
                    uvs.Add(new Vector2((float)i / (uCoords.Length - 1), (float)j / (vCoords.Length - 1)));
                }
            }
            int w = uCoords.Length;
            for (int j = 0; j < vCoords.Length - 1; j++)
            {
                for (int i = 0; i < w - 1; i++)
                {
                    int a = start + j * w + i;
                    AddQuad(triangles, vertices, a, a + 1, a + w + 1, a + w, n);
                }
            }
        }
        return CreateMesh(vertices, normals, uvs, triangles);
    }

    // grid lines: hard face, then 2 bevel steps, then the flat middle
    private static float[] BevelCoords(float half, float r)
    {
        if (r <= 0.0001f) return new[] { -half, half };
        return new[] { -half, -half + r * .5f, -half + r, half - r, half - r * .5f, half };
    }

    private struct Profile
    {
        public List<Vector2> points;
        public List<Vector2> normals;
        public Profile(List<Vector2> points, List<Vector2> normals) { this.points = points; this.normals = normals; }
    }

    private static Profile BevelCylinderProfile(float radiusBottom, float radiusTop, float height, float bevel)
    {
        List<Vector2> points = new List<Vector2>();
        List<Vector2> normals = new List<Vector2>();
        Vector2 side = new Vector2(height, radiusBottom - radiusTop).normalized;
        points.Add(new Vector2(0f, 0f)); normals.Add(Vector2.down);
        AddArc(points, normals, new Vector2(radiusBottom - bevel, bevel), bevel, Vector2.down, side);
        AddArc(points, normals, new Vector2(radiusTop - bevel, height - bevel), bevel, side, Vector2.up);
        points.Add(new Vector2(0f, height)); normals.Add(Vector2.up);
        return new Profile(points, normals);
    }

    private static void AddArc(List<Vector2> points, List<Vector2> normals, Vector2 centre, float radius, Vector2 fromNormal, Vector2 toNormal)
    {
        const int STEPS = 3;
        for (int i = 0; i <= STEPS; i++)
        {
            Vector2 n = Vector3.Slerp(fromNormal, toNormal, (float)i / STEPS);
            points.Add(centre + n * radius);
            normals.Add(n);
        }
    }

    // revolves a (radius, height) profile around the Y axis
    private static Mesh Lathe(Profile profile, int segments)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        int count = profile.points.Count;
        for (int s = 0; s <= segments; s++)
        {
            float a = 360f * s / segments * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            for (int k = 0; k < count; k++)
            {
                Vector2 p = profile.points[k];
                Vector2 n = profile.normals[k];
                vertices.Add(dir * p.x + Vector3.up * p.y);
                normals.Add((dir * n.x + Vector3.up * n.y).normalized);
                uvs.Add(new Vector2((float)s / segments, (float)k / (count - 1)));
            }
        }
        for (int s = 0; s < segments; s++)
        {
            for (int k = 0; k < count - 1; k++)
            {
                int a = s * count + k;
                Vector3 outward = (normals[a] + normals[a + 1] + normals[a + count] + normals[a + count + 1]).normalized;
                AddQuad(triangles, vertices, a, a + count, a + count + 1, a + 1, outward);
            }
        }
        return CreateMesh(vertices, normals, uvs, triangles);
    }

    // winding is fixed per triangle so each one faces along 'outward' (Unity front face = cross(b-a, c-a))
    private static void AddQuad(List<int> triangles, List<Vector3> vertices, int a, int b, int c, int d, Vector3 outward)
    {
        AddTriangle(triangles, vertices, a, b, c, outward);
        AddTriangle(triangles, vertices, a, c, d, outward);
    }

    private static void AddTriangle(List<int> triangles, List<Vector3> vertices, int a, int b, int c, Vector3 outward)
    {
        Vector3 cross = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
        if (cross.sqrMagnitude < 1e-12f) return; // collapsed pole triangle
        if (Vector3.Dot(cross, outward) >= 0f) triangles.AddRange(new[] { a, b, c });
        else triangles.AddRange(new[] { a, c, b });
    }

    private static Mesh CreateMesh(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
    {
        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    private static Mesh GetOrCreate(string key, System.Func<Mesh> build)
    {
        string path = $"{FOLDER}/{key}.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        if (!AssetDatabase.IsValidFolder(FOLDER)) AssetDatabase.CreateFolder("Assets/_Project/Art/Models", "Generated");
        Mesh mesh = build();
        mesh.name = key;
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', 'p');
}
