using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

// Fades scenery that really hides the player from the gameplay camera, and brings it back once the view is clear.
// A sphere is swept along the sight line against the scenery's colliders: only what it hits before the player fades.
// A faded renderer swaps to transparent copies of its own materials, made once per renderer and reused; the shared originals are never edited.
// Its depth is drawn first (depthOnlyMaterial), so only the outer shell shows, not the walls and windows inside it.
public class CameraOcclusionVisual : MonoBehaviour
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

    [Tooltip("Only fades while this camera is live, so cutscene shots leave the scenery alone.")]
    [SerializeField] private CinemachineCamera followCamera;
    [Tooltip("Scenery whose renderers may be faded (the environment root).")]
    [SerializeField] private Transform[] occluderRootArray;
    [Tooltip("Project/DepthOnly: draws a faded object's depth just before its see-through materials.")]
    [SerializeField] private Material depthOnlyMaterial;
    [SerializeField] private float fadedAlpha = .25f;
    [SerializeField] private float fadeSpeed = 4f;
    [Tooltip("Aims at the player's chest, not the feet.")]
    [SerializeField] private float targetHeight = 1f;
    [Tooltip("Radius (m) of the sphere swept along the sight line: how much of the player's body must stay clear.")]
    [SerializeField] private float sightRadius = .3f;
    [Tooltip("Renderers flatter than this (ground, paths, low fences) never block the view.")]
    [SerializeField] private float minOccluderHeight = 1.5f;

    private class Fade
    {
        public Mesh mesh;
        public Material[] originalArray;
        public Material[] fadedArray;
        public int[] colorIdArray;
        public Color[] colorArray;
        public float alpha = 1f;
        public bool isSwapped;
    }

    private readonly Dictionary<Collider, Renderer> candidateDictionary = new Dictionary<Collider, Renderer>();
    private readonly RaycastHit[] hitArray = new RaycastHit[32];
    private readonly Dictionary<Renderer, Fade> fadeDictionary = new Dictionary<Renderer, Fade>();
    private readonly HashSet<Renderer> unsupportedSet = new HashSet<Renderer>();
    private readonly HashSet<Renderer> occludingSet = new HashSet<Renderer>();
    private Transform cameraTransform;

    private void Start()
    {
        cameraTransform = Camera.main.transform;
        foreach (Transform root in occluderRootArray)
        {
            foreach (Renderer candidate in root.GetComponentsInChildren<Renderer>(true))
            {
                if (candidate.bounds.size.y < minOccluderHeight || !candidate.TryGetComponent(out Collider collider)) continue;
                candidateDictionary[collider] = candidate;
            }
        }
    }

    private void LateUpdate()
    {
        occludingSet.Clear();
        if (followCamera.IsLive && Player.Instance != null) FindOccluders();

        foreach (Renderer occluder in occludingSet)
        {
            if (fadeDictionary.ContainsKey(occluder) || unsupportedSet.Contains(occluder)) continue;
            Fade fade = CreateFade(occluder);
            if (fade == null) unsupportedSet.Add(occluder);
            else fadeDictionary.Add(occluder, fade);
        }

        foreach (KeyValuePair<Renderer, Fade> pair in fadeDictionary) UpdateFade(pair.Key, pair.Value);
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<Renderer, Fade> pair in fadeDictionary)
        {
            if (pair.Key != null && pair.Value.isSwapped) pair.Key.sharedMaterials = pair.Value.originalArray;
            foreach (Material faded in pair.Value.fadedArray) Destroy(faded);
        }
    }

    private void FindOccluders()
    {
        Vector3 target = Player.Instance.transform.position + Vector3.up * targetHeight;
        Vector3 toTarget = target - cameraTransform.position;
        float distance = toTarget.magnitude;
        if (distance < .01f) return;
        // stop one radius short, so a wall right behind the player isn't touched by the sphere's end
        int count = Physics.SphereCastNonAlloc(cameraTransform.position, sightRadius, toTarget / distance, hitArray, distance - sightRadius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (!candidateDictionary.TryGetValue(hitArray[i].collider, out Renderer candidate)) continue;
            if (candidate.enabled && candidate.gameObject.activeInHierarchy) occludingSet.Add(candidate);
        }
    }

    private void UpdateFade(Renderer occluder, Fade fade)
    {
        float targetAlpha = occludingSet.Contains(occluder) ? fadedAlpha : 1f;
        if (Mathf.Approximately(fade.alpha, targetAlpha) && fade.isSwapped == targetAlpha < 1f)
        {
            if (fade.isSwapped) DrawDepth(occluder, fade);
            return;
        }
        fade.alpha = Mathf.MoveTowards(fade.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

        // fully back: the untouched shared materials again, so batching and shadows are exactly as before
        if (fade.alpha >= 1f)
        {
            if (fade.isSwapped) occluder.sharedMaterials = fade.originalArray;
            fade.isSwapped = false;
            return;
        }
        if (!fade.isSwapped) occluder.sharedMaterials = fade.fadedArray;
        fade.isSwapped = true;
        for (int i = 0; i < fade.fadedArray.Length; i++)
        {
            Color color = fade.colorArray[i];
            color.a *= fade.alpha;
            fade.fadedArray[i].SetColor(fade.colorIdArray[i], color);
        }
        DrawDepth(occluder, fade);
    }

    // shortcut: draws the renderer's own mesh, so a static-batched renderer would be misplaced; handle subMeshStartIndex if the scenery gets batched
    private void DrawDepth(Renderer occluder, Fade fade)
    {
        RenderParams renderParams = new RenderParams(depthOnlyMaterial) { layer = occluder.gameObject.layer, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
        Matrix4x4 localToWorld = occluder.localToWorldMatrix;
        for (int i = 0; i < fade.originalArray.Length && i < fade.mesh.subMeshCount; i++) Graphics.RenderMesh(renderParams, fade.mesh, i, localToWorld);
    }

    // null when a material can't switch to transparent (its shader has no URP surface override), so that renderer is left alone
    private Fade CreateFade(Renderer occluder)
    {
        if (!occluder.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null) return null;
        Material[] originalArray = occluder.sharedMaterials;
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

        Fade fade = new Fade
        {
            mesh = meshFilter.sharedMesh,
            originalArray = originalArray,
            fadedArray = new Material[originalArray.Length],
            colorIdArray = colorIdArray,
            colorArray = new Color[originalArray.Length],
        };
        for (int i = 0; i < originalArray.Length; i++)
        {
            Material faded = new Material(originalArray[i]) { name = originalArray[i].name + " (Faded)" };
            MakeTransparent(faded);
            fade.fadedArray[i] = faded;
            fade.colorArray[i] = originalArray[i].GetColor(colorIdArray[i]);
        }
        return fade;
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
