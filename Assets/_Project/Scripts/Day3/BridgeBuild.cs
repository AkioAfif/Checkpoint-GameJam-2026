using System;
using UnityEngine;

// Day 3 on the CozyFarm map, once the new field is up: tired of the long walk round by the old bridge, the MC imagines one
// right by his garden (the camera on the river, a see-through bridge), walks to the bank and pounds it up out of the water
// (LandQTE mash). The new bridge is walkable from then on. LandSequence starts it and finishes Day 3's new land on OnFinished.
public class BridgeBuild : MonoBehaviour
{
    public event EventHandler<OnStepChangedEventArgs> OnStepChanged;
    public class OnStepChangedEventArgs : EventArgs { public Step step; }
    public event EventHandler<OnSmashEventArgs> OnSmash;
    public class OnSmashEventArgs : EventArgs { public float progressNormalized; public bool isFinal; }
    public event EventHandler OnFinished;

    public enum Step { None, Think, Idea, Wait, Smash, Settle, DoneTalk, Done }

    [SerializeField] private Day3SO day3SO;
    [SerializeField] private LandQTE landQTE;
    [SerializeField] private CutsceneActor playerActor;
    [Tooltip("The new bridge, hidden until the first blow.")]
    [SerializeField] private GameObject bridgeRoot;
    [Tooltip("On the bank where the bridge starts, facing across the river.")]
    [SerializeField] private Transform bridgePoint;
    [SerializeField] private float bridgeRadius = 2.4f;
    [SerializeField] private int pressesPerSmash = 4;
    [SerializeField] private int smashCount = 4;
    [Tooltip("Seconds of the river shot before the MC speaks.")]
    [SerializeField] private float thinkDuration = 1.2f;
    [SerializeField] private float settleDuration = 1.6f;

    private Step step;
    private float timer = -1f;
    private int smashIndex;

    private void Start()
    {
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        landQTE.OnFilled += LandQTE_OnFilled;
        bridgeRoot.SetActive(false);
    }

    private void Update()
    {
        if (timer >= 0f)
        {
            timer -= Time.deltaTime;
            if (timer < 0f) TimerFinished();
        }

        if (step != Step.Wait || !GameManager.Instance.IsGamePlaying()) return;
        Vector3 toPoint = bridgePoint.position - Player.Instance.transform.position;
        toPoint.y = 0f;
        if (toPoint.magnitude < bridgeRadius) StartSmash();
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        switch (step)
        {
            case Step.Idea:
                TaskManager.Instance.SetObjectives(day3SO.bridgeObjectiveArray);
                SetStep(Step.Wait);
                break;
            case Step.DoneTalk:
                SetStep(Step.Done);
                OnFinished?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private void LandQTE_OnFilled(object sender, LandQTE.OnFilledEventArgs e)
    {
        if (step != Step.Smash) return;
        smashIndex++;
        playerActor.Punch();
        OnSmash?.Invoke(this, new OnSmashEventArgs { progressNormalized = (float)smashIndex / smashCount, isFinal = e.isLast });
        if (!e.isLast) return;
        SetStep(Step.Settle);
        timer = settleDuration;
    }

    // ---------- public ----------

    public void Play()
    {
        Hold();
        SetStep(Step.Think);
        timer = thinkDuration;
    }

    public Step GetStep() => step;

    // ---------- helpers ----------

    private void TimerFinished()
    {
        switch (step)
        {
            case Step.Think:
                SetStep(Step.Idea);
                DialogueManager.Instance.StartDialogue(null, day3SO.bridgeIdeaDialogueSO);
                break;
            case Step.Settle:
                playerActor.SetPose(CutscenePose.None);
                // the deck is up: it holds the player from now on
                foreach (Collider collider in bridgeRoot.GetComponentsInChildren<Collider>(true)) collider.enabled = true;
                SetStep(Step.DoneTalk);
                DialogueManager.Instance.StartDialogue(null, day3SO.bridgeDoneDialogueSO);
                break;
        }
    }

    private void StartSmash()
    {
        TaskManager.Instance.CompleteStory();
        Hold();
        Player.Instance.Teleport(bridgePoint.position);
        Player.Instance.FaceTowards(bridgePoint.position + bridgePoint.forward);
        playerActor.SetPose(CutscenePose.Crouch);
        // rising out of the water, not walkable until it's all the way up
        foreach (Collider collider in bridgeRoot.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        bridgeRoot.SetActive(true);
        smashIndex = 0;
        SetStep(Step.Smash);
        landQTE.StartMash(pressesPerSmash, smashCount);
    }

    // no player control: a dialogue that just ended handed it back
    private static void Hold() => GameManager.Instance.SetState(GameManager.State.Cutscene);

    private void SetStep(Step step)
    {
        this.step = step;
        OnStepChanged?.Invoke(this, new OnStepChangedEventArgs { step = step });
    }
}
