using UnityEngine;

// The ground answering what the MC ate. Dirt flies with each bare-handed blow, then a single garden bed pushes up out of the
// grass with a puff of dirt; after the dive the whole tiled garden heaves up out of the shockwave, its beds empty.
// The single bed goes once the real garden stands where it was. The punch also levels the ground where the ponds were
// (their water is switched off by PlantationSequence): the map's terrain swaps to a copy with the basins filled.
public class PlantationRevealVisual : MonoBehaviour
{
    [SerializeField] private PlantationSequence plantationSequence;
    [Tooltip("The one bed that rises after the first bunch (a piece of the first garden tile, at its first bed).")]
    [SerializeField] private Transform tinyBed;
    [Tooltip("Where that bed is (the cut piece keeps the tile's pivot): the dig's dirt flies from here.")]
    [SerializeField] private Transform digSpot;
    [Tooltip("The tiled 3x4 garden models.")]
    [SerializeField] private Transform[] gardenTileArray;
    [SerializeField] private Transform dustPrefab;
    [SerializeField] private MeshFilter terrainFilter;
    [SerializeField] private MeshCollider terrainCollider;
    [Tooltip("The terrain with the pond basins filled level (FixSceneDay5Builder).")]
    [SerializeField] private Mesh flatTerrainMesh;
    [SerializeField] private float riseDepth = .45f;
    [SerializeField] private float riseDuration = .55f;

    private Vector3 tinyBedRestPosition;
    private Vector3[] tileRestPositionArray;
    private float tinyTimer = -1f;
    private float revealTimer = -1f;

    private void Start()
    {
        plantationSequence.OnStepChanged += PlantationSequence_OnStepChanged;
        plantationSequence.OnDigBlow += PlantationSequence_OnDigBlow;
        plantationSequence.OnImpact += PlantationSequence_OnImpact;
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
        if (revealTimer >= riseDuration) revealTimer = -1f;
    }

    // a little dirt flies up with each bare-handed blow
    private void PlantationSequence_OnDigBlow(object sender, System.EventArgs e) =>
        Instantiate(dustPrefab, digSpot.position, Quaternion.identity).localScale = Vector3.one * .6f;

    private void PlantationSequence_OnImpact(object sender, PlantationSequence.OnImpactEventArgs e)
    {
        terrainFilter.sharedMesh = flatTerrainMesh;
        terrainCollider.sharedMesh = flatTerrainMesh;
    }

    private void PlantationSequence_OnStepChanged(object sender, PlantationSequence.OnStepChangedEventArgs e)
    {
        switch (e.step)
        {
            case PlantationSequence.Step.TinyGrowth:
                tinyBed.gameObject.SetActive(true);
                tinyTimer = 0f;
                Instantiate(dustPrefab, digSpot.position, Quaternion.identity);
                break;
            case PlantationSequence.Step.Reveal:
                tinyBed.gameObject.SetActive(false);
                revealTimer = 0f;
                break;
        }
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
}
