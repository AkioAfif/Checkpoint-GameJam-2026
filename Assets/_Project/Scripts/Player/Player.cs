using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float turnSpeed = 14f;
    [SerializeField] private float gravity = -20f;

    private CharacterController characterController;
    private Transform cameraTransform;
    private float verticalVelocity;
    private float currentSpeed;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one Player", this);
        Instance = this;
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        // a cutscene hop moves the transform itself
        if (!characterController.enabled) return;
        Vector3 moveDir = Vector3.zero;
        if (GameManager.Instance.IsGamePlaying()) moveDir = CameraRelative(GameInput.Instance.GetMovementVectorNormalized());

        currentSpeed = moveDir == Vector3.zero ? 0f : GameInput.Instance.IsSprintPressed() ? sprintSpeed : walkSpeed;
        verticalVelocity = characterController.isGrounded ? -1f : verticalVelocity + gravity * Time.deltaTime;
        characterController.Move((moveDir * currentSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (moveDir != Vector3.zero) transform.forward = Vector3.Slerp(transform.forward, moveDir, turnSpeed * Time.deltaTime);
    }

    // 0 idle, 1 walking, ~1.5 sprinting
    public float GetSpeedNormalized() => currentSpeed / walkSpeed;
    public bool IsMoving() => currentSpeed > 0f;

    public void FaceTowards(Vector3 worldPosition)
    {
        Vector3 dir = worldPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > .0001f) transform.forward = dir.normalized;
    }

    public void Teleport(Vector3 position)
    {
        characterController.enabled = false; // otherwise the controller overwrites the move next frame
        transform.position = position;
        characterController.enabled = true;
        // no fall speed carried over from where they were
        verticalVelocity = 0f;
        currentSpeed = 0f;
    }

    // a cutscene flight: no controller, no gravity, whoever flies the player sets its position and rotation; Teleport ends it
    public void BeginFlight() => characterController.enabled = false;

    // "Unstuck me": back to the front of the house. False (and the player stays put) when this scene has no return point.
    public bool TryReturnHome()
    {
        if (PlayerHomeReturnPoint.Instance == null)
        {
            Debug.LogWarning("Unstuck me: this scene has no PlayerHomeReturnPoint, the player stays put", this);
            return false;
        }
        Transform returnPoint = PlayerHomeReturnPoint.Instance.transform;
        Teleport(returnPoint.position);
        FaceTowards(returnPoint.position + returnPoint.forward);
        return true;
    }

    // W = away from the camera on the ground plane; a camera looking straight down uses its screen-up instead
    private Vector3 CameraRelative(Vector2 input)
    {
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < .01f)
        {
            forward = cameraTransform.up;
            forward.y = 0f;
        }
        forward.Normalize();
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        return right * input.x + forward * input.y;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
