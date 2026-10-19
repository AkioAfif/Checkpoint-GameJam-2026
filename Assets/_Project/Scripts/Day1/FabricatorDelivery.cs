using System;
using UnityEngine;

// The Fabricator's trip from the pickup truck's bed, in the Distributor's arms, to its spot in front of the house.
public class FabricatorDelivery : MonoBehaviour
{
    public event EventHandler OnPickedUp;
    public event EventHandler OnDelivered;

    [SerializeField] private Transform placePoint;
    [SerializeField] private float pickUpDuration = .5f;
    [SerializeField] private float putDownDuration = .8f;
    [SerializeField] private float liftHeight = .3f;
    [Tooltip("Rides the truck smaller, grows to this while it is set down.")]
    [SerializeField] private float placedScale = 2f;

    private Transform target;
    private bool isPlacing;
    private float duration;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startScale;
    private float timer;

    private void Update()
    {
        if (target == null) return;
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        // reads the target every frame: the carrier may still be turning
        transform.position = Vector3.Lerp(startPosition, target.position, eased) + Vector3.up * Mathf.Sin(t * Mathf.PI) * liftHeight;
        transform.rotation = Quaternion.Slerp(startRotation, target.rotation, eased);
        if (isPlacing) transform.localScale = Vector3.Lerp(startScale, Vector3.one * placedScale, eased);
        if (t < 1f) return;

        Transform reached = target;
        target = null;
        if (isPlacing)
        {
            OnDelivered?.Invoke(this, EventArgs.Empty);
            return;
        }
        AttachTo(reached);
        OnPickedUp?.Invoke(this, EventArgs.Empty);
    }

    // rides on whatever holds it (the truck bed, the Distributor's arms), keeping its size
    public void AttachTo(Transform anchor)
    {
        transform.SetParent(anchor, true);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    // lifted out of the truck bed into the carrier's arms
    public void PickUp(Transform carryPoint) => StartMove(carryPoint, pickUpDuration, false);

    // set down on its spot in front of the house, growing to full size
    public void PutDown() => StartMove(placePoint, putDownDuration, true);

    public Transform GetPlacePoint() => placePoint;

    private void StartMove(Transform target, float duration, bool isPlacing)
    {
        transform.SetParent(null, true);
        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;
        timer = 0f;
        this.duration = duration;
        this.isPlacing = isPlacing;
        this.target = target;
    }
}
