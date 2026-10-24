using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static GrayboxBuild;

// Rebuilds Level01_VillagePrototype from scratch. Camera looks north (+Z); "right" of the player house is +X.
public static partial class Level01SceneBuilder
{
    public const string SCENE_PATH = "Assets/_Project/Scenes/Level01_VillagePrototype.unity";

    // playable area (invisible walls sit on these lines)
    public const float WEST = -46f, EAST = 29.5f, SOUTH = -22f, NORTH = 27f;
    public const float RIVER_X = -20f, RIVER_HALF_WIDTH = 2.5f;
    public static readonly Vector3 PlayerHousePosition = new Vector3(14f, 0f, 10f);
    public static readonly Vector3 PlayerSpawn = new Vector3(14f, 0f, 2.5f);
    public static readonly Vector3 FabricatorPosition = new Vector3(10.2f, 0f, 4.6f);
    public static readonly Vector3 FarmCentre = new Vector3(23f, 0f, 5.5f);

    private static Transform environment;

    [MenuItem("Tools/Level01/Rebuild Scene")]
    public static void BuildAll()
    {
        GameDataBuilder.BuildAll();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildLightingAndVolume();
        BuildWorld();
        BuildGameplay();
        BakeNavMesh();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SCENE_PATH);
        AssetDatabase.SaveAssets();
    }

    // later phases fill this in (player, camera, interactables, UI)
    static partial void BuildGameplayParts();

    private static void BuildGameplay() => BuildGameplayParts();

    public static void BuildLightingAndVolume()
    {
        GameObject sun = new GameObject("Directional Light");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, .95f, .86f);
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = .65f;
        light.cullingMask = ~LayerMask.GetMask("Portrait", "Puzzle"); // the booths have their own lights
        sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.78f, .83f, .92f);
        RenderSettings.ambientEquatorColor = new Color(.72f, .74f, .70f);
        RenderSettings.ambientGroundColor = new Color(.48f, .45f, .41f);
        RenderSettings.skybox = null;
        RenderSettings.sun = light;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(.80f, .88f, .93f);
        RenderSettings.fogStartDistance = 35f;
        RenderSettings.fogEndDistance = 90f;

        GameObject volumeGo = new GameObject("Global Volume");
        Volume volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = VolumeProfileAsset();
    }

    private static VolumeProfile VolumeProfileAsset()
    {
        const string PATH = "Assets/_Project/Art/Level01_VolumeProfile.asset";
        VolumeProfile existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PATH);
        if (existing != null) return existing;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, PATH);
        Tonemapping tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.Neutral);
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(.35f);
        bloom.threshold.Override(1f);
        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(.15f);
        color.saturation.Override(8f);
        color.contrast.Override(-4f);
        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(.18f);
        foreach (VolumeComponent component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void BuildWorld()
    {
        environment = Root("Environment").transform;
        BuildGround();
        BuildRiver();
        BuildPaths();
        BuildHouses();
        BuildFarmAndFence();
        BuildTrees();
        BuildBorders();
        foreach (Transform t in environment.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
    }

    private static void BuildGround()
    {
        Transform ground = Root("Ground", environment).transform;
        float westWidth = RIVER_X - RIVER_HALF_WIDTH + 75f;
        float eastWidth = 75f - (RIVER_X + RIVER_HALF_WIDTH);
        GameObject west = Part(ground, "GroundWest", GrayboxMeshFactory.RoundedBox(new Vector3(westWidth, 2f, 110f), .45f), GrayboxPalette.Grass, new Vector3(-75f + westWidth * .5f, -2f, 0f));
        GameObject east = Part(ground, "GroundEast", GrayboxMeshFactory.RoundedBox(new Vector3(eastWidth, 2f, 110f), .45f), GrayboxPalette.Grass, new Vector3(75f - eastWidth * .5f, -2f, 0f));
        Box(west, new Vector3(0f, 1f, 0f), new Vector3(westWidth, 2f, 110f));
        Box(east, new Vector3(0f, 1f, 0f), new Vector3(eastWidth, 2f, 110f));
    }

    private static void BuildRiver()
    {
        Transform river = Root("River", environment).transform;
        Part(river, "RiverBed", GrayboxMeshFactory.RoundedBox(new Vector3(RIVER_HALF_WIDTH * 2f + 1f, 1f, 110f), .3f), GrayboxPalette.Soil, new Vector3(RIVER_X, -2.1f, 0f));
        GameObject water = Part(river, "Water", GrayboxMeshFactory.RoundedBox(new Vector3(RIVER_HALF_WIDTH * 2f + .6f, .1f, 110f), .04f), GrayboxPalette.Water, new Vector3(RIVER_X, -.5f, 0f));
        water.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        water.layer = LayerMask.NameToLayer("Water");

        // invisible bank walls with a gap where the bridge crosses
        const float GAP = 1.7f;
        foreach (float bankX in new[] { RIVER_X - RIVER_HALF_WIDTH + .2f, RIVER_X + RIVER_HALF_WIDTH - .2f })
        {
            GameObject bank = Root("BankWall", river);
            Box(bank, new Vector3(bankX, 1f, (SOUTH - 5f + -GAP) * .5f), new Vector3(.4f, 3f, -GAP - (SOUTH - 5f)));
            Box(bank, new Vector3(bankX, 1f, (NORTH + 5f + GAP) * .5f), new Vector3(.4f, 3f, NORTH + 5f - GAP));
        }
        Spawn(EnvironmentPrefabBuilder.Bridge(), river, new Vector3(RIVER_X, 0f, 0f));
        GameObject rock = EnvironmentPrefabBuilder.Rock();
        Spawn(rock, river, new Vector3(RIVER_X - 3.2f, 0f, 6f), 40f);
        Spawn(rock, river, new Vector3(RIVER_X + 3.3f, 0f, -7f), 130f);
        Spawn(rock, river, new Vector3(RIVER_X + 3.1f, 0f, 15f), 200f);
    }

    private static void BuildPaths()
    {
        Transform paths = Root("Paths", environment).transform;
        void PathStrip(Vector3 from, Vector3 to, float width)
        {
            Vector3 centre = (from + to) * .5f;
            Vector3 size = new Vector3(Mathf.Abs(to.x - from.x) + width, .06f, Mathf.Abs(to.z - from.z) + width);
            Part(paths, "Path", GrayboxMeshFactory.RoundedBox(size, .03f), GrayboxPalette.Path, new Vector3(centre.x, -.02f, centre.z));
        }
        PathStrip(new Vector3(WEST + 2f, 0f, 0f), new Vector3(RIVER_X - 4.2f, 0f, 0f), 2.6f);
        PathStrip(new Vector3(RIVER_X + 4.2f, 0f, 0f), new Vector3(PlayerHousePosition.x, 0f, 0f), 2.6f);
        PathStrip(new Vector3(PlayerHousePosition.x, 0f, 0f), new Vector3(PlayerHousePosition.x, 0f, 6.6f), 2f);
        PathStrip(new Vector3(-34f, 0f, 0f), new Vector3(-34f, 0f, -6.5f), 1.6f);
        PathStrip(new Vector3(-36f, 0f, 0f), new Vector3(-36f, 0f, 6.5f), 1.6f);
        PathStrip(new Vector3(-8f, 0f, 0f), new Vector3(-8f, 0f, -6.5f), 1.6f);
        PathStrip(new Vector3(-6f, 0f, 0f), new Vector3(-6f, 0f, 7.5f), 1.6f);
        PathStrip(new Vector3(PlayerHousePosition.x, 0f, 2f), new Vector3(FarmCentre.x - 4.5f, 0f, 2f), 1.4f);
    }

    private static void BuildHouses()
    {
        Transform houses = Root("Houses", environment).transform;
        houseDoor = Spawn(InteractablePrefabBuilder.PlayerHouseWithDoor(), houses, PlayerHousePosition).GetComponentInChildren<HouseDoor>();
        Spawn(EnvironmentPrefabBuilder.VillagerHouse("VillagerHouse_A", GrayboxPalette.Roof), houses, new Vector3(-34f, 0f, -10f), 180f);
        Spawn(EnvironmentPrefabBuilder.VillagerHouse("VillagerHouse_B", GrayboxPalette.Lit("RoofSage", new Color(.58f, .68f, .62f))), houses, new Vector3(-36f, 0f, 10f));
        Spawn(EnvironmentPrefabBuilder.VillagerHouse("VillagerHouse_C", GrayboxPalette.Lit("RoofLilac", new Color(.70f, .63f, .76f))), houses, new Vector3(-8f, 0f, -10f), 180f);
        Spawn(EnvironmentPrefabBuilder.VillagerHouse("VillagerHouse_D", GrayboxPalette.Lit("RoofSand", new Color(.80f, .72f, .56f))), houses, new Vector3(-6f, 0f, 11f));
    }

    private static void BuildFarmAndFence()
    {
        Transform farm = Root("FarmGround", environment).transform;
        Part(farm, "FarmSoil", GrayboxMeshFactory.RoundedBox(new Vector3(8.6f, .1f, 8.6f), .05f), GrayboxPalette.Soil, new Vector3(FarmCentre.x, -.04f, FarmCentre.z));
        Transform fence = Root("ForestFence", environment).transform;
        GameObject span = EnvironmentPrefabBuilder.FenceSpan();
        for (float z = SOUTH - 4f; z < NORTH + 4f; z += 2f) Spawn(span, fence, new Vector3(EAST, 0f, z), -90f);
    }

    private static void BuildTrees()
    {
        Transform trees = Root("Trees", environment).transform;
        GameObject pine = EnvironmentPrefabBuilder.PineTree();
        GameObject round = EnvironmentPrefabBuilder.RoundTree();
        GameObject bush = EnvironmentPrefabBuilder.Bush();
        Random.InitState(2026);

        // forest behind the fence (backdrop only)
        for (float x = EAST + 2.5f; x < EAST + 22f; x += 2.6f)
            for (float z = SOUTH - 8f; z < NORTH + 8f; z += 2.6f)
                Spawn(pine, trees, new Vector3(x + Random.Range(-.8f, .8f), 0f, z + Random.Range(-.8f, .8f)), Random.Range(0f, 360f)).transform.localScale = Vector3.one * Random.Range(.85f, 1.25f);

        // ring of round trees just outside the other three borders
        for (float x = WEST - 6f; x < EAST; x += 3.2f)
        {
            Spawn(round, trees, new Vector3(x + Random.Range(-.6f, .6f), 0f, SOUTH - 2.2f + Random.Range(-.5f, .5f)), Random.Range(0f, 360f));
            Spawn(round, trees, new Vector3(x + Random.Range(-.6f, .6f), 0f, NORTH + 2.2f + Random.Range(-.5f, .5f)), Random.Range(0f, 360f));
            Spawn(round, trees, new Vector3(x + Random.Range(-.6f, .6f), 0f, NORTH + 6f + Random.Range(-.5f, .5f)), Random.Range(0f, 360f));
        }
        for (float z = SOUTH; z < NORTH; z += 3.2f)
        {
            Spawn(round, trees, new Vector3(WEST - 2.2f + Random.Range(-.5f, .5f), 0f, z + Random.Range(-.6f, .6f)), Random.Range(0f, 360f));
            Spawn(round, trees, new Vector3(WEST - 6f + Random.Range(-.5f, .5f), 0f, z + Random.Range(-.6f, .6f)), Random.Range(0f, 360f));
        }

        // village decoration (kept clear of paths and houses)
        Vector3[] villageTrees = { new Vector3(-42f, 0f, -16f), new Vector3(-27f, 0f, 18f), new Vector3(-13f, 0f, 19f), new Vector3(-14f, 0f, -17f), new Vector3(3f, 0f, 16f), new Vector3(4f, 0f, -12f), new Vector3(-28f, 0f, -18f), new Vector3(24f, 0f, 17f), new Vector3(6f, 0f, 9f) };
        foreach (Vector3 p in villageTrees) Spawn(round, trees, p, Random.Range(0f, 360f));
        Vector3[] bushes = { new Vector3(-31f, 0f, -4f), new Vector3(-39f, 0f, 4.5f), new Vector3(-3f, 0f, -5f), new Vector3(-10f, 0f, 5f), new Vector3(9f, 0f, 12f), new Vector3(19f, 0f, 12f), new Vector3(1f, 0f, 4f), new Vector3(17f, 0f, -6f) };
        foreach (Vector3 p in bushes) Spawn(bush, trees, p, Random.Range(0f, 360f));
    }

    private static void BuildBorders()
    {
        GameObject walls = Root("MapBorderWalls", environment);
        float width = EAST - WEST, depth = NORTH - SOUTH;
        Box(walls, new Vector3(WEST - .5f, 2f, (NORTH + SOUTH) * .5f), new Vector3(1f, 4f, depth + 2f));
        Box(walls, new Vector3(EAST + .6f, 2f, (NORTH + SOUTH) * .5f), new Vector3(1f, 4f, depth + 2f));
        Box(walls, new Vector3((WEST + EAST) * .5f, 2f, SOUTH - .5f), new Vector3(width + 2f, 4f, 1f));
        Box(walls, new Vector3((WEST + EAST) * .5f, 2f, NORTH + .5f), new Vector3(width + 2f, 4f, 1f));
    }
}
