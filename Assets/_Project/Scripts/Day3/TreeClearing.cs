using System;
using UnityEngine;

// Day 3 on the CozyFarm map, after the buff: the MC realises the trees on the new field can go. Walk up to each marked tree
// and mash [Interact] (LandQTE) to punch it down; the first one gets a line, and with the last one the rest of the trees on
// the field fall too. LandSequence starts it and carries on to the land smash on OnFinished.
public class TreeClearing : MonoBehaviour
{
    public event EventHandler<OnStepChangedEventArgs> OnStepChanged;
    public class OnStepChangedEventArgs : EventArgs { public Step step; }
    public event EventHandler<OnHitEventArgs> OnHit;
    public class OnHitEventArgs : EventArgs { public Vector3 position; public bool isFinal; }
    public event EventHandler OnFinished;

    public enum Step { None, Idea, Wait, Punching, FirstTreeTalk, Clearing, ClearedTalk, Done }

    [SerializeField] private Day3SO day3SO;
    [SerializeField] private LandQTE landQTE;
    [SerializeField] private CutsceneActor playerActor;
    [Tooltip("The trees the player punches down, one by one.")]
    [SerializeField] private PunchableTree[] markedTreeArray;
    [Tooltip("Everything else on the new field: falls by itself after the last marked tree.")]
    [SerializeField] private PunchableTree[] extraTreeArray;
    [SerializeField] private int pressesPerBlow = 4;
    [SerializeField] private int blowsPerTree = 3;
    [Tooltip("Seconds between a tree falling and the next beat.")]
    [SerializeField] private float afterFallDelay = 1.1f;
    [SerializeField] private float extraFallInterval = .3f;
    [SerializeField] private float clearedTalkDelay = 1.6f;

    private Step step;
    private float timer = -1f;
    private PunchableTree currentTree;
    private int felledCount;
    private int extraIndex;

    private void Start()
    {
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        landQTE.OnFilled += LandQTE_OnFilled;
        foreach (PunchableTree tree in markedTreeArray) tree.OnPunchRequested += PunchableTree_OnPunchRequested;
    }

    private void Update()
    {
        if (timer < 0f) return;
        timer -= Time.deltaTime;
        if (timer < 0f) TimerFinished();
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
                TaskManager.Instance.SetObjectives(day3SO.treeObjectiveArray);
                foreach (PunchableTree tree in markedTreeArray) tree.SetPunchable(true);
                SetStep(Step.Wait);
                break;
            case Step.FirstTreeTalk:
                SetStep(Step.Wait);
                break;
            case Step.ClearedTalk:
                SetStep(Step.Done);
                OnFinished?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private void PunchableTree_OnPunchRequested(object sender, EventArgs e)
    {
        if (step != Step.Wait || !GameManager.Instance.IsGamePlaying()) return;
        currentTree = (PunchableTree)sender;
        Hold();
        Player.Instance.Teleport(currentTree.GetPunchPoint().position);
        Player.Instance.FaceTowards(currentTree.transform.position);
        playerActor.SetPose(CutscenePose.Jab);
        SetStep(Step.Punching);
        landQTE.StartMash(pressesPerBlow, blowsPerTree);
    }

    private void LandQTE_OnFilled(object sender, LandQTE.OnFilledEventArgs e)
    {
        if (step != Step.Punching) return;
        playerActor.Punch();
        OnHit?.Invoke(this, new OnHitEventArgs { position = currentTree.transform.position, isFinal = e.isLast });
        if (!e.isLast) return;

        currentTree.Fell(Player.Instance.transform.position);
        felledCount++;
        TaskManager.Instance.CompleteStory();
        playerActor.SetPose(CutscenePose.None);
        timer = afterFallDelay;
    }

    // ---------- public ----------

    public void Play()
    {
        Hold();
        SetStep(Step.Idea);
        DialogueManager.Instance.StartDialogue(null, day3SO.treesIdeaDialogueSO);
    }

    public Step GetStep() => step;

    // ---------- helpers ----------

    private void TimerFinished()
    {
        switch (step)
        {
            case Step.Punching:
                if (felledCount >= markedTreeArray.Length)
                {
                    extraIndex = 0;
                    SetStep(Step.Clearing);
                    timer = extraFallInterval;
                }
                else if (felledCount == 1)
                {
                    SetStep(Step.FirstTreeTalk);
                    DialogueManager.Instance.StartDialogue(null, day3SO.firstTreeDialogueSO);
                }
                else
                {
                    SetStep(Step.Wait);
                    GameManager.Instance.SetState(GameManager.State.Playing);
                }
                break;
            case Step.Clearing:
                // the rest topple one after another, away from the MC
                if (extraIndex < extraTreeArray.Length)
                {
                    extraTreeArray[extraIndex].Fell(Player.Instance.transform.position);
                    extraIndex++;
                    timer = extraIndex < extraTreeArray.Length ? extraFallInterval : clearedTalkDelay;
                    break;
                }
                SetStep(Step.ClearedTalk);
                DialogueManager.Instance.StartDialogue(null, day3SO.treesClearedDialogueSO);
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
