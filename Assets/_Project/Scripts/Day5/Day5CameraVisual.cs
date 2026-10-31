using Unity.Cinemachine;
using UnityEngine;

// Day 5's cutscene cameras: the land the MC pictures as a plantation; the one bed the first sawit raises; the launch, chasing the MC up from below; a fixed shot
// at the top of the arc the MC drops out of; a hard cut to a chase on the dive; the plantation once it stands; and the
// plantation's edge where the plants pulse red (after the reveal, and again at night). Cinemachine blends between them,
// except into the launch, the peak and the dive, which cut.
public class Day5CameraVisual : MonoBehaviour
{
    [SerializeField] private Day5Director director;
    [SerializeField] private PlantationSequence plantationSequence;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineCamera wishCamera;
    [Tooltip("On the single bed the first sawit raises.")]
    [SerializeField] private CinemachineCamera tinyBedCamera;
    [SerializeField] private CinemachineCamera launchCamera;
    [SerializeField] private CinemachineCamera peakCamera;
    [SerializeField] private CinemachineCamera diveCamera;
    [SerializeField] private CinemachineCamera plantationCamera;
    [SerializeField] private CinemachineCamera edgeCamera;

    private CinemachineBlendDefinition defaultBlend;

    private void Start()
    {
        defaultBlend = brain.DefaultBlend;
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
        bool isCut = step == PlantationSequence.Step.Launch || step == PlantationSequence.Step.Peak || step == PlantationSequence.Step.Dive;
        brain.DefaultBlend = isCut ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f) : defaultBlend;
        tinyBedCamera.enabled = step == PlantationSequence.Step.TinyGrowth || step == PlantationSequence.Step.TinyGrowthTalk;
        launchCamera.enabled = step == PlantationSequence.Step.Launch;
        // the peak shot holds while the MC drops out of it
        peakCamera.enabled = step == PlantationSequence.Step.Peak || step == PlantationSequence.Step.Fall;
        // the dive camera stays on through the punch, so the earthquake shakes it
        diveCamera.enabled = step == PlantationSequence.Step.Dive || step == PlantationSequence.Step.Impact;
        plantationCamera.enabled = step == PlantationSequence.Step.Reveal || step == PlantationSequence.Step.RevealTalk;
    }
}
