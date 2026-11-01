using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Opens when the player uses the Fabricator: which recipe (once more than one is unlocked), the materials one batch needs,
// the seeds it makes, and how many batches to run at once.
[RequireComponent(typeof(CanvasGroup))]
public class FabricatorRecipeUI : MonoBehaviour
{
    [SerializeField] private FabricatorController fabricator;
    [Tooltip("The recipe switcher row; hidden while only one recipe is unlocked.")]
    [SerializeField] private GameObject recipeSelector;
    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private Button previousRecipeButton;
    [SerializeField] private Button nextRecipeButton;
    [SerializeField] private Transform inputContainer;
    [SerializeField] private FabricatorRecipeSingleUI inputRowTemplate;
    [SerializeField] private Image outputIconImage;
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private TextMeshProUGUI batchCountText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button makeButton;
    [SerializeField] private Button cancelButton;

    private readonly List<FabricatorRecipeSingleUI> rowList = new List<FabricatorRecipeSingleUI>();
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        inputRowTemplate.gameObject.SetActive(false);
        previousRecipeButton.onClick.AddListener(() => { fabricator.ChangeRecipe(-1); });
        nextRecipeButton.onClick.AddListener(() => { fabricator.ChangeRecipe(1); });
        minusButton.onClick.AddListener(() => { fabricator.ChangeBatchCount(-1); });
        plusButton.onClick.AddListener(() => { fabricator.ChangeBatchCount(1); });
        makeButton.onClick.AddListener(() => { fabricator.ConfirmBatch(); });
        cancelButton.onClick.AddListener(() => { fabricator.CancelBatch(); });
        Hide();
    }

    private void Start()
    {
        fabricator.OnStateChanged += Fabricator_OnStateChanged;
        fabricator.OnChoiceChanged += Fabricator_OnChoiceChanged;
    }

    private void Fabricator_OnStateChanged(object sender, FabricatorController.OnStateChangedEventArgs e)
    {
        if (e.state == FabricatorController.State.Choosing) Show();
        else Hide();
    }

    private void Fabricator_OnChoiceChanged(object sender, EventArgs e) => UpdateVisual();

    private void UpdateVisual()
    {
        foreach (FabricatorRecipeSingleUI row in rowList) Destroy(row.gameObject);
        rowList.Clear();

        int batchCount = fabricator.GetBatchCount();
        foreach (ItemAmount input in fabricator.GetInputArray())
        {
            FabricatorRecipeSingleUI row = Instantiate(inputRowTemplate, inputContainer);
            row.gameObject.SetActive(true);
            row.SetItem(input.itemDataSO, input.amount * batchCount, Inventory.Instance.GetCount(input.itemDataSO));
            rowList.Add(row);
        }

        ItemAmount output = fabricator.GetOutput();
        int choiceCount = fabricator.GetChoiceCount();
        recipeSelector.SetActive(choiceCount > 1);
        recipeNameText.text = $"{output.itemDataSO.itemName}  ({fabricator.GetChoiceIndex() + 1}/{choiceCount})";
        outputIconImage.sprite = output.itemDataSO.icon;
        outputIconImage.enabled = output.itemDataSO.icon != null;
        outputText.text = $"{output.itemDataSO.itemName} x{output.amount * batchCount}";
        batchCountText.text = batchCount.ToString();
        minusButton.interactable = batchCount > 1;
        plusButton.interactable = batchCount < fabricator.GetMaxBatchCount();
        // the rows above show what is missing; Make only works once the bag covers the batches
        makeButton.interactable = fabricator.CanAffordBatches();
    }

    private void Show()
    {
        UpdateVisual();
        canvasGroup.SetVisible(true);
        if (makeButton.interactable) makeButton.Select();
        else cancelButton.Select();
    }

    private void Hide() => canvasGroup.SetVisible(false);
}
