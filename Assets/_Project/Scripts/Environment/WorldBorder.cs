using UnityEngine;

// Invisible walls along the playable outline. Any scene can use it: place a WorldBorder object,
// give it the outline points, then bake the walls with Tools > World Border > Bake Colliders.
public class WorldBorder : MonoBehaviour
{
    public const string COLLISION_BOUNDARY_NAME = "CollisionBoundary";

    // XZ points in this object's local space; the outline is a closed loop, one wall per pair of neighbours
    [Tooltip("Outline on the XZ plane, in local space. Closed loop, in order around the playable area.")]
    [SerializeField] private Vector2[] pointArray = new Vector2[0];
    [SerializeField] private float wallHeight = 12f;
    [SerializeField] private float wallThickness = 1f;
    [SerializeField] private float wallBottomY = -1f;

    private Collider[] colliderArray;

    private void Awake()
    {
        if (pointArray.Length < 3) Debug.LogError($"{name}: WorldBorder needs at least 3 outline points", this);
        if (transform.Find(COLLISION_BOUNDARY_NAME) == null) Debug.LogError($"{name}: no {COLLISION_BOUNDARY_NAME}. Run Tools > World Border > Bake Colliders", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (pointArray == null || pointArray.Length < 2) return;
        Gizmos.color = Color.magenta;
        for (int i = 0; i < pointArray.Length; i++)
        {
            Vector3 a = transform.TransformPoint(new Vector3(pointArray[i].x, wallBottomY, pointArray[i].y));
            Vector3 b = transform.TransformPoint(new Vector3(pointArray[(i + 1) % pointArray.Length].x, wallBottomY, pointArray[(i + 1) % pointArray.Length].y));
            Gizmos.DrawLine(a, b);
        }
    }

    public Vector2[] GetPointArray() => pointArray;
    public float GetWallHeight() => wallHeight;
    public float GetWallThickness() => wallThickness;
    public float GetWallBottomY() => wallBottomY;

    // the baked wall colliders (empty until the bake has run)
    public Collider[] GetColliderArray()
    {
        if (colliderArray == null)
        {
            Transform boundary = transform.Find(COLLISION_BOUNDARY_NAME);
            colliderArray = boundary != null ? boundary.GetComponentsInChildren<BoxCollider>() : new Collider[0];
        }
        return colliderArray;
    }
}
