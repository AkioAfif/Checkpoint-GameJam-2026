using UnityEngine;

// Still water (ponds, the well): the river's look without its current. Nothing scrolls; only the fine detail ripples wander
// round a small circle so the surface stays alive. Uses a property block on the water's material slot, so the shared
// StillWater material (set up by Tools > FixScene > Build Still Water) is never changed.
[RequireComponent(typeof(Renderer))]
public class StillWaterVisual : MonoBehaviour
{
    // URP Lit's detail normal map uses the detail albedo map's tiling and offset
    private static readonly int DETAIL_MAP_ST = Shader.PropertyToID("_DetailAlbedoMap_ST");

    [SerializeField] private int materialIndex;
    [Tooltip("How far (texture repeats) the detail ripples wander.")]
    [SerializeField] private float detailSway = .12f;
    [SerializeField] private float detailSwaySpeed = .15f;

    private Renderer waterRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Vector2 detailTiling;
    private float phase;

    private void Awake()
    {
        waterRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        Vector4 tilingOffset = waterRenderer.sharedMaterials[materialIndex].GetVector(DETAIL_MAP_ST);
        detailTiling = new Vector2(tilingOffset.x, tilingOffset.y);
        // each pond starts somewhere else on the circle, so they don't ripple in step
        phase = Mathf.Abs(transform.position.x * .37f + transform.position.z * .61f);
    }

    private void Update()
    {
        float angle = (Time.time * detailSwaySpeed + phase) * 2f * Mathf.PI;
        waterRenderer.GetPropertyBlock(propertyBlock, materialIndex);
        propertyBlock.SetVector(DETAIL_MAP_ST, new Vector4(detailTiling.x, detailTiling.y, Mathf.Sin(angle) * detailSway, Mathf.Cos(angle * .8f) * detailSway));
        waterRenderer.SetPropertyBlock(propertyBlock, materialIndex);
    }
}
