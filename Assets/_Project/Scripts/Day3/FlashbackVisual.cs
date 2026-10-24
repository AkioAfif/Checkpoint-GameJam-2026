using UnityEngine;
using UnityEngine.Rendering;

// The flashback is black and white (a full-weight grayscale Volume), and the Distributor's glasses flash once after he
// pushes them up: an anime glint that pops big and shrinks away. The MC is hidden for his solo shot, filmed from where
// the MC stands.
public class FlashbackVisual : MonoBehaviour
{
    [SerializeField] private LandSequence landSequence;
    [SerializeField] private Volume grayscaleVolume;
    [Tooltip("The player's Visual child, hidden during the Distributor's solo shot.")]
    [SerializeField] private Transform playerVisual;
    [Tooltip("A bright quad at the Distributor's glasses.")]
    [SerializeField] private Transform glint;
    [Tooltip("Seconds into his solo shot (while he adjusts the glasses).")]
    [SerializeField] private float glintDelay = .9f;
    [SerializeField] private float glintDuration = .18f;
    [SerializeField] private float glintScale = .5f;

    private float soloTimer = -1f;
    private Renderer[] playerRendererArray;

    private void Awake()
    {
        playerRendererArray = playerVisual.GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        landSequence.OnStepChanged += LandSequence_OnStepChanged;
        grayscaleVolume.weight = 0f;
        glint.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (soloTimer < 0f) return;
        soloTimer += Time.deltaTime;
        float t = (soloTimer - glintDelay) / glintDuration;
        bool isGlinting = t >= 0f && t <= 1f;
        glint.gameObject.SetActive(isGlinting);
        if (!isGlinting) return;
        glint.localScale = Vector3.one * glintScale * Mathf.Sin(t * Mathf.PI);
        // a flat card: always faces the camera
        glint.forward = glint.position - Camera.main.transform.position;
        glint.Rotate(0f, 0f, t * 90f, Space.Self);
    }

    private void LandSequence_OnStepChanged(object sender, LandSequence.OnStepChangedEventArgs e)
    {
        bool isFlashback = e.step == LandSequence.Step.Flashback || e.step == LandSequence.Step.FlashbackSolo;
        grayscaleVolume.weight = isFlashback ? 1f : 0f;
        soloTimer = e.step == LandSequence.Step.FlashbackSolo ? 0f : -1f;
        if (soloTimer < 0f) glint.gameObject.SetActive(false);
        // renderers only: the Visual's scripts and Animator keep running
        foreach (Renderer playerRenderer in playerRendererArray) playerRenderer.enabled = soloTimer < 0f;
    }
}
