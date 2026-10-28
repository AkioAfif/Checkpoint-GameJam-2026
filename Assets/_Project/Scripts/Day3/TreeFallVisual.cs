using UnityEngine;

// Topples a felled tree's model about its trunk base, lets it lie a moment, sinks it into the grass and switches it off,
// with a puff of dirt where it lands.
public class TreeFallVisual : MonoBehaviour
{
    [SerializeField] private PunchableTree punchableTree;
    [SerializeField] private Transform dustPrefab;
    [SerializeField] private float fallDuration = .8f;
    [SerializeField] private float lieDuration = .6f;
    [SerializeField] private float sinkDuration = .6f;
    [SerializeField] private float sinkDepth = 1.2f;

    private Transform tree;
    private Vector3 pivot;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 axis;
    private float timer = -1f;
    private bool hasLanded;

    private void Start()
    {
        punchableTree.OnFelled += PunchableTree_OnFelled;
        tree = punchableTree.GetTreeTransform();
    }

    private void Update()
    {
        if (timer < 0f) return;
        timer += Time.deltaTime;

        // slow at the top, crashing at the end
        float fall = Mathf.Clamp01(timer / fallDuration);
        Quaternion tilt = Quaternion.AngleAxis(fall * fall * 88f, axis);
        float sink = Mathf.Clamp01((timer - fallDuration - lieDuration) / sinkDuration) * sinkDepth;
        tree.SetPositionAndRotation(pivot + tilt * (restPosition - pivot) + Vector3.down * sink, tilt * restRotation);

        if (fall >= 1f && !hasLanded)
        {
            hasLanded = true;
            Vector3 landing = pivot + Vector3.Cross(axis, Vector3.up) * 2.5f;
            Instantiate(dustPrefab, landing, Quaternion.identity).localScale = Vector3.one * 1.6f;
        }
        if (timer < fallDuration + lieDuration + sinkDuration) return;
        timer = -1f;
        tree.gameObject.SetActive(false);
    }

    private void PunchableTree_OnFelled(object sender, PunchableTree.OnFelledEventArgs e)
    {
        Bounds bounds = new Bounds(tree.position, Vector3.zero);
        foreach (Renderer part in tree.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(part.bounds);
        // the trunk's foot, under this object
        pivot = new Vector3(punchableTree.transform.position.x, bounds.min.y, punchableTree.transform.position.z);
        restPosition = tree.position;
        restRotation = tree.rotation;
        axis = Vector3.Cross(Vector3.up, e.fallDirection);
        hasLanded = false;
        timer = 0f;
    }
}
