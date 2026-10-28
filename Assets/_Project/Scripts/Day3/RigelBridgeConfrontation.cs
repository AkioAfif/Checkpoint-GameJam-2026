using System;
using UnityEngine;
using UnityEngine.AI;

// Day 3 on the CozyFarm map, once the new bridge stands: halfway across it on the way home, Rigel strides onto the far end and
// stops the MC. He is suspicious of what happened to the forest (and quietly wanted that edge for himself); the MC hears only
// jealousy. Rigel walks home and is gone for the day. LandSequence starts it after BridgeBuild and finishes on OnFinished.
public class RigelBridgeConfrontation : MonoBehaviour
{
    public event EventHandler<OnStepChangedEventArgs> OnStepChanged;
    public class OnStepChangedEventArgs : EventArgs { public Step step; }
    public event EventHandler OnFinished;

    public enum Step { None, WaitCross, Approach, Talk, Leave, Done }

    [SerializeField] private Day3SO day3SO;
    [SerializeField] private Npc rigelNpc;
    [SerializeField] private CutsceneActor rigelActor;
    [Tooltip("The middle of the new bridge: reaching it starts the cutscene.")]
    [SerializeField] private Transform bridgeMidPoint;
    [SerializeField] private float midRadius = 1.2f;
    [Tooltip("The garden end of the bridge, where Rigel steps on and later leaves by (toward his house).")]
    [SerializeField] private Transform rigelEntryPoint;
    [Tooltip("A few steps ahead of the MC, blocking the way (its height is taken from the MC on the deck).")]
    [SerializeField] private Transform rigelStopPoint;
    [SerializeField] private float rigelWalkSpeed = 3.2f;

    private Step step;
    private Vector3 stopPosition;
    private NavMeshAgent rigelAgent;

    private void Awake()
    {
        rigelAgent = rigelNpc.GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        rigelActor.OnMoveFinished += RigelActor_OnMoveFinished;
    }

    private void Update()
    {
        if (step != Step.WaitCross || !GameManager.Instance.IsGamePlaying()) return;
        Vector3 toMid = bridgeMidPoint.position - Player.Instance.transform.position;
        toMid.y = 0f;
        if (toMid.magnitude < midRadius) StartConfrontation();
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        switch (step)
        {
            case Step.Talk:
                // he has the last word, then goes; the MC's verdict follows him off the bridge
                Hold();
                SetStep(Step.Leave);
                WalkRigel(rigelEntryPoint.position);
                DialogueManager.Instance.StartDialogue(null, day3SO.rigelBridgeAfterDialogueSO);
                break;
            case Step.Leave:
                SetStep(Step.Done);
                OnFinished?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private void RigelActor_OnMoveFinished(object sender, EventArgs e)
    {
        if (step == Step.Approach)
        {
            // the bridge has no NavMesh: a re-seated agent would drop him onto the river bed below the deck
            if (rigelAgent != null) rigelAgent.enabled = false;
            rigelActor.transform.position = stopPosition;
            rigelActor.FaceTowards(Player.Instance.transform.position);
            SetStep(Step.Talk);
            DialogueManager.Instance.StartDialogue(rigelNpc, day3SO.rigelBridgeDialogueSO);
        }
        // inside for the rest of the day
        else if (step == Step.Leave || step == Step.Done) rigelNpc.gameObject.SetActive(false);
    }

    public void Play()
    {
        TaskManager.Instance.SetObjectives(day3SO.crossBridgeObjectiveArray);
        SetStep(Step.WaitCross);
    }

    private void StartConfrontation()
    {
        TaskManager.Instance.CompleteStory();
        Hold();
        Player.Instance.FaceTowards(rigelStopPoint.position);
        // placed while inactive, so its agent wakes up on the right spot
        rigelNpc.transform.position = rigelEntryPoint.position;
        rigelNpc.gameObject.SetActive(true);
        SetStep(Step.Approach);
        // on the deck, level with the MC
        stopPosition = rigelStopPoint.position;
        stopPosition.y = Player.Instance.transform.position.y;
        WalkRigel(stopPosition);
    }

    private void WalkRigel(Vector3 target)
    {
        float duration = Mathf.Max(.3f, Vector3.Distance(rigelActor.transform.position, target) / rigelWalkSpeed);
        rigelActor.MoveTo(target, duration, 0f);
    }

    // no player control: a dialogue that just ended handed it back
    private static void Hold() => GameManager.Instance.SetState(GameManager.State.Cutscene);

    private void SetStep(Step step)
    {
        this.step = step;
        OnStepChanged?.Invoke(this, new OnStepChangedEventArgs { step = step });
    }
}
