using System;
using UnityEngine;

// Locked until today's tasks are done. Entering: face the door → (optional line) → camera zooms in → iris closes → next scene.
public class HouseDoor : MonoBehaviour, IInteractable
{
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; public float duration; }

    public enum State { Closed, ZoomingIn, IrisClosing, Done }

    [SerializeField] private float zoomDuration = 1.3f;
    [SerializeField] private float irisDuration = 1.4f;
    [SerializeField] private string promptVerb = "go inside";
    [SerializeField] private string lockedReason = "Finish today's tasks first";
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private Loader.Scene nextScene = Loader.Scene.HouseInterior_Stub;

    private State state;
    private float timer;
    private bool isLocked;
    private bool isWaitingForDialogue;
    private bool isHidden;
    private DialogueSO entryDialogueSO;

    private void Start()
    {
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
    }

    private void Update()
    {
        if (state != State.ZoomingIn && state != State.IrisClosing) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        if (state == State.ZoomingIn)
        {
            SetState(State.IrisClosing, irisDuration);
            return;
        }
        SetState(State.Done, 0f);
        Loader.Load(nextScene);
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (!isWaitingForDialogue) return;
        isWaitingForDialogue = false;
        Enter();
    }

    public bool IsSelectable() => state == State.Closed && !isWaitingForDialogue && !isHidden;
    public bool CanInteract(Player player) => !isLocked && TaskManager.Instance.AreRequiredTasksComplete();

    public void Interact(Player player)
    {
        player.FaceTowards(transform.position);
        if (entryDialogueSO == null)
        {
            Enter();
            return;
        }
        // a story line first, the zoom starts when it ends
        isWaitingForDialogue = true;
        DialogueSO dialogueSO = entryDialogueSO;
        entryDialogueSO = null;
        DialogueManager.Instance.StartDialogue(null, dialogueSO);
    }

    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => lockedReason;
    public Transform GetPromptAnchor() => promptAnchor;

    // the story can keep the door shut regardless of the checklist
    public void SetLocked(bool isLocked) => this.isLocked = isLocked;
    // not targetable at all (no prompt), e.g. while the player has only just stepped out of it
    public void SetHidden(bool isHidden) => this.isHidden = isHidden;
    public void SetEntryDialogue(DialogueSO dialogueSO) => entryDialogueSO = dialogueSO;

    private void Enter()
    {
        TaskManager.Instance.CompleteEnterHouse();
        GameManager.Instance.SetState(GameManager.State.EnteringHouse);
        SetState(State.ZoomingIn, zoomDuration);
    }

    private void SetState(State state, float duration)
    {
        this.state = state;
        timer = duration;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state, duration = duration });
    }
}
