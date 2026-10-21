using UnityEngine;

// All of Day 3's text, numbers and checklists, so the directors hold no strings.
[CreateAssetMenu()]
public class Day3SO : ScriptableObject
{
    [Header("Time skip (applied on Day 3's first morning)")]
    public string skipCaption;
    // what the harvests between Day 2 and Day 3 paid to the lender
    public long paidDuringSkip = 9000000;
    public int daysUntilDue = 16;
    [Tooltip("Debt when Level01_Day3 is played without the days before it.")]
    public long testStartingDebt = 46000000;

    [Header("Land")]
    public long landPrice = 4000000;

    [Header("Dialogue")]
    // after the savings book in the bedroom
    public DialogueSO morningDialogueSO;
    // after the first sale at the ICAN crate
    public DialogueSO soldDialogueSO;
    // Darto at the For Sale sign; the purchase happens when it ends
    public DialogueSO dartoSellsLandDialogueSO;
    // optional talks while the player works
    public DialogueSO ratnaDialogueSO;
    public DialogueSO ijahDialogueSO;
    public DialogueSO rigelDialogueSO;
    // the evening, after the savings book
    public DialogueSO eveningMathDialogueSO;
    // the camera on the trees
    public DialogueSO forestHookDialogueSO;

    [Header("Checklists")]
    public TaskObjectiveSO[] harvestObjectiveArray;
    public TaskObjectiveSO[] buyLandObjectiveArray;
    public TaskObjectiveSO[] plantObjectiveArray;
    public TaskObjectiveSO[] nightObjectiveArray;

    [Header("Screens")]
    public string nightCaption;
}
