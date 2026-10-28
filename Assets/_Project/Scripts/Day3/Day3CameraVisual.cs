using Unity.Cinemachine;
using UnityEngine;

// Day 3's cutscene cameras: the new-land sequence's shots, and in the evening the look over the new strip into the forest.
// On the CozyFarm map also the tree punches (low beside the MC) and the second bridge: the river shot while the MC pictures it,
// and the bank while he pounds it up.
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
    [Tooltip("Optional (the CozyFarm map): low beside the MC while he punches a tree, so the gameplay camera never looks through its canopy.")]
    [SerializeField] private TreeClearing treeClearing;
    [SerializeField] private CinemachineCamera treePunchCamera;
    [Tooltip("Optional (the CozyFarm map), with its two cameras.")]
    [SerializeField] private BridgeBuild bridgeBuild;
    [SerializeField] private CinemachineCamera bridgeIdeaCamera;
    [SerializeField] private CinemachineCamera bridgeSmashCamera;
    [Tooltip("Optional (the CozyFarm map): side-on to Rigel and the MC on the new bridge.")]
    [SerializeField] private RigelBridgeConfrontation rigelBridgeConfrontation;
    [SerializeField] private CinemachineCamera confrontCamera;

    private CinemachineBlendDefinition defaultBlend;

    private void Start()
    {
        defaultBlend = brain.DefaultBlend;
        landSequence.OnStepChanged += LandSequence_OnStepChanged;
        if (bridgeBuild != null) bridgeBuild.OnStepChanged += BridgeBuild_OnStepChanged;
        if (treeClearing != null) treeClearing.OnStepChanged += TreeClearing_OnStepChanged;
        if (rigelBridgeConfrontation != null) rigelBridgeConfrontation.OnStepChanged += RigelBridgeConfrontation_OnStepChanged;
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

    private void TreeClearing_OnStepChanged(object sender, TreeClearing.OnStepChangedEventArgs e)
    {
        brain.DefaultBlend = defaultBlend;
        treePunchCamera.enabled = e.step == TreeClearing.Step.Punching;
    }

    private void BridgeBuild_OnStepChanged(object sender, BridgeBuild.OnStepChangedEventArgs e)
    {
        brain.DefaultBlend = defaultBlend;
        bridgeIdeaCamera.enabled = e.step == BridgeBuild.Step.Think || e.step == BridgeBuild.Step.Idea;
        bridgeSmashCamera.enabled = e.step == BridgeBuild.Step.Smash || e.step == BridgeBuild.Step.Settle || e.step == BridgeBuild.Step.DoneTalk;
    }

    private void RigelBridgeConfrontation_OnStepChanged(object sender, RigelBridgeConfrontation.OnStepChangedEventArgs e)
    {
        brain.DefaultBlend = defaultBlend;
        confrontCamera.enabled = e.step == RigelBridgeConfrontation.Step.Approach || e.step == RigelBridgeConfrontation.Step.Talk;
    }
}
