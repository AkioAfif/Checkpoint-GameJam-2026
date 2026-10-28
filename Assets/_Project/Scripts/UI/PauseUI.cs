using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Esc menu: resume, "unstuck me" (back to the front of the house), and rebind the Interact key (every "Press [E]" prompt follows the new key).
[RequireComponent(typeof(CanvasGroup))]
public class PauseUI : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button unstuckButton;
    [SerializeField] private TextMeshProUGUI unstuckButtonText;
    [Tooltip("Shown on the Unstuck button when this scene has no PlayerHomeReturnPoint.")]
    [SerializeField] private string unstuckUnavailableText = "Can't find home here";
    [SerializeField] private Button rebindButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI rebindButtonText;
    [SerializeField] private GameObject pressKeyOverlay;
    [SerializeField] private string rebindFormat = "Interact key: [{0}]";

    private CanvasGroup canvasGroup;
    private string unstuckLabel;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        resumeButton.onClick.AddListener(() => GameManager.Instance.TogglePauseGame());
        unstuckButton.onClick.AddListener(Unstuck);
        unstuckLabel = unstuckButtonText.text;
        rebindButton.onClick.AddListener(StartRebind);
        resetButton.onClick.AddListener(() => GameInput.Instance.ResetBindings());
        Hide();
    }

    private void Start()
    {
        GameManager.Instance.OnGamePaused += GameManager_OnGamePaused;
        GameManager.Instance.OnGameUnpaused += GameManager_OnGameUnpaused;
        GameInput.Instance.OnBindingDisplayChanged += GameInput_OnBindingDisplayChanged;
        pressKeyOverlay.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused -= GameManager_OnGamePaused;
            GameManager.Instance.OnGameUnpaused -= GameManager_OnGameUnpaused;
        }
        if (GameInput.Instance != null) GameInput.Instance.OnBindingDisplayChanged -= GameInput_OnBindingDisplayChanged;
    }

    private void GameManager_OnGamePaused(object sender, EventArgs e)
    {
        RefreshRebindText();
        unstuckButtonText.text = unstuckLabel;
        canvasGroup.SetVisible(true);
        resumeButton.Select();
    }

    private void GameManager_OnGameUnpaused(object sender, EventArgs e) => Hide();

    private void GameInput_OnBindingDisplayChanged(object sender, EventArgs e) => RefreshRebindText();

    // closes the menu the way Resume does
    private void Unstuck()
    {
        // the bedroom has no walking player
        if (Player.Instance != null && Player.Instance.TryReturnHome()) GameManager.Instance.TogglePauseGame();
        else unstuckButtonText.text = unstuckUnavailableText;
    }

    private void StartRebind()
    {
        pressKeyOverlay.SetActive(true);
        GameInput.Instance.RebindInteract(() =>
        {
            pressKeyOverlay.SetActive(false);
            rebindButton.Select();
        });
    }

    private void RefreshRebindText()
    {
        rebindButtonText.text = string.Format(rebindFormat, GameInput.Instance.GetInteractBindingDisplay());
    }

    private void Hide() => canvasGroup.SetVisible(false);
}
