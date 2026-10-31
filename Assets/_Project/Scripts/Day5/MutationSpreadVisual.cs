using UnityEngine;

// The mutation leaving the palms: wild plants at the clearing's edge flicker to the same raw red as Day 2's palms
// (MutationPulseVisual), over and over while they're on screen behind the MC's back after the planting, and again through
// the night, where the player sees it and the MC doesn't.
// Day 5 only swaps materials: most of the map is static-batched, so nothing moves or scales.
// With no director (Day 6) the red has spread into daylight: the forest pulses all day, and with isTearing every flicker
// also throws each plant into red, black, nothing or itself, out of shape and out of place (the builder un-batches them).
public class MutationSpreadVisual : MonoBehaviour
{
    [Tooltip("Empty: pulse all the time.")]
    [SerializeField] private Day5Director director;
    [Tooltip("The plants at the clearing's edge (in CM_Day5Mutation's and CM_Day5Forest's frame).")]
    [SerializeField] private Renderer[] rendererArray;
    [SerializeField] private Material pulseMaterial;
    [SerializeField] private float pulseDuration = 1.1f;
    [SerializeField] private float flickerInterval = .09f;
    [Tooltip("Seconds from one pulse's start to the next.")]
    [SerializeField] private float pulseInterval = 2.2f;
    [Header("Tearing (Day 6)")]
    [Tooltip("Each flicker picks red, black, hidden or itself per plant, and stretches and jolts some of them.")]
    [SerializeField] private bool isTearing;
    [SerializeField] private Material blackMaterial;
    [Tooltip("Chance per flicker that a plant is also out of shape and out of place.")]
    [SerializeField] private float tearChance = .45f;
    [SerializeField] private float maxJolt = .7f;
    [Tooltip("How far a torn plant stretches or squashes on each axis (0.6 = 40% to 160%).")]
    [SerializeField] private float maxStretch = .6f;

    private Material[][] originalMaterialsArray;
    private Material[][] pulseMaterialsArray;
    private Material[][] blackMaterialsArray;
    private Vector3[] positionArray;
    private Vector3[] localScaleArray;
    private bool isPulsing;
    private float timer;
    // the flicker on screen, -1 between pulses
    private int shownStep = -1;

    private void Start()
    {
        int count = rendererArray.Length;
        originalMaterialsArray = new Material[count][];
        pulseMaterialsArray = new Material[count][];
        blackMaterialsArray = new Material[count][];
        positionArray = new Vector3[count];
        localScaleArray = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            originalMaterialsArray[i] = rendererArray[i].sharedMaterials;
            pulseMaterialsArray[i] = Filled(originalMaterialsArray[i].Length, pulseMaterial);
            blackMaterialsArray[i] = Filled(originalMaterialsArray[i].Length, blackMaterial);
            positionArray[i] = rendererArray[i].transform.position;
            localScaleArray[i] = rendererArray[i].transform.localScale;
        }
    }

    private void Update()
    {
        bool shouldPulse = director == null || director.IsMutationShot() || director.GetState() == Day5Director.State.Night;
        if (shouldPulse != isPulsing)
        {
            isPulsing = shouldPulse;
            // the first pulse waits a beat, so the camera has arrived to see it
            timer = pulseInterval - pulseDuration;
        }
        if (isPulsing) timer = (timer + Time.deltaTime) % pulseInterval;
        int step = isPulsing && timer < pulseDuration ? Mathf.FloorToInt(timer / flickerInterval) : -1;
        if (step == shownStep) return;
        shownStep = step;
        for (int i = 0; i < rendererArray.Length; i++)
        {
            if (step < 0) Show(i, originalMaterialsArray[i], true, false);
            else if (!isTearing) Show(i, step % 2 == 0 ? pulseMaterialsArray[i] : originalMaterialsArray[i], true, false);
            else
            {
                // every plant on its own: red, black, itself, or gone for this flicker
                float roll = Random.value;
                Material[] materials = roll < .4f ? pulseMaterialsArray[i] : roll < .7f ? blackMaterialsArray[i] : originalMaterialsArray[i];
                Show(i, materials, roll < .88f, Random.value < tearChance);
            }
        }
    }

    private void Show(int index, Material[] materials, bool isShown, bool isTorn)
    {
        Renderer plant = rendererArray[index];
        plant.enabled = isShown;
        plant.sharedMaterials = materials;
        if (!isTearing) return;
        Vector3 jolt = isTorn ? Vector3.Scale(Random.insideUnitSphere, new Vector3(1f, .4f, 1f)) * maxJolt : Vector3.zero;
        Vector3 stretch = isTorn ? new Vector3(Stretch(), Stretch(), Stretch()) : Vector3.one;
        plant.transform.position = positionArray[index] + jolt;
        plant.transform.localScale = Vector3.Scale(localScaleArray[index], stretch);
    }

    private float Stretch() => 1f + Random.Range(-maxStretch, maxStretch);

    private static Material[] Filled(int length, Material material)
    {
        Material[] materials = new Material[length];
        for (int i = 0; i < length; i++) materials[i] = material;
        return materials;
    }
}
