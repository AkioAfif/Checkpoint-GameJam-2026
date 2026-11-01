using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static GrayboxBuild;

// Adds "Day 1: The Crisis" to the open Level01 scene. Re-runnable: it deletes and rebuilds only what it created
// (the Day1_Graybox / Day1 roots and its UI panels) and re-points the few existing objects it has to touch.
// The village, bridge and farm plots are reused as they are. After Tools > Level01 > Rebuild Scene, run it again.
// The bedroom morning lives in its own scene: Tools > Day1 > Build House Interior (HouseInteriorBuilder).
public static class Day1GrayboxBuilder
{
    private const string GRAYBOX_ROOT = "Day1_Graybox";
    private const string DIRECTOR_ROOT = "Day1";

    // comes from across the river, down the middle of the road (z 0) so it fits between the bridge rails; parks west of
    // the yard (spun round to face west) so it stays out of the meeting shot, and leaves west the way it came
    private static readonly Vector3 TruckStart = new Vector3(-34f, 0f, 0f);
    private static readonly Vector3 TruckStop = new Vector3(6f, 0f, 0f);
    // far enough west that the held departure shot has lost it before it is switched off
    private static readonly Vector3 TruckExit = new Vector3(-45f, 0f, 0f);
    private static readonly Vector3 YardPosition = Level01SceneBuilder.PlayerHousePosition + new Vector3(0f, 0f, -4.4f);
    private static readonly Vector3 DistributorStand = Level01SceneBuilder.FabricatorPosition + new Vector3(1.6f, 0f, -.7f);
    // at the east end of the bridge, on the road: right in the leaving truck's way
    private static readonly Vector3 SariPosition = new Vector3(-15.3f, 0f, -.4f);
    // the house shot sits far enough west that no grown palm peeks in; the MC stops mid-field, clear of the dialogue box
    private static readonly Vector3 EveningStart = new Vector3(.5f, 0f, 2.5f);
    private static readonly Vector3 EveningStop = new Vector3(11.5f, 0f, 2.5f);
    private static readonly Vector3 FarmInspect = new Vector3(18.6f, 0f, 6.5f);
    private static readonly Vector3 HouseShot = new Vector3(10f, 0f, 4.5f);
    private static readonly Vector3 FarmShot = new Vector3(22.5f, 0f, 4.5f);

    [MenuItem("Tools/Day1/Build Graybox")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().path != Level01SceneBuilder.SCENE_PATH)
        {
            Debug.LogError($"Open {Level01SceneBuilder.SCENE_PATH} first (Tools > Level01 > Rebuild Scene builds it).");
            return;
        }

        GameDataBuilder.LoadAll();
        Day1DataBuilder.Build();
        RemovePreviousBuild();

        Transform graybox = Root(GRAYBOX_ROOT).transform;
        Transform director = Root(DIRECTOR_ROOT).transform;

        FabricatorController fabricator = ReplaceFabricator();
        FabricatorDelivery delivery = fabricator.GetComponent<FabricatorDelivery>();
        Transform placePoint = Point(graybox, "FabricatorPlacePoint", Level01SceneBuilder.FabricatorPosition, 180f);
        SetField(delivery, "placePoint", placePoint);
        PlaceFabricatorCamera();

        Transform points = Root("Points", graybox).transform;
        // in front of the door step, facing the camera (south)
        Transform yardPoint = Point(points, "YardPoint", YardPosition, 180f);
        // the evening walk: in from the west along the front of the house (south of the Fabricator), then over to the field
        Transform eveningStartPoint = Point(points, "EveningStartPoint", EveningStart, 90f);
        Transform eveningStopPoint = Point(points, "EveningStopPoint", EveningStop, 90f);
        Transform farmInspectPoint = Point(points, "FarmInspectPoint", FarmInspect, 90f);
        Transform houseShotPoint = Point(points, "HouseShotPoint", HouseShot, 0f);
        Transform farmShotPoint = Point(points, "FarmShotPoint", FarmShot, 0f);

        DistributorTruck truck = SpawnTruck(graybox, points);
        Player player = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        EnsureActor(player.gameObject, player.GetComponentInChildren<PlayerVisual>());
        player.transform.SetPositionAndRotation(YardPosition, Quaternion.Euler(0f, 180f, 0f));

        Npc distributorNpc = FindNpc("Npc_Distributor");
        CutsceneActor distributorActor = EnsureActor(distributorNpc.gameObject, distributorNpc.GetComponentInChildren<NpcVisual>());
        distributorNpc.transform.position = DistributorStand;
        Npc sari = SpawnSari(graybox);
        CutsceneActor sariActor = EnsureActor(sari.gameObject, sari.GetComponentInChildren<NpcVisual>());
        GameObject doorLamp = SpawnDoorLamp(graybox);

        HouseDoor houseDoor = Object.FindAnyObjectByType<HouseDoor>(FindObjectsInactive.Include);
        PatchDialogueUi();
        Transform hud = Object.FindAnyObjectByType<HudUI>(FindObjectsInactive.Include).transform.parent;
        ScreenFadeUI fade = Day1UiBuilder.BuildVillage(hud, Object.FindAnyObjectByType<FabricatorMiniGame>(FindObjectsInactive.Include));

        // CM_Truck follows a stand-in that Day1CameraVisual stops short of the river, so the bridge never shows
        Transform truckCameraTarget = Root("TruckCameraTarget", director).transform;
        truckCameraTarget.position = TruckStop;
        // a little ahead of the truck (east) so the arrival shows its front corner, not a flat side view
        CinemachineCamera truckCamera = ChaseCamera(director, "CM_Truck", truckCameraTarget, new Vector3(4f, 4.5f, -8.5f), 38f);
        truckCamera.enabled = false;
        // frames the player and the Distributor wherever the player stopped; Day1CameraVisual keeps it side-on to the pair
        CinemachineTargetGroup meetingGroup = Root("MeetingGroup", director).AddComponent<CinemachineTargetGroup>();
        meetingGroup.AddMember(player.transform, 1f, .8f);
        meetingGroup.AddMember(distributorNpc.transform, 1f, .8f);
        CinemachineCamera meetingCamera = ChaseCamera(director, "CM_Meeting", meetingGroup.transform, new Vector3(0f, 2.8f, -6.5f), 40f);
        // aim a little above the feet: both characters sit above the dialogue box
        meetingCamera.GetComponent<CinemachineRotationComposer>().TargetOffset = new Vector3(0f, .4f, 0f);
        meetingCamera.enabled = false;
        // keeps the Distributor in shot while he fetches and carries the Fabricator, wherever the player stopped
        CinemachineCamera deliveryCamera = ChaseCamera(director, "CM_Delivery", distributorNpc.transform, new Vector3(4f, 4f, -6f), 40f);
        deliveryCamera.GetComponent<CinemachineRotationComposer>().TargetOffset = new Vector3(0f, .8f, 0f);
        deliveryCamera.enabled = false;
        // the evening at the usual gameplay angle (CM_Follow's offset and lens): the house front, then a pan over to the grown field
        Transform eveningCameraTarget = Root("EveningCameraTarget", director).transform;
        eveningCameraTarget.position = HouseShot;
        CinemachineCamera eveningCamera = ChaseCamera(director, "CM_Evening", eveningCameraTarget, new Vector3(0f, 11f, -10.5f), 35f);
        eveningCamera.enabled = false;

        Day1Director day1 = director.gameObject.AddComponent<Day1Director>();
        SetField(day1, "day1SO", Day1DataBuilder.Day1);
        SetField(day1, "screenFade", fade);
        SetField(day1, "chapterListSO", ChapterDataBuilder.Build());
        SetField(day1, "truck", truck);
        SetField(day1, "fabricator", fabricator);
        SetField(day1, "fabricatorDelivery", delivery);
        SetField(day1, "distributorNpc", distributorNpc);
        SetField(day1, "distributorActor", distributorActor);
        SetField(day1, "playerActor", player.GetComponent<CutsceneActor>());
        SetField(day1, "sariNpc", sari);
        SetField(day1, "sariActor", sariActor);
        SetField(day1, "houseDoor", houseDoor);
        SetField(day1, "yardPoint", yardPoint);
        SetField(day1, "distributorCarryPoint", CarryPoint(distributorNpc.transform));
        SetArray(day1, "farmPlotArray", Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include));
        SetField(day1, "eveningStartPoint", eveningStartPoint);
        SetField(day1, "eveningStopPoint", eveningStopPoint);
        SetField(day1, "farmInspectPoint", farmInspectPoint);

        Day1CameraVisual cameraVisual = director.gameObject.AddComponent<Day1CameraVisual>();
        SetField(cameraVisual, "director", day1);
        SetField(cameraVisual, "truckCamera", truckCamera);
        SetField(cameraVisual, "meetingCamera", meetingCamera);
        SetField(cameraVisual, "deliveryCamera", deliveryCamera);
        SetField(cameraVisual, "meetingFollow", meetingCamera.GetComponent<CinemachineFollow>());
        SetField(cameraVisual, "distributor", distributorNpc.transform);
        SetField(cameraVisual, "truck", truck);
        SetField(cameraVisual, "truckCameraTarget", truckCameraTarget);
        SetField(cameraVisual, "eveningCamera", eveningCamera);
        SetField(cameraVisual, "eveningCameraTarget", eveningCameraTarget);
        SetField(cameraVisual, "houseShotPoint", houseShotPoint);
        SetField(cameraVisual, "farmShotPoint", farmShotPoint);
        SetField(Object.FindAnyObjectByType<SoundManager>(FindObjectsInactive.Include), "truck", truck);

        WorldGlitchVisual glitch = director.gameObject.AddComponent<WorldGlitchVisual>();
        SetField(glitch, "director", day1);
        Material voidMaterial = GrayboxPalette.Unlit("GlitchVoid", new Color(.02f, .02f, .03f));
        SetField(glitch, "apparition", Apparition(director, voidMaterial, distributorNpc.GetComponentInChildren<Animator>(true).transform.lossyScale));
        SetArray(glitch, "corruptMaterialArray", new Object[]
        {
            voidMaterial,
            GrayboxPalette.Unlit("GlitchBlood", new Color(.42f, 0f, .03f)),
            GrayboxPalette.Unlit("GlitchBone", new Color(.93f, .91f, .86f)),
        });
        SerializedObject glitchSo = new SerializedObject(glitch);
        glitchSo.FindProperty("ignoredLayers").intValue = LayerMask.GetMask("Portrait", "Puzzle");
        glitchSo.ApplyModifiedPropertiesWithoutUndo();

        NightVisual night = director.gameObject.AddComponent<NightVisual>();
        SetField(night, "director", day1);
        SetField(night, "sun", GameObject.Find("Directional Light").GetComponent<Light>());
        SetField(night, "worldCamera", Camera.main);
        SetField(night, "doorLamp", doorLamp);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Day 1 graybox built. Press Play: the truck arrives (the bedroom is the HouseInterior scene).");
    }

    // ---------- clean-up ----------

    private static void RemovePreviousBuild()
    {
        foreach (string name in new[] { GRAYBOX_ROOT, DIRECTOR_ROOT, "LetterUI", "FabricatorHintUI", "FabricatorComboUI", "ScreenFadeUI" })
        {
            GameObject old = GameObject.Find(name);
            while (old != null)
            {
                Object.DestroyImmediate(old);
                old = GameObject.Find(name);
            }
        }
        // UI panels live on an inactive-safe canvas child: GameObject.Find skips inactive objects, so sweep those too
        foreach (LetterUI ui in Object.FindObjectsByType<LetterUI>(FindObjectsInactive.Include)) Object.DestroyImmediate(ui.gameObject);
        foreach (FabricatorComboUI ui in Object.FindObjectsByType<FabricatorComboUI>(FindObjectsInactive.Include)) Object.DestroyImmediate(ui.gameObject);
        foreach (ScreenFadeUI ui in Object.FindObjectsByType<ScreenFadeUI>(FindObjectsInactive.Include)) Object.DestroyImmediate(ui.gameObject);
    }

    // ---------- existing objects ----------

    // the old machine is a 3 m graybox tube: swap it for the 1.3 m Blender model and re-point everything that referenced it
    private static FabricatorController ReplaceFabricator()
    {
        FabricatorController old = Object.FindAnyObjectByType<FabricatorController>(FindObjectsInactive.Include);
        Transform parent = old != null ? old.transform.parent : GameObject.Find("Interactables").transform;
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject fabricatorGo = Spawn(InteractablePrefabBuilder.Fabricator(), parent, Level01SceneBuilder.FabricatorPosition, 180f);
        FabricatorController fabricator = fabricatorGo.GetComponent<FabricatorController>();
        FabricatorMiniGame miniGame = Object.FindAnyObjectByType<FabricatorMiniGame>(FindObjectsInactive.Include);
        SetField(fabricator, "miniGame", miniGame);
        SetField(Object.FindAnyObjectByType<FabricatorPuzzleCamera>(FindObjectsInactive.Include), "fabricator", fabricator);
        SetField(Object.FindAnyObjectByType<FabricatorUI>(FindObjectsInactive.Include), "fabricator", fabricator);
        SetField(Object.FindAnyObjectByType<FadeFlashUI>(FindObjectsInactive.Include), "fabricator", fabricator);
        return fabricator;
    }

    private static void PlaceFabricatorCamera()
    {
        GameObject cam = GameObject.Find("CM_Fabricator");
        if (cam == null) return;
        cam.transform.position = Level01SceneBuilder.FabricatorPosition + new Vector3(-1.7f, 1.5f, -2.1f);
        cam.transform.LookAt(Level01SceneBuilder.FabricatorPosition + new Vector3(0f, .65f, 0f));
    }

    // the dialogue box hides the NPC portrait during the player's monologues, and pops each frame for its speaker
    public static void PatchDialogueUi()
    {
        DialogueUI ui = Object.FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
        foreach (Transform t in ui.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "NpcPortrait") SetField(ui, "npcPortraitFrame", t.gameObject);
            if (t.name == "PlayerPortrait") SetField(ui, "playerPortraitFrame", t.gameObject);
        }
    }

    // characters get a CutsceneActor so the director can hop them around and ask for emotes
    public static CutsceneActor EnsureActor(GameObject character, Component visualComponent)
    {
        CutsceneActor actor = character.GetComponent<CutsceneActor>();
        if (actor == null) actor = character.AddComponent<CutsceneActor>();
        SetField(visualComponent, "cutsceneActor", actor);
        return actor;
    }

    // the Distributor's rig, solid void-black, hidden until the glitch flickers him in
    private static GameObject Apparition(Transform parent, Material voidMaterial, Vector3 scale)
    {
        GameObject apparition = (GameObject)PrefabUtility.InstantiatePrefab(CharacterBuilder.DistributorRigPrefab(), parent);
        apparition.name = "GlitchApparition";
        apparition.transform.localScale = scale;
        foreach (Renderer renderer in apparition.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++) materials[i] = voidMaterial;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        apparition.SetActive(false);
        return apparition;
    }

    // in front of his belly, where the Fabricator rides while he carries it (reused on rebuilds)
    private static Transform CarryPoint(Transform distributor)
    {
        Transform point = distributor.Find("CarryPoint");
        if (point == null) point = Root("CarryPoint", distributor).transform;
        point.SetLocalPositionAndRotation(new Vector3(0f, .45f, 1.35f), Quaternion.identity);
        return point;
    }

    private static Npc FindNpc(string objectName)
    {
        foreach (Npc npc in Object.FindObjectsByType<Npc>(FindObjectsInactive.Include))
            if (npc.name == objectName) return npc;
        Debug.LogError($"{objectName} is not in the scene: run Tools > Level01 > Rebuild Scene first");
        return null;
    }

    // ---------- truck, Sari, door lamp ----------

    private static DistributorTruck SpawnTruck(Transform graybox, Transform points)
    {
        GameObject go = Spawn(Day1PrefabBuilder.Truck(), graybox, TruckStart, 90f);
        go.name = "DistributorTruck";
        DistributorTruck truck = go.GetComponent<DistributorTruck>();
        SetField(truck, "startPoint", Point(points, "TruckStartPoint", TruckStart, 90f));
        SetField(truck, "stopPoint", Point(points, "TruckStopPoint", TruckStop, 90f));
        SetField(truck, "exitPoint", Point(points, "TruckExitPoint", TruckExit, 90f));
        SetField(truck, "bridge", GameObject.Find("Bridge").transform);
        return truck;
    }

    private static Npc SpawnSari(Transform graybox)
    {
        GameObject go = Spawn(Day1PrefabBuilder.Sari(Day1DataBuilder.Sari), graybox, SariPosition, 90f);
        return go.GetComponent<Npc>();
    }

    private static GameObject SpawnDoorLamp(Transform graybox)
    {
        GameObject doorLamp = Root("DoorLamp", graybox);
        doorLamp.transform.position = Level01SceneBuilder.PlayerHousePosition + new Vector3(0f, 2.8f, -4.2f);
        Light light = doorLamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 9f;
        light.intensity = 4f;
        light.color = new Color(1f, .78f, .5f);
        return doorLamp;
    }

    // ---------- helpers ----------

    private static Transform Point(Transform parent, string name, Vector3 position, float yaw)
    {
        GameObject go = Root(name, parent);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        return go.transform;
    }

    // rides alongside the target at a fixed world offset and keeps it centred
    private static CinemachineCamera ChaseCamera(Transform parent, string name, Transform target, Vector3 offset, float fov)
    {
        CinemachineCamera cam = FixedCamera(parent, name, target.position + offset, target.position, fov);
        cam.Follow = target;
        cam.LookAt = target;
        CinemachineFollow follow = cam.gameObject.AddComponent<CinemachineFollow>();
        follow.FollowOffset = offset;
        follow.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
        follow.TrackerSettings.PositionDamping = new Vector3(.6f, .6f, .6f);
        cam.gameObject.AddComponent<CinemachineRotationComposer>().Damping = new Vector2(.3f, .3f);
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
        lens.FarClipPlane = CAMERA_FAR_CLIP;
        cam.Lens = lens;
        return cam;
    }
}
