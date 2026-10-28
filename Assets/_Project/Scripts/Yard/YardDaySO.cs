using UnityEngine;

// All of the yard day's text, items and checklists (Day 3 on the CozyFarm map: the MC widens the garden by hand).
[CreateAssetMenu()]
public class YardDaySO : ScriptableObject
{
    [Header("Bedroom (the morning)")]
    public DialogueSO morningDialogueSO;

    [Header("Morning")]
    // Rigel at his fence, about the palms that grew overnight
    public DialogueSO rigelMorningDialogueSO;
    // after the sale: the math doesn't work
    public DialogueSO mathDialogueSO;
    // the camera on the overgrown strip
    public DialogueSO wishDialogueSO;
    // pulling a weed bare-handed
    public DialogueSO bareHandsDialogueSO;

    [Header("Errands")]
    public DialogueSO ijahAskDialogueSO;
    // gives the sickle once the player has the flowers
    public DialogueSO ijahGiveDialogueSO;
    public DialogueSO ijahAfterDialogueSO;
    public int ijahFlowerCount = 3;
    public DialogueSO dartoHoeMissingDialogueSO;
    public DialogueSO dartoWhereDialogueSO;
    // gives the rope once the hoe is back
    public DialogueSO dartoRopeDialogueSO;
    public DialogueSO dimasAskDialogueSO;
    // swaps the hoe for a seed
    public DialogueSO dimasTradeDialogueSO;
    public DialogueSO dimasAfterDialogueSO;
    // gives the compost
    public DialogueSO ratnaLunchDialogueSO;

    [Header("Afternoon")]
    public DialogueSO stumpAloneDialogueSO;
    public DialogueSO rigelHelpDialogueSO;
    public DialogueSO rigelTalkDialogueSO;

    [Header("Evening")]
    public DialogueSO eveningDialogueSO;

    [Header("Checklists")]
    public TaskObjectiveSO[] harvestObjectiveArray;
    public TaskObjectiveSO[] clearObjectiveArray;
    public TaskObjectiveSO[] errandObjectiveArray;
    public TaskObjectiveSO[] lunchObjectiveArray;
    public TaskObjectiveSO[] workObjectiveArray;
    public TaskObjectiveSO[] stumpObjectiveArray;
    public TaskObjectiveSO[] plantObjectiveArray;
    public TaskObjectiveSO[] nightObjectiveArray;

    [Header("Played on its own (no bedroom before it)")]
    public long testStartingSavings = 1000;
    public long testStartingDebt = 46500000;
    public int testDaysUntilDue = 29;

    [Header("Screens")]
    public string afternoonCaption;
    public string nightCaption;
}
