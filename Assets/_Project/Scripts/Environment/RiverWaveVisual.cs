using UnityEngine;

// Lifts and drops the river surface a few centimetres in uneven waves: two carried downstream by the current at different
// lengths and angles, and one almost standing still (the chop where fast water piles up), all calming toward the banks.
// River waves, not sea swell: low, short, irregular, and never rolling in to a shore. The fine ripples are normal maps
// (RiverFlowVisual); the collider keeps the flat mesh.
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(RiverFlowVisual))]
public class RiverWaveVisual : MonoBehaviour
{
    [SerializeField] private River river;
    [Tooltip("Tallest rise (m) mid-river; the banks stay still.")]
    [SerializeField] private float amplitude = .06f;
    [Tooltip("Wavelengths (m) along the flow of the two carried waves and the near-standing chop. The mesh has a row every metre, so keep them above ~3 m.")]
    [SerializeField] private Vector3 wavelengths = new Vector3(5.3f, 3.7f, 3.2f);

    private Mesh mesh;
    private Vector3[] restVertexArray;
    private Vector3[] vertexArray;
    // per vertex: metres downstream, and 0..1 from one bank to the other
    private float[] alongArray;
    private float[] acrossArray;

    private void Awake()
    {
        // its own copy of the mesh to bend (the collider keeps the flat original)
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.MarkDynamic();
        restVertexArray = mesh.vertices;
        vertexArray = (Vector3[])restVertexArray.Clone();
        Vector2[] uvArray = mesh.uv;
        float maxAcross = 0f;
        foreach (Vector2 uv in uvArray) maxAcross = Mathf.Max(maxAcross, uv.x);
        float metresPerRepeat = GetComponent<RiverFlowVisual>().GetMetresPerRepeat();
        alongArray = new float[uvArray.Length];
        acrossArray = new float[uvArray.Length];
        for (int i = 0; i < uvArray.Length; i++)
        {
            alongArray[i] = uvArray[i].y * metresPerRepeat;
            acrossArray[i] = uvArray[i].x / maxAcross;
        }
        Bounds bounds = mesh.bounds;
        bounds.Expand(new Vector3(0f, amplitude * 4f, 0f));
        mesh.bounds = bounds;
    }

    private void Update()
    {
        float time = Time.time;
        float travelled = river.GetCurrentSpeed() * time;
        for (int i = 0; i < vertexArray.Length; i++)
        {
            float along = alongArray[i];
            float across = acrossArray[i];
            // carried by the current, its crests bent by a slow wobble across the river
            float carried = Mathf.Sin((along - travelled) / wavelengths.x * 2f * Mathf.PI + Mathf.Sin(across * 8.2f + time * .4f) * 1.7f);
            // a shorter one at an angle, a little slower than the water
            float angled = Mathf.Sin((along * .8f - travelled * .6f) / wavelengths.y * 2f * Mathf.PI + across * 13.8f + 1.1f);
            // chop that barely moves downstream but flickers in place
            float chop = Mathf.Sin((along - travelled * .15f) / wavelengths.z * 2f * Mathf.PI - across * 19.5f + time * 1.7f);
            float bankCalm = Mathf.Sin(across * Mathf.PI);
            vertexArray[i].y = restVertexArray[i].y + amplitude * bankCalm * (carried * .5f + angled * .3f + chop * .2f);
        }
        mesh.vertices = vertexArray;
        mesh.RecalculateNormals();
    }
}
