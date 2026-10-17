using UnityEngine;

// Films the puzzle diorama while it is open and sways a little with the pointer so it reads as a 3D tube.
[RequireComponent(typeof(Camera))]
public class FabricatorPuzzleCamera : MonoBehaviour
{
    [SerializeField] private FabricatorController fabricator;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform pivot;
    [SerializeField] private float maxYaw = 18f;
    [SerializeField] private float maxPitch = 7f;
    [SerializeField] private float swaySpeed = 3f;

    private Camera puzzleCamera;
    private float yaw;
    private float pitch;

    private void Awake()
    {
        puzzleCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        fabricator.OnStateChanged += Fabricator_OnStateChanged;
        SetActive(false);
    }

    private void LateUpdate()
    {
        if (!puzzleCamera.enabled) return;
        Vector2 pointer = GameInput.Instance.GetPointerScreenPosition();
        Vector2 offset = new Vector2(pointer.x / Screen.width - .5f, pointer.y / Screen.height - .5f) * 2f;
        offset = Vector2.ClampMagnitude(offset, 1f);
        yaw = Mathf.Lerp(yaw, -offset.x * maxYaw, swaySpeed * Time.deltaTime);
        pitch = Mathf.Lerp(pitch, offset.y * maxPitch, swaySpeed * Time.deltaTime);
        pivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void Fabricator_OnStateChanged(object sender, FabricatorController.OnStateChangedEventArgs e)
    {
        SetActive(e.state == FabricatorController.State.Puzzle || e.state == FabricatorController.State.Solved);
    }

    private void SetActive(bool isActive)
    {
        puzzleCamera.enabled = isActive;
        worldCamera.enabled = !isActive; // no need to render the village behind a full-screen puzzle
    }
}
