using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Esc menu: resume, and rebind the Interact key (every "Press [E]" prompt follows the new key).
[RequireComponent(typeof(CanvasGroup))]
public class PauseUI : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button rebindButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI rebindButtonText;
    [SerializeField] private GameObject pressKeyOverlay;
    [SerializeField] private string rebindFormat = "Interact key: [{0}]";

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        resumeButton.onClick.AddListener(() => GameManager.Instance.TogglePauseGame());
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
        canvasGroup.SetVisible(true);
        resumeButton.Select();
    }

    private void GameManager_OnGameUnpaused(object sender, EventArgs e) => Hide();

    private void GameInput_OnBindingDisplayChanged(object sender, EventArgs e) => RefreshRebindText();

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
