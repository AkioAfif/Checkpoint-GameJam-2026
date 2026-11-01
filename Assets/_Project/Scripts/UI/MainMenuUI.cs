using UnityEngine;
using UnityEngine.UI;

// Title screen: Start (the bedroom, Day 1 morning), Settings, Quit.
[RequireComponent(typeof(CanvasGroup))]
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private SettingsUI settingsUI;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Time.timeScale = 1f;
        startButton.onClick.AddListener(() => Loader.Load(Loader.Scene.HouseInterior));
        settingsButton.onClick.AddListener(() =>
        {
            Hide();
            settingsUI.Show(Show);
        });
        quitButton.onClick.AddListener(() => Application.Quit());
    }

    private void Start()
    {
        Show();
    }

    private void Show()
    {
        canvasGroup.SetVisible(true);
        startButton.Select();
    }

    private void Hide() => canvasGroup.SetVisible(false);
}
