using UnityEngine;

// Eyelids: two black bars close from the top and bottom and open again, twice before the glitch and once after it.
public class EyeBlinkUI : MonoBehaviour
{
    [SerializeField] private LandSequence landSequence;
    [Tooltip("Pivot at the top of the screen.")]
    [SerializeField] private RectTransform topLid;
    [Tooltip("Pivot at the bottom of the screen.")]
    [SerializeField] private RectTransform bottomLid;

    private float timer = -1f;
    private float duration;

    private void Start()
    {
        landSequence.OnStepChanged += LandSequence_OnStepChanged;
        SetClosed(0f);
    }

    private void Update()
    {
        if (timer < 0f) return;
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            timer = -1f;
            SetClosed(0f);
            return;
        }
        // one full close-and-open per blink
        SetClosed(Mathf.Abs(Mathf.Sin(timer / landSequence.GetBlinkDuration() * Mathf.PI)));
    }

    private void LandSequence_OnStepChanged(object sender, LandSequence.OnStepChangedEventArgs e)
    {
        if (e.step != LandSequence.Step.BlinkIn && e.step != LandSequence.Step.BlinkOut) return;
        duration = landSequence.GetBlinkDuration() * (e.step == LandSequence.Step.BlinkIn ? 2f : 1f);
        timer = 0f;
    }

    private void SetClosed(float closedNormalized)
    {
        topLid.localScale = new Vector3(1f, closedNormalized, 1f);
        bottomLid.localScale = new Vector3(1f, closedNormalized, 1f);
    }
}
