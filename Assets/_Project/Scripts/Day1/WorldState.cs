using UnityEngine;

// What Day 1 left behind in the world. Static so it survives scene loads (like Loader), but it is not saved to disk.
public static class WorldState
{
    private static bool isDay1Complete;
    private static bool isFabricatorPlaced;
    private static int saplingCount;

    public static void CompleteDay1(int plantedSaplingCount)
    {
        isDay1Complete = true;
        isFabricatorPlaced = true;
        saplingCount = plantedSaplingCount;
    }

    public static bool IsDay1Complete() => isDay1Complete;
    public static bool IsFabricatorPlaced() => isFabricatorPlaced;
    public static int GetSaplingCount() => saplingCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData()
    {
        isDay1Complete = false;
        isFabricatorPlaced = false;
        saplingCount = 0;
    }
}
