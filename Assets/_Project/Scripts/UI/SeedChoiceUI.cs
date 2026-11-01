using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Opens when the player plants with more than one kind of seed in the bag: which seed goes in, and how many are left.
[RequireComponent(typeof(CanvasGroup))]
public class SeedChoiceUI : MonoBehaviour
{
    [SerializeField] private Image seedIconImage;
    [SerializeField] private TextMeshProUGUI seedNameText;
    [SerializeField] private TextMeshProUGUI seedCountText;
    [SerializeField] private Button previousSeedButton;
    [SerializeField] private Button nextSeedButton;
    [SerializeField] private Button plantButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private string countFormat = "You have {0}   ({1}/{2})";

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        previousSeedButton.onClick.AddListener(() => { PlantingController.Instance.ChangeSeed(-1); });
        nextSeedButton.onClick.AddListener(() => { PlantingController.Instance.ChangeSeed(1); });
        plantButton.onClick.AddListener(() => { PlantingController.Instance.ConfirmSeed(); });
        cancelButton.onClick.AddListener(() => { PlantingController.Instance.CancelSeed(); });
        Hide();
    }

    private void Start()
    {
        PlantingController.Instance.OnStateChanged += PlantingController_OnStateChanged;
        PlantingController.Instance.OnSeedChoiceChanged += PlantingController_OnSeedChoiceChanged;
    }

    private void OnDestroy()
    {
        if (PlantingController.Instance == null) return;
        PlantingController.Instance.OnStateChanged -= PlantingController_OnStateChanged;
        PlantingController.Instance.OnSeedChoiceChanged -= PlantingController_OnSeedChoiceChanged;
    }

    private void PlantingController_OnStateChanged(object sender, PlantingController.OnStateChangedEventArgs e)
    {
        if (e.state == PlantingController.State.Choosing) Show();
        else Hide();
    }

    private void PlantingController_OnSeedChoiceChanged(object sender, EventArgs e) => UpdateVisual();

    private void UpdateVisual()
    {
        ItemDataSO seedItemDataSO = PlantingController.Instance.GetChosenCropDataSO().seedItemDataSO;
        seedIconImage.sprite = seedItemDataSO.icon;
        seedIconImage.enabled = seedItemDataSO.icon != null;
        seedNameText.text = seedItemDataSO.itemName;
        seedCountText.text = string.Format(countFormat, Inventory.Instance.GetCount(seedItemDataSO),
            PlantingController.Instance.GetSeedChoiceIndex() + 1, PlantingController.Instance.GetSeedChoiceCount());
    }

    private void Show()
    {
        UpdateVisual();
        canvasGroup.SetVisible(true);
        plantButton.Select();
    }

    private void Hide() => canvasGroup.SetVisible(false);
}
