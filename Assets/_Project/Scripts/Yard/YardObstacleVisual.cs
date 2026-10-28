using UnityEngine;

// What a cleared job looks like: a weed clump shrinks away, a rock sinks into the soil, the stump is yanked up, tips over
// and is gone; a tilled patch only throws up dirt (its plot appears). Then the model is switched off.
public class YardObstacleVisual : MonoBehaviour
{
    public enum Style { Shrink, Sink, Yank, DustOnly }

    [SerializeField] private YardObstacle yardObstacle;
    [Tooltip("Empty for DustOnly.")]
    [SerializeField] private Transform model;
    [SerializeField] private Style style;
    [SerializeField] private Transform dustPrefab;
    [SerializeField] private float duration = .7f;

    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 restScale;
    private float timer = -1f;

    private void Start()
    {
        yardObstacle.OnCleared += YardObstacle_OnCleared;
        if (model == null) return;
        restPosition = model.localPosition;
        restRotation = model.localRotation;
        restScale = model.localScale;
    }

    private void Update()
    {
        if (timer < 0f) return;
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        switch (style)
        {
            case Style.Shrink:
                model.localScale = restScale * (1f - t * t);
                break;
            case Style.Sink:
                model.localPosition = restPosition + Vector3.down * t * t * 1.2f;
                break;
            case Style.Yank:
                // up out of the ground and over on its side, then dragged off
                model.localPosition = restPosition + Vector3.up * Mathf.Sin(t * Mathf.PI) * .8f;
                model.localRotation = restRotation * Quaternion.Euler(0f, 0f, t * 85f);
                model.localScale = restScale * (t < .7f ? 1f : 1f - (t - .7f) / .3f);
                break;
        }
        if (t < 1f) return;
        timer = -1f;
        model.gameObject.SetActive(false);
    }

    private void YardObstacle_OnCleared(object sender, System.EventArgs e)
    {
        Instantiate(dustPrefab, yardObstacle.transform.position, Quaternion.identity);
        if (model != null && style != Style.DustOnly) timer = 0f;
    }
}
