using UnityEngine;

// The Plough Dash as seen: the MC crouches while charging, dust bursts where a dash starts and where each bed gets its seed.
public class PlantingDashVisual : MonoBehaviour
{
    [SerializeField] private PlantingDash plantingDash;
    [SerializeField] private CutsceneActor playerActor;
    [SerializeField] private Transform dustPrefab;
    [SerializeField] private float plotDustScale = .5f;

    private bool isCrouching;

    private void Start()
    {
        plantingDash.OnChargeChanged += PlantingDash_OnChargeChanged;
        plantingDash.OnDashed += PlantingDash_OnDashed;
        plantingDash.OnPlotPlanted += PlantingDash_OnPlotPlanted;
    }

    private void PlantingDash_OnChargeChanged(object sender, PlantingDash.OnChargeChangedEventArgs e)
    {
        bool isCharging = e.chargeNormalized > 0f;
        if (isCharging == isCrouching) return;
        isCrouching = isCharging;
        playerActor.SetPose(isCharging ? CutscenePose.Crouch : CutscenePose.None);
    }

    private void PlantingDash_OnDashed(object sender, System.EventArgs e) => Instantiate(dustPrefab, Player.Instance.transform.position, Quaternion.identity);

    private void PlantingDash_OnPlotPlanted(object sender, PlantingDash.OnPlotPlantedEventArgs e) =>
        Instantiate(dustPrefab, e.position, Quaternion.identity).localScale = Vector3.one * plotDustScale;
}
