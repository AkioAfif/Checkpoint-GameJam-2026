using Unity.Cinemachine;
using UnityEngine;

// Turns the zoom cameras on for their game state; the brain blends to the most recently enabled camera.
public class CameraDirector : MonoBehaviour
{
    [SerializeField] private CinemachineCamera plantingCamera;
    [SerializeField] private CinemachineCamera fabricatorCamera;
    [SerializeField] private CinemachineCamera houseEntryCamera;

    private void Start()
    {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        Apply(GameManager.Instance.GetState());
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= GameManager_OnStateChanged;
    }

    // read the live state: a listener may already have switched it again inside this same event
    private void GameManager_OnStateChanged(object sender, GameManager.OnStateChangedEventArgs e) => Apply(GameManager.Instance.GetState());

    private void Apply(GameManager.State state)
    {
        plantingCamera.enabled = state == GameManager.State.Planting;
        fabricatorCamera.enabled = state == GameManager.State.Fabricating;
        houseEntryCamera.enabled = state == GameManager.State.EnteringHouse;
    }
}
