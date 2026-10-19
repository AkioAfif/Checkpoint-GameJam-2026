using Unity.Cinemachine;
using UnityEngine;

// The truck camera chases the pickup in and out; the meeting camera frames the intro talk once it is parked,
// side-on to the player and the Distributor so neither hides the other; the delivery camera follows him while he carries
// the Fabricator over. Cinemachine blends to the newest enabled one.
public class Day1CameraVisual : MonoBehaviour
{
    [SerializeField] private Day1Director director;
    [SerializeField] private DistributorTruck truck;
    [SerializeField] private CinemachineCamera truckCamera;
    [Tooltip("What CM_Truck follows: the truck, but held at a minimum x so the shot waits on the bridge for the arrival and stops by the bridge on the way out.")]
    [SerializeField] private Transform truckCameraTarget;
    [SerializeField] private float arrivalMinTruckCameraX = -20f;
    // follows the truck out until the bridge is in view; the truck rides over it on the deck
    [SerializeField] private float departureMinTruckCameraX = -14f;
    [SerializeField] private CinemachineCamera meetingCamera;
    [SerializeField] private CinemachineFollow meetingFollow;
    [SerializeField] private Transform distributor;
    [SerializeField] private float meetingDistance = 6.5f;
    [SerializeField] private float meetingHeight = 2.8f;
    [SerializeField] private CinemachineCamera deliveryCamera;

    private void Start()
    {
        director.OnStateChanged += Director_OnStateChanged;
        truck.OnStateChanged += Truck_OnStateChanged;
        Apply();
    }

    private void Update()
    {
        Vector3 truckPosition = truck.transform.position;
        float minX = director.GetState() == Day1Director.State.Departure ? departureMinTruckCameraX : arrivalMinTruckCameraX;
        truckCameraTarget.position = new Vector3(Mathf.Max(truckPosition.x, minX), 0f, truckPosition.z);
        deliveryCamera.enabled = director.IsDelivering();

        if (!meetingCamera.enabled) return;
        Vector3 pair = distributor.position - Player.Instance.transform.position;
        pair.y = 0f;
        if (pair.sqrMagnitude < .01f) return;
        Vector3 side = Vector3.Cross(Vector3.up, pair).normalized;
        // of the two sides, film from the south-east: away from the house and from the truck parked to the west
        if (Vector3.Dot(side, new Vector3(1f, 0f, -1f)) < 0f) side = -side;
        meetingFollow.FollowOffset = side * meetingDistance + Vector3.up * meetingHeight;
    }

    private void Director_OnStateChanged(object sender, Day1Director.OnStateChangedEventArgs e) => Apply();
    private void Truck_OnStateChanged(object sender, DistributorTruck.OnStateChangedEventArgs e) => Apply();

    private void Apply()
    {
        Day1Director.State state = director.GetState();
        bool isParked = truck.GetState() == DistributorTruck.State.Parked;
        truckCamera.enabled = (state == Day1Director.State.Arrival && !isParked) || state == Day1Director.State.Departure;
        meetingCamera.enabled = state == Day1Director.State.Arrival && isParked;
    }
}
