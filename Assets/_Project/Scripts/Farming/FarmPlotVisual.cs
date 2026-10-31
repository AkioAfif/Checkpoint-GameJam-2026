using UnityEngine;

// Crop model while growing, float-up on harvest, sapling (grows in with a pop) once planted, a grown palm by the evening,
// a fruiting palm the next morning (cleared away with a float-up of its fruit when harvested). What it shows always comes
// from the plot's current state and crop (ShowState), so a plot that set its state before this subscribed still looks right.
public class FarmPlotVisual : MonoBehaviour
{
    [SerializeField] private FarmPlot farmPlot;
    [SerializeField] private GameObject cropGameObject;
    [SerializeField] private Transform saplingParent;
    [SerializeField] private Transform burstPrefab;
    [SerializeField] private float growDuration = .6f;
    [Tooltip("Lifts the planted crop's model above the sapling parent (Day 5's plantation beds stand higher than the plot).")]
    [SerializeField] private float modelHeight;

    private Transform sapling;
    private float growTimer = -1f;

    private void Start()
    {
        farmPlot.OnStateChanged += FarmPlot_OnStateChanged;
        // the plot may already have woken up from yesterday's garden in its own Start
        ShowState(farmPlot.GetState(), false);
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
        // harvested during play: what was picked floats up
        if (!e.isInstant && e.state == FarmPlot.State.Empty)
        {
            if (cropGameObject.activeSelf) ItemFloatUp.Spawn(farmPlot.GetCropItemDataSO().prefab, cropGameObject.transform.position, burstPrefab);
            if (sapling != null) ItemFloatUp.Spawn(farmPlot.GetPlantedCropDataSO().harvestItemDataSO.prefab, sapling.position, burstPrefab);
        }
        // a grown palm swaps in behind the evening fade, so only a fresh planting pops
        ShowState(e.state, !e.isInstant && e.state == FarmPlot.State.Planted);
    }

    // the one place the plot's model is chosen: the leafy crop, or the planted crop's model for its stage, never both
    private void ShowState(FarmPlot.State state, bool isGrowingIn)
    {
        cropGameObject.SetActive(state == FarmPlot.State.Crop);
        if (sapling != null) Destroy(sapling.gameObject);
        sapling = null;
        growTimer = -1f;
        Transform modelPrefab = GetModelPrefab(state);
        if (modelPrefab == null) return;
        sapling = Instantiate(modelPrefab, saplingParent);
        sapling.localPosition += Vector3.up * modelHeight;
        if (!isGrowingIn) return;
        sapling.localScale = Vector3.zero;
        growTimer = 0f;
    }

    private Transform GetModelPrefab(FarmPlot.State state)
    {
        CropDataSO cropDataSO = farmPlot.GetPlantedCropDataSO();
        if (cropDataSO == null) return null;
        switch (state)
        {
            case FarmPlot.State.Planted: return cropDataSO.saplingPrefab;
            case FarmPlot.State.Grown: return cropDataSO.grownPrefab;
            case FarmPlot.State.Ripe: return cropDataSO.ripePrefab;
            default: return null;
        }
    }
}
