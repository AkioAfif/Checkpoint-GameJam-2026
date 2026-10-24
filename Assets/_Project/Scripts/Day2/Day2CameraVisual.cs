using Unity.Cinemachine;
using UnityEngine;

// The evening camera looks over the field from the house side while the director runs the night cutscene.
// Cinemachine blends to it and back to the follow camera.
public class Day2CameraVisual : MonoBehaviour
{
    [SerializeField] private Day2Director director;
    [SerializeField] private CinemachineCamera farmCamera;

    private void Update()
    {
        farmCamera.enabled = director.IsFarmShot();
    }
}
