using System;
using UnityEngine;

// Runs "Day 2: The First Harvest" in the village (the morning savings book is BedroomDirector in HouseInterior).
// It listens to the existing systems (crate, tasks, dialogue, fade) and moves the story on; camera, lighting and the
// palms' twitch react to its events.
public class Day2Director : MonoBehaviour
{
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }
    // the night's hook: the new palms twitch for a moment while the MC looks away
    public event EventHandler OnCropsPulsed;

    // Rigel: the neighbour is already waiting at the garden; talking to him opens the day. Harvest: pick yesterday's sawit
    // and sell it at the ICAN crate (locked until then).
    // Experiment: forest resin → Sawit Lv.2 seeds → plant them. ShowRigel: he sees the new seedlings.
    // Night: the field at dusk, the twitch, the savings book, then home.
    public enum State { Harvest, Rigel, Experiment, ShowRigel, Night, Done }

    // the sub-steps of the evening
    private enum Step { None, FarmShot, Pulse, Monologue, Book }

    [SerializeField] private Day2SO day2SO;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private SavingsBookUI savingsBookUI;
    [SerializeField] private FabricatorController fabricator;
    [Tooltip("Today's presses; the last one repeats.")]
    [SerializeField] private FabricatorController.Recipe[] recipeArray;
    [Tooltip("What Day 1 planted: ripe this morning.")]
    [SerializeField] private CropDataSO ripeCropDataSO;
    [Tooltip("What today's seeds grow into.")]
    [SerializeField] private CropDataSO newCropDataSO;
    [Tooltip("Front row first: when this scene is played on its own, the first ones stand in for Day 1's saplings.")]
    [SerializeField] private FarmPlot[] farmPlotArray;
    [Tooltip("Ripe palms when this scene is played without Day 1 before it.")]
    [SerializeField] private int testRipeCount = 3;
    [Tooltip("Waits where he stands in the scene (by the garden) from the morning on.")]
    [SerializeField] private Npc rigelNpc;
    [SerializeField] private SellCrate sellCrate;
    [SerializeField] private HouseDoor houseDoor;
    [Tooltip("Just out of the front door, facing the camera.")]
    [SerializeField] private Transform morningPoint;
    [Tooltip("At the field's edge, facing the palms.")]
    [SerializeField] private Transform eveningPoint;
    [SerializeField] private float pulseDelay = 1.2f;
    [SerializeField] private float pulseDuration = 1.6f;

    private State state;
    private Step step;
    private float stepTimer = -1f;
    private bool hasStarted;
    private bool isNightFadePlaying;
    private bool isSoldPending;
    private bool isSoldDialogueActive;
    private bool isShowRigelPending;

    private void Start()
    {
        // played on its own, this scene is still Day 2 (no-op after the bedroom counted the morning)
        WorldState.StartDay(2);
        screenFade.OnBlack += ScreenFade_OnBlack;
        screenFade.OnFinished += ScreenFade_OnFinished;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        TaskManager.Instance.OnRequiredTasksCompleted += TaskManager_OnRequiredTasksCompleted;
        houseDoor.OnStateChanged += HouseDoor_OnStateChanged;
        SellCrate.OnAnySold += SellCrate_OnAnySold;

        fabricator.SetRecipeArray(recipeArray);
        foreach (FarmPlot farmPlot in farmPlotArray) farmPlot.SetPlantableCrop(newCropDataSO);
        houseDoor.SetLocked(true);
        // no "Finish today's tasks first" over the player's head all day
        houseDoor.SetHidden(true);
        houseDoor.SetNextScene(Loader.Scene.HouseInterior);
        // the day opens with Rigel: nothing sells until he has had his say
        sellCrate.SetLocked(true, day2SO.crateLockedReason);
        TaskManager.Instance.SetObjectives(day2SO.rigelObjectiveArray);
        Player.Instance.Teleport(morningPoint.position);
        Player.Instance.FaceTowards(morningPoint.position + morningPoint.forward);
        SetState(State.Rigel);
    }

    private void Update()
    {
        // the plots set their start state in their own Start, so yesterday's palms ripen on the first frame
        if (!hasStarted)
        {
            hasStarted = true;
            RipenYesterdaysPalms();
        }

        if (stepTimer >= 0f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer < 0f) StepTimerFinished();
        }

        // beats triggered during free play wait until nothing else (a discovery card, a planting) holds the player
        if (GameManager.Instance.GetState() != GameManager.State.Playing) return;
        if (isSoldPending)
        {
            isSoldPending = false;
            isSoldDialogueActive = true;
            DialogueManager.Instance.StartDialogue(null, day2SO.soldDialogueSO);
        }
        else if (isShowRigelPending)
        {
            isShowRigelPending = false;
            rigelNpc.SetOverrideDialogue(day2SO.rigelSeesLv2DialogueSO);
            TaskManager.Instance.SetObjectives(day2SO.showRigelObjectiveArray);
            SetState(State.ShowRigel);
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
        if (state == State.Harvest) isSoldPending = true;
        else if (state == State.Experiment) isShowRigelPending = true;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (state == State.Rigel && e.npc == rigelNpc)
        {
            // his bet starts the day: harvest and sell, then the Lv.2 experiment
            TaskManager.Instance.CompleteStory();
            sellCrate.SetLocked(false, null);
            TaskManager.Instance.SetObjectives(day2SO.harvestObjectiveArray);
            SetState(State.Harvest);
        }
        else if (state == State.Harvest && isSoldDialogueActive)
        {
            isSoldDialogueActive = false;
            TaskManager.Instance.SetObjectives(day2SO.experimentObjectiveArray);
            SetState(State.Experiment);
        }
        else if (state == State.ShowRigel && e.npc == rigelNpc)
        {
            TaskManager.Instance.CompleteStory();
            rigelNpc.SetOverrideDialogue(null);
            isNightFadePlaying = true;
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            screenFade.Play(day2SO.nightCaption, 1.6f);
        }
        else if (state == State.Night && step == Step.Monologue)
        {
            step = Step.Book;
            GameManager.Instance.SetState(GameManager.State.Popup);
            savingsBookUI.Show(SavingsBookUI_OnClosed);
        }
    }

    // ---------- night ----------

    private void ScreenFade_OnBlack(object sender, EventArgs e)
    {
        if (!isNightFadePlaying) return;
        Player.Instance.Teleport(eveningPoint.position);
        Player.Instance.FaceTowards(eveningPoint.position + eveningPoint.forward);
        rigelNpc.gameObject.SetActive(false);
        foreach (FarmPlot farmPlot in farmPlotArray) farmPlot.Grow();
        // set before the state so the camera already frames the field when Night begins
        step = Step.FarmShot;
        SetState(State.Night);
    }

    private void ScreenFade_OnFinished(object sender, EventArgs e)
    {
        if (!isNightFadePlaying) return;
        isNightFadePlaying = false;
        stepTimer = pulseDelay;
    }

    private void StepTimerFinished()
    {
        if (step == Step.FarmShot)
        {
            step = Step.Pulse;
            OnCropsPulsed?.Invoke(this, EventArgs.Empty);
            stepTimer = pulseDuration;
        }
        else if (step == Step.Pulse)
        {
            // the MC never saw it
            step = Step.Monologue;
            DialogueManager.Instance.StartDialogue(null, day2SO.eveningDialogueSO);
        }
    }

    private void SavingsBookUI_OnClosed()
    {
        // back in control: walk home to end the day
        step = Step.None;
        GameManager.Instance.SetState(GameManager.State.Playing);
        houseDoor.SetHidden(false);
        houseDoor.SetLocked(false);
        TaskManager.Instance.SetObjectives(day2SO.nightObjectiveArray);
    }

    private void HouseDoor_OnStateChanged(object sender, HouseDoor.OnStateChangedEventArgs e)
    {
        if (state != State.Night || e.state != HouseDoor.State.IrisClosing) return;
        WorldState.RecordGarden(FindObjectsByType<FarmPlot>());
        WorldState.CompleteDay2();
        SetState(State.Done);
    }

    // Day 1's garden wakes up by itself (FarmPlot); without a Day 1 before this scene, the first few plots stand in
    private void RipenYesterdaysPalms()
    {
        if (WorldState.HasGarden()) return;
        for (int i = 0; i < farmPlotArray.Length && i < testRipeCount; i++) farmPlotArray[i].SetRipe(ripeCropDataSO);
    }

    public State GetState() => state;
    // the evening cutscene, filmed on the field
    public bool IsFarmShot() => step != Step.None;

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }
}
