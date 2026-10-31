using System;
using System.Collections.Generic;
using UnityEngine;

// Runs "Day 6: The Offer" on the CozyFarm map, two weeks after Day 5 (the caption and the payoff are BedroomDirector's).
// The debt is paid and half the forest is palms; the wild plants along the plantation's edges now pulse red in daylight
// (MutationSpreadVisual, always on here). The Distributor walks up the path to the MC's door, congratulates them and pitches
// Sawit Lv.3 with no pressure, then slips: he mentions the red, which the MC never told anyone. The camera holds on his
// glasses, he goes to wait by the Fabricator, and the choice is left for later. The villagers are quieter; at night the edge
// pulses again and the door leads to the end card.
public class Day6Director : MonoBehaviour
{
    // how far the MC stands in front of him for the talk (the scene's cameras are framed on it)
    public const float TALK_DISTANCE = 1.8f;

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }

    public enum State { Morning, Arrival, Offer, Errands, Night, Done }

    // the sub-steps that wait on a timer, a walk or a talk
    private enum Step { None, WideShot, WideTalk, WalkToPath, WalkToMc, OfferTalk, GlassesHold, NightShot, NightTalk }

    [SerializeField] private Day6SO day6SO;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private FabricatorController fabricator;
    [Tooltip("Today's presses; the last one repeats.")]
    [SerializeField] private FabricatorController.Recipe[] recipeArray;
    [Tooltip("Two weeks on, every bed of the plantation bears again.")]
    [SerializeField] private CropDataSO lv2CropDataSO;
    [SerializeField] private FarmPlot[] plantationPlotArray;
    [SerializeField] private Npc distributorNpc;
    [SerializeField] private CutsceneActor distributorActor;
    [SerializeField] private Npc ijahNpc;
    [SerializeField] private Npc ratnaNpc;
    [SerializeField] private Npc dimasNpc;
    [SerializeField] private HouseDoor houseDoor;
    [Tooltip("Just out of the front door, facing the camera.")]
    [SerializeField] private Transform morningPoint;
    [Tooltip("Where the Distributor comes into view, up the path north of the house.")]
    [SerializeField] private Transform arrivalPoint;
    [Tooltip("The path's corner, where he turns toward the MC's door.")]
    [SerializeField] private Transform pathPoint;
    [Tooltip("North of the MC's door, facing south; the MC steps up to TALK_DISTANCE in front of him.")]
    [SerializeField] private Transform distributorStandPoint;
    [Tooltip("By the Fabricator, where he waits after the offer.")]
    [SerializeField] private Transform fabricatorStandPoint;
    [Tooltip("At the plantation's edge, looking over it.")]
    [SerializeField] private Transform eveningPoint;
    [SerializeField] private float walkSpeed = 1.8f;
    [Tooltip("Seconds over half the forest as palms before the MC speaks.")]
    [SerializeField] private float wideShotDuration = 2.5f;
    [Tooltip("Seconds the camera holds on his glasses after the offer.")]
    [SerializeField] private float glassesHoldDuration = 2.5f;
    [SerializeField] private float nightShotDelay = 2.4f;
    [Tooltip("Savings when this scene is played without the days before it.")]
    [SerializeField] private long testStartingSavings = 1000;

    private State state;
    private Step step;
    private float stepTimer = -1f;
    private bool hasStarted;
    private bool isNightPending;
    private bool isNightFadePlaying;
    // the villagers still to talk to today
    private readonly HashSet<Npc> quietNpcSet = new HashSet<Npc>();

    private void Start()
    {
        screenFade.OnBlack += ScreenFade_OnBlack;
        screenFade.OnFinished += ScreenFade_OnFinished;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        TaskManager.Instance.OnRequiredTasksCompleted += TaskManager_OnRequiredTasksCompleted;
        houseDoor.OnStateChanged += HouseDoor_OnStateChanged;
        distributorActor.OnMoveFinished += DistributorActor_OnMoveFinished;

        // no-ops when the bedroom already did them this morning
        WorldState.OpenAccounts(testStartingSavings, 0L, day6SO.daysUntilDue);
        WorldState.StartDay(6);
        WorldState.PayOffDebt(day6SO.daysUntilDue);

        fabricator.SetRecipeArray(recipeArray);
        houseDoor.SetLocked(true);
        houseDoor.SetHidden(true);
        houseDoor.SetNextScene(Loader.Scene.HouseInterior_Stub);
        distributorNpc.gameObject.SetActive(false);
        Player.Instance.Teleport(morningPoint.position);
        Player.Instance.FaceTowards(morningPoint.position + morningPoint.forward);
        TaskManager.Instance.SetObjectives(Array.Empty<TaskObjectiveSO>());
    }

    private void Update()
    {
        // GameManager.Start puts the game in Playing and the plots wake up in their own Start: begin on the first frame
        if (!hasStarted)
        {
            hasStarted = true;
            foreach (FarmPlot farmPlot in plantationPlotArray) farmPlot.SetRipe(lv2CropDataSO);
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            SetState(State.Morning);
            step = Step.WideShot;
            stepTimer = wideShotDuration;
        }

        if (stepTimer >= 0f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer < 0f) StepTimerFinished();
        }

        // the evening waits until nothing else (a discovery card, a sale) holds the player
        if (!isNightPending || GameManager.Instance.GetState() != GameManager.State.Playing) return;
        isNightPending = false;
        isNightFadePlaying = true;
        GameManager.Instance.SetState(GameManager.State.Cutscene);
        screenFade.Play(day6SO.nightCaption, 1.6f);
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
        if (TaskManager.Instance != null) TaskManager.Instance.OnRequiredTasksCompleted -= TaskManager_OnRequiredTasksCompleted;
    }

    // ---------- day ----------

    private void TaskManager_OnRequiredTasksCompleted(object sender, EventArgs e)
    {
        if (state == State.Errands) isNightPending = true;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (state == State.Errands && e.npc != null && quietNpcSet.Remove(e.npc))
        {
            e.npc.SetOverrideDialogue(null);
            TaskManager.Instance.CompleteStory();
            return;
        }
        switch (step)
        {
            case Step.WideTalk:
                // he comes up the path; the MC waits at the door
                GameManager.Instance.SetState(GameManager.State.Cutscene);
                SetState(State.Arrival);
                // the MC steps out from the door to meet him
                Player.Instance.Teleport(distributorStandPoint.position + distributorStandPoint.forward * TALK_DISTANCE);
                distributorNpc.transform.SetPositionAndRotation(arrivalPoint.position, arrivalPoint.rotation);
                distributorNpc.gameObject.SetActive(true);
                WalkDistributor(Step.WalkToPath, pathPoint);
                break;
            case Step.OfferTalk:
                step = Step.GlassesHold;
                GameManager.Instance.SetState(GameManager.State.Cutscene);
                stepTimer = glassesHoldDuration;
                break;
            case Step.NightTalk:
                step = Step.None;
                houseDoor.SetHidden(false);
                houseDoor.SetLocked(false);
                TaskManager.Instance.SetObjectives(day6SO.nightObjectiveArray);
                break;
        }
    }

    private void DistributorActor_OnMoveFinished(object sender, EventArgs e)
    {
        switch (step)
        {
            case Step.WalkToPath:
                WalkDistributor(Step.WalkToMc, distributorStandPoint);
                break;
            case Step.WalkToMc:
                distributorActor.FaceTowards(Player.Instance.transform.position);
                Player.Instance.FaceTowards(distributorNpc.transform.position);
                SetState(State.Offer);
                step = Step.OfferTalk;
                DialogueManager.Instance.StartDialogue(distributorNpc, day6SO.offerDialogueSO);
                break;
        }
    }

    private void WalkDistributor(Step walkStep, Transform target)
    {
        step = walkStep;
        float distance = Vector3.Distance(distributorNpc.transform.position, target.position);
        distributorActor.MoveTo(target.position, distance / walkSpeed, 0f);
    }

    // he goes to wait by the Fabricator; the MC is free, and the village is quieter
    private void StartErrands()
    {
        step = Step.None;
        float distance = Vector3.Distance(distributorNpc.transform.position, fabricatorStandPoint.position);
        distributorActor.MoveTo(fabricatorStandPoint.position, distance / walkSpeed, 0f);
        distributorNpc.SetOverrideDialogue(day6SO.distributorRepeatDialogueSO);
        quietNpcSet.Clear();
        AddQuiet(ijahNpc, day6SO.ijahDialogueSO);
        AddQuiet(ratnaNpc, day6SO.ratnaDialogueSO);
        AddQuiet(dimasNpc, day6SO.dimasDialogueSO);
        TaskManager.Instance.SetObjectives(day6SO.errandObjectiveArray);
        SetState(State.Errands);
        GameManager.Instance.SetState(GameManager.State.Playing);
    }

    private void AddQuiet(Npc npc, DialogueSO dialogueSO)
    {
        npc.SetOverrideDialogue(dialogueSO);
        quietNpcSet.Add(npc);
    }

    // ---------- night ----------

    private void ScreenFade_OnBlack(object sender, EventArgs e)
    {
        if (!isNightFadePlaying) return;
        Player.Instance.Teleport(eveningPoint.position);
        Player.Instance.FaceTowards(eveningPoint.position + eveningPoint.forward);
        SetState(State.Night);
    }

    private void ScreenFade_OnFinished(object sender, EventArgs e)
    {
        if (!isNightFadePlaying) return;
        isNightFadePlaying = false;
        step = Step.NightShot;
        stepTimer = nightShotDelay;
    }

    private void StepTimerFinished()
    {
        switch (step)
        {
            case Step.WideShot:
                step = Step.WideTalk;
                DialogueManager.Instance.StartDialogue(null, day6SO.wideDialogueSO);
                break;
            case Step.GlassesHold:
                StartErrands();
                break;
            case Step.NightShot:
                step = Step.NightTalk;
                DialogueManager.Instance.StartDialogue(null, day6SO.nightDialogueSO);
                break;
        }
    }

    private void HouseDoor_OnStateChanged(object sender, HouseDoor.OnStateChangedEventArgs e)
    {
        if (state != State.Night || e.state != HouseDoor.State.IrisClosing) return;
        WorldState.RecordGarden(FindObjectsByType<FarmPlot>());
        WorldState.CompleteDay6();
        SetState(State.Done);
    }

    // ---------- public ----------

    public State GetState() => state;
    // half the forest as palms, before and during the MC's line
    public bool IsWideShot() => step == Step.WideShot || step == Step.WideTalk;
    // the Distributor walking up the path
    public bool IsArrivalShot() => state == State.Arrival;
    // the two of them at the door, through the offer and the hold on his glasses
    public bool IsOfferShot() => step == Step.OfferTalk || step == Step.GlassesHold;
    public bool IsGlassesHold() => step == Step.GlassesHold;
    // the plantation's edge at night
    public bool IsNightShot() => step == Step.NightShot || step == Step.NightTalk;

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }
}
