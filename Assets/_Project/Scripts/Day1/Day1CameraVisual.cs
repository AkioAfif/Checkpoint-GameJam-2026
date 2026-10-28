using Unity.Cinemachine;
using UnityEngine;

// The truck camera chases the pickup in and out; the meeting camera frames the intro talk once it is parked,
// side-on to the player and the Distributor so neither hides the other; the delivery camera (if set; otherwise the meeting
// camera carries on) follows him while he carries the Fabricator over; the evening camera looks down on the house front and pans over to the grown field.
// Cinemachine blends to the newest enabled one.
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
    [Tooltip("Once they stand further apart than meetingDistance, the camera keeps this much more distance than the gap between them.")]
    [SerializeField] private float meetingPadding = 3f;
    [Tooltip("The side the meeting shot opens from (flattened). Put it on the gameplay camera's side so the hand-back to it is a short blend, not a swing round the player.")]
    [SerializeField] private Vector3 meetingOpeningSide = new Vector3(1f, 0f, -1f);
    [Tooltip("The meeting camera's target group (player + Distributor). Without a delivery camera, the player's weight eases to 0 while the Distributor fetches and sets down the Fabricator, so the shot focuses on him.")]
    [SerializeField] private CinemachineTargetGroup meetingGroup;
    [Tooltip("Weight per second the player's share of the meeting focus changes by.")]
    [SerializeField] private float meetingFocusSpeed = 1.5f;
    [Tooltip("Seconds the meeting camera takes to ease into a new side or distance, so it never snaps or pumps.")]
    [SerializeField] private float meetingOffsetSmoothTime = .8f;
    [Tooltip("Optional: left empty, the meeting camera keeps filming while he fetches, carries and sets down the Fabricator.")]
    [SerializeField] private CinemachineCamera deliveryCamera;
    [SerializeField] private CinemachineCamera eveningCamera;
    [Tooltip("What CM_Evening follows: eased from the house front to the field when the director asks for the farm shot.")]
    [SerializeField] private Transform eveningCameraTarget;
    [SerializeField] private Transform houseShotPoint;
    [SerializeField] private Transform farmShotPoint;
    [SerializeField] private float panSmoothTime = .9f;

    private Vector3 panVelocity;
    // which side of the player–Distributor line the meeting camera films from; zero until the shot starts
    private Vector3 meetingSide;
    private Vector3 meetingOffset;
    private Vector3 meetingOffsetVelocity;

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
        if (deliveryCamera != null) deliveryCamera.enabled = director.IsDelivering();
        eveningCamera.enabled = director.IsEveningCutscene();
        if (director.IsEveningFollowShot())
        {
            // the gameplay camera films the night walk and the "What?!"; the evening shot waits on the player (same offset), so
            // it takes over without a jump and then pans to the garden
            eveningCameraTarget.position = Player.Instance.transform.position;
            panVelocity = Vector3.zero;
        }
        else
        {
            Vector3 farmShot = director.TryGetGardenFocus(out Vector3 gardenFocus) ? gardenFocus : farmShotPoint.position;
            Vector3 shot = director.IsFarmShot() ? farmShot : houseShotPoint.position;
            eveningCameraTarget.position = Vector3.SmoothDamp(eveningCameraTarget.position, shot, ref panVelocity, panSmoothTime);
        }

        if (meetingGroup != null)
        {
            int playerIndex = meetingGroup.FindMember(Player.Instance.transform);
            float playerWeight = deliveryCamera == null && director.IsDelivering() ? 0f : 1f;
            meetingGroup.Targets[playerIndex].Weight = Mathf.MoveTowards(meetingGroup.Targets[playerIndex].Weight, playerWeight, meetingFocusSpeed * Time.deltaTime);
        }

        if (!meetingCamera.enabled)
        {
            meetingSide = Vector3.zero;
            return;
        }

        bool isShotStarting = meetingSide == Vector3.zero;
        // while he fetches and carries the Fabricator the shot is already on him alone (the player's weight is 0):
        // hold the framing and just follow him, instead of re-aiming and pulling back as he walks away from the player
        bool isFollowingDelivery = deliveryCamera == null && director.IsDelivering() && !isShotStarting;
        if (!isFollowingDelivery)
        {
            Vector3 pair = distributor.position - Player.Instance.transform.position;
            pair.y = 0f;
            if (pair.sqrMagnitude < .01f) return;
            Vector3 side = Vector3.Cross(Vector3.up, pair).normalized;
            // the shot opens from meetingOpeningSide (Level01: the south-east, away from the house and the parked truck), then
            // stays on that side as he walks round the player: switching sides would jump-cut across the line
            Vector3 keepSide = isShotStarting ? meetingOpeningSide : meetingSide;
            if (Vector3.Dot(side, keepSide) < 0f) side = -side;
            meetingSide = side;
            // back off as they move apart so both stay in frame
            float distance = Mathf.Max(meetingDistance, pair.magnitude + meetingPadding);
            meetingOffset = side * distance + Vector3.up * meetingHeight;
        }
        if (isShotStarting)
        {
            meetingFollow.FollowOffset = meetingOffset;
            meetingOffsetVelocity = Vector3.zero;
            return;
        }
        meetingFollow.FollowOffset = Vector3.SmoothDamp(meetingFollow.FollowOffset, meetingOffset, ref meetingOffsetVelocity, meetingOffsetSmoothTime);
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
