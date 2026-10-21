using UnityEngine;

// Crop model while growing, float-up on harvest, sapling (grows in with a pop) once planted, a grown palm by the evening,
// a fruiting palm the next morning (cleared away with a float-up of its fruit when harvested).
public class FarmPlotVisual : MonoBehaviour
{
    [SerializeField] private FarmPlot farmPlot;
    [SerializeField] private GameObject cropGameObject;
    [SerializeField] private Transform saplingParent;
    [SerializeField] private Transform burstPrefab;
    [SerializeField] private float growDuration = .6f;

    private Transform sapling;
    private float growTimer = -1f;

    private void Start()
    {
        farmPlot.OnStateChanged += FarmPlot_OnStateChanged;
    }

    private void Update()
    {
        if (growTimer < 0f) return;
        growTimer += Time.deltaTime;
        float t = Mathf.Clamp01(growTimer / growDuration);
        sapling.localScale = Vector3.one * (t + Mathf.Sin(t * Mathf.PI) * .35f);
        if (t >= 1f) growTimer = -1f;
    }

    private void FarmPlot_OnStateChanged(object sender, FarmPlot.OnStateChangedEventArgs e)
    {
        bool wasCrop = cropGameObject.activeSelf;
        cropGameObject.SetActive(e.state == FarmPlot.State.Crop);
        if (wasCrop && e.state == FarmPlot.State.Empty)
            ItemFloatUp.Spawn(farmPlot.GetCropItemDataSO().prefab, cropGameObject.transform.position, burstPrefab);
        if (sapling != null && e.state == FarmPlot.State.Empty)
        {
            ItemFloatUp.Spawn(farmPlot.GetPlantedCropDataSO().harvestItemDataSO.prefab, sapling.position, burstPrefab);
            Destroy(sapling.gameObject);
            sapling = null;
        }

        if (e.state == FarmPlot.State.Grown || e.state == FarmPlot.State.Ripe)
        {
            // swapped behind the evening fade (or before the morning's first frame), so no pop
            if (sapling != null) Destroy(sapling.gameObject);
            CropDataSO cropDataSO = farmPlot.GetPlantedCropDataSO();
            sapling = Instantiate(e.state == FarmPlot.State.Ripe ? cropDataSO.ripePrefab : cropDataSO.grownPrefab, saplingParent);
            growTimer = -1f;
            return;
        }
        if (e.state != FarmPlot.State.Planted) return;
        sapling = Instantiate(farmPlot.GetPlantedCropDataSO().saplingPrefab, saplingParent);
        sapling.localScale = Vector3.zero;
        growTimer = 0f;
    }
}
