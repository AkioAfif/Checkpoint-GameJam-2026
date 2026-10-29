using System;
using TMPro;
using UnityEngine;

// Puzzle HUD (the note right of the board): which level this is, the energy left, Seed Embryos left, slides so far (against
// the par for the combo), the controls, a "seed ready" banner when solved, and a "try again" banner when the level is lost.
[RequireComponent(typeof(CanvasGroup))]
public class FabricatorUI : MonoBehaviour
{
    [SerializeField] private FabricatorController fabricator;
    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI embryoText;
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private CanvasGroup solvedBanner;
    [SerializeField] private UIPop solvedPop;
    [SerializeField] private CanvasGroup lostBanner;
    [SerializeField] private TextMeshProUGUI lostText;
    [SerializeField] private UIPop lostPop;
    [SerializeField] private string destroyedMessage = "The Tech Bug was destroyed! [R] Try again";
    [SerializeField] private string outOfEnergyMessage = "Out of energy! [R] Try again";
    [SerializeField] private string stuckMessage = "Stuck! [R] Try again";
    [Tooltip("The energy line turns this colour at this much energy or less.")]
    [SerializeField] private int lowEnergy = 2;
    [SerializeField] private Color lowEnergyColor = new Color(.78f, .22f, .2f);

    private CanvasGroup canvasGroup;
    private Color energyColor;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        energyColor = energyText.color;
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
        levelText.text = $"Level {fabricator.GetLevelNumber()}/{fabricator.GetLevelCount()}: {miniGame.GetPuzzleSO().puzzleName}";
        RefreshCounts();
    }

    private void MiniGame_OnBoardChanged(object sender, EventArgs e) => RefreshCounts();

    private void MiniGame_OnLost(object sender, EventArgs e)
    {
        switch (miniGame.GetLoseReason())
        {
            case FabricatorMiniGame.LoseReason.Destroyed: lostText.text = destroyedMessage; break;
            case FabricatorMiniGame.LoseReason.OutOfEnergy: lostText.text = outOfEnergyMessage; break;
            default: lostText.text = stuckMessage; break;
        }
        lostBanner.alpha = 1f;
        lostPop.Play(0f);
    }

    private void RefreshCounts()
    {
        FabricatorBoard board = miniGame.GetBoard();
        if (board.IsEnergyLimited())
        {
            energyText.text = $"Energy: {board.GetEnergy()} / {board.GetMaxEnergy()}";
            energyText.color = board.GetEnergy() <= lowEnergy ? lowEnergyColor : energyColor;
        }
        else
        {
            energyText.text = "Energy: unlimited";
            energyText.color = energyColor;
        }
        embryoText.text = $"Seed Embryos left: {miniGame.GetEmbryoCount()}";
        int parMoves = miniGame.GetParMoves();
        movesText.text = parMoves > 0 ? $"Slides: {miniGame.GetMoveCount()}  (combo at {parMoves} or fewer)" : $"Slides: {miniGame.GetMoveCount()}";
    }

    private void Hide() => canvasGroup.SetVisible(false);
}
