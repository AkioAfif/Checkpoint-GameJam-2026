using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static GrayboxBuild;

// Builds the title screen as MainMenu (Tools > MainMenu > Build Main Menu Scene). It copies FixScene for the CozyFarm map,
// its NavMesh and lighting, strips Day 1's gameplay, and adds:
// - six villagers (bare rigs, no dialogue) looping through activity spots at time-lapse speed (MenuTimeLapse);
// - a CinemachineCamera drifting from the east (the bridge) to the west (the playground) over the rooftops (MenuCameraPan);
// - the menu UI (title, Start / Settings / Quit, the settings card).
// Re-runnable: MainMenu is regenerated each time and is put first in the build list (the game starts there).
public static class MainMenuBuilder
{
    public const string SCENE_PATH = "Assets/_Project/Scenes/MainMenu.unity";
    private const string SOURCE_SCENE_PATH = "Assets/_Project/Scenes/FixScene.unity";
    private const string GAMEPLAY_ROOT = "Day1_Gameplay";
    private const string TITLE = "Checkpoint";
    private const string SUBTITLE = "A village that never sleeps";

    // the gameplay camera looks south (yaw 180); the pan keeps that view, high enough to see over the rooftops
    private static readonly Vector3 PanEastPosition = new Vector3(-2f, 21f, 36f);
    private static readonly Vector3 PanWestPosition = new Vector3(-58f, 21f, 30f);
    private static readonly Vector3 PanEuler = new Vector3(42f, 180f, 0f);
    private const float PAN_FOV = 45f;
    private static readonly Color CardColor = new Color(UiBuilder.Cream.r, UiBuilder.Cream.g, UiBuilder.Cream.b, .45f);

    // Rigel's garden (Rival_Property_Garden): 5 x 3 beds, 2.32 m apart; he works the two rows nearest the camera (north)
    private static readonly float[] GardenBedXArray = { -18.12f, -15.8f, -13.47f, -11.16f, -8.85f };
    private static readonly float[] GardenBedZArray = { 26.65f, 24.33f };
    // he stands on the south side of a bed, facing it and the camera
    private const float GARDEN_STAND_OFFSET = 1.15f;
    private const float GARDEN_WORK_DURATION = 3f;
    private const string CROP_FOLDER = PREFABS + "/Items";

    private struct Spot
    {
        public Vector3 position;
        public float yaw;
        public Emote emote;
        public float duration;
        public bool hasPlot;
        public Vector3 plotPosition;
        public Spot(float x, float z, float yaw, Emote emote, float duration)
        {
            position = new Vector3(x, 0f, z);
            this.yaw = yaw;
            this.emote = emote;
            this.duration = duration;
            hasPlot = false;
            plotPosition = Vector3.zero;
        }
    }

    // yaw 0 faces north, toward the camera; durations are villager seconds (divide by the time-lapse for real time)
    private static readonly (string name, Spot[] spotArray)[] VillagerArray =
    {
        ("Darto", new[] { new Spot(-8f, 2.6f, 0f, Emote.Dig, 9f), new Spot(-2f, -2f, 90f, Emote.Wave, 4f), new Spot(-10f, 5f, 20f, Emote.Nod, 5f) }),
        ("Rigel", RigelGardenSpots()),
        ("Ratna", new[] { new Spot(-27f, -5f, 0f, Emote.Nod, 6f), new Spot(-22f, -2f, 30f, Emote.TiltHead, 4f), new Spot(-31f, 5f, 0f, Emote.Wave, 4f) }),
        ("Ijah", new[] { new Spot(-35f, 6f, 0f, Emote.Wave, 5f), new Spot(-36.6f, 11f, -20f, Emote.Nod, 4f), new Spot(-41f, 13f, 0f, Emote.TiltHead, 4f) }),
        ("Sari", new[] { new Spot(-44f, 13f, 0f, Emote.Wave, 4f), new Spot(-46f, 2f, 60f, Emote.Nod, 4f), new Spot(-29f, 12f, 0f, Emote.Cheer, 5f) }),
        ("Dimas", new[] { new Spot(-52f, -2f, 0f, Emote.Hop, 4f), new Spot(-58.6f, -4.4f, 0f, Emote.GoofyFlip, 5f), new Spot(-53.6f, -7.6f, 20f, Emote.Cheer, 4f), new Spot(-50.6f, -5.5f, 0f, Emote.Dig, 6f) }),
    };

    [MenuItem("Tools/MainMenu/Build Main Menu Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // overwrite the file, not delete + copy: the scene keeps its GUID
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) == null) AssetDatabase.CopyAsset(SOURCE_SCENE_PATH, SCENE_PATH);
        else
        {
            System.IO.File.Copy(SOURCE_SCENE_PATH, SCENE_PATH, true);
            AssetDatabase.ImportAsset(SCENE_PATH, ImportAssetOptions.ForceUpdate);
        }
        EditorSceneManager.OpenScene(SCENE_PATH);

        Camera mainCamera = StripGameplay();
        Transform managers = Root("MenuManagers").transform;
        managers.gameObject.AddComponent<GameInput>();
        managers.gameObject.AddComponent<MenuTimeLapse>();
        // Day 6's tear (MutationSpreadVisual's red and black) plus the blood of the other days' world glitch
        SetArray(managers.gameObject.AddComponent<MenuGlitchVisual>(), "corruptMaterialArray", new Object[]
        {
            GrayboxPalette.Unlit("MutantPulse", new Color(.62f, .05f, .1f)),
            GrayboxPalette.Unlit("GlitchVoid", new Color(.02f, .02f, .03f)),
            GrayboxPalette.Unlit("GlitchBlood", new Color(.42f, 0f, .03f)),
        });
        BuildVillagers();
        Transform canvas = UiBuilder.Canvas("Canvas");
        ScreenFadeUI fade = BuildFade(canvas);
        BuildCamera(mainCamera, fade);
        SettingsUI settingsUI = BuildSettings(canvas);
        BuildMainPanel(canvas, settingsUI);
        // the settings card above the main panel
        settingsUI.transform.SetAsLastSibling();
        UiBuilder.BuildEventSystem();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AddFirstToBuildProfile(SCENE_PATH);
        Debug.Log($"Built {SCENE_PATH}");
    }

    // keeps the map, its NavMesh, the sun, the volume and the border; returns the kept Main Camera
    private static Camera StripGameplay()
    {
        Transform gameplay = GameObject.Find(GAMEPLAY_ROOT).transform;
        Transform volume = gameplay.Find("Global Volume");
        Transform cameraTransform = gameplay.Find("Main Camera");
        volume.SetParent(null, true);
        cameraTransform.SetParent(null, true);
        foreach (CameraOcclusionVisual occlusion in cameraTransform.GetComponents<CameraOcclusionVisual>()) Object.DestroyImmediate(occlusion);
        Object.DestroyImmediate(gameplay.gameObject);
        // the river's current, prompt and fill-a-bottle interactable need the player; switched off, the water visuals
        // still read the current's speed and keep flowing
        foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            if (behaviour is River || behaviour is RiverPromptAnchor || behaviour is ResourceNode || behaviour is InteractableOutlineVisual) behaviour.enabled = false;
        return cameraTransform.GetComponent<Camera>();
    }

    private static void BuildVillagers()
    {
        Transform root = Root("MenuVillagers").transform;
        foreach ((string name, Spot[] spotArray) in VillagerArray)
        {
            GameObject npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/Characters/Npc_{name}.prefab");
            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/Characters/Rig_{name}.prefab");
            if (npcPrefab == null || rigPrefab == null)
            {
                Debug.LogError($"Npc_{name} or Rig_{name} prefab not found");
                continue;
            }
            NavMeshAgent npcAgent = npcPrefab.GetComponent<NavMeshAgent>();
            Transform npcVisual = npcPrefab.transform.Find("Visual");

            Transform spots = Root($"{name}_Spots", root).transform;
            Transform[] pointArray = new Transform[spotArray.Length];
            MenuGardenPlot[] plotArray = new MenuGardenPlot[spotArray.Length];
            for (int i = 0; i < spotArray.Length; i++)
            {
                Transform point = Root($"Spot{i}_{spotArray[i].emote}", spots).transform;
                point.position = OnNavMesh(spotArray[i].position);
                point.rotation = Quaternion.Euler(0f, spotArray[i].yaw, 0f);
                pointArray[i] = point;
                if (spotArray[i].hasPlot) plotArray[i] = BuildPlot(spots, $"Bed{i}", spotArray[i].plotPosition, i);
            }

            GameObject villager = Root($"Villager_{name}", root);
            villager.transform.SetPositionAndRotation(pointArray[0].position, pointArray[0].rotation);
            NavMeshAgent agent = villager.AddComponent<NavMeshAgent>();
            agent.radius = npcAgent.radius;
            agent.height = npcAgent.height;
            agent.speed = npcAgent.speed;
            agent.angularSpeed = npcAgent.angularSpeed;
            agent.acceleration = npcAgent.acceleration;
            agent.stoppingDistance = npcAgent.stoppingDistance;
            MenuVillager menuVillager = villager.AddComponent<MenuVillager>();
            SetActivities(menuVillager, pointArray, plotArray, spotArray);

            GameObject visual = Root("Visual", villager.transform);
            visual.transform.localScale = npcVisual != null ? npcVisual.localScale : Vector3.one;
            GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, visual.transform);
            MenuVillagerVisual menuVillagerVisual = visual.AddComponent<MenuVillagerVisual>();
            SetField(menuVillagerVisual, "menuVillager", menuVillager);
            SetField(menuVillagerVisual, "animator", rig.GetComponent<Animator>());
        }
    }

    // a snake through the beds (west to east on the first row, back on the second), digging at each: an empty bed gets
    // planted, a ripe one harvested
    private static Spot[] RigelGardenSpots()
    {
        Spot[] spotArray = new Spot[GardenBedXArray.Length * GardenBedZArray.Length];
        int index = 0;
        for (int row = 0; row < GardenBedZArray.Length; row++)
            for (int column = 0; column < GardenBedXArray.Length; column++)
            {
                float x = GardenBedXArray[row % 2 == 0 ? column : GardenBedXArray.Length - 1 - column];
                float z = GardenBedZArray[row];
                Spot spot = new Spot(x, z - GARDEN_STAND_OFFSET, 0f, Emote.Dig, GARDEN_WORK_DURATION);
                spot.hasPlot = true;
                spot.plotPosition = new Vector3(x, 0f, z);
                spotArray[index++] = spot;
            }
        return spotArray;
    }

    // the bed's three plant stages (the farm's duskbell models), switched by MenuGardenPlotVisual; the first beds start
    // part-grown so the garden isn't bare when the menu opens
    private static MenuGardenPlot BuildPlot(Transform parent, string name, Vector3 position, int index)
    {
        GameObject bed = Root(name, parent);
        bed.transform.position = OnGarden(position);
        GameObject sprout = Spawn(AssetDatabase.LoadAssetAtPath<GameObject>($"{CROP_FOLDER}/Sapling_DuskbellLv1.prefab"), bed.transform, bed.transform.position, 180f);
        GameObject grown = Spawn(AssetDatabase.LoadAssetAtPath<GameObject>($"{CROP_FOLDER}/Plant_DuskbellLv1_Grown.prefab"), bed.transform, bed.transform.position, 180f);
        GameObject ripe = Spawn(AssetDatabase.LoadAssetAtPath<GameObject>($"{CROP_FOLDER}/Plant_DuskbellLv1_Ripe.prefab"), bed.transform, bed.transform.position, 180f);
        MenuGardenPlot plot = bed.AddComponent<MenuGardenPlot>();
        SerializedObject plotSo = new SerializedObject(plot);
        plotSo.FindProperty("startState").enumValueIndex = index % 4;
        plotSo.ApplyModifiedPropertiesWithoutUndo();
        MenuGardenPlotVisual visual = bed.AddComponent<MenuGardenPlotVisual>();
        SetField(visual, "plot", plot);
        SetField(visual, "sproutGameObject", sprout);
        SetField(visual, "grownGameObject", grown);
        SetField(visual, "ripeGameObject", ripe);
        return plot;
    }

    // the top of the bed's soil (the garden is one mesh, raised a little off the terrain)
    private static Vector3 OnGarden(Vector3 position)
    {
        float best = 0f;
        foreach (RaycastHit hit in Physics.RaycastAll(position + Vector3.up * 10f, Vector3.down, 20f))
            if (hit.collider.name.Contains("Garden") || hit.collider.name.Contains("Terrain")) best = Mathf.Max(best, hit.point.y);
        return new Vector3(position.x, best, position.z);
    }

    private static void SetActivities(MenuVillager menuVillager, Transform[] pointArray, MenuGardenPlot[] plotArray, Spot[] spotArray)
    {
        SerializedObject so = new SerializedObject(menuVillager);
        SerializedProperty array = so.FindProperty("activityArray");
        array.arraySize = spotArray.Length;
        for (int i = 0; i < spotArray.Length; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("point").objectReferenceValue = pointArray[i];
            element.FindPropertyRelative("plot").objectReferenceValue = plotArray[i];
            element.FindPropertyRelative("emote").intValue = (int)spotArray[i].emote;
            element.FindPropertyRelative("duration").floatValue = spotArray[i].duration;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Vector3 OnNavMesh(Vector3 position)
    {
        if (NavMesh.SamplePosition(position + Vector3.up, out NavMeshHit hit, 6f, NavMesh.AllAreas)) return hit.position;
        Debug.LogWarning($"No NavMesh near {position}");
        return position;
    }

    private static void BuildCamera(Camera mainCamera, ScreenFadeUI fade)
    {
        if (mainCamera.GetComponent<CinemachineBrain>() == null) mainCamera.gameObject.AddComponent<CinemachineBrain>();
        Transform cameras = Root("MenuCameras").transform;
        Transform east = Root("MenuPan_East", cameras).transform;
        east.SetPositionAndRotation(PanEastPosition, Quaternion.Euler(PanEuler));
        Transform west = Root("MenuPan_West", cameras).transform;
        west.SetPositionAndRotation(PanWestPosition, Quaternion.Euler(PanEuler));

        GameObject go = Root("CM_MenuPan", cameras);
        go.transform.SetPositionAndRotation(PanEastPosition, Quaternion.Euler(PanEuler));
        CinemachineCamera cinemachineCamera = go.AddComponent<CinemachineCamera>();
        LensSettings lens = cinemachineCamera.Lens;
        lens.FieldOfView = PAN_FOV;
        cinemachineCamera.Lens = lens;
        MenuCameraPan pan = go.AddComponent<MenuCameraPan>();
        SetField(pan, "eastPoint", east);
        SetField(pan, "westPoint", west);
        SetField(pan, "screenFadeUI", fade);
        mainCamera.transform.SetPositionAndRotation(PanEastPosition, Quaternion.Euler(PanEuler));
    }

    // first child of the canvas: the village fades between pans, the menu stays on top
    private static ScreenFadeUI BuildFade(Transform canvas)
    {
        Image black = UiBuilder.Img(canvas, "ScreenFadeUI", null, Color.black, Vector2.zero, Vector2.one, UiBuilder.Centre, Vector2.zero, Vector2.zero);
        black.gameObject.AddComponent<CanvasGroup>();
        TextMeshProUGUI caption = UiBuilder.Text(black.rectTransform, "Caption", string.Empty, 84, UiBuilder.Cream, TextAlignmentOptions.Center, FontStyles.Bold);
        UiBuilder.Stretch(caption.rectTransform, Vector2.zero, Vector2.zero);
        ScreenFadeUI fade = black.gameObject.AddComponent<ScreenFadeUI>();
        SetField(fade, "captionText", caption);
        return fade;
    }

    private static void BuildMainPanel(Transform canvas, SettingsUI settingsUI)
    {
        RectTransform root = UiBuilder.Rect(canvas, "MainMenuUI", Vector2.zero, Vector2.one, UiBuilder.Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        // centred, see-through so the busy village shows behind the menu
        RectTransform card = UiBuilder.Panel(root, "Card", UiBuilder.Centre, UiBuilder.Centre, UiBuilder.Centre, Vector2.zero, new Vector2(620f, 640f), CardColor);
        TextMeshProUGUI title = UiBuilder.Text(card, "TitleText", TITLE, 96, UiBuilder.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        UiBuilder.Place(title.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -100f), new Vector2(580f, 120f));
        TextMeshProUGUI subtitle = UiBuilder.Text(card, "SubtitleText", SUBTITLE, 32, UiBuilder.Ink, TextAlignmentOptions.Center, FontStyles.Italic);
        UiBuilder.Place(subtitle.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -180f), new Vector2(580f, 50f));
        Button start = UiBuilder.Button(card, "StartButton", "Start", new Vector2(0f, 250f), new Vector2(420f, 80f));
        Button settings = UiBuilder.Button(card, "SettingsButton", "Settings", new Vector2(0f, 150f), new Vector2(420f, 80f));
        Button quit = UiBuilder.Button(card, "QuitButton", "Quit", new Vector2(0f, 50f), new Vector2(420f, 80f));
        MainMenuUI ui = root.gameObject.AddComponent<MainMenuUI>();
        SetField(ui, "startButton", start);
        SetField(ui, "settingsButton", settings);
        SetField(ui, "quitButton", quit);
        SetField(ui, "settingsUI", settingsUI);
    }

    private static SettingsUI BuildSettings(Transform canvas)
    {
        RectTransform root = UiBuilder.Rect(canvas, "SettingsUI", Vector2.zero, Vector2.one, UiBuilder.Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        Image dim = UiBuilder.Img(root, "Dim", null, new Color(.15f, .12f, .12f, .55f), Vector2.zero, Vector2.one, UiBuilder.Centre, Vector2.zero, Vector2.zero);
        dim.raycastTarget = true;
        RectTransform card = UiBuilder.Panel(root, "Card", UiBuilder.Centre, UiBuilder.Centre, UiBuilder.Centre, Vector2.zero, new Vector2(640f, 640f), UiBuilder.Cream);
        TextMeshProUGUI header = UiBuilder.Text(card, "Header", "Settings", 52, UiBuilder.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        UiBuilder.Place(header.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -70f), new Vector2(580f, 70f));

        TextMeshProUGUI volumeLabel = UiBuilder.Text(card, "VolumeLabel", "Volume", 32, UiBuilder.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        UiBuilder.Place(volumeLabel.rectTransform, new Vector2(.5f, 1f), new Vector2(-170f, -165f), new Vector2(200f, 50f));
        Slider slider = BuildSlider(card);

        TextMeshProUGUI fullscreenLabel = UiBuilder.Text(card, "FullscreenLabel", "Fullscreen", 32, UiBuilder.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        UiBuilder.Place(fullscreenLabel.rectTransform, new Vector2(.5f, 1f), new Vector2(-170f, -245f), new Vector2(200f, 50f));
        Toggle toggle = BuildToggle(card);

        Button rebind = UiBuilder.Button(card, "RebindButton", "Interact key: [E]", new Vector2(0f, 240f), new Vector2(460f, 74f));
        Button reset = UiBuilder.Button(card, "ResetButton", "Reset keys", new Vector2(0f, 145f), new Vector2(460f, 74f));
        Button back = UiBuilder.Button(card, "BackButton", "Back", new Vector2(0f, 40f), new Vector2(460f, 74f));
        RectTransform overlay = UiBuilder.Panel(card, "PressKeyOverlay", Vector2.zero, Vector2.one, UiBuilder.Centre, Vector2.zero, Vector2.zero, UiBuilder.Accent);
        TextMeshProUGUI overlayText = UiBuilder.Text(overlay, "Text", "Press a key for Interact...\n<size=26>(Esc to cancel)</size>", 38, UiBuilder.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        UiBuilder.Stretch(overlayText.rectTransform, Vector2.zero, Vector2.zero);

        SettingsUI ui = root.gameObject.AddComponent<SettingsUI>();
        SetField(ui, "volumeSlider", slider);
        SetField(ui, "fullscreenToggle", toggle);
        SetField(ui, "rebindButton", rebind);
        SetField(ui, "resetButton", reset);
        SetField(ui, "rebindButtonText", rebind.GetComponentInChildren<TextMeshProUGUI>());
        SetField(ui, "pressKeyOverlay", overlay.gameObject);
        SetField(ui, "backButton", back);
        return ui;
    }

    private static Slider BuildSlider(RectTransform card)
    {
        RectTransform rect = UiBuilder.Rect(card, "VolumeSlider", new Vector2(.5f, 1f), new Vector2(.5f, 1f), UiBuilder.Centre, new Vector2(110f, -165f), new Vector2(300f, 36f));
        Image background = UiBuilder.Panel(rect, "Background", Vector2.zero, Vector2.one, UiBuilder.Centre, Vector2.zero, Vector2.zero, new Color(.85f, .80f, .72f)).GetComponent<Image>();
        RectTransform fillArea = UiBuilder.Rect(rect, "FillArea", Vector2.zero, Vector2.one, UiBuilder.Centre, Vector2.zero, Vector2.zero);
        UiBuilder.Stretch(fillArea, new Vector2(10f, 6f), new Vector2(-10f, -6f));
        RectTransform fill = UiBuilder.Panel(fillArea, "Fill", Vector2.zero, Vector2.one, UiBuilder.Centre, Vector2.zero, Vector2.zero, UiBuilder.Accent);
        UiBuilder.Stretch(fill, Vector2.zero, Vector2.zero);
        RectTransform handleArea = UiBuilder.Rect(rect, "HandleArea", Vector2.zero, Vector2.one, UiBuilder.Centre, Vector2.zero, Vector2.zero);
        UiBuilder.Stretch(handleArea, new Vector2(18f, 0f), new Vector2(-18f, 0f));
        RectTransform handle = UiBuilder.Panel(handleArea, "Handle", new Vector2(0f, 0f), new Vector2(0f, 1f), UiBuilder.Centre, Vector2.zero, new Vector2(36f, 12f), UiBuilder.Ink);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.raycastTarget = true;
        background.raycastTarget = true;
        Slider slider = rect.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    private static Toggle BuildToggle(RectTransform card)
    {
        RectTransform box = UiBuilder.Panel(card, "FullscreenToggle", new Vector2(.5f, 1f), new Vector2(.5f, 1f), UiBuilder.Centre, new Vector2(0f, -245f), new Vector2(56f, 56f), new Color(.85f, .80f, .72f));
        Image boxImage = box.GetComponent<Image>();
        boxImage.raycastTarget = true;
        RectTransform check = UiBuilder.Panel(box, "Checkmark", Vector2.zero, Vector2.one, UiBuilder.Centre, Vector2.zero, Vector2.zero, UiBuilder.Accent);
        UiBuilder.Stretch(check, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        Toggle toggle = box.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = true;
        return toggle;
    }

    private static void AddFirstToBuildProfile(string scenePath)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Length > 0 && scenes[0].path == scenePath) return;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) }.Concat(scenes.Where(s => s.path != scenePath)).ToArray();
    }
}
