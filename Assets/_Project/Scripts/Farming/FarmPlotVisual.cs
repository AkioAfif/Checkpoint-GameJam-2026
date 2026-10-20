using UnityEngine;

// Crop model while growing, float-up on harvest, sapling (grows in with a pop) once planted, a grown palm by the evening.
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

        if (e.state == FarmPlot.State.Grown)
        {
            // swapped behind the evening fade, so no pop
            Destroy(sapling.gameObject);
            sapling = Instantiate(farmPlot.GetPlantedCropDataSO().grownPrefab, saplingParent);
            growTimer = -1f;
            return;
        }
        if (e.state != FarmPlot.State.Planted) return;
        sapling = Instantiate(farmPlot.GetPlantedCropDataSO().saplingPrefab, saplingParent);
        sapling.localScale = Vector3.zero;
        growTimer = 0f;
    }
}
