using System;
using UnityEngine;

// Day 5's set piece, from the wish to the grown plantation. The MC eats one Lv.2 sawit from the bag (hold [Interact]), then
// digs by hand at the marker south of the pond (a hard mash): one single bed is all it buys. Not enough, so they eat every
// bunch left (mash, one fill per bunch): the world tears for three seconds (WorldGlitchVisual), and through it the MC
// remembers a childhood TV show (ChildhoodShowVisual): a superhero who lands from the sky and flowers bloom from the crater.
// They walk onto the jump marker ("Okay, let's try this.") and shoot up through the sky, the clouds and into orbit while the
// player steers them through floating sawit and birds (SkyRunMinigame). At the top the camera simply stops following and the
// MC drops out of its frame; a cut to the dive, fist first into the middle of the marked land: an earthquake
// (SmashImpactVisual), the land's trees are gone, and the tiled 3x4 gardens stand there, every bed empty and ready for seed.
// Day5Director starts it and carries on on OnFinished.
public class PlantationSequence : MonoBehaviour
{
    public event EventHandler<OnStepChangedEventArgs> OnStepChanged;
    public class OnStepChangedEventArgs : EventArgs { public Step step; }
    public event EventHandler<OnImpactEventArgs> OnImpact;
    public class OnImpactEventArgs : EventArgs { public Vector3 position; }
    public event EventHandler OnDigBlow;
    public event EventHandler OnFinished;

    public enum Step
    {
        None, EatOneTalk, WaitEatOne, EatingOne, DigTalk, WaitDig, Digging, TinyGrowth, TinyGrowthTalk, WaitEatMany, EatingMany,
        Glitch, Hallucination, HallucinationTalk, WaitMarker, MarkerTalk, Launch, Peak, Fall, Dive, Impact, Reveal, RevealTalk, Done,
    }

    [SerializeField] private Day5SO day5SO;
    [SerializeField] private LandQTE landQTE;
    [SerializeField] private CutsceneActor playerActor;
    [SerializeField] private SkyRunMinigame skyRunMinigame;
    [Tooltip("What the MC eats, and what the sky run's pickups add to the bag.")]
    [SerializeField] private ItemDataSO bunchItemDataSO;
    [SerializeField] private GameObject plantationRoot;
    [Tooltip("The map's trees, bushes and rocks on the marked land: gone with the impact.")]
    [SerializeField] private GameObject[] areaPlantArray;
    [Tooltip("Where the MC digs by hand (the single bed rises there); the MC kneels beside it, facing it.")]
    [SerializeField] private Transform digSpot;
    [SerializeField] private Transform digStandPoint;
    [Tooltip("The jump marker at the marked land's north edge: stepping on it starts the launch.")]
    [SerializeField] private Transform launchPoint;
    [Tooltip("The middle of the marked land: the dive's target.")]
    [SerializeField] private Transform impactPoint;
    [SerializeField] private float eatHoldDuration = 1f;
    [Tooltip("How close (m) to a marker counts as on it.")]
    [SerializeField] private float markerRadius = 1.3f;
    [Tooltip("Digging by hand is hard: many presses per blow.")]
    [SerializeField] private int digPressesPerBlow = 8;
    [SerializeField] private int digBlowCount = 3;
    [Tooltip("Mash presses per bunch eaten.")]
    [SerializeField] private int chompsPerBunch = 3;
    [SerializeField] private float tinyGrowthTalkDelay = 1.3f;
    [SerializeField] private float glitchDuration = 3f;
    [Tooltip("Seconds the childhood TV show plays.")]
    [SerializeField] private float hallucinationDuration = 5.5f;
    [Tooltip("Metres above the impact point the arc peaks: past the clouds, into orbit.")]
    [SerializeField] private float launchHeight = 520f;
    [SerializeField] private float launchDuration = 9f;
    [Tooltip("Sky-run checkpoints laid out evenly up the climb, one row of pickups or birds each.")]
    [SerializeField] private int skyRunRowCount = 12;
    [SerializeField] private float peakHangDuration = .35f;
    [Tooltip("Seconds of free fall at the peak (the MC drops out of the stopped camera's frame).")]
    [SerializeField] private float fallDuration = .8f;
    [SerializeField] private float fallGravity = 30f;
    [SerializeField] private float diveDuration = 2.4f;
    [Tooltip("Seconds between the punch and the plantation standing there.")]
    [SerializeField] private float revealDelay = .5f;
    [SerializeField] private float revealShotDuration = 2.4f;

    private Step step;
    private float timer = -1f;
    private Vector3 launchStart;
    private Vector3 peakPosition;
    private Vector3 fallStart;
    private float fallSpeed;

    private void Start()
    {
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
        landQTE.OnStateChanged += LandQTE_OnStateChanged;
        landQTE.OnFilled += LandQTE_OnFilled;
        plantationRoot.SetActive(false);
    }

    private void Update()
    {
        switch (step)
        {
            case Step.WaitMarker: CheckMarker(); break;
            case Step.Launch: UpdateLaunch(); break;
            case Step.Fall: UpdateFall(); break;
            case Step.Dive: UpdateDive(); break;
        }

        if (timer < 0f) return;
        timer -= Time.deltaTime;
        if (timer < 0f) TimerFinished();
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
        if (GameInput.Instance != null) GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
    }

    // ---------- eating and digging ----------

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        switch (step)
        {
            case Step.EatOneTalk:
                TaskManager.Instance.SetObjectives(day5SO.eatOneObjectiveArray);
                SetStep(Step.WaitEatOne);
                break;
            case Step.DigTalk:
                TaskManager.Instance.SetObjectives(day5SO.digObjectiveArray);
                SetStep(Step.WaitDig);
                break;
            case Step.TinyGrowthTalk:
                TaskManager.Instance.SetObjectives(day5SO.eatManyObjectiveArray);
                SetStep(Step.WaitEatMany);
                break;
            case Step.HallucinationTalk:
                TaskManager.Instance.SetObjectives(day5SO.markerObjectiveArray);
                SetStep(Step.WaitMarker);
                break;
            case Step.MarkerTalk:
                StartLaunch();
                break;
            case Step.RevealTalk:
                playerActor.SetPose(CutscenePose.None);
                SetStep(Step.Done);
                OnFinished?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    // [Interact] anywhere with nothing targeted to eat; on the dig marker to dig
    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (!GameManager.Instance.IsGamePlaying() || PlayerInteraction.Instance.HasSelected()) return;
        if (CanDig())
        {
            StartDigging();
            return;
        }
        if (!IsWaitingToEat()) return;
        int count = Inventory.Instance.GetCount(bunchItemDataSO);
        if (count == 0)
        {
            DialogueManager.Instance.StartDialogue(null, day5SO.noBunchDialogueSO);
            return;
        }
        Hold();
        playerActor.SetPose(CutscenePose.Eat);
        if (step == Step.WaitEatOne)
        {
            Inventory.Instance.TryRemoveItem(bunchItemDataSO);
            SetStep(Step.EatingOne);
            landQTE.StartHold(eatHoldDuration);
        }
        else
        {
            // every bunch left in the bag, one mash fill each
            SetStep(Step.EatingMany);
            landQTE.StartMash(chompsPerBunch, count);
        }
    }

    private void StartDigging()
    {
        Hold();
        Player.Instance.Teleport(digStandPoint.position);
        Player.Instance.FaceTowards(digSpot.position);
        // on the knees, pounding the dirt with bare fists
        playerActor.SetPose(CutscenePose.Crouch);
        SetStep(Step.Digging);
        landQTE.StartMash(digPressesPerBlow, digBlowCount);
    }

    // the hold finished: one bunch down
    private void LandQTE_OnStateChanged(object sender, LandQTE.OnStateChangedEventArgs e)
    {
        if (step != Step.EatingOne || e.state != LandQTE.State.Idle) return;
        TaskManager.Instance.CompleteStory();
        playerActor.SetPose(CutscenePose.None);
        SetStep(Step.DigTalk);
        DialogueManager.Instance.StartDialogue(null, day5SO.digIdeaDialogueSO);
    }

    private void LandQTE_OnFilled(object sender, LandQTE.OnFilledEventArgs e)
    {
        switch (step)
        {
            case Step.Digging:
                playerActor.Punch();
                OnDigBlow?.Invoke(this, EventArgs.Empty);
                if (!e.isLast) return;
                TaskManager.Instance.CompleteStory();
                playerActor.SetPose(CutscenePose.None);
                SetStep(Step.TinyGrowth);
                timer = tinyGrowthTalkDelay;
                break;
            case Step.EatingMany:
                Inventory.Instance.TryRemoveItem(bunchItemDataSO);
                // a big chomp per bunch
                playerActor.Punch();
                if (!e.isLast) return;
                TaskManager.Instance.CompleteStory();
                playerActor.SetPose(CutscenePose.None);
                SetStep(Step.Glitch);
                timer = glitchDuration;
                break;
        }
    }

    // ---------- the jump ----------

    // standing on the marker: one line, then up
    private void CheckMarker()
    {
        if (!GameManager.Instance.IsGamePlaying() || !IsNear(launchPoint)) return;
        Hold();
        TaskManager.Instance.CompleteStory();
        Player.Instance.Teleport(launchPoint.position);
        Player.Instance.FaceTowards(launchPoint.position + launchPoint.forward);
        SetStep(Step.MarkerTalk);
        DialogueManager.Instance.StartDialogue(null, day5SO.markerDialogueSO);
    }

    private void StartLaunch()
    {
        Hold();
        Player.Instance.BeginFlight();
        launchStart = launchPoint.position;
        peakPosition = impactPoint.position + Vector3.up * launchHeight;
        // facing along the flight, so the launch camera behind sees the back and the raised fist
        Player.Instance.transform.SetPositionAndRotation(launchStart, launchPoint.rotation);
        playerActor.SetPose(CutscenePose.SupermanFly);
        Vector3[] rowArray = new Vector3[skyRunRowCount];
        // rows from just above the ground to just short of the top
        for (int i = 0; i < skyRunRowCount; i++) rowArray[i] = GetLaunchPoint((i + 1f) / (skyRunRowCount + 1f));
        skyRunMinigame.Begin(rowArray);
        SetStep(Step.Launch);
        timer = launchDuration;
    }

    // straight across to over the impact point, rising fast and slowing to a stop at the top
    private Vector3 GetLaunchPoint(float t)
    {
        Vector3 across = Vector3.Lerp(launchStart, new Vector3(peakPosition.x, launchStart.y, peakPosition.z), t);
        float rise = 1f - (1f - t) * (1f - t);
        return across + Vector3.up * (peakPosition.y - launchStart.y) * rise;
    }

    private void UpdateLaunch()
    {
        float t = 1f - Mathf.Clamp01(timer / launchDuration);
        Vector3 position = GetLaunchPoint(t) + skyRunMinigame.GetSteerOffset();
        Fly(position, position - Player.Instance.transform.position);
    }

    private void UpdateFall()
    {
        fallSpeed += fallGravity * Time.deltaTime;
        Vector3 position = Player.Instance.transform.position + Vector3.down * fallSpeed * Time.deltaTime;
        Fly(position, Vector3.down);
    }

    // speeding up all the way into the ground
    private void UpdateDive()
    {
        float t = 1f - Mathf.Clamp01(timer / diveDuration);
        Vector3 position = Vector3.Lerp(fallStart, impactPoint.position, t * t);
        Fly(position, impactPoint.position - fallStart);
    }

    // the body lies along the flight: head first up the climb, head first down the dive
    private static void Fly(Vector3 position, Vector3 direction)
    {
        Transform player = Player.Instance.transform;
        player.position = position;
        if (direction.sqrMagnitude < .0001f) return;
        Vector3 facing = Vector3.ProjectOnPlane(player.forward, direction);
        if (facing.sqrMagnitude < .0001f) facing = Vector3.ProjectOnPlane(Vector3.forward, direction);
        player.rotation = Quaternion.LookRotation(facing, direction.normalized);
    }

    // upright again, crouched over the fist, facing the way the impact point does
    private void Land()
    {
        // upright before the controller comes back: switched on head-down, it drops straight through the ground
        Player.Instance.transform.rotation = impactPoint.rotation;
        Player.Instance.Teleport(impactPoint.position);
        playerActor.SetPose(CutscenePose.GroundPunch);
        playerActor.Punch();
        foreach (GameObject plant in areaPlantArray) plant.SetActive(false);
        SetStep(Step.Impact);
        OnImpact?.Invoke(this, new OnImpactEventArgs { position = impactPoint.position });
        timer = revealDelay;
    }

    // ---------- public ----------

    public void Play()
    {
        Hold();
        SetStep(Step.EatOneTalk);
        DialogueManager.Instance.StartDialogue(null, day5SO.eatOneDialogueSO);
    }

    public Step GetStep() => step;
    public bool IsWaitingToEat() => step == Step.WaitEatOne || step == Step.WaitEatMany;
    public bool CanDig() => step == Step.WaitDig && IsNear(digSpot);
    public float GetGlitchDuration() => glitchDuration;
    public float GetHallucinationDuration() => hallucinationDuration;
    // the ground the dive lands on (the sky's altitude bands count from it)
    public Vector3 GetImpactPosition() => impactPoint.position;

    // ---------- helpers ----------

    private bool IsNear(Transform marker)
    {
        Vector3 offset = Player.Instance.transform.position - marker.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= markerRadius * markerRadius;
    }

    private void TimerFinished()
    {
        switch (step)
        {
            case Step.TinyGrowth:
                SetStep(Step.TinyGrowthTalk);
                DialogueManager.Instance.StartDialogue(null, day5SO.tinyGrowthDialogueSO);
                break;
            case Step.Glitch:
                SetStep(Step.Hallucination);
                timer = hallucinationDuration;
                break;
            case Step.Hallucination:
                SetStep(Step.HallucinationTalk);
                DialogueManager.Instance.StartDialogue(null, day5SO.hallucinationDialogueSO);
                break;
            case Step.Launch:
                skyRunMinigame.End();
                SetStep(Step.Peak);
                timer = peakHangDuration;
                break;
            case Step.Peak:
                fallSpeed = 0f;
                SetStep(Step.Fall);
                timer = fallDuration;
                break;
            case Step.Fall:
                fallStart = Player.Instance.transform.position;
                playerActor.SetPose(CutscenePose.SupermanDive);
                SetStep(Step.Dive);
                timer = diveDuration;
                break;
            case Step.Dive:
                Land();
                break;
            case Step.Impact:
                plantationRoot.SetActive(true);
                SetStep(Step.Reveal);
                timer = revealShotDuration;
                break;
            case Step.Reveal:
                SetStep(Step.RevealTalk);
                DialogueManager.Instance.StartDialogue(null, day5SO.revealDialogueSO);
                break;
        }
    }

    // no player control: a dialogue that just ended handed it back
    private static void Hold() => GameManager.Instance.SetState(GameManager.State.Cutscene);

    private void SetStep(Step step)
    {
        this.step = step;
        OnStepChanged?.Invoke(this, new OnStepChangedEventArgs { step = step });
    }
}
