using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

// Fades scenery that really hides the player from the gameplay camera, and brings it back once the view is clear.
// A sphere is swept along the sight line against the scenery's colliders: only what it hits before the player fades.
// A faded renderer swaps to transparent copies of its own materials (SeeThroughRenderer); the shared originals are never edited.
// Its depth is drawn first (depthOnlyMaterial), so only the outer shell shows, not the walls and windows inside it.
public class CameraOcclusionVisual : MonoBehaviour
{
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
        public SeeThroughRenderer seeThrough;
        public float alpha = 1f;
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
            SeeThroughRenderer seeThrough = SeeThroughRenderer.TryCreate(occluder);
            if (seeThrough == null) unsupportedSet.Add(occluder);
            else fadeDictionary.Add(occluder, new Fade { seeThrough = seeThrough });
        }

        foreach (KeyValuePair<Renderer, Fade> pair in fadeDictionary) UpdateFade(pair.Key, pair.Value);
    }

    private void OnDestroy()
    {
        foreach (Fade fade in fadeDictionary.Values) fade.seeThrough.Dispose();
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
        if (Mathf.Approximately(fade.alpha, targetAlpha) && fade.seeThrough.IsSwapped() == targetAlpha < 1f)
        {
            if (fade.seeThrough.IsSwapped()) DrawDepth(occluder, fade.seeThrough);
            return;
        }
        fade.alpha = Mathf.MoveTowards(fade.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        fade.seeThrough.SetAlpha(fade.alpha);
        if (fade.seeThrough.IsSwapped()) DrawDepth(occluder, fade.seeThrough);
    }

    // shortcut: draws the renderer's own mesh, so a static-batched renderer would be misplaced; handle subMeshStartIndex if the scenery gets batched
    private void DrawDepth(Renderer occluder, SeeThroughRenderer seeThrough)
    {
        RenderParams renderParams = new RenderParams(depthOnlyMaterial) { layer = occluder.gameObject.layer, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
        Matrix4x4 localToWorld = occluder.localToWorldMatrix;
        Mesh mesh = seeThrough.GetMesh();
        for (int i = 0; i < seeThrough.GetMaterialCount() && i < mesh.subMeshCount; i++) Graphics.RenderMesh(renderParams, mesh, i, localToWorld);
    }
}
