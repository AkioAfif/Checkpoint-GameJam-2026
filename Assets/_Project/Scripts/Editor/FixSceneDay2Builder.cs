using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static GrayboxBuild;

// Builds "Day 2: The First Harvest" on the CozyFarm map as FixScene_Day2 (Tools > FixScene > Build Day 2 Scene, with FixScene open).
// It copies FixScene, takes Day 1's story out of the copy and adds Day 2 laid out for this map, not the old village's:
// - Rigel lives at Rival_Property_House, ~48 m north of the player's garden, so after the sale he walks down the path to the
//   garden and waits there (the old village put his fence right next to the field);
// - the resin stumps stand in the map's own forest across the bridge, so no pine grove or rice field is built;
// - the ICAN crate stands by the path between the garden and the Fabricator; the river right behind them gives the water.
// Re-runnable: FixScene_Day2 is regenerated each time, so tune the map in FixScene and rebuild.
// It only loads existing data (GameDataBuilder.LoadAll would put the old sapling back on the CozyFarm Duskbell crop).
public static class FixSceneDay2Builder
{
    public const string SOURCE_SCENE_PATH = "Assets/_Project/Scenes/FixScene.unity";
    public const string SCENE_PATH = "Assets/_Project/Scenes/FixScene_Day2.unity";
    private const string GAMEPLAY_ROOT = "Day1_Gameplay";
    private const string GRAYBOX_ROOT = "Day2_Graybox";
    private const string DIRECTOR_ROOT = "Day2";

    // the camera looks south over the yard, so "facing the camera" is yaw 0
    private static readonly Vector3 MorningPosition = new Vector3(-22.2f, 0f, -27.2f);
    // north of the garden fence, facing the palms
    private static readonly Vector3 EveningPosition = new Vector3(-13.1f, 0f, -28.8f);
    private static readonly Vector3 CratePosition = new Vector3(-9.6f, 0f, -27.8f);
    // he waits at the garden's north fence; he arrives down the path from the village
    private static readonly Vector3 RigelStand = new Vector3(-13.1f, 0f, -27.6f);
    private static readonly Vector3 RigelArrival = new Vector3(-14.4f, 0f, -16f);
    // under the pines just across the bridge, either side of the forest path
    private static readonly Vector3[] ResinPositions = { new Vector3(15.5f, 0f, -8.5f), new Vector3(24.2f, 0f, -10f), new Vector3(23f, 0f, 2.5f) };
    // above the yard's north-west corner, looking down over the garden toward the river
    private static readonly Vector3 FarmCameraPosition = new Vector3(-19.5f, 6.5f, -24f);
    private static readonly Vector3 FarmCameraLookAt = new Vector3(-12f, .9f, -33.7f);

    [MenuItem("Tools/FixScene/Build Day 2 Scene")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().path != SOURCE_SCENE_PATH)
        {
            Debug.LogError($"Open {SOURCE_SCENE_PATH} first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        AssetDatabase.DeleteAsset(SCENE_PATH);
        AssetDatabase.CopyAsset(SOURCE_SCENE_PATH, SCENE_PATH);
        EditorSceneManager.OpenScene(SCENE_PATH);
        // after opening: OpenScene unloads assets loaded before it, and references to them would save as missing
        LoadData();
        Day2SO day2SO = CozyFarmDay2SO();
        Transform gameplay = GameObject.Find(GAMEPLAY_ROOT).transform;
        gameplay.name = "Gameplay";
        Transform graybox = Root(GRAYBOX_ROOT, gameplay).transform;
        Transform director = Root(DIRECTOR_ROOT, gameplay).transform;
        GameObject doorLamp = StripDay1(gameplay, graybox);

        Spawn(Prefab("Interactables/IcanCrate"), graybox, Ground(CratePosition), 180f);
        Transform resin = Root("ResinNodes", graybox).transform;
        foreach (Vector3 position in ResinPositions) Spawn(Prefab("Interactables/ResinNode"), resin, Ground(position), Random.Range(0f, 360f));
        GameObject rigel = Spawn(Prefab("Characters/Npc_Rigel"), graybox, Ground(RigelStand), 180f);
        Day1GrayboxBuilder.EnsureActor(rigel, rigel.GetComponentInChildren<NpcVisual>(true));
        Transform points = Root("Points", graybox).transform;
        Transform morningPoint = Day2GrayboxBuilder.Point(points, "MorningPoint", Ground(MorningPosition), 0f);
        Transform eveningPoint = Day2GrayboxBuilder.Point(points, "EveningPoint", Ground(EveningPosition), 180f);
        Transform rigelArrivalPoint = Day2GrayboxBuilder.Point(points, "RigelArrivalPoint", Ground(RigelArrival), 180f);

        Transform hud = Object.FindAnyObjectByType<HudUI>(FindObjectsInactive.Include).transform.parent;
        SavingsBookUI savingsBookUI = Day2UiBuilder.BuildSavingsBook(hud, day2SO);
        Day2UiBuilder.BuildMoneyPopup(hud);
        // the fade stays the top panel, over the new ones
        Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include).transform.SetAsLastSibling();

        // the garden's active plots, the row nearest the camera (north) first
        FarmPlot[] plots = Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include)
            .Where(p => p.gameObject.activeSelf)
            .OrderBy(p => -Mathf.Round(p.transform.position.z * 10f)).ThenBy(p => p.transform.position.x).ToArray();
        // the scene's own sun, not the first directional light (the portrait and puzzle booths have their own)
        Light sun = RenderSettings.sun;
        Day2Director day2 = Day2GrayboxBuilder.AddDirector(director, day2SO, savingsBookUI, rigel.GetComponent<Npc>(), morningPoint, eveningPoint,
            plots, doorLamp, sun, FarmCameraPosition, FarmCameraLookAt);
        SetField(day2, "rigelArrivalPoint", rigelArrivalPoint);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log($"Day 2 built into {SCENE_PATH} on the CozyFarm map. Press Play in it to test Day 2 on its own (3 stand-in ripe palms).");
    }

    // Day 1's story leaves the copy (truck, Sari, the Distributor, Day 1's director and cameras). The house door and the
    // door lamp sit under Day 1's roots in FixScene, so they move out first. Returns the door lamp, kept for Day 2's night.
    private static GameObject StripDay1(Transform gameplay, Transform graybox)
    {
        Transform day1Graybox = gameplay.Find("Day1_Graybox");
        Transform day1 = gameplay.Find("Day1");

        // the Fabricator stands where the Distributor set it down yesterday
        FabricatorController fabricator = Object.FindAnyObjectByType<FabricatorController>(FindObjectsInactive.Include);
        Transform placePoint = day1Graybox.Find("FabricatorPlacePoint");
        float placedScale = new SerializedObject(fabricator.GetComponent<FabricatorDelivery>()).FindProperty("placedScale").floatValue;
        fabricator.transform.SetPositionAndRotation(placePoint.position, placePoint.rotation);
        fabricator.transform.localScale = Vector3.one * placedScale;
        Object.DestroyImmediate(fabricator.GetComponent<FabricatorDelivery>());

        day1.Find("PlayerHouse_Door").SetParent(gameplay.Find("Interactables"), true);
        GameObject doorLamp = day1Graybox.Find("DoorLamp").gameObject;
        doorLamp.transform.SetParent(graybox, true);
        Object.DestroyImmediate(day1Graybox.gameObject);
        Object.DestroyImmediate(day1.gameObject);
        Object.DestroyImmediate(Object.FindObjectsByType<Npc>(FindObjectsInactive.Include).First(n => n.name == "Npc_Distributor").gameObject);
        SetField(Object.FindAnyObjectByType<SoundManager>(FindObjectsInactive.Include), "truck", null);
        return doorLamp;
    }

    // Day 2's text with this map's one difference: Rigel comes to the player's field instead of waiting at his own
    private static Day2SO CozyFarmDay2SO()
    {
        Day2SO day2SO = GameDataBuilder.Asset<Day2SO>("Day2", "Day2_CozyFarm");
        EditorUtility.CopySerialized(Day2DataBuilder.Day2, day2SO);
        day2SO.name = "Day2_CozyFarm";
        day2SO.rigelObjectiveArray = new[] { GameDataBuilder.Task("D2_MeetRigelAtGarden", TaskObjectiveSO.Type.Story, "Talk to Rigel at your garden", null, 1) };
        EditorUtility.SetDirty(day2SO);
        return day2SO;
    }

    private static void LoadData()
    {
        Day2DataBuilder.LoadAll();
        GameDataBuilder.EmptyBottle = GameDataBuilder.Load<ItemDataSO>("Items", "EmptyBottle");
        GameDataBuilder.Water = GameDataBuilder.Load<ItemDataSO>("Items", "Water");
        GameDataBuilder.Flower = GameDataBuilder.Load<ItemDataSO>("Items", "Flower");
        GameDataBuilder.Crop = GameDataBuilder.Load<ItemDataSO>("Items", "Crop");
        GameDataBuilder.SawitLv1 = GameDataBuilder.Load<CropDataSO>("Crops", "SawitLv1");
        GameDataBuilder.PuzzleSplit = GameDataBuilder.Load<FabricatorPuzzleSO>("Puzzles", "Puzzle_03_Split");
    }

    private static GameObject Prefab(string pathInPrefabs)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/{pathInPrefabs}.prefab");
        if (prefab == null) Debug.LogError($"{pathInPrefabs}.prefab is missing: run Tools > Day2 > Build Day 2 Scene once to create Day 2's prefabs");
        return prefab;
    }

    // the map's ground isn't flat at y 0: snap to the terrain or path under the point
    private static Vector3 Ground(Vector3 position)
    {
        float height = position.y;
        float best = float.MinValue;
        foreach (RaycastHit hit in Physics.RaycastAll(position + Vector3.up * 30f, Vector3.down, 60f))
        {
            string name = hit.collider.name;
            bool isGround = name.Contains("Terrain") || name.Contains("Path") || name.Contains("Ground");
            if (isGround && hit.point.y > best) best = hit.point.y;
        }
        if (best > float.MinValue) height = best;
        return new Vector3(position.x, height, position.z);
    }
}
