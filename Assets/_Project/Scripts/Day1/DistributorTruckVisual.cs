using UnityEngine;

// Cartoon truck: wheels spin with the speed, the body squashes and slides on the skid, and shakes during the burnout.
public class DistributorTruckVisual : MonoBehaviour
{
    [SerializeField] private DistributorTruck truck;
    [SerializeField] private Transform body;
    [SerializeField] private Transform[] wheelArray;
    [SerializeField] private float wheelRadius = .42f;
    [SerializeField] private float skidYaw = 14f;
    [SerializeField] private float skidPitch = 7f;
    [SerializeField] private float shakeAmount = .06f;

    private float wheelAngle;

    private void Update()
    {
        DistributorTruck.State state = truck.GetState();
        bool isBurnout = state == DistributorTruck.State.Burnout;
        // metres per second → degrees per second for the wheel radius
        float speed = isBurnout ? 14f : truck.GetSpeedNormalized() * 12f;
        wheelAngle += speed / wheelRadius * Mathf.Rad2Deg * Time.deltaTime;
        foreach (Transform wheel in wheelArray) wheel.localRotation = Quaternion.Euler(wheelAngle, 0f, 0f);

        Vector3 localPosition = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        Vector3 scale = Vector3.one;
        switch (state)
        {
            case DistributorTruck.State.Driving:
                // nose up while accelerating, a little bounce on the road
                rotation = Quaternion.Euler(-truck.GetSpeedNormalized() * 3f, 0f, 0f);
                localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 22f)) * .04f * truck.GetSpeedNormalized();
                break;
            case DistributorTruck.State.Skidding:
                float t = truck.GetStateProgress(truck.GetSkidDuration());
                float lurch = Mathf.Sin(t * Mathf.PI);
                // nose dips, the back swings out, the body squashes then springs back
                rotation = Quaternion.Euler(skidPitch * lurch, skidYaw * lurch * (1f - t), 0f);
                scale = new Vector3(1f + .08f * lurch, 1f - .14f * lurch, 1f + .1f * lurch);
                break;
            case DistributorTruck.State.Parked:
                // settle wobble
                float wobble = Mathf.Sin(Time.time * 18f) * Mathf.Exp(-truck.GetStateProgress(1f) * 5f);
                scale = new Vector3(1f, 1f + .03f * wobble, 1f);
                break;
            case DistributorTruck.State.Burnout:
                float charge = truck.GetStateProgress(truck.GetBurnoutDuration());
                localPosition = new Vector3(Mathf.Sin(Time.time * 60f) * shakeAmount, 0f, Mathf.Sin(Time.time * 47f) * shakeAmount * .5f) * charge;
                rotation = Quaternion.Euler(-4f * charge, 0f, 0f);
                scale = new Vector3(1f + .05f * charge, 1f - .08f * charge, 1f);
                break;
            case DistributorTruck.State.Leaving:
                rotation = Quaternion.Euler(-8f, 0f, 0f);
                scale = new Vector3(.94f, 1.04f, 1.12f);
                break;
        }
        body.SetLocalPositionAndRotation(localPosition, rotation);
        body.localScale = scale;
    }
}
