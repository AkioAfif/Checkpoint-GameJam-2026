using System;
using TMPro;
using UnityEngine;

// The sky run's HUD: how to steer, and how many bunches are caught so far (flashing red when a bird knocks one loose).
[RequireComponent(typeof(CanvasGroup))]
public class SkyRunUI : MonoBehaviour
{
    [SerializeField] private SkyRunMinigame skyRunMinigame;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private string hint = "[A] [D] Steer - catch the sawit, dodge the birds!";
    [SerializeField] private string countFormat = "Sawit x{0}";
    [SerializeField] private Color hitColor = new Color(1f, .3f, .3f);
    [SerializeField] private float hitFlashDuration = .4f;
    [SerializeField] private UIPop countPop;

    private CanvasGroup canvasGroup;
    private Color countColor;
    private float hitTimer;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        countColor = countText.color;
        canvasGroup.SetVisible(false);
    }

    private void Start()
    {
        skyRunMinigame.OnStarted += SkyRunMinigame_OnStarted;
        skyRunMinigame.OnEnded += SkyRunMinigame_OnEnded;
        skyRunMinigame.OnCountChanged += SkyRunMinigame_OnCountChanged;
    }

    private void Update()
    {
        if (hitTimer <= 0f) return;
        hitTimer -= Time.unscaledDeltaTime;
        countText.color = hitTimer > 0f ? hitColor : countColor;
    }

    private void SkyRunMinigame_OnStarted(object sender, EventArgs e)
    {
        hintText.text = hint;
        canvasGroup.SetVisible(true);
    }

    private void SkyRunMinigame_OnEnded(object sender, EventArgs e) => canvasGroup.SetVisible(false);

    private void SkyRunMinigame_OnCountChanged(object sender, SkyRunMinigame.OnCountChangedEventArgs e)
    {
        countText.text = string.Format(countFormat, e.count);
        if (e.isHit) hitTimer = hitFlashDuration;
        countPop.Play();
    }
}
