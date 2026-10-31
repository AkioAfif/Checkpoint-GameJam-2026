using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static GrayboxBuild;
using static UiBuilder;

// Builds "Day 5: Half the Forest" on the CozyFarm map as FixScene_Day5 (Tools > FixScene > Build Day 5 Scene). It copies
// FixScene_Day4 (so build Days 2, 3 and 4 first), leaves Day 4's results standing (its field, its bridge; its felled trees
// gone) and lays out the day:
// - the marked land south of Pond_3 and Day 4's field (x 13.5..32.7, z -47.5..-32.9) tiled 2 x 2 with the 3x4 garden model,
//   a FarmPlot on each of its 48 beds (their own soil off: the model's beds are the soil), hidden until the dive's impact,
//   which also hides the map's own trees, bushes and rocks on that land (PlantationSequence, PlantationRevealVisual);
// - one bed cut from the first tile's mesh, which pushes up after the first sawit;
// - the climb's sky run (SkyRunMinigame, a graybox bird prefab, the HUD), the cameras, and the plants on the plantation's
//   east edge that pulse red behind the MC's back (MutationSpreadVisual).
// Re-runnable: FixScene_Day5 is regenerated each time.
public static class FixSceneDay5Builder
{
    public const string SCENE_PATH = "Assets/_Project/Scenes/FixScene_Day5.unity";
    private const string GAMEPLAY_ROOT = "Gameplay";
    private const string GRAYBOX_ROOT = "Day5_Graybox";
    private const string DIRECTOR_ROOT = "Day5";
    private const string BEDROOM_DIRECTOR_PATH = "BedroomDirector";
    private const string GARDEN_3X4_MODEL = "Assets/Ilkham/models/garden/garden_3x4.glb";
    private const string BIRD_PREFAB_PATH = "Assets/_Project/Prefabs/Day5/SkyBird.prefab";

    // the marked land, filled exactly by 2 x 2 tiles of the 3x4 garden (9.63 x 7.34 m with its fence, turned like Day 3's)
    private const float AREA_MIN_X = 13.5f;
    private const float AREA_MAX_X = 32.7f;
    private const float AREA_MIN_Z = -47.5f;
    private const float AREA_MAX_Z = -32.9f;
    private const int TILE_COLUMNS = 2;
    private const int TILE_ROWS = 2;
    private const float GARDEN_YAW = 180f;
    // the model's height above the ground, as Day 3 sets it (FixSceneDay3Builder.GARDEN_3X4_Y on its flat ground)
    private const float GARDEN_LIFT = .19f;
    // the 12 beds in the model's own space: 4 columns x 3 rows, 2.3 m apart
    private static readonly float[] BedXArray = { -3.45f, -1.15f, 1.15f, 3.45f };
    private static readonly float[] BedZArray = { -2.3f, 0f, 2.3f };
    private const float BED_HALF_SIZE = .85f;
    // the model's furrows and ridges
    private static readonly int[] BedSubMeshArray = { 1, 2 };
    // what isn't scenery on the marked land: the ground, the water, the paths
    private static readonly string[] KeptNamePartArray = { "Terrain", "Path", "River", "Pond", "Water", "Bridge", "Fence", "Ground" };

    // the take-off at the land's north edge, below the pond; the dive's target in its middle, facing north (the camera)
    private static readonly Vector3 LaunchPosition = new Vector3(23.1f, 0f, -33.6f);
    private static readonly Vector3 ImpactPosition = new Vector3(23.1f, 0f, -40.2f);
    // the evening: the plantation's north-west corner, looking over it
    private static readonly Vector3 EveningPosition = new Vector3(14.6f, 0f, -31.8f);
    private const float LAUNCH_HEIGHT = 26f;
    // the launch camera rides north of the MC at foot height (any lower starts it under the ground), looking south and up past
    // them, so screen-right is world west
    private static readonly Vector3 LaunchCameraOffset = new Vector3(0f, .6f, 8.5f);
    private static readonly Vector3 SteerAxis = Vector3.left;

    // the plants just past the plantation's east edge: they pulse red behind the MC's back, and again at night
    private static readonly string[] MutationPathArray =
    {
        "CozyFarm/Forest_Bush_3_030", "CozyFarm/Forest_Tree_3_003", "CozyFarm/Forest_Tree_2_005", "CozyFarm/Forest_Tree_3_021",
    };
    // the edge shot, by day and at night: from over the plantation's north-west, across it at those plants
    private static readonly Vector3 EdgeShotPosition = new Vector3(14f, 7f, -29f);
    private static readonly Vector3 EdgeShotLookAt = new Vector3(38f, 1.5f, -39f);

    [MenuItem("Tools/FixScene/Build Day 5 Scene")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FixSceneDay4Builder.SCENE_PATH) == null)
        {
            Debug.LogError($"{FixSceneDay4Builder.SCENE_PATH} is missing: run Tools > FixScene > Build Day 4 Scene first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // overwrite the file, not delete + copy: the scene keeps its GUID
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) == null) AssetDatabase.CopyAsset(FixSceneDay4Builder.SCENE_PATH, SCENE_PATH);
        else
        {
            System.IO.File.Copy(FixSceneDay4Builder.SCENE_PATH, SCENE_PATH, true);
            AssetDatabase.ImportAsset(SCENE_PATH, ImportAssetOptions.ForceUpdate);
        }
        EditorSceneManager.OpenScene(SCENE_PATH);
        // after opening: OpenScene unloads assets loaded before it
        ItemDataSO bunchLv2 = GameDataBuilder.Load<ItemDataSO>("Items", "SawitBunchLv2");
        Day5SO day5SO = LoadData(bunchLv2);

        Transform gameplay = GameObject.Find(GAMEPLAY_ROOT).transform;
        StripDay4(gameplay);
        Transform graybox = Root(GRAYBOX_ROOT, gameplay).transform;
        Transform director = Root(DIRECTOR_ROOT, gameplay).transform;
        GameObject[] areaPlants = FindAreaPlants();
        (GameObject plantationRoot, Transform[] tiles, FarmPlot[] plots) = BuildPlantation(graybox);
        (Transform tinyBed, Vector3 tinyBedPosition) = BuildTinyBed(graybox, tiles[0]);

        Transform points = Root("Points", director).transform;
        Transform eveningPoint = Day2GrayboxBuilder.Point(points, "EveningPoint", Ground(EveningPosition), 135f);
        Transform launchPoint = Day2GrayboxBuilder.Point(points, "LaunchPoint", Ground(LaunchPosition), 180f);
        Transform impactPoint = Day2GrayboxBuilder.Point(points, "ImpactPoint", Ground(ImpactPosition), 0f);
        Transform day2Graybox = gameplay.Find("Day2_Graybox");
        CutsceneActor playerActor = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include).GetComponent<CutsceneActor>();
        Transform dust = VfxBuilder.SmashDust().transform;

        LandQTE landQTE = director.gameObject.AddComponent<LandQTE>();
        SkyRunMinigame skyRun = director.gameObject.AddComponent<SkyRunMinigame>();
        SetField(skyRun, "rewardItemDataSO", bunchLv2);
        SetField(skyRun, "pickupPrefab", bunchLv2.prefab);
        SetField(skyRun, "birdPrefab", BirdPrefab().transform);
        SetVector(skyRun, "steerAxis", SteerAxis);

        PlantationSequence sequence = director.gameObject.AddComponent<PlantationSequence>();
        SetField(sequence, "day5SO", day5SO);
        SetField(sequence, "landQTE", landQTE);
        SetField(sequence, "playerActor", playerActor);
        SetField(sequence, "skyRunMinigame", skyRun);
        SetField(sequence, "bunchItemDataSO", bunchLv2);
        SetField(sequence, "lv2CropDataSO", Day2DataBuilder.SawitLv2);
        SetArray(sequence, "plantationPlotArray", plots);
        SetField(sequence, "plantationRoot", plantationRoot);
        SetArray(sequence, "areaPlantArray", areaPlants);
        SetField(sequence, "launchPoint", launchPoint);
        SetField(sequence, "impactPoint", impactPoint);
        SetValue(sequence, "launchHeight", LAUNCH_HEIGHT);

        PlantationRevealVisual reveal = director.gameObject.AddComponent<PlantationRevealVisual>();
        SetField(reveal, "plantationSequence", sequence);
        SetField(reveal, "tinyBed", tinyBed);
        SetArray(reveal, "gardenTileArray", tiles);
        SetArray(reveal, "plotVisualArray", plots.Select(p => (Object)p.transform.Find("Visual")).ToArray());
        SetField(reveal, "dustPrefab", dust);

        Day5Director day5 = director.gameObject.AddComponent<Day5Director>();
        SetField(day5, "day5SO", day5SO);
        SetField(day5, "screenFade", Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include));
        SetField(day5, "savingsBookUI", Object.FindAnyObjectByType<SavingsBookUI>(FindObjectsInactive.Include));
        SetField(day5, "fabricator", Object.FindAnyObjectByType<FabricatorController>(FindObjectsInactive.Include));
        Day3GrayboxBuilder.SetRecipe(day5);
        SetField(day5, "lv2CropDataSO", Day2DataBuilder.SawitLv2);
        SetField(day5, "sellCrate", Object.FindAnyObjectByType<SellCrate>(FindObjectsInactive.Include));
        SetField(day5, "plantationSequence", sequence);
        SetField(day5, "ijahNpc", Day3GrayboxBuilder.FindNpc("Npc_Ijah"));
        SetField(day5, "ratnaNpc", Day3GrayboxBuilder.FindNpc("Npc_Ratna"));
        SetField(day5, "dimasNpc", Day3GrayboxBuilder.FindNpc("Npc_Dimas"));
        SetField(day5, "houseDoor", Object.FindAnyObjectByType<HouseDoor>(FindObjectsInactive.Include));
        SetField(day5, "morningPoint", day2Graybox.Find("Points/MorningPoint"));
        SetField(day5, "eveningPoint", eveningPoint);

        NightVisual night = director.gameObject.AddComponent<NightVisual>();
        SetField(night, "day5Director", day5);
        SetField(night, "sun", RenderSettings.sun);
        SetField(night, "worldCamera", Camera.main);
        SetField(night, "doorLamp", day2Graybox.Find("DoorLamp").gameObject);
        SetField(director.gameObject.AddComponent<NightCurfew>(), "day5Director", day5);

        MutationSpreadVisual mutation = director.gameObject.AddComponent<MutationSpreadVisual>();
        SetField(mutation, "director", day5);
        SetArray(mutation, "rendererArray", MutationPathArray.Select(path => (Object)GameObject.Find(path).GetComponent<Renderer>()).ToArray());
        // the same raw red Day 2's palms flicker to (Day2GrayboxBuilder)
        SetField(mutation, "pulseMaterial", GrayboxPalette.Unlit("MutantPulse", new Color(.62f, .05f, .1f)));

        ImaginationVisual imagination = director.gameObject.AddComponent<ImaginationVisual>();
        SetField(imagination, "day5Director", day5);
        SetField(imagination, "ghostFarmRoot", BuildGhostFarm(graybox, tiles, plots));
        SetArray(imagination, "imaginedAwayTreeArray", areaPlants.Select(p => (Object)p.transform).ToArray());

        WorldGlitchVisual glitch = director.gameObject.AddComponent<WorldGlitchVisual>();
        SetField(glitch, "plantationSequence", sequence);
        SetValue(glitch, "screenSplit", 0f);
        SetValue(glitch, "severity", 1.6f);
        SetValue(glitch, "maxJolt", .9f);
        SetArray(glitch, "corruptMaterialArray", new Object[]
        {
            GrayboxPalette.Unlit("GlitchVoid", new Color(.02f, .02f, .03f)),
            GrayboxPalette.Unlit("GlitchBlood", new Color(.42f, 0f, .03f)),
            GrayboxPalette.Unlit("GlitchBone", new Color(.93f, .91f, .86f)),
        });
        SerializedObject glitchSo = new SerializedObject(glitch);
        glitchSo.FindProperty("ignoredLayers").intValue = LayerMask.GetMask("Portrait", "Puzzle");
        glitchSo.ApplyModifiedPropertiesWithoutUndo();

        (CinemachineCamera dive, CinemachineCamera plantation) = BuildCameras(director, day5, sequence, tinyBedPosition);
        BuildImpact(director, sequence, dust, dive, plantation);
        SetField(Day3GrayboxBuilder.BuildUi(null, landQTE), "plantationSequence", sequence);
        BuildSkyRunUi(skyRun);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Day2GrayboxBuilder.AddToBuildProfile(SCENE_PATH);
        WireBedroom(day5SO);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(SCENE_PATH);
        Debug.Log($"Day 5 built into {SCENE_PATH}: {plots.Length} plantation plots, {areaPlants.Length} plants cleared by the impact.");
    }

    // ---------- the map ----------

    // Day 4's story leaves: its director (and its UI), the trees it felled, its ghosts; its field and bridge stay, now real
    private static void StripDay4(Transform gameplay)
    {
        Object.DestroyImmediate(gameplay.Find("Day3").gameObject);
        foreach (string path in new[] { "HUD/LandQTEUI", "HUD/EyeBlinkUI" })
        {
            Transform ui = gameplay.Find(path);
            if (ui != null) Object.DestroyImmediate(ui.gameObject);
        }
        foreach (string path in new[] { "CozyFarm/Day3ClearingTrees", "CozyFarm/Forest_Tree_2_038", "CozyFarm/Forest_Bush_2_011", "CozyFarm/Forest_Bush_3_018" })
            Object.DestroyImmediate(GameObject.Find(path));
        Transform day3Graybox = gameplay.Find("Day3_Graybox");
        foreach (string child in new[] { "Crater", "PunchTargets", "GhostBridge", "GhostFarm" }) Object.DestroyImmediate(day3Graybox.Find(child).gameObject);
        day3Graybox.Find("LandPlots").gameObject.SetActive(true);
        day3Graybox.Find("NewBridge").gameObject.SetActive(true);
        // after the bridge, Rigel keeps away from the MC
        Day3GrayboxBuilder.FindNpc("Npc_Rigel").gameObject.SetActive(false);
    }

    // the map's own scenery standing on the marked land (its trees, bushes, rocks)
    private static GameObject[] FindAreaPlants()
    {
        List<GameObject> plantList = new List<GameObject>();
        foreach (MeshRenderer renderer in GameObject.Find("CozyFarm").GetComponentsInChildren<MeshRenderer>(true))
        {
            Vector3 centre = renderer.bounds.center;
            if (centre.x < AREA_MIN_X || centre.x > AREA_MAX_X || centre.z < AREA_MIN_Z || centre.z > AREA_MAX_Z) continue;
            if (KeptNamePartArray.Any(part => renderer.name.Contains(part)) || renderer.bounds.size.x > 15f) continue;
            plantList.Add(renderer.gameObject);
        }
        return plantList.ToArray();
    }

    // the 3x4 garden model tiled over the marked land, a plot on every bed; all hidden until the impact
    private static (GameObject root, Transform[] tiles, FarmPlot[] plots) BuildPlantation(Transform graybox)
    {
        GameObject root = Root("Plantation", graybox);
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(GARDEN_3X4_MODEL);
        GameObject plotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/Interactables/FarmPlot.prefab");
        float tileWidth = (AREA_MAX_X - AREA_MIN_X) / TILE_COLUMNS;
        float tileDepth = (AREA_MAX_Z - AREA_MIN_Z) / TILE_ROWS;
        Quaternion rotation = Quaternion.Euler(0f, GARDEN_YAW, 0f);
        List<Transform> tileList = new List<Transform>();
        List<FarmPlot> plotList = new List<FarmPlot>();
        // north row first, west to east: the first tile is the one nearest the take-off
        for (int row = 0; row < TILE_ROWS; row++)
        {
            for (int column = 0; column < TILE_COLUMNS; column++)
            {
                Vector3 centre = new Vector3(AREA_MIN_X + tileWidth * (column + .5f), 0f, AREA_MAX_Z - tileDepth * (row + .5f));
                // the land slopes: sat on its highest point, so no grass pokes through the garden's floor
                float baseY = HighestGround(centre, tileWidth, tileDepth);
                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                tile.name = $"Garden3x4_{tileList.Count + 1}";
                tile.transform.SetPositionAndRotation(new Vector3(centre.x, baseY + GARDEN_LIFT, centre.z), rotation);
                tileList.Add(tile.transform);
                foreach (float bedZ in BedZArray)
                {
                    foreach (float bedX in BedXArray)
                    {
                        Vector3 bed = centre + rotation * new Vector3(bedX, 0f, bedZ);
                        bed.y = baseY;
                        FarmPlot plot = Spawn(plotPrefab, root.transform, bed, 0f).GetComponent<FarmPlot>();
                        plot.name = $"FarmPlot_Plantation_{plotList.Count + 1}";
                        SerializedObject so = new SerializedObject(plot);
                        so.FindProperty("startState").enumValueIndex = (int)FarmPlot.State.Empty;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        // the model's bed is the soil
                        plot.transform.Find("Visual/Soil").gameObject.SetActive(false);
                        plotList.Add(plot);
                    }
                }
            }
        }
        root.SetActive(false);
        return (root, tileList.ToArray(), plotList.ToArray());
    }

    // the bed of the first tile nearest the take-off, cut out of its mesh: the only ground one sawit buys
    private static (Transform piece, Vector3 bed) BuildTinyBed(Transform graybox, Transform firstTile)
    {
        MeshFilter model = firstTile.GetComponentInChildren<MeshFilter>(true);
        Vector3 bed = BedXArray.SelectMany(x => BedZArray.Select(z => firstTile.position + firstTile.rotation * new Vector3(x, 0f, z)))
            .OrderByDescending(p => p.z).ThenBy(p => Mathf.Abs(p.x - LaunchPosition.x)).First();
        Mesh mesh = FixSceneDay3Builder.CutMesh(model, "Garden3x4_Bed_Day5", BedSubMeshArray,
            (a, b, c) => Mathf.Abs((a.x + b.x + c.x) / 3f - bed.x) < BED_HALF_SIZE && Mathf.Abs((a.z + b.z + c.z) / 3f - bed.z) < BED_HALF_SIZE);
        GameObject piece = Root("TinyBed", graybox);
        piece.transform.SetPositionAndRotation(model.transform.position, model.transform.rotation);
        piece.transform.localScale = model.transform.lossyScale;
        piece.AddComponent<MeshFilter>().sharedMesh = mesh;
        piece.AddComponent<MeshRenderer>().sharedMaterials = model.GetComponent<MeshRenderer>().sharedMaterials;
        // the cut keeps the whole model's vertex list, so its bounds are the tile's: hand back where the bed really is
        return (piece.transform, new Vector3(bed.x, firstTile.position.y, bed.z));
    }

    // what the MC pictures: the tiled gardens with a ripe palm on every bed, models only
    private static GameObject BuildGhostFarm(Transform graybox, Transform[] tiles, FarmPlot[] plots)
    {
        GameObject root = Root("GhostFarm", graybox);
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(GARDEN_3X4_MODEL);
        foreach (Transform tile in tiles)
        {
            GameObject ghostTile = FixSceneDay4Builder.ModelCopy(model, root.transform, tile.position);
            ghostTile.transform.rotation = tile.rotation;
        }
        foreach (FarmPlot plot in plots)
            FixSceneDay4Builder.ModelCopy(Day2DataBuilder.SawitLv2.ripePrefab.gameObject, root.transform, plot.transform.position + Vector3.up * .1f);
        root.SetActive(false);
        return root;
    }

    // a stubby graybox bird: body, beak and two wings that beat (SkyBirdVisual)
    private static GameObject BirdPrefab()
    {
        GameObject root = Root("SkyBird");
        Material body = GrayboxPalette.Lit("SkyBirdBody", new Color(.25f, .25f, .3f));
        Material beak = GrayboxPalette.Lit("SkyBirdBeak", new Color(.95f, .7f, .2f));
        Part(root.transform, "Body", GrayboxMeshFactory.RoundedBox(new Vector3(.35f, .3f, .6f), .12f), body, Vector3.zero);
        Part(root.transform, "Beak", GrayboxMeshFactory.RoundedBox(new Vector3(.12f, .1f, .2f), .03f), beak, new Vector3(0f, .05f, .38f));
        Transform leftWing = Root("LeftWing", root.transform).transform;
        Transform rightWing = Root("RightWing", root.transform).transform;
        leftWing.localPosition = new Vector3(-.15f, .1f, 0f);
        rightWing.localPosition = new Vector3(.15f, .1f, 0f);
        Mesh wing = GrayboxMeshFactory.RoundedBox(new Vector3(.75f, .03f, .3f), .01f);
        Part(leftWing, "Wing", wing, body, new Vector3(-.35f, 0f, 0f));
        Part(rightWing, "Wing", wing, body, new Vector3(.35f, 0f, 0f));
        root.transform.localScale = Vector3.one * 1.6f;
        SkyBirdVisual flap = root.AddComponent<SkyBirdVisual>();
        SetField(flap, "leftWing", leftWing);
        SetField(flap, "rightWing", rightWing);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BIRD_PREFAB_PATH));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BIRD_PREFAB_PATH);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ---------- impact, cameras, UI ----------

    private static void BuildImpact(Transform director, PlantationSequence sequence, Transform dust, CinemachineCamera dive, CinemachineCamera plantation)
    {
        CinemachineImpulseSource impulse = director.gameObject.AddComponent<CinemachineImpulseSource>();
        impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Rumble;
        impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        // an earthquake: long and rolling, not one jolt
        impulse.ImpulseDefinition.ImpulseDuration = 1.4f;
        impulse.DefaultVelocity = new Vector3(1f, .6f, 0f);
        SmashImpactVisual impact = director.gameObject.AddComponent<SmashImpactVisual>();
        SetField(impact, "plantationSequence", sequence);
        SetField(impact, "impulseSource", impulse);
        SetField(impact, "dustPrefab", dust);
        SetArray(impact, "punchCameraArray", new Object[] { dive, plantation });
        SetValue(impact, "finalDustScale", 4f);
        Day3GrayboxBuilder.AddImpulseListeners();
    }

    private static (CinemachineCamera dive, CinemachineCamera plantation) BuildCameras(Transform director, Day5Director day5, PlantationSequence sequence, Vector3 tinyBed)
    {
        Transform player = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include).transform;
        Vector3 impact = Ground(ImpactPosition);
        Vector3 peak = impact + Vector3.up * LAUNCH_HEIGHT;
        Day5CameraVisual cameraVisual = director.gameObject.AddComponent<Day5CameraVisual>();
        SetField(cameraVisual, "director", day5);
        SetField(cameraVisual, "plantationSequence", sequence);
        SetField(cameraVisual, "brain", Camera.main.GetComponent<CinemachineBrain>());
        // from over the pond, south across the marked land
        SetField(cameraVisual, "wishCamera", Day3GrayboxBuilder.FixedCamera(director, "CM_Day5Wish", new Vector3(23f, 10f, -21f), impact + Vector3.up * .5f, 45f));
        // close on the one bed, from the pond's side, the empty land behind it; aimed below it, so it sits above the dialogue box
        SetField(cameraVisual, "tinyBedCamera", Day3GrayboxBuilder.FixedCamera(director, "CM_Day5TinyBed", tinyBed + new Vector3(0f, 3f, 4.5f), tinyBed + Vector3.down * 1.2f, 40f));
        // below and behind the MC all the way up, looking up past them into the sky
        CinemachineCamera launch = Day3GrayboxBuilder.ChaseCamera(director, "CM_Day5Launch", player, LaunchCameraOffset, 55f);
        launch.GetComponent<CinemachineRotationComposer>().TargetOffset = new Vector3(0f, 2.5f, 0f);
        SetField(cameraVisual, "launchCamera", launch);
        // level with the top of the arc: the MC hangs there, then drops out of the bottom of the frame
        SetField(cameraVisual, "peakCamera", Day3GrayboxBuilder.FixedCamera(director, "CM_Day5Peak", peak + new Vector3(0f, 1.5f, 11f), peak + Vector3.up * .8f, 45f));
        // above and behind the falling MC, looking down past them at the land rushing up
        CinemachineCamera dive = Day3GrayboxBuilder.ChaseCamera(director, "CM_Day5Dive", player, new Vector3(0f, 4.5f, 3.5f), 60f);
        dive.GetComponent<CinemachineRotationComposer>().TargetOffset = new Vector3(0f, -1f, 0f);
        SetField(cameraVisual, "diveCamera", dive);
        CinemachineCamera plantation = Day3GrayboxBuilder.FixedCamera(director, "CM_Day5Plantation", impact + new Vector3(0f, 15f, 17f), impact + new Vector3(0f, 0f, -1.5f), 50f);
        SetField(cameraVisual, "plantationCamera", plantation);
        // the same edge twice: by day behind the MC's back, at night where he stands looking the other way
        SetField(cameraVisual, "edgeCamera", Day3GrayboxBuilder.FixedCamera(director, "CM_Day5Edge", EdgeShotPosition, EdgeShotLookAt, 45f));
        return (dive, plantation);
    }

    // top-centre: the steering hint and the count of caught bunches; under the screen fade
    private static void BuildSkyRunUi(SkyRunMinigame skyRun)
    {
        ScreenFadeUI fade = Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include);
        Transform canvas = fade.transform.parent;
        RectTransform root = Rect(canvas, "SkyRunUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        TextMeshProUGUI hint = Text(root, "Hint", string.Empty, 34, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        hint.outlineWidth = .25f;
        hint.outlineColor = Color.black;
        Place(hint.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -90f), new Vector2(1200f, 60f));
        TextMeshProUGUI count = Text(root, "Count", "Sawit x0", 54, Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        count.outlineWidth = .25f;
        count.outlineColor = Color.black;
        Place(count.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -160f), new Vector2(600f, 80f));
        UIPop pop = count.gameObject.AddComponent<UIPop>();
        SkyRunUI ui = root.gameObject.AddComponent<SkyRunUI>();
        SetField(ui, "skyRunMinigame", skyRun);
        SetField(ui, "hintText", hint);
        SetField(ui, "countText", count);
        SetField(ui, "countPop", pop);
        fade.transform.SetAsLastSibling();
    }

    // ---------- helpers ----------

    private static Vector3 Ground(Vector3 position) => FixSceneDay2Builder.Ground(position);

    // the highest ground on a 5 x 5 grid over a width x depth rectangle round the centre
    private static float HighestGround(Vector3 centre, float width, float depth)
    {
        float highest = float.MinValue;
        for (int i = 0; i <= 4; i++)
            for (int j = 0; j <= 4; j++)
                highest = Mathf.Max(highest, Ground(centre + new Vector3((i / 4f - .5f) * width, 0f, (j / 4f - .5f) * depth)).y);
        return highest;
    }

    private static void SetVector(Object target, string field, Vector3 value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).vector3Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Day5SO LoadData(ItemDataSO bunchLv2)
    {
        Day2DataBuilder.LoadAll();
        GameDataBuilder.EmptyBottle = GameDataBuilder.Load<ItemDataSO>("Items", "EmptyBottle");
        GameDataBuilder.Water = GameDataBuilder.Load<ItemDataSO>("Items", "Water");
        GameDataBuilder.Flower = GameDataBuilder.Load<ItemDataSO>("Items", "Flower");
        return Day5DataBuilder.Build(bunchLv2);
    }

    // the bedroom's director needs Day 5's morning thought
    private static void WireBedroom(Day5SO day5SO)
    {
        // OpenScene unloads assets loaded before it: load the SO again by path once the bedroom is open
        string path = AssetDatabase.GetAssetPath(day5SO);
        EditorSceneManager.OpenScene(HouseInteriorBuilder.SCENE_PATH);
        BedroomDirector bedroom = GameObject.Find(BEDROOM_DIRECTOR_PATH).GetComponent<BedroomDirector>();
        SetField(bedroom, "day5SO", AssetDatabase.LoadAssetAtPath<Day5SO>(path));
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }
}
