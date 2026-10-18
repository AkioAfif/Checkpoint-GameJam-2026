using System;
using UnityEngine;

// The Fabricator's trip from the pickup truck's bed to its spot in front of the house.
public class FabricatorDelivery : MonoBehaviour
{
    public event EventHandler OnDelivered;

    [SerializeField] private Transform placePoint;
    [SerializeField] private float duration = 1.6f;
    [SerializeField] private float liftHeight = 1.8f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float timer;
    private bool isMoving;

    private void Update()
    {
        if (!isMoving) return;
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        transform.position = Vector3.Lerp(startPosition, placePoint.position, eased) + Vector3.up * Mathf.Sin(t * Mathf.PI) * liftHeight;
        transform.rotation = Quaternion.Slerp(startRotation, placePoint.rotation, eased);
        if (t < 1f) return;
        isMoving = false;
        OnDelivered?.Invoke(this, EventArgs.Empty);
    }

    // rides on the truck bed until Deliver()
    public void AttachTo(Transform cargoAnchor)
    {
        transform.SetParent(cargoAnchor, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Deliver()
    {
        transform.SetParent(null, true);
        startPosition = transform.position;
        startRotation = transform.rotation;
        timer = 0f;
        isMoving = true;
    }
}
