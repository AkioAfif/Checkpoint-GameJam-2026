using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static GrayboxBuild;

// Builds "Day 6: The Offer" on the CozyFarm map as FixScene_Day6 (Tools > FixScene > Build Day 6 Scene). It copies
// FixScene_Day5 (so build Day 5 first), strips Day 5's story and lays out the day, two weeks on:
// - Day 5's plantation stands from the start, and "half the forest" is palms: the 3x4 garden model tiled further east
//   (2 columns x 3 rows, scenery only: the model and a ripe palm on every bed), the map's plants under it hidden;
// - the wild plants just past the whole plantation's edges pulse red all day (MutationSpreadVisual with no director);
// - a copy of the Distributor who walks up the path to the MC's door, the cameras, and Day6Director.
// Re-runnable: FixScene_Day6 is regenerated each time.
public static class FixSceneDay6Builder
{
    public const string SCENE_PATH = "Assets/_Project/Scenes/FixScene_Day6.unity";
    private const string GAMEPLAY_ROOT = "Gameplay";
    private const string DAY5_GRAYBOX_ROOT = "Day5_Graybox";
    private const string GRAYBOX_ROOT = "Day6_Graybox";
    private const string DIRECTOR_ROOT = "Day6";
    private const string BEDROOM_DIRECTOR_PATH = "BedroomDirector";

    // the extension: east of Day 5's land, one row further north (clear of Pond_2), up to the eastern cliffs
    private const int EXTRA_COLUMNS = 2;
    private const int EXTRA_ROWS = 3;
    // how far past the plantation's edges the pulsing plants stand
    private const float EDGE_BAND = 6f;
    // the torn forest: everything from the plantation's south edge to past the world border (river to cliffs), and the strip
    // east of it up to the cliffs
    private const float TORN_SOUTH_DEPTH = 28f;
    private const float TORN_EAST_WIDTH = 12f;
    private const float TORN_WEST_X = 4f;
    // ground, roads, the skirt and the mountains are bigger than any plant or rock
    private const float TORN_MAX_SIZE = 15f;

    // the Distributor's walk: down the path north of the house, round its corner, to the MC by the front door (stand points on
    // the NavMesh: the agent re-seats him on it after each walk)
    private static readonly Vector3 ArrivalPosition = new Vector3(-14.2f, 0f, -11f);
    private static readonly Vector3 PathCornerPosition = new Vector3(-14.2f, 0f, -25.6f);
    private static readonly Vector3 StandPosition = new Vector3(-22.2f, 0f, -24f);
    private static readonly Vector3 FabricatorStandPosition = new Vector3(-7.3f, 0f, -23.2f);

    [MenuItem("Tools/FixScene/Build Day 6 Scene")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FixSceneDay5Builder.SCENE_PATH) == null)
        {
            Debug.LogError($"{FixSceneDay5Builder.SCENE_PATH} is missing: run Tools > FixScene > Build Day 5 Scene first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // overwrite the file, not delete + copy: the scene keeps its GUID
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) == null) AssetDatabase.CopyAsset(FixSceneDay5Builder.SCENE_PATH, SCENE_PATH);
        else
        {
            System.IO.File.Copy(FixSceneDay5Builder.SCENE_PATH, SCENE_PATH, true);
            AssetDatabase.ImportAsset(SCENE_PATH, ImportAssetOptions.ForceUpdate);
        }
        EditorSceneManager.OpenScene(SCENE_PATH);
        // after opening: OpenScene unloads assets loaded before it
        Day2DataBuilder.LoadAll();
        GameDataBuilder.EmptyBottle = GameDataBuilder.Load<ItemDataSO>("Items", "EmptyBottle");
        GameDataBuilder.Water = GameDataBuilder.Load<ItemDataSO>("Items", "Water");
        GameDataBuilder.Flower = GameDataBuilder.Load<ItemDataSO>("Items", "Flower");
        Day6SO day6SO = Day6DataBuilder.Build();

        Transform gameplay = GameObject.Find(GAMEPLAY_ROOT).transform;
        FarmPlot[] plantationPlots = StripDay5(gameplay);
        Transform graybox = Root(GRAYBOX_ROOT, gameplay).transform;
        Transform director = Root(DIRECTOR_ROOT, gameplay).transform;
        (float minX, float maxX, float minZ, float maxZ) = BuildExtension(graybox);

        Transform points = Root("Points", director).transform;
        Transform arrivalPoint = Day2GrayboxBuilder.Point(points, "ArrivalPoint", Ground(ArrivalPosition), 180f);
        Transform pathPoint = Day2GrayboxBuilder.Point(points, "PathPoint", Ground(PathCornerPosition), 270f);
        Transform standPoint = Day2GrayboxBuilder.Point(points, "DistributorStandPoint", Ground(StandPosition), 180f);
        Transform fabricatorStandPoint = Day2GrayboxBuilder.Point(points, "FabricatorStandPoint", Ground(FabricatorStandPosition), 180f);
        Transform eveningPoint = Day2GrayboxBuilder.Point(points, "EveningPoint", Ground(FixSceneDay5Builder.EveningPosition), 135f);
        Transform day2Graybox = gameplay.Find("Day2_Graybox");
        Transform morningPoint = day2Graybox.Find("Points/MorningPoint");

        // a copy of the Distributor (Day 1's is gone from these days); the director walks him in, so he doesn't wander
        // (NpcVisual still reads the disabled wander's speed)
        GameObject distributorGo = Spawn(AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/Characters/Npc_Distributor.prefab"), director, arrivalPoint.position, 180f);
        distributorGo.name = "Npc_Distributor";
        distributorGo.GetComponent<NpcWander>().enabled = false;
        CutsceneActor distributorActor = Day1GrayboxBuilder.EnsureActor(distributorGo, distributorGo.GetComponentInChildren<NpcVisual>(true));

        Day6Director day6 = director.gameObject.AddComponent<Day6Director>();
        SetField(day6, "day6SO", day6SO);
        SetField(day6, "screenFade", Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include));
        SetField(day6, "fabricator", Object.FindAnyObjectByType<FabricatorController>(FindObjectsInactive.Include));
        Day3GrayboxBuilder.SetRecipe(day6);
        SetField(day6, "lv2CropDataSO", Day2DataBuilder.SawitLv2);
        SetArray(day6, "plantationPlotArray", plantationPlots);
        SetField(day6, "distributorNpc", distributorGo.GetComponent<Npc>());
        SetField(day6, "distributorActor", distributorActor);
        SetField(day6, "ijahNpc", Day3GrayboxBuilder.FindNpc("Npc_Ijah"));
        SetField(day6, "ratnaNpc", Day3GrayboxBuilder.FindNpc("Npc_Ratna"));
        SetField(day6, "dimasNpc", Day3GrayboxBuilder.FindNpc("Npc_Dimas"));
        SetField(day6, "houseDoor", Object.FindAnyObjectByType<HouseDoor>(FindObjectsInactive.Include));
        SetField(day6, "morningPoint", morningPoint);
        SetField(day6, "arrivalPoint", arrivalPoint);
        SetField(day6, "pathPoint", pathPoint);
        SetField(day6, "distributorStandPoint", standPoint);
        SetField(day6, "fabricatorStandPoint", fabricatorStandPoint);
        SetField(day6, "eveningPoint", eveningPoint);

        NightVisual night = director.gameObject.AddComponent<NightVisual>();
        SetField(night, "day6Director", day6);
        SetField(night, "sun", RenderSettings.sun);
        SetField(night, "worldCamera", Camera.main);
        SetField(night, "doorLamp", day2Graybox.Find("DoorLamp").gameObject);
        SetField(director.gameObject.AddComponent<NightCurfew>(), "day6Director", day6);

        Renderer[] edgePlants = FindEdgePlants(minX, maxX, minZ, maxZ);
        Renderer[] tornPlants = edgePlants.Union(FindTornForest(minX, maxX, minZ, maxZ)).ToArray();
        // static batching would freeze them in place: the tear moves and stretches each one
        foreach (Renderer plant in tornPlants)
            GameObjectUtility.SetStaticEditorFlags(plant.gameObject, GameObjectUtility.GetStaticEditorFlags(plant.gameObject) & ~StaticEditorFlags.BatchingStatic);
        MutationSpreadVisual mutation = director.gameObject.AddComponent<MutationSpreadVisual>();
        SetArray(mutation, "rendererArray", tornPlants);
        // the same raw red Day 2's palms flicker to (Day2GrayboxBuilder), and the void-black of the world glitch
        SetField(mutation, "pulseMaterial", GrayboxPalette.Unlit("MutantPulse", new Color(.62f, .05f, .1f)));
        SetField(mutation, "blackMaterial", GrayboxPalette.Unlit("GlitchVoid", new Color(.02f, .02f, .03f)));
        SetValue(mutation, "isTearing", 1f);

        BuildCameras(director, day6, distributorGo.transform, standPoint.position, edgePlants, minX, maxX, minZ, maxZ);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Day2GrayboxBuilder.AddToBuildProfile(SCENE_PATH);
        WireBedroom(day6SO);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(SCENE_PATH);
        Debug.Log($"Day 6 built into {SCENE_PATH}: plantation x {minX}..{maxX}, z {minZ}..{maxZ}, {tornPlants.Length} plants tear.");
    }

    // ---------- the map ----------

    // Day 5's story leaves; its plantation stands (its plots are handed back) and the land's plants under it stay gone
    private static FarmPlot[] StripDay5(Transform gameplay)
    {
        Object.DestroyImmediate(gameplay.Find("Day5").gameObject);
        foreach (Component ui in new Component[]
                 {
                     Object.FindAnyObjectByType<LandQTEUI>(FindObjectsInactive.Include), Object.FindAnyObjectByType<SkyRunUI>(FindObjectsInactive.Include),
                     Object.FindAnyObjectByType<ChildhoodShowUI>(FindObjectsInactive.Include),
                 })
            if (ui != null) Object.DestroyImmediate(ui.gameObject);
        Transform day5Graybox = gameplay.Find(DAY5_GRAYBOX_ROOT);
        foreach (string child in new[] { "ChildhoodShowStudio", "Sky", "TinyBed", "GhostFarm", "DigMarker", "JumpMarker" })
            Object.DestroyImmediate(day5Graybox.Find(child).gameObject);
        GameObject plantation = day5Graybox.Find("Plantation").gameObject;
        plantation.SetActive(true);
        foreach (GameObject plant in FixSceneDay5Builder.FindAreaPlants()) plant.SetActive(false);
        return plantation.GetComponentsInChildren<FarmPlot>(true);
    }

    // more 3x4 gardens east of Day 5's, each with a ripe palm model on every bed; returns the whole plantation's rectangle
    private static (float minX, float maxX, float minZ, float maxZ) BuildExtension(Transform graybox)
    {
        float tileWidth = (FixSceneDay5Builder.AREA_MAX_X - FixSceneDay5Builder.AREA_MIN_X) / FixSceneDay5Builder.TILE_COLUMNS;
        float tileDepth = (FixSceneDay5Builder.AREA_MAX_Z - FixSceneDay5Builder.AREA_MIN_Z) / FixSceneDay5Builder.TILE_ROWS;
        float minX = FixSceneDay5Builder.AREA_MAX_X, maxX = minX + tileWidth * EXTRA_COLUMNS;
        float minZ = FixSceneDay5Builder.AREA_MIN_Z, maxZ = minZ + tileDepth * EXTRA_ROWS;
        foreach (GameObject plant in FixSceneDay5Builder.FindPlants(minX, maxX, minZ, maxZ)) plant.SetActive(false);

        Transform root = Root("PlantationExtension", graybox).transform;
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(FixSceneDay5Builder.GARDEN_3X4_MODEL);
        GameObject palm = Day2DataBuilder.SawitLv2.ripePrefab.gameObject;
        Quaternion rotation = Quaternion.Euler(0f, FixSceneDay5Builder.GARDEN_YAW, 0f);
        for (int row = 0; row < EXTRA_ROWS; row++)
        {
            for (int column = 0; column < EXTRA_COLUMNS; column++)
            {
                Vector3 centre = new Vector3(minX + tileWidth * (column + .5f), 0f, maxZ - tileDepth * (row + .5f));
                float baseY = FixSceneDay5Builder.HighestGround(centre, tileWidth, tileDepth);
                GameObject tile = FixSceneDay4Builder.ModelCopy(model, root, new Vector3(centre.x, baseY + FixSceneDay5Builder.GARDEN_LIFT, centre.z));
                tile.name = $"Garden3x4_Extra_{row * EXTRA_COLUMNS + column + 1}";
                tile.transform.rotation = rotation;
                foreach (float bedZ in FixSceneDay5Builder.BedZArray)
                {
                    foreach (float bedX in FixSceneDay5Builder.BedXArray)
                    {
                        Vector3 bed = centre + rotation * new Vector3(bedX, 0f, bedZ);
                        // where a FarmPlot's palm stands on Day 5's tiles
                        bed.y = baseY + .1f;
                        FixSceneDay4Builder.ModelCopy(palm, tile.transform, bed);
                    }
                }
            }
        }
        return (FixSceneDay5Builder.AREA_MIN_X, maxX, minZ, maxZ);
    }

    // the map's trees and bushes within EDGE_BAND outside the plantation's rectangle
    private static Renderer[] FindEdgePlants(float minX, float maxX, float minZ, float maxZ)
    {
        List<Renderer> plantList = new List<Renderer>();
        foreach (MeshRenderer renderer in GameObject.Find("CozyFarm").GetComponentsInChildren<MeshRenderer>())
        {
            if (!renderer.name.StartsWith("Forest_")) continue;
            Vector3 c = renderer.bounds.center;
            bool isInside = c.x >= minX && c.x <= maxX && c.z >= minZ && c.z <= maxZ;
            bool isNear = c.x >= minX - EDGE_BAND && c.x <= maxX + EDGE_BAND && c.z >= minZ - EDGE_BAND && c.z <= maxZ + EDGE_BAND;
            if (isNear && !isInside) plantList.Add(renderer);
        }
        return plantList.ToArray();
    }

    // every scenery piece (the map's and the world border's) south of the plantation, river to cliffs, and east of it
    private static IEnumerable<Renderer> FindTornForest(float minX, float maxX, float minZ, float maxZ)
    {
        foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>())
        {
            Bounds bounds = renderer.bounds;
            if (bounds.size.x > TORN_MAX_SIZE || bounds.size.z > TORN_MAX_SIZE) continue;
            Vector3 c = bounds.center;
            bool isSouth = c.z < minZ && c.z > minZ - TORN_SOUTH_DEPTH && c.x > TORN_WEST_X && c.x < maxX + TORN_EAST_WIDTH;
            bool isEast = c.x > maxX && c.x < maxX + TORN_EAST_WIDTH && c.z >= minZ && c.z < maxZ + EDGE_BAND;
            if (isSouth || isEast) yield return renderer;
        }
    }

    // ---------- cameras ----------

    private static void BuildCameras(Transform director, Day6Director day6, Transform distributor, Vector3 stand, Renderer[] edgePlants,
        float minX, float maxX, float minZ, float maxZ)
    {
        Vector3 centre = Ground(new Vector3((minX + maxX) * .5f, 0f, (minZ + maxZ) * .5f));
        Day6CameraVisual cameraVisual = director.gameObject.AddComponent<Day6CameraVisual>();
        SetField(cameraVisual, "director", day6);
        // high over the north edge, across all of it to the cliffs
        CinemachineCamera wide = Day3GrayboxBuilder.FixedCamera(director, "CM_Day6Wide", centre + new Vector3(-6f, 24f, 22f), centre + new Vector3(2f, 0f, -2f), 55f);
        SetField(cameraVisual, "wideCamera", wide);
        // from above the garden's north fence, up the path at him
        CinemachineCamera arrival = Day3GrayboxBuilder.FixedCamera(director, "CM_Day6Arrival", Ground(new Vector3(-12.5f, 0f, -30.5f)) + Vector3.up * 4.5f, stand, 40f);
        arrival.LookAt = distributor;
        arrival.gameObject.AddComponent<CinemachineRotationComposer>().TargetOffset = Vector3.up * 1.2f;
        SetField(cameraVisual, "arrivalCamera", arrival);
        // side-on from the east, the two of them by the door; aimed low so they stand above the dialogue box
        Vector3 between = (stand + Vector3.back * Day6Director.TALK_DISTANCE + stand) * .5f;
        SetField(cameraVisual, "meetingCamera", Day3GrayboxBuilder.FixedCamera(director, "CM_Day6Meeting", between + new Vector3(8f, 3f, .6f), between + Vector3.up * .3f, 40f));
        // close on his face past the MC's shoulder, off to the east of their hat; aimed under it, above the dialogue box
        Vector3 face = stand + Vector3.up * 1.35f;
        SetField(cameraVisual, "glassesCamera", Day3GrayboxBuilder.FixedCamera(director, "CM_Day6Glasses", face + new Vector3(1.4f, .15f, -2.6f), face + Vector3.down * .45f, 35f));
        // the night: low over the palms, a few rows in from the edge, at the plants past it
        Vector3 edge = edgePlants.Length > 0 ? edgePlants.Aggregate(Vector3.zero, (sum, r) => sum + r.bounds.center) / edgePlants.Length : centre;
        SetField(cameraVisual, "edgeCamera", Day3GrayboxBuilder.FixedCamera(director, "CM_Day6Edge", edge + new Vector3(-4f, 6f, 14f), edge + Vector3.up * 2f, 50f));
    }

    // ---------- helpers ----------

    private static Vector3 Ground(Vector3 position) => FixSceneDay2Builder.Ground(position);

    // the bedroom's director needs Day 6's caption, payoff and morning thought
    private static void WireBedroom(Day6SO day6SO)
    {
        // OpenScene unloads assets loaded before it: load the SO again by path once the bedroom is open
        string path = AssetDatabase.GetAssetPath(day6SO);
        EditorSceneManager.OpenScene(HouseInteriorBuilder.SCENE_PATH);
        BedroomDirector bedroom = GameObject.Find(BEDROOM_DIRECTOR_PATH).GetComponent<BedroomDirector>();
        SetField(bedroom, "day6SO", AssetDatabase.LoadAssetAtPath<Day6SO>(path));
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }
}
