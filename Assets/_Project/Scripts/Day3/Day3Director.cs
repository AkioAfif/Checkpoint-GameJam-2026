using System;
using UnityEngine;

// Runs "Day 3: Room to Grow" in the village (the morning savings book is BedroomDirector in HouseInterior).
// Two weeks after Day 2: harvest the Lv.2 field, sell it, raise a new strip by the forest with your bare fists (LandSequence),
// press seeds and plant it.
// The evening's savings book shows it still isn't enough, and the camera turns to the trees.
public class Day3Director : MonoBehaviour
{
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }

    // Harvest: pick the ripe field and sell it at the ICAN crate. NewLand: the LandSequence (eat a sawit, smash the ground).
    // Plant: press seeds and fill the new strip. Night: the savings book, the math, the forest, then home.
    public enum State { Harvest, NewLand, Plant, Night, Done }

    // the sub-steps of the evening
    private enum Step { None, Book, Math, ForestShot, Hook }

    [SerializeField] private Day3SO day3SO;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private SavingsBookUI savingsBookUI;
    [SerializeField] private FabricatorController fabricator;
    [Tooltip("Today's presses; the last one repeats.")]
    [SerializeField] private FabricatorController.Recipe[] recipeArray;
    [Tooltip("What the field bears this morning, and what the new strip is planted with.")]
    [SerializeField] private CropDataSO lv2CropDataSO;
    [Tooltip("The old field only takes this crop today, so the new seeds go into the new strip.")]
    [SerializeField] private CropDataSO oldFieldCropDataSO;
    [SerializeField] private FarmPlot[] fieldPlotArray;
    [SerializeField] private FarmPlot[] landPlotArray;
    [Tooltip("Hidden until the MC pounds it out of the ground (the new plots).")]
    [SerializeField] private GameObject landPlotsRoot;
    [SerializeField] private LandSequence landSequence;
    [SerializeField] private Npc ratnaNpc;
    [SerializeField] private Npc ijahNpc;
    [SerializeField] private Npc rigelNpc;
    [SerializeField] private HouseDoor houseDoor;
    [Tooltip("Just out of the front door, facing the camera.")]
    [SerializeField] private Transform morningPoint;
    [Tooltip("At the new strip's edge, facing the trees.")]
    [SerializeField] private Transform eveningPoint;
    [SerializeField] private float forestShotDelay = 1.8f;
    [Tooltip("Savings when this scene is played without the days before it.")]
    [SerializeField] private long testStartingSavings = 1000;

    private State state;
    private Step step;
    private float stepTimer = -1f;
    private bool hasStarted;
    private bool isNightFadePlaying;
    private bool isNewLandPending;
    private bool isNightPending;

    private void Start()
    {
        screenFade.OnBlack += ScreenFade_OnBlack;
        screenFade.OnFinished += ScreenFade_OnFinished;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        TaskManager.Instance.OnRequiredTasksCompleted += TaskManager_OnRequiredTasksCompleted;
        houseDoor.OnStateChanged += HouseDoor_OnStateChanged;
        SellCrate.OnAnySold += SellCrate_OnAnySold;
        landSequence.OnFinished += LandSequence_OnFinished;

        // both no-ops when the bedroom already did them this morning
        WorldState.OpenAccounts(testStartingSavings, day3SO.testStartingDebt, day3SO.daysUntilDue);
        WorldState.SkipTime(day3SO.paidDuringSkip, day3SO.daysUntilDue);

        fabricator.SetRecipeArray(recipeArray);
        foreach (FarmPlot farmPlot in fieldPlotArray) farmPlot.SetPlantableCrop(oldFieldCropDataSO);
        foreach (FarmPlot farmPlot in landPlotArray) farmPlot.SetPlantableCrop(lv2CropDataSO);
        landPlotsRoot.SetActive(false);
        ratnaNpc.SetOverrideDialogue(day3SO.ratnaDialogueSO);
        ijahNpc.SetOverrideDialogue(day3SO.ijahDialogueSO);
        rigelNpc.SetOverrideDialogue(day3SO.rigelDialogueSO);
        houseDoor.SetLocked(true);
        // no "Finish today's tasks first" over the player's head all day
        houseDoor.SetHidden(true);
        houseDoor.SetNextScene(Loader.Scene.HouseInterior_Stub);
        TaskManager.Instance.SetObjectives(day3SO.harvestObjectiveArray);
        Player.Instance.Teleport(morningPoint.position);
        Player.Instance.FaceTowards(morningPoint.position + morningPoint.forward);
        SetState(State.Harvest);
    }

    private void Update()
    {
        // the plots set their start state in their own Start, so the field ripens on the first frame
        if (!hasStarted)
        {
            hasStarted = true;
            foreach (FarmPlot farmPlot in fieldPlotArray) farmPlot.SetRipe(lv2CropDataSO);
        }

        if (stepTimer >= 0f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer < 0f) StepTimerFinished();
        }

        // beats triggered during free play wait until nothing else (a discovery card, a planting) holds the player
        if (GameManager.Instance.GetState() != GameManager.State.Playing) return;
        if (isNewLandPending)
        {
            isNewLandPending = false;
            SetState(State.NewLand);
            landSequence.Play();
        }
        else if (isNightPending)
        {
            isNightPending = false;
            isNightFadePlaying = true;
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            screenFade.Play(day3SO.nightCaption, 1.6f);
        }
    }

    private void OnDestroy()
    {
        SellCrate.OnAnySold -= SellCrate_OnAnySold;
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
        if (TaskManager.Instance != null) TaskManager.Instance.OnRequiredTasksCompleted -= TaskManager_OnRequiredTasksCompleted;
    }

    // ---------- day ----------

    private void SellCrate_OnAnySold(object sender, SellCrate.OnAnySoldEventArgs e)
    {
        if (state == State.Harvest) TaskManager.Instance.CompleteStory();
    }

    private void TaskManager_OnRequiredTasksCompleted(object sender, EventArgs e)
    {
        if (state == State.Harvest) isNewLandPending = true;
        else if (state == State.Plant) isNightPending = true;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (state == State.Night && step == Step.Math)
        {
            step = Step.ForestShot;
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            stepTimer = forestShotDelay;
        }
        else if (state == State.Night && step == Step.Hook)
        {
            // back in control: walk home to end the day
            step = Step.None;
            houseDoor.SetHidden(false);
            houseDoor.SetLocked(false);
            TaskManager.Instance.SetObjectives(day3SO.nightObjectiveArray);
        }
    }

    // the strip is out of the ground and the player is free again
    private void LandSequence_OnFinished(object sender, EventArgs e)
    {
        TaskManager.Instance.SetObjectives(day3SO.plantObjectiveArray);
        SetState(State.Plant);
    }

    // ---------- night ----------

    private void ScreenFade_OnBlack(object sender, EventArgs e)
    {
        if (!isNightFadePlaying) return;
        Player.Instance.Teleport(eveningPoint.position);
        Player.Instance.FaceTowards(eveningPoint.position + eveningPoint.forward);
        foreach (FarmPlot farmPlot in landPlotArray) farmPlot.Grow();
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
        DialogueManager.Instance.StartDialogue(null, day3SO.eveningMathDialogueSO);
    }

    private void StepTimerFinished()
    {
        if (step != Step.ForestShot) return;
        step = Step.Hook;
        DialogueManager.Instance.StartDialogue(null, day3SO.forestHookDialogueSO);
    }

    private void HouseDoor_OnStateChanged(object sender, HouseDoor.OnStateChangedEventArgs e)
    {
        if (state != State.Night || e.state != HouseDoor.State.IrisClosing) return;
        WorldState.RecordGarden(FindObjectsByType<FarmPlot>());
        WorldState.CompleteDay3();
        SetState(State.Done);
    }

    public State GetState() => state;
    // the evening cutscene, filmed over the new strip into the forest
    public bool IsForestShot() => step == Step.ForestShot || step == Step.Hook;

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }
}
