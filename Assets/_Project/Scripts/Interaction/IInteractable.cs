using UnityEngine;

// Anything the player can target with [Interact]: resources, NPCs, the Fabricator, farm plots, the house door.
public interface IInteractable
{
    // false = ignored by targeting (already collected, already planted, ...)
    bool IsSelectable();
    // false = targeted and outlined, but the prompt shows GetBlockedReason() instead of the key
    bool CanInteract(Player player);
    void Interact(Player player);
    string GetPromptVerb();
    string GetBlockedReason();
    Transform GetPromptAnchor();
}
