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
    // the Distributor's congratulations, the Lv.3 pitch and his slip (a glaring line)
    public DialogueSO offerDialogueSO;
    // talking to him again by the Fabricator
    public DialogueSO distributorRepeatDialogueSO;
    // the villagers, quieter than before
    public DialogueSO ijahDialogueSO;
    public DialogueSO ratnaDialogueSO;
    public DialogueSO dimasDialogueSO;
    // the plantation's edge at night
    public DialogueSO nightDialogueSO;

    [Header("Checklists")]
    public TaskObjectiveSO[] errandObjectiveArray;
    public TaskObjectiveSO[] nightObjectiveArray;

    [Header("Screens")]
    [Tooltip("The bedroom's caption before the morning.")]
    public string skipCaption;
    public string nightCaption;
}
