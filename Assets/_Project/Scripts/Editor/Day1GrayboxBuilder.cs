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

    private static readonly Vector3 TruckStart = new Vector3(-13f, 0f, .6f);
    private static readonly Vector3 TruckStop = new Vector3(9f, 0f, .6f);
    private static readonly Vector3 TruckExit = new Vector3(42f, 0f, .6f);
    private static readonly Vector3 DistributorStand = Level01SceneBuilder.FabricatorPosition + new Vector3(1.6f, 0f, -.7f);
    private static readonly Vector3 SariPosition = new Vector3(-17.2f, .34f, 0f);
    private static readonly Vector3 MotherPosition = Level01SceneBuilder.PlayerHousePosition + new Vector3(0f, .14f, -3.1f);

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
        Transform yardPoint = Point(points, "YardPoint", Level01SceneBuilder.PlayerSpawn, 0f);
        Transform distributorStand = Point(points, "DistributorStandPoint", DistributorStand, 180f);
        Transform nightPlayerPoint = Point(points, "NightPlayerPoint", Level01SceneBuilder.PlayerHousePosition + new Vector3(0f, 0f, -6.2f), 0f);

        DistributorTruck truck = SpawnTruck(graybox, points);
        Player player = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        EnsureActor(player.gameObject, player.GetComponentInChildren<PlayerVisual>());
        player.transform.SetPositionAndRotation(Level01SceneBuilder.PlayerSpawn, Quaternion.Euler(0f, -90f, 0f));

        Npc distributorNpc = FindNpc("Npc_Distributor");
        CutsceneActor distributorActor = EnsureActor(distributorNpc.gameObject, distributorNpc.GetComponentInChildren<NpcVisual>());
        distributorNpc.transform.position = DistributorStand;
        Npc sari = SpawnSari(graybox);
        GameObject mother = SpawnMother(graybox, out GameObject doorLamp);

        HouseDoor houseDoor = Object.FindAnyObjectByType<HouseDoor>(FindObjectsInactive.Include);
        PatchDialogueUi();
        Transform hud = Object.FindAnyObjectByType<HudUI>(FindObjectsInactive.Include).transform.parent;
        ScreenFadeUI fade = Day1UiBuilder.BuildVillage(hud, Object.FindAnyObjectByType<FabricatorMiniGame>(FindObjectsInactive.Include));

        CinemachineCamera truckCamera = FixedCamera(director, "CM_Truck", new Vector3(9f, 4.2f, -8.5f), new Vector3(10f, 1.1f, 3f), 38f);
        truckCamera.enabled = false;

        Day1Director day1 = director.gameObject.AddComponent<Day1Director>();
        SetField(day1, "day1SO", Day1DataBuilder.Day1);
        SetField(day1, "screenFade", fade);
        SetField(day1, "truck", truck);
        SetField(day1, "fabricator", fabricator);
        SetField(day1, "fabricatorDelivery", delivery);
        SetField(day1, "distributorNpc", distributorNpc);
        SetField(day1, "distributorActor", distributorActor);
        SetField(day1, "sariNpc", sari);
        SetField(day1, "motherGameObject", mother);
        SetField(day1, "houseDoor", houseDoor);
        SetField(day1, "yardPoint", yardPoint);
        SetField(day1, "distributorStandPoint", distributorStand);
        SetField(day1, "nightPlayerPoint", nightPlayerPoint);

        Day1CameraVisual cameraVisual = director.gameObject.AddComponent<Day1CameraVisual>();
        SetField(cameraVisual, "director", day1);
        SetField(cameraVisual, "truckCamera", truckCamera);

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
        foreach (string name in new[] { GRAYBOX_ROOT, DIRECTOR_ROOT, "LetterUI", "FabricatorHintUI", "ScreenFadeUI" })
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
        foreach (FabricatorHintUI ui in Object.FindObjectsByType<FabricatorHintUI>(FindObjectsInactive.Include)) Object.DestroyImmediate(ui.gameObject);
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

    // the dialogue box hides the NPC portrait during the player's monologues
    private static void PatchDialogueUi()
    {
        DialogueUI ui = Object.FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
        foreach (Transform t in ui.GetComponentsInChildren<Transform>(true))
            if (t.name == "NpcPortrait") SetField(ui, "npcPortraitFrame", t.gameObject);
    }

    // characters get a CutsceneActor so the director can hop them around and ask for emotes
    public static CutsceneActor EnsureActor(GameObject character, Component visualComponent)
    {
        CutsceneActor actor = character.GetComponent<CutsceneActor>();
        if (actor == null) actor = character.AddComponent<CutsceneActor>();
        SetField(visualComponent, "cutsceneActor", actor);
        return actor;
    }

    private static Npc FindNpc(string objectName)
    {
        foreach (Npc npc in Object.FindObjectsByType<Npc>(FindObjectsInactive.Include))
            if (npc.name == objectName) return npc;
        Debug.LogError($"{objectName} is not in the scene: run Tools > Level01 > Rebuild Scene first");
        return null;
    }

    // ---------- truck, Sari, Mother ----------

    private static DistributorTruck SpawnTruck(Transform graybox, Transform points)
    {
        GameObject go = Spawn(Day1PrefabBuilder.Truck(), graybox, TruckStart, 90f);
        go.name = "DistributorTruck";
        DistributorTruck truck = go.GetComponent<DistributorTruck>();
        SetField(truck, "startPoint", Point(points, "TruckStartPoint", TruckStart, 90f));
        SetField(truck, "stopPoint", Point(points, "TruckStopPoint", TruckStop, 90f));
        SetField(truck, "exitPoint", Point(points, "TruckExitPoint", TruckExit, 90f));
        return truck;
    }

    private static Npc SpawnSari(Transform graybox)
    {
        GameObject go = Spawn(Day1PrefabBuilder.Sari(Day1DataBuilder.Sari), graybox, SariPosition, 90f);
        return go.GetComponent<Npc>();
    }

    private static GameObject SpawnMother(Transform graybox, out GameObject doorLamp)
    {
        GameObject mother = Spawn(Day1PrefabBuilder.Mother(), graybox, MotherPosition, 180f);
        doorLamp = Root("DoorLamp", graybox);
        doorLamp.transform.position = Level01SceneBuilder.PlayerHousePosition + new Vector3(0f, 2.8f, -4.2f);
        Light light = doorLamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 9f;
        light.intensity = 4f;
        light.color = new Color(1f, .78f, .5f);
        return mother;
    }

    // ---------- helpers ----------

    private static Transform Point(Transform parent, string name, Vector3 position, float yaw)
    {
        GameObject go = Root(name, parent);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        return go.transform;
    }

    private static CinemachineCamera FixedCamera(Transform parent, string name, Vector3 position, Vector3 lookAt, float fov)
    {
        GameObject go = Root(name, parent);
        go.transform.position = position;
        go.transform.LookAt(lookAt);
        CinemachineCamera cam = go.AddComponent<CinemachineCamera>();
        LensSettings lens = LensSettings.Default;
        lens.FieldOfView = fov;
        lens.FarClipPlane = 200f;
        cam.Lens = lens;
        return cam;
    }
}
