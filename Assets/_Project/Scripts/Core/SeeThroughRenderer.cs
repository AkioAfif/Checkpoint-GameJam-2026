using UnityEngine;
using UnityEngine.Rendering;

// Lets one renderer turn see-through: it swaps to transparent copies of its own materials (made once, reused) and tints their
// alpha; at alpha 1 the untouched shared originals come back, so batching and shadows are exactly as before.
// Shared by the camera's occlusion fade and Day 3's imagined farm.
public class SeeThroughRenderer
{
    private const string TRANSPARENT_KEYWORD = "_SURFACE_TYPE_TRANSPARENT";

    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int BlendId = Shader.PropertyToID("_Blend");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int SrcBlendAlphaId = Shader.PropertyToID("_SrcBlendAlpha");
    private static readonly int DstBlendAlphaId = Shader.PropertyToID("_DstBlendAlpha");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
    // glTF materials, URP Lit, legacy-style shaders
    private static readonly int[] ColorIdArray = { Shader.PropertyToID("baseColorFactor"), Shader.PropertyToID("_BaseColor"), Shader.PropertyToID("_Color") };

    private readonly Renderer renderer;
    private readonly Mesh mesh;
    private readonly Material[] originalArray;
    private readonly Material[] fadedArray;
    private readonly int[] colorIdArray;
    private readonly Color[] colorArray;
    private readonly ShadowCastingMode shadowCastingMode;
    private readonly bool isShadowHidden;
    private bool isSwapped;

    private SeeThroughRenderer(Renderer renderer, Mesh mesh, Material[] originalArray, int[] colorIdArray, bool isShadowHidden)
    {
        this.renderer = renderer;
        this.isShadowHidden = isShadowHidden;
        this.mesh = mesh;
        this.originalArray = originalArray;
        this.colorIdArray = colorIdArray;
        shadowCastingMode = renderer.shadowCastingMode;
        fadedArray = new Material[originalArray.Length];
        colorArray = new Color[originalArray.Length];
        for (int i = 0; i < originalArray.Length; i++)
        {
            Material faded = new Material(originalArray[i]) { name = originalArray[i].name + " (Faded)" };
            MakeTransparent(faded);
            fadedArray[i] = faded;
            colorArray[i] = originalArray[i].GetColor(colorIdArray[i]);
        }
    }

    // null when a material can't switch to transparent (its shader has no URP surface override), so that renderer is left alone
    // isShadowHidden: no shadow while see-through (the shadow pass ignores alpha, so a faded-out tree would still cast one)
    public static SeeThroughRenderer TryCreate(Renderer renderer, bool isShadowHidden = false)
    {
        if (!renderer.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null) return null;
        Material[] originalArray = renderer.sharedMaterials;
        int[] colorIdArray = new int[originalArray.Length];
        for (int i = 0; i < originalArray.Length; i++)
        {
            Material original = originalArray[i];
            if (original == null || !original.HasProperty(SurfaceId)) return null;
            colorIdArray[i] = -1;
            foreach (int colorId in ColorIdArray)
            {
                if (!original.HasProperty(colorId)) continue;
                colorIdArray[i] = colorId;
                break;
            }
            if (colorIdArray[i] == -1) return null;
        }
        return new SeeThroughRenderer(renderer, meshFilter.sharedMesh, originalArray, colorIdArray, isShadowHidden);
    }

    public void SetAlpha(float alpha)
    {
        if (alpha >= 1f)
        {
            if (!isSwapped) return;
            renderer.sharedMaterials = originalArray;
            renderer.shadowCastingMode = shadowCastingMode;
            isSwapped = false;
            return;
        }
        if (!isSwapped)
        {
            renderer.sharedMaterials = fadedArray;
            if (isShadowHidden) renderer.shadowCastingMode = ShadowCastingMode.Off;
            isSwapped = true;
        }
        for (int i = 0; i < fadedArray.Length; i++)
        {
            Color color = colorArray[i];
            color.a *= alpha;
            fadedArray[i].SetColor(colorIdArray[i], color);
        }
    }

    public bool IsSwapped() => isSwapped;
    public Mesh GetMesh() => mesh;
    public int GetMaterialCount() => originalArray.Length;

    // puts the originals back (if the renderer still exists) and frees the copies
    public void Dispose()
    {
        if (renderer != null) SetAlpha(1f);
        foreach (Material faded in fadedArray) Object.Destroy(faded);
    }

    private static void MakeTransparent(Material material)
    {
        material.SetFloat(SurfaceId, 1f);
        material.SetFloat(BlendId, 0f);
        material.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
        material.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat(SrcBlendAlphaId, (float)BlendMode.One);
        material.SetFloat(DstBlendAlphaId, (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat(ZWriteId, 0f);
        material.EnableKeyword(TRANSPARENT_KEYWORD);
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
    }
}
