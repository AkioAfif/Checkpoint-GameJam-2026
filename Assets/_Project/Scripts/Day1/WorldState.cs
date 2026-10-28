using System.Collections.Generic;
using UnityEngine;

// What the days so far left behind in the world, plus the debt and savings and the player's items. Static so it survives
// scene loads (like Loader), but it is not saved to disk.
public static class WorldState
{
    // how close a plot must sit to a recorded position to count as the same plot (the day scenes share the field layout)
    private const float SAME_PLOT_DISTANCE = .1f;

    private static bool isDay1Complete;
    private static bool isDay2Complete;
    private static bool isDay3Complete;
    private static bool hasSkippedTime;
    private static bool isFabricatorPlaced;
    // where Day 1's saplings stand; they are ripe the next morning
    private static readonly List<Vector3> plantedPositionList = new List<Vector3>();
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

    public static void CompleteDay1(IEnumerable<Vector3> plantedPositions)
    {
        isDay1Complete = true;
        isFabricatorPlaced = true;
        plantedPositionList.Clear();
        plantedPositionList.AddRange(plantedPositions);
    }

    public static void CompleteDay2() => isDay2Complete = true;
    public static void CompleteDay3() => isDay3Complete = true;

    // later calls keep the running totals
    public static void OpenAccounts(long startingSavings, long startingDebt, int startingDaysUntilDue)
    {
        if (hasAccounts) return;
        hasAccounts = true;
        savings = startingSavings;
        debt = startingDebt;
        daysUntilDue = startingDaysUntilDue;
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
        debt = System.Math.Max(0L, debt - amount);
    }

    // the weeks between Day 2 and Day 3: the harvests in between went to the lender (applied once)
    public static void SkipTime(long paidMeanwhile, int newDaysUntilDue)
    {
        if (hasSkippedTime) return;
        hasSkippedTime = true;
        debt = System.Math.Max(0L, debt - paidMeanwhile);
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
    public static bool IsFabricatorPlaced() => isFabricatorPlaced;
    public static int GetSaplingCount() => plantedPositionList.Count;

    public static bool WasPlantedOnDay1(Vector3 position)
    {
        foreach (Vector3 planted in plantedPositionList)
            if (Vector3.Distance(planted, position) < SAME_PLOT_DISTANCE) return true;
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

    // a fresh playthrough (the end screen's replay button)
    public static void Clear() => ResetStaticData();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData()
    {
        isDay1Complete = false;
        isDay2Complete = false;
        isDay3Complete = false;
        hasSkippedTime = false;
        isFabricatorPlaced = false;
        plantedPositionList.Clear();
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
    }
}
