using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shrinking ring around the crouching player, one pip per check, and a big result banner.
[RequireComponent(typeof(CanvasGroup))]
public class QTEUI : MonoBehaviour
{
    [SerializeField] private RectTransform ringGroup;
    [SerializeField] private RectTransform shrinkingRing;
    [SerializeField] private Image shrinkingRingImage;
    [SerializeField] private RectTransform hitZone;
    [SerializeField] private RectTransform perfectZone;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private UIPop feedbackPop;
    [SerializeField] private Image[] pipImageArray;
    [SerializeField] private TextMeshProUGUI bannerText;
    [SerializeField] private UIPop bannerPop;
    [SerializeField] private CanvasGroup bannerCanvasGroup;
    [SerializeField] private Color hitColor = new Color(.55f, .9f, .45f);
    [SerializeField] private Color perfectColor = new Color(1f, .82f, .3f);
    [SerializeField] private Color missColor = new Color(.92f, .45f, .42f);
    [SerializeField] private Color pendingColor = new Color(1f, .97f, .91f);
    [SerializeField] private string successBanner = "Planted!";
    [SerializeField] private string failBanner = "Oops! Try again";
    [SerializeField] private float targetSize = 140f;

    private CanvasGroup canvasGroup;
    private float bannerTimer;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
        bannerCanvasGroup.alpha = 0f;
    }

    private void Start()
    {
        QTEManager.Instance.OnCheckStarted += QTEManager_OnCheckStarted;
        QTEManager.Instance.OnCheckResolved += QTEManager_OnCheckResolved;
        PlantingController.Instance.OnPlantingFinished += PlantingController_OnPlantingFinished;
        PlantingController.Instance.OnStateChanged += PlantingController_OnStateChanged;
    }

    private void Update()
    {
        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.deltaTime;
            bannerCanvasGroup.alpha = Mathf.Clamp01(bannerTimer * 3f);
        }
        if (!PlantingController.Instance.IsPlanting()) return;

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(Player.Instance.transform.position + Vector3.up * .9f);
        ringGroup.position = screenPoint;
        if (QTEManager.Instance.IsRunning()) shrinkingRing.sizeDelta = Vector2.one * targetSize * Mathf.Max(0f, QTEManager.Instance.GetRingScale());
    }

    private void OnDestroy()
    {
        if (QTEManager.Instance != null)
        {
            QTEManager.Instance.OnCheckStarted -= QTEManager_OnCheckStarted;
            QTEManager.Instance.OnCheckResolved -= QTEManager_OnCheckResolved;
        }
        if (PlantingController.Instance != null)
        {
            PlantingController.Instance.OnPlantingFinished -= PlantingController_OnPlantingFinished;
            PlantingController.Instance.OnStateChanged -= PlantingController_OnStateChanged;
        }
    }

    private void PlantingController_OnStateChanged(object sender, PlantingController.OnStateChangedEventArgs e)
    {
        if (e.state == PlantingController.State.QTE) Show();
        else Hide();
    }

    private void QTEManager_OnCheckStarted(object sender, QTEManager.OnCheckStartedEventArgs e)
    {
        if (e.index == 0)
        {
            for (int i = 0; i < pipImageArray.Length; i++)
            {
                pipImageArray[i].gameObject.SetActive(i < e.count);
                pipImageArray[i].color = pendingColor;
            }
            hitZone.sizeDelta = Vector2.one * targetSize * (1f + QTEManager.Instance.GetHitWindow()) ;
            perfectZone.sizeDelta = Vector2.one * targetSize * (1f + QTEManager.Instance.GetPerfectWindow());
        }
        keyText.text = GameInput.Instance.GetInteractBindingDisplay();
        shrinkingRing.gameObject.SetActive(true);
        shrinkingRingImage.color = Color.white;
        feedbackText.text = string.Empty;
    }

    private void QTEManager_OnCheckResolved(object sender, QTEManager.OnCheckResolvedEventArgs e)
    {
        Color color = e.result == QTEManager.Result.Perfect ? perfectColor : e.result == QTEManager.Result.Hit ? hitColor : missColor;
        if (e.index < pipImageArray.Length) pipImageArray[e.index].color = color;
        shrinkingRing.gameObject.SetActive(false);
        feedbackText.text = e.result == QTEManager.Result.Perfect ? "Perfect!" : e.result == QTEManager.Result.Hit ? "Good!" : "Miss";
        feedbackText.color = color;
        feedbackPop.Play(.4f);
    }

    private void PlantingController_OnPlantingFinished(object sender, PlantingController.OnPlantingFinishedEventArgs e)
    {
        bannerText.text = e.isSuccess ? successBanner : failBanner;
        bannerText.color = e.isSuccess ? hitColor : missColor;
        bannerTimer = 1.6f;
        bannerCanvasGroup.alpha = 1f;
        bannerPop.Play(0f);
    }

    private void Show() => canvasGroup.SetVisible(true);
    private void Hide() => canvasGroup.SetVisible(false);
}
