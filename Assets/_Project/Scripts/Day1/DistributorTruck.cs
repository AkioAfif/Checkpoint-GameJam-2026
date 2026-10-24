using System;
using UnityEngine;

// The Distributor's pickup: races in along its road route (or the straight startPoint→stopPoint line when no route is set), handbrake-spins to a stop and later burns rubber out the way it came.
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
    [Tooltip("Road waypoints from the entrance to the stop, in order. Empty = straight from startPoint to stopPoint.")]
    [SerializeField] private Transform[] routePoints = Array.Empty<Transform>();
    [Tooltip("Degrees the nose swings at each route point (0 = none). A drift is a counter-swing as it passes that point.")]
    [SerializeField] private float[] routeDriftDegrees = Array.Empty<float>();
    [Tooltip("Metres either side of a drift point over which the swing eases in and out.")]
    [SerializeField] private float driftWindow = 3f;
    [Tooltip("Metres either side of a route corner over which the heading turns.")]
    [SerializeField] private float cornerWindow = 1.5f;
    [Tooltip("Slow start, then still flat out when it hits the brakes (end slope > 1).")]
    [SerializeField] private AnimationCurve driveCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 1.6f, 1.6f));
    [SerializeField] private float driveDuration = 3f;
    // skid entry speed (2 * skidDistance / skidDuration) matches the drive's end speed, so the brake doesn't jolt
    [SerializeField] private float skidDuration = 1.2f;
    [SerializeField] private float skidDistance = 10f;
    [Tooltip("Share of the skid spent spinning; the rest it slides on backwards, already turned round.")]
    [SerializeField] private float skidSpinPortion = .65f;
    [Tooltip("Yaw turned during the skid; -180 swings the nose toward the house and parks it facing the way it came. With a route, it turns from the last road heading (+90 turns a westbound truck north).")]
    [SerializeField] private float skidSpinAngle = -180f;
    [SerializeField] private float burnoutDuration = 1.1f;
    [SerializeField] private float leaveDuration = 2.8f;

    private State state = State.Hidden;
    private float timer;
    private float speedNormalized;
    private Vector3 lastPosition;
    private Quaternion parkedRotation;
    // distance along the route to each route point, built once (the route is fixed in the scene)
    private float[] routeDistances = Array.Empty<float>();

    private void Awake()
    {
        routeDistances = new float[routePoints.Length];
        for (int i = 1; i < routePoints.Length; i++)
            routeDistances[i] = routeDistances[i - 1] + Vector3.Distance(routePoints[i - 1].position, routePoints[i].position);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        switch (state)
        {
            case State.Driving:
                float progress = driveCurve.Evaluate(Mathf.Clamp01(timer / driveDuration));
                if (HasRoute())
                {
                    float distance = progress * BrakeDistance();
                    transform.SetPositionAndRotation(OnRoad(RoutePosition(distance)), RouteRotation(distance));
                }
                else
                {
                    transform.position = OnRoad(Vector3.Lerp(startPoint.position, BrakePoint(), progress));
                }
                if (timer >= driveDuration) SetState(State.Skidding);
                break;
            case State.Skidding:
                // handbrake: the momentum carries it on east while it whips round, then it slides on backwards to a stop
                float skid = Mathf.Clamp01(timer / skidDuration);
                float slide = 1f - (1f - skid) * (1f - skid);
                float spin = Mathf.Clamp01(skid / skidSpinPortion);
                spin = 1f - (1f - spin) * (1f - spin);
                transform.SetPositionAndRotation(Vector3.Lerp(BrakePoint(), stopPoint.position, slide), Quaternion.AngleAxis(skidSpinAngle * spin, Vector3.up) * ArrivalRotation());
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
                if (HasRoute())
                {
                    // backs out along the route it came in on, turning the nose round from the parked pose
                    float distance = RouteLength() * (1f - leave * leave);
                    Quaternion reversed = RouteRotation(distance) * Quaternion.Euler(0f, 180f, 0f);
                    transform.SetPositionAndRotation(OnRoad(RoutePosition(distance)), Quaternion.Slerp(parkedRotation, reversed, Mathf.Clamp01(leave * 4f)));
                }
                else
                {
                    transform.SetPositionAndRotation(OnRoad(Vector3.Lerp(stopPoint.position, exitPoint.position, leave * leave)), parkedRotation);
                }
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

    private bool HasRoute() => routePoints.Length > 1;

    private float RouteLength() => routeDistances[routeDistances.Length - 1];

    // the skid starts this far back along the route, so the slide ends on the stop
    private float BrakeDistance() => Mathf.Max(0f, RouteLength() - skidDistance);

    private Vector3 BrakePoint()
    {
        if (HasRoute()) return RoutePosition(BrakeDistance());
        return stopPoint.position - (stopPoint.position - startPoint.position).normalized * skidDistance;
    }

    private Quaternion ArrivalRotation() => HasRoute() ? RouteRotation(RouteLength()) : startPoint.rotation;

    private int SegmentAt(float distance)
    {
        int segment = 0;
        while (segment < routePoints.Length - 2 && distance > routeDistances[segment + 1]) segment++;
        return segment;
    }

    private Vector3 RoutePosition(float distance)
    {
        distance = Mathf.Clamp(distance, 0f, RouteLength());
        int segment = SegmentAt(distance);
        float segmentLength = routeDistances[segment + 1] - routeDistances[segment];
        float t = segmentLength > 0f ? (distance - routeDistances[segment]) / segmentLength : 0f;
        return Vector3.Lerp(routePoints[segment].position, routePoints[segment + 1].position, t);
    }

    private Quaternion RouteRotation(float distance) => Quaternion.Euler(0f, RouteYaw(distance), 0f);

    // heading eases between neighbouring segments near corners, then gets a drift swing near any drift point
    private float RouteYaw(float distance)
    {
        distance = Mathf.Clamp(distance, 0f, RouteLength());
        int segment = SegmentAt(distance);
        float yaw = SegmentYaw(segment);
        float intoSegment = distance - routeDistances[segment];
        float outOfSegment = routeDistances[segment + 1] - distance;
        if (segment > 0 && intoSegment < cornerWindow)
            yaw = Mathf.LerpAngle(SegmentYaw(segment - 1), yaw, Mathf.SmoothStep(0f, 1f, intoSegment / cornerWindow));
        if (segment + 2 < routePoints.Length && outOfSegment < cornerWindow)
            yaw = Mathf.LerpAngle(SegmentYaw(segment + 1), yaw, Mathf.SmoothStep(0f, 1f, outOfSegment / cornerWindow));
        for (int i = 0; i < routeDriftDegrees.Length && i < routePoints.Length; i++)
        {
            float fromDrift = distance - routeDistances[i];
            if (routeDriftDegrees[i] != 0f && Mathf.Abs(fromDrift) < driftWindow)
                yaw += routeDriftDegrees[i] * Mathf.Cos(Mathf.PI * .5f * fromDrift / driftWindow);
        }
        return yaw;
    }

    private float SegmentYaw(int segment)
    {
        Vector3 heading = routePoints[segment + 1].position - routePoints[segment].position;
        return Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
    }

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
