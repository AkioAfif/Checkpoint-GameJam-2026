using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Over the MC's head: the "[E] Eat Sawit" hint while they can eat, then a ring that fills while [E] is held (eating) or
// mashed (pounding the ground).
[RequireComponent(typeof(CanvasGroup))]
public class LandQTEUI : MonoBehaviour
{
    [SerializeField] private LandSequence landSequence;
    [SerializeField] private LandQTE landQTE;
    [SerializeField] private RectTransform group;
    [SerializeField] private GameObject ringGameObject;
    [Tooltip("A radial Filled image.")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private string eatHint = "[{0}] Eat Sawit";
    [SerializeField] private string holdHint = "Hold [{0}] to eat!";
    [SerializeField] private string mashHint = "Mash [{0}]!";

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.SetVisible(false);
    }

    private void Start()
    {
        landQTE.OnProgressChanged += LandQTE_OnProgressChanged;
    }

    private void Update()
    {
        LandQTE.State state = landQTE.GetState();
        // the yard day (no LandSequence) only uses the ring
        bool canEat = landSequence != null && landSequence.GetStep() == LandSequence.Step.WaitEat && GameManager.Instance.IsGamePlaying() && !PlayerInteraction.Instance.HasSelected();
        bool isVisible = state != LandQTE.State.Idle || canEat;
        if (isVisible != canvasGroup.alpha > 0f) canvasGroup.SetVisible(isVisible);
        if (!isVisible) return;

        ringGameObject.SetActive(state != LandQTE.State.Idle);
        string hint = state == LandQTE.State.Hold ? holdHint : state == LandQTE.State.Mash ? mashHint : eatHint;
        labelText.text = string.Format(hint, GameInput.Instance.GetInteractBindingDisplay());
        group.position = Camera.main.WorldToScreenPoint(Player.Instance.transform.position + Vector3.up * 2.2f);
    }

    private void LandQTE_OnProgressChanged(object sender, LandQTE.OnProgressChangedEventArgs e) => fillImage.fillAmount = e.progressNormalized;
}
