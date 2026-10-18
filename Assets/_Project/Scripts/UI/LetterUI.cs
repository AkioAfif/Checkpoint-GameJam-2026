using System;
using TMPro;
using UnityEngine;

// The debt letter: paper card with the massive debt, dismissed with [Interact].
[RequireComponent(typeof(CanvasGroup))]
public class LetterUI : MonoBehaviour
{
    [SerializeField] private BedsideLetter bedsideLetter;
    [SerializeField] private Day1SO day1SO;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI debtText;
    [SerializeField] private TextMeshProUGUI continueText;
    [SerializeField] private UIPop cardPop;

    private CanvasGroup canvasGroup;
    private bool isShowing;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    private void Start()
    {
        bedsideLetter.OnOpened += BedsideLetter_OnOpened;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
    }

    private void BedsideLetter_OnOpened(object sender, EventArgs e)
    {
        titleText.text = day1SO.letterTitle;
        bodyText.text = day1SO.letterBody;
        debtText.text = day1SO.debtAmount;
        continueText.text = string.Format(day1SO.letterCloseHint, GameInput.Instance.GetInteractBindingDisplay());
        isShowing = true;
        canvasGroup.SetVisible(true);
        cardPop.Play(0f);
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        // the same press that opened the letter must not close it
        if (!isShowing || GameManager.Instance.IsStateChangedThisFrame()) return;
        isShowing = false;
        Hide();
        bedsideLetter.Close();
    }

    private void Hide() => canvasGroup.SetVisible(false);
}
