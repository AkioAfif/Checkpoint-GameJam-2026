using System;
using UnityEngine;

// Drives the Distributor's truck along its route with real Rigidbody physics: four raycast wheels (spring suspension,
// tyre grip that saturates and slides), rear-wheel drive, pure-pursuit steering and a speed plan that brakes into the stop.
// Drift zones loosen the rear tyres; a yaw assist holds the slide angle and a line assist keeps the slide on the road,
// so the drift reads like a stunt driver's while momentum and sideways sliding stay physical.
// Idle it is kinematic, so DistributorTruck can still place it by hand (settling into the parked pose, the departure).
[RequireComponent(typeof(Rigidbody))]
public class TruckDriver : MonoBehaviour
{
    public event EventHandler OnArrived;

    private enum DriftMode { None, Light, Hard }

    [Header("Wheels (local, at rest: FL, FR, RL, RR)")]
    [SerializeField] private Vector3[] wheelOffsetArray = { new Vector3(1.08f, .46f, 1.45f), new Vector3(-1.08f, .46f, 1.45f), new Vector3(1.08f, .46f, -1.55f), new Vector3(-1.08f, .46f, -1.55f) };
    [SerializeField] private float wheelRadius = .46f;
    [SerializeField] private float suspensionTravel = .3f;
    [Tooltip("How far the springs compress under the truck's own weight.")]
    [SerializeField] private float suspensionSag = .08f;
    [SerializeField] private float suspensionDamping = .55f;
    [SerializeField] private Vector3 centerOfMass = new Vector3(0f, .3f, -.05f);
    [Header("Tyres (friction coefficients)")]
    [SerializeField] private float frontGrip = 1.3f;
    [SerializeField] private float rearGrip = 1.2f;
    [SerializeField] private float lightDriftRearGrip = .3f;
    [SerializeField] private float hardDriftRearGrip = .18f;
    [Tooltip("Seconds the rear tyres take to bite again after a drift.")]
    [SerializeField] private float gripRecoverTime = .6f;
    [Header("Driving")]
    [SerializeField] private float launchSpeed = 8f;
    [SerializeField] private float cruiseSpeed = 15f;
    [Tooltip("Speed through an ordinary sharp (90°) corner that isn't a drift zone.")]
    [SerializeField] private float cornerSpeed = 6f;
    [SerializeField] private float maxAcceleration = 6f;
    [Tooltip("Planned braking into the stop; gentle enough that it stays on its line round the last corner.")]
    [SerializeField] private float brakeDeceleration = 4.5f;
    [SerializeField] private float maxSteerAngle = 38f;
    [SerializeField] private float steerSpeed = 160f;
    [SerializeField] private float wheelbase = 3f;
    [Header("Drift assist")]
    [SerializeField] private float yawStiffness = 40f;
    [SerializeField] private float yawDamping = 5f;
    [Tooltip("Yaw speed (rad/s) thrown in when a hard drift starts.")]
    [SerializeField] private float hardDriftKick = 1.4f;
    [SerializeField] private float lineAssist = 1.5f;
    [SerializeField] private float driftLineAssist = 4f;
    [Header("Arrival")]
    [Tooltip("The furthest from the stop that the settle may take over.")]
    [SerializeField] private float maxSettleDistance = 6f;
    [Tooltip("Safety net: whatever happens, the truck counts as arrived after this many seconds.")]
    [SerializeField] private float maxDriveTime = 30f;

    private Rigidbody body;
    private TruckPath path;
    private readonly RaycastHit[] hitBuffer = new RaycastHit[8];
    private bool isDriving;
    private float progress;
    private float driveTimer;
    private float stuckTimer;
    private float settleTime;
    private float steerAngle;
    private float throttle;
    private float brake;
    private float rearGripNow;
    private DriftMode driftMode;
    private float zoneSpeed = float.MaxValue;
    private float driftSlip;
    private float driftSide = 1f;
    private Vector3 hardExitHeading;
    private float hardTimer;
    private bool hasHardDrifted;
    private float slipAngle;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        SetPhysicsActive(false);
    }

    private void FixedUpdate()
    {
        if (!isDriving) return;
        float dt = Time.fixedDeltaTime;
        driveTimer += dt;
        Vector3 velocity = body.linearVelocity;
        Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float forwardSpeed = Vector3.Dot(velocity, transform.forward);
        slipAngle = flatVelocity.sqrMagnitude > 1f ? Vector3.SignedAngle(flatVelocity, Flat(transform.forward), Vector3.up) : 0f;
        progress = path.GetClosestDistance(body.position, progress);
        float distanceLeft = path.GetLength() - progress;

        UpdateDrift(flatVelocity, dt);
        Vector3 target = path.GetPositionAt(progress + 4f + flatVelocity.magnitude * .45f);
        Steer(target, dt);
        PlanSpeed(forwardSpeed, distanceLeft);
        for (int i = 0; i < wheelOffsetArray.Length; i++) ApplyWheel(i, dt);
        Assist(flatVelocity, target);

        stuckTimer = flatVelocity.magnitude < .3f ? stuckTimer + dt : 0f;
        // the settle eases out from 2 × distance / settleTime: let go where that equals the current speed, so it glides straight on
        float handoverDistance = Mathf.Max(flatVelocity.magnitude * settleTime * .5f, .3f);
        bool hasArrived = distanceLeft <= handoverDistance && distanceLeft < maxSettleDistance;
        if (hasArrived || stuckTimer > 2f || driveTimer > maxDriveTime) Arrive();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isDriving || !other.TryGetComponent(out TruckDriftZone zone)) return;
        driftSlip = zone.GetSlipAngle();
        if (zone.GetKind() == TruckDriftZone.Kind.Hard)
        {
            // handbrake turn: commit to the corner ahead and kick the tail out
            driftMode = DriftMode.Hard;
            hardTimer = 0f;
            hardExitHeading = path.GetHeadingAt(progress + 22f);
            float side = TurnSide(path.GetHeadingAt(progress), hardExitHeading);
            if (side != 0f) driftSide = side;
            body.AddTorque(Vector3.up * driftSide * hardDriftKick, ForceMode.VelocityChange);
        }
        else if (driftMode == DriftMode.None)
        {
            driftMode = DriftMode.Light;
        }
        zoneSpeed = zone.GetEntrySpeed();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out TruckDriftZone zone) || zone.GetKind() != TruckDriftZone.Kind.Light) return;
        if (driftMode == DriftMode.Light) driftMode = DriftMode.None;
        zoneSpeed = float.MaxValue;
    }

    // settleTime: how long the caller then takes to ease the truck into its parked pose
    public void Drive(Transform[] routePointArray, float settleTime)
    {
        this.settleTime = settleTime;
        path = new TruckPath(routePointArray);
        Vector3 heading = path.GetHeadingAt(0f);
        Quaternion rotation = Quaternion.LookRotation(heading, Vector3.up);
        transform.SetPositionAndRotation(path.GetPositionAt(0f), rotation);
        SetPhysicsActive(true);
        body.position = transform.position;
        body.rotation = rotation;
        body.linearVelocity = heading * launchSpeed;
        body.angularVelocity = Vector3.zero;
        progress = 0f;
        driveTimer = 0f;
        stuckTimer = 0f;
        steerAngle = 0f;
        rearGripNow = rearGrip;
        driftMode = DriftMode.None;
        zoneSpeed = float.MaxValue;
        hasHardDrifted = false;
        isDriving = true;
        SetCharactersIgnored(true);
    }

    public bool IsDriving() => isDriving;
    // the road this far ahead of the truck (the chase camera rides it)
    public Vector3 GetPointAhead(float distance) => path.GetPositionAt(progress + distance);
    public bool IsDrifting() => driftMode != DriftMode.None;
    // the handbrake corner is behind it: all that is left is pulling up
    public bool HasHardDrifted() => hasHardDrifted;
    // degrees the body points away from where it is travelling (sign = which way)
    public float GetSlipAngle() => slipAngle;

    private void UpdateDrift(Vector3 flatVelocity, float dt)
    {
        if (driftMode == DriftMode.Hard)
        {
            hardTimer += dt;
            // the driver lets go once it is travelling down the new road; the tyres bite back and straighten it
            bool isLinedUp = Vector3.Angle(flatVelocity, hardExitHeading) < 15f;
            if (isLinedUp || hardTimer > 3f)
            {
                driftMode = DriftMode.None;
                hasHardDrifted = true;
                zoneSpeed = float.MaxValue;
            }
        }
        else if (driftMode == DriftMode.Light)
        {
            float side = TurnSide(path.GetHeadingAt(progress), path.GetHeadingAt(progress + 15f));
            if (side != 0f) driftSide = side;
        }
        float targetGrip = driftMode == DriftMode.Hard ? hardDriftRearGrip : driftMode == DriftMode.Light ? lightDriftRearGrip : rearGrip;
        // grip lets go at once, then bites back gradually
        rearGripNow = targetGrip < rearGripNow ? targetGrip : Mathf.MoveTowards(rearGripNow, targetGrip, (rearGrip - hardDriftRearGrip) / gripRecoverTime * dt);
    }

    private void Steer(Vector3 target, float dt)
    {
        Vector3 toTarget = Flat(target - body.position);
        float alpha = Vector3.SignedAngle(Flat(transform.forward), toTarget, Vector3.up);
        // pure pursuit; while sliding the front wheels simply point down the road, which is the counter-steer
        float desired = driftMode == DriftMode.None
            ? Mathf.Atan2(2f * wheelbase * Mathf.Sin(alpha * Mathf.Deg2Rad), Mathf.Max(toTarget.magnitude, .1f)) * Mathf.Rad2Deg
            : alpha;
        steerAngle = Mathf.MoveTowards(steerAngle, Mathf.Clamp(desired, -maxSteerAngle, maxSteerAngle), steerSpeed * dt);
    }

    private void PlanSpeed(float forwardSpeed, float distanceLeft)
    {
        float turn = Vector3.Angle(path.GetHeadingAt(progress), path.GetHeadingAt(progress + 12f));
        float targetSpeed = Mathf.Lerp(cruiseSpeed, cornerSpeed, Mathf.Clamp01(turn / 90f));
        // a drift zone sets its own pace: the hard drift keeps its momentum through the corner
        if (zoneSpeed < float.MaxValue) targetSpeed = zoneSpeed;
        targetSpeed = Mathf.Min(targetSpeed, Mathf.Sqrt(2f * brakeDeceleration * Mathf.Max(0f, distanceLeft - .5f)));
        float error = targetSpeed - forwardSpeed;
        throttle = Mathf.Clamp01(error * .6f);
        brake = Mathf.Clamp01(-error * .6f);
    }

    private void ApplyWheel(int index, float dt)
    {
        bool isFront = index < 2;
        Vector3 up = transform.up;
        Vector3 origin = transform.TransformPoint(wheelOffsetArray[index]) + up * suspensionTravel;
        if (!TryGetGround(origin, -up, suspensionTravel * 2f + wheelRadius, out RaycastHit hit)) return;

        // spring + damper, tuned from the mass so the truck sags suspensionSag at rest
        float springStrength = body.mass * -Physics.gravity.y / (wheelOffsetArray.Length * suspensionSag);
        float damperStrength = 2f * suspensionDamping * Mathf.Sqrt(springStrength * body.mass / wheelOffsetArray.Length);
        float compression = suspensionTravel + wheelRadius + suspensionSag - hit.distance;
        Vector3 pointVelocity = body.GetPointVelocity(hit.point);
        float load = Mathf.Max(0f, springStrength * compression - damperStrength * Vector3.Dot(pointVelocity, up));
        body.AddForceAtPosition(up * load, hit.point);

        Quaternion steer = isFront ? Quaternion.AngleAxis(steerAngle, up) : Quaternion.identity;
        Vector3 wheelForward = Vector3.ProjectOnPlane(steer * transform.forward, hit.normal).normalized;
        Vector3 wheelRight = Vector3.Cross(hit.normal, wheelForward);
        float wheelMass = body.mass / wheelOffsetArray.Length;

        // the tyre tries to kill all sideways motion; past its grip limit it slides
        Vector3 lateral = -wheelRight * Vector3.Dot(pointVelocity, wheelRight) * wheelMass / dt * .5f;
        float forwardVelocity = Vector3.Dot(pointVelocity, wheelForward);
        float drive = isFront ? 0f : throttle * body.mass * maxAcceleration * .5f;
        float braking = Mathf.Min(brake * body.mass * brakeDeceleration / wheelOffsetArray.Length, Mathf.Abs(forwardVelocity) * wheelMass / dt);
        Vector3 longitudinal = wheelForward * (drive - Mathf.Sign(forwardVelocity) * braking);
        float grip = (isFront ? frontGrip : rearGripNow) * load;
        body.AddForceAtPosition(Vector3.ClampMagnitude(lateral + longitudinal, grip), hit.point);
    }

    private void Assist(Vector3 flatVelocity, Vector3 target)
    {
        float speed = flatVelocity.magnitude;
        if (speed < 1f) return;
        // keep the line: bend the direction of travel toward the road without adding or taking speed
        Vector3 wanted = Flat(target - body.position).normalized * speed;
        Vector3 correction = Vector3.ProjectOnPlane(wanted - flatVelocity, flatVelocity.normalized);
        body.AddForce(correction * (driftMode == DriftMode.None ? lineAssist : driftLineAssist), ForceMode.Acceleration);

        // after a drift, while the rear tyres bite back, catch the slide (aim for zero slip) instead of spinning out
        bool isRecovering = driftMode == DriftMode.None && rearGripNow < rearGrip;
        if (driftMode == DriftMode.None && !isRecovering) return;
        // hold the slide: the nose points driftSlip degrees into the turn from the direction of travel
        float targetSlip = isRecovering ? 0f : driftSide * driftSlip;
        float error = (targetSlip - slipAngle) * Mathf.Deg2Rad;
        float yawAcceleration = error * yawStiffness - body.angularVelocity.y * yawDamping;
        body.AddTorque(Vector3.up * yawAcceleration, ForceMode.Acceleration);
    }

    private bool TryGetGround(Vector3 origin, Vector3 direction, float length, out RaycastHit ground)
    {
        int count = Physics.RaycastNonAlloc(origin, direction, hitBuffer, length, ~0, QueryTriggerInteraction.Ignore);
        ground = default;
        float nearest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (hitBuffer[i].collider.attachedRigidbody == body || hitBuffer[i].distance >= nearest) continue;
            nearest = hitBuffer[i].distance;
            ground = hitBuffer[i];
        }
        return nearest < float.MaxValue;
    }

    // a cutscene, not a hit-and-run: while driving in it passes through the player and the villagers; parked, it is solid again
    private void SetCharactersIgnored(bool isIgnored)
    {
        Npc[] npcArray = FindObjectsByType<Npc>(FindObjectsInactive.Include);
        foreach (Collider truckCollider in GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(truckCollider, Player.Instance.GetComponent<CharacterController>(), isIgnored);
            foreach (Npc npc in npcArray)
                foreach (Collider npcCollider in npc.GetComponentsInChildren<Collider>(true))
                    Physics.IgnoreCollision(truckCollider, npcCollider, isIgnored);
        }
    }

    private void Arrive()
    {
        isDriving = false;
        driftMode = DriftMode.None;
        SetPhysicsActive(false);
        SetCharactersIgnored(false);
        OnArrived?.Invoke(this, EventArgs.Empty);
    }

    // kinematic while idle; interpolation only while simulated, or it would fight hand placement
    private void SetPhysicsActive(bool isActive)
    {
        if (!isActive && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.isKinematic = !isActive;
        body.interpolation = isActive ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        body.centerOfMass = centerOfMass;
    }

    // +1 when the road ahead turns clockwise (right), -1 left, 0 when straight
    private static float TurnSide(Vector3 from, Vector3 to)
    {
        float angle = Vector3.SignedAngle(from, to, Vector3.up);
        return Mathf.Abs(angle) < 5f ? 0f : Mathf.Sign(angle);
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
