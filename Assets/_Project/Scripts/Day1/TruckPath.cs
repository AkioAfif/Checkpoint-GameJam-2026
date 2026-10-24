using UnityEngine;

// The truck's road as a smooth curve: a Catmull-Rom spline through the route points, sampled into short steps
// so corners are real arcs instead of polyline kinks. Flat (y is taken from the points).
public class TruckPath
{
    private const float SAMPLE_SPACING = .5f;

    private readonly Vector3[] positionArray;
    private readonly float[] distanceArray;

    public TruckPath(Transform[] pointArray)
    {
        int last = pointArray.Length - 1;
        // a rough sample count from the straight-line length; spacing only needs to be about right
        float roughLength = 0f;
        for (int i = 0; i < last; i++) roughLength += Vector3.Distance(pointArray[i].position, pointArray[i + 1].position);
        int perSegment = Mathf.Max(2, Mathf.CeilToInt(roughLength / SAMPLE_SPACING / last));

        positionArray = new Vector3[last * perSegment + 1];
        distanceArray = new float[positionArray.Length];
        for (int segment = 0; segment < last; segment++)
        {
            // the ends repeat themselves so the curve starts and finishes on the first and last points
            Vector3 p0 = pointArray[Mathf.Max(segment - 1, 0)].position;
            Vector3 p1 = pointArray[segment].position;
            Vector3 p2 = pointArray[segment + 1].position;
            Vector3 p3 = pointArray[Mathf.Min(segment + 2, last)].position;
            for (int step = 0; step < perSegment; step++)
                positionArray[segment * perSegment + step] = CatmullRom(p0, p1, p2, p3, (float)step / perSegment);
        }
        positionArray[positionArray.Length - 1] = pointArray[last].position;
        for (int i = 1; i < positionArray.Length; i++)
            distanceArray[i] = distanceArray[i - 1] + Vector3.Distance(positionArray[i - 1], positionArray[i]);
    }

    public float GetLength() => distanceArray[distanceArray.Length - 1];

    public Vector3 GetPositionAt(float distance)
    {
        int i = IndexAt(distance);
        float span = distanceArray[i + 1] - distanceArray[i];
        float t = span > 0f ? (distance - distanceArray[i]) / span : 0f;
        return Vector3.Lerp(positionArray[i], positionArray[i + 1], t);
    }

    // flat unit direction of travel
    public Vector3 GetHeadingAt(float distance)
    {
        int i = IndexAt(distance);
        Vector3 heading = positionArray[i + 1] - positionArray[i];
        heading.y = 0f;
        return heading.normalized;
    }

    // distance along the path closest to position, only looking ahead of searchFrom so it never jumps back
    public float GetClosestDistance(Vector3 position, float searchFrom)
    {
        int best = IndexAt(searchFrom);
        float bestSqr = float.MaxValue;
        for (int i = best; i < positionArray.Length && distanceArray[i] < searchFrom + 15f; i++)
        {
            Vector3 offset = positionArray[i] - position;
            offset.y = 0f;
            if (offset.sqrMagnitude < bestSqr)
            {
                bestSqr = offset.sqrMagnitude;
                best = i;
            }
        }
        return distanceArray[best];
    }

    private int IndexAt(float distance)
    {
        distance = Mathf.Clamp(distance, 0f, GetLength());
        int low = 0;
        int high = distanceArray.Length - 2;
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            if (distanceArray[mid] <= distance) low = mid;
            else high = mid - 1;
        }
        return low;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return .5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }
}
