using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
    // enum names must equal scene file names, and every scene must be in Build Profiles
    // new scenes go at the end: HouseDoor serializes this enum as an int
    public enum Scene { Level01_VillagePrototype, LoadingScene, HouseInterior_Stub, HouseInterior, Level01_Day2, Level01_Day3, FixScene_Day2, FixScene, FixScene_Day3, FixScene_Day4, FixScene_Day5, FixScene_Day6 }

    private static Scene targetScene;

    public static void Load(Scene targetScene)
    {
        Loader.targetScene = targetScene;
        Time.timeScale = 1f;
        SceneManager.LoadScene(Scene.LoadingScene.ToString());
    }

    public static void LoaderCallback() => SceneManager.LoadScene(targetScene.ToString());

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => targetScene = Scene.Level01_VillagePrototype;
}
