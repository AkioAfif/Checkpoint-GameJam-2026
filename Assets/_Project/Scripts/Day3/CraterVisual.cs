using UnityEngine;

// The final double-fisted blow leaves a cracked crater under the MC, which fades away after a few seconds.
public class CraterVisual : MonoBehaviour
{
    private const string BASE_COLOR = "_BaseColor";

    [SerializeField] private LandSequence landSequence;
    [SerializeField] private Renderer craterRenderer;
    [SerializeField] private Transform smashPoint;
    [SerializeField] private float visibleDuration = 3f;
    [SerializeField] private float fadeDuration = .8f;

    private MaterialPropertyBlock propertyBlock;
    private Color baseColor;
    private float timer = -1f;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        landSequence.OnSmash += LandSequence_OnSmash;
        baseColor = craterRenderer.sharedMaterial.GetColor(BASE_COLOR);
        craterRenderer.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (timer < 0f) return;
        timer += Time.deltaTime;
        float alpha = 1f - Mathf.Clamp01((timer - visibleDuration) / fadeDuration);
        SetAlpha(alpha);
        if (alpha > 0f) return;
        timer = -1f;
        craterRenderer.gameObject.SetActive(false);
    }

    private void LandSequence_OnSmash(object sender, LandSequence.OnSmashEventArgs e)
    {
        if (!e.isFinal) return;
        // in front of the MC, where both fists landed
        Vector3 position = smashPoint.position + smashPoint.forward * .55f;
        craterRenderer.transform.SetPositionAndRotation(new Vector3(position.x, .03f, position.z), Quaternion.Euler(90f, Random.Range(0f, 360f), 0f));
        craterRenderer.gameObject.SetActive(true);
        SetAlpha(1f);
        timer = 0f;
    }

    private void SetAlpha(float alpha)
    {
        propertyBlock.SetColor(BASE_COLOR, new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha));
        craterRenderer.SetPropertyBlock(propertyBlock);
    }
}
