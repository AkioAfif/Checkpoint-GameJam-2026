using System;
using UnityEngine;
using UnityEngine.AI;

// Lets the Day 1 director move a character along a short arc and ask for a reaction.
// NpcVisual / PlayerVisual play the emotes, so this class never touches an Animator.
public class CutsceneActor : MonoBehaviour
{
    public event EventHandler<OnEmoteRequestedEventArgs> OnEmoteRequested;
    public class OnEmoteRequestedEventArgs : EventArgs { public Emote emote; }
    public event EventHandler OnMoveFinished;

    private Vector3 from;
    private Vector3 to;
    private float duration;
    private float arcHeight;
    private float timer;
    private bool isMoving;
    private bool isCarrying;
    private NavMeshAgent agent;
    private CharacterController characterController;

    private void Awake()
    {
        TryGetComponent(out agent);
        TryGetComponent(out characterController);
    }

    private void Update()
    {
        if (!isMoving) return;
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * arcHeight;
        if (t < 1f) return;
        isMoving = false;
        if (agent != null) agent.enabled = true; // re-seats the agent on the NavMesh at the new spot
        if (characterController != null) characterController.enabled = true;
        OnMoveFinished?.Invoke(this, EventArgs.Empty);
    }

    public bool IsMoving() => isMoving;
    public bool IsCarrying() => isCarrying;

    // arms out in front, holding something (the visual poses them)
    public void SetCarrying(bool isCarrying) => this.isCarrying = isCarrying;

    public void FaceTowards(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > .0001f) transform.forward = direction.normalized;
    }

    public void PlayEmote(Emote emote) => OnEmoteRequested?.Invoke(this, new OnEmoteRequestedEventArgs { emote = emote });

    // a hop (arcHeight) from where the character stands to 'target', facing the way it goes
    public void MoveTo(Vector3 target, float duration, float arcHeight)
    {
        from = transform.position;
        to = target;
        this.duration = duration;
        this.arcHeight = arcHeight;
        timer = 0f;
        isMoving = true;
        if (agent != null) agent.enabled = false; // an enabled agent would overwrite the move
        if (characterController != null) characterController.enabled = false; // same for the player's controller
        Vector3 direction = target - from;
        direction.y = 0f;
        if (direction.sqrMagnitude > .0001f) transform.forward = direction.normalized;
    }
}
