using UnityEngine;

// The visual half of the world border: links the scene to its WorldBorderLayoutSO. Tools > World Border > Bake Visuals
// builds the layout into the VisualBorder child (trees, rocks, fences, skirt, mountains). Nothing here runs in play mode.
public class WorldBorderDressing : MonoBehaviour
{
    public const string VISUAL_ROOT_NAME = "VisualBorder";

    [SerializeField] private WorldBorderLayoutSO worldBorderLayoutSO;

    private void OnDrawGizmosSelected()
    {
        if (worldBorderLayoutSO == null) return;

        Gizmos.color = Color.cyan;
        foreach (WorldBorderLayoutSO.Strip strip in worldBorderLayoutSO.stripArray)
            Gizmos.DrawLine(transform.TransformPoint(new Vector3(strip.start.x, .5f, strip.start.y)), transform.TransformPoint(new Vector3(strip.end.x, .5f, strip.end.y)));

        Gizmos.color = Color.yellow;
        foreach (WorldBorderLayoutSO.Road road in worldBorderLayoutSO.roadArray)
        {
            Vector2 end = road.start + road.direction.normalized * road.length;
            Gizmos.DrawLine(transform.TransformPoint(new Vector3(road.start.x, .5f, road.start.y)), transform.TransformPoint(new Vector3(end.x, .5f, end.y)));
        }
    }

    public WorldBorderLayoutSO GetWorldBorderLayoutSO() => worldBorderLayoutSO;
}
