using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static GrayboxBuild;

// Builds "Day 2: The First Harvest" as its own scene, Level01_Day2 (Tools > Day2 > Build Day 2 Scene, with Level01 open).
// It adds Rigel's farm and the forest edge (Day2_World) to Level01, so both days share one village, then copies Level01 to
// Level01_Day2, strips Day 1's story out of the copy and adds the crate, Rigel and the resin stumps (Day2_Graybox) and the
// director with its camera and night (Day2). Re-runnable; Level01_Day2 is regenerated each time, so hand-tune the village in
// Level01. Run it after Tools > Day1 > Build Graybox: it reuses Day 1's fade and door lamp.
// HouseInterior (Tools > Day2 > Build Bedroom): the savings book on the bedside table and its page.
public static class Day2GrayboxBuilder
{
    public const string SCENE_PATH = "Assets/_Project/Scenes/Level01_Day2.unity";
    private const string WORLD_ROOT = "Day2_World";
    private const string GRAYBOX_ROOT = "Day2_Graybox";
    private const string DIRECTOR_ROOT = "Day2";
    // the one-scene setup before Level01_Day2 existed; removed from Level01 when found
    private const string OLD_SWITCH_ROOT = "DaySwitch";
    private const string BEDROOM_DIRECTOR_PATH = "BedroomDirector";

    // south-east of the player's field, across a fence: Rigel's house faces north onto his rice
    private static readonly Vector3 RigelHousePosition = new Vector3(25f, 0f, -16f);
    private static readonly Vector3 RigelFieldCentre = new Vector3(22f, 0f, -9f);
    private const float RIGEL_FENCE_Z = -5.6f;
    // on the player's side of his fence, looking over at the sawit
    private static readonly Vector3 RigelStand = new Vector3(19.4f, 0f, -4.4f);
    // north-east corner of the map, behind the player's field
    private static readonly Vector3 ForestMin = new Vector3(16.5f, 0f, 14.5f);
    private static readonly Vector3 ForestMax = new Vector3(28.5f, 0f, 26f);
    private static readonly Vector3[] ResinPositions = { new Vector3(20.5f, 0f, 18f), new Vector3(26f, 0f, 20.5f), new Vector3(22f, 0f, 23.5f) };
    // west of the Fabricator, by the path to the house
    private static readonly Vector3 CratePosition = Level01SceneBuilder.FabricatorPosition + new Vector3(-1.9f, 0f, -.6f);
    private static readonly Vector3 MorningPosition = Level01SceneBuilder.PlayerHousePosition + new Vector3(0f, 0f, -4.4f);
    // the path's end at the field's west edge, facing the palms
    private static readonly Vector3 EveningPosition = new Vector3(17.6f, 0f, 4.2f);

    [MenuItem("Tools/Day2/Build Day 2 Scene")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().path != Level01SceneBuilder.SCENE_PATH)
        {
            Debug.LogError($"Open {Level01SceneBuilder.SCENE_PATH} first.");
            return;
        }
        if (GameObject.Find("Day1_Graybox") == null || GameObject.Find("Day1") == null)
        {
            Debug.LogError("Day 1 is not in the scene: run Tools > Day1 > Build Graybox first.");
            return;
        }

        LoadData();
        // Level01 (Day 1): the shared village gains Rigel's farm and the forest edge
        RemoveRoots(WORLD_ROOT, GRAYBOX_ROOT, DIRECTOR_ROOT, OLD_SWITCH_ROOT);
        Day2UiBuilder.RemovePrevious();
        Transform world = Root(WORLD_ROOT).transform;
        BuildRigelFarm(world);
        BuildForestEdge(world);
        foreach (Transform t in world.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        // Level01_Day2: the same village, the morning after
        AssetDatabase.DeleteAsset(SCENE_PATH);
        AssetDatabase.CopyAsset(Level01SceneBuilder.SCENE_PATH, SCENE_PATH);
        EditorSceneManager.OpenScene(SCENE_PATH);
        Transform graybox = Root(GRAYBOX_ROOT).transform;
        Transform director = Root(DIRECTOR_ROOT).transform;
        GameObject doorLamp = StripDay1(graybox);
        Spawn(Day2PrefabBuilder.SellCrate(Day2DataBuilder.SawitBunch), graybox, CratePosition, 0f);
        GameObject resinNode = Day2PrefabBuilder.ResinNode(Day2DataBuilder.Resin, Existing("VFX/CollectBurst", VfxBuilder.CollectBurst).transform);
        Transform resin = Root("ResinNodes", graybox).transform;
        foreach (Vector3 position in ResinPositions) Spawn(resinNode, resin, position, 0f);
        Npc rigel = Spawn(Day2PrefabBuilder.Rigel(Day2DataBuilder.Rigel), graybox, RigelStand, 20f).GetComponent<Npc>();
        Transform points = Root("Points", graybox).transform;
        Transform morningPoint = Point(points, "MorningPoint", MorningPosition, 180f);
        Transform eveningPoint = Point(points, "EveningPoint", EveningPosition, 80f);

        Transform hud = Object.FindAnyObjectByType<HudUI>(FindObjectsInactive.Include).transform.parent;
        SavingsBookUI savingsBookUI = Day2UiBuilder.BuildSavingsBook(hud, Day2DataBuilder.Day2);
        Day2UiBuilder.BuildMoneyPopup(hud);

        FabricatorController fabricator = Object.FindAnyObjectByType<FabricatorController>(FindObjectsInactive.Include);
        FarmPlot[] plots = Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include)
            .OrderBy(p => Mathf.Round(p.transform.position.z * 10f)).ThenBy(p => p.transform.position.x).ToArray();

        Day2Director day2 = director.gameObject.AddComponent<Day2Director>();
        SetField(day2, "day2SO", Day2DataBuilder.Day2);
        SetField(day2, "screenFade", Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include));
        SetField(day2, "savingsBookUI", savingsBookUI);
        SetField(day2, "fabricator", fabricator);
        SetLv2Recipe(day2);
        SetField(day2, "ripeCropDataSO", GameDataBuilder.SawitLv1);
        SetField(day2, "newCropDataSO", Day2DataBuilder.SawitLv2);
        SetArray(day2, "farmPlotArray", plots);
        SetField(day2, "rigelNpc", rigel);
        SetField(day2, "houseDoor", Object.FindAnyObjectByType<HouseDoor>(FindObjectsInactive.Include));
        SetField(day2, "morningPoint", morningPoint);
        SetField(day2, "eveningPoint", eveningPoint);

        // over the house-side path, looking east across the field
        CinemachineCamera farmCamera = Root("CM_Day2Farm", director).AddComponent<CinemachineCamera>();
        farmCamera.transform.position = new Vector3(14.5f, 6.2f, -3.5f);
        farmCamera.transform.LookAt(Level01SceneBuilder.FarmCentre + new Vector3(0f, .9f, 0f));
        LensSettings lens = LensSettings.Default;
        lens.FieldOfView = 42f;
        lens.FarClipPlane = 200f;
        farmCamera.Lens = lens;
        farmCamera.enabled = false;
        Day2CameraVisual cameraVisual = director.gameObject.AddComponent<Day2CameraVisual>();
        SetField(cameraVisual, "director", day2);
        SetField(cameraVisual, "farmCamera", farmCamera);

        NightVisual night = director.gameObject.AddComponent<NightVisual>();
        SetField(night, "day2Director", day2);
        SetField(night, "sun", GameObject.Find("Directional Light").GetComponent<Light>());
        SetField(night, "worldCamera", Camera.main);
        SetField(night, "doorLamp", doorLamp);

        MutationPulseVisual pulse = director.gameObject.AddComponent<MutationPulseVisual>();
        SetField(pulse, "director", day2);
        SetField(pulse, "mutantCropDataSO", Day2DataBuilder.SawitLv2);
        SetArray(pulse, "farmPlotArray", plots);
        SetField(pulse, "pulseMaterial", GrayboxPalette.Unlit("MutantPulse", new Color(.62f, .05f, .1f)));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AddToBuildProfile(SCENE_PATH);
        AssetDatabase.SaveAssets();
        Debug.Log($"Day 2 built into {SCENE_PATH}. Press Play in it to test Day 2 on its own (3 stand-in ripe palms).");
    }

    // Day 1's story leaves the copy: the truck, Sari, the Distributor and Day 1's director, cameras and night.
    // Returns the door lamp, kept for Day 2's night.
    private static GameObject StripDay1(Transform graybox)
    {
        GameObject day1Graybox = GameObject.Find("Day1_Graybox");
        // the Fabricator stands where the Distributor set it down yesterday
        FabricatorController fabricator = Object.FindAnyObjectByType<FabricatorController>(FindObjectsInactive.Include);
        Transform placePoint = day1Graybox.transform.Find("FabricatorPlacePoint");
        float placedScale = new SerializedObject(fabricator.GetComponent<FabricatorDelivery>()).FindProperty("placedScale").floatValue;
        fabricator.transform.SetPositionAndRotation(placePoint.position, placePoint.rotation);
        fabricator.transform.localScale = Vector3.one * placedScale;

        GameObject doorLamp = day1Graybox.transform.Find("DoorLamp").gameObject;
        doorLamp.transform.SetParent(graybox, true);
        Object.DestroyImmediate(day1Graybox);
        Object.DestroyImmediate(GameObject.Find("Day1"));
        Object.DestroyImmediate(Object.FindObjectsByType<Npc>(FindObjectsInactive.Include).First(n => n.name == "Npc_Distributor").gameObject);
        return doorLamp;
    }

    public static void AddToBuildProfile(string scenePath)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Any(s => s.path == scenePath)) return;
        EditorBuildSettings.scenes = scenes.Append(new EditorBuildSettingsScene(scenePath, true)).ToArray();
    }

    [MenuItem("Tools/Day2/Build Bedroom")]
    public static void BuildBedroom()
    {
        if (EditorSceneManager.GetActiveScene().path != HouseInteriorBuilder.SCENE_PATH)
        {
            Debug.LogError($"Open {HouseInteriorBuilder.SCENE_PATH} first.");
            return;
        }
        LoadData();
        foreach (BedsideLetter old in Object.FindObjectsByType<BedsideLetter>(FindObjectsInactive.Include))
            if (old.name == "SavingsBook") Object.DestroyImmediate(old.gameObject);
        Day2UiBuilder.RemovePrevious();

        BedsideLetter letter = Object.FindAnyObjectByType<BedsideLetter>(FindObjectsInactive.Include);
        // where the letter lay yesterday; only one of them is out each morning
        GameObject book = Spawn(Day2PrefabBuilder.SavingsBook(), letter.transform.parent, letter.transform.position, 0f);
        book.name = "SavingsBook";
        Transform hud = Object.FindAnyObjectByType<HudUI>(FindObjectsInactive.Include).transform.parent;
        SavingsBookUI savingsBookUI = Day2UiBuilder.BuildSavingsBook(hud, Day2DataBuilder.Day2);

        BedroomDirector director = GameObject.Find(BEDROOM_DIRECTOR_PATH).GetComponent<BedroomDirector>();
        SetField(director, "day2SO", Day2DataBuilder.Day2);
        SetField(director, "savingsBook", book.GetComponent<BedsideLetter>());
        SetField(director, "savingsBookUI", savingsBookUI);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Day 2 bedroom built: the savings book replaces the letter once Day 1 is complete.");
    }

    // ---------- data ----------

    private static void LoadData()
    {
        GameDataBuilder.LoadAll();
        Day2DataBuilder.Build();
    }

    // the baker renders in a throwaway scene, so it reopens the scene that was open
    [MenuItem("Tools/Day2/Bake Item Icons")]
    public static void BakeIcons()
    {
        string scenePath = EditorSceneManager.GetActiveScene().path;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        LoadData();
        Day2DataBuilder.BakeIcons();
        if (scenePath.Length > 0) EditorSceneManager.OpenScene(scenePath);
    }

    // the shared environment/VFX prefabs as they are (re-saving them would reset every instance already in the scene)
    public static GameObject Existing(string pathInPrefabs, System.Func<GameObject> build)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/{pathInPrefabs}.prefab");
        return prefab != null ? prefab : build();
    }

    // press 1+: 2 resin + water + flower + plant make 2 Lv.2 seeds and hand the bottle back
    private static void SetLv2Recipe(Day2Director day2)
    {
        SerializedObject so = new SerializedObject(day2);
        SerializedProperty recipes = so.FindProperty("recipeArray");
        recipes.arraySize = 1;
        SerializedProperty recipe = recipes.GetArrayElementAtIndex(0);
        (ItemDataSO item, int amount)[] inputs = { (Day2DataBuilder.Resin, 2), (GameDataBuilder.Water, 1), (GameDataBuilder.Flower, 1), (GameDataBuilder.Crop, 1) };
        SerializedProperty inputArray = recipe.FindPropertyRelative("inputArray");
        inputArray.arraySize = inputs.Length;
        for (int i = 0; i < inputs.Length; i++)
        {
            inputArray.GetArrayElementAtIndex(i).FindPropertyRelative("itemDataSO").objectReferenceValue = inputs[i].item;
            inputArray.GetArrayElementAtIndex(i).FindPropertyRelative("amount").intValue = inputs[i].amount;
        }
        recipe.FindPropertyRelative("output.itemDataSO").objectReferenceValue = Day2DataBuilder.SawitSeedLv2;
        recipe.FindPropertyRelative("output.amount").intValue = 2;
        recipe.FindPropertyRelative("returned.itemDataSO").objectReferenceValue = GameDataBuilder.EmptyBottle;
        recipe.FindPropertyRelative("returned.amount").intValue = 1;
        recipe.FindPropertyRelative("puzzleSO").objectReferenceValue = GameDataBuilder.PuzzleSplit;
        recipe.FindPropertyRelative("blockedReason").stringValue = "Needs 2 resin, water, a flower and a plant";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- world ----------

    private static void BuildRigelFarm(Transform world)
    {
        Transform farm = Root("RigelFarm", world).transform;
        Spawn(EnvironmentPrefabBuilder.VillagerHouse("VillagerHouse_Rigel", GrayboxPalette.Lit("RoofRust", new Color(.74f, .46f, .36f))), farm, RigelHousePosition, 180f);
        Part(farm, "RigelSoil", GrayboxMeshFactory.RoundedBox(new Vector3(8.2f, .1f, 5.6f), .05f), GrayboxPalette.SoilTilled, new Vector3(RigelFieldCentre.x, -.04f, RigelFieldCentre.z));
        // slow, honest rice: low green rows
        GameObject crop = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/Items/Crop_OnPlot.prefab");
        for (int row = 0; row < 2; row++)
            for (int column = 0; column < 4; column++)
            {
                GameObject plant = Spawn(crop, farm, RigelFieldCentre + new Vector3((column - 1.5f) * 1.9f, .05f, (row - .5f) * 2.4f), Random.Range(0f, 360f));
                plant.transform.localScale = Vector3.one * .8f;
            }
        GameObject span = Existing("Environment/FenceSpan", EnvironmentPrefabBuilder.FenceSpan);
        for (float x = RigelFieldCentre.x - 4f; x < RigelFieldCentre.x + 4f; x += 2f) Spawn(span, farm, new Vector3(x, 0f, RIGEL_FENCE_Z), 0f);
    }

    // a ring of dark pines with clearings around the resin stumps
    private static void BuildForestEdge(Transform world)
    {
        Transform forest = Root("ForestEdge", world).transform;
        GameObject pine = Existing("Environment/TreePine", EnvironmentPrefabBuilder.PineTree);
        GameObject round = Existing("Environment/TreeRound", EnvironmentPrefabBuilder.RoundTree);
        Random.InitState(2027);
        for (float x = ForestMin.x; x <= ForestMax.x; x += 2.9f)
            for (float z = ForestMin.z; z <= ForestMax.z; z += 2.9f)
            {
                Vector3 position = new Vector3(x + Random.Range(-.7f, .7f), 0f, z + Random.Range(-.7f, .7f));
                // clearings at the stumps, and a lane in from the field (x 19-22) so the player can walk in
                if (ResinPositions.Any(r => Vector3.Distance(r, position) < 2.4f)) continue;
                if (position.x > 18.8f && position.x < 22.4f && position.z < 18f) continue;
                if (Vector3.Distance(position, new Vector3(24f, 0f, 17f)) < 2f) continue; // the village tree already there
                GameObject tree = Spawn(Random.value < .8f ? pine : round, forest, position, Random.Range(0f, 360f));
                tree.transform.localScale = Vector3.one * Random.Range(.95f, 1.35f);
            }
    }

    // ---------- helpers ----------

    public static void RemoveRoots(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject old = FindIncludingInactive(name);
            while (old != null)
            {
                Object.DestroyImmediate(old);
                old = FindIncludingInactive(name);
            }
        }
    }

    // a root saved inactive is skipped by GameObject.Find
    private static GameObject FindIncludingInactive(string name)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    public static Transform Point(Transform parent, string name, Vector3 position, float yaw)
    {
        GameObject go = Root(name, parent);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        return go.transform;
    }
}
