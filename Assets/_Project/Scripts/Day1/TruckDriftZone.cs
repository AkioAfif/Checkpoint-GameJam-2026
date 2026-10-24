using UnityEngine;

// A stretch of road where the Distributor's truck drifts. Light: a held, gentle slide while inside (the yellow bend).
// Hard: entering it throws the truck into a sharp handbrake drift through the next corner (the red junction).
// Only data; TruckDriver reacts when its Rigidbody enters the trigger.
[RequireComponent(typeof(BoxCollider))]
public class TruckDriftZone : MonoBehaviour
{
    public enum Kind { Light, Hard }

    [SerializeField] private Kind kind = Kind.Light;
    [Tooltip("Speed the truck aims for while in this zone (m/s).")]
    [SerializeField] private float entrySpeed = 12f;
    [Tooltip("Degrees the body is held sideways to its direction of travel.")]
    [SerializeField] private float slipAngle = 18f;

    public Kind GetKind() => kind;
    public float GetEntrySpeed() => entrySpeed;
    public float GetSlipAngle() => slipAngle;

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = kind == Kind.Light ? new Color(1f, .9f, 0f, .35f) : new Color(1f, .1f, .1f, .45f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
