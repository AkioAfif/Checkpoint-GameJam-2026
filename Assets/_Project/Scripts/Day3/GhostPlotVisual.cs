using UnityEngine;

// The see-through plots the MC imagines over the empty grass: on for half a second, off for half a second, while they look.
public class GhostPlotVisual : MonoBehaviour
{
    [SerializeField] private LandSequence landSequence;
    [SerializeField] private GameObject ghostPlotsGameObject;
    [SerializeField] private float blinkInterval = .5f;

    private float timer;

    private void Start()
    {
        ghostPlotsGameObject.SetActive(false);
    }

    private void Update()
    {
        LandSequence.Step step = landSequence.GetStep();
        bool isImagining = step == LandSequence.Step.Inspect || step == LandSequence.Step.Realise;
        if (!isImagining)
        {
            timer = 0f;
            if (ghostPlotsGameObject.activeSelf) ghostPlotsGameObject.SetActive(false);
            return;
        }
        timer += Time.deltaTime;
        ghostPlotsGameObject.SetActive(Mathf.FloorToInt(timer / blinkInterval) % 2 == 0);
    }
}
