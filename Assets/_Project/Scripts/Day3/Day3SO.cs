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

    [Header("Dialogue")]
    // after the savings book in the bedroom
    public DialogueSO morningDialogueSO;
    // the new land, after the first sale: looking at the empty grass
    public DialogueSO landWishDialogueSO;
    // the grayscale flashback (the Distributor speaks)
    public DialogueSO flashbackDialogueSO;
    // back in the present, before eating
    public DialogueSO tryEatDialogueSO;
    // [Interact] to eat with no Lv.2 bunch left
    public DialogueSO noBunchDialogueSO;
    // Rigel, sprinting over (a shaking line)
    public DialogueSO rigelShoutDialogueSO;
    // after the glitch (Rigel speaks too)
    public DialogueSO buffDialogueSO;
    // Rigel, backing off toward his house
    public DialogueSO rigelPretendDialogueSO;
    // the CozyFarm map's forest field: after the buff, after the first tree falls, once they all have
    public DialogueSO treesIdeaDialogueSO;
    public DialogueSO firstTreeDialogueSO;
    public DialogueSO treesClearedDialogueSO;
    // the CozyFarm map's second bridge: looking at the river, and once it stands
    public DialogueSO bridgeIdeaDialogueSO;
    public DialogueSO bridgeDoneDialogueSO;
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
    public TaskObjectiveSO[] eatObjectiveArray;
    public TaskObjectiveSO[] treeObjectiveArray;
    public TaskObjectiveSO[] smashObjectiveArray;
    public TaskObjectiveSO[] bridgeObjectiveArray;
    public TaskObjectiveSO[] plantObjectiveArray;
    public TaskObjectiveSO[] nightObjectiveArray;

    [Header("Screens")]
    public string nightCaption;
}
