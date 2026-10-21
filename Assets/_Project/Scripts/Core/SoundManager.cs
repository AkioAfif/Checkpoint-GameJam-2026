using System;
using UnityEngine;

// One-shot SFX for every gameplay event. Clips come from AudioClipRefsSO; empty slots are silent.
public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClipRefsSO audioClipRefsSO;
    [SerializeField] private HouseDoor houseDoor;
    [Tooltip("Day 1 only; empty in the Day 2 scene.")]
    [SerializeField] private DistributorTruck truck;
    [SerializeField] private float volume = 1f;

    private void Start()
    {
        ResourceNode.OnAnyCollected += Collected;
        FarmPlot.OnAnyHarvested += Collected;
        FabricatorController.OnAnySeedFabricated += FabricatorController_OnAnySeedFabricated;
        DialogueManager.Instance.OnLineChanged += DialogueManager_OnLineChanged;
        QTEManager.Instance.OnCheckResolved += QTEManager_OnCheckResolved;
        PlantingController.Instance.OnPlantingFinished += PlantingController_OnPlantingFinished;
        houseDoor.OnStateChanged += HouseDoor_OnStateChanged;
        if (truck != null) truck.OnStateChanged += Truck_OnStateChanged;
    }

    private void OnDestroy()
    {
        ResourceNode.OnAnyCollected -= Collected;
        FarmPlot.OnAnyHarvested -= Collected;
        FabricatorController.OnAnySeedFabricated -= FabricatorController_OnAnySeedFabricated;
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnLineChanged -= DialogueManager_OnLineChanged;
        if (QTEManager.Instance != null) QTEManager.Instance.OnCheckResolved -= QTEManager_OnCheckResolved;
        if (PlantingController.Instance != null) PlantingController.Instance.OnPlantingFinished -= PlantingController_OnPlantingFinished;
    }

    private void Collected(object sender, EventArgs e) => PlaySound(audioClipRefsSO.collectArray, ((Component)sender).transform.position);
    private void FabricatorController_OnAnySeedFabricated(object sender, EventArgs e) => PlaySound(audioClipRefsSO.seedFabricatedArray, ((Component)sender).transform.position);
    private void DialogueManager_OnLineChanged(object sender, DialogueManager.OnLineChangedEventArgs e) => PlaySound(audioClipRefsSO.dialogueLineArray, ListenerPosition());

    private void QTEManager_OnCheckResolved(object sender, QTEManager.OnCheckResolvedEventArgs e)
    {
        AudioClip[] clipArray = e.result == QTEManager.Result.Perfect ? audioClipRefsSO.qtePerfectArray : e.result == QTEManager.Result.Hit ? audioClipRefsSO.qteHitArray : audioClipRefsSO.qteMissArray;
        PlaySound(clipArray, ListenerPosition());
    }

    private void PlantingController_OnPlantingFinished(object sender, PlantingController.OnPlantingFinishedEventArgs e)
    {
        PlaySound(e.isSuccess ? audioClipRefsSO.plantSuccessArray : audioClipRefsSO.plantFailArray, e.farmPlot.transform.position);
    }

    private void HouseDoor_OnStateChanged(object sender, HouseDoor.OnStateChangedEventArgs e)
    {
        if (e.state == HouseDoor.State.IrisClosing) PlaySound(audioClipRefsSO.houseEnterArray, ListenerPosition());
    }

    private void Truck_OnStateChanged(object sender, DistributorTruck.OnStateChangedEventArgs e)
    {
        if (e.state == DistributorTruck.State.Skidding) PlaySound(audioClipRefsSO.truckSkidArray, truck.transform.position);
        else if (e.state == DistributorTruck.State.Burnout) PlaySound(audioClipRefsSO.truckBurnoutArray, truck.transform.position);
    }

    private Vector3 ListenerPosition() => Camera.main.transform.position;

    private void PlaySound(AudioClip[] audioClipArray, Vector3 position)
    {
        if (audioClipArray == null || audioClipArray.Length == 0) return; // clips not assigned yet (or a slot newer than the asset) → stay silent
        AudioSource.PlayClipAtPoint(audioClipArray[UnityEngine.Random.Range(0, audioClipArray.Length)], position, volume);
    }
}
