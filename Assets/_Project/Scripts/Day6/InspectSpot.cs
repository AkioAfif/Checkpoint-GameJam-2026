using System;
using UnityEngine;

// A spot the MC can look at closely with [Interact] (Day 6's cracked soil in the old garden). Offered once, while a director
// switches it on; the director plays what the MC makes of it.
[RequireComponent(typeof(Collider))]
public class InspectSpot : MonoBehaviour, IInteractable
{
    public event EventHandler OnInspected;

    [SerializeField] private Transform promptAnchor;
    [SerializeField] private string promptVerb = "inspect";

    private bool isAvailable;

    public bool IsSelectable() => isAvailable;
    public bool CanInteract(Player player) => true;

    public void Interact(Player player)
    {
        isAvailable = false;
        OnInspected?.Invoke(this, EventArgs.Empty);
    }

    public void SetAvailable(bool isAvailable) => this.isAvailable = isAvailable;
    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => string.Empty;
    public Transform GetPromptAnchor() => promptAnchor;
}
