using System;
using UnityEngine;

// A tree (or bush) on Day 3's new field, sitting on its trunk: TreeClearing makes the marked ones punchable, the player's
// [Interact] asks for a punch (TreeClearing runs the mash), and Fell() knocks it down. TreeFallVisual topples the model.
public class PunchableTree : MonoBehaviour, IInteractable
{
    public event EventHandler OnPunchRequested;
    public event EventHandler<OnFelledEventArgs> OnFelled;
    public class OnFelledEventArgs : EventArgs { public Vector3 fallDirection; }

    [Tooltip("The tree's own model; its colliders stop blocking once it falls.")]
    [SerializeField] private Transform treeTransform;
    [Tooltip("Where the MC stands to punch it: on the gameplay camera's side, so the tree never fades out of the way and falls away from the camera.")]
    [SerializeField] private Transform punchPoint;
    [SerializeField] private string promptVerb = "punch";
    [SerializeField] private string blockedReason = "";
    [SerializeField] private Transform promptAnchor;

    private bool isPunchable;
    private bool isFelled;

    public bool IsSelectable() => isPunchable && !isFelled;
    public bool CanInteract(Player player) => true;
    public void Interact(Player player) => OnPunchRequested?.Invoke(this, EventArgs.Empty);
    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => blockedReason;
    public Transform GetPromptAnchor() => promptAnchor;

    public void SetPunchable(bool isPunchable) => this.isPunchable = isPunchable;
    public bool IsFelled() => isFelled;
    public Transform GetTreeTransform() => treeTransform;
    public Transform GetPunchPoint() => punchPoint;

    // falls away from 'from' (flattened)
    public void Fell(Vector3 from)
    {
        if (isFelled) return;
        isFelled = true;
        foreach (Collider collider in treeTransform.GetComponentsInChildren<Collider>()) collider.enabled = false;
        Vector3 direction = transform.position - from;
        direction.y = 0f;
        if (direction.sqrMagnitude < .01f) direction = transform.forward;
        OnFelled?.Invoke(this, new OnFelledEventArgs { fallDirection = direction.normalized });
    }
}
