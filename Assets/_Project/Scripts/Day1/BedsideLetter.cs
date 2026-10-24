using System;
using UnityEngine;

// The debt letter on the bedside table. Reading it opens a popup (the world freezes); closing it starts the player's monologue.
public class BedsideLetter : MonoBehaviour, IInteractable
{
    public event EventHandler OnOpened;
    public event EventHandler OnClosed;

    [SerializeField] private string promptVerb = "read the letter";
    [SerializeField] private Transform promptAnchor;

    private bool isRead;

    public bool IsSelectable() => !isRead;
    public bool CanInteract(Player player) => true;

    public void Interact(Player player)
    {
        isRead = true;
        GameManager.Instance.SetState(GameManager.State.Popup);
        OnOpened?.Invoke(this, EventArgs.Empty);
    }

    public bool IsRead() => isRead;
    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => string.Empty;
    public Transform GetPromptAnchor() => promptAnchor;

    // called by the letter popup when the player dismisses it
    public void Close()
    {
        GameManager.Instance.SetState(GameManager.State.Playing);
        OnClosed?.Invoke(this, EventArgs.Empty);
    }
}
