using Unity.Cinemachine;
using UnityEngine;

// Day 6's cutscene cameras: the wide shot over half the forest as palms; the path the Distributor walks up; the two of them
// at the door for the offer, cutting close on his glasses from his glaring line on and holding there after the talk; the two
// of them by the Fabricator at dusk for his bill and the choice; and the plantation's edge at night.
public class Day6CameraVisual : MonoBehaviour
{
    [SerializeField] private Day6Director director;
    [SerializeField] private CinemachineCamera wideCamera;
    [SerializeField] private CinemachineCamera arrivalCamera;
    [SerializeField] private CinemachineCamera meetingCamera;
    [SerializeField] private CinemachineCamera glassesCamera;
    [SerializeField] private CinemachineCamera fabricatorCamera;
    [SerializeField] private CinemachineCamera edgeCamera;

    // his slip has been said: stay on his glasses until the hold ends
    private bool hasGlared;

    private void Start()
    {
        DialogueManager.Instance.OnLineChanged += DialogueManager_OnLineChanged;
    }

    private void Update()
    {
        bool isOffer = director.IsOfferShot();
        if (!isOffer) hasGlared = false;
        wideCamera.enabled = director.IsWideShot();
        arrivalCamera.enabled = director.IsArrivalShot();
        glassesCamera.enabled = isOffer && (hasGlared || director.IsGlassesHold());
        meetingCamera.enabled = isOffer && !glassesCamera.enabled;
        fabricatorCamera.enabled = director.IsFabricatorShot();
        edgeCamera.enabled = director.IsNightShot();
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnLineChanged -= DialogueManager_OnLineChanged;
    }

    private void DialogueManager_OnLineChanged(object sender, DialogueManager.OnLineChangedEventArgs e)
    {
        if (director.IsOfferShot() && e.line.isNpcGlaring) hasGlared = true;
    }
}
