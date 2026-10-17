using System;
using UnityEngine;

// Locked until today's tasks are done. Entering: face the door → camera zooms in → iris closes → next scene.
public class HouseDoor : MonoBehaviour, IInteractable
{
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; public float duration; }

    public enum State { Closed, ZoomingIn, IrisClosing, Done }

    [SerializeField] private float zoomDuration = 1.3f;
    [SerializeField] private float irisDuration = 1.4f;
    [SerializeField] private string promptVerb = "go inside";
    [SerializeField] private string lockedReason = "Finish today's tasks first";
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private Loader.Scene nextScene = Loader.Scene.HouseInterior_Stub;

    private State state;
    private float timer;

    private void Update()
    {
        if (state != State.ZoomingIn && state != State.IrisClosing) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        if (state == State.ZoomingIn)
        {
            SetState(State.IrisClosing, irisDuration);
            return;
        }
        SetState(State.Done, 0f);
        Loader.Load(nextScene);
    }

    public bool IsSelectable() => state == State.Closed;
    public bool CanInteract(Player player) => TaskManager.Instance.AreRequiredTasksComplete();

    public void Interact(Player player)
    {
        player.FaceTowards(transform.position);
        TaskManager.Instance.CompleteEnterHouse();
        GameManager.Instance.SetState(GameManager.State.EnteringHouse);
        SetState(State.ZoomingIn, zoomDuration);
    }

    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => lockedReason;
    public Transform GetPromptAnchor() => promptAnchor;

    private void SetState(State state, float duration)
    {
        this.state = state;
        timer = duration;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state, duration = duration });
    }
}
