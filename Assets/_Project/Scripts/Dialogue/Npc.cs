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
        DialogueSO dialogueSO = overrideDialogueSO != null ? overrideDialogueSO : hasTalked && npcSO.repeatDialogueSO != null ? npcSO.repeatDialogueSO : npcSO.firstDialogueSO;
        hasTalked = true;
        DialogueManager.Instance.StartDialogue(this, dialogueSO);
    }

    // story beats swap what this character says (null = back to first/repeat)
    public void SetOverrideDialogue(DialogueSO dialogueSO) => overrideDialogueSO = dialogueSO;

    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => string.Empty;
    public Transform GetPromptAnchor() => promptAnchor;
    public NpcSO GetNpcSO() => npcSO;
}
