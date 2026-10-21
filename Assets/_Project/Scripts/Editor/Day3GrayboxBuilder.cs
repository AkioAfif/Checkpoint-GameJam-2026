using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static GrayboxBuild;

// Builds "Day 3: Room to Grow" as its own scene, Level01_Day3 (Tools > Day3 > Build Day 3 Scene). It copies Level01_Day2,
// keeps its village, crate, Rigel and resin stumps (Day2_Graybox), swaps Day 2's director for Day 3's (Day3), and adds
// Darto's strip between the field and the forest with its For Sale sign and hidden plots (Day3_World). Re-runnable;
// Level01_Day3 is regenerated each time, so run it after rebuilding Day 2. It also wires Day 3 into the HouseInterior bedroom.
public static class Day3GrayboxBuilder
{
    public const string SCENE_PATH = "Assets/_Project/Scenes/Level01_Day3.unity";
    private const string WORLD_ROOT = "Day3_World";
    private const string DIRECTOR_ROOT = "Day3";
    private const string DAY2_DIRECTOR_ROOT = "Day2";
    private const string BEDROOM_DIRECTOR_PATH = "BedroomDirector";

    // Darto's strip: between the field's north edge (z 9.8) and the forest edge (z 14.5)
    private static readonly Vector3 StripCentre = new Vector3(23f, 0f, 12.3f);
    private static readonly Vector3 StripSize = new Vector3(9f, .1f, 3f);
    private const float PLOT_SPACING = 2.25f;
    private const int PLOT_COUNT = 4;
    // the strip's front edge, clear of the house corner, facing the field
    private static readonly Vector3 SignPosition = new Vector3(20.6f, 0f, 10.4f);
    private static readonly Vector3 DartoPosition = new Vector3(19.4f, 0f, 10.2f);
    // the strip's west end, looking along it toward the trees
    private static readonly Vector3 EveningPosition = new Vector3(18.4f, 0f, 12.3f);

    [MenuItem("Tools/Day3/Build Day 3 Scene")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Day2GrayboxBuilder.SCENE_PATH) == null)
        {
            Debug.LogError($"{Day2GrayboxBuilder.SCENE_PATH} is missing: run Tools > Day2 > Build Day 2 Scene first.");
            return;
        }

        AssetDatabase.DeleteAsset(SCENE_PATH);
        AssetDatabase.CopyAsset(Day2GrayboxBuilder.SCENE_PATH, SCENE_PATH);
        EditorSceneManager.OpenScene(SCENE_PATH);
        LoadData();
        Day2GrayboxBuilder.RemoveRoots(DAY2_DIRECTOR_ROOT, WORLD_ROOT, DIRECTOR_ROOT);

        // the old field, before the strip's plots join the scene
        FarmPlot[] fieldPlots = Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include);
        Transform world = Root(WORLD_ROOT).transform;
        Transform director = Root(DIRECTOR_ROOT).transform;
        (GameObject plotsRoot, FarmPlot[] landPlots, GameObject sign) = BuildStrip(world);

        Npc darto = FindNpc("Npc_Darto");
        darto.transform.SetPositionAndRotation(DartoPosition, Quaternion.Euler(0f, 160f, 0f));
        // he waits by his sign all day
        SetArray(darto.GetComponent<NpcWander>(), "wanderPointArray", new Object[0]);

        SellCrate crate = Object.FindAnyObjectByType<SellCrate>(FindObjectsInactive.Include);
        SetArray(crate, "sellableItemDataSOArray", new Object[] { Day2DataBuilder.SawitBunch, Day3DataBuilder.SawitBunchLv2 });

        Transform points = Root("Points", director).transform;
        Transform eveningPoint = Day2GrayboxBuilder.Point(points, "EveningPoint", EveningPosition, 70f);

        Day3Director day3 = director.gameObject.AddComponent<Day3Director>();
        SetField(day3, "day3SO", Day3DataBuilder.Day3);
        SetField(day3, "screenFade", Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include));
        SetField(day3, "savingsBookUI", Object.FindAnyObjectByType<SavingsBookUI>(FindObjectsInactive.Include));
        SetField(day3, "fabricator", Object.FindAnyObjectByType<FabricatorController>(FindObjectsInactive.Include));
        SetRecipe(day3);
        SetField(day3, "lv2CropDataSO", Day2DataBuilder.SawitLv2);
        SetField(day3, "oldFieldCropDataSO", GameDataBuilder.SawitLv1);
        SetArray(day3, "fieldPlotArray", fieldPlots);
        SetArray(day3, "landPlotArray", landPlots);
        SetField(day3, "landPlotsRoot", plotsRoot);
        SetField(day3, "forSaleSign", sign);
        SetField(day3, "dartoNpc", darto);
        SetField(day3, "ratnaNpc", FindNpc("Npc_Ratna"));
        SetField(day3, "ijahNpc", FindNpc("Npc_Ijah"));
        SetField(day3, "rigelNpc", FindNpc("Npc_Rigel"));
        SetField(day3, "houseDoor", Object.FindAnyObjectByType<HouseDoor>(FindObjectsInactive.Include));
        SetField(day3, "morningPoint", GameObject.Find("Day2_Graybox/Points/MorningPoint").transform);
        SetField(day3, "eveningPoint", eveningPoint);

        // from the field, over the new strip into the dark pines
        CinemachineCamera forestCamera = Root("CM_Day3Forest", director).AddComponent<CinemachineCamera>();
        forestCamera.transform.position = new Vector3(20.5f, 5f, 5.5f);
        forestCamera.transform.LookAt(new Vector3(23.5f, 1.8f, 18f));
        LensSettings lens = LensSettings.Default;
        lens.FieldOfView = 45f;
        lens.FarClipPlane = 200f;
        forestCamera.Lens = lens;
        forestCamera.enabled = false;
        Day3CameraVisual cameraVisual = director.gameObject.AddComponent<Day3CameraVisual>();
        SetField(cameraVisual, "director", day3);
        SetField(cameraVisual, "forestCamera", forestCamera);

        NightVisual night = director.gameObject.AddComponent<NightVisual>();
        SetField(night, "day3Director", day3);
        SetField(night, "sun", GameObject.Find("Directional Light").GetComponent<Light>());
        SetField(night, "worldCamera", Camera.main);
        SetField(night, "doorLamp", GameObject.Find("Day2_Graybox").transform.Find("DoorLamp").gameObject);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Day2GrayboxBuilder.AddToBuildProfile(SCENE_PATH);
        WireBedroom();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(SCENE_PATH);
        Debug.Log($"Day 3 built into {SCENE_PATH}. Press Play in it to test Day 3 on its own.");
    }

    // ---------- world ----------

    // the soil strip, its fence (open toward the field and at the forest lane, x 19-22), the sign and the hidden plots
    private static (GameObject plotsRoot, FarmPlot[] landPlots, GameObject sign) BuildStrip(Transform world)
    {
        Transform strip = Root("DartoStrip", world).transform;
        Part(strip, "StripSoil", GrayboxMeshFactory.RoundedBox(StripSize, .05f), GrayboxPalette.Soil, new Vector3(StripCentre.x, -.04f, StripCentre.z));
        GameObject span = Day2GrayboxBuilder.Existing("Environment/FenceSpan", EnvironmentPrefabBuilder.FenceSpan);
        float west = StripCentre.x - StripSize.x * .5f, north = StripCentre.z + StripSize.z * .5f;
        for (float z = StripCentre.z - 1f; z < north; z += 2f) Spawn(span, strip, new Vector3(west - .3f, 0f, z), -90f);
        for (float x = 22.6f; x < StripCentre.x + StripSize.x * .5f; x += 2f) Spawn(span, strip, new Vector3(x, 0f, north + .2f), 0f);
        GameObject sign = Spawn(Day3PrefabBuilder.ForSaleSign(), strip, SignPosition, 0f);

        GameObject plotsRoot = Root("LandPlots", strip);
        GameObject plot = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/Interactables/FarmPlot.prefab");
        FarmPlot[] plots = new FarmPlot[PLOT_COUNT];
        for (int i = 0; i < PLOT_COUNT; i++)
        {
            float x = StripCentre.x + (i - (PLOT_COUNT - 1) * .5f) * PLOT_SPACING;
            plots[i] = Spawn(plot, plotsRoot.transform, new Vector3(x, 0f, StripCentre.z), 0f).GetComponent<FarmPlot>();
            // bare soil, ready for a seed
            SerializedObject so = new SerializedObject(plots[i]);
            so.FindProperty("startState").enumValueIndex = (int)FarmPlot.State.Empty;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return (plotsRoot, plots, sign);
    }

    // ---------- data ----------

    private static void LoadData()
    {
        GameDataBuilder.LoadAll();
        Day2DataBuilder.LoadAll();
        Day3DataBuilder.Build();
    }

    // every press: 2 resin + water + flower make 4 Lv.2 seeds (one press fills the strip) and hand the bottle back
    private static void SetRecipe(Day3Director day3)
    {
        SerializedObject so = new SerializedObject(day3);
        SerializedProperty recipes = so.FindProperty("recipeArray");
        recipes.arraySize = 1;
        SerializedProperty recipe = recipes.GetArrayElementAtIndex(0);
        (ItemDataSO item, int amount)[] inputs = { (Day2DataBuilder.Resin, 2), (GameDataBuilder.Water, 1), (GameDataBuilder.Flower, 1) };
        SerializedProperty inputArray = recipe.FindPropertyRelative("inputArray");
        inputArray.arraySize = inputs.Length;
        for (int i = 0; i < inputs.Length; i++)
        {
            inputArray.GetArrayElementAtIndex(i).FindPropertyRelative("itemDataSO").objectReferenceValue = inputs[i].item;
            inputArray.GetArrayElementAtIndex(i).FindPropertyRelative("amount").intValue = inputs[i].amount;
        }
        recipe.FindPropertyRelative("output.itemDataSO").objectReferenceValue = Day2DataBuilder.SawitSeedLv2;
        recipe.FindPropertyRelative("output.amount").intValue = PLOT_COUNT;
        recipe.FindPropertyRelative("returned.itemDataSO").objectReferenceValue = GameDataBuilder.EmptyBottle;
        recipe.FindPropertyRelative("returned.amount").intValue = 1;
        recipe.FindPropertyRelative("puzzleSO").objectReferenceValue = GameDataBuilder.PuzzleSplit;
        recipe.FindPropertyRelative("blockedReason").stringValue = "Needs 2 resin, water and a flower";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // the bedroom's director needs Day 3's morning text and the time skip
    private static void WireBedroom()
    {
        EditorSceneManager.OpenScene(HouseInteriorBuilder.SCENE_PATH);
        BedroomDirector bedroom = GameObject.Find(BEDROOM_DIRECTOR_PATH).GetComponent<BedroomDirector>();
        SetField(bedroom, "day3SO", Day3DataBuilder.Day3);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static Npc FindNpc(string name) => Object.FindObjectsByType<Npc>(FindObjectsInactive.Include).First(n => n.name == name);
}
