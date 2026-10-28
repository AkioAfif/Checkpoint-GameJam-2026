using System;
using TMPro;
using UnityEngine;

// Puzzle HUD: Seed Embryos left, slides so far (against the par for the combo), the controls, a "seed ready" banner when
// solved, and a "try again" banner when the bug is lost in thorns or stuck.
[RequireComponent(typeof(CanvasGroup))]
public class FabricatorUI : MonoBehaviour
{
    [SerializeField] private FabricatorController fabricator;
    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private TextMeshProUGUI embryoText;
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private CanvasGroup solvedBanner;
    [SerializeField] private UIPop solvedPop;
    [SerializeField] private CanvasGroup lostBanner;
    [SerializeField] private TextMeshProUGUI lostText;
    [SerializeField] private UIPop lostPop;
    [SerializeField] private string stuckMessage = "Stuck! [R] Try again";
    [SerializeField] private string thornsMessage = "Caught in the thorns! [R] Try again";

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    private void Start()
    {
        fabricator.OnStateChanged += Fabricator_OnStateChanged;
        miniGame.OnBuilt += MiniGame_OnBuilt;
        miniGame.OnBoardChanged += MiniGame_OnBoardChanged;
        miniGame.OnLost += MiniGame_OnLost;
    }

    private void Fabricator_OnStateChanged(object sender, FabricatorController.OnStateChangedEventArgs e)
    {
        bool isOpen = e.state == FabricatorController.State.Puzzle || e.state == FabricatorController.State.Solved;
        canvasGroup.SetVisible(isOpen);
        solvedBanner.alpha = e.state == FabricatorController.State.Solved ? 1f : 0f;
        if (e.state == FabricatorController.State.Solved) solvedPop.Play(0f);
    }

    private void MiniGame_OnBuilt(object sender, EventArgs e)
    {
        lostBanner.alpha = 0f;
        RefreshCounts();
    }

    private void MiniGame_OnBoardChanged(object sender, EventArgs e) => RefreshCounts();

    private void MiniGame_OnLost(object sender, EventArgs e)
    {
        lostText.text = miniGame.IsLostInThorns() ? thornsMessage : stuckMessage;
        lostBanner.alpha = 1f;
        lostPop.Play(0f);
    }

    private void RefreshCounts()
    {
        embryoText.text = $"Seed Embryos left: {miniGame.GetEmbryoCount()}";
        int parMoves = miniGame.GetParMoves();
        movesText.text = parMoves > 0 ? $"Slides: {miniGame.GetMoveCount()}  (combo at {parMoves} or fewer)" : $"Slides: {miniGame.GetMoveCount()}";
    }

    private void Hide() => canvasGroup.SetVisible(false);
}
