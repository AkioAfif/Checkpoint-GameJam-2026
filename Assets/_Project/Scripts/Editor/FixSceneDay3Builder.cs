using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static GrayboxBuild;

// Builds Day 3, the yard day ("Dirt Under Your Nails"), on the CozyFarm map as FixScene_Day3 (Tools > FixScene > Build Day 3
// Scene). It copies FixScene_Day2 (so build Day 2 first), keeps its crate, Rigel and resin stumps (Day2_Graybox), swaps Day 2's
// director for the yard day's (Day3Yard), and turns the strip beside the garden into the day's work:
// - the clutter FixSceneMapBuilder put there (weeds, rocks, the stump) becomes jobs that need the borrowed tools;
// - three hidden plots in a fourth column east of the garden appear as the MC tills their patches, each with its bed cut from
//   the 3x4 garden model; the last one swaps the old 3x3 garden for the whole 3x4 model (GardenExpansionVisual);
// - Dimas digs by the river with Darto's hoe; a Lv.2 bunch sits on a crate by the front door for the evening's pulse.
// Re-runnable: FixScene_Day3 is regenerated each time. FixScene_Day4 copies it (StripYardDay), so rebuild Day 4 after it.
public static class FixSceneDay3Builder
{
    public const string SCENE_PATH = "Assets/_Project/Scenes/FixScene_Day3.unity";
    private const string GAMEPLAY_ROOT = "Gameplay";
    private const string DAY2_DIRECTOR_ROOT = "Day2";
    private const string GRAYBOX_ROOT = "Day3Yard_Graybox";
    private const string DIRECTOR_ROOT = "Day3Yard";
    private const string NEW_PLOTS_ROOT = "NewPlots";
    private const string EXPANDED_GARDEN = "Garden_3x4";
    private const string BED_PIECE = "Bed";
    private const string OLD_GARDEN_PATH = "CozyFarm/Player_Garden";
    private const string GARDEN_3X4_MODEL = "Assets/Ilkham/models/garden/garden_3x4.glb";
    private const string PIECES_FOLDER = "Assets/_Project/Art/Models/Yard";

    // the 3x4 garden is the 3x3 one with a fourth column of beds on its east side, so it sits half a column east of the old one
    private const float GARDEN_COLUMN_SPACING = 2.3f;
    // how far a bed reaches from its plot's centre
    private const float BED_HALF_SIZE = .85f;
    // the 3x4 model's height, so it sits on the ground instead of sinking into it (its beds and ghost pieces follow it)
    private const float GARDEN_3X4_Y = .19f;
    // the model's materials: 1 furrows and 2 ridges make the beds, 3 and 4 the wooden fences
    private static readonly int[] BedSubMeshArray = { 1, 2 };
    private static readonly int[] FenceSubMeshArray = { 3, 4 };
    private static readonly Vector3 BareHandsPosition = new Vector3(-7.5f, 0f, -30.1f);
    // east of the stump, facing it; Rigel takes the rope behind the MC
    private static readonly Vector3 StumpPullPosition = new Vector3(-6.6f, 0f, -34f);
    private static readonly Vector3 RigelHelpPosition = new Vector3(-5.7f, 0f, -34f);
    private static readonly Vector3 AfternoonPosition = new Vector3(-6.2f, 0f, -30.6f);
    // a crate beside the front door (the morning point) holding the Lv.2 bunch he kept
    private static readonly Vector3 PorchCratePosition = new Vector3(-20.9f, 0f, -26.3f);
    // up the river bank north of the old bridge, near Darto
    private static readonly Vector3 DimasDigPosition = new Vector3(-6.5f, 0f, 9f);
    // side-on to the gameplay camera (which looks south), so his swing reads
    private const float DIMAS_DIG_YAW = 90f;
    private const string HELD_HOE = "HeldHoe";
    // the hoe in his right hand, flipped so the handle runs on down the arm and the blade hooks back toward the ground
    private static readonly Vector3 HeldHoePosition = new Vector3(0f, -.3f, 0f);
    private static readonly Vector3 HeldHoeEuler = new Vector3(180f, 0f, 0f);
    private const float HELD_HOE_SCALE = .8f;
    // the mention shot: high and in front of whoever a line talks about, from the gameplay camera's side
    private static readonly Vector3 FocusCameraOffset = new Vector3(2.5f, 3.2f, 5.5f);
    // in the upper part of the screen, clear of the dialogue box (negative y = higher)
    private static readonly Vector2 FocusScreenPosition = new Vector2(0f, -.22f);
    private const float FOCUS_CAMERA_FOV = 38f;

    [MenuItem("Tools/FixScene/Build Day 3 Scene")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FixSceneDay2Builder.SCENE_PATH) == null)
        {
            Debug.LogError($"{FixSceneDay2Builder.SCENE_PATH} is missing: run Tools > FixScene > Build Day 2 Scene first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        // the item icons bake in a scene of their own, so before this one opens
        YardDayDataBuilder.Build();

        // overwrite the file, not delete + copy: the scene keeps its GUID (and its .meta stays out of every diff)
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) == null) AssetDatabase.CopyAsset(FixSceneDay2Builder.SCENE_PATH, SCENE_PATH);
        else
        {
            System.IO.File.Copy(FixSceneDay2Builder.SCENE_PATH, SCENE_PATH, true);
            AssetDatabase.ImportAsset(SCENE_PATH, ImportAssetOptions.ForceUpdate);
        }
        EditorSceneManager.OpenScene(SCENE_PATH);
        // after opening: OpenScene unloads assets loaded before it, and references to them would save as missing
        LoadData();
        YardDaySO yardDaySO = YardDayDataBuilder.Yard;

        Transform gameplay = GameObject.Find(GAMEPLAY_ROOT).transform;
        Transform day2 = gameplay.Find(DAY2_DIRECTOR_ROOT);
        Material pulseMaterial = (Material)new SerializedObject(day2.GetComponent<MutationPulseVisual>()).FindProperty("pulseMaterial").objectReferenceValue;
        Object.DestroyImmediate(day2.gameObject);
        // the garden, front (north) row first: the first ones are ripe this morning
        FarmPlot[] gardenPlots = Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include)
            .Where(p => p.gameObject.activeSelf)
            .OrderBy(p => -Mathf.Round(p.transform.position.z * 10f)).ThenBy(p => p.transform.position.x).ToArray();

        // ICAN only buys the usual bunches today, so the MC keeps the dark Lv.2 ones
        SellCrate crate = Object.FindAnyObjectByType<SellCrate>(FindObjectsInactive.Include);
        SetArray(crate, "sellableItemDataSOArray", new Object[] { Day2DataBuilder.SawitBunch });

        Transform graybox = Root(GRAYBOX_ROOT, gameplay).transform;
        Transform director = Root(DIRECTOR_ROOT, gameplay).transform;
        Transform points = Root("Points", director).transform;
        Transform day2Graybox = gameplay.Find("Day2_Graybox");
        Transform morningPoint = day2Graybox.Find("Points/MorningPoint");
        Transform bareHandsPoint = Day2GrayboxBuilder.Point(points, "BareHandsPoint", Ground(BareHandsPosition), -90f);
        Transform stumpPullPoint = Day2GrayboxBuilder.Point(points, "StumpPullPoint", Ground(StumpPullPosition), -90f);
        Transform rigelHelpPoint = Day2GrayboxBuilder.Point(points, "RigelHelpPoint", Ground(RigelHelpPosition), -90f);
        Transform afternoonPoint = Day2GrayboxBuilder.Point(points, "AfternoonPoint", Ground(AfternoonPosition), 180f);
        Vector3 porchCrate = Ground(PorchCratePosition);
        Vector3 toCrate = porchCrate - morningPoint.position;
        Transform eveningPoint = Day2GrayboxBuilder.Point(points, "EveningPoint", morningPoint.position, Mathf.Atan2(toCrate.x, toCrate.z) * Mathf.Rad2Deg);

        Transform dust = VfxBuilder.SmashDust().transform;
        Transform jobs = Root("Jobs", director).transform;
        FarmPlot[] newPlots = BuildNewPlots(graybox, gardenPlots);
        YardObstacle[] tills = newPlots.Select((plot, i) => Job(jobs, $"Till_{i + 1}", null, YardDayDataBuilder.Hoe, "till", "Need a hoe",
            YardObstacleVisual.Style.DustOnly, dust, plot.transform.position, plot.gameObject)).ToArray();
        Transform clutter = gameplay.Find(FixSceneMapBuilder.YARD_CLUTTER_ROOT);
        if (clutter == null)
        {
            Debug.LogError($"No {FixSceneMapBuilder.YARD_CLUTTER_ROOT} in the scene: run Tools > FixScene > Add Yard Strip Clutter on FixScene, rebuild Day 2, then Day 3.");
            return;
        }
        YardObstacle[] weeds = ClutterJobs(jobs, clutter, "Weed_", YardDayDataBuilder.Sickle, "cut", "Need a sickle", YardObstacleVisual.Style.Shrink, dust);
        YardObstacle[] rocks = ClutterJobs(jobs, clutter, "Rock_", YardDayDataBuilder.Hoe, "dig out", "Need a hoe", YardObstacleVisual.Style.Sink, dust);
        Transform stumpModel = clutter.Find("Stump");
        YardObstacle stump = Job(jobs, "Job_Stump", stumpModel, YardDayDataBuilder.Rope, "pull out", "Need a rope", YardObstacleVisual.Style.Yank, dust, stumpModel.position, null);

        Player player = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        PlayerVisual playerVisual = player.GetComponentInChildren<PlayerVisual>(true);
        CutsceneActor playerActor = Day1GrayboxBuilder.EnsureActor(player.gameObject, playerVisual);
        Day3GrayboxBuilder.AddPoseVisual(playerActor, playerVisual.gameObject);
        Npc rigel = Day3GrayboxBuilder.FindNpc("Npc_Rigel");
        NpcVisual rigelVisual = rigel.GetComponentInChildren<NpcVisual>(true);
        Day3GrayboxBuilder.AddPoseVisual(Day1GrayboxBuilder.EnsureActor(rigel.gameObject, rigelVisual), rigelVisual.gameObject);
        Npc dimas = Day3GrayboxBuilder.FindNpc("Npc_Dimas");
        dimas.transform.position = Ground(DimasDigPosition);
        // he stays put, digging, instead of strolling his usual points round the house
        if (dimas.TryGetComponent(out NpcWander wander)) wander.enabled = false;

        LandQTE landQTE = director.gameObject.AddComponent<LandQTE>();
        LandQTEUI qteUi = Day3GrayboxBuilder.BuildUi(null, landQTE);
        SetText(qteUi, "holdHint", "Hold [{0}]!");
        YardErrands errands = BuildErrands(director, yardDaySO, dimas);
        BuildDimasDig(director, errands, dimas);
        BuildDialogueFocus(director, dimas);

        YardDayDirector yard = director.gameObject.AddComponent<YardDayDirector>();
        SetField(yard, "yardDaySO", yardDaySO);
        SetField(yard, "errands", errands);
        SetField(yard, "screenFade", Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include));
        SetField(yard, "savingsBookUI", Object.FindAnyObjectByType<SavingsBookUI>(FindObjectsInactive.Include));
        FabricatorController fabricator = Object.FindAnyObjectByType<FabricatorController>(FindObjectsInactive.Include);
        SetField(yard, "fabricator", fabricator);
        SetRecipe(yard);
        SetField(yard, "landQTE", landQTE);
        SetField(yard, "playerActor", playerActor);
        SetArray(yard, "gardenPlotArray", gardenPlots);
        SetField(yard, "lv2CropDataSO", Day2DataBuilder.SawitLv2);
        SetField(yard, "lv1CropDataSO", GameDataBuilder.SawitLv1);
        SetArray(yard, "newPlotArray", newPlots);
        SetArray(yard, "weedArray", weeds);
        SetArray(yard, "rockArray", rocks);
        SetField(yard, "stump", stump);
        SetArray(yard, "tillArray", tills);
        SetField(yard, "ratnaNpc", Day3GrayboxBuilder.FindNpc("Npc_Ratna"));
        SetField(yard, "rigelNpc", rigel);
        SetField(yard, "houseDoor", Object.FindAnyObjectByType<HouseDoor>(FindObjectsInactive.Include));
        SetField(yard, "morningPoint", morningPoint);
        SetField(yard, "bareHandsPoint", bareHandsPoint);
        SetField(yard, "stumpPullPoint", stumpPullPoint);
        SetField(yard, "rigelHelpPoint", rigelHelpPoint);
        SetField(yard, "afternoonPoint", afternoonPoint);
        SetField(yard, "eveningPoint", eveningPoint);

        NightVisual night = director.gameObject.AddComponent<NightVisual>();
        SetField(night, "yardDayDirector", yard);
        SetField(night, "sun", RenderSettings.sun);
        SetField(night, "worldCamera", Camera.main);
        SetField(night, "doorLamp", day2Graybox.Find("DoorLamp").gameObject);

        Transform bunch = BuildPorchBunch(director, porchCrate);
        BunchPulseVisual pulse = director.gameObject.AddComponent<BunchPulseVisual>();
        SetField(pulse, "director", yard);
        SetField(pulse, "bunch", bunch);
        SetField(pulse, "pulseMaterial", pulseMaterial);
        BuildRope(director, yard, landQTE, stumpModel);
        BuildCameras(director, yard, stumpModel.position, bunch.position);

        // the wish: the garden's new column, see-through on the strip, and the clutter gone from the picture
        ImaginationVisual imagination = director.gameObject.AddComponent<ImaginationVisual>();
        SetField(imagination, "yardDayDirector", yard);
        SetField(imagination, "ghostFarmRoot", BuildGarden(director, graybox, newPlots, tills));
        SetArray(imagination, "imaginedAwayTreeArray", clutter.Cast<Transform>().ToArray());

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Day2GrayboxBuilder.AddToBuildProfile(SCENE_PATH);
        WireBedroom(yardDaySO);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(SCENE_PATH);
        Debug.Log($"Day 3 (the yard day) built into {SCENE_PATH}. Press Play in it to test it on its own; rebuild Day 4 after it.");
    }

    // Day 4 copies this scene: the yard day's story leaves, the clutter is gone (cleared two weeks ago), its plots stay and the
    // garden is the 3x4 one
    public static void StripYardDay(Transform gameplay)
    {
        Object.DestroyImmediate(gameplay.Find(DIRECTOR_ROOT).gameObject);
        // Dimas gave the hoe back two weeks ago
        Transform heldHoe = Day3GrayboxBuilder.FindNpc("Npc_Dimas").GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == HELD_HOE);
        if (heldHoe != null) Object.DestroyImmediate(heldHoe.gameObject);
        Transform clutter = gameplay.Find(FixSceneMapBuilder.YARD_CLUTTER_ROOT);
        if (clutter != null) Object.DestroyImmediate(clutter.gameObject);
        Transform graybox = gameplay.Find(GRAYBOX_ROOT);
        foreach (Transform plot in graybox.Find(NEW_PLOTS_ROOT))
        {
            plot.gameObject.SetActive(true);
            Transform bed = plot.Find(BED_PIECE);
            if (bed != null) Object.DestroyImmediate(bed.gameObject);
        }
        graybox.Find(EXPANDED_GARDEN).gameObject.SetActive(true);
        GameObject.Find(OLD_GARDEN_PATH).SetActive(false);
        // its ring under the HUD; the next day builds its own
        foreach (LandQTEUI ui in Object.FindObjectsByType<LandQTEUI>(FindObjectsInactive.Include)) Object.DestroyImmediate(ui.gameObject);
    }

    // ---------- the strip ----------

    // a fourth column for the garden, one per row of it, hidden until its patch is tilled; the bed under it comes from the model
    private static FarmPlot[] BuildNewPlots(Transform graybox, FarmPlot[] gardenPlots)
    {
        Transform root = Root(NEW_PLOTS_ROOT, graybox).transform;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/Interactables/FarmPlot.prefab");
        float columnX = gardenPlots.Max(p => p.transform.position.x) + GARDEN_COLUMN_SPACING;
        float[] rowZArray = gardenPlots.GroupBy(p => Mathf.RoundToInt(p.transform.position.z * 10f))
            .Select(row => row.Average(p => p.transform.position.z)).OrderByDescending(z => z).ToArray();
        FarmPlot[] plots = new FarmPlot[rowZArray.Length];
        for (int i = 0; i < plots.Length; i++)
        {
            plots[i] = Spawn(prefab, root, Ground(new Vector3(columnX, 0f, rowZArray[i])), 0f).GetComponent<FarmPlot>();
            plots[i].name = $"FarmPlot_Yard_{i + 1}";
            SerializedObject so = new SerializedObject(plots[i]);
            so.FindProperty("startState").enumValueIndex = (int)FarmPlot.State.Empty;
            so.ApplyModifiedPropertiesWithoutUndo();
            // like the garden's own plots: the model's bed is the soil
            plots[i].transform.Find("Visual/Soil").gameObject.SetActive(false);
            plots[i].gameObject.SetActive(false);
        }
        return plots;
    }

    // The 3x4 garden over the old 3x3 one, hidden until the last patch is tilled; each new plot gets its bed, cut out of the
    // model, as a child (so tilling the patch shows it). Returns what the MC pictures at the wish: the new column only (its
    // beds and the outer fence) with a ripe palm on each bed, models only (ImaginationVisual makes them see-through).
    private static GameObject BuildGarden(Transform director, Transform graybox, FarmPlot[] newPlots, YardObstacle[] tills)
    {
        GameObject oldGarden = GameObject.Find(OLD_GARDEN_PATH);
        Transform old = oldGarden.transform;
        GameObject expanded = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GARDEN_3X4_MODEL), graybox);
        expanded.name = EXPANDED_GARDEN;
        Vector3 expandedPosition = old.position + old.rotation * Vector3.left * (GARDEN_COLUMN_SPACING / 2f);
        expandedPosition.y = GARDEN_3X4_Y;
        expanded.transform.SetPositionAndRotation(expandedPosition, old.rotation);
        expanded.SetActive(false);
        MeshFilter model = expanded.GetComponentInChildren<MeshFilter>(true);
        Material[] materialArray = model.GetComponent<MeshRenderer>().sharedMaterials;

        GameObject ghost = Root("GhostFarm", director);
        GameObject[] bedArray = new GameObject[newPlots.Length];
        for (int i = 0; i < newPlots.Length; i++)
        {
            Vector3 plot = newPlots[i].transform.position;
            Mesh bed = CutMesh(model, $"Garden3x4_Bed_{i + 1}", BedSubMeshArray,
                (a, b, c) => IsNear((a + b + c) / 3f, plot, BED_HALF_SIZE));
            bedArray[i] = MeshPiece(BED_PIECE, bed, materialArray, model.transform, newPlots[i].transform);
            MeshPiece($"Bed_{i + 1}", bed, materialArray, model.transform, ghost.transform);
            FixSceneDay4Builder.ModelCopy(GameDataBuilder.SawitLv1.ripePrefab.gameObject, ghost.transform, plot + Vector3.up * .1f);
        }
        // the 3x4's own copy of the old fence stays out of the picture: only rails and posts reaching past the old garden are new
        Bounds oldBounds = oldGarden.GetComponentInChildren<Renderer>().bounds;
        oldBounds.Expand(new Vector3(.06f, 10f, .06f));
        Mesh fence = CutMesh(model, "Garden3x4_OuterFence", FenceSubMeshArray,
            (a, b, c) => !oldBounds.Contains(a) || !oldBounds.Contains(b) || !oldBounds.Contains(c));
        MeshPiece("OuterFence", fence, materialArray, model.transform, ghost.transform);
        ghost.SetActive(false);

        GardenExpansionVisual expansion = director.gameObject.AddComponent<GardenExpansionVisual>();
        SetArray(expansion, "tillArray", tills);
        SetField(expansion, "oldGarden", oldGarden);
        SetField(expansion, "expandedGarden", expanded);
        SetArray(expansion, "bedPieceArray", bedArray);
        return ghost;
    }

    private static bool IsNear(Vector3 point, Vector3 centre, float halfSize) =>
        Mathf.Abs(point.x - centre.x) < halfSize && Mathf.Abs(point.z - centre.z) < halfSize;

    // a copy of the model's mesh keeping only the triangles of the listed materials that pass the test (corners in world space)
    private static Mesh CutMesh(MeshFilter source, string name, int[] subMeshArray, System.Func<Vector3, Vector3, Vector3, bool> isKept)
    {
        Mesh mesh = Object.Instantiate(source.sharedMesh);
        mesh.name = name;
        Vector3[] vertexArray = mesh.vertices;
        for (int sub = 0; sub < mesh.subMeshCount; sub++)
        {
            int[] triangleArray = mesh.GetTriangles(sub);
            List<int> keptList = new List<int>();
            if (subMeshArray.Contains(sub))
            {
                for (int i = 0; i < triangleArray.Length; i += 3)
                {
                    Vector3 a = source.transform.TransformPoint(vertexArray[triangleArray[i]]);
                    Vector3 b = source.transform.TransformPoint(vertexArray[triangleArray[i + 1]]);
                    Vector3 c = source.transform.TransformPoint(vertexArray[triangleArray[i + 2]]);
                    if (!isKept(a, b, c)) continue;
                    keptList.Add(triangleArray[i]);
                    keptList.Add(triangleArray[i + 1]);
                    keptList.Add(triangleArray[i + 2]);
                }
            }
            mesh.SetTriangles(keptList, sub);
        }
        mesh.RecalculateBounds();
        string path = $"{PIECES_FOLDER}/{name}.asset";
        if (!AssetDatabase.IsValidFolder(PIECES_FOLDER)) AssetDatabase.CreateFolder("Assets/_Project/Art/Models", "Yard");
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    // a model-only piece that lines up with the model it was cut from
    private static GameObject MeshPiece(string name, Mesh mesh, Material[] materialArray, Transform model, Transform parent)
    {
        GameObject piece = new GameObject(name);
        piece.transform.SetPositionAndRotation(model.position, model.rotation);
        piece.transform.localScale = model.lossyScale;
        piece.transform.SetParent(parent, true);
        piece.AddComponent<MeshFilter>().sharedMesh = mesh;
        piece.AddComponent<MeshRenderer>().sharedMaterials = materialArray;
        return piece;
    }

    private static YardObstacle[] ClutterJobs(Transform jobs, Transform clutter, string prefix, ItemDataSO tool, string verb, string blocked,
        YardObstacleVisual.Style style, Transform dust)
    {
        return clutter.Cast<Transform>().Where(t => t.name.StartsWith(prefix)).OrderBy(t => t.name)
            .Select(model => Job(jobs, $"Job_{model.name}", model, tool, verb, blocked, style, dust, model.position, null)).ToArray();
    }


    // a job's interactable sits on the ground at the model's foot: a trigger on the Interactable layer, a prompt over it
    private static YardObstacle Job(Transform parent, string name, Transform model, ItemDataSO tool, string verb, string blocked, YardObstacleVisual.Style style,
        Transform dust, Vector3 position, GameObject reveal)
    {
        GameObject job = Root(name, parent);
        job.transform.position = position;
        job.layer = LayerMask.NameToLayer("Interactable");
        Box(job, new Vector3(0f, .6f, 0f), new Vector3(1.4f, 1.2f, 1.4f), true);
        YardObstacle obstacle = job.AddComponent<YardObstacle>();
        SetField(obstacle, "requiredItemDataSO", tool);
        SetText(obstacle, "promptVerb", verb);
        SetText(obstacle, "blockedReason", blocked);
        Transform anchor = Root("PromptAnchor", job.transform).transform;
        anchor.localPosition = Vector3.up * 1.4f;
        SetField(obstacle, "promptAnchor", anchor);
        if (reveal != null) SetField(obstacle, "revealGameObject", reveal);

        YardObstacleVisual visual = job.AddComponent<YardObstacleVisual>();
        SetField(visual, "yardObstacle", obstacle);
        if (model != null) SetField(visual, "model", model);
        SerializedObject visualSo = new SerializedObject(visual);
        visualSo.FindProperty("style").enumValueIndex = (int)style;
        visualSo.ApplyModifiedPropertiesWithoutUndo();
        SetField(visual, "dustPrefab", dust);
        return obstacle;
    }

    private static YardErrands BuildErrands(Transform director, YardDaySO yardDaySO, Npc dimas)
    {
        YardErrands errands = director.gameObject.AddComponent<YardErrands>();
        SetField(errands, "yardDaySO", yardDaySO);
        SetField(errands, "ijahNpc", Day3GrayboxBuilder.FindNpc("Npc_Ijah"));
        SetField(errands, "dartoNpc", Day3GrayboxBuilder.FindNpc("Npc_Darto"));
        SetField(errands, "dimasNpc", dimas);
        SetField(errands, "flowerItemDataSO", GameDataBuilder.Flower);
        SetField(errands, "seedItemDataSO", GameDataBuilder.Load<ItemDataSO>("Items", "SawitSeed"));
        SetField(errands, "sickleItemDataSO", YardDayDataBuilder.Sickle);
        SetField(errands, "hoeItemDataSO", YardDayDataBuilder.Hoe);
        SetField(errands, "ropeItemDataSO", YardDayDataBuilder.Rope);
        return errands;
    }

    // Dimas faces the bank with Darto's hoe in his hands; the hole's pile beside him grows with each blow
    public static void BuildDimasDig(Transform director, YardErrands errands, Npc dimas)
    {
        dimas.transform.rotation = Quaternion.Euler(0f, DIMAS_DIG_YAW, 0f);
        Animator animator = (Animator)new SerializedObject(dimas.GetComponentInChildren<NpcVisual>(true)).FindProperty("animator").objectReferenceValue;
        Transform armR = animator.transform.Find("Hips/ArmR");
        Transform oldHoe = armR.Find(HELD_HOE);
        if (oldHoe != null) Object.DestroyImmediate(oldHoe.gameObject);
        GameObject hoe = (GameObject)PrefabUtility.InstantiatePrefab(YardDayDataBuilder.Hoe.prefab.gameObject, armR);
        hoe.name = HELD_HOE;
        hoe.transform.localPosition = HeldHoePosition;
        hoe.transform.localEulerAngles = HeldHoeEuler;
        hoe.transform.localScale = Vector3.one * HELD_HOE_SCALE;

        Transform dig = Root("DimasDig", director).transform;
        dig.SetPositionAndRotation(dimas.transform.position, dimas.transform.rotation);
        Transform strike = Root("StrikePoint", dig).transform;
        strike.localPosition = new Vector3(0f, .05f, 1.2f);
        Transform mound = Part(dig, "DirtMound", GrayboxMeshFactory.Blob(.24f, .2f), GrayboxPalette.Lit("DugSoil", new Color(.33f, .24f, .17f)),
            new Vector3(.6f, 0f, .5f)).transform;

        DimasDigVisual visual = director.gameObject.AddComponent<DimasDigVisual>();
        SetField(visual, "errands", errands);
        SetField(visual, "dimasNpc", dimas);
        SetField(visual, "animator", animator);
        SetField(visual, "hoeModel", hoe);
        SetField(visual, "strikePoint", strike);
        SetField(visual, "dirtMound", mound);
        SetField(visual, "dirtPrefab", VfxBuilder.DigDirt().transform);
    }

    // lines that talk about another villager cut to them (DialogueLine.focusNpcSO)
    public static void BuildDialogueFocus(Transform director, Npc anyNpc)
    {
        CinemachineCamera cam = Day3GrayboxBuilder.ChaseCamera(director, "CM_NpcFocus", anyNpc.transform, FocusCameraOffset, FOCUS_CAMERA_FOV);
        CinemachineRotationComposer composer = cam.GetComponent<CinemachineRotationComposer>();
        composer.TargetOffset = new Vector3(0f, .7f, 0f);
        ScreenComposerSettings composition = composer.Composition;
        composition.ScreenPosition = FocusScreenPosition;
        composer.Composition = composition;
        // never behind a roof or inside a wall
        cam.gameObject.AddComponent<CinemachineDeoccluder>();
        cam.gameObject.AddComponent<CinemachineDecollider>();
        DialogueFocusCameraVisual visual = director.gameObject.AddComponent<DialogueFocusCameraVisual>();
        SetField(visual, "focusCamera", cam);
        SetArray(visual, "npcArray", Object.FindObjectsByType<Npc>(FindObjectsInactive.Include));
    }

    // a wooden crate by the front door with a dark Lv.2 bunch on it
    private static Transform BuildPorchBunch(Transform director, Vector3 position)
    {
        Transform root = Root("PorchBunch", director).transform;
        root.position = position;
        Part(root, "Crate", GrayboxMeshFactory.RoundedBox(new Vector3(.6f, .45f, .5f), .03f), GrayboxPalette.Wood, new Vector3(0f, .225f, 0f));
        GameObject bunch = (GameObject)PrefabUtility.InstantiatePrefab(Day3DataBuilder.SawitBunchLv2.prefab.gameObject, root);
        bunch.name = "SawitBunchLv2";
        bunch.transform.localPosition = new Vector3(0f, .45f, 0f);
        return bunch.transform;
    }

    private static void BuildRope(Transform director, YardDayDirector yard, LandQTE landQTE, Transform stumpModel)
    {
        GameObject rope = Root("Rope", director);
        LineRenderer line = rope.AddComponent<LineRenderer>();
        line.sharedMaterial = GrayboxPalette.Lit("RopeFibre", new Color(.78f, .64f, .44f));
        line.widthMultiplier = .05f;
        line.numCapVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        Transform tied = Root("RopeTie", director).transform;
        tied.position = stumpModel.position + Vector3.up * .4f;
        RopeVisual ropeVisual = rope.AddComponent<RopeVisual>();
        SetField(ropeVisual, "director", yard);
        SetField(ropeVisual, "landQTE", landQTE);
        SetField(ropeVisual, "stumpEnd", tied);
    }

    private static void BuildCameras(Transform director, YardDayDirector yard, Vector3 stump, Vector3 bunch)
    {
        YardCameraVisual cameraVisual = director.gameObject.AddComponent<YardCameraVisual>();
        SetField(cameraVisual, "director", yard);
        // from above the crate, down the overgrown strip between the garden and the river fence
        SetField(cameraVisual, "wishCamera", Day3GrayboxBuilder.FixedCamera(director, "CM_YardWish", new Vector3(-5.2f, 7f, -27.6f),
            Ground(new Vector3(-7.8f, 0f, -33.4f)), 50f));
        // from the south-east, both of them on the rope with the stump on the left
        SetField(cameraVisual, "stumpCamera", Day3GrayboxBuilder.FixedCamera(director, "CM_YardStump", Ground(new Vector3(-4f, 0f, -38.6f)) + Vector3.up * 2.4f,
            stump + new Vector3(1.4f, .7f, 0f), 42f));
        // close on the bunch on the crate, the MC turned away beside it
        SetField(cameraVisual, "pulseCamera", Day3GrayboxBuilder.FixedCamera(director, "CM_YardPulse", bunch + new Vector3(1.4f, 1.1f, 1.6f), bunch, 35f));
    }

    // ---------- data ----------

    private static void LoadData()
    {
        Day2DataBuilder.LoadAll();
        GameDataBuilder.Flower = GameDataBuilder.Load<ItemDataSO>("Items", "Flower");
        GameDataBuilder.SawitLv1 = GameDataBuilder.Load<CropDataSO>("Crops", "SawitLv1");
        Day3DataBuilder.SawitBunchLv2 = GameDataBuilder.Load<ItemDataSO>("Items", "SawitBunchLv2");
        YardDayDataBuilder.LoadAll();
    }

    // every press: 3 flowers make 4 Lv.1 seeds (one for Dimas, three for the new plots)
    private static void SetRecipe(YardDayDirector yard)
    {
        SerializedObject so = new SerializedObject(yard);
        SerializedProperty recipes = so.FindProperty("recipeArray");
        recipes.arraySize = 1;
        SerializedProperty recipe = recipes.GetArrayElementAtIndex(0);
        SerializedProperty inputArray = recipe.FindPropertyRelative("inputArray");
        inputArray.arraySize = 1;
        inputArray.GetArrayElementAtIndex(0).FindPropertyRelative("itemDataSO").objectReferenceValue = GameDataBuilder.Flower;
        inputArray.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 3;
        recipe.FindPropertyRelative("output.itemDataSO").objectReferenceValue = GameDataBuilder.Load<ItemDataSO>("Items", "SawitSeed");
        recipe.FindPropertyRelative("output.amount").intValue = 4;
        recipe.FindPropertyRelative("returned.itemDataSO").objectReferenceValue = null;
        recipe.FindPropertyRelative("returned.amount").intValue = 0;
        recipe.FindPropertyRelative("puzzleSO").objectReferenceValue = GameDataBuilder.Load<FabricatorPuzzleSO>("Puzzles", "Puzzle_00_Intro");
        recipe.FindPropertyRelative("blockedReason").stringValue = "Needs 3 flowers";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // the bedroom's director needs the yard day's morning thought
    private static void WireBedroom(YardDaySO yardDaySO)
    {
        // OpenScene unloads assets loaded before it: load the SO again by path once the bedroom is open
        string path = AssetDatabase.GetAssetPath(yardDaySO);
        EditorSceneManager.OpenScene(HouseInteriorBuilder.SCENE_PATH);
        SetField(GameObject.Find("BedroomDirector").GetComponent<BedroomDirector>(), "yardDaySO", AssetDatabase.LoadAssetAtPath<YardDaySO>(path));
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static void SetText(Object target, string field, string value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Vector3 Ground(Vector3 position) => FixSceneDay2Builder.Ground(position);
}
