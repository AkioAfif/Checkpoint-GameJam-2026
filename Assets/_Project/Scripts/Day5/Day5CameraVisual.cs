using Unity.Cinemachine;
using UnityEngine;

// Day 5's cutscene cameras: the land the MC pictures as a plantation; the one bed the hand-digging raises; the launch,
// chasing the MC up from behind, which simply stops where it is at the top of the arc (no cut) so the MC drops out of its
// frame; a hard cut to a chase on the dive; the plantation once it stands; and the plantation's edge where the plants pulse
// red (after the reveal, and again at night). Cinemachine blends between them, except into the launch and the dive, which cut.
public class Day5CameraVisual : MonoBehaviour
{
    [SerializeField] private Day5Director director;
    [SerializeField] private PlantationSequence plantationSequence;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineCamera wishCamera;
    [Tooltip("On the single bed the hand-digging raises.")]
    [SerializeField] private CinemachineCamera tinyBedCamera;
    [SerializeField] private CinemachineCamera launchCamera;
    [SerializeField] private CinemachineCamera diveCamera;
    [SerializeField] private CinemachineCamera plantationCamera;
    [SerializeField] private CinemachineCamera edgeCamera;

    private CinemachineBlendDefinition defaultBlend;
    private Transform launchTarget;

    private void Start()
    {
        defaultBlend = brain.DefaultBlend;
        launchTarget = launchCamera.Follow;
        plantationSequence.OnStepChanged += PlantationSequence_OnStepChanged;
    }

    private void Update()
    {
        wishCamera.enabled = director.IsWishShot();
        edgeCamera.enabled = director.IsMutationShot() || director.IsNightShot();
    }

    private void PlantationSequence_OnStepChanged(object sender, PlantationSequence.OnStepChangedEventArgs e)
    {
        PlantationSequence.Step step = e.step;
        bool isCut = step == PlantationSequence.Step.Launch || step == PlantationSequence.Step.Dive;
        brain.DefaultBlend = isCut ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f) : defaultBlend;
        tinyBedCamera.enabled = step == PlantationSequence.Step.TinyGrowth || step == PlantationSequence.Step.TinyGrowthTalk;
        // the climb's camera holds its last position and angle through the hang and the fall
        launchCamera.enabled = step == PlantationSequence.Step.Launch || step == PlantationSequence.Step.Peak || step == PlantationSequence.Step.Fall;
        bool isFollowing = step == PlantationSequence.Step.Launch;
        launchCamera.Follow = isFollowing ? launchTarget : null;
        launchCamera.LookAt = isFollowing ? launchTarget : null;
        // the dive camera stays on through the punch, so the earthquake shakes it
        diveCamera.enabled = step == PlantationSequence.Step.Dive || step == PlantationSequence.Step.Impact;
        plantationCamera.enabled = step == PlantationSequence.Step.Reveal || step == PlantationSequence.Step.RevealTalk;
    }
}
