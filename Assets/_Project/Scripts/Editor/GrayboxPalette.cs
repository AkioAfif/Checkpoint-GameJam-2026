using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Cozy pastel-gray materials. Every Get() creates the material once, then returns the saved asset.
public static class GrayboxPalette
{
    private const string FOLDER = "Assets/_Project/Art/Materials";
    private const string LIT = "Universal Render Pipeline/Lit";
    private const string UNLIT = "Universal Render Pipeline/Unlit";
    private const string PARTICLES = "Universal Render Pipeline/Particles/Unlit";

    public static Material Grass => Lit("Grass", new Color(.64f, .73f, .58f));
    public static Material Path => Lit("Path", new Color(.83f, .78f, .69f));
    public static Material Soil => Lit("Soil", new Color(.56f, .47f, .41f));
    public static Material SoilTilled => Lit("SoilTilled", new Color(.47f, .39f, .34f));
    public static Material Wall => Lit("Wall", new Color(.92f, .90f, .86f));
    public static Material WallPlayer => Lit("WallPlayer", new Color(.95f, .91f, .82f));
    public static Material Roof => Lit("Roof", new Color(.60f, .62f, .70f));
    public static Material RoofPlayer => Lit("RoofPlayer", new Color(.82f, .58f, .50f));
    public static Material Wood => Lit("Wood", new Color(.69f, .60f, .51f));
    public static Material WoodDark => Lit("WoodDark", new Color(.50f, .43f, .38f));
    public static Material Stone => Lit("Stone", new Color(.74f, .74f, .76f));
    public static Material DarkGray => Lit("DarkGray", new Color(.36f, .36f, .40f));
    public static Material Window => Lit("Window", new Color(.62f, .74f, .82f), .7f);
    public static Material Foliage => Lit("Foliage", new Color(.52f, .66f, .49f));
    public static Material FoliageDark => Lit("FoliageDark", new Color(.38f, .51f, .41f));
    public static Material Trunk => Lit("Trunk", new Color(.58f, .50f, .44f));
    public static Material CropLeaf => Lit("CropLeaf", new Color(.62f, .80f, .45f));
    public static Material CropFruit => Lit("CropFruit", new Color(.96f, .74f, .38f));
    public static Material PetalPink => Lit("PetalPink", new Color(.97f, .62f, .74f));
    public static Material PetalYellow => Lit("PetalYellow", new Color(.98f, .86f, .45f));
    public static Material Skin => Lit("Skin", new Color(.97f, .87f, .77f));
    public static Material Eye => Lit("Eye", new Color(.18f, .17f, .20f), .6f);
    public static Material ClothPlayer => Lit("ClothPlayer", new Color(.52f, .67f, .88f));
    public static Material Metal => Lit("Metal", new Color(.80f, .82f, .86f), .65f, .3f);
    public static Material Accent => Lit("Accent", new Color(.98f, .80f, .42f), .5f);
    public static Material Water => Transparent("Water", new Color(.52f, .76f, .88f, .78f), .9f);
    public static Material Glass => Transparent("Glass", new Color(.85f, .95f, 1f, .22f), .95f);
    public static Material Outline => OutlineMaterial("Outline", new Color(1f, .93f, .55f));
    public static Material OutlinePlot => OutlineMaterial("OutlinePlot", new Color(.65f, 1f, .7f));

    public static Material Cloth(string name, Color color) => Lit("Cloth" + name, color);

    public static Material Lit(string name, Color color, float smoothness = .25f, float metallic = 0f)
    {
        return GetOrCreate(name, LIT, material =>
        {
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.enableInstancing = true;
        });
    }

    // same lighting response as Lit(), plus a baked base map + normal map (the Distributor's head)
    public static Material LitMapped(string name, Texture2D baseMap, Texture2D normalMap, float smoothness = .25f)
    {
        return GetOrCreate(name, LIT, material =>
        {
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", baseMap);
            material.SetTexture("_BumpMap", normalMap);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            material.enableInstancing = true;
        });
    }

    public static Material Transparent(string name, Color color, float smoothness)
    {
        return GetOrCreate(name, LIT, material =>
        {
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            MakeTransparent(material);
        });
    }

    public static Material Unlit(string name, Color color)
    {
        return GetOrCreate(name, UNLIT, material => material.SetColor("_BaseColor", color));
    }

    // soft round sprite particles, tinted per system through the particle colour
    public static Material Particle(string name, Texture2D texture, bool additive)
    {
        return GetOrCreate(name, PARTICLES, material =>
        {
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            MakeTransparent(material);
            if (additive)
            {
                material.SetFloat("_Blend", 2f);
                material.SetFloat("_DstBlend", (float)BlendMode.One);
            }
        });
    }

    // a see-through coloured texture (Day 3's crater, the ghost plots' hologram tint); lit unless isUnlit
    public static Material TransparentMapped(string name, Color color, Texture2D baseMap, bool isUnlit)
    {
        return GetOrCreate(name, isUnlit ? UNLIT : LIT, material =>
        {
            material.SetColor("_BaseColor", color);
            if (baseMap != null) material.SetTexture("_BaseMap", baseMap);
            if (!isUnlit) material.SetFloat("_Smoothness", .1f);
            MakeTransparent(material);
        });
    }

    private static Material OutlineMaterial(string name, Color color)
    {
        return GetOrCreate(name, UNLIT, material =>
        {
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Cull", (float)CullMode.Front); // draw only the back faces of the inflated hull
        });
    }

    private static void MakeTransparent(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static Material GetOrCreate(string name, string shaderName, System.Action<Material> setup)
    {
        string path = $"{FOLDER}/{name}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        Material material = new Material(Shader.Find(shaderName)) { name = name };
        setup(material);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
