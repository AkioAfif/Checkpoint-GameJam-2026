using UnityEngine;

// World feedback at the plot when a planting attempt ends.
public class PlantingVisual : MonoBehaviour
{
    [SerializeField] private Transform successPrefab;
    [SerializeField] private Transform failPrefab;

    private void Start()
    {
        PlantingController.Instance.OnPlantingFinished += PlantingController_OnPlantingFinished;
    }

    private void OnDestroy()
    {
        if (PlantingController.Instance != null) PlantingController.Instance.OnPlantingFinished -= PlantingController_OnPlantingFinished;
    }

    private void PlantingController_OnPlantingFinished(object sender, PlantingController.OnPlantingFinishedEventArgs e)
    {
        Instantiate(e.isSuccess ? successPrefab : failPrefab, e.farmPlot.transform.position + Vector3.up * .2f, Quaternion.identity);
    }
}
