using UnityEngine;

// The river is one long interactable: keep its prompt anchor on the water right next to the player instead of one fixed spot.
[RequireComponent(typeof(MeshFilter), typeof(Collider))]
public class RiverPromptAnchor : MonoBehaviour
{
    // same strip layout River reads: rows of this many vertices straight across the water
    private const int ROW_VERTEX_COUNT = 11;

    [SerializeField] private Transform promptAnchor;
    [SerializeField] private float height = 1.3f;

    private Collider waterCollider;
    private Vector3[] leftBankArray;
    private Vector3[] rightBankArray;

    private void Awake()
    {
        waterCollider = GetComponent<Collider>();
        Vector3[] vertexArray = GetComponent<MeshFilter>().sharedMesh.vertices;
        int rowCount = vertexArray.Length / ROW_VERTEX_COUNT;
        leftBankArray = new Vector3[rowCount];
        rightBankArray = new Vector3[rowCount];
        for (int row = 0; row < rowCount; row++)
        {
            leftBankArray[row] = transform.TransformPoint(vertexArray[row * ROW_VERTEX_COUNT]);
            rightBankArray[row] = transform.TransformPoint(vertexArray[row * ROW_VERTEX_COUNT + ROW_VERTEX_COUNT - 1]);
        }
    }

    private void LateUpdate()
    {
        Vector3 player = Player.Instance.transform.position;
        promptAnchor.position = ClosestWaterPoint(player) + Vector3.up * height;
    }

    // straight below the player when they stand over the water, else the nearest point on either bank edge
    private Vector3 ClosestWaterPoint(Vector3 position)
    {
        if (waterCollider.Raycast(new Ray(position + Vector3.up * 5f, Vector3.down), out RaycastHit hit, 10f)) return hit.point;

        Vector3 closest = leftBankArray[0];
        float closestSqrDistance = float.MaxValue;
        for (int i = 0; i < leftBankArray.Length - 1; i++)
        {
            TryCloser(leftBankArray[i], leftBankArray[i + 1], position, ref closest, ref closestSqrDistance);
            TryCloser(rightBankArray[i], rightBankArray[i + 1], position, ref closest, ref closestSqrDistance);
        }
        return closest;
    }

    // closest point on segment a-b to the position, measured flat on the ground
    private static void TryCloser(Vector3 a, Vector3 b, Vector3 position, ref Vector3 closest, ref float closestSqrDistance)
    {
        Vector3 ab = b - a;
        ab.y = 0f;
        Vector3 ap = position - a;
        ap.y = 0f;
        float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude) : 0f;
        Vector3 point = Vector3.Lerp(a, b, t);
        Vector3 offset = point - position;
        offset.y = 0f;
        if (offset.sqrMagnitude >= closestSqrDistance) return;
        closestSqrDistance = offset.sqrMagnitude;
        closest = point;
    }
}
