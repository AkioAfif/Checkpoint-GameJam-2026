using UnityEngine;

// All of Day 6's text, numbers and checklists ("The Offer"), so the director holds no strings.
[CreateAssetMenu()]
public class Day6SO : ScriptableObject
{
    [Header("Money")]
    [Tooltip("Days left on the deadline once the debt is paid off (thirty days, done in twenty-eight).")]
    public int daysUntilDue = 2;

    [Header("Dialogue")]
    // after the savings book in the bedroom
    public DialogueSO morningDialogueSO;
    // the wide shot over half the forest as palms
    public DialogueSO wideDialogueSO;
    // after the morning's sale: the first money that's the MC's own
    public DialogueSO profitDialogueSO;
    // the first bottle filled at the brown river, brushed off
    public DialogueSO murkyWaterDialogueSO;
    // the Distributor's congratulations, the Lv.3 pitch and his slip (a glaring line)
    public DialogueSO offerDialogueSO;
    // alone after the offer: he knew about the red
    public DialogueSO doubtDialogueSO;
    // talking to him again by the Fabricator
    public DialogueSO distributorRepeatDialogueSO;
    // the evidence: the old garden's soil, then what the villagers have seen (Ratna's is optional)
    public DialogueSO oldGardenDialogueSO;
    public DialogueSO dimasDialogueSO;
    public DialogueSO dartoDialogueSO;
    public DialogueSO ijahDialogueSO;
    public DialogueSO rigelDialogueSO;
    public DialogueSO ratnaDialogueSO;
    // the evening: the savings book, then the Distributor's bill by the Fabricator
    public DialogueSO eveningBookDialogueSO;
    public DialogueSO billDialogueSO;
    // the choice: his reply, then the MC's line at the plantation's edge that night
    public DialogueSO expandDialogueSO;
    public DialogueSO stopDialogueSO;
    public DialogueSO expandNightDialogueSO;
    public DialogueSO stopNightDialogueSO;

    [Header("Checklists")]
    public TaskObjectiveSO[] harvestObjectiveArray;
    public TaskObjectiveSO[] investigateObjectiveArray;
    public TaskObjectiveSO[] nightObjectiveArray;

    [Header("Screens")]
    [Tooltip("The bedroom's caption before the morning.")]
    public string skipCaption;
    public string eveningCaption;
    public string nightCaption;
    public string choiceTitle;
    public string expandChoiceText;
    public string stopChoiceText;
}
