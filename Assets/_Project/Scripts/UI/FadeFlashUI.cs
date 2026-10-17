using UnityEngine;

// Soft white flash that hides the cut between the village camera and the Fabricator diorama.
[RequireComponent(typeof(CanvasGroup))]
public class FadeFlashUI : MonoBehaviour
{
    [SerializeField] private FabricatorController fabricator;
    [SerializeField] private float fadeDuration = .45f;

    private CanvasGroup canvasGroup;
    private float timer;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private void Start()
    {
        fabricator.OnStateChanged += Fabricator_OnStateChanged;
    }

    private void Update()
    {
        if (timer <= 0f) return;
        timer -= Time.unscaledDeltaTime;
        canvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
    }

    private void Fabricator_OnStateChanged(object sender, FabricatorController.OnStateChangedEventArgs e)
    {
        if (e.state == FabricatorController.State.Puzzle || e.state == FabricatorController.State.Exiting) timer = fadeDuration;
    }
}
