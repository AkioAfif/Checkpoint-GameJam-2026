using System;
using System.Collections.Generic;
using UnityEngine;

// Runs "Day 6: The Cost of Success" on the CozyFarm map, two weeks after Day 5 (the caption and the payoff are
// BedroomDirector's; the story is DAY6_DESIGN.md). Debt paid, half the forest is palms, and its edges tear red in daylight
// (MutationSpreadVisual). The MC harvests and sells into their own savings; the river runs brown. The Distributor walks up,
// pitches Sawit Lv.3 and slips: he knows about the red. Doubt: the MC goes to see the old garden's soil, Dimas by the river,
// Darto's harvest, Ijah's flowers and Rigel's dried garden. That evening, by the Fabricator, the Distributor sets the
// profit against the damage and the player chooses: take the Lv.3 seeds or refuse. Night, then the door to the end card.
public class Day6Director : MonoBehaviour
{
    // how far the MC stands in front of him for a talk (the scene's cameras are framed on it)
    public const float TALK_DISTANCE = 1.8f;

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }

    public enum State { Morning, Harvest, Arrival, Offer, Investigate, Evening, Decision, Answer, Night, Done }

    // the sub-steps that wait on a timer, a walk or a talk
    private enum Step
    {
        None, WideShot, WideTalk, ProfitTalk, MurkyTalk, WalkToPath, WalkToMc, OfferTalk, GlassesHold, DoubtTalk, InspectTalk,
        EveningTalk, BillWait, BillTalk, Choosing, AnswerTalk, NightShot, NightTalk,
    }

    [SerializeField] private Day6SO day6SO;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private SavingsBookUI savingsBookUI;
    [SerializeField] private FabricatorController fabricator;
    [Tooltip("Today's presses; the last one repeats.")]
    [SerializeField] private FabricatorController.Recipe[] recipeArray;
    [Tooltip("Two weeks on, every bed of the plantation bears again.")]
    [SerializeField] private CropDataSO lv2CropDataSO;
    [SerializeField] private FarmPlot[] plantationPlotArray;
    [Tooltip("The river's water spot: the first bottle filled there is brown.")]
    [SerializeField] private ResourceNode riverNode;
    [Tooltip("The old garden's cracked soil.")]
    [SerializeField] private InspectSpot oldGardenSpot;
    [SerializeField] private Npc distributorNpc;
    [SerializeField] private CutsceneActor distributorActor;
    [SerializeField] private Npc dimasNpc;
    [SerializeField] private Npc dartoNpc;
    [SerializeField] private Npc ijahNpc;
    [SerializeField] private Npc rigelNpc;
    [SerializeField] private Npc ratnaNpc;
    [SerializeField] private HouseDoor houseDoor;
    [Tooltip("Just out of the front door, facing the camera.")]
    [SerializeField] private Transform morningPoint;
    [Tooltip("Where the Distributor comes into view, up the path north of the house.")]
    [SerializeField] private Transform arrivalPoint;
    [Tooltip("The path's corner, where he turns toward the MC's door.")]
    [SerializeField] private Transform pathPoint;
    [Tooltip("North of the MC's door, facing south; the MC steps up to TALK_DISTANCE in front of him.")]
    [SerializeField] private Transform distributorStandPoint;
    [Tooltip("By the Fabricator, where he waits after the offer and gives the bill in the evening.")]
    [SerializeField] private Transform fabricatorStandPoint;
    [Tooltip("In front of his dried garden, where Rigel waits.")]
    [SerializeField] private Transform rigelStandPoint;
    [Tooltip("At the plantation's edge, looking over it.")]
    [SerializeField] private Transform eveningPoint;
    [SerializeField] private float walkSpeed = 1.8f;
    [Tooltip("Seconds over half the forest as palms before the MC speaks.")]
    [SerializeField] private float wideShotDuration = 2.5f;
    [Tooltip("Seconds the camera holds on his glasses after the offer.")]
    [SerializeField] private float glassesHoldDuration = 2.5f;
    [SerializeField] private float nightShotDelay = 2.4f;
    [Tooltip("Seconds between a talk ending and the next one starting, so the next talk's listeners don't hear the same end.")]
    [SerializeField] private float handOffDelay = .4f;
    [Tooltip("Savings when this scene is played without the days before it.")]
    [SerializeField] private long testStartingSavings = 1000;

    private State state;
    private Step step;
    private float stepTimer = -1f;
    private bool hasStarted;
    private bool isProfitPending;
    private bool isMurkyPending;
    private bool hasSeenMurkyWater;
    private bool isEveningPending;
    private bool isFadePlaying;
    // the places and people still to see this afternoon
    private readonly HashSet<Npc> witnessSet = new HashSet<Npc>();

    private void Start()
    {
        screenFade.OnBlack += ScreenFade_OnBlack;
        screenFade.OnFinished += ScreenFade_OnFinished;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        TaskManager.Instance.OnRequiredTasksCompleted += TaskManager_OnRequiredTasksCompleted;
        ResourceNode.OnAnyCollected += ResourceNode_OnAnyCollected;
        houseDoor.OnStateChanged += HouseDoor_OnStateChanged;
        distributorActor.OnMoveFinished += DistributorActor_OnMoveFinished;
        oldGardenSpot.OnInspected += OldGardenSpot_OnInspected;

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

        // beats triggered during free play wait until nothing else (a discovery card, a sale, a talk) holds the player
        if (GameManager.Instance.GetState() != GameManager.State.Playing || step != Step.None) return;
        if (isProfitPending)
        {
            isProfitPending = false;
            Talk(Step.ProfitTalk, null, day6SO.profitDialogueSO);
        }
        else if (isEveningPending)
        {
            isEveningPending = false;
            isFadePlaying = true;
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            screenFade.Play(day6SO.eveningCaption, 1.6f);
        }
        else if (isMurkyPending)
        {
            isMurkyPending = false;
            Talk(Step.MurkyTalk, null, day6SO.murkyWaterDialogueSO);
        }
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
        if (TaskManager.Instance != null) TaskManager.Instance.OnRequiredTasksCompleted -= TaskManager_OnRequiredTasksCompleted;
        ResourceNode.OnAnyCollected -= ResourceNode_OnAnyCollected;
    }

    // ---------- day ----------

    private void TaskManager_OnRequiredTasksCompleted(object sender, EventArgs e)
    {
        if (state == State.Harvest) isProfitPending = true;
        else if (state == State.Investigate) isEveningPending = true;
    }

    private void ResourceNode_OnAnyCollected(object sender, EventArgs e)
    {
        if ((ResourceNode)sender != riverNode || hasSeenMurkyWater) return;
        hasSeenMurkyWater = true;
        isMurkyPending = true;
    }

    private void OldGardenSpot_OnInspected(object sender, EventArgs e) => Talk(Step.InspectTalk, null, day6SO.oldGardenDialogueSO);

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (state == State.Investigate && e.npc != null)
        {
            // each witness counts once and is back to their everyday talk; Ratna's talk counts for nothing
            bool isWitness = witnessSet.Remove(e.npc);
            if (isWitness || e.npc == ratnaNpc) e.npc.SetOverrideDialogue(null);
            if (isWitness) TaskManager.Instance.CompleteStory();
            return;
        }
        switch (step)
        {
            case Step.WideTalk:
                step = Step.None;
                SetState(State.Harvest);
                TaskManager.Instance.SetObjectives(day6SO.harvestObjectiveArray);
                break;
            case Step.ProfitTalk:
                StartArrival();
                break;
            case Step.MurkyTalk:
                step = Step.None;
                break;
            case Step.OfferTalk:
                step = Step.GlassesHold;
                GameManager.Instance.SetState(GameManager.State.Cutscene);
                stepTimer = glassesHoldDuration;
                break;
            case Step.DoubtTalk:
                StartInvestigation();
                break;
            case Step.InspectTalk:
                step = Step.None;
                TaskManager.Instance.CompleteStory();
                break;
            case Step.EveningTalk:
                step = Step.BillWait;
                GameManager.Instance.SetState(GameManager.State.Cutscene);
                stepTimer = handOffDelay;
                break;
            case Step.BillTalk:
                step = Step.Choosing;
                SetState(State.Decision);
                GameManager.Instance.SetState(GameManager.State.Popup);
                break;
            case Step.AnswerTalk:
                step = Step.None;
                isFadePlaying = true;
                GameManager.Instance.SetState(GameManager.State.Cutscene);
                screenFade.Play(day6SO.nightCaption, 1.6f);
                break;
            case Step.NightTalk:
                // back in control: walk home to end the day
                step = Step.None;
                houseDoor.SetHidden(false);
                houseDoor.SetLocked(false);
                TaskManager.Instance.SetObjectives(day6SO.nightObjectiveArray);
                break;
        }
    }

    // he comes up the path; the MC steps out from the door to meet him
    private void StartArrival()
    {
        GameManager.Instance.SetState(GameManager.State.Cutscene);
        SetState(State.Arrival);
        Player.Instance.Teleport(distributorStandPoint.position + distributorStandPoint.forward * TALK_DISTANCE);
        Player.Instance.FaceTowards(distributorStandPoint.position);
        distributorNpc.transform.SetPositionAndRotation(arrivalPoint.position, arrivalPoint.rotation);
        distributorNpc.gameObject.SetActive(true);
        WalkDistributor(Step.WalkToPath, pathPoint);
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
                Talk(Step.OfferTalk, distributorNpc, day6SO.offerDialogueSO);
                break;
        }
    }

    private void WalkDistributor(Step walkStep, Transform target)
    {
        step = walkStep;
        float distance = Vector3.Distance(distributorNpc.transform.position, target.position);
        distributorActor.MoveTo(target.position, distance / walkSpeed, 0f);
    }

    // he goes to wait by the Fabricator; the MC goes to see what he meant
    private void StartInvestigation()
    {
        step = Step.None;
        distributorNpc.SetOverrideDialogue(day6SO.distributorRepeatDialogueSO);
        rigelNpc.transform.SetPositionAndRotation(rigelStandPoint.position, rigelStandPoint.rotation);
        rigelNpc.gameObject.SetActive(true);
        witnessSet.Clear();
        AddWitness(dimasNpc, day6SO.dimasDialogueSO);
        AddWitness(dartoNpc, day6SO.dartoDialogueSO);
        AddWitness(ijahNpc, day6SO.ijahDialogueSO);
        AddWitness(rigelNpc, day6SO.rigelDialogueSO);
        ratnaNpc.SetOverrideDialogue(day6SO.ratnaDialogueSO);
        oldGardenSpot.SetAvailable(true);
        TaskManager.Instance.SetObjectives(day6SO.investigateObjectiveArray);
        SetState(State.Investigate);
    }

    private void AddWitness(Npc npc, DialogueSO dialogueSO)
    {
        npc.SetOverrideDialogue(dialogueSO);
        witnessSet.Add(npc);
    }

    // ---------- evening and night ----------

    private void ScreenFade_OnBlack(object sender, EventArgs e)
    {
        if (!isFadePlaying) return;
        if (state == State.Investigate)
        {
            // dusk at the Fabricator: he is waiting there, ledger open
            Vector3 meetPoint = fabricatorStandPoint.position + fabricatorStandPoint.forward * TALK_DISTANCE;
            distributorActor.MoveTo(fabricatorStandPoint.position, .05f, 0f, true);
            distributorNpc.transform.rotation = fabricatorStandPoint.rotation;
            Player.Instance.Teleport(meetPoint);
            Player.Instance.FaceTowards(fabricatorStandPoint.position);
            SetState(State.Evening);
        }
        else if (state == State.Answer)
        {
            Player.Instance.Teleport(eveningPoint.position);
            Player.Instance.FaceTowards(eveningPoint.position + eveningPoint.forward);
            SetState(State.Night);
        }
    }

    private void ScreenFade_OnFinished(object sender, EventArgs e)
    {
        if (!isFadePlaying) return;
        isFadePlaying = false;
        if (state == State.Evening)
        {
            GameManager.Instance.SetState(GameManager.State.Popup);
            savingsBookUI.Show(SavingsBookUI_OnClosed);
        }
        else if (state == State.Night)
        {
            step = Step.NightShot;
            stepTimer = nightShotDelay;
        }
    }

    private void SavingsBookUI_OnClosed() => Talk(Step.EveningTalk, null, day6SO.eveningBookDialogueSO);

    private void StepTimerFinished()
    {
        switch (step)
        {
            case Step.WideShot:
                Talk(Step.WideTalk, null, day6SO.wideDialogueSO);
                break;
            case Step.GlassesHold:
                // he heads for the Fabricator while the MC stands thinking
                float distance = Vector3.Distance(distributorNpc.transform.position, fabricatorStandPoint.position);
                distributorActor.MoveTo(fabricatorStandPoint.position, distance / walkSpeed, 0f);
                Talk(Step.DoubtTalk, null, day6SO.doubtDialogueSO);
                break;
            case Step.BillWait:
                Talk(Step.BillTalk, distributorNpc, day6SO.billDialogueSO);
                break;
            case Step.NightShot:
                Talk(Step.NightTalk, null, WorldState.HasTakenLv3() ? day6SO.expandNightDialogueSO : day6SO.stopNightDialogueSO);
                break;
        }
    }

    private void Talk(Step talkStep, Npc npc, DialogueSO dialogueSO)
    {
        step = talkStep;
        DialogueManager.Instance.StartDialogue(npc, dialogueSO);
    }

    private void HouseDoor_OnStateChanged(object sender, HouseDoor.OnStateChangedEventArgs e)
    {
        if (state != State.Night || e.state != HouseDoor.State.IrisClosing) return;
        WorldState.RecordGarden(FindObjectsByType<FarmPlot>());
        WorldState.CompleteDay6();
        SetState(State.Done);
    }

    // ---------- public ----------

    // OfferChoiceUI's buttons: take the Lv.3 seeds (keep expanding) or refuse them (stop)
    public void Choose(bool isTaken)
    {
        if (state != State.Decision) return;
        WorldState.ChooseLv3(isTaken);
        SetState(State.Answer);
        GameManager.Instance.SetState(GameManager.State.Cutscene);
        Talk(Step.AnswerTalk, distributorNpc, isTaken ? day6SO.expandDialogueSO : day6SO.stopDialogueSO);
    }

    public State GetState() => state;
    // half the forest as palms, before and during the MC's line
    public bool IsWideShot() => step == Step.WideShot || step == Step.WideTalk;
    // the Distributor walking up the path
    public bool IsArrivalShot() => state == State.Arrival;
    // the two of them at the door, through the offer and the hold on his glasses
    public bool IsOfferShot() => step == Step.OfferTalk || step == Step.GlassesHold;
    public bool IsGlassesHold() => step == Step.GlassesHold;
    // dusk by the Fabricator: the book, his bill, the choice and his answer
    public bool IsFabricatorShot() => state == State.Evening || state == State.Decision || state == State.Answer;
    // the plantation's edge at night
    public bool IsNightShot() => step == Step.NightShot || step == Step.NightTalk;

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }
}
