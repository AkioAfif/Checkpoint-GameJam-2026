using System;
using UnityEngine;

// One job on the overgrown strip by the garden: a weed clump, a rock, the stump, or a patch to till into a plot. While the
// director has it open, [Interact] asks for the work (the director runs the hold or mash), needing a tool from the errands.
// Clear() finishes it: its colliders stop blocking and an optional object (a new plot) appears.
public class YardObstacle : MonoBehaviour, IInteractable
{
    public event EventHandler OnWorkRequested;
    public event EventHandler OnCleared;

    [Tooltip("The tool this job needs (sickle, hoe, rope). Empty = bare hands.")]
    [SerializeField] private ItemDataSO requiredItemDataSO;
    [SerializeField] private string promptVerb = "clear";
    [SerializeField] private string blockedReason = "Need a tool";
    [SerializeField] private Transform promptAnchor;
    [Tooltip("Optional: shown once the job is done (the plot a tilled patch becomes).")]
    [SerializeField] private GameObject revealGameObject;

    private bool isOpen;
    private bool isCleared;

    private void Start()
    {
        if (revealGameObject != null) revealGameObject.SetActive(false);
    }

    public bool IsSelectable() => isOpen && !isCleared;
    public bool CanInteract(Player player) => requiredItemDataSO == null || Inventory.Instance.GetCount(requiredItemDataSO) > 0;
    public void Interact(Player player) => OnWorkRequested?.Invoke(this, EventArgs.Empty);
    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => blockedReason;
    public Transform GetPromptAnchor() => promptAnchor;

    public void SetOpen(bool isOpen) => this.isOpen = isOpen;
    public bool IsCleared() => isCleared;

    public void Clear()
    {
        if (isCleared) return;
        isCleared = true;
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        if (revealGameObject != null) revealGameObject.SetActive(true);
        OnCleared?.Invoke(this, EventArgs.Empty);
    }
}
