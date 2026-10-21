using System.Collections.Generic;
using UnityEngine;

// What the days so far left behind in the world, plus the debt and savings. Static so it survives scene loads (like Loader),
// but it is not saved to disk.
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

    // paid out of today's sales, so that much less reaches the lender
    public static void Spend(long amount)
    {
        earnedToday -= amount;
        debt += amount;
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
    }
}
