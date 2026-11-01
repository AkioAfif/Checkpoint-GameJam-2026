using System.Collections.Generic;
using UnityEngine;

// Every so often the whole menu village tears for a second, the way Day 6's forest does (MutationSpreadVisual): everything
// on screen flickers to blood-red, void-black, nothing or itself, and loose objects jump out of place and out of shape.
// The villagers in view stop and stare into the camera. Then it snaps back as if nothing happened. The UI never glitches.
public class MenuGlitchVisual : MonoBehaviour
{
    private const int UI_LAYER = 5;

    [SerializeField] private Material[] corruptMaterialArray;
    [Tooltip("Real seconds between glitches, picked at random each time.")]
    [SerializeField] private Vector2 intervalRange = new Vector2(6f, 16f);
    [SerializeField] private float duration = 1f;
    [SerializeField] private float flickerInterval = .07f;
    [Tooltip("Chance per flicker that an object is hidden.")]
    [SerializeField] private float hideChance = .15f;
    [Tooltip("Chance per flicker that a loose object is also out of shape and out of place.")]
    [SerializeField] private float tearChance = .45f;
    [SerializeField] private float maxJolt = .7f;
    [Tooltip("How far a torn object stretches or squashes on each axis (0.6 = 40% to 160%).")]
    [SerializeField] private float maxStretch = .6f;

    private class GlitchTarget
    {
        public Renderer renderer;
        public Material[] materialArray;
        // null for static-batched and skinned objects: those can only flicker and change material
        public Transform movable;
        public Vector3 position;
        public Vector3 localScale;
    }

    private readonly List<GlitchTarget> targetList = new List<GlitchTarget>();
    private readonly List<Transform> starerList = new List<Transform>();
    private float waitTimer;
    private float glitchTimer = -1f;
    private float flickerTimer;

    private void Start()
    {
        waitTimer = Random.Range(intervalRange.x, intervalRange.y);
    }

    private void Update()
    {
        if (glitchTimer < 0f)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) Begin();
            return;
        }

        glitchTimer += Time.deltaTime;
        if (glitchTimer >= duration)
        {
            End();
            return;
        }
        flickerTimer -= Time.deltaTime;
        if (flickerTimer > 0f) return;
        flickerTimer = flickerInterval;
        // ramps in, peaks, ramps out, with random spikes
        float strength = Mathf.Clamp01(Mathf.Sin(glitchTimer / duration * Mathf.PI) * 1.3f + Random.Range(-.2f, .3f));
        foreach (GlitchTarget target in targetList) Flicker(target, strength);
    }

    // after the villagers have turned toward their spots: they look through the screen instead
    private void LateUpdate()
    {
        if (glitchTimer < 0f) return;
        Vector3 cameraPosition = Camera.main.transform.position;
        foreach (Transform starer in starerList)
        {
            Vector3 toCamera = cameraPosition - starer.position;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude > .01f) starer.rotation = Quaternion.LookRotation(toCamera);
        }
    }

    private void Begin()
    {
        Camera camera = Camera.main;
        Plane[] frustum = GeometryUtility.CalculateFrustumPlanes(camera);
        targetList.Clear();
        starerList.Clear();
        // one scan when it starts: whatever is on screen right now
        foreach (Renderer renderer in FindObjectsByType<Renderer>())
        {
            if (!renderer.enabled || renderer.gameObject.layer == UI_LAYER) continue;
            if (!GeometryUtility.TestPlanesAABB(frustum, renderer.bounds)) continue;
            bool isMovable = !renderer.gameObject.isStatic && !(renderer is SkinnedMeshRenderer);
            targetList.Add(new GlitchTarget
            {
                renderer = renderer,
                materialArray = renderer.sharedMaterials,
                movable = isMovable ? renderer.transform : null,
                position = renderer.transform.position,
                localScale = renderer.transform.localScale,
            });
        }
        foreach (MenuVillager villager in FindObjectsByType<MenuVillager>())
        {
            Vector3 viewport = camera.WorldToViewportPoint(villager.transform.position);
            if (viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f) starerList.Add(villager.transform);
        }
        glitchTimer = 0f;
        flickerTimer = 0f;
    }

    private void Flicker(GlitchTarget target, float strength)
    {
        // destroyed mid-glitch
        if (target.renderer == null) return;
        target.renderer.enabled = Random.value > hideChance * strength;
        if (Random.value < .7f * strength)
        {
            Material corrupt = corruptMaterialArray[Random.Range(0, corruptMaterialArray.Length)];
            Material[] corruptArray = new Material[target.materialArray.Length];
            for (int i = 0; i < corruptArray.Length; i++) corruptArray[i] = corrupt;
            target.renderer.sharedMaterials = corruptArray;
        }
        else target.renderer.sharedMaterials = target.materialArray;

        if (target.movable == null) return;
        bool isTorn = Random.value < tearChance * strength;
        Vector3 jolt = isTorn ? Vector3.Scale(Random.insideUnitSphere, new Vector3(1f, .4f, 1f)) * maxJolt * strength : Vector3.zero;
        Vector3 stretch = isTorn ? new Vector3(Stretch(strength), Stretch(strength), Stretch(strength)) : Vector3.one;
        target.movable.position = target.position + jolt;
        target.movable.localScale = Vector3.Scale(target.localScale, stretch);
    }

    private float Stretch(float strength) => 1f + Random.Range(-maxStretch, maxStretch) * strength;

    private void End()
    {
        glitchTimer = -1f;
        waitTimer = Random.Range(intervalRange.x, intervalRange.y);
        foreach (GlitchTarget target in targetList)
        {
            if (target.renderer == null) continue;
            target.renderer.enabled = true;
            target.renderer.sharedMaterials = target.materialArray;
            if (target.movable == null) continue;
            target.movable.position = target.position;
            target.movable.localScale = target.localScale;
        }
        targetList.Clear();
        starerList.Clear();
    }
}
