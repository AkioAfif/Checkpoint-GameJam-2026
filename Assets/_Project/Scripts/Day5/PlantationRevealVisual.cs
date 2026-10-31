using UnityEngine;

// The ground answering what the MC ate. After the first bunch a single garden bed pushes up out of the grass with a puff of
// dirt; after the dive the whole tiled garden heaves up out of the shockwave and every palm on it springs to full size.
// The single bed goes once the real garden stands where it was.
public class PlantationRevealVisual : MonoBehaviour
{
    [SerializeField] private PlantationSequence plantationSequence;
    [Tooltip("The one bed that rises after the first bunch (a piece of the first garden tile, at its first bed).")]
    [SerializeField] private Transform tinyBed;
    [Tooltip("The tiled 3x4 garden models.")]
    [SerializeField] private Transform[] gardenTileArray;
    [Tooltip("Each plantation plot's Visual (the palm on it), scaled up from nothing.")]
    [SerializeField] private Transform[] plotVisualArray;
    [SerializeField] private Transform dustPrefab;
    [SerializeField] private float riseDepth = .45f;
    [SerializeField] private float riseDuration = .55f;
    [Tooltip("Seconds of overshoot as the palms spring up.")]
    [SerializeField] private float popDuration = .6f;

    private Vector3 tinyBedRestPosition;
    private Vector3[] tileRestPositionArray;
    private float tinyTimer = -1f;
    private float revealTimer = -1f;

    private void Start()
    {
        plantationSequence.OnStepChanged += PlantationSequence_OnStepChanged;
        tinyBedRestPosition = tinyBed.localPosition;
        tileRestPositionArray = new Vector3[gardenTileArray.Length];
        for (int i = 0; i < gardenTileArray.Length; i++) tileRestPositionArray[i] = gardenTileArray[i].localPosition;
        tinyBed.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (tinyTimer >= 0f)
        {
            tinyTimer += Time.deltaTime;
            float t = Mathf.Clamp01(tinyTimer / riseDuration);
            tinyBed.localPosition = tinyBedRestPosition + Vector3.down * riseDepth * (1f - EaseOut(t));
            if (t >= 1f) tinyTimer = -1f;
        }

        if (revealTimer < 0f) return;
        revealTimer += Time.deltaTime;
        float rise = EaseOut(Mathf.Clamp01(revealTimer / riseDuration));
        for (int i = 0; i < gardenTileArray.Length; i++) gardenTileArray[i].localPosition = tileRestPositionArray[i] + Vector3.down * riseDepth * (1f - rise);
        float pop = Mathf.Clamp01(revealTimer / popDuration);
        // overshoots a little past full size, then settles
        float scale = pop + Mathf.Sin(pop * Mathf.PI) * .25f;
        foreach (Transform plotVisual in plotVisualArray) plotVisual.localScale = Vector3.one * scale;
        if (revealTimer >= Mathf.Max(riseDuration, popDuration)) revealTimer = -1f;
    }

    private void PlantationSequence_OnStepChanged(object sender, PlantationSequence.OnStepChangedEventArgs e)
    {
        switch (e.step)
        {
            case PlantationSequence.Step.TinyGrowth:
                tinyBed.gameObject.SetActive(true);
                tinyTimer = 0f;
                Instantiate(dustPrefab, tinyBed.position, Quaternion.identity);
                break;
            case PlantationSequence.Step.Reveal:
                tinyBed.gameObject.SetActive(false);
                foreach (Transform plotVisual in plotVisualArray) plotVisual.localScale = Vector3.zero;
                revealTimer = 0f;
                break;
        }
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
}
