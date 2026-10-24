using UnityEngine;

// The pickup's engine: a looping clip that revs with the speed. Silent until a clip is assigned.
[RequireComponent(typeof(AudioSource))]
public class DistributorTruckSound : MonoBehaviour
{
    [SerializeField] private DistributorTruck truck;
    [SerializeField] private float minPitch = .8f;
    [SerializeField] private float maxPitch = 1.6f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        DistributorTruck.State state = truck.GetState();
        bool isRunning = state == DistributorTruck.State.Driving || state == DistributorTruck.State.Burnout || state == DistributorTruck.State.Leaving;
        if (audioSource.clip == null) return;
        if (isRunning && !audioSource.isPlaying) audioSource.Play();
        else if (!isRunning && audioSource.isPlaying) audioSource.Stop();
        // the burnout revs flat out while standing still
        if (isRunning) audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, state == DistributorTruck.State.Burnout ? 1f : truck.GetSpeedNormalized());
    }
}
