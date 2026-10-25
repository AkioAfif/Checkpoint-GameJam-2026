using Unity.Cinemachine;
using UnityEngine;

// The truck chase: CM_Truck rides the road a few metres ahead of the pickup, up and to one side, looking back down at its
// nose (its RotationComposer aims at TruckCameraTarget), and rolls into the turns and drifts. Riding the road keeps it in
// the open and round each corner before the truck; the Deoccluder/Decollider on the same camera keep it out of buildings.
[RequireComponent(typeof(CinemachineCamera))]
public class TruckCameraVisual : MonoBehaviour
{
    // DistributorTruck.GetSpeedNormalized() is 1 at this many m/s
    private const float TRUCK_FULL_SPEED = 12f;

    [SerializeField] private DistributorTruck truck;
    [SerializeField] private float leadDistance = 9f;
    [SerializeField] private float height = 2.6f;
    [SerializeField] private float sideOffset = 1.5f;
    [SerializeField] private float smoothTime = .3f;
    [Tooltip("Further than this from where it should be, the camera cuts there instead of gliding.")]
    [SerializeField] private float snapDistance = 25f;
    [SerializeField] private float maxDutch = 10f;
    [Tooltip("Degrees of roll per degree-per-second the truck turns.")]
    [SerializeField] private float dutchPerYawRate = .08f;
    [SerializeField] private float dutchSmoothTime = .35f;
    [Tooltip("Where the camera glides to once the truck is out of its hard drift and pulling up. Left empty, it holds wherever it was.")]
    [SerializeField] private Transform parkedShotPoint;
    [SerializeField] private float parkedShotSmoothTime = 1.2f;

    private CinemachineCamera cinemachineCamera;
    private bool wasActive;
    private Vector3 velocity;
    private float dutch;
    private float dutchVelocity;
    private float lastTruckYaw;

    private void Awake()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    private void Update()
    {
        float truckYaw = truck.transform.eulerAngles.y;
        float yawRate = Time.deltaTime > 0f ? Mathf.DeltaAngle(lastTruckYaw, truckYaw) / Time.deltaTime : 0f;
        lastTruckYaw = truckYaw;
        if (!cinemachineCamera.enabled)
        {
            wasActive = false;
            return;
        }
        // run further ahead the faster it goes, so the smoothing lag never lets the truck catch the camera (the departure is quick)
        float lead = leadDistance + truck.GetSpeedNormalized() * TRUCK_FULL_SPEED * smoothTime;
        // out of the hard drift and pulling up, or settling into its spot: glide to the parked shot (or hold) and level the picture
        float wantedDutch = 0f;
        if (truck.IsPastHardDrift() || !truck.TryGetRoadPointAhead(lead, out Vector3 roadAhead))
        {
            if (parkedShotPoint != null) transform.position = Vector3.SmoothDamp(transform.position, parkedShotPoint.position, ref velocity, parkedShotSmoothTime);
        }
        else
        {
            Vector3 travel = roadAhead - truck.transform.position;
            travel.y = 0f;
            if (travel.sqrMagnitude < .01f) travel = truck.transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, travel).normalized;
            Vector3 wanted = roadAhead + Vector3.up * height + side * sideOffset;
            if (!wasActive || Vector3.Distance(transform.position, wanted) > snapDistance)
            {
                transform.position = wanted;
                velocity = Vector3.zero;
                dutch = 0f;
                dutchVelocity = 0f;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, wanted, ref velocity, smoothTime);
            }
            // lean into the turn: the faster it swings round (a drift most of all), the harder the picture rolls
            wantedDutch = Mathf.Clamp(yawRate * dutchPerYawRate, -maxDutch, maxDutch);
        }
        wasActive = true;

        dutch = Mathf.SmoothDamp(dutch, wantedDutch, ref dutchVelocity, dutchSmoothTime);
        LensSettings lens = cinemachineCamera.Lens;
        lens.Dutch = dutch;
        cinemachineCamera.Lens = lens;
    }
}
