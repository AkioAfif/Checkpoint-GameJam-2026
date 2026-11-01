using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static GrayboxBuild;

// The HouseInterior scene: the player's bedroom where Day 1 starts (wake up, read the letter, leave for the village).
// Prefabs: PlayerBedroom (floor, walls, window, door, light), Bed, BedsideTable, BedsideLetter. Re-runnable; it overwrites the scene.
public static class HouseInteriorBuilder
{
    public const string SCENE_PATH = "Assets/_Project/Scenes/HouseInterior.unity";
    private const string DAY1_SO_PATH = "Assets/_Project/ScriptableObjects/Day1/Day1.asset";
    private const string PLAYER_PREFAB_PATH = "Assets/_Project/Prefabs/Characters/Player.prefab";

    private static readonly Vector3 BedPosition = new Vector3(-1.6f, 0f, -.3f);
    private static readonly Vector3 TablePosition = new Vector3(-1.6f, 0f, 1.6f);
    private static readonly Vector3 BedSpawn = new Vector3(-1.6f, .56f, -.2f);

    private static int InteractableLayer => LayerMask.NameToLayer("Interactable");

    [MenuItem("Tools/Day1/Build House Interior")]
    public static void Build()
    {
        PlayerBedroom();
        Bed();
        BedsideTable();
        Letter();
        BedroomModelBuilder.Apply();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // NewScene unloads assets nobody references yet, so everything the scene points at is loaded after it
        GameDataBuilder.LoadAll();
        Day1SO day1SO = AssetDatabase.LoadAssetAtPath<Day1SO>(DAY1_SO_PATH);
        GameObject roomPrefab = LoadPrefab("Environment/PlayerBedroom"), bedPrefab = LoadPrefab("Environment/Bed");
        GameObject tablePrefab = LoadPrefab("Environment/BedsideTable"), letterPrefab = LoadPrefab("Interactables/BedsideLetter");
        Level01SceneBuilder.BuildLightingAndVolume();

        Transform bedroom = Root("Bedroom").transform;
        BedroomDoor bedroomDoor = Spawn(roomPrefab, bedroom, Vector3.zero).GetComponentInChildren<BedroomDoor>();
        Spawn(bedPrefab, bedroom, BedPosition);
        Spawn(tablePrefab, bedroom, TablePosition);
        BedsideLetter letter = Spawn(letterPrefab, bedroom, TablePosition + new Vector3(0f, .6f, 0f)).GetComponent<BedsideLetter>();

        Transform managers = Root("Managers").transform;
        managers.gameObject.AddComponent<GameInput>();
        managers.gameObject.AddComponent<GameManager>();
        Inventory inventory = managers.gameObject.AddComponent<Inventory>();
        SerializedObject so = new SerializedObject(inventory);
        SerializedProperty start = so.FindProperty("startingItemArray");
        start.arraySize = 1;
        start.GetArrayElementAtIndex(0).FindPropertyRelative("itemDataSO").objectReferenceValue = GameDataBuilder.EmptyBottle;
        start.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
        managers.gameObject.AddComponent<DialogueManager>();
        // the player's visual listens to these even indoors
        managers.gameObject.AddComponent<QTEManager>();
        SetField(managers.gameObject.AddComponent<PlantingController>(), "cropListSO", AssetDatabase.LoadAssetAtPath<CropListSO>("Assets/_Project/ScriptableObjects/Crops/CropList.asset"));
        SetArray(managers.gameObject.AddComponent<TaskManager>(), "objectiveSOArray", day1SO.letterObjectiveArray);

        // before the player: it re-saves the character rig prefab, which resets the player instance's position
        Level01SceneBuilder.BuildPortraitBooth();
        GameObject player = Spawn(AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH), null, BedSpawn, 180f);
        CutsceneActor playerActor = Day1GrayboxBuilder.EnsureActor(player, player.GetComponentInChildren<PlayerVisual>());

        BuildCamera();
        UiBuilder.BuildLevelHud(new UiBuilder.Refs
        {
            playerPortrait = Level01SceneBuilder.PortraitTexture("Portrait_Player"),
            npcPortrait = Level01SceneBuilder.PortraitTexture("Portrait_Npc"),
        });
        Transform hud = Object.FindAnyObjectByType<HudUI>(FindObjectsInactive.Include).transform.parent;
        ScreenFadeUI fade = Day1UiBuilder.BuildBedroom(hud, letter, day1SO);

        BedroomDirector director = Root("BedroomDirector").AddComponent<BedroomDirector>();
        SetField(director, "day1SO", day1SO);
        SetField(director, "chapterListSO", ChapterDataBuilder.Build());
        SetField(director, "bedsideLetter", letter);
        SetField(director, "bedroomDoor", bedroomDoor);
        SetField(director, "screenFade", fade);
        SetField(director, "playerActor", playerActor);

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SCENE_PATH);
        AssetDatabase.SaveAssets();
        Debug.Log("HouseInterior built. Press Play in it: you wake up in the bedroom, the door leads to Level01.");
    }

    // fixed shot through the open south side of the room
    private static void BuildCamera()
    {
        GameObject cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.80f, .88f, .93f);
        camera.cullingMask = ~LayerMask.GetMask("Portrait", "Puzzle");
        camera.fieldOfView = 46f;
        camera.farClipPlane = CAMERA_FAR_CLIP;
        cameraGo.AddComponent<AudioListener>();
        cameraGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        cameraGo.transform.position = new Vector3(.9f, 3.3f, -4.6f);
        cameraGo.transform.LookAt(new Vector3(-.2f, .6f, .3f));
    }

    // ---------- prefabs ----------

    // the shell: floor, walls (south side open for the camera, an invisible wall keeps the player in), window, locked door, ceiling light
    private static GameObject PlayerBedroom()
    {
        GameObject root = Root("PlayerBedroom");
        Transform room = root.transform;
        Material wall = GrayboxPalette.Wall;

        Part(room, "Floor", GrayboxMeshFactory.RoundedBox(new Vector3(5.6f, .3f, 4.6f), .06f), GrayboxPalette.Wood, new Vector3(0f, -.3f, 0f));
        Box(root, new Vector3(0f, -.15f, 0f), new Vector3(5.6f, .3f, 4.6f));
        Wall(room, "WallNorth", wall, new Vector3(0f, 1.4f, 2.2f), new Vector3(5.2f, 2.8f, .2f));
        Wall(room, "WallWest", wall, new Vector3(-2.6f, 1.4f, 0f), new Vector3(.2f, 2.8f, 4.6f));
        Wall(room, "WallEast", wall, new Vector3(2.6f, 1.4f, 0f), new Vector3(.2f, 2.8f, 4.6f));
        Box(root, new Vector3(0f, 1.4f, -2.3f), new Vector3(5.2f, 2.8f, .2f));
        Part(room, "Window", GrayboxMeshFactory.RoundedBox(new Vector3(.12f, 1f, 1.2f), .04f), GrayboxPalette.Window, new Vector3(2.5f, 1.2f, .4f));
        BuildDoor(room, new Vector3(1.2f, 0f, 2.1f));

        GameObject lightGo = Root("BedroomLight", room);
        lightGo.transform.localPosition = new Vector3(0f, 2.3f, -.3f);
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 8f;
        light.intensity = 3.2f;
        light.color = new Color(1f, .92f, .78f);
        return SavePrefab(root, "Environment", "PlayerBedroom");
    }

    // head to the north (+Z), pivot on the floor at the bed's centre
    private static GameObject Bed()
    {
        GameObject root = Root("Bed");
        Transform bed = root.transform;
        Material wood = GrayboxPalette.WoodDark;
        Part(bed, "BedFrame", GrayboxMeshFactory.RoundedBox(new Vector3(1.3f, .3f, 2.2f), .06f), wood, Vector3.zero);
        Part(bed, "Mattress", GrayboxMeshFactory.RoundedBox(new Vector3(1.2f, .22f, 2f), .1f), GrayboxPalette.Lit("BedSheet", new Color(.70f, .82f, .92f)), new Vector3(0f, .3f, 0f));
        Part(bed, "Pillow", GrayboxMeshFactory.RoundedBox(new Vector3(.8f, .14f, .45f), .06f), GrayboxPalette.Wall, new Vector3(0f, .52f, .92f));
        Part(bed, "Headboard", GrayboxMeshFactory.RoundedBox(new Vector3(1.3f, .9f, .12f), .05f), wood, new Vector3(0f, 0f, 1.15f));
        Box(root, new Vector3(0f, .26f, 0f), new Vector3(1.3f, .52f, 2.2f));
        return SavePrefab(root, "Environment", "Bed");
    }

    private static GameObject BedsideTable()
    {
        GameObject root = Root("BedsideTable");
        Part(root.transform, "Table", GrayboxMeshFactory.RoundedBox(new Vector3(.6f, .6f, .6f), .06f), GrayboxPalette.WoodDark, Vector3.zero);
        Box(root, new Vector3(0f, .3f, 0f), new Vector3(.6f, .6f, .6f));
        return SavePrefab(root, "Environment", "BedsideTable");
    }

    // pivot at the paper's underside: place it on top of a table
    private static GameObject Letter()
    {
        GameObject go = Root("BedsideLetter");
        go.layer = InteractableLayer;
        BedsideLetter letter = go.AddComponent<BedsideLetter>();
        Box(go, new Vector3(0f, .3f, 0f), new Vector3(1f, 1.2f, 1.1f), true);
        Transform visual = Root("Visual", go.transform).transform;
        Part(visual, "Paper", GrayboxMeshFactory.RoundedBox(new Vector3(.34f, .02f, .24f), .008f), GrayboxPalette.Lit("LetterPaper", new Color(.98f, .95f, .86f)), new Vector3(0f, .01f, 0f), new Vector3(0f, 18f, 0f));
        Part(visual, "Seal", GrayboxMeshFactory.Blob(.04f, .03f, 8, 4), GrayboxPalette.Lit("LetterSeal", new Color(.78f, .22f, .20f)), new Vector3(.02f, .03f, 0f));
        AddOutline(go, visual, .02f);
        SetField(letter, "promptAnchor", Anchor(go, "PromptAnchor", new Vector3(0f, 1.1f, 0f)));
        return SavePrefab(go, "Interactables", "BedsideLetter");
    }

    private static void BuildDoor(Transform room, Vector3 localPosition)
    {
        GameObject go = Root("BedroomDoor", room);
        go.transform.localPosition = localPosition;
        go.layer = InteractableLayer;
        BedroomDoor door = go.AddComponent<BedroomDoor>();
        Box(go, new Vector3(0f, 1f, -.5f), new Vector3(1.6f, 2f, 1.6f), true);
        Transform visual = Root("Visual", go.transform).transform;
        Part(visual, "Frame", GrayboxMeshFactory.RoundedBox(new Vector3(1.4f, 2.15f, .16f), .06f), GrayboxPalette.WoodDark, Vector3.zero);
        Part(visual, "Door", GrayboxMeshFactory.RoundedBox(new Vector3(1.1f, 1.95f, .16f), .06f), GrayboxPalette.Wood, new Vector3(0f, 0f, -.08f));
        Part(visual, "Knob", GrayboxMeshFactory.Blob(.07f, .14f, 10, 5), GrayboxPalette.Accent, new Vector3(.38f, .95f, -.2f));
        AddOutline(go, visual, .05f);
        SetField(door, "promptAnchor", Anchor(go, "PromptAnchor", new Vector3(0f, 2.1f, -.4f)));
    }

    // ---------- helpers ----------

    private static GameObject LoadPrefab(string pathInPrefabs) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/{pathInPrefabs}.prefab");

    private static void Wall(Transform room, string name, Material material, Vector3 centre, Vector3 size)
    {
        GameObject wall = Part(room, name, GrayboxMeshFactory.RoundedBox(size, .05f), material, centre - new Vector3(0f, size.y * .5f, 0f));
        Box(wall, new Vector3(0f, size.y * .5f, 0f), size);
    }

    private static Transform Anchor(GameObject parent, string name, Vector3 localPosition)
    {
        GameObject anchor = Root(name, parent.transform);
        anchor.transform.localPosition = localPosition;
        return anchor.transform;
    }

    private static void AddOutline(GameObject root, Transform visual, float thickness)
    {
        InteractableOutlineVisual outline = root.AddComponent<InteractableOutlineVisual>();
        SetField(outline, "interactableGameObject", root);
        SetArray(outline, "outlineGameObjectArray", AddOutlineHulls(visual, GrayboxPalette.Outline, thickness));
    }
}
