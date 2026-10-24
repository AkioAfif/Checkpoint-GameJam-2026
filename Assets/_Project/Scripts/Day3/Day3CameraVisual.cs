using Unity.Cinemachine;
using UnityEngine;

// Day 3's cutscene cameras: the new-land sequence's shots, and in the evening the look over the new strip into the forest.
// Cinemachine blends to the enabled one and back to the follow camera; the whip to Rigel is a hard cut.
public class Day3CameraVisual : MonoBehaviour
{
    [SerializeField] private Day3Director director;
    [SerializeField] private LandSequence landSequence;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineCamera forestCamera;
    [SerializeField] private CinemachineCamera emptyLandCamera;
    [SerializeField] private CinemachineCamera flashbackTwoCamera;
    [SerializeField] private CinemachineCamera flashbackDistributorCamera;
    [SerializeField] private CinemachineCamera rigelCamera;
    [SerializeField] private CinemachineCamera smashFrontCamera;
    [Tooltip("The same spot zoomed in, for the last blow.")]
    [SerializeField] private CinemachineCamera smashCloseCamera;

    private CinemachineBlendDefinition defaultBlend;

    private void Start()
    {
        defaultBlend = brain.DefaultBlend;
        landSequence.OnStepChanged += LandSequence_OnStepChanged;
    }

    private void Update()
    {
        forestCamera.enabled = director.IsForestShot();
    }

    private void LandSequence_OnStepChanged(object sender, LandSequence.OnStepChangedEventArgs e)
    {
        // the flashback and the Rigel whip cut; everything else glides
        bool isCut = e.shot == LandSequence.Shot.Rigel || e.shot == LandSequence.Shot.FlashbackTwo || e.shot == LandSequence.Shot.FlashbackDistributor
            || e.step == LandSequence.Step.Return;
        brain.DefaultBlend = isCut ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f) : defaultBlend;
        emptyLandCamera.enabled = e.shot == LandSequence.Shot.EmptyLand;
        flashbackTwoCamera.enabled = e.shot == LandSequence.Shot.FlashbackTwo;
        flashbackDistributorCamera.enabled = e.shot == LandSequence.Shot.FlashbackDistributor;
        rigelCamera.enabled = e.shot == LandSequence.Shot.Rigel;
        smashFrontCamera.enabled = e.shot == LandSequence.Shot.SmashFront;
        smashCloseCamera.enabled = e.shot == LandSequence.Shot.SmashClose;
    }
}
