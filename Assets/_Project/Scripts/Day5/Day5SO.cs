using UnityEngine;

// All of Day 5's text, numbers and checklists ("Half the Forest"), so the director holds no strings.
[CreateAssetMenu()]
public class Day5SO : ScriptableObject
{
    [Header("Played on its own")]
    [Tooltip("Debt when FixScene_Day5 is played without the days before it (Day 4 leaves about this).")]
    public long testStartingDebt = 28000000;
    public int testDaysUntilDue = 16;

    [Header("Dialogue")]
    // after the savings book in the bedroom
    public DialogueSO morningDialogueSO;
    // after the harvest: the camera on the land south of the pond
    public DialogueSO wishDialogueSO;
    // PlantationSequence: before the first bunch, after it (dig by hand), after the one bed rises, [Interact] with no bunch in
    // the bag, after the childhood TV show, on the jump marker, the plantation
    public DialogueSO eatOneDialogueSO;
    public DialogueSO digIdeaDialogueSO;
    public DialogueSO tinyGrowthDialogueSO;
    public DialogueSO noBunchDialogueSO;
    public DialogueSO hallucinationDialogueSO;
    public DialogueSO markerDialogueSO;
    public DialogueSO revealDialogueSO;
    // the empty plantation: too lazy to plant 48 beds by hand, seeds first, one more bite, a new power, the field planted
    public DialogueSO lazyDialogueSO;
    public DialogueSO eatMoreDialogueSO;
    public DialogueSO dashReadyDialogueSO;
    public DialogueSO moreDashesDialogueSO;
    public DialogueSO plantedDialogueSO;
    // the MC, turned away from the plants pulsing red at the plantation's edge, heads off to see the village
    public DialogueSO fieldDoneDialogueSO;
    // each villager has noticed something red; the MC talks it away
    public DialogueSO ijahDialogueSO;
    public DialogueSO ratnaDialogueSO;
    public DialogueSO dimasDialogueSO;
    // the evening, after the savings book
    public DialogueSO eveningMathDialogueSO;
    // the camera on the plantation's edge at night (the MC doesn't see it pulse)
    public DialogueSO nightDialogueSO;

    [Header("Checklists")]
    public TaskObjectiveSO[] harvestObjectiveArray;
    public TaskObjectiveSO[] eatOneObjectiveArray;
    public TaskObjectiveSO[] digObjectiveArray;
    public TaskObjectiveSO[] eatManyObjectiveArray;
    public TaskObjectiveSO[] markerObjectiveArray;
    // the empty plantation: press the seeds, then plant every bed
    public TaskObjectiveSO[] seedObjectiveArray;
    public TaskObjectiveSO[] plantObjectiveArray;
    // selling and the three villagers, in any order
    public TaskObjectiveSO[] errandObjectiveArray;
    public TaskObjectiveSO[] nightObjectiveArray;

    [Header("Screens")]
    public string nightCaption;
    [Tooltip("Over the crate until the plantation stands: the MC needs every bunch.")]
    public string crateLockedReason;

    [Header("Plough Dash")]
    [Tooltip("Seeds the plantation takes; with this many in the bag already, the Fabricator step is skipped.")]
    public int plantationSeedCount = 48;
    [Tooltip("Lv.2 bunches eaten (at most) for the dash power.")]
    public int eatMoreBunchCount = 2;
    public string dashHint;
    // {0} dashes left
    public string dashCountFormat;
}
