using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static GrayboxBuild;

public static partial class Level01SceneBuilder
{
    private static readonly Vector3 PortraitBoothPosition = new Vector3(0f, -200f, 0f);
    private static readonly Vector3 PuzzleBoothPosition = new Vector3(0f, -400f, 0f);

    private static Transform managers;
    private static GameObject player;
    private static Transform cameraTarget;
    private static Camera mainCamera;
    private static FabricatorController fabricator;
    private static FabricatorMiniGame miniGame;
    private static HouseDoor houseDoor;
    private static RenderTexture playerPortraitTexture, npcPortraitTexture;

    static partial void BuildGameplayParts()
    {
        managers = Root("Managers").transform;
        managers.gameObject.AddComponent<GameInput>();
        managers.gameObject.AddComponent<GameManager>();
        BuildManagers();
        BuildPlayer();
        BuildCameras();
        BuildInteractables();
        BuildVillagers();
        BuildPortraitBooth();
        BuildPuzzleBooth();
        SoundManager sound = managers.gameObject.AddComponent<SoundManager>();
        SetField(sound, "audioClipRefsSO", GameDataBuilder.AudioRefs);
        SetField(sound, "houseDoor", houseDoor);
        UiBuilder.BuildLevelHud(new UiBuilder.Refs { fabricator = fabricator, miniGame = miniGame, houseDoor = houseDoor, playerPortrait = playerPortraitTexture, npcPortrait = npcPortraitTexture });
    }

    private static void BuildManagers()
    {
        Inventory inventory = managers.gameObject.AddComponent<Inventory>();
        SerializedObject so = new SerializedObject(inventory);
        SerializedProperty start = so.FindProperty("startingItemArray");
        start.arraySize = 1;
        start.GetArrayElementAtIndex(0).FindPropertyRelative("itemDataSO").objectReferenceValue = GameDataBuilder.EmptyBottle;
        start.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
        managers.gameObject.AddComponent<DialogueManager>();
        managers.gameObject.AddComponent<QTEManager>();
        managers.gameObject.AddComponent<PlantingController>();
        SetArray(managers.gameObject.AddComponent<TaskManager>(), "objectiveSOArray", GameDataBuilder.Tasks);
        PlantingVisual plantingVisual = managers.gameObject.AddComponent<PlantingVisual>();
        SetField(plantingVisual, "successPrefab", VfxBuilder.PlantSuccess().transform);
        SetField(plantingVisual, "failPrefab", VfxBuilder.PlantFail().transform);
    }

    private static void BuildPlayer()
    {
        player = Spawn(PlayerPrefab(), null, PlayerSpawn, 0f);
        cameraTarget = player.transform.Find("CameraTarget");
    }

    private static GameObject PlayerPrefab()
    {
        GameObject root = Root("Player");
        CharacterController controller = root.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, .82f, 0f);
        controller.height = 1.6f;
        controller.radius = .35f;
        controller.stepOffset = .4f;
        controller.skinWidth = .05f;
        root.AddComponent<Player>();
        PlayerInteraction interaction = root.AddComponent<PlayerInteraction>();
        SetMask(interaction, "interactableLayerMask", LayerMask.GetMask("Interactable"));

        GameObject visual = Root("Visual", root.transform);
        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(CharacterBuilder.PlayerRigPrefab(), visual.transform);
        PlayerVisual playerVisual = visual.AddComponent<PlayerVisual>();
        SetField(playerVisual, "player", root.GetComponent<Player>());
        SetField(playerVisual, "animator", rig.GetComponent<Animator>());
        SetField(playerVisual, "dustParticleSystem", VfxBuilder.Dust(root.transform));

        GameObject target = Root("CameraTarget", root.transform);
        target.transform.localPosition = new Vector3(0f, 1f, 0f);
        return SavePrefab(root, "Characters", "Player");
    }

    private static void BuildCameras()
    {
        GameObject cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
        mainCamera = cameraGo.AddComponent<Camera>();
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(.80f, .88f, .93f);
        mainCamera.cullingMask = ~LayerMask.GetMask("Portrait", "Puzzle");
        mainCamera.farClipPlane = 200f;
        cameraGo.AddComponent<AudioListener>();
        cameraGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        CinemachineBrain brain = cameraGo.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, .8f);
        brain.IgnoreTimeScale = true; // keep blending while the discovery popup freezes the world

        Transform cameras = Root("Cameras").transform;

        // camera positions are confined to this box: at the map edge the camera stops and only turns to keep the player framed
        GameObject bounds = Root("CameraBounds", cameras);
        bounds.layer = LayerMask.NameToLayer("Ignore Raycast");
        BoxCollider boundsBox = Box(bounds, new Vector3(-8.5f, 11f, -7.5f), new Vector3(63f, 4f, 39f), true);

        CinemachineCamera follow = FollowCamera(cameras, "CM_Follow", new Vector3(0f, 11f, -10.5f), 35f, new Vector3(.8f, .8f, .8f));
        CinemachineConfiner3D confiner = follow.gameObject.AddComponent<CinemachineConfiner3D>();
        confiner.BoundingVolume = boundsBox;
        confiner.SlowingDistance = 3f;

        CinemachineCamera planting = FollowCamera(cameras, "CM_Planting", new Vector3(0f, 4.4f, -5.4f), 34f, new Vector3(.3f, .3f, .3f));
        CinemachineCamera house = FollowCamera(cameras, "CM_HouseEntry", new Vector3(0f, 3.2f, -4.4f), 32f, new Vector3(.4f, .4f, .4f));
        CinemachineCamera fabricatorCam = FixedCamera(cameras, "CM_Fabricator", FabricatorPosition + new Vector3(-2.4f, 2.6f, -2.6f), FabricatorPosition + new Vector3(0f, 1.4f, 0f), 40f);

        CameraDirector director = managers.gameObject.AddComponent<CameraDirector>();
        SetField(director, "plantingCamera", planting);
        SetField(director, "fabricatorCamera", fabricatorCam);
        SetField(director, "houseEntryCamera", house);
        planting.enabled = false;
        fabricatorCam.enabled = false;
        house.enabled = false;

        cameraGo.transform.position = cameraTarget.position + new Vector3(0f, 11f, -10.5f);
        cameraGo.transform.LookAt(cameraTarget);
    }

    private static CinemachineCamera FollowCamera(Transform parent, string name, Vector3 offset, float fov, Vector3 damping)
    {
        GameObject go = Root(name, parent);
        go.transform.position = cameraTarget.position + offset;
        CinemachineCamera cam = go.AddComponent<CinemachineCamera>();
        cam.Follow = cameraTarget;
        cam.LookAt = cameraTarget;
        LensSettings lens = LensSettings.Default;
        lens.FieldOfView = fov;
        lens.FarClipPlane = 200f;
        cam.Lens = lens;
        CinemachineFollow follow = go.AddComponent<CinemachineFollow>();
        follow.FollowOffset = offset;
        follow.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
        follow.TrackerSettings.PositionDamping = damping;
        CinemachineRotationComposer composer = go.AddComponent<CinemachineRotationComposer>();
        composer.Damping = new Vector2(.4f, .4f);
        return cam;
    }

    private static CinemachineCamera FixedCamera(Transform parent, string name, Vector3 position, Vector3 lookAt, float fov)
    {
        GameObject go = Root(name, parent);
        go.transform.position = position;
        go.transform.LookAt(lookAt);
        CinemachineCamera cam = go.AddComponent<CinemachineCamera>();
        LensSettings lens = LensSettings.Default;
        lens.FieldOfView = fov;
        cam.Lens = lens;
        return cam;
    }

    private static void BuildInteractables()
    {
        Transform interactables = Root("Interactables").transform;

        GameObject fabricatorGo = Spawn(InteractablePrefabBuilder.Fabricator(), interactables, FabricatorPosition, 0f);
        fabricator = fabricatorGo.GetComponent<FabricatorController>();

        GameObject plot = InteractablePrefabBuilder.FarmPlot();
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                Spawn(plot, interactables, FarmCentre + new Vector3((column - 1) * 2.5f, 0f, (row - 1) * 2.5f), 0f);

        GameObject water = InteractablePrefabBuilder.WaterSpot();
        float eastBank = RIVER_X + RIVER_HALF_WIDTH, westBank = RIVER_X - RIVER_HALF_WIDTH;
        Spawn(water, interactables, new Vector3(eastBank, 0f, 5.5f), 0f);
        Spawn(water, interactables, new Vector3(eastBank, 0f, -6f), 0f);
        Spawn(water, interactables, new Vector3(westBank, 0f, -4.5f), 180f);
        Spawn(water, interactables, new Vector3(westBank, 0f, 9f), 180f);

        // flower clumps in front of and beside every villager house
        GameObject flower = InteractablePrefabBuilder.FlowerNode();
        (Vector3 house, bool facesNorth)[] houses = { (new Vector3(-34f, 0f, -10f), true), (new Vector3(-36f, 0f, 10f), false), (new Vector3(-8f, 0f, -10f), true), (new Vector3(-6f, 0f, 11f), false) };
        foreach ((Vector3 house, bool facesNorth) in houses)
        {
            float front = facesNorth ? 3.5f : -3.5f;
            Spawn(flower, interactables, house + new Vector3(-2.4f, 0f, front), Random.Range(0f, 360f));
            Spawn(flower, interactables, house + new Vector3(2.4f, 0f, front), Random.Range(0f, 360f));
            Spawn(flower, interactables, house + new Vector3(facesNorth ? -3.6f : 3.6f, 0f, front * .2f), Random.Range(0f, 360f));
        }
    }

    private static void BuildVillagers()
    {
        Transform villagers = Root("Villagers").transform;
        Transform points = Root("NpcWanderPoints").transform;
        Villager(villagers, points, InteractablePrefabBuilder.Villager("Npc_Ratna", CharacterBuilder.RatnaLook, GameDataBuilder.Ratna), new Vector3(-6f, 0f, -5.2f), new[] { new Vector3(-10.5f, 0f, -5f), new Vector3(-4.5f, 0f, -4.5f), new Vector3(-7f, 0f, -3.2f) });
        Villager(villagers, points, InteractablePrefabBuilder.Villager("Npc_Darto", CharacterBuilder.DartoLook, GameDataBuilder.Darto), new Vector3(-31.5f, 0f, -5f), new[] { new Vector3(-29f, 0f, -4f), new Vector3(-26f, 0f, -2.2f), new Vector3(-31f, 0f, -3f) });
        Villager(villagers, points, InteractablePrefabBuilder.Villager("Npc_Dimas", CharacterBuilder.DimasLook, GameDataBuilder.Dimas), new Vector3(-33f, 0f, 5f), new[] { new Vector3(-40f, 0f, 4f), new Vector3(-30f, 0f, 3.5f), new Vector3(-37f, 0f, 2.2f), new Vector3(-28f, 0f, 6f) });
        Villager(villagers, points, InteractablePrefabBuilder.Villager("Npc_Ijah", CharacterBuilder.IjahLook, GameDataBuilder.Ijah), new Vector3(-3.5f, 0f, 6.5f), new[] { new Vector3(-2f, 0f, 5.5f), new Vector3(-5f, 0f, 6.2f) });
        // the antagonist loiters by the Fabricator and never wanders
        GameObject distributor = InteractablePrefabBuilder.Villager("Npc_Distributor", CharacterBuilder.DistributorRigPrefab(), GameDataBuilder.Distributor, 1.3f, .6f, 1.05f);
        Villager(villagers, points, distributor, FabricatorPosition + new Vector3(2.3f, 0f, -.5f), new Vector3[0]);
    }

    private static void Villager(Transform parent, Transform pointsParent, GameObject prefab, Vector3 position, Vector3[] wanderPoints)
    {
        GameObject npc = Spawn(prefab, parent, position, 180f);
        Transform[] pointArray = new Transform[wanderPoints.Length];
        for (int i = 0; i < wanderPoints.Length; i++)
        {
            pointArray[i] = Root($"{prefab.name}_Point{i}", pointsParent).transform;
            pointArray[i].position = wanderPoints[i];
        }
        SetArray(npc.GetComponent<NpcWander>(), "wanderPointArray", pointArray);
    }

    // hidden photo studio: copies of the player and the current NPC filmed into the dialogue portraits
    public static void BuildPortraitBooth()
    {
        int layer = LayerMask.NameToLayer("Portrait");
        GameObject booth = Root("PortraitBooth");
        booth.transform.position = PortraitBoothPosition;
        PortraitBooth portraitBooth = booth.AddComponent<PortraitBooth>();

        GameObject playerRig = (GameObject)PrefabUtility.InstantiatePrefab(CharacterBuilder.PlayerRigPrefab(), booth.transform);
        playerRig.transform.localPosition = Vector3.zero;
        playerRig.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
        Transform npcStand = Root("NpcStand", booth.transform).transform;
        npcStand.localPosition = new Vector3(10f, 0f, 0f);
        npcStand.localRotation = Quaternion.Euler(0f, -18f, 0f);

        playerPortraitTexture = PortraitTexture("Portrait_Player");
        npcPortraitTexture = PortraitTexture("Portrait_Npc");
        Camera playerCamera = PortraitCamera(booth.transform, "PlayerPortraitCamera", Vector3.zero, playerPortraitTexture, new Color(.84f, .90f, .98f));
        Camera npcCamera = PortraitCamera(booth.transform, "NpcPortraitCamera", npcStand.localPosition, npcPortraitTexture, new Color(.99f, .87f, .88f));

        GameObject lightGo = Root("PortraitLight", booth.transform);
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.color = new Color(1f, .96f, .9f);
        light.cullingMask = LayerMask.GetMask("Portrait");
        lightGo.transform.rotation = Quaternion.Euler(28f, 160f, 0f);

        SetLayerRecursively(booth, layer);
        SetField(portraitBooth, "playerPortraitAnimator", playerRig.GetComponent<Animator>());
        SetField(portraitBooth, "npcStand", npcStand);
        SetField(portraitBooth, "playerCamera", playerCamera);
        SetField(portraitBooth, "npcCamera", npcCamera);
    }

    private static Camera PortraitCamera(Transform booth, string name, Vector3 standLocal, RenderTexture target, Color background)
    {
        GameObject go = Root(name, booth);
        go.transform.localPosition = standLocal + new Vector3(0f, 1.2f, 2.7f);
        go.transform.LookAt(booth.TransformPoint(standLocal + new Vector3(0f, 1.05f, 0f)));
        Camera camera = go.AddComponent<Camera>();
        camera.fieldOfView = 30f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        camera.cullingMask = LayerMask.GetMask("Portrait");
        camera.targetTexture = target;
        go.AddComponent<UniversalAdditionalCameraData>();
        return camera;
    }

    public static RenderTexture PortraitTexture(string name)
    {
        string path = $"Assets/_Project/Art/RenderTextures/{name}.renderTexture";
        RenderTexture existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if (existing != null) return existing;
        RenderTexture texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32) { name = name, antiAliasing = 4 };
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }

    // the Fabricator puzzle: the grid board, filmed flat-on in 2D by its own camera (FabricatorGridPuzzleBuilder sets it up)
    private static void BuildPuzzleBooth()
    {
        int layer = LayerMask.NameToLayer("Puzzle");
        GameObject booth = Root("FabricatorPuzzleBooth");
        booth.transform.position = PuzzleBoothPosition;

        GameObject cameraGo = Root("PuzzleCamera", booth.transform);
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.cullingMask = LayerMask.GetMask("Puzzle");
        camera.depth = 5f;
        cameraGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        FabricatorPuzzleCamera puzzleCamera = cameraGo.AddComponent<FabricatorPuzzleCamera>();
        SetField(puzzleCamera, "fabricator", fabricator);
        SetField(puzzleCamera, "worldCamera", mainCamera);

        SetLayerRecursively(booth, layer);
        miniGame = booth.AddComponent<FabricatorMiniGame>();
        FabricatorGridPuzzleBuilder.SetUpBooth(miniGame, camera);
        SetField(fabricator, "miniGame", miniGame);
    }

    private static void BakeNavMesh()
    {
        GameObject go = Root("NavMesh");
        NavMeshSurface surface = go.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = LayerMask.GetMask("Default");
        surface.BuildNavMesh();
        string folder = SCENE_PATH.Replace(".unity", "");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Project/Scenes", "Level01_VillagePrototype");
        string path = $"{folder}/NavMesh.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(surface.navMeshData, path);
    }

    private static void SetMask(Object target, string field, int mask)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).intValue = mask;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
