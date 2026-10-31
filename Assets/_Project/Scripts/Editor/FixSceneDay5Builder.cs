using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
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
    public const string GARDEN_3X4_MODEL = "Assets/Ilkham/models/garden/garden_3x4.glb";
    private const string BIRD_PREFAB_PATH = "Assets/_Project/Prefabs/Day5/SkyBird.prefab";

    // the marked land, filled exactly by 2 x 2 tiles of the 3x4 garden (9.63 x 7.34 m with its fence, turned like Day 3's)
    public const float AREA_MIN_X = 13.5f;
    public const float AREA_MAX_X = 32.7f;
    public const float AREA_MIN_Z = -47.5f;
    public const float AREA_MAX_Z = -32.9f;
    public const int TILE_COLUMNS = 2;
    public const int TILE_ROWS = 2;
    public const float GARDEN_YAW = 180f;
    // the model's height above the ground, as Day 3 sets it (FixSceneDay3Builder.GARDEN_3X4_Y on its flat ground)
    public const float GARDEN_LIFT = .19f;
    // the 12 beds in the model's own space: 4 columns x 3 rows, 2.3 m apart
    public static readonly float[] BedXArray = { -3.45f, -1.15f, 1.15f, 3.45f };
    public static readonly float[] BedZArray = { -2.3f, 0f, 2.3f };
    private const float BED_HALF_SIZE = .85f;
    // the model's furrows and ridges
    private static readonly int[] BedSubMeshArray = { 1, 2 };
    // what isn't scenery on the marked land: the ground, the water, the paths
    private static readonly string[] KeptNamePartArray = { "Terrain", "Path", "River", "Pond", "Water", "Bridge", "Fence", "Ground" };

    // the take-off at the land's north edge, below the pond; the dive's target in its middle, facing north (the camera)
    private static readonly Vector3 LaunchPosition = new Vector3(23.1f, 0f, -33.6f);
    private static readonly Vector3 ImpactPosition = new Vector3(23.1f, 0f, -40.2f);
    // the evening: the plantation's north-west corner, looking over it
    public static readonly Vector3 EveningPosition = new Vector3(14.6f, 0f, -31.8f);
    // past the clouds and into orbit
    private const float LAUNCH_HEIGHT = 520f;
    private const float DIG_STAND_DISTANCE = 1.1f;
    // the sky by altitude: the morning's blue, deep blue, the cloud layer's white, the stratosphere, black space
    private static readonly (float altitude, Color color)[] SkyBandArray =
    {
        (20f, new Color(.80f, .88f, .93f)), (90f, new Color(.45f, .70f, .95f)), (145f, new Color(.88f, .92f, .97f)),
        (200f, new Color(.30f, .50f, .88f)), (290f, new Color(.12f, .16f, .45f)), (380f, new Color(.03f, .03f, .09f)),
        (520f, new Color(.01f, .01f, .03f)),
    };
    private const float SPACE_ALTITUDE = 380f;
    // the cloud layer the MC punches through
    private const float CLOUD_BOTTOM = 140f;
    private const float CLOUD_TOP = 175f;
    // a planet 120 m under the peak, small enough that its curve and continents read from the top; it shows only above
    // EARTH_SHOW_ALTITUDE, past its top, so the MC never flies through it
    private const float EARTH_RADIUS = 3000f;
    private const float EARTH_TOP_ALTITUDE = 400f;
    private const float EARTH_SHOW_ALTITUDE = 420f;
    // far enough for the Earth's horizon and the stars
    private const float SPACE_FAR_CLIP = 9000f;
    // the childhood show's studio, far under the map like the portrait booth
    private static readonly Vector3 StudioPosition = new Vector3(0f, -300f, 60f);
    private const string SHOW_TEXTURE_PATH = "Assets/_Project/Art/RenderTextures/ChildhoodShow.renderTexture";
    private const string EARTH_TEXTURE_PATH = "Assets/_Project/Art/Textures/Day5Earth.png";
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
        // the dig: the single bed's spot, the MC kneeling on its north side (toward the camera's side) facing it
        Transform digSpot = Day2GrayboxBuilder.Point(points, "DigSpot", Ground(tinyBedPosition), 0f);
        Transform digStandPoint = Day2GrayboxBuilder.Point(points, "DigStandPoint", Ground(tinyBedPosition + Vector3.forward * DIG_STAND_DISTANCE), 180f);
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
        SetField(sequence, "digSpot", digSpot);
        SetField(sequence, "digStandPoint", digStandPoint);
        SetField(sequence, "launchPoint", launchPoint);
        SetField(sequence, "impactPoint", impactPoint);
        SetValue(sequence, "launchHeight", LAUNCH_HEIGHT);
        BuildMarker(graybox, "DigMarker", digSpot.position, sequence, PlantationSequence.Step.WaitDig);
        BuildMarker(graybox, "JumpMarker", launchPoint.position, sequence, PlantationSequence.Step.WaitMarker);
        BuildSky(director, graybox, sequence);
        BuildChildhoodShow(graybox, sequence, dust);

        PlantationRevealVisual reveal = director.gameObject.AddComponent<PlantationRevealVisual>();
        SetField(reveal, "plantationSequence", sequence);
        SetField(reveal, "tinyBed", tinyBed);
        SetField(reveal, "digSpot", digSpot);
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
        BuildChildhoodShowUi(sequence);

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
    public static GameObject[] FindAreaPlants() => FindPlants(AREA_MIN_X, AREA_MAX_X, AREA_MIN_Z, AREA_MAX_Z);

    // the map's own scenery standing in a rectangle (its trees, bushes, rocks)
    public static GameObject[] FindPlants(float minX, float maxX, float minZ, float maxZ)
    {
        List<GameObject> plantList = new List<GameObject>();
        foreach (MeshRenderer renderer in GameObject.Find("CozyFarm").GetComponentsInChildren<MeshRenderer>(true))
        {
            Vector3 centre = renderer.bounds.center;
            if (centre.x < minX || centre.x > maxX || centre.z < minZ || centre.z > maxZ) continue;
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

    // a bed of the first tile near the take-off, cut out of its mesh: the only ground one sawit and bare hands buy
    private static (Transform piece, Vector3 bed) BuildTinyBed(Transform graybox, Transform firstTile)
    {
        MeshFilter model = firstTile.GetComponentInChildren<MeshFilter>(true);
        // the middle row: the north one is in reach of the pond's water spot, which would take the [Interact] press
        Vector3 bed = BedXArray.SelectMany(x => BedZArray.Select(z => firstTile.position + firstTile.rotation * new Vector3(x, 0f, z)))
            .OrderBy(p => Mathf.Abs(p.z - firstTile.position.z)).ThenBy(p => Mathf.Abs(p.x - LaunchPosition.x)).First();
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
        // aimed a little below the MC: at the top, the Earth's curved horizon fills the bottom of the frame
        launch.GetComponent<CinemachineRotationComposer>().TargetOffset = new Vector3(0f, -.5f, 0f);
        SetFarClip(launch, SPACE_FAR_CLIP);
        // at the top it stops following (Day5CameraVisual) and the MC drops out of its frame
        SetField(cameraVisual, "launchCamera", launch);
        // above and behind the falling MC, looking down past them at the land rushing up
        CinemachineCamera dive = Day3GrayboxBuilder.ChaseCamera(director, "CM_Day5Dive", player, new Vector3(0f, 4.5f, 3.5f), 60f);
        dive.GetComponent<CinemachineRotationComposer>().TargetOffset = new Vector3(0f, -1f, 0f);
        SetFarClip(dive, SPACE_FAR_CLIP);
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

    // ---------- markers, sky, the childhood show ----------

    // a glowing disc on the ground and a diamond bobbing over it, shown while the sequence waits for the player there
    private static void BuildMarker(Transform graybox, string name, Vector3 position, PlantationSequence sequence, PlantationSequence.Step shownStep)
    {
        Transform root = Root(name, graybox).transform;
        root.position = position;
        GameObject marker = Root("Marker", root);
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ring.name = "Ring";
        Object.DestroyImmediate(ring.GetComponent<Collider>());
        ring.transform.SetParent(marker.transform, false);
        ring.transform.localPosition = Vector3.up * .05f;
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale = Vector3.one * 2.2f;
        ring.GetComponent<MeshRenderer>().sharedMaterial = GrayboxPalette.TransparentMapped("MarkerGlow", new Color(1f, .85f, .3f, .6f), GrayboxTextures.Circle.texture, true);
        ring.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Transform arrow = Root("Arrow", marker.transform).transform;
        arrow.localPosition = Vector3.up * 2.4f;
        GameObject diamond = Part(arrow, "Diamond", GrayboxMeshFactory.RoundedBox(new Vector3(.4f, .4f, .4f), .04f), GrayboxPalette.Unlit("MarkerArrow", new Color(1f, .82f, .25f)), Vector3.zero);
        diamond.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        diamond.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        GroundMarkerVisual visual = root.gameObject.AddComponent<GroundMarkerVisual>();
        SetField(visual, "plantationSequence", sequence);
        SerializedObject so = new SerializedObject(visual);
        so.FindProperty("shownStep").enumValueIndex = (int)shownStep;
        so.ApplyModifiedPropertiesWithoutUndo();
        SetField(visual, "markerRoot", marker);
        SetField(visual, "ring", ring.transform);
        SetField(visual, "arrow", arrow);
    }

    // the cloud layer over the climb, the stars and the Earth (hidden until space), and the visual that colours the sky by altitude
    private static void BuildSky(Transform director, Transform graybox, PlantationSequence sequence)
    {
        Vector3 impact = Ground(ImpactPosition);
        Vector3 column = (Ground(LaunchPosition) + impact) * .5f;
        Transform sky = Root("Sky", graybox).transform;

        // squashed white puffs in a wide disc round the climb, some right on it so the MC bursts through
        Transform clouds = Root("Clouds", sky).transform;
        Mesh sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        Material cloud = GrayboxPalette.Unlit("Cloud", new Color(.97f, .98f, 1f));
        System.Random random = new System.Random(5);
        for (int i = 0; i < 60; i++)
        {
            float radius = i < 8 ? (float)random.NextDouble() * 4f : 6f + (float)random.NextDouble() * 55f;
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float altitude = Mathf.Lerp(CLOUD_BOTTOM, CLOUD_TOP, (float)random.NextDouble());
            GameObject puff = Part(clouds, $"Puff_{i + 1}", sphere, cloud, Vector3.zero);
            // world space: the gameplay root isn't at the origin
            puff.transform.position = column + new Vector3(Mathf.Cos(angle) * radius, altitude, Mathf.Sin(angle) * radius);
            puff.transform.localScale = new Vector3(8f + (float)random.NextDouble() * 10f, 2.5f + (float)random.NextDouble() * 2f, 8f + (float)random.NextDouble() * 10f);
            puff.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // stars: one mesh of small cubes on a far sphere round the peak, above its horizon
        Vector3 peak = impact + Vector3.up * LAUNCH_HEIGHT;
        Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        List<CombineInstance> starList = new List<CombineInstance>();
        for (int i = 0; i < 260; i++)
        {
            Vector3 direction = new Vector3((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 1.1f - .1f, (float)random.NextDouble() * 2f - 1f).normalized;
            float size = 6f + (float)random.NextDouble() * 10f;
            starList.Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(direction * 3000f, Random.rotation, Vector3.one * size) });
        }
        Mesh stars = new Mesh { name = "Day5_Stars" };
        stars.CombineMeshes(starList.ToArray(), true, true);
        SaveMesh(stars, "Assets/_Project/Art/Models/Day5/Day5_Stars.asset");
        GameObject starsRoot = Part(sky, "Stars", stars, GrayboxPalette.Unlit("Star", Color.white), Vector3.zero);
        starsRoot.transform.position = peak;
        starsRoot.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // the Earth: a vast textured sphere under the peak with a thin pale haze round it
        GameObject earthRoot = Root("Earth", sky);
        earthRoot.transform.position = impact + Vector3.up * (EARTH_TOP_ALTITUDE - EARTH_RADIUS);
        Material earthMaterial = GrayboxPalette.Lit("Day5Earth", Color.white, .35f);
        earthMaterial.SetTexture("_BaseMap", EarthTexture());
        EditorUtility.SetDirty(earthMaterial);
        GameObject earth = Part(earthRoot.transform, "Planet", sphere, earthMaterial, Vector3.zero);
        earth.transform.localScale = Vector3.one * EARTH_RADIUS * 2f;
        // turned so a continent faces up under the climb
        earth.transform.rotation = Quaternion.Euler(-70f, 0f, 20f);
        earth.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        GameObject haze = Part(earthRoot.transform, "Haze", sphere, GrayboxPalette.TransparentMapped("EarthHaze", new Color(.55f, .78f, 1f, .28f), null, true), Vector3.zero);
        haze.transform.localScale = Vector3.one * EARTH_RADIUS * 2.012f;
        haze.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        SkyAscentVisual ascent = director.gameObject.AddComponent<SkyAscentVisual>();
        SetField(ascent, "plantationSequence", sequence);
        SetField(ascent, "worldCamera", Camera.main);
        SetField(ascent, "starsRoot", starsRoot);
        SetField(ascent, "earthRoot", earthRoot);
        SetValue(ascent, "spaceAltitude", SPACE_ALTITUDE);
        SetValue(ascent, "earthAltitude", EARTH_SHOW_ALTITUDE);
        SerializedObject so = new SerializedObject(ascent);
        SerializedProperty bands = so.FindProperty("skyBandArray");
        bands.arraySize = SkyBandArray.Length;
        for (int i = 0; i < SkyBandArray.Length; i++)
        {
            bands.GetArrayElementAtIndex(i).FindPropertyRelative("altitude").floatValue = SkyBandArray[i].altitude;
            bands.GetArrayElementAtIndex(i).FindPropertyRelative("color").colorValue = SkyBandArray[i].color;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // oceans, green and brown land and white cloud wisps on an equirectangular map (wraps left to right)
    private static Texture2D EarthTexture()
    {
        const int width = 2048, height = 1024;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        Color ocean = new Color(.10f, .32f, .62f), deep = new Color(.05f, .18f, .42f), land = new Color(.28f, .52f, .24f), desert = new Color(.62f, .55f, .34f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width, v = (float)y / height;
                // fine enough that a few continents show in the small cap seen from the peak
                float continent = WrapNoise(u, v, 24, 12, 1) * .6f + WrapNoise(u, v, 56, 28, 2) * .3f + WrapNoise(u, v, 120, 60, 3) * .1f;
                float cloudWisp = Mathf.InverseLerp(.62f, .8f, WrapNoise(u, v, 70, 32, 4) * .7f + WrapNoise(u, v, 150, 70, 5) * .3f);
                Color color = continent > .52f ? Color.Lerp(land, desert, Mathf.InverseLerp(.6f, .75f, continent)) : Color.Lerp(deep, ocean, Mathf.InverseLerp(.3f, .52f, continent));
                // polar ice
                if (v < .07f || v > .93f) color = Color.white;
                texture.SetPixel(x, y, Color.Lerp(color, Color.white, cloudWisp * .85f));
            }
        }
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(EARTH_TEXTURE_PATH));
        System.IO.File.WriteAllBytes(EARTH_TEXTURE_PATH, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(EARTH_TEXTURE_PATH);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(EARTH_TEXTURE_PATH);
    }

    // smooth value noise on a grid that wraps in u (cellsX × cellsY, 0..1 in and out)
    private static float WrapNoise(float u, float v, int cellsX, int cellsY, int seed)
    {
        float x = u * cellsX, y = v * cellsY;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float tx = Mathf.SmoothStep(0f, 1f, x - x0), ty = Mathf.SmoothStep(0f, 1f, y - y0);
        float a = Mathf.Lerp(Hash(x0, y0, cellsX, seed), Hash(x0 + 1, y0, cellsX, seed), tx);
        float b = Mathf.Lerp(Hash(x0, y0 + 1, cellsX, seed), Hash(x0 + 1, y0 + 1, cellsX, seed), tx);
        return Mathf.Lerp(a, b, ty);
    }

    private static float Hash(int x, int y, int cellsX, int seed)
    {
        x = ((x % cellsX) + cellsX) % cellsX;
        uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
        h = (h ^ (h >> 13)) * 1274126177u;
        return (h ^ (h >> 16)) / (float)uint.MaxValue;
    }

    // a tiny cartoon studio far under the map: grass, two lollipop trees, a crater, flowers round it and the superhero,
    // filmed into a RenderTexture for the old TV (ChildhoodShowUI)
    private static void BuildChildhoodShow(Transform graybox, PlantationSequence sequence, Transform dust)
    {
        Transform studio = Root("ChildhoodShowStudio", graybox).transform;
        studio.position = StudioPosition;
        Mesh sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        Mesh cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
        Part(studio, "Ground", cylinder, GrayboxPalette.Lit("ShowGrass", new Color(.45f, .78f, .35f)), Vector3.zero).transform.localScale = new Vector3(14f, .05f, 14f);
        foreach (float x in new[] { -3.6f, 3.4f })
        {
            Part(studio, "TreeTrunk", cylinder, GrayboxPalette.Lit("ShowTrunk", new Color(.5f, .32f, .18f)), new Vector3(x, .9f, 3f)).transform.localScale = new Vector3(.35f, .9f, .35f);
            Part(studio, "TreeTop", sphere, GrayboxPalette.Lit("ShowLeaves", new Color(.25f, .62f, .3f)), new Vector3(x, 2.3f, 3f)).transform.localScale = Vector3.one * 1.8f;
        }

        Renderer crater = Day3GrayboxBuilder.BuildCrater(studio);
        crater.transform.position = StudioPosition + Vector3.up * .06f;
        crater.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // the flowers: three rings round the crater, nearest first, so they bloom outward
        GameObject flower = GameDataBuilder.Load<ItemDataSO>("Items", "Flower").prefab.gameObject;
        List<Transform> flowerList = new List<Transform>();
        foreach ((float radius, int count) in new[] { (1.1f, 6), (1.8f, 9), (2.6f, 12) })
        {
            for (int i = 0; i < count; i++)
            {
                // every other ring turned half a step, so the flowers don't line up in spokes
                float angle = (i + count % 2 * .5f) * Mathf.PI * 2f / count;
                GameObject copy = FixSceneDay4Builder.ModelCopy(flower, studio, StudioPosition + new Vector3(Mathf.Cos(angle) * radius, .05f, Mathf.Sin(angle) * radius));
                copy.transform.localScale *= 1.6f;
                flowerList.Add(copy.transform);
            }
        }

        (Transform hero, Transform armL, Transform armR) = BuildHero(studio);

        Camera camera = Root("ShowCamera", studio).AddComponent<Camera>();
        camera.transform.position = StudioPosition + new Vector3(0f, 1.9f, -5.6f);
        camera.transform.LookAt(StudioPosition + Vector3.up * 1.4f);
        camera.fieldOfView = 45f;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 30f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.55f, .82f, 1f);
        camera.targetTexture = ShowTexture();
        // the map's terrain is 300 m above: no shadows, or it would darken the studio
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderShadows = false;
        camera.enabled = false;

        ChildhoodShowVisual visual = studio.gameObject.AddComponent<ChildhoodShowVisual>();
        SetField(visual, "plantationSequence", sequence);
        SetField(visual, "showCamera", camera);
        SetField(visual, "hero", hero);
        SetField(visual, "heroArmL", armL);
        SetField(visual, "heroArmR", armR);
        SetField(visual, "crater", crater.gameObject);
        SetArray(visual, "flowerArray", flowerList.ToArray());
        SetField(visual, "dustPrefab", dust);
    }

    // a chunky cartoon superhero: blue suit, red mask, gloves and cape, a yellow belt and badge; arms hinge at the shoulders
    private static (Transform hero, Transform armL, Transform armR) BuildHero(Transform studio)
    {
        Transform hero = Root("Superhero", studio).transform;
        hero.position = StudioPosition;
        Material suit = GrayboxPalette.Lit("HeroSuit", new Color(.18f, .36f, .85f));
        Material red = GrayboxPalette.Lit("HeroRed", new Color(.88f, .16f, .14f));
        Material gold = GrayboxPalette.Lit("HeroGold", new Color(1f, .82f, .2f));
        Material skin = GrayboxPalette.Lit("HeroSkin", new Color(.93f, .74f, .58f));
        Mesh Box(float x, float y, float z, float r) => GrayboxMeshFactory.RoundedBox(new Vector3(x, y, z), r);
        foreach (float x in new[] { -.12f, .12f }) Part(hero, "Leg", Box(.18f, .6f, .2f, .06f), suit, new Vector3(x, .3f, 0f));
        Part(hero, "Body", Box(.5f, .55f, .32f, .1f), suit, new Vector3(0f, .88f, 0f));
        Part(hero, "Belt", Box(.52f, .08f, .34f, .02f), gold, new Vector3(0f, .64f, 0f));
        Part(hero, "Badge", Box(.18f, .18f, .04f, .02f), gold, new Vector3(0f, .95f, -.17f));
        Part(hero, "Cape", Box(.52f, .78f, .04f, .02f), red, new Vector3(0f, .78f, .2f));
        Part(hero, "Head", Box(.42f, .42f, .42f, .2f), skin, new Vector3(0f, 1.4f, 0f));
        Part(hero, "Mask", Box(.44f, .1f, .44f, .04f), red, new Vector3(0f, 1.44f, 0f));
        Transform Arm(string name, float x)
        {
            Transform shoulder = Root(name, hero).transform;
            shoulder.localPosition = new Vector3(x, 1.1f, 0f);
            Part(shoulder, "Sleeve", Box(.14f, .5f, .16f, .05f), suit, Vector3.down * .25f);
            Part(shoulder, "Glove", Box(.17f, .15f, .19f, .05f), red, Vector3.down * .54f);
            return shoulder;
        }
        Transform armL = Arm("ArmL", -.33f);
        Transform armR = Arm("ArmR", .33f);
        // built facing -z (badge in front, cape behind): straight at the show's camera
        return (hero, armL, armR);
    }

    private static RenderTexture ShowTexture()
    {
        RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(SHOW_TEXTURE_PATH);
        if (texture != null) return texture;
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SHOW_TEXTURE_PATH));
        texture = new RenderTexture(640, 400, 24) { name = "ChildhoodShow" };
        AssetDatabase.CreateAsset(texture, SHOW_TEXTURE_PATH);
        return texture;
    }

    // an old TV in the middle of the screen, the studio's picture on it, over a dimmed world; under the screen fade
    private static void BuildChildhoodShowUi(PlantationSequence sequence)
    {
        ScreenFadeUI fade = Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include);
        Transform canvas = fade.transform.parent;
        RectTransform root = Rect(canvas, "ChildhoodShowUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        Image dim = Img(root, "Dim", null, new Color(0f, 0f, 0f, .7f), Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        dim.raycastTarget = false;
        RectTransform tv = Rect(root, "TV", Centre, Centre, Centre, new Vector2(0f, 20f), new Vector2(1060f, 720f));
        Image body = Img(tv, "Body", null, new Color(.30f, .19f, .12f), Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        body.raycastTarget = false;
        Image bezel = Img(tv, "Bezel", null, new Color(.08f, .08f, .09f), Centre, Centre, Centre, new Vector2(0f, 30f), new Vector2(970f, 610f));
        bezel.raycastTarget = false;
        RectTransform screenRect = Rect(tv, "Screen", Centre, Centre, Centre, new Vector2(0f, 30f), new Vector2(940f, 588f));
        RawImage screen = screenRect.gameObject.AddComponent<RawImage>();
        screen.texture = ShowTexture();
        // a warm, faded picture: a memory, not a broadcast
        screen.color = new Color(1f, .93f, .8f);
        screen.raycastTarget = false;
        TextMeshProUGUI title = Text(tv, "Title", "CAPTAIN HARVEST", 40, Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(title.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, 40f), new Vector2(800f, 60f));
        TextMeshProUGUI caption = Text(root, "Caption", "A show from when I was little...", 36, Color.white, TextAlignmentOptions.Center, FontStyles.Italic);
        caption.outlineWidth = .2f;
        caption.outlineColor = Color.black;
        Place(caption.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -70f), new Vector2(1200f, 60f));
        UIPop pop = tv.gameObject.AddComponent<UIPop>();
        ChildhoodShowUI ui = root.gameObject.AddComponent<ChildhoodShowUI>();
        SetField(ui, "plantationSequence", sequence);
        SetField(ui, "tvPop", pop);
        fade.transform.SetAsLastSibling();
    }

    // ---------- helpers ----------

    private static Vector3 Ground(Vector3 position) => FixSceneDay2Builder.Ground(position);

    private static void SetFarClip(CinemachineCamera camera, float farClip)
    {
        LensSettings lens = camera.Lens;
        lens.FarClipPlane = farClip;
        camera.Lens = lens;
    }

    private static void SaveMesh(Mesh mesh, string path)
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
    }

    // the highest ground on a 5 x 5 grid over a width x depth rectangle round the centre
    public static float HighestGround(Vector3 centre, float width, float depth)
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
