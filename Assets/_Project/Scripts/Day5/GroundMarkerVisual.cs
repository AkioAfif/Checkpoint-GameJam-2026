using UnityEngine;

// A glowing ring on the ground with an arrow bobbing over it: "stand here". Shown only while the plantation sequence waits
// for the player at this spot (the dig marker, the jump marker).
public class GroundMarkerVisual : MonoBehaviour
{
    [SerializeField] private PlantationSequence plantationSequence;
    [Tooltip("The step this marker is shown for.")]
    [SerializeField] private PlantationSequence.Step shownStep;
    [SerializeField] private GameObject markerRoot;
    [SerializeField] private Transform ring;
    [SerializeField] private Transform arrow;
    [SerializeField] private float pulseScale = .12f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float bobHeight = .25f;

    private Vector3 ringScale;
    private Vector3 arrowPosition;

    private void Start()
    {
        ringScale = ring.localScale;
        arrowPosition = arrow.localPosition;
        markerRoot.SetActive(false);
    }

    private void Update()
    {
        bool isShown = plantationSequence.GetStep() == shownStep;
        if (markerRoot.activeSelf != isShown) markerRoot.SetActive(isShown);
        if (!isShown) return;
        float wave = Mathf.Sin(Time.time * pulseSpeed);
        ring.localScale = ringScale * (1f + wave * pulseScale);
        arrow.localPosition = arrowPosition + Vector3.up * (wave * .5f + .5f) * bobHeight;
        arrow.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
    }
}
