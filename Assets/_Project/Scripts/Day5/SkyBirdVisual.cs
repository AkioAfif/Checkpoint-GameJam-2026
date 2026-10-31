using UnityEngine;

// A sky-run bird's wings beating.
public class SkyBirdVisual : MonoBehaviour
{
    [SerializeField] private Transform leftWing;
    [SerializeField] private Transform rightWing;
    [SerializeField] private float flapAngle = 25f;
    [SerializeField] private float flapSpeed = 12f;

    private void Update()
    {
        float angle = Mathf.Sin(Time.time * flapSpeed) * flapAngle;
        leftWing.localRotation = Quaternion.Euler(0f, 0f, angle);
        rightWing.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }
}
