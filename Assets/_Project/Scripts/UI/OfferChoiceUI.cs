using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Day 6's choice at the Fabricator: take the Distributor's Lv.3 seeds or refuse them. Shown while the director waits for it;
// a click, or [Interact] on the selected button (A/D or the arrows move between them), answers.
[RequireComponent(typeof(CanvasGroup))]
public class OfferChoiceUI : MonoBehaviour
{
    [SerializeField] private Day6Director director;
    [SerializeField] private Day6SO day6SO;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button expandButton;
    [SerializeField] private Button stopButton;

    private CanvasGroup canvasGroup;
    private bool isShown;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        expandButton.onClick.AddListener(() => { director.Choose(true); });
        stopButton.onClick.AddListener(() => { director.Choose(false); });
        Hide();
    }

    private void Start()
    {
        titleText.text = day6SO.choiceTitle;
        expandButton.GetComponentInChildren<TextMeshProUGUI>().text = day6SO.expandChoiceText;
        stopButton.GetComponentInChildren<TextMeshProUGUI>().text = day6SO.stopChoiceText;
        director.OnStateChanged += Director_OnStateChanged;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
    }

    private void Director_OnStateChanged(object sender, Day6Director.OnStateChangedEventArgs e)
    {
        if (e.state == Day6Director.State.Decision) Show();
        else Hide();
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        // the press that closed the Distributor's last line must not also choose
        if (!isShown || GameManager.Instance.IsStateChangedThisFrame()) return;
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null && selected.TryGetComponent(out Button button)) button.onClick.Invoke();
    }

    private void Show()
    {
        isShown = true;
        canvasGroup.SetVisible(true);
        // refusing is selected first: nothing is taken by an accidental press
        stopButton.Select();
    }

    private void Hide()
    {
        isShown = false;
        canvasGroup.SetVisible(false);
    }
}
