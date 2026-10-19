using Unity.Cinemachine;
using UnityEngine;

// The truck camera films the arrival and the departure. Cinemachine blends to the newest enabled one.
public class Day1CameraVisual : MonoBehaviour
{
    [SerializeField] private Day1Director director;
    [SerializeField] private CinemachineCamera truckCamera;

    private void Start()
    {
        director.OnStateChanged += Director_OnStateChanged;
        Apply(director.GetState());
    }

    private void Director_OnStateChanged(object sender, Day1Director.OnStateChangedEventArgs e) => Apply(e.state);

    private void Apply(Day1Director.State state)
    {
        truckCamera.enabled = state == Day1Director.State.Arrival || state == Day1Director.State.Departure;
    }
}
