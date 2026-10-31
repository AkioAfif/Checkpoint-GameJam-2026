using System;
using TMPro;
using UnityEngine;

// The savings book: savings, today's earnings, the debt left and the days until it is due, dismissed with [Interact].
// The day's director opens it and gets called back when it closes.
[RequireComponent(typeof(CanvasGroup))]
public class SavingsBookUI : MonoBehaviour
{
    [SerializeField] private Day2SO day2SO;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI continueText;
    [SerializeField] private UIPop cardPop;

    private CanvasGroup canvasGroup;
    private Action onClosed;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    private void Start()
    {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        // the same press that opened the book must not close it
        if (onClosed == null || GameManager.Instance.IsStateChangedThisFrame()) return;
        Action closed = onClosed;
        onClosed = null;
        Hide();
        closed();
    }

    public void Show(Action onClosed)
    {
        this.onClosed = onClosed;
        titleText.text = day2SO.bookTitle;
        string savings = Koin.Format(WorldState.GetSavings()), debt = Koin.Format(WorldState.GetDebt());
        long earned = WorldState.GetEarnedToday();
        bodyText.text = WorldState.HasAccounts() && WorldState.GetDebt() == 0L
            ? string.Format(day2SO.bookPaidFormat, savings)
            : earned > 0L
            ? string.Format(day2SO.bookEveningFormat, savings, Koin.Format(earned), debt, WorldState.GetDaysUntilDue())
            : string.Format(day2SO.bookMorningFormat, savings, debt, WorldState.GetDaysUntilDue());
        continueText.text = string.Format(day2SO.bookCloseHint, GameInput.Instance.GetInteractBindingDisplay());
        canvasGroup.SetVisible(true);
        cardPop.Play(0f);
    }

    private void Hide() => canvasGroup.SetVisible(false);
}
