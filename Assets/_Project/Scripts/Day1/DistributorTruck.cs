using System;
using UnityEngine;

// The Distributor's pickup: races in from the west over the river bridge, handbrake-spins to a stop by the house facing back west, and later burns rubber out the way it came.
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
    [SerializeField] private Transform tailgatePoint;
    [Tooltip("The river bridge the road crosses; the truck rides up and over its arched deck.")]
    [SerializeField] private Transform bridge;
    // must match EnvironmentPrefabBuilder.Bridge(): 8 m long along x, plank tops at .14 + .45 * sin(pi * t)
    [SerializeField] private float bridgeLength = 8f;
    [SerializeField] private float bridgeBaseHeight = .14f;
    [SerializeField] private float bridgeRise = .45f;
    [Tooltip("Slow start, then still flat out when it hits the brakes (end slope > 1).")]
    [SerializeField] private AnimationCurve driveCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 1.6f, 1.6f));
    [SerializeField] private float driveDuration = 3f;
    // skid entry speed (2 * skidDistance / skidDuration) matches the drive's end speed, so the brake doesn't jolt
    [SerializeField] private float skidDuration = 1.2f;
    [SerializeField] private float skidDistance = 10f;
    [Tooltip("Share of the skid spent spinning; the rest it slides on backwards, already turned round.")]
    [SerializeField] private float skidSpinPortion = .65f;
    [Tooltip("Yaw turned during the skid; -180 swings the nose toward the house and parks it facing the way it came.")]
    [SerializeField] private float skidSpinAngle = -180f;
    [SerializeField] private float burnoutDuration = 1.1f;
    [SerializeField] private float leaveDuration = 2.8f;

    private State state = State.Hidden;
    private float timer;
    private float speedNormalized;
    private Vector3 lastPosition;
    private Quaternion parkedRotation;

    private void Update()
    {
        Vector3 brakePoint = stopPoint.position - (stopPoint.position - startPoint.position).normalized * skidDistance;
        timer += Time.deltaTime;
        switch (state)
        {
            case State.Driving:
                transform.position = OnRoad(Vector3.Lerp(startPoint.position, brakePoint, driveCurve.Evaluate(Mathf.Clamp01(timer / driveDuration))));
                if (timer >= driveDuration) SetState(State.Skidding);
                break;
            case State.Skidding:
                // handbrake: the momentum carries it on east while it whips round, then it slides on backwards to a stop
                float skid = Mathf.Clamp01(timer / skidDuration);
                float slide = 1f - (1f - skid) * (1f - skid);
                float spin = Mathf.Clamp01(skid / skidSpinPortion);
                spin = 1f - (1f - spin) * (1f - spin);
                transform.SetPositionAndRotation(Vector3.Lerp(brakePoint, stopPoint.position, slide), Quaternion.AngleAxis(skidSpinAngle * spin, Vector3.up) * startPoint.rotation);
                if (timer >= skidDuration)
                {
                    parkedRotation = transform.rotation;
                    SetState(State.Parked);
                }
                break;
            case State.Burnout:
                if (timer >= burnoutDuration) SetState(State.Leaving);
                break;
            case State.Leaving:
                float leave = Mathf.Clamp01(timer / leaveDuration);
                transform.SetPositionAndRotation(OnRoad(Vector3.Lerp(stopPoint.position, exitPoint.position, leave * leave)), parkedRotation);
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
    public Transform GetTailgatePoint() => tailgatePoint;

    // lifts a road position onto the bridge deck while it is over the river (the bridge runs along x)
    private Vector3 OnRoad(Vector3 position)
    {
        float t = (position.x - bridge.position.x) / bridgeLength + .5f;
        if (t > 0f && t < 1f) position.y += bridgeBaseHeight + bridgeRise * Mathf.Sin(Mathf.PI * t);
        return position;
    }

    private void SetState(State state)
    {
        this.state = state;
        timer = 0f;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }
}
