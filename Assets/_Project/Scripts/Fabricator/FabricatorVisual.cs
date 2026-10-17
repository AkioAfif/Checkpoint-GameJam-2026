using UnityEngine;

// World machine: gently hums while idle, the seed pops out of the output tray after a successful run.
public class FabricatorVisual : MonoBehaviour
{
    [SerializeField] private FabricatorController fabricator;
    [SerializeField] private Transform outputPoint;
    [SerializeField] private Transform seedPopPrefab;
    [SerializeField] private Transform bobTransform;
    [SerializeField] private float bobAmount = .04f;

    private Vector3 bobStart;

    private void Start()
    {
        FabricatorController.OnAnySeedFabricated += FabricatorController_OnAnySeedFabricated;
        bobStart = bobTransform.localPosition;
    }

    private void Update()
    {
        bobTransform.localPosition = bobStart + Vector3.up * Mathf.Sin(Time.time * 2f) * bobAmount;
    }

    private void OnDestroy()
    {
        FabricatorController.OnAnySeedFabricated -= FabricatorController_OnAnySeedFabricated;
    }

    private void FabricatorController_OnAnySeedFabricated(object sender, System.EventArgs e)
    {
        if ((FabricatorController)sender != fabricator) return;
        ItemFloatUp.Spawn(fabricator.GetOutputItemDataSO().prefab, outputPoint.position, seedPopPrefab);
    }
}
