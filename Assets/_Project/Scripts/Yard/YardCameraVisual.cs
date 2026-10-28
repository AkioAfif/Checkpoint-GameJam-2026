using Unity.Cinemachine;
using UnityEngine;

// The yard day's cutscene shots: the overgrown strip while the MC pictures a farm on it, the rope scene with Rigel, and the
// bunch on the porch table in the evening. Cinemachine blends to the enabled one and back to the follow camera.
public class YardCameraVisual : MonoBehaviour
{
    [SerializeField] private YardDayDirector director;
    [SerializeField] private CinemachineCamera wishCamera;
    [SerializeField] private CinemachineCamera stumpCamera;
    [SerializeField] private CinemachineCamera pulseCamera;

    private void Update()
    {
        wishCamera.enabled = director.IsWishShot();
        stumpCamera.enabled = director.IsStumpShot();
        pulseCamera.enabled = director.IsPulseShot();
    }
}
