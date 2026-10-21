using System;
using UnityEngine;

// Day 3's new land, played after the first sale. The MC wishes for more field over the empty grass, remembers the Distributor
// (a grayscale flashback), eats a Lv.2 bunch while Rigel sprints over in shock, glitches, and pounds the four plots up out of
// the ground. Rigel backs off into his house. Visuals, cameras and UI listen to OnStepChanged / OnSmash.
public class LandSequence : MonoBehaviour
{
    public event EventHandler<OnStepChangedEventArgs> OnStepChanged;
    public class OnStepChangedEventArgs : EventArgs { public Step step; public Shot shot; }
    public event EventHandler<OnSmashEventArgs> OnSmash;
    public class OnSmashEventArgs : EventArgs { public float progressNormalized; public bool isFinal; }
    public event EventHandler OnFinished;

    public enum Step { None, Inspect, Realise, Flashback, FlashbackSolo, Return, WaitEat, RigelAlarm, Eat, BlinkIn, Glitch, BlinkOut, BuffTalk, WaitSmash, Smash, FinalSmash, Retreat, Done }
    public enum Shot { None, EmptyLand, FlashbackTwo, FlashbackDistributor, Rigel, SmashFront }

    // Rigel's cutscene moves, in order
    private enum RigelMove { None, Hop, Sprint, StepBack, StepBackAgain, RunHome }

    [SerializeField] private Day3SO day3SO;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private LandQTE landQTE;
    [SerializeField] private ItemDataSO bunchItemDataSO;
    [SerializeField] private CutsceneActor playerActor;
    [SerializeField] private Npc rigelNpc;
    [SerializeField] private CutsceneActor rigelActor;
    [Tooltip("A copy of the Distributor, hidden outside the flashback.")]
    [SerializeField] private Npc distributorNpc;
    [SerializeField] private CutsceneActor distributorActor;
    [Tooltip("The four new plots, hidden until the first blow.")]
    [SerializeField] private GameObject landPlotsRoot;
    [Tooltip("The middle of the empty grass.")]
    [SerializeField] private Transform landCentre;
    [SerializeField] private Transform flashbackPlayerPoint;
    [SerializeField] private Transform flashbackDistributorPoint;
    [Tooltip("Where the MC kneels to pound the ground, facing the plots.")]
    [SerializeField] private Transform smashPoint;
    [SerializeField] private Transform rigelHousePoint;
    [Tooltip("Where Rigel has followed to, watching the MC pound the ground.")]
    [SerializeField] private Transform rigelWatchPoint;
    [SerializeField] private float smashRadius = 2.2f;
    [SerializeField] private int pressesPerSmash = 4;
    [SerializeField] private int smashCount = 5;
    [SerializeField] private float eatHoldDuration = 1f;
    [SerializeField] private float blackHoldDuration = 1f;
    [SerializeField] private float blinkDuration = .3f;
    [SerializeField] private float glitchDuration = .7f;
    [Tooltip("Three small steps.")]
    [SerializeField] private float rigelStepBackDistance = 1.2f;
    [SerializeField] private float rigelStepBackDuration = 1.5f;
    [SerializeField] private float rigelSprintSpeed = 9f;

    private Step step;
    private Shot shot;
    private RigelMove rigelMove;
    private float timer = -1f;
    private bool isFadePlaying;
    private Vector3 playerReturnPosition;
    private Quaternion playerReturnRotation;
    private int smashIndex;

    private void Start()
    {
        screenFade.OnBlack += ScreenFade_OnBlack;
        screenFade.OnFinished += ScreenFade_OnFinished;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
        rigelActor.OnMoveFinished += RigelActor_OnMoveFinished;
        landQTE.OnStateChanged += LandQTE_OnStateChanged;
        landQTE.OnFilled += LandQTE_OnFilled;
    }

    private void Update()
    {
        if (timer >= 0f)
        {
            timer -= Time.deltaTime;
            if (timer < 0f) TimerFinished();
        }

        if (step != Step.WaitSmash || !GameManager.Instance.IsGamePlaying()) return;
        Vector3 toPoint = smashPoint.position - Player.Instance.transform.position;
        toPoint.y = 0f;
        if (toPoint.magnitude < smashRadius) StartSmash();
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
        if (GameInput.Instance != null) GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
    }

    // ---------- handlers ----------

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        switch (step)
        {
            case Step.Inspect:
                Hold();
                playerActor.PlayEmote(Emote.Surprised);
                playerActor.SetPose(CutscenePose.Realise);
                SetStep(Step.Realise, Shot.EmptyLand);
                timer = 1f;
                break;
            case Step.FlashbackSolo:
                Hold();
                PlayFade();
                break;
            case Step.Return:
                TaskManager.Instance.SetObjectives(day3SO.eatObjectiveArray);
                SetStep(Step.WaitEat, Shot.None);
                break;
            case Step.RigelAlarm:
                Hold();
                playerActor.SetPose(CutscenePose.Eat);
                SetStep(Step.Eat, Shot.None);
                landQTE.StartHold(eatHoldDuration);
                break;
            case Step.BuffTalk:
                TaskManager.Instance.SetObjectives(day3SO.smashObjectiveArray);
                SetStep(Step.WaitSmash, Shot.None);
                MoveRigel(RigelMove.StepBack);
                break;
            case Step.Retreat:
                MoveRigel(RigelMove.RunHome);
                SetStep(Step.Done, Shot.None);
                OnFinished?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (step != Step.WaitEat || !GameManager.Instance.IsGamePlaying() || PlayerInteraction.Instance.HasSelected()) return;
        if (!Inventory.Instance.TryRemoveItem(bunchItemDataSO))
        {
            DialogueManager.Instance.StartDialogue(null, day3SO.noBunchDialogueSO);
            return;
        }
        TaskManager.Instance.CompleteStory();
        Hold();
        SetStep(Step.RigelAlarm, Shot.Rigel);
        rigelActor.PlayEmote(Emote.Surprised);
        MoveRigel(RigelMove.Hop);
    }

    private void ScreenFade_OnBlack(object sender, EventArgs e)
    {
        if (!isFadePlaying) return;
        if (step == Step.Realise)
        {
            // the flashback: the MC beside the Distributor by the Fabricator, some time ago
            playerReturnPosition = Player.Instance.transform.position;
            playerReturnRotation = Player.Instance.transform.rotation;
            playerActor.SetPose(CutscenePose.None);
            Player.Instance.Teleport(flashbackPlayerPoint.position);
            Player.Instance.FaceTowards(flashbackDistributorPoint.position);
            // placed while inactive, so its agent wakes up on the right spot
            distributorNpc.transform.SetPositionAndRotation(flashbackDistributorPoint.position, flashbackDistributorPoint.rotation);
            distributorNpc.gameObject.SetActive(true);
            SetStep(Step.Flashback, Shot.FlashbackTwo);
        }
        else if (step == Step.FlashbackSolo)
        {
            distributorNpc.gameObject.SetActive(false);
            Player.Instance.Teleport(playerReturnPosition);
            Player.Instance.transform.rotation = playerReturnRotation;
            SetStep(Step.Return, Shot.None);
        }
    }

    private void ScreenFade_OnFinished(object sender, EventArgs e)
    {
        if (!isFadePlaying) return;
        isFadePlaying = false;
        if (step == Step.Flashback) timer = 1.5f;
        else if (step == Step.Return) DialogueManager.Instance.StartDialogue(null, day3SO.tryEatDialogueSO);
    }

    private void RigelActor_OnMoveFinished(object sender, EventArgs e)
    {
        RigelMove finished = rigelMove;
        rigelMove = RigelMove.None;
        switch (finished)
        {
            case RigelMove.Hop:
                timer = .25f;
                break;
            case RigelMove.Sprint:
                rigelActor.FaceTowards(Player.Instance.transform.position);
                playerActor.SetPose(CutscenePose.Eat);
                SetStep(Step.RigelAlarm, Shot.None);
                DialogueManager.Instance.StartDialogue(rigelNpc, day3SO.rigelShoutDialogueSO);
                break;
            case RigelMove.StepBack:
                if (step == Step.Retreat) timer = 1f;
                break;
            case RigelMove.StepBackAgain:
                Hold();
                DialogueManager.Instance.StartDialogue(rigelNpc, day3SO.rigelPretendDialogueSO);
                break;
            case RigelMove.RunHome:
                // inside for the rest of the day
                rigelNpc.gameObject.SetActive(false);
                break;
        }
    }

    private void LandQTE_OnStateChanged(object sender, LandQTE.OnStateChangedEventArgs e)
    {
        if (step != Step.Eat || e.state != LandQTE.State.Idle) return;
        // eaten: two blinks, the world tears, one more blink
        playerActor.SetPose(CutscenePose.None);
        SetStep(Step.BlinkIn, Shot.None);
        timer = blinkDuration * 2f;
    }

    private void LandQTE_OnFilled(object sender, LandQTE.OnFilledEventArgs e)
    {
        if (!e.isLast)
        {
            Smash(false);
            return;
        }
        // the last blow: wind up both fists, filmed from the front
        playerActor.SetPose(CutscenePose.OverheadSmash);
        SetStep(Step.FinalSmash, Shot.SmashFront);
        timer = .7f;
    }

    // ---------- public ----------

    public void Play()
    {
        Hold();
        Player.Instance.FaceTowards(landCentre.position);
        SetStep(Step.Inspect, Shot.EmptyLand);
        timer = 1f;
    }

    public Step GetStep() => step;
    public Shot GetShot() => shot;
    public float GetGlitchDuration() => glitchDuration;
    public float GetBlinkDuration() => blinkDuration;

    // ---------- helpers ----------

    private void TimerFinished()
    {
        switch (step)
        {
            case Step.Inspect:
                DialogueManager.Instance.StartDialogue(null, day3SO.landWishDialogueSO);
                break;
            case Step.Realise:
                PlayFade();
                break;
            case Step.Flashback:
                SetStep(Step.FlashbackSolo, Shot.FlashbackDistributor);
                distributorActor.SetPose(CutscenePose.AdjustGlasses);
                timer = 1.4f;
                break;
            case Step.FlashbackSolo:
                distributorActor.SetPose(CutscenePose.None);
                DialogueManager.Instance.StartDialogue(distributorNpc, day3SO.flashbackDialogueSO);
                break;
            case Step.RigelAlarm:
                MoveRigel(RigelMove.Sprint);
                break;
            case Step.BlinkIn:
                SetStep(Step.Glitch, Shot.None);
                timer = glitchDuration;
                break;
            case Step.Glitch:
                SetStep(Step.BlinkOut, Shot.None);
                timer = blinkDuration;
                break;
            case Step.BlinkOut:
                SetStep(Step.BuffTalk, Shot.None);
                DialogueManager.Instance.StartDialogue(rigelNpc, day3SO.buffDialogueSO);
                break;
            case Step.FinalSmash:
                if (smashIndex < smashCount)
                {
                    Smash(true);
                    timer = 2f;
                    break;
                }
                // the dust settles: Rigel has seen enough
                playerActor.SetPose(CutscenePose.None);
                SetStep(Step.Retreat, Shot.None);
                MoveRigel(RigelMove.StepBack);
                break;
            case Step.Retreat:
                MoveRigel(RigelMove.StepBackAgain);
                break;
        }
    }

    private void StartSmash()
    {
        TaskManager.Instance.CompleteStory();
        Hold();
        Player.Instance.Teleport(smashPoint.position);
        Player.Instance.FaceTowards(landCentre.position);
        playerActor.SetPose(CutscenePose.Crouch);
        landPlotsRoot.SetActive(true);
        // he followed at a safe distance, staring
        Vector3 toPlayer = smashPoint.position - rigelWatchPoint.position;
        toPlayer.y = 0f;
        rigelActor.transform.rotation = Quaternion.LookRotation(toPlayer);
        rigelActor.MoveTo(rigelWatchPoint.position, .05f, 0f, true);
        smashIndex = 0;
        SetStep(Step.Smash, Shot.None);
        landQTE.StartMash(pressesPerSmash, smashCount);
    }

    private void Smash(bool isFinal)
    {
        smashIndex++;
        playerActor.Punch();
        OnSmash?.Invoke(this, new OnSmashEventArgs { progressNormalized = (float)smashIndex / smashCount, isFinal = isFinal });
    }

    private void MoveRigel(RigelMove move)
    {
        rigelMove = move;
        Vector3 rigel = rigelActor.transform.position;
        Vector3 player = Player.Instance.transform.position;
        Vector3 away = rigel - player;
        away.y = 0f;
        away = away.sqrMagnitude > .01f ? away.normalized : -rigelActor.transform.forward;
        switch (move)
        {
            case RigelMove.Hop:
                rigelActor.MoveTo(rigel, .45f, .55f);
                break;
            case RigelMove.Sprint:
                Vector3 target = player + away * 1.4f;
                rigelActor.MoveTo(target, Mathf.Max(.3f, Vector3.Distance(rigel, target) / rigelSprintSpeed), 0f);
                break;
            case RigelMove.StepBack:
            case RigelMove.StepBackAgain:
                rigelActor.FaceTowards(player);
                rigelActor.MoveTo(rigel + away * rigelStepBackDistance, rigelStepBackDuration, 0f, true);
                break;
            case RigelMove.RunHome:
                rigelActor.MoveTo(rigelHousePoint.position, Mathf.Max(.3f, Vector3.Distance(rigel, rigelHousePoint.position) / rigelSprintSpeed), 0f);
                break;
        }
    }

    private void PlayFade()
    {
        isFadePlaying = true;
        screenFade.Play("", blackHoldDuration);
    }

    // no player control: a dialogue that just ended handed it back
    private static void Hold() => GameManager.Instance.SetState(GameManager.State.Cutscene);

    private void SetStep(Step step, Shot shot)
    {
        this.step = step;
        this.shot = shot;
        OnStepChanged?.Invoke(this, new OnStepChangedEventArgs { step = step, shot = shot });
    }
}
