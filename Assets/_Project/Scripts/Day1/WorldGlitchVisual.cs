using System.Collections.Generic;
using UnityEngine;

// A one-second tear in the world, for the player's eyes only: once the Distributor has gone, everything on the right of the
// screen flickers out, turns void-black / blood-red / bone-white, jolts sideways, and the villagers there stare into the
// camera, and a pitch-black Distributor flickers in among them. Then it all snaps back. The protagonist never reacts.
// World objects only, no HUD. Day 3 reuses it (full screen, no apparition) when the MC eats the Lv.2 sawit, Day 5 for three
// violent seconds (severity) when the MC eats a whole harvest of it.
public class WorldGlitchVisual : MonoBehaviour
{
    private const int UI_LAYER = 5;

    [Tooltip("Day 1: plays after Sari's objective appears. Leave empty elsewhere.")]
    [SerializeField] private Day1Director director;
    [Tooltip("Day 3: plays for the sequence's glitch step. Leave empty elsewhere.")]
    [SerializeField] private LandSequence landSequence;
    [Tooltip("Day 5: plays for the plantation sequence's glitch step. Leave empty elsewhere.")]
    [SerializeField] private PlantationSequence plantationSequence;
    [SerializeField] private Material[] corruptMaterialArray;
    [Tooltip("Scales how often renderers vanish, change material and jolt, and how far: 1 normal, about 1.5 for a violent tear.")]
    [SerializeField] private float severity = 1f;
    [Tooltip("Seconds after Sari's objective appears, so the camera is back on the player.")]
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float duration = 1f;
    [Tooltip("The world updates in choppy steps, like a bad signal.")]
    [SerializeField] private float tickInterval = .05f;
    [Tooltip("Viewport x from which the world glitches (0 left, 1 right).")]
    [SerializeField] private float screenSplit = .53f;
    [SerializeField] private float maxJolt = .45f;
    [SerializeField] private LayerMask ignoredLayers;
    [Tooltip("A void-black copy of the Distributor's rig, hidden until the glitch peaks (optional).")]
    [SerializeField] private GameObject apparition;
    [Tooltip("Where on screen he stands (viewport x, y), projected onto the ground.")]
    [SerializeField] private Vector2 apparitionViewport = new Vector2(.8f, .62f);

    private class GlitchTarget
    {
        public Renderer renderer;
        public Material[] materialArray;
        // null for static-batched objects: those can only flicker and change material
        public Transform movable;
        public Vector3 position;
        public Vector3 localScale;
    }

    private readonly List<GlitchTarget> targetList = new List<GlitchTarget>();
    private readonly List<Transform> starerList = new List<Transform>();
    private float delayTimer = -1f;
    private float glitchTimer = -1f;
    private float tickTimer;
    private bool hasPlayed;

    private void Start()
    {
        if (director != null) director.OnStateChanged += Director_OnStateChanged;
        if (landSequence != null) landSequence.OnStepChanged += LandSequence_OnStepChanged;
        if (plantationSequence != null) plantationSequence.OnStepChanged += PlantationSequence_OnStepChanged;
    }

    private void Update()
    {
        if (delayTimer >= 0f)
        {
            delayTimer -= Time.deltaTime;
            if (delayTimer < 0f) Begin();
        }
        if (glitchTimer < 0f) return;

        glitchTimer += Time.deltaTime;
        if (glitchTimer >= duration)
        {
            End();
            return;
        }
        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f) return;
        tickTimer = tickInterval;
        // ramps in, peaks, ramps out, with random spikes
        float strength = Mathf.Clamp01(Mathf.Sin(glitchTimer / duration * Mathf.PI) * 1.3f + Random.Range(-.2f, .3f));
        foreach (GlitchTarget target in targetList) Tick(target, strength);
        // only near the peak, and never for long
        if (apparition != null) apparition.SetActive(strength > .7f && Random.value < .6f);
    }

    // after the NavMesh agents have turned them: they look at the player, through the screen
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

    private void Director_OnStateChanged(object sender, Day1Director.OnStateChangedEventArgs e)
    {
        if (e.state != Day1Director.State.Sari || hasPlayed) return;
        hasPlayed = true;
        delayTimer = startDelay;
    }

    private void LandSequence_OnStepChanged(object sender, LandSequence.OnStepChangedEventArgs e)
    {
        if (e.step != LandSequence.Step.Glitch) return;
        duration = landSequence.GetGlitchDuration();
        Begin();
    }

    private void PlantationSequence_OnStepChanged(object sender, PlantationSequence.OnStepChangedEventArgs e)
    {
        if (e.step != PlantationSequence.Step.Glitch) return;
        duration = plantationSequence.GetGlitchDuration();
        Begin();
    }

    private void Begin()
    {
        Camera camera = Camera.main;
        Plane[] frustum = GeometryUtility.CalculateFrustumPlanes(camera);
        Transform player = Player.Instance.transform;
        targetList.Clear();
        starerList.Clear();
        // one scan when it starts: whatever happens to be on the right of the screen right now
        foreach (Renderer renderer in FindObjectsByType<Renderer>())
        {
            if (!renderer.enabled || renderer.transform.IsChildOf(player)) continue;
            int layer = renderer.gameObject.layer;
            if (layer == UI_LAYER || (ignoredLayers.value & (1 << layer)) != 0) continue;
            if (!GeometryUtility.TestPlanesAABB(frustum, renderer.bounds)) continue;
            if (LeftmostViewportX(camera, renderer.bounds) < screenSplit) continue;

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
        foreach (Npc npc in FindObjectsByType<Npc>())
        {
            Vector3 viewport = camera.WorldToViewportPoint(npc.transform.position);
            if (viewport.z > 0f && viewport.x >= screenSplit && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f) starerList.Add(npc.transform);
        }
        if (apparition != null) PlaceApparition(camera);
        glitchTimer = 0f;
        tickTimer = 0f;
    }

    // the whole object has to sit right of the line: big ground and road meshes that cross it stay untouched
    private static float LeftmostViewportX(Camera camera, Bounds bounds)
    {
        float leftmost = float.MaxValue;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
            Vector3 viewport = camera.WorldToViewportPoint(corner);
            // behind the camera counts as crossing the line
            if (viewport.z <= 0f) return float.MinValue;
            leftmost = Mathf.Min(leftmost, viewport.x);
        }
        return leftmost;
    }

    private void PlaceApparition(Camera camera)
    {
        Ray ray = camera.ViewportPointToRay(new Vector3(apparitionViewport.x, apparitionViewport.y, 0f));
        if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)) return;
        Vector3 position = ray.GetPoint(distance);
        Vector3 toCamera = camera.transform.position - position;
        toCamera.y = 0f;
        apparition.transform.SetPositionAndRotation(position, Quaternion.LookRotation(toCamera));
    }

    private void Tick(GlitchTarget target, float strength)
    {
        // destroyed mid-glitch (e.g. a sapling swapped for its grown palm)
        if (target.renderer == null) return;
        strength *= severity;
        target.renderer.enabled = Random.value > .35f * strength;
        if (Random.value < .7f * strength)
        {
            Material corrupt = corruptMaterialArray[Random.Range(0, corruptMaterialArray.Length)];
            Material[] corruptArray = new Material[target.materialArray.Length];
            for (int i = 0; i < corruptArray.Length; i++) corruptArray[i] = corrupt;
            target.renderer.sharedMaterials = corruptArray;
        }
        else target.renderer.sharedMaterials = target.materialArray;

        if (target.movable == null) return;
        bool isJolted = Random.value < .5f * strength;
        // a sideways screen-tear jolt and a vertical stretch
        target.movable.position = isJolted ? target.position + Vector3.right * Random.Range(-maxJolt, maxJolt) * strength : target.position;
        target.movable.localScale = isJolted ? Vector3.Scale(target.localScale, new Vector3(1f, 1f + Random.Range(-.35f, .8f) * strength, 1f)) : target.localScale;
    }

    private void End()
    {
        glitchTimer = -1f;
        if (apparition != null) apparition.SetActive(false);
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
