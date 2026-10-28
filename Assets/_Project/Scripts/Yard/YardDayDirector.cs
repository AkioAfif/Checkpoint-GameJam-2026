using System;
using UnityEngine;

// Runs Day 3 on the CozyFarm map, "Dirt Under Your Nails" (the morning savings book is BedroomDirector in HouseInterior):
// the MC widens the garden onto the overgrown strip beside it, by hand. Harvest and sell → the math doesn't work → he pictures
// a farm on the strip → bare hands fail → errands for a sickle, a hoe and a rope (YardErrands) → lunch at Ratna's →
// the afternoon's work (weeds, rocks) → the stump, which only comes out once Rigel lends a hand → till and plant 4 plots →
// the evening book and the quiet hook (the Lv.2 bunch he kept pulses red; he doesn't see it) → home.
// Cameras, light and the pulse listen to its events and getters.
public class YardDayDirector : MonoBehaviour
{
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }
    public event EventHandler OnBunchPulsed;

    public enum State { Harvest, Wish, BareHands, Errands, Lunch, Work, Stump, Till, Night, Done }

    // the stump and the evening, a beat at a time
    public enum Step { None, Fail, PullAlone, RigelWalk, RigelHelp, PullTogether, Fallen, RigelTalk, Book, Monologue, Pulse }

    [SerializeField] private YardDaySO yardDaySO;
    [SerializeField] private YardErrands errands;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private SavingsBookUI savingsBookUI;
    [SerializeField] private FabricatorController fabricator;
    [Tooltip("Today's presses; the last one repeats.")]
    [SerializeField] private FabricatorController.Recipe[] recipeArray;
    [SerializeField] private LandQTE landQTE;
    [SerializeField] private CutsceneActor playerActor;
    [Tooltip("The garden, front row first: the first ones are ripe this morning.")]
    [SerializeField] private FarmPlot[] gardenPlotArray;
    [SerializeField] private CropDataSO lv2CropDataSO;
    [SerializeField] private int ripeLv2Count = 2;
    [SerializeField] private CropDataSO lv1CropDataSO;
    [SerializeField] private int ripeLv1Count = 4;
    [Tooltip("The 4 plots the tilled patches become; planted with Lv.1.")]
    [SerializeField] private FarmPlot[] newPlotArray;
    [SerializeField] private YardObstacle[] weedArray;
    [SerializeField] private YardObstacle[] rockArray;
    [SerializeField] private YardObstacle stump;
    [SerializeField] private YardObstacle[] tillArray;
    [SerializeField] private Npc ratnaNpc;
    [SerializeField] private Npc rigelNpc;
    [SerializeField] private HouseDoor houseDoor;
    [Tooltip("Just out of the front door, facing the camera.")]
    [SerializeField] private Transform morningPoint;
    [Tooltip("Where the bare-handed tug at a weed happens, facing it.")]
    [SerializeField] private Transform bareHandsPoint;
    [Tooltip("Where the MC holds the rope, facing the stump.")]
    [SerializeField] private Transform stumpPullPoint;
    [Tooltip("Behind the MC on the rope.")]
    [SerializeField] private Transform rigelHelpPoint;
    [Tooltip("Where the MC stands after lunch, at the strip.")]
    [SerializeField] private Transform afternoonPoint;
    [Tooltip("By the front door in the evening, facing the bunch he kept.")]
    [SerializeField] private Transform eveningPoint;
    [SerializeField] private float weedHoldDuration = 1.2f;
    [SerializeField] private int rockPressesPerBlow = 5;
    [SerializeField] private int rockBlows = 3;
    [SerializeField] private int pullPressesPerHeave = 5;
    [SerializeField] private int pullHeaves = 2;
    [Tooltip("How far the meter gets when he pulls alone: never all the way.")]
    [SerializeField] private float aloneMaxProgress = .7f;
    [SerializeField] private float tillHoldDuration = 1f;
    [SerializeField] private float rigelWalkSpeed = 3f;
    [SerializeField] private float wishShotDelay = 1.2f;
    [SerializeField] private float pulseDuration = 1.6f;

    private State state;
    private Step step;
    private float timer = -1f;
    private bool hasStarted;
    private bool isMathPending;
    private bool isMathDialogueActive;
    private bool isNightPending;
    private bool isNightFadePlaying;
    private bool isAfternoonFadePlaying;
    private YardObstacle currentJob;
    private bool isJobRunning;
    private CutsceneActor rigelActor;
    private Pose rigelStandPose;
    // how far the meter can get in the current doomed pull
    private float pullCap;

    private void Start()
    {
        rigelActor = rigelNpc.GetComponent<CutsceneActor>();
        rigelStandPose = new Pose(rigelNpc.transform.position, rigelNpc.transform.rotation);
        rigelActor.OnMoveFinished += RigelActor_OnMoveFinished;
        screenFade.OnBlack += ScreenFade_OnBlack;
        screenFade.OnFinished += ScreenFade_OnFinished;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        TaskManager.Instance.OnRequiredTasksCompleted += TaskManager_OnRequiredTasksCompleted;
        SellCrate.OnAnySold += SellCrate_OnAnySold;
        houseDoor.OnStateChanged += HouseDoor_OnStateChanged;
        landQTE.OnStateChanged += LandQTE_OnStateChanged;
        landQTE.OnFilled += LandQTE_OnFilled;
        landQTE.OnCapReached += LandQTE_OnCapReached;
        foreach (YardObstacle job in weedArray) job.OnWorkRequested += Job_OnWorkRequested;
        foreach (YardObstacle job in rockArray) job.OnWorkRequested += Job_OnWorkRequested;
        foreach (YardObstacle job in tillArray) job.OnWorkRequested += Job_OnWorkRequested;
        stump.OnWorkRequested += Stump_OnWorkRequested;

        // both no-ops when the bedroom already did them this morning
        WorldState.OpenAccounts(yardDaySO.testStartingSavings, yardDaySO.testStartingDebt, yardDaySO.testDaysUntilDue);
        WorldState.StartDay(3);
        fabricator.SetRecipeArray(recipeArray);
        foreach (FarmPlot farmPlot in newPlotArray) farmPlot.SetPlantableCrop(lv1CropDataSO);
        rigelNpc.SetOverrideDialogue(yardDaySO.rigelMorningDialogueSO);
        houseDoor.SetLocked(true);
        // no "Finish today's tasks first" over the player's head all day
        houseDoor.SetHidden(true);
        houseDoor.SetNextScene(Loader.Scene.HouseInterior);
        TaskManager.Instance.SetObjectives(yardDaySO.harvestObjectiveArray);
        Player.Instance.Teleport(morningPoint.position);
        Player.Instance.FaceTowards(morningPoint.position + morningPoint.forward);
        SetState(State.Harvest);
    }

    private void Update()
    {
        // the plots set their start state in their own Start, so yesterday's palms ripen on the first frame
        if (!hasStarted)
        {
            hasStarted = true;
            for (int i = 0; i < gardenPlotArray.Length && i < ripeLv2Count + ripeLv1Count; i++)
                gardenPlotArray[i].SetRipe(i < ripeLv2Count ? lv2CropDataSO : lv1CropDataSO);
        }

        if (timer >= 0f)
        {
            timer -= Time.deltaTime;
            if (timer < 0f) TimerFinished();
        }

        // beats triggered during free play wait until nothing else (a discovery card, a planting) holds the player
        if (GameManager.Instance.GetState() != GameManager.State.Playing) return;
        if (isMathPending)
        {
            isMathPending = false;
            isMathDialogueActive = true;
            DialogueManager.Instance.StartDialogue(null, yardDaySO.mathDialogueSO);
        }
        else if (isNightPending)
        {
            isNightPending = false;
            isNightFadePlaying = true;
            Hold();
            screenFade.Play(yardDaySO.nightCaption, 1.6f);
        }
    }

    private void OnDestroy()
    {
        SellCrate.OnAnySold -= SellCrate_OnAnySold;
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
        if (TaskManager.Instance != null) TaskManager.Instance.OnRequiredTasksCompleted -= TaskManager_OnRequiredTasksCompleted;
    }

    // ---------- handlers ----------

    private void SellCrate_OnAnySold(object sender, SellCrate.OnAnySoldEventArgs e)
    {
        if (state == State.Harvest) TaskManager.Instance.CompleteStory();
    }

    private void TaskManager_OnRequiredTasksCompleted(object sender, EventArgs e)
    {
        switch (state)
        {
            case State.Harvest:
                isMathPending = true;
                break;
            case State.Errands:
                // all three tools: Ratna calls him in on the way back
                errands.Close();
                ratnaNpc.SetOverrideDialogue(yardDaySO.ratnaLunchDialogueSO);
                TaskManager.Instance.SetObjectives(yardDaySO.lunchObjectiveArray);
                SetState(State.Lunch);
                break;
            case State.Work:
                stump.SetOpen(true);
                TaskManager.Instance.SetObjectives(yardDaySO.stumpObjectiveArray);
                SetState(State.Stump);
                break;
            case State.Till:
                isNightPending = true;
                break;
        }
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (state == State.Harvest && isMathDialogueActive)
        {
            isMathDialogueActive = false;
            rigelNpc.SetOverrideDialogue(null);
            Hold();
            SetState(State.Wish);
            timer = wishShotDelay;
        }
        else if (state == State.Wish)
        {
            // he tries the nearest weed bare-handed
            Hold();
            Player.Instance.Teleport(bareHandsPoint.position);
            Player.Instance.FaceTowards(bareHandsPoint.position + bareHandsPoint.forward);
            playerActor.SetPose(CutscenePose.Pull);
            TaskManager.Instance.SetObjectives(yardDaySO.clearObjectiveArray);
            SetState(State.BareHands);
            step = Step.PullAlone;
            pullCap = aloneMaxProgress * .7f;
            landQTE.StartMash(pullPressesPerHeave, 1, pullCap);
        }
        else if (state == State.BareHands && step == Step.Fail)
        {
            step = Step.None;
            errands.Open();
            TaskManager.Instance.SetObjectives(yardDaySO.errandObjectiveArray);
            SetState(State.Errands);
        }
        else if (state == State.Lunch && e.npc == ratnaNpc)
        {
            TaskManager.Instance.CompleteStory();
            ratnaNpc.SetOverrideDialogue(null);
            isAfternoonFadePlaying = true;
            Hold();
            screenFade.Play(yardDaySO.afternoonCaption, 1.4f);
        }
        else if (state == State.Stump) OnStumpDialogueEnded();
        else if (state == State.Night && step == Step.Monologue)
        {
            // he turns to the door; the bunch on the crate behind him pulses
            step = Step.Pulse;
            Hold();
            Player.Instance.FaceTowards(eveningPoint.position - eveningPoint.forward);
            OnBunchPulsed?.Invoke(this, EventArgs.Empty);
            timer = pulseDuration;
        }
    }

    private void OnStumpDialogueEnded()
    {
        switch (step)
        {
            case Step.Fail:
                // Rigel has been watching from his fence
                step = Step.RigelWalk;
                Hold();
                rigelActor.MoveTo(rigelHelpPoint.position, Vector3.Distance(rigelNpc.transform.position, rigelHelpPoint.position) / rigelWalkSpeed, 0f);
                break;
            case Step.RigelHelp:
                step = Step.PullTogether;
                Hold();
                playerActor.SetPose(CutscenePose.Pull);
                rigelActor.SetPose(CutscenePose.Pull);
                landQTE.StartMash(pullPressesPerHeave, pullHeaves);
                break;
            case Step.RigelTalk:
                // he heads home; the plots are next
                step = Step.None;
                rigelNpc.SetOverrideDialogue(null);
                rigelActor.MoveTo(rigelStandPose.position, Vector3.Distance(rigelNpc.transform.position, rigelStandPose.position) / rigelWalkSpeed, 0f);
                foreach (YardObstacle job in tillArray) job.SetOpen(true);
                TaskManager.Instance.SetObjectives(yardDaySO.plantObjectiveArray);
                SetState(State.Till);
                break;
        }
    }

    private void RigelActor_OnMoveFinished(object sender, EventArgs e)
    {
        if (step != Step.RigelWalk)
        {
            rigelNpc.transform.rotation = rigelStandPose.rotation;
            return;
        }
        step = Step.RigelHelp;
        rigelActor.FaceTowards(stump.transform.position);
        DialogueManager.Instance.StartDialogue(rigelNpc, yardDaySO.rigelHelpDialogueSO);
    }

    private void Job_OnWorkRequested(object sender, EventArgs e)
    {
        if (isJobRunning || !GameManager.Instance.IsGamePlaying()) return;
        currentJob = (YardObstacle)sender;
        isJobRunning = true;
        Hold();
        Player.Instance.FaceTowards(currentJob.transform.position);
        playerActor.SetPose(CutscenePose.Crouch);
        bool isRock = Array.IndexOf(rockArray, currentJob) >= 0;
        if (isRock) landQTE.StartMash(rockPressesPerBlow, rockBlows);
        else landQTE.StartHold(Array.IndexOf(tillArray, currentJob) >= 0 ? tillHoldDuration : weedHoldDuration);
    }

    private void Stump_OnWorkRequested(object sender, EventArgs e)
    {
        if (step != Step.None || !GameManager.Instance.IsGamePlaying()) return;
        Hold();
        Player.Instance.Teleport(stumpPullPoint.position);
        Player.Instance.FaceTowards(stump.transform.position);
        playerActor.SetPose(CutscenePose.Pull);
        step = Step.PullAlone;
        pullCap = aloneMaxProgress;
        landQTE.StartMash(pullPressesPerHeave, pullHeaves, pullCap);
    }

    // a hold finished (a weed cut, a patch tilled)
    private void LandQTE_OnStateChanged(object sender, LandQTE.OnStateChangedEventArgs e)
    {
        if (e.state != LandQTE.State.Idle || !isJobRunning || Array.IndexOf(rockArray, currentJob) >= 0) return;
        FinishJob();
    }

    // pulling alone: the meter stalls short of the top, and he gives out
    private void LandQTE_OnCapReached(object sender, EventArgs e)
    {
        if (step == Step.PullAlone && timer < 0f) timer = .6f;
    }

    private void LandQTE_OnFilled(object sender, LandQTE.OnFilledEventArgs e)
    {
        if (isJobRunning)
        {
            // each fill is one swing of the hoe
            playerActor.Punch();
            if (e.isLast) FinishJob();
            return;
        }
        if (step != Step.PullTogether) return;
        playerActor.Punch();
        rigelActor.Punch();
        if (!e.isLast) return;
        // it tears free and both of them land in the dirt
        stump.Clear();
        TaskManager.Instance.CompleteStory();
        playerActor.SetPose(CutscenePose.None);
        rigelActor.SetPose(CutscenePose.None);
        playerActor.PlayEmote(Emote.Dodge);
        rigelActor.PlayEmote(Emote.Dodge);
        step = Step.Fallen;
        timer = 2.2f;
    }

    // ---------- the day's fades ----------

    private void ScreenFade_OnBlack(object sender, EventArgs e)
    {
        if (isAfternoonFadePlaying)
        {
            Player.Instance.Teleport(afternoonPoint.position);
            Player.Instance.FaceTowards(afternoonPoint.position + afternoonPoint.forward);
            foreach (YardObstacle job in weedArray) job.SetOpen(true);
            foreach (YardObstacle job in rockArray) job.SetOpen(true);
            TaskManager.Instance.SetObjectives(yardDaySO.workObjectiveArray);
            SetState(State.Work);
        }
        else if (isNightFadePlaying)
        {
            Player.Instance.Teleport(eveningPoint.position);
            Player.Instance.FaceTowards(eveningPoint.position + eveningPoint.forward);
            foreach (FarmPlot farmPlot in newPlotArray) farmPlot.Grow();
            SetState(State.Night);
        }
    }

    private void ScreenFade_OnFinished(object sender, EventArgs e)
    {
        if (isAfternoonFadePlaying)
        {
            isAfternoonFadePlaying = false;
            GameManager.Instance.SetState(GameManager.State.Playing);
        }
        else if (isNightFadePlaying)
        {
            isNightFadePlaying = false;
            step = Step.Book;
            GameManager.Instance.SetState(GameManager.State.Popup);
            savingsBookUI.Show(SavingsBookUI_OnClosed);
        }
    }

    private void SavingsBookUI_OnClosed()
    {
        step = Step.Monologue;
        DialogueManager.Instance.StartDialogue(null, yardDaySO.eveningDialogueSO);
    }

    private void HouseDoor_OnStateChanged(object sender, HouseDoor.OnStateChangedEventArgs e)
    {
        if (state != State.Night || e.state != HouseDoor.State.IrisClosing) return;
        WorldState.CompleteDay3();
        SetState(State.Done);
    }

    // ---------- public ----------

    public State GetState() => state;
    public Step GetStep() => step;
    // the cutscene on the strip while he pictures the farm on it
    public bool IsWishShot() => state == State.Wish;
    // the rope scene, from Rigel's arrival to his talk
    public bool IsStumpShot() => state == State.Stump && step != Step.None && step != Step.PullAlone;
    public bool IsPulseShot() => step == Step.Pulse;

    // ---------- helpers ----------

    private void TimerFinished()
    {
        if (state == State.Wish)
        {
            DialogueManager.Instance.StartDialogue(null, yardDaySO.wishDialogueSO);
            return;
        }
        switch (step)
        {
            case Step.PullAlone:
                // he gives out and falls on his back
                landQTE.Stop();
                playerActor.SetPose(CutscenePose.None);
                playerActor.PlayEmote(Emote.Dodge);
                step = Step.Fail;
                DialogueManager.Instance.StartDialogue(null, state == State.BareHands ? yardDaySO.bareHandsDialogueSO : yardDaySO.stumpAloneDialogueSO);
                break;
            case Step.Fallen:
                step = Step.RigelTalk;
                DialogueManager.Instance.StartDialogue(rigelNpc, yardDaySO.rigelTalkDialogueSO);
                break;
            case Step.Pulse:
                // back in control: walk home to end the day
                step = Step.None;
                GameManager.Instance.SetState(GameManager.State.Playing);
                houseDoor.SetHidden(false);
                houseDoor.SetLocked(false);
                TaskManager.Instance.SetObjectives(yardDaySO.nightObjectiveArray);
                break;
        }
    }

    private void FinishJob()
    {
        isJobRunning = false;
        currentJob.Clear();
        playerActor.SetPose(CutscenePose.None);
        TaskManager.Instance.CompleteStory();
        GameManager.Instance.SetState(GameManager.State.Playing);
    }

    // no player control: a dialogue that just ended handed it back
    private static void Hold() => GameManager.Instance.SetState(GameManager.State.Cutscene);

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }
}
