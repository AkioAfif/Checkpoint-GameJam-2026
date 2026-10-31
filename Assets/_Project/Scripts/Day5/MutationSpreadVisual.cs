using UnityEngine;

// The mutation leaving the palms: wild plants at the clearing's edge flicker to the same raw red as Day 2's palms
// (MutationPulseVisual), over and over while they're on screen behind the MC's back after the planting, and again through
// the night, where the player sees it and the MC doesn't.
// Material swaps only: most of the map is static-batched, so nothing here moves or scales.
public class MutationSpreadVisual : MonoBehaviour
{
    [SerializeField] private Day5Director director;
    [Tooltip("The plants at the clearing's edge (in CM_Day5Mutation's and CM_Day5Forest's frame).")]
    [SerializeField] private Renderer[] rendererArray;
    [SerializeField] private Material pulseMaterial;
    [SerializeField] private float pulseDuration = 1.1f;
    [SerializeField] private float flickerInterval = .09f;
    [Tooltip("Seconds from one pulse's start to the next.")]
    [SerializeField] private float pulseInterval = 2.2f;

    private Material[][] originalMaterialsArray;
    private bool isPulsing;
    private float timer;
    private bool isShowingPulse;

    private void Start()
    {
        originalMaterialsArray = new Material[rendererArray.Length][];
        for (int i = 0; i < rendererArray.Length; i++) originalMaterialsArray[i] = rendererArray[i].sharedMaterials;
    }

    private void Update()
    {
        bool shouldPulse = director.IsMutationShot() || director.GetState() == Day5Director.State.Night;
        if (shouldPulse != isPulsing)
        {
            isPulsing = shouldPulse;
            // the first pulse waits a beat, so the camera has arrived to see it
            timer = pulseInterval - pulseDuration;
        }
        if (!isPulsing)
        {
            SetPulse(false);
            return;
        }
        timer = (timer + Time.deltaTime) % pulseInterval;
        SetPulse(timer < pulseDuration && Mathf.FloorToInt(timer / flickerInterval) % 2 == 0);
    }

    private void SetPulse(bool isPulse)
    {
        if (isPulse == isShowingPulse) return;
        isShowingPulse = isPulse;
        for (int i = 0; i < rendererArray.Length; i++)
        {
            Material[] materials = originalMaterialsArray[i];
            if (isPulse)
            {
                materials = new Material[materials.Length];
                for (int m = 0; m < materials.Length; m++) materials[m] = pulseMaterial;
            }
            rendererArray[i].sharedMaterials = materials;
        }
    }
}
