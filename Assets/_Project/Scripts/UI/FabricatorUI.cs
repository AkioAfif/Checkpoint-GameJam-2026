using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Puzzle HUD: one fill bar per liquid, the controls, and a "seed ready" banner when solved.
[RequireComponent(typeof(CanvasGroup))]
public class FabricatorUI : MonoBehaviour
{
    [SerializeField] private FabricatorController fabricator;
    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private Image[] barFillArray;
    [SerializeField] private TextMeshProUGUI[] barLabelArray;
    [SerializeField] private CanvasGroup solvedBanner;
    [SerializeField] private UIPop solvedPop;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    private void Start()
    {
        fabricator.OnStateChanged += Fabricator_OnStateChanged;
        for (int i = 0; i < barFillArray.Length; i++)
        {
            ItemDataSO liquid = miniGame.GetLiquidItemDataSO(i);
            barFillArray[i].color = liquid.liquidColor;
            barLabelArray[i].text = liquid.itemName;
        }
    }

    private void Update()
    {
        if (!miniGame.IsRunning()) return;
        for (int i = 0; i < barFillArray.Length; i++) barFillArray[i].fillAmount = miniGame.GetLiquidProgress(i);
    }

    private void Fabricator_OnStateChanged(object sender, FabricatorController.OnStateChangedEventArgs e)
    {
        bool isOpen = e.state == FabricatorController.State.Puzzle || e.state == FabricatorController.State.Solved;
        canvasGroup.SetVisible(isOpen);
        if (e.state == FabricatorController.State.Puzzle) ShowUsedBars();
        solvedBanner.alpha = e.state == FabricatorController.State.Solved ? 1f : 0f;
        if (e.state == FabricatorController.State.Solved) solvedPop.Play(0f);
    }

    // the tutorial puzzle has a single liquid: hide the bars of the unused ones
    private void ShowUsedBars()
    {
        for (int i = 0; i < barFillArray.Length; i++)
        {
            bool isUsed = miniGame.IsLiquidUsed(i);
            barLabelArray[i].gameObject.SetActive(isUsed);
            barFillArray[i].transform.parent.gameObject.SetActive(isUsed);
        }
    }

    private void Hide() => canvasGroup.SetVisible(false);
}
