using UnityEngine;

// All of Day 2's text, numbers and checklists, so the directors hold no strings.
[CreateAssetMenu()]
public class Day2SO : ScriptableObject
{
    [Header("Money (opened on the first look at the savings book)")]
    public long startingSavings = 1000;
    public long startingDebt = 48500000;
    public int daysUntilDue = 30;

    [Header("Savings book")]
    public string bookTitle;
    // {0} savings, {1} debt left, {2} days until due
    [TextArea(3, 8)] public string bookMorningFormat;
    // {0} savings, {1} earned today, {2} debt left, {3} days until due
    [TextArea(3, 8)] public string bookEveningFormat;
    public string bookCloseHint;

    [Header("Dialogue")]
    // after the savings book in the bedroom
    public DialogueSO morningDialogueSO;
    // after the first sale at the ICAN crate
    public DialogueSO soldDialogueSO;
    // Rigel, once the Sawit Lv.2 seedlings are in the ground
    public DialogueSO rigelSeesLv2DialogueSO;
    // the evening at the field, after the palms twitch
    public DialogueSO eveningDialogueSO;

    [Header("Checklists")]
    public TaskObjectiveSO[] morningObjectiveArray;
    public TaskObjectiveSO[] harvestObjectiveArray;
    public TaskObjectiveSO[] rigelObjectiveArray;
    public TaskObjectiveSO[] experimentObjectiveArray;
    public TaskObjectiveSO[] showRigelObjectiveArray;
    public TaskObjectiveSO[] nightObjectiveArray;

    [Header("Screens")]
    public string nightCaption;
}
