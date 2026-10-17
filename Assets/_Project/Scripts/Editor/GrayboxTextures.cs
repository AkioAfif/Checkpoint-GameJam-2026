using System.IO;
using UnityEditor;
using UnityEngine;

// Procedural UI/VFX textures, saved as PNG sprites so they work in uGUI Images and particle materials.
public static class GrayboxTextures
{
    private const string FOLDER = "Assets/_Project/Art/Textures";

    public static Sprite SoftCircle => GetOrCreate("SoftCircle", 128, 0, (x, y) => Mathf.Clamp01(1f - Dist(x, y)) * Mathf.Clamp01(1f - Dist(x, y)));
    public static Sprite Circle => GetOrCreate("Circle", 128, 0, (x, y) => Edge(1f - Dist(x, y), 128));
    public static Sprite Ring => GetOrCreate("Ring", 256, 0, (x, y) => Edge(1f - Dist(x, y), 256) * Edge(Dist(x, y) - .86f, 256));
    // black everywhere except a transparent circle in the middle (iris wipe)
    public static Sprite IrisHole => GetOrCreate("IrisHole", 512, 0, (x, y) => 1f - Edge(1f - Dist(x, y), 512));
    // 9-sliced rounded panel for every UI box
    public static Sprite RoundedPanel => GetOrCreate("RoundedPanel", 64, 24, (x, y) => Edge(RoundedRect(x, y, 24f / 32f), 64));

    public static Texture2D SoftCircleTexture => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(SoftCircle));

    // x, y in -1..1
    private static float Dist(float x, float y) => Mathf.Sqrt(x * x + y * y);

    private static float RoundedRect(float x, float y, float r)
    {
        float qx = Mathf.Abs(x) - (1f - r), qy = Mathf.Abs(y) - (1f - r);
        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
        return r - outside - Mathf.Min(Mathf.Max(qx, qy), 0f);
    }

    // signed distance (in -1..1 units) → anti-aliased 0..1 coverage
    private static float Edge(float signedDistance, int size) => Mathf.Clamp01(signedDistance * size * .5f + .5f);

    private static Sprite GetOrCreate(string name, int size, int border, System.Func<float, float, float> alpha)
    {
        string path = $"{FOLDER}/{name}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size * 2f - 1f, v = (y + .5f) / size * 2f - 1f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(u, v)));
            }
        }
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.spriteBorder = new Vector4(border, border, border, border);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
