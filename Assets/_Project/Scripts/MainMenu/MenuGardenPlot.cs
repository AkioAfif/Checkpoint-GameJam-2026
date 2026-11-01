using System;
using UnityEngine;

// One bed in a menu villager's garden: planted, it grows sprout → grown → ripe on the MenuTimeLapse clock until it is
// harvested and left empty for the next planting.
public class MenuGardenPlot : MonoBehaviour
{
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs
    {
        public State state;
    }

    public enum State { Empty, Sprout, Grown, Ripe }

    [Tooltip("Villager seconds from sprout to grown, and again from grown to ripe.")]
    [SerializeField] private float stageDuration = 14f;
    [SerializeField] private State startState;

    private State state;
    private float growTimer;

    private void Start()
    {
        SetState(startState);
    }

    private void Update()
    {
        if (state != State.Sprout && state != State.Grown) return;
        growTimer += MenuTimeLapse.Instance.GetDeltaTime();
        if (growTimer < stageDuration) return;
        SetState(state == State.Sprout ? State.Grown : State.Ripe);
    }

    // planting an empty bed, harvesting a ripe one; a bed still growing is only tended
    public void Tend()
    {
        if (state == State.Empty) SetState(State.Sprout);
        else if (state == State.Ripe) SetState(State.Empty);
    }

    private void SetState(State state)
    {
        this.state = state;
        growTimer = 0f;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }
}
