using UnityEngine;

// An old TV in the middle of the screen while the MC remembers the childhood show (ChildhoodShowVisual films it).
[RequireComponent(typeof(CanvasGroup))]
public class ChildhoodShowUI : MonoBehaviour
{
    [SerializeField] private PlantationSequence plantationSequence;
    [SerializeField] private UIPop tvPop;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.SetVisible(false);
    }

    private void Start()
    {
        plantationSequence.OnStepChanged += PlantationSequence_OnStepChanged;
    }

    private void PlantationSequence_OnStepChanged(object sender, PlantationSequence.OnStepChangedEventArgs e)
    {
        bool isShown = e.step == PlantationSequence.Step.Hallucination;
        if (isShown == canvasGroup.alpha > 0f) return;
        canvasGroup.SetVisible(isShown);
        if (isShown) tvPop.Play(.6f);
    }
}
