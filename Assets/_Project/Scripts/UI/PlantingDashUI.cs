using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Top-centre during Day 5's Plough Dash: the hint, the charge bar and how many dashes are left.
[RequireComponent(typeof(CanvasGroup))]
public class PlantingDashUI : MonoBehaviour
{
    [SerializeField] private PlantingDash plantingDash;
    [SerializeField] private Day5SO day5SO;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image chargeFillImage;
    [SerializeField] private UIPop countPop;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    private void Start()
    {
        hintText.text = day5SO.dashHint;
        chargeFillImage.fillAmount = 0f;
        plantingDash.OnDashesChanged += PlantingDash_OnDashesChanged;
        plantingDash.OnChargeChanged += PlantingDash_OnChargeChanged;
        plantingDash.OnFinished += PlantingDash_OnFinished;
    }

    private void PlantingDash_OnDashesChanged(object sender, EventArgs e)
    {
        canvasGroup.SetVisible(true);
        countText.text = string.Format(day5SO.dashCountFormat, plantingDash.GetDashesLeft());
        countPop.Play();
    }

    private void PlantingDash_OnChargeChanged(object sender, PlantingDash.OnChargeChangedEventArgs e) => chargeFillImage.fillAmount = e.chargeNormalized;

    private void PlantingDash_OnFinished(object sender, EventArgs e) => Hide();

    private void Hide() => canvasGroup.SetVisible(false);
}
