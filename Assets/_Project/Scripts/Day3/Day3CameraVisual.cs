using Unity.Cinemachine;
using UnityEngine;

// The evening camera looks over the new strip into the forest while the director runs the night's last beat.
// Cinemachine blends to it and back to the follow camera.
public class Day3CameraVisual : MonoBehaviour
{
    [SerializeField] private Day3Director director;
    [SerializeField] private CinemachineCamera forestCamera;

    private void Update()
    {
        forestCamera.enabled = director.IsForestShot();
    }
}
