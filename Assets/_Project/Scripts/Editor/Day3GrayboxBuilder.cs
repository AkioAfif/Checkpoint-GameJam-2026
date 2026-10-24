using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static GrayboxBuild;
using static UiBuilder;

// Builds "Day 3: Room to Grow" as its own scene, Level01_Day3 (Tools > Day3 > Build Day 3 Scene). It copies Level01_Day2,
// keeps its village, crate, Rigel and resin stumps (Day2_Graybox), swaps Day 2's director for Day 3's (Day3), removes Darto,
// and adds the empty grass between the field and the forest, where the MC pounds four plots out of the ground: the sunk
// plots, their blinking ghosts, the crater, the flashback's Distributor and grayscale Volume, the cutscene cameras and the
// sequence's UI (Day3_World + Day3). Re-runnable;
// Level01_Day3 is regenerated each time, so run it after rebuilding Day 2. It also wires Day 3 into the HouseInterior bedroom.
public static class Day3GrayboxBuilder
{
    public const string SCENE_PATH = "Assets/_Project/Scenes/Level01_Day3.unity";
    private const string WORLD_ROOT = "Day3_World";
    private const string DIRECTOR_ROOT = "Day3";
    private const string DAY2_DIRECTOR_ROOT = "Day2";
    private const string BEDROOM_DIRECTOR_PATH = "BedroomDirector";

    private const string FLASHBACK_PROFILE_PATH = "Assets/_Project/Art/Day3_FlashbackProfile.asset";

    // the new strip: between the field's north edge (z 9.8) and the forest edge (z 14.5)
    private static readonly Vector3 StripCentre = new Vector3(23f, 0f, 12.3f);
    private const float PLOT_SPACING = 2.25f;
    private const int PLOT_COUNT = 4;
    // the strip's west end, looking along it toward the trees
    private static readonly Vector3 EveningPosition = new Vector3(18.4f, 0f, 12.3f);
    // just south of the strip, facing south (the camera), so the plots rise behind the MC
    private static readonly Vector3 SmashPosition = new Vector3(23f, 0f, 10.8f);
    // low under the canopy of the field's last palm row (trunks at x 20.5 / 23 / 25.5, z 8), looking the MC in the face
    private static readonly Vector3 SmashCameraPosition = new Vector3(24.4f, 1.05f, 8.55f);
    private static readonly Vector3 SmashCloseCameraPosition = new Vector3(24.2f, .75f, 8.9f);
    // in front of Rigel's door (his house at (25, -16) faces north)
    private static readonly Vector3 RigelHouseDoorPosition = new Vector3(25f, 0f, -13.8f);

    [MenuItem("Tools/Day3/Build Day 3 Scene")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Day2GrayboxBuilder.SCENE_PATH) == null)
        {
            Debug.LogError($"{Day2GrayboxBuilder.SCENE_PATH} is missing: run Tools > Day2 > Build Day 2 Scene first.");
            return;
        }

        AssetDatabase.DeleteAsset(SCENE_PATH);
        AssetDatabase.CopyAsset(Day2GrayboxBuilder.SCENE_PATH, SCENE_PATH);
        EditorSceneManager.OpenScene(SCENE_PATH);
        LoadData();
        Day2GrayboxBuilder.RemoveRoots(DAY2_DIRECTOR_ROOT, WORLD_ROOT, DIRECTOR_ROOT);

        // the old field, before the strip's plots join the scene
        FarmPlot[] fieldPlots = Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include);
        Transform world = Root(WORLD_ROOT).transform;
        Transform director = Root(DIRECTOR_ROOT).transform;
        (GameObject plotsRoot, FarmPlot[] landPlots, GameObject ghostPlots) = BuildStrip(world);
        Renderer crater = BuildCrater(world);
        // no land to sell any more
        Object.DestroyImmediate(FindNpc("Npc_Darto").gameObject);

        SellCrate crate = Object.FindAnyObjectByType<SellCrate>(FindObjectsInactive.Include);
        SetArray(crate, "sellableItemDataSOArray", new Object[] { Day2DataBuilder.SawitBunch, Day3DataBuilder.SawitBunchLv2 });

        Transform points = Root("Points", director).transform;
        Transform eveningPoint = Day2GrayboxBuilder.Point(points, "EveningPoint", EveningPosition, 70f);
        Transform landCentre = Day2GrayboxBuilder.Point(points, "LandCentre", StripCentre, 0f);
        Transform smashPoint = Day2GrayboxBuilder.Point(points, "SmashPoint", SmashPosition, 180f);
        Transform rigelHousePoint = Day2GrayboxBuilder.Point(points, "RigelHousePoint", RigelHouseDoorPosition, 180f);
        Transform rigelWatchPoint = Day2GrayboxBuilder.Point(points, "RigelWatchPoint", SmashPosition + new Vector3(-4.6f, 0f, -.3f), 90f);
        // the flashback: the two of them beside the Fabricator, the day it arrived
        FabricatorController fabricator = Object.FindAnyObjectByType<FabricatorController>(FindObjectsInactive.Include);
        Vector3 distributorPosition = fabricator.transform.position + new Vector3(1.6f, 0f, -1.6f);
        Transform flashbackDistributorPoint = Day2GrayboxBuilder.Point(points, "FlashbackDistributorPoint", distributorPosition, -90f);
        Transform flashbackPlayerPoint = Day2GrayboxBuilder.Point(points, "FlashbackPlayerPoint", distributorPosition + new Vector3(-1.9f, 0f, 0f), 90f);

        Day3Director day3 = director.gameObject.AddComponent<Day3Director>();
        SetField(day3, "day3SO", Day3DataBuilder.Day3);
        SetField(day3, "screenFade", Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include));
        SetField(day3, "savingsBookUI", Object.FindAnyObjectByType<SavingsBookUI>(FindObjectsInactive.Include));
        SetField(day3, "fabricator", fabricator);
        SetRecipe(day3);
        SetField(day3, "lv2CropDataSO", Day2DataBuilder.SawitLv2);
        SetField(day3, "oldFieldCropDataSO", GameDataBuilder.SawitLv1);
        SetArray(day3, "fieldPlotArray", fieldPlots);
        SetArray(day3, "landPlotArray", landPlots);
        SetField(day3, "landPlotsRoot", plotsRoot);
        SetField(day3, "ratnaNpc", FindNpc("Npc_Ratna"));
        SetField(day3, "ijahNpc", FindNpc("Npc_Ijah"));
        Npc rigel = FindNpc("Npc_Rigel");
        SetField(day3, "rigelNpc", rigel);
        SetField(day3, "houseDoor", Object.FindAnyObjectByType<HouseDoor>(FindObjectsInactive.Include));
        SetField(day3, "morningPoint", GameObject.Find("Day2_Graybox/Points/MorningPoint").transform);
        SetField(day3, "eveningPoint", eveningPoint);

        // from the field, over the new strip into the dark pines
        CinemachineCamera forestCamera = Root("CM_Day3Forest", director).AddComponent<CinemachineCamera>();
        forestCamera.transform.position = new Vector3(20.5f, 5f, 5.5f);
        forestCamera.transform.LookAt(new Vector3(23.5f, 1.8f, 18f));
        LensSettings lens = LensSettings.Default;
        lens.FieldOfView = 45f;
        lens.FarClipPlane = 200f;
        forestCamera.Lens = lens;
        forestCamera.enabled = false;

        LandSequence landSequence = BuildLandSequence(director, plotsRoot, rigel, landCentre, smashPoint, rigelHousePoint, rigelWatchPoint,
            flashbackPlayerPoint, flashbackDistributorPoint, out Npc distributor);
        SetField(day3, "landSequence", landSequence);
        BuildLandVisuals(director, landSequence, plotsRoot, ghostPlots, crater, smashPoint, distributor);

        Day3CameraVisual cameraVisual = director.gameObject.AddComponent<Day3CameraVisual>();
        SetField(cameraVisual, "director", day3);
        SetField(cameraVisual, "landSequence", landSequence);
        SetField(cameraVisual, "brain", Camera.main.GetComponent<CinemachineBrain>());
        SetField(cameraVisual, "forestCamera", forestCamera);
        SetField(cameraVisual, "emptyLandCamera", FixedCamera(director, "CM_Day3EmptyLand", new Vector3(23f, 9.5f, 6.5f), StripCentre, 42f));
        // side-on to the pair, close
        CinemachineTargetGroup pair = Root("FlashbackPair", director).AddComponent<CinemachineTargetGroup>();
        pair.AddMember(Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include).transform, 1f, .6f);
        pair.AddMember(distributor.transform, 1f, .6f);
        CinemachineCamera two = ChaseCamera(director, "CM_Day3FlashbackTwo", pair.transform, new Vector3(0f, 1.8f, -5.6f), 34f);
        two.GetComponent<CinemachineRotationComposer>().TargetOffset = new Vector3(0f, 1f, 0f);
        SetField(cameraVisual, "flashbackTwoCamera", two);
        // a close-up of his face, from the MC's side
        SetField(cameraVisual, "flashbackDistributorCamera", FixedCamera(director, "CM_Day3FlashbackDistributor",
            distributorPosition + new Vector3(-2.6f, 1.75f, -2.6f), distributorPosition + Vector3.up * 1.55f, 30f));
        CinemachineCamera rigelCamera = ChaseCamera(director, "CM_Day3Rigel", rigel.transform, new Vector3(2.5f, 2.6f, -5f), 40f);
        rigelCamera.GetComponent<CinemachineRotationComposer>().TargetOffset = new Vector3(0f, .8f, 0f);
        SetField(cameraVisual, "rigelCamera", rigelCamera);
        // low, between the MC and the new plots, looking back at them
        CinemachineCamera smashFront = FixedCamera(director, "CM_Day3SmashFront", SmashCameraPosition, SmashPosition + new Vector3(0f, .55f, .4f), 58f);
        CinemachineCamera smashClose = FixedCamera(director, "CM_Day3SmashClose", SmashCloseCameraPosition, SmashPosition + Vector3.up * .45f, 58f);
        SetField(cameraVisual, "smashFrontCamera", smashFront);
        SetField(cameraVisual, "smashCloseCamera", smashClose);
        SetArray(director.GetComponent<SmashImpactVisual>(), "punchCameraArray", new Object[] { smashFront, smashClose });
        // every camera feels the blows
        foreach (CinemachineCamera cam in Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include))
            if (cam.GetComponent<CinemachineImpulseListener>() == null) cam.gameObject.AddComponent<CinemachineImpulseListener>();

        NightVisual night = director.gameObject.AddComponent<NightVisual>();
        SetField(night, "day3Director", day3);
        SetField(night, "sun", GameObject.Find("Directional Light").GetComponent<Light>());
        SetField(night, "worldCamera", Camera.main);
        SetField(night, "doorLamp", GameObject.Find("Day2_Graybox").transform.Find("DoorLamp").gameObject);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Day2GrayboxBuilder.AddToBuildProfile(SCENE_PATH);
        WireBedroom();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(SCENE_PATH);
        Debug.Log($"Day 3 built into {SCENE_PATH}. Press Play in it to test Day 3 on its own.");
    }

    // ---------- world ----------

    // plain grass at first: the hidden plots (LandRiseVisual sinks them, the sequence shows them) and their see-through ghosts
    private static (GameObject plotsRoot, FarmPlot[] landPlots, GameObject ghostPlots) BuildStrip(Transform world)
    {
        Transform strip = Root("NewStrip", world).transform;
        GameObject plotsRoot = Root("LandPlots", strip);
        GameObject plot = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/Interactables/FarmPlot.prefab");
        FarmPlot[] plots = new FarmPlot[PLOT_COUNT];
        for (int i = 0; i < PLOT_COUNT; i++)
        {
            float x = StripCentre.x + (i - (PLOT_COUNT - 1) * .5f) * PLOT_SPACING;
            plots[i] = Spawn(plot, plotsRoot.transform, new Vector3(x, 0f, StripCentre.z), 0f).GetComponent<FarmPlot>();
            // bare soil, ready for a seed
            SerializedObject so = new SerializedObject(plots[i]);
            so.FindProperty("startState").enumValueIndex = (int)FarmPlot.State.Empty;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        GameObject ghostPlots = Root("GhostPlots", strip);
        Material hologram = GrayboxPalette.TransparentMapped("Hologram", new Color(.45f, .95f, 1f, .38f), null, true);
        Mesh tile = GrayboxMeshFactory.RoundedBox(new Vector3(2f, .16f, 2f), .07f);
        foreach (FarmPlot landPlot in plots)
        {
            GameObject ghost = Part(ghostPlots.transform, "GhostPlot", tile, hologram, landPlot.transform.position + new Vector3(0f, -.02f, 0f));
            ghost.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
        plotsRoot.SetActive(false);
        return (plotsRoot, plots, ghostPlots);
    }

    // a flat decal laid on the grass, hidden until the final blow
    private static Renderer BuildCrater(Transform world)
    {
        Material material = GrayboxPalette.TransparentMapped("Crater", new Color(.22f, .16f, .12f, 1f), GrayboxTextures.Crater.texture, false);
        GameObject crater = GameObject.CreatePrimitive(PrimitiveType.Quad);
        crater.name = "Crater";
        crater.transform.SetParent(world, false);
        crater.transform.localScale = Vector3.one * 2.2f;
        Object.DestroyImmediate(crater.GetComponent<Collider>());
        MeshRenderer renderer = crater.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        crater.SetActive(false);
        return renderer;
    }

    // ---------- the new land ----------

    private static LandSequence BuildLandSequence(Transform director, GameObject plotsRoot, Npc rigel, Transform landCentre, Transform smashPoint,
        Transform rigelHousePoint, Transform rigelWatchPoint, Transform flashbackPlayerPoint, Transform flashbackDistributorPoint, out Npc distributor)
    {
        Player player = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        PlayerVisual playerVisual = player.GetComponentInChildren<PlayerVisual>(true);
        NpcVisual rigelVisual = rigel.GetComponentInChildren<NpcVisual>(true);
        CutsceneActor playerActor = Day1GrayboxBuilder.EnsureActor(player.gameObject, playerVisual);
        CutsceneActor rigelActor = Day1GrayboxBuilder.EnsureActor(rigel.gameObject, rigelVisual);
        AddPoseVisual(playerActor, playerVisual.gameObject);
        AddPoseVisual(rigelActor, rigelVisual.gameObject);

        // a copy of the Distributor (Day 1's is gone from this village), only shown in the flashback
        GameObject distributorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/Characters/Npc_Distributor.prefab");
        GameObject distributorGo = Spawn(distributorPrefab, director, flashbackDistributorPoint.position, flashbackDistributorPoint.eulerAngles.y);
        distributorGo.name = "Npc_DistributorFlashback";
        distributor = distributorGo.GetComponent<Npc>();
        NpcVisual distributorVisual = distributorGo.GetComponentInChildren<NpcVisual>(true);
        CutsceneActor distributorActor = Day1GrayboxBuilder.EnsureActor(distributorGo, distributorVisual);
        AddPoseVisual(distributorActor, distributorVisual.gameObject);
        distributorGo.SetActive(false);

        LandQTE landQTE = director.gameObject.AddComponent<LandQTE>();
        LandSequence landSequence = director.gameObject.AddComponent<LandSequence>();
        SetField(landSequence, "day3SO", Day3DataBuilder.Day3);
        SetField(landSequence, "screenFade", Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include));
        SetField(landSequence, "landQTE", landQTE);
        SetField(landSequence, "bunchItemDataSO", Day3DataBuilder.SawitBunchLv2);
        SetField(landSequence, "playerActor", playerActor);
        SetField(landSequence, "rigelNpc", rigel);
        SetField(landSequence, "rigelActor", rigelActor);
        SetField(landSequence, "distributorNpc", distributor);
        SetField(landSequence, "distributorActor", distributorActor);
        SetField(landSequence, "landPlotsRoot", plotsRoot);
        SetField(landSequence, "landCentre", landCentre);
        SetField(landSequence, "flashbackPlayerPoint", flashbackPlayerPoint);
        SetField(landSequence, "flashbackDistributorPoint", flashbackDistributorPoint);
        SetField(landSequence, "smashPoint", smashPoint);
        SetField(landSequence, "rigelHousePoint", rigelHousePoint);
        SetField(landSequence, "rigelWatchPoint", rigelWatchPoint);
        return landSequence;
    }

    private static void AddPoseVisual(CutsceneActor actor, GameObject visual)
    {
        CutscenePoseVisual pose = visual.GetComponent<CutscenePoseVisual>();
        if (pose == null) pose = visual.AddComponent<CutscenePoseVisual>();
        SetField(pose, "cutsceneActor", actor);
        SetField(pose, "animator", visual.GetComponentInChildren<Animator>(true));
    }

    private static void BuildLandVisuals(Transform director, LandSequence landSequence, GameObject plotsRoot, GameObject ghostPlots, Renderer crater,
        Transform smashPoint, Npc distributor)
    {
        GhostPlotVisual ghost = director.gameObject.AddComponent<GhostPlotVisual>();
        SetField(ghost, "landSequence", landSequence);
        SetField(ghost, "ghostPlotsGameObject", ghostPlots);

        LandRiseVisual rise = director.gameObject.AddComponent<LandRiseVisual>();
        SetField(rise, "landSequence", landSequence);
        SetField(rise, "landPlotsRoot", plotsRoot.transform);

        CraterVisual craterVisual = director.gameObject.AddComponent<CraterVisual>();
        SetField(craterVisual, "landSequence", landSequence);
        SetField(craterVisual, "craterRenderer", crater);
        SetField(craterVisual, "smashPoint", smashPoint);

        CinemachineImpulseSource impulse = director.gameObject.AddComponent<CinemachineImpulseSource>();
        impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion;
        impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        impulse.ImpulseDefinition.ImpulseDuration = .35f;
        // mostly sideways: the smash cameras sit under a metre off the grass, a big drop would sink them into it
        impulse.DefaultVelocity = new Vector3(1f, -.3f, 0f);
        SmashImpactVisual impact = director.gameObject.AddComponent<SmashImpactVisual>();
        SetField(impact, "landSequence", landSequence);
        SetField(impact, "impulseSource", impulse);
        SetField(impact, "smashPoint", smashPoint);
        SetField(impact, "dustPrefab", VfxBuilder.SmashDust().transform);

        FlashbackVisual flashback = director.gameObject.AddComponent<FlashbackVisual>();
        SetField(flashback, "landSequence", landSequence);
        SetField(flashback, "grayscaleVolume", GrayscaleVolume(director));
        SetField(flashback, "glint", Glint(distributor));

        // the whole screen this time, and no apparition
        WorldGlitchVisual glitch = director.gameObject.AddComponent<WorldGlitchVisual>();
        SetField(glitch, "landSequence", landSequence);
        SetValue(glitch, "screenSplit", 0f);
        SetArray(glitch, "corruptMaterialArray", new Object[]
        {
            GrayboxPalette.Unlit("GlitchVoid", new Color(.02f, .02f, .03f)),
            GrayboxPalette.Unlit("GlitchBlood", new Color(.42f, 0f, .03f)),
            GrayboxPalette.Unlit("GlitchBone", new Color(.93f, .91f, .86f)),
        });
        SerializedObject glitchSo = new SerializedObject(glitch);
        glitchSo.FindProperty("ignoredLayers").intValue = LayerMask.GetMask("Portrait", "Puzzle");
        glitchSo.ApplyModifiedPropertiesWithoutUndo();

        BuildUi(landSequence, director.GetComponent<LandQTE>());
    }

    // black and white at full weight, off until the flashback
    private static Volume GrayscaleVolume(Transform director)
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(FLASHBACK_PROFILE_PATH);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, FLASHBACK_PROFILE_PATH);
            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(-100f);
            color.contrast.Override(15f);
            foreach (VolumeComponent component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
        }
        Volume volume = Root("FlashbackVolume", director).AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.weight = 0f;
        volume.sharedProfile = profile;
        return volume;
    }

    // a white star at his right lens, hidden until FlashbackVisual flashes it
    private static Transform Glint(Npc distributor)
    {
        Transform head = distributor.GetComponentInChildren<Animator>(true).transform.Find("Hips/Head");
        GameObject glint = GameObject.CreatePrimitive(PrimitiveType.Quad);
        glint.name = "GlassesGlint";
        Object.DestroyImmediate(glint.GetComponent<Collider>());
        glint.transform.SetParent(head, false);
        glint.transform.localPosition = new Vector3(.19f, .69f, .69f);
        MeshRenderer renderer = glint.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = GrayboxPalette.TransparentMapped("Glint", Color.white, GrayboxTextures.Glint.texture, true);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        glint.SetActive(false);
        return glint.transform;
    }

    // eyelids and the eat / smash ring, under the screen fade so the fade still covers everything
    private static void BuildUi(LandSequence landSequence, LandQTE landQTE)
    {
        ScreenFadeUI fade = Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include);
        Transform canvas = fade.transform.parent;

        RectTransform blink = Rect(canvas, "EyeBlinkUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        Image top = Img(blink, "TopLid", null, Color.black, new Vector2(0f, .5f), Vector2.one, new Vector2(.5f, 1f), Vector2.zero, Vector2.zero);
        Image bottom = Img(blink, "BottomLid", null, Color.black, Vector2.zero, new Vector2(1f, .5f), new Vector2(.5f, 0f), Vector2.zero, Vector2.zero);
        top.raycastTarget = false;
        bottom.raycastTarget = false;
        EyeBlinkUI eyeBlink = blink.gameObject.AddComponent<EyeBlinkUI>();
        SetField(eyeBlink, "landSequence", landSequence);
        SetField(eyeBlink, "topLid", top.rectTransform);
        SetField(eyeBlink, "bottomLid", bottom.rectTransform);

        RectTransform root = Rect(canvas, "LandQTEUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        RectTransform group = Rect(root, "Group", Centre, Centre, Centre, Vector2.zero, new Vector2(160f, 160f));
        Image ringBack = Img(group, "Ring", GrayboxTextures.Circle, new Color(0f, 0f, 0f, .45f), Centre, Centre, Centre, Vector2.zero, new Vector2(110f, 110f));
        Image fill = Img(ringBack.rectTransform, "Fill", GrayboxTextures.Circle, Accent, Centre, Centre, Centre, Vector2.zero, new Vector2(96f, 96f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillAmount = 0f;
        TextMeshProUGUI label = Text(group, "Label", "Mash [E]!", 34, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        label.outlineWidth = .25f;
        label.outlineColor = Color.black;
        Place(label.rectTransform, Centre, new Vector2(0f, 90f), new Vector2(420f, 50f));
        LandQTEUI ui = root.gameObject.AddComponent<LandQTEUI>();
        SetField(ui, "landSequence", landSequence);
        SetField(ui, "landQTE", landQTE);
        SetField(ui, "group", group);
        SetField(ui, "ringGameObject", ringBack.gameObject);
        SetField(ui, "fillImage", fill);
        SetField(ui, "labelText", label);

        fade.transform.SetAsLastSibling();
    }

    private static CinemachineCamera FixedCamera(Transform parent, string name, Vector3 position, Vector3 lookAt, float fov)
    {
        CinemachineCamera cam = Root(name, parent).AddComponent<CinemachineCamera>();
        cam.transform.position = position;
        cam.transform.LookAt(lookAt);
        LensSettings lens = LensSettings.Default;
        lens.FieldOfView = fov;
        lens.FarClipPlane = 200f;
        cam.Lens = lens;
        cam.enabled = false;
        return cam;
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
        follow.TrackerSettings.PositionDamping = new Vector3(.3f, .3f, .3f);
        cam.gameObject.AddComponent<CinemachineRotationComposer>().Damping = new Vector2(.2f, .2f);
        return cam;
    }

    // ---------- data ----------

    private static void LoadData()
    {
        GameDataBuilder.LoadAll();
        Day2DataBuilder.LoadAll();
        Day3DataBuilder.Build();
    }

    // every press: 2 resin + water + flower make 4 Lv.2 seeds (one press fills the strip) and hand the bottle back
    private static void SetRecipe(Day3Director day3)
    {
        SerializedObject so = new SerializedObject(day3);
        SerializedProperty recipes = so.FindProperty("recipeArray");
        recipes.arraySize = 1;
        SerializedProperty recipe = recipes.GetArrayElementAtIndex(0);
        (ItemDataSO item, int amount)[] inputs = { (Day2DataBuilder.Resin, 2), (GameDataBuilder.Water, 1), (GameDataBuilder.Flower, 1) };
        SerializedProperty inputArray = recipe.FindPropertyRelative("inputArray");
        inputArray.arraySize = inputs.Length;
        for (int i = 0; i < inputs.Length; i++)
        {
            inputArray.GetArrayElementAtIndex(i).FindPropertyRelative("itemDataSO").objectReferenceValue = inputs[i].item;
            inputArray.GetArrayElementAtIndex(i).FindPropertyRelative("amount").intValue = inputs[i].amount;
        }
        recipe.FindPropertyRelative("output.itemDataSO").objectReferenceValue = Day2DataBuilder.SawitSeedLv2;
        recipe.FindPropertyRelative("output.amount").intValue = PLOT_COUNT;
        recipe.FindPropertyRelative("returned.itemDataSO").objectReferenceValue = GameDataBuilder.EmptyBottle;
        recipe.FindPropertyRelative("returned.amount").intValue = 1;
        recipe.FindPropertyRelative("puzzleSO").objectReferenceValue = GameDataBuilder.PuzzleSplit;
        recipe.FindPropertyRelative("blockedReason").stringValue = "Needs 2 resin, water and a flower";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // the bedroom's director needs Day 3's morning text and the time skip
    private static void WireBedroom()
    {
        EditorSceneManager.OpenScene(HouseInteriorBuilder.SCENE_PATH);
        BedroomDirector bedroom = GameObject.Find(BEDROOM_DIRECTOR_PATH).GetComponent<BedroomDirector>();
        SetField(bedroom, "day3SO", Day3DataBuilder.Day3);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static Npc FindNpc(string name) => Object.FindObjectsByType<Npc>(FindObjectsInactive.Include).First(n => n.name == name);
}
