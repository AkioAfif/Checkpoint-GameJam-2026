using Unity.Cinemachine;
using UnityEngine;

// The yard day's cutscene shots: the overgrown strip while the MC pictures a farm on it and the rope scene with Rigel.
// Cinemachine blends to the enabled one and back to the follow camera.
public class YardCameraVisual : MonoBehaviour
{
    [SerializeField] private YardDayDirector director;
    [SerializeField] private CinemachineCamera wishCamera;
    [SerializeField] private CinemachineCamera stumpCamera;

    private void Update()
    {
        wishCamera.enabled = director.IsWishShot();
        stumpCamera.enabled = director.IsStumpShot();
    }
}
