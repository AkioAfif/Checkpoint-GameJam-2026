using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Over the MC's head: the "[E] Eat Sawit" hint while they can eat (or "[E] Dig" on Day 5's dig marker), then a ring that
// fills while [E] is held (eating) or mashed (pounding the ground).
[RequireComponent(typeof(CanvasGroup))]
public class LandQTEUI : MonoBehaviour
{
    [SerializeField] private LandSequence landSequence;
    [Tooltip("Day 5: shows the eat hint while the plantation sequence waits for a bite.")]
    [SerializeField] private PlantationSequence plantationSequence;
    [SerializeField] private LandQTE landQTE;
    [SerializeField] private RectTransform group;
    [SerializeField] private GameObject ringGameObject;
    [Tooltip("A radial Filled image.")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private string eatHint = "[{0}] Eat Sawit";
    [SerializeField] private string digHint = "[{0}] Dig";
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
        bool isWaitingToEat = (landSequence != null && landSequence.GetStep() == LandSequence.Step.WaitEat) || (plantationSequence != null && plantationSequence.IsWaitingToEat());
        bool isFree = GameManager.Instance.IsGamePlaying() && !PlayerInteraction.Instance.HasSelected();
        bool canDig = isFree && plantationSequence != null && plantationSequence.CanDig();
        bool canEat = isFree && isWaitingToEat;
        bool isVisible = state != LandQTE.State.Idle || canEat || canDig;
        if (isVisible != canvasGroup.alpha > 0f) canvasGroup.SetVisible(isVisible);
        if (!isVisible) return;

        ringGameObject.SetActive(state != LandQTE.State.Idle);
        string hint = state == LandQTE.State.Hold ? holdHint : state == LandQTE.State.Mash ? mashHint : canDig ? digHint : eatHint;
        labelText.text = string.Format(hint, GameInput.Instance.GetInteractBindingDisplay());
        group.position = Camera.main.WorldToScreenPoint(Player.Instance.transform.position + Vector3.up * 2.2f);
    }

    private void LandQTE_OnProgressChanged(object sender, LandQTE.OnProgressChangedEventArgs e) => fillImage.fillAmount = e.progressNormalized;
}
