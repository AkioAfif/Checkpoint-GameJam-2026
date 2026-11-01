using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main menu settings: master volume (saved), fullscreen, and the Interact key rebind (same as the pause menu's).
[RequireComponent(typeof(CanvasGroup))]
public class SettingsUI : MonoBehaviour
{
    private const string PLAYER_PREFS_MASTER_VOLUME = "MasterVolume";

    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button rebindButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI rebindButtonText;
    [SerializeField] private GameObject pressKeyOverlay;
    [SerializeField] private Button backButton;
    [SerializeField] private string rebindFormat = "Interact key: [{0}]";

    private CanvasGroup canvasGroup;
    private Action onCloseAction;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        volumeSlider.onValueChanged.AddListener(volume =>
        {
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(PLAYER_PREFS_MASTER_VOLUME, volume);
            PlayerPrefs.Save();
        });
        fullscreenToggle.onValueChanged.AddListener(isOn => Screen.fullScreen = isOn);
        rebindButton.onClick.AddListener(StartRebind);
        resetButton.onClick.AddListener(() => GameInput.Instance.ResetBindings());
        backButton.onClick.AddListener(() =>
        {
            Hide();
            onCloseAction();
        });
        Hide();
    }

    private void Start()
    {
        GameInput.Instance.OnBindingDisplayChanged += GameInput_OnBindingDisplayChanged;
        pressKeyOverlay.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnBindingDisplayChanged -= GameInput_OnBindingDisplayChanged;
    }

    private void GameInput_OnBindingDisplayChanged(object sender, EventArgs e) => RefreshRebindText();

    public void Show(Action onCloseAction)
    {
        this.onCloseAction = onCloseAction;
        volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        RefreshRebindText();
        canvasGroup.SetVisible(true);
        backButton.Select();
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

    // the saved volume applies from the first scene on, whether or not the panel is ever opened
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedVolume() => AudioListener.volume = PlayerPrefs.GetFloat(PLAYER_PREFS_MASTER_VOLUME, 1f);
}
