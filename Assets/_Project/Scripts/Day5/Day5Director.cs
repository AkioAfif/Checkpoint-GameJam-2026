using System;
using System.Collections.Generic;
using UnityEngine;

// Runs "Day 5: Half the Forest" on the CozyFarm map (the morning savings book is BedroomDirector in HouseInterior).
// The morning after Day 4: harvest the Lv.2 palms (the crate stays shut: the MC needs every bunch), picture a plantation on
// the land south of the pond, then PlantationSequence (eat, glitch, launch, dive, punch) makes it stand there, ripe. Its edge
// pulses red behind the MC's back (MutationSpreadVisual); while the MC sells and visits the village, three villagers have
// noticed red things, and the MC talks each one away. The evening's savings book looks good; at night the edge pulses again
// where only the player sees it.
public class Day5Director : MonoBehaviour
{
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }

    public enum State { Harvest, Wish, Plantation, Mutation, Errands, Night, Done }

    // the sub-steps that wait on a timer or a talk
    private enum Step { None, WishTalk, PlantationStart, MutationShot, FieldDoneTalk, Book, Math, NightShot, NightTalk }

    [SerializeField] private Day5SO day5SO;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private SavingsBookUI savingsBookUI;
    [SerializeField] private FabricatorController fabricator;
    [Tooltip("Today's presses; the last one repeats.")]
    [SerializeField] private FabricatorController.Recipe[] recipeArray;
    [Tooltip("What every plot already in the ground bears when the scene is played alone.")]
    [SerializeField] private CropDataSO lv2CropDataSO;
    [SerializeField] private SellCrate sellCrate;
    [SerializeField] private PlantationSequence plantationSequence;
    [SerializeField] private Npc ijahNpc;
    [SerializeField] private Npc ratnaNpc;
    [SerializeField] private Npc dimasNpc;
    [SerializeField] private HouseDoor houseDoor;
    [Tooltip("Just out of the front door, facing the camera.")]
    [SerializeField] private Transform morningPoint;
    [Tooltip("At the plantation's edge, looking over it.")]
    [SerializeField] private Transform eveningPoint;
    [Tooltip("Seconds between a talk ending and the next one starting, so the next talk doesn't hear the same end.")]
    [SerializeField] private float handOffDelay = .4f;
    [Tooltip("Seconds the plantation's edge is on screen (MutationSpreadVisual pulses it) before the MC speaks.")]
    [SerializeField] private float mutationShotDuration = 2.2f;
    [SerializeField] private float nightShotDelay = 2.4f;
    [Tooltip("Savings when this scene is played without the days before it.")]
    [SerializeField] private long testStartingSavings = 1000;

    private State state;
    private Step step;
    private float stepTimer = -1f;
    private bool hasStarted;
    private bool isWishPending;
    private bool isNightPending;
    private bool isNightFadePlaying;
    // the villagers still to talk to today
    private readonly HashSet<Npc> worriedNpcSet = new HashSet<Npc>();

    private void Start()
    {
        screenFade.OnBlack += ScreenFade_OnBlack;
        screenFade.OnFinished += ScreenFade_OnFinished;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        TaskManager.Instance.OnRequiredTasksCompleted += TaskManager_OnRequiredTasksCompleted;
        houseDoor.OnStateChanged += HouseDoor_OnStateChanged;
        plantationSequence.OnFinished += PlantationSequence_OnFinished;

        // no-ops when the bedroom already did them this morning
        WorldState.OpenAccounts(testStartingSavings, day5SO.testStartingDebt, day5SO.testDaysUntilDue);
        WorldState.StartDay(5);

        fabricator.SetRecipeArray(recipeArray);
        sellCrate.SetLocked(true, day5SO.crateLockedReason);
        houseDoor.SetLocked(true);
        // no "Finish today's tasks first" over the player's head all day
        houseDoor.SetHidden(true);
        // shortcut: the end card until Day 6 is built; then the bedroom (HouseInterior) opens it
        houseDoor.SetNextScene(Loader.Scene.HouseInterior_Stub);
        Player.Instance.Teleport(morningPoint.position);
        Player.Instance.FaceTowards(morningPoint.position + morningPoint.forward);
        SetState(State.Harvest);
    }

    private void Update()
    {
        // the plots wake up from yesterday's garden in their own Start, so the harvest is only known on the first frame
        if (!hasStarted)
        {
            hasStarted = true;
            // played on its own (no Day 4 before it), every plot already in the ground stands in for yesterday's palms
            if (!WorldState.HasGarden())
                foreach (FarmPlot farmPlot in FindObjectsByType<FarmPlot>()) farmPlot.SetRipe(lv2CropDataSO);
            TaskObjectiveSO[] harvestObjectiveArray = HarvestObjectiveFilter.Filter(day5SO.harvestObjectiveArray);
            TaskManager.Instance.SetObjectives(harvestObjectiveArray);
            if (harvestObjectiveArray.Length == 0) isWishPending = true;
        }

        if (stepTimer >= 0f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer < 0f) StepTimerFinished();
        }

        // beats triggered during free play wait until nothing else (a discovery card, a harvest) holds the player
        if (GameManager.Instance.GetState() != GameManager.State.Playing) return;
        if (isWishPending)
        {
            isWishPending = false;
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            SetState(State.Wish);
            step = Step.WishTalk;
            DialogueManager.Instance.StartDialogue(null, day5SO.wishDialogueSO);
        }
        else if (isNightPending)
        {
            isNightPending = false;
            isNightFadePlaying = true;
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            screenFade.Play(day5SO.nightCaption, 1.6f);
        }
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
        if (TaskManager.Instance != null) TaskManager.Instance.OnRequiredTasksCompleted -= TaskManager_OnRequiredTasksCompleted;
    }

    // ---------- day ----------

    private void TaskManager_OnRequiredTasksCompleted(object sender, EventArgs e)
    {
        if (state == State.Harvest) isWishPending = true;
        else if (state == State.Errands) isNightPending = true;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (state == State.Errands && e.npc != null && worriedNpcSet.Remove(e.npc))
        {
            // said their piece; from now on they're back to their everyday talk
            e.npc.SetOverrideDialogue(null);
            TaskManager.Instance.CompleteStory();
            return;
        }
        switch (step)
        {
            case Step.WishTalk:
                // PlantationSequence listens to the same event: start it a moment later so it doesn't hear this talk end
                step = Step.PlantationStart;
                stepTimer = handOffDelay;
                break;
            case Step.FieldDoneTalk:
                step = Step.None;
                StartErrands();
                break;
            case Step.Math:
                step = Step.NightShot;
                GameManager.Instance.SetState(GameManager.State.Cutscene);
                stepTimer = nightShotDelay;
                break;
            case Step.NightTalk:
                // back in control: walk home to end the day
                step = Step.None;
                houseDoor.SetHidden(false);
                houseDoor.SetLocked(false);
                TaskManager.Instance.SetObjectives(day5SO.nightObjectiveArray);
                break;
        }
    }

    // the plantation stands: its edge pulses red behind the MC's back
    private void PlantationSequence_OnFinished(object sender, EventArgs e)
    {
        GameManager.Instance.SetState(GameManager.State.Cutscene);
        SetState(State.Mutation);
        step = Step.MutationShot;
        stepTimer = mutationShotDuration;
    }

    // sell the harvest at last, and look in on the village
    private void StartErrands()
    {
        sellCrate.SetLocked(false, string.Empty);
        worriedNpcSet.Clear();
        AddWorried(ijahNpc, day5SO.ijahDialogueSO);
        AddWorried(ratnaNpc, day5SO.ratnaDialogueSO);
        AddWorried(dimasNpc, day5SO.dimasDialogueSO);
        TaskManager.Instance.SetObjectives(day5SO.errandObjectiveArray);
        SetState(State.Errands);
    }

    private void AddWorried(Npc npc, DialogueSO dialogueSO)
    {
        npc.SetOverrideDialogue(dialogueSO);
        worriedNpcSet.Add(npc);
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
        step = Step.Book;
        GameManager.Instance.SetState(GameManager.State.Popup);
        savingsBookUI.Show(SavingsBookUI_OnClosed);
    }

    private void SavingsBookUI_OnClosed()
    {
        step = Step.Math;
        DialogueManager.Instance.StartDialogue(null, day5SO.eveningMathDialogueSO);
    }

    private void StepTimerFinished()
    {
        switch (step)
        {
            case Step.PlantationStart:
                step = Step.None;
                SetState(State.Plantation);
                plantationSequence.Play();
                break;
            case Step.MutationShot:
                step = Step.FieldDoneTalk;
                DialogueManager.Instance.StartDialogue(null, day5SO.fieldDoneDialogueSO);
                break;
            case Step.NightShot:
                step = Step.NightTalk;
                DialogueManager.Instance.StartDialogue(null, day5SO.nightDialogueSO);
                break;
        }
    }

    private void HouseDoor_OnStateChanged(object sender, HouseDoor.OnStateChangedEventArgs e)
    {
        if (state != State.Night || e.state != HouseDoor.State.IrisClosing) return;
        WorldState.RecordGarden(FindObjectsByType<FarmPlot>());
        WorldState.CompleteDay5();
        SetState(State.Done);
    }

    // ---------- public ----------

    public State GetState() => state;
    // the MC pictures a plantation south of the pond
    public bool IsWishShot() => state == State.Wish;
    // the plantation's edge pulses behind the MC, who says it's done
    public bool IsMutationShot() => state == State.Mutation;
    // the evening cutscene over the plantation
    public bool IsNightShot() => step == Step.NightShot || step == Step.NightTalk;

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }
}
