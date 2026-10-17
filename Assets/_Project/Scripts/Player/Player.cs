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
    private float verticalVelocity;
    private float currentSpeed;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one Player", this);
        Instance = this;
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Vector3 moveDir = Vector3.zero;
        if (GameManager.Instance.IsGamePlaying())
        {
            Vector2 input = GameInput.Instance.GetMovementVectorNormalized();
            moveDir = new Vector3(input.x, 0f, input.y);
        }

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
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
