using UnityEngine;

public class Npc : MonoBehaviour, IInteractable
{
    [SerializeField] private NpcSO npcSO;
    [SerializeField] private string promptVerb = "talk";
    [SerializeField] private Transform promptAnchor;

    private bool hasTalked;
    private DialogueSO overrideDialogueSO;

    public bool IsSelectable() => true;
    public bool CanInteract(Player player) => true;

    public void Interact(Player player)
    {
        DialogueSO dialogueSO = GetNextDialogue();
        hasTalked = true;
        DialogueManager.Instance.StartDialogue(this, dialogueSO);
    }

    // story beats swap what this character says (null = back to first/repeat)
    public void SetOverrideDialogue(DialogueSO dialogueSO) => overrideDialogueSO = dialogueSO;

    // what the next talk will play
    public DialogueSO GetNextDialogue() => overrideDialogueSO != null ? overrideDialogueSO : IsFirstTalkOver() && npcSO.repeatDialogueSO != null ? npcSO.repeatDialogueSO : npcSO.firstDialogueSO;

    // a first talk that hands something over only plays while it still can (Ratna's bottles: Day 1, once)
    private bool IsFirstTalkOver() =>
        hasTalked || (npcSO.firstDialogueSO != null && npcSO.firstDialogueSO.reward.amount > 0 && !DialogueManager.Instance.HasPendingReward(npcSO.firstDialogueSO));

    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => string.Empty;
    public Transform GetPromptAnchor() => promptAnchor;
    public NpcSO GetNpcSO() => npcSO;
}
