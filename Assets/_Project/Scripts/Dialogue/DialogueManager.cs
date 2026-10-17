using System;
using System.Collections.Generic;
using UnityEngine;

// Runs one conversation: [Interact] first reveals the whole line, then moves to the next one.
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public event EventHandler<OnDialogueEventArgs> OnDialogueStarted;
    public event EventHandler<OnDialogueEventArgs> OnDialogueEnded;
    public class OnDialogueEventArgs : EventArgs { public Npc npc; }
    public event EventHandler<OnLineChangedEventArgs> OnLineChanged;
    public class OnLineChangedEventArgs : EventArgs { public DialogueSO.DialogueLine line; public Npc npc; }

    [SerializeField] private float charactersPerSecond = 40f;

    private Npc npc;
    private DialogueSO dialogueSO;
    private int lineIndex = -1;
    private float revealedCharacters;
    private readonly HashSet<DialogueSO> rewardGivenSet = new HashSet<DialogueSO>();

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one DialogueManager", this);
        Instance = this;
    }

    private void Start()
    {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    private void Update()
    {
        if (!IsActive()) return;
        revealedCharacters += charactersPerSecond * Time.deltaTime;
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (!IsActive() || GameManager.Instance.IsStateChangedThisFrame()) return;
        if (!IsLineFullyRevealed()) revealedCharacters = GetCurrentLine().text.Length;
        else NextLine();
    }

    public void StartDialogue(Npc npc, DialogueSO dialogueSO)
    {
        this.npc = npc;
        this.dialogueSO = dialogueSO;
        lineIndex = -1;
        GameManager.Instance.SetState(GameManager.State.Dialogue);
        OnDialogueStarted?.Invoke(this, new OnDialogueEventArgs { npc = npc });
        NextLine();
    }

    public bool IsActive() => lineIndex >= 0;
    public DialogueSO.DialogueLine GetCurrentLine() => dialogueSO.lineArray[lineIndex];
    public int GetVisibleCharacterCount() => Mathf.FloorToInt(revealedCharacters);
    public bool IsLineFullyRevealed() => revealedCharacters >= GetCurrentLine().text.Length;

    private void NextLine()
    {
        lineIndex++;
        if (lineIndex >= dialogueSO.lineArray.Length)
        {
            EndDialogue();
            return;
        }
        revealedCharacters = 0f;
        OnLineChanged?.Invoke(this, new OnLineChangedEventArgs { line = GetCurrentLine(), npc = npc });
    }

    private void EndDialogue()
    {
        lineIndex = -1;
        if (dialogueSO.reward.amount > 0 && rewardGivenSet.Add(dialogueSO)) Inventory.Instance.AddItem(dialogueSO.reward.itemDataSO, dialogueSO.reward.amount);
        GameManager.Instance.SetState(GameManager.State.Playing);
        OnDialogueEnded?.Invoke(this, new OnDialogueEventArgs { npc = npc });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
