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
    private NavMeshAgent agent;

    private void Awake()
    {
        TryGetComponent(out agent);
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
        OnMoveFinished?.Invoke(this, EventArgs.Empty);
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
        Vector3 direction = target - from;
        direction.y = 0f;
        if (direction.sqrMagnitude > .0001f) transform.forward = direction.normalized;
    }
}
