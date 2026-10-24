using System;
using UnityEngine;

// The bedroom door stays locked until the letter has been read.
public class BedroomDoor : MonoBehaviour, IInteractable
{
    public event EventHandler OnPassed;

    [SerializeField] private string promptVerb = "go outside";
    [SerializeField] private string lockedReason = "Locked. What's that letter on the table?";
    [SerializeField] private Transform promptAnchor;

    private bool isLocked = true;
    private bool isUsed;

    public bool IsSelectable() => !isUsed;
    public bool CanInteract(Player player) => !isLocked;

    public void Interact(Player player)
    {
        isUsed = true;
        OnPassed?.Invoke(this, EventArgs.Empty);
    }

    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => lockedReason;
    public Transform GetPromptAnchor() => promptAnchor;

    public void SetLocked(bool isLocked) => this.isLocked = isLocked;
}
