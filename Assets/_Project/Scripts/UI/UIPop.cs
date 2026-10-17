using UnityEngine;

// Small bouncy scale animation for UI juice (works while the game is paused).
public class UIPop : MonoBehaviour
{
    [SerializeField] private float duration = .35f;
    [SerializeField] private float overshoot = .25f;

    private float timer = -1f;
    private float fromScale = 1f;

    private void Update()
    {
        if (timer < 0f) return;
        timer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float scale = Mathf.LerpUnclamped(fromScale, 1f, t) + Mathf.Sin(t * Mathf.PI) * overshoot;
        transform.localScale = Vector3.one * scale;
        if (t >= 1f) timer = -1f;
    }

    // fromScale 0 = pop in from nothing, 1 = a quick pulse
    public void Play(float fromScale = 1f)
    {
        this.fromScale = fromScale;
        timer = 0f;
        transform.localScale = Vector3.one * fromScale;
    }
}
