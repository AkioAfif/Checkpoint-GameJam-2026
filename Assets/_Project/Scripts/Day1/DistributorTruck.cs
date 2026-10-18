using System;
using UnityEngine;

// The Distributor's pickup: races in from the west, skids to a stop in front of the house, and later burns rubber out of the village.
// It only moves; DistributorTruckVisual does the squash, wheel spin and shaking.
public class DistributorTruck : MonoBehaviour
{
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }

    public enum State { Hidden, Driving, Skidding, Parked, Burnout, Leaving }

    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform stopPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform cargoAnchor;
    [SerializeField] private Transform doorPoint;
    [Tooltip("Slow start, then flat out.")]
    [SerializeField] private AnimationCurve driveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float driveDuration = 3.2f;
    [SerializeField] private float skidDuration = .9f;
    [SerializeField] private float skidDistance = 5f;
    [SerializeField] private float burnoutDuration = 1.1f;
    [SerializeField] private float leaveDuration = 2.2f;

    private State state = State.Hidden;
    private float timer;
    private float speedNormalized;
    private Vector3 lastPosition;

    private void Update()
    {
        Vector3 brakePoint = stopPoint.position - (stopPoint.position - startPoint.position).normalized * skidDistance;
        timer += Time.deltaTime;
        switch (state)
        {
            case State.Driving:
                transform.position = Vector3.Lerp(startPoint.position, brakePoint, driveCurve.Evaluate(Mathf.Clamp01(timer / driveDuration)));
                if (timer >= driveDuration) SetState(State.Skidding);
                break;
            case State.Skidding:
                // hard braking: fast at first, then dead stop
                float skid = Mathf.Clamp01(timer / skidDuration);
                transform.position = Vector3.Lerp(brakePoint, stopPoint.position, 1f - (1f - skid) * (1f - skid));
                if (timer >= skidDuration) SetState(State.Parked);
                break;
            case State.Burnout:
                if (timer >= burnoutDuration) SetState(State.Leaving);
                break;
            case State.Leaving:
                float leave = Mathf.Clamp01(timer / leaveDuration);
                transform.position = Vector3.Lerp(stopPoint.position, exitPoint.position, leave * leave);
                if (timer >= leaveDuration) SetState(State.Hidden);
                break;
        }
        if (Time.deltaTime > 0f) speedNormalized = Mathf.Lerp(speedNormalized, (transform.position - lastPosition).magnitude / Time.deltaTime / 12f, 12f * Time.deltaTime);
        lastPosition = transform.position;
    }

    public void Drive()
    {
        transform.SetPositionAndRotation(startPoint.position, startPoint.rotation);
        lastPosition = startPoint.position;
        SetState(State.Driving);
    }

    // the Distributor is aboard: wheels spin in place, then off it goes
    public void Leave() => SetState(State.Burnout);

    public State GetState() => state;
    // 0..1 through the current state
    public float GetStateProgress(float stateDuration) => Mathf.Clamp01(timer / stateDuration);
    // 0 standing, 1 about 12 m/s
    public float GetSpeedNormalized() => speedNormalized;
    public float GetSkidDuration() => skidDuration;
    public float GetBurnoutDuration() => burnoutDuration;
    public Transform GetCargoAnchor() => cargoAnchor;
    public Transform GetDoorPoint() => doorPoint;

    private void SetState(State state)
    {
        this.state = state;
        timer = 0f;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }
}
