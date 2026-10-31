using System;
using System.Collections.Generic;
using UnityEngine;

// What the days so far left behind in the world, plus the debt and savings and the player's items. Static so it survives
// scene loads (like Loader), but it is not saved to disk.
public static class WorldState
{
    // the debt went up or down (raised once per change)
    public static event EventHandler OnDebtChanged;

    // one plot of last night's garden; the day scenes share the map, so a plot is found again by where it stands
    public struct GardenPlot { public Vector3 position; public FarmPlot.State state; public CropDataSO cropDataSO; }

    // how close a plot must sit to a recorded position to count as the same plot (the day scenes share the field layout)
    private const float SAME_PLOT_DISTANCE = .1f;

    private static bool isDay1Complete;
    private static bool isDay2Complete;
    private static bool isDay3Complete;
    private static bool isDay4Complete;
    private static bool isDay5Complete;
    private static bool hasSkippedTime;
    private static bool isFabricatorPlaced;
    // every plot as the last day's door closed on it; the next morning's plots wake up from these (FarmPlot.Start)
    private static bool hasGarden;
    private static readonly List<GardenPlot> gardenPlotList = new List<GardenPlot>();
    // the money only exists from the first look at the savings book (Day 2's morning) on
    private static bool hasAccounts;
    private static long savings;
    private static long debt;
    private static int daysUntilDue;
    private static long earnedToday;
    // the last morning StartDay counted (0 until Day 2's: Day 1)
    private static int currentDay;
    // the player's items in the order first obtained: every scene has its own Inventory, which starts from these
    private static bool hasInventory;
    private static readonly List<ItemAmount> inventoryList = new List<ItemAmount>();
    private static readonly HashSet<ItemDataSO> discoveredItemSet = new HashSet<ItemDataSO>();
    // dialogue rewards already handed out, so no scene gives one twice
    private static readonly HashSet<DialogueSO> givenRewardSet = new HashSet<DialogueSO>();
    // Fabricator recipes made at least once (the latest per seed type), choosable in the recipe panel on later presses
    private static readonly List<FabricatorController.Recipe> unlockedRecipeList = new List<FabricatorController.Recipe>();
    // Fabricator levels solved so far, in FabricatorLevelListSO order (every press plays the next one)
    private static int solvedFabricatorLevelCount;

    public static void CompleteDay1()
    {
        isDay1Complete = true;
        isFabricatorPlaced = true;
    }

    // called as each day's door closes, so the next day starts from this garden
    public static void RecordGarden(FarmPlot[] farmPlotArray)
    {
        hasGarden = true;
        gardenPlotList.Clear();
        foreach (FarmPlot farmPlot in farmPlotArray)
            gardenPlotList.Add(new GardenPlot { position = farmPlot.transform.position, state = farmPlot.GetState(), cropDataSO = farmPlot.GetPlantedCropDataSO() });
    }

    public static void CompleteDay2() => isDay2Complete = true;
    public static void CompleteDay3() => isDay3Complete = true;
    public static void CompleteDay4() => isDay4Complete = true;
    public static void CompleteDay5() => isDay5Complete = true;

    // later calls keep the running totals
    public static void OpenAccounts(long startingSavings, long startingDebt, int startingDaysUntilDue)
    {
        if (hasAccounts) return;
        hasAccounts = true;
        savings = startingSavings;
        debt = startingDebt;
        daysUntilDue = startingDaysUntilDue;
        OnDebtChanged?.Invoke(null, EventArgs.Empty);
    }

    // a new morning (Day 2 on): one day less until the debt is due, and a fresh "earned today" (applied once per day)
    public static void StartDay(int day)
    {
        if (day <= currentDay) return;
        if (currentDay > 0)
        {
            daysUntilDue = System.Math.Max(0, daysUntilDue - (day - currentDay));
            earnedToday = 0L;
        }
        currentDay = day;
    }

    // what ICAN pays goes straight to the lender
    public static void Earn(long amount)
    {
        earnedToday += amount;
        debt = Math.Max(0L, debt - amount);
        OnDebtChanged?.Invoke(null, EventArgs.Empty);
    }

    // the weeks between Day 2 and Day 3: the harvests in between went to the lender (applied once)
    public static void SkipTime(long paidMeanwhile, int newDaysUntilDue)
    {
        if (hasSkippedTime) return;
        hasSkippedTime = true;
        debt = Math.Max(0L, debt - paidMeanwhile);
        OnDebtChanged?.Invoke(null, EventArgs.Empty);
        daysUntilDue = newDaysUntilDue;
        earnedToday = 0L;
    }

    // the first scene of a playthrough seeds the items; the scenes after it carry them on
    public static void StartInventory(ItemAmount[] startingItemArray)
    {
        if (hasInventory) return;
        hasInventory = true;
        foreach (ItemAmount itemAmount in startingItemArray)
        {
            discoveredItemSet.Add(itemAmount.itemDataSO);
            SetItemCount(itemAmount.itemDataSO, itemAmount.amount);
        }
    }

    public static void SetItemCount(ItemDataSO itemDataSO, int count)
    {
        for (int i = 0; i < inventoryList.Count; i++)
        {
            if (inventoryList[i].itemDataSO != itemDataSO) continue;
            inventoryList[i] = new ItemAmount { itemDataSO = itemDataSO, amount = count };
            return;
        }
        inventoryList.Add(new ItemAmount { itemDataSO = itemDataSO, amount = count });
    }

    // true the first time the item is ever collected
    public static bool TryDiscoverItem(ItemDataSO itemDataSO) => discoveredItemSet.Add(itemDataSO);

    // true if the reward wasn't given yet (it is recorded as given now)
    public static bool TryGiveReward(DialogueSO dialogueSO) => givenRewardSet.Add(dialogueSO);

    // a recipe making the same seed replaces the older one
    public static void UnlockRecipe(FabricatorController.Recipe recipe)
    {
        unlockedRecipeList.RemoveAll(unlocked => unlocked.output.itemDataSO == recipe.output.itemDataSO);
        unlockedRecipeList.Add(recipe);
    }

    public static bool IsDay1Complete() => isDay1Complete;
    public static bool IsDay2Complete() => isDay2Complete;
    public static bool IsDay3Complete() => isDay3Complete;
    public static bool IsDay4Complete() => isDay4Complete;
    public static bool IsDay5Complete() => isDay5Complete;
    public static bool IsFabricatorPlaced() => isFabricatorPlaced;
    public static bool HasAccounts() => hasAccounts;
    public static bool HasGarden() => hasGarden;

    // saplings in the ground when the last day ended
    public static int GetSaplingCount()
    {
        int count = 0;
        foreach (GardenPlot gardenPlot in gardenPlotList)
            if (gardenPlot.state == FarmPlot.State.Planted || gardenPlot.state == FarmPlot.State.Grown) count++;
        return count;
    }

    // matched on the ground plane: a later day may set the same bed a little higher or lower
    public static bool TryGetGardenPlot(Vector3 position, out GardenPlot gardenPlot)
    {
        foreach (GardenPlot recorded in gardenPlotList)
        {
            Vector2 offset = new Vector2(recorded.position.x - position.x, recorded.position.z - position.z);
            if (offset.magnitude >= SAME_PLOT_DISTANCE) continue;
            gardenPlot = recorded;
            return true;
        }
        gardenPlot = default;
        return false;
    }
    public static long GetSavings() => savings;
    public static long GetDebt() => debt;
    public static int GetDaysUntilDue() => daysUntilDue;
    public static long GetEarnedToday() => earnedToday;
    // Day 1 has no counted morning (StartDay starts at Day 2)
    public static int GetCurrentDay() => Mathf.Max(currentDay, 1);
    public static List<ItemAmount> GetInventoryList() => inventoryList;
    public static bool HasRewardBeenGiven(DialogueSO dialogueSO) => givenRewardSet.Contains(dialogueSO);
    public static List<FabricatorController.Recipe> GetUnlockedRecipeList() => unlockedRecipeList;
    public static int GetSolvedFabricatorLevelCount() => solvedFabricatorLevelCount;
    public static void SolveFabricatorLevel() => solvedFabricatorLevelCount++;

    // a fresh playthrough (the end screen's replay button)
    public static void Clear() => ResetStaticData();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData()
    {
        isDay1Complete = false;
        isDay2Complete = false;
        isDay3Complete = false;
        isDay4Complete = false;
        isDay5Complete = false;
        hasSkippedTime = false;
        isFabricatorPlaced = false;
        hasGarden = false;
        gardenPlotList.Clear();
        hasAccounts = false;
        savings = 0L;
        debt = 0L;
        daysUntilDue = 0;
        earnedToday = 0L;
        currentDay = 0;
        hasInventory = false;
        inventoryList.Clear();
        discoveredItemSet.Clear();
        givenRewardSet.Clear();
        unlockedRecipeList.Clear();
        solvedFabricatorLevelCount = 0;
    }

    // kept apart from ResetStaticData: Clear() (replay) must not drop the open scene's listeners
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticEvents() => OnDebtChanged = null;
}
