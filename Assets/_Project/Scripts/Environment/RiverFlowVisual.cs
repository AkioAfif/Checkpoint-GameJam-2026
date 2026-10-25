using UnityEngine;

// Scrolls the river's textures downstream at the River's current speed: the flow streaks and main ripples (_BaseMap, which
// the normal map shares) ride the current, while the finer detail ripples go slower and wander sideways, so the two layers
// keep crossing and the ripples never settle into a pattern. The mesh's UVs run along the river (v = metres from the first
// row ÷ metresPerRepeat), set up by Tools > FixScene > Build River Flow.
[RequireComponent(typeof(Renderer))]
public class RiverFlowVisual : MonoBehaviour
{
    private static readonly int BASE_MAP = Shader.PropertyToID("_BaseMap");
    // URP Lit's detail normal map uses the detail albedo map's tiling and offset
    private static readonly int DETAIL_MAP = Shader.PropertyToID("_DetailAlbedoMap");

    [SerializeField] private River river;
    [Tooltip("Metres of river one texture repeat covers along the flow (RiverFlowBuilder.TILE_LENGTH).")]
    [SerializeField] private float metresPerRepeat = 8f;
    [Tooltip("The detail ripples' speed as a share of the current.")]
    [SerializeField] private float detailSpeedRatio = .55f;
    [Tooltip("How far (texture repeats) and how fast the detail ripples sway across the flow.")]
    [SerializeField] private float detailSway = .15f;
    [SerializeField] private float detailSwaySpeed = .23f;

    private Material material;
    private float offset;
    private float detailOffset;
    private float detailTilingAlong;

    private void Awake()
    {
        // one river, so its own material instance is fine
        material = GetComponent<Renderer>().material;
        detailTilingAlong = material.GetTextureScale(DETAIL_MAP).y;
    }

    private void Update()
    {
        // a falling offset slides the pattern toward higher v: downstream
        float repeatsPerSecond = river.GetCurrentSpeed() / metresPerRepeat;
        offset = Mathf.Repeat(offset - repeatsPerSecond * Time.deltaTime, 1f);
        material.SetTextureOffset(BASE_MAP, new Vector2(0f, offset));

        detailOffset = Mathf.Repeat(detailOffset - repeatsPerSecond * detailTilingAlong * detailSpeedRatio * Time.deltaTime, 1f);
        float sway = Mathf.Sin(Time.time * detailSwaySpeed * 2f * Mathf.PI) * detailSway;
        material.SetTextureOffset(DETAIL_MAP, new Vector2(sway, detailOffset));
    }

    public float GetMetresPerRepeat() => metresPerRepeat;
}
