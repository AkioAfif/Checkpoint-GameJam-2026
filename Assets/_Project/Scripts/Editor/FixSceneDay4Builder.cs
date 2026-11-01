using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static GrayboxBuild;

// Builds "Day 4: Room to Grow" (two weeks after the yard day) on the CozyFarm map as FixScene_Day4 (Tools > FixScene > Build
// Day 4 Scene). It copies FixScene_Day3 (so build Days 2 and 3 first), keeps its crate, Rigel and resin stumps (Day2_Graybox)
// and the four plots the MC dug by hand, swaps the yard day's director for this day's (the Day3* classes, named before the
// yard day came in), and lays the new land out for this map:
// - the new field is the forest grass across the river from the garden, by the pond: the MC pictures a see-through farm on
//   it with its trees gone (ImaginationVisual), punches three trees down (TreeClearing), then pounds four plots up;
// - the flashback stands the MC and the Distributor beside the Fabricator, framed like Day 1's meeting shot;
// - afterwards he pounds a second bridge up out of the river right by his garden (BridgeBuild), a copy of the map's bridge.
// Re-runnable: FixScene_Day4 is regenerated each time, so tune the map in FixScene and rebuild Days 2, 3, then 4.
public static class FixSceneDay4Builder
{
    public const string SCENE_PATH = "Assets/_Project/Scenes/FixScene_Day4.unity";
    private const string GAMEPLAY_ROOT = "Gameplay";
    private const string GRAYBOX_ROOT = "Day3_Graybox";
    private const string DIRECTOR_ROOT = "Day3";
    // the yard day's 3x4 garden (on from Day 4) and the old 3x3 one under CozyFarm (off from Day 4), for the flashback
    private const string EXPANDED_GARDEN_PATH = "Day3Yard_Graybox/Garden_3x4";
    private const string OLD_GARDEN_NAME = "Player_Garden";

    // the new field: four plots in a square between the river's east bank (x ~9.5) and the pond (x 20-26)
    private static readonly Vector3 FieldCentre = new Vector3(15.6f, 0f, -29.4f);
    private const float PLOT_SPACING = 2.25f;
    // on the bank side of the plots, facing west (the camera), so the plots rise behind the MC
    private static readonly Vector3 SmashPosition = new Vector3(12.9f, 0f, -29.4f);
    private const float SMASH_YAW = -90f;
    // the field's west edge, looking east over it toward the forest
    private static readonly Vector3 EveningPosition = new Vector3(12.9f, 0f, -29.4f);
    // up the east bank toward the old bridge, on his way home (he is switched off there for the day)
    private static readonly Vector3 RigelHomewardPosition = new Vector3(12.6f, 0f, -6.5f);

    // the trees the player punches: the three round-crowned ones FixSceneMapBuilder planted (a pine's low branches would hide the MC)
    private static readonly string[] MarkedTreePathArray =
    {
        "CozyFarm/Day3ClearingTrees/ClearingTree_1",
        "CozyFarm/Day3ClearingTrees/ClearingTree_2",
        "CozyFarm/Day3ClearingTrees/ClearingTree_3",
    };
    // the map's pine and bushes on the field: they go down with the last tree
    private static readonly string[] ExtraTreePathArray = { "CozyFarm/Forest_Tree_2_038", "CozyFarm/Forest_Bush_2_011", "CozyFarm/Forest_Bush_3_018" };
    // metres from the trunk the MC stands to punch
    private const float PUNCH_DISTANCE = 1.3f;

    // the second bridge: the map's bridge (centre z -2) moved down the river to just south of the garden's latitude
    private const string BRIDGE_SOURCE_PATH = "CozyFarm/River_Bridge";
    private const float BRIDGE_Z = -32.8f;
    private const string FENCE_PATH = "CozyFarm/Village_Fences";
    private const string MODELS_FOLDER = "Assets/_Project/Art/Models/Day3";
    // how far in from the bridge's west end the fence is cleared, and how far past the deck's sides
    private const float FENCE_GAP_REACH = 3f;
    private const float FENCE_GAP_MARGIN = .6f;
    // how far short of the bridge's middle (toward the garden) Rigel stops, facing the MC
    private const float RIGEL_STOP_DISTANCE = 1.8f;

    // Day 1's meeting shot as FixScene has it (CM_Meeting + Day1CameraVisual on its Day1 root): side-on to the pair, from the
    // north (the gameplay camera's side), 8 m back and 6.5 m up, FOV 40, aimed 0.4 m above the pair
    private const float MEETING_DISTANCE = 8f;
    private const float MEETING_HEIGHT = 6.5f;
    private static readonly Vector3 MeetingOpeningSide = new Vector3(0f, 0f, 1f);
    private const float MEETING_FOV = 40f;
    private static readonly Vector3 MeetingTargetOffset = new Vector3(0f, .4f, 0f);

    [MenuItem("Tools/FixScene/Build Day 4 Scene")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FixSceneDay3Builder.SCENE_PATH) == null)
        {
            Debug.LogError($"{FixSceneDay3Builder.SCENE_PATH} is missing: run Tools > FixScene > Build Day 3 Scene first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // overwrite the file, not delete + copy: the scene keeps its GUID (and its .meta stays out of every diff)
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) == null) AssetDatabase.CopyAsset(FixSceneDay3Builder.SCENE_PATH, SCENE_PATH);
        else
        {
            System.IO.File.Copy(FixSceneDay3Builder.SCENE_PATH, SCENE_PATH, true);
            AssetDatabase.ImportAsset(SCENE_PATH, ImportAssetOptions.ForceUpdate);
        }
        EditorSceneManager.OpenScene(SCENE_PATH);
        // after opening: OpenScene unloads assets loaded before it, and references to them would save as missing
        Day3SO day3SO = LoadData();

        Transform gameplay = GameObject.Find(GAMEPLAY_ROOT).transform;
        // two weeks after the yard day: its story leaves, its four plots stay (and join the field below)
        FixSceneDay3Builder.StripYardDay(gameplay);
        // the garden's plots, before the new field's join the scene
        FarmPlot[] fieldPlots = Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include).Where(p => p.gameObject.activeSelf).ToArray();
        Transform graybox = Root(GRAYBOX_ROOT, gameplay).transform;
        Transform director = Root(DIRECTOR_ROOT, gameplay).transform;
        (GameObject plotsRoot, FarmPlot[] landPlots) = BuildField(graybox);
        Renderer crater = Day3GrayboxBuilder.BuildCrater(graybox);

        SellCrate crate = Object.FindAnyObjectByType<SellCrate>(FindObjectsInactive.Include);
        SetArray(crate, "sellableItemDataSOArray", new Object[] { Day2DataBuilder.SawitBunch, Day3DataBuilder.SawitBunchLv2 });

        Transform points = Root("Points", director).transform;
        Transform eveningPoint = Day2GrayboxBuilder.Point(points, "EveningPoint", Ground(EveningPosition), 90f);
        Transform landCentre = Day2GrayboxBuilder.Point(points, "LandCentre", Ground(FieldCentre), 0f);
        Transform smashPoint = Day2GrayboxBuilder.Point(points, "SmashPoint", Ground(SmashPosition), SMASH_YAW);
        Transform rigelHousePoint = Day2GrayboxBuilder.Point(points, "RigelHousePoint", Ground(RigelHomewardPosition), 0f);
        // the flashback: the two of them in front of the Fabricator, the day it arrived; on its north side, so Day 1's meeting angle
        // (from the north) films them with the machine behind them, not in the way
        FabricatorController fabricator = Object.FindAnyObjectByType<FabricatorController>(FindObjectsInactive.Include);
        Vector3 distributorPosition = Ground(fabricator.transform.position + new Vector3(1f, 0f, 2f));
        Transform flashbackDistributorPoint = Day2GrayboxBuilder.Point(points, "FlashbackDistributorPoint", distributorPosition, -90f);
        Transform flashbackPlayerPoint = Day2GrayboxBuilder.Point(points, "FlashbackPlayerPoint", Ground(distributorPosition + new Vector3(-1.9f, 0f, 0f)), 90f);

        Npc rigel = Day3GrayboxBuilder.FindNpc("Npc_Rigel");
        Transform day2Graybox = gameplay.Find("Day2_Graybox");
        Day3Director day3 = Day3GrayboxBuilder.AddDirector(director, day3SO, fabricator, fieldPlots, landPlots, plotsRoot, rigel,
            day2Graybox.Find("Points/MorningPoint"), eveningPoint, RenderSettings.sun, day2Graybox.Find("DoorLamp").gameObject);

        LandSequence landSequence = Day3GrayboxBuilder.BuildLandSequence(director, day3SO, plotsRoot, rigel, landCentre, smashPoint, rigelHousePoint,
            flashbackPlayerPoint, flashbackDistributorPoint, out Npc distributor);
        SetField(day3, "landSequence", landSequence);
        Day3GrayboxBuilder.BuildLandVisuals(director, landSequence, plotsRoot, null, crater, smashPoint, distributor);
        SetFlashbackSet(director, fieldPlots, crate, rigel);

        LandQTE landQTE = director.GetComponent<LandQTE>();
        CutsceneActor playerActor = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include).GetComponent<CutsceneActor>();
        Transform dust = VfxBuilder.SmashDust().transform;
        TreeClearing treeClearing = BuildTreeClearing(director, graybox, day3SO, landQTE, playerActor, dust, out Transform[] fieldTreeArray);
        BridgeBuild bridgeBuild = BuildBridge(director, graybox, day3SO, landQTE, playerActor, out Transform bridgePoint, out GameObject ghostBridge);
        RigelBridgeConfrontation confrontation = BuildConfrontation(director, day3SO, rigel, bridgePoint, out CinemachineCamera confrontCamera);
        SetField(landSequence, "treeClearing", treeClearing);
        SetField(landSequence, "bridgeBuild", bridgeBuild);
        SetField(landSequence, "rigelBridgeConfrontation", confrontation);

        ImaginationVisual imagination = director.gameObject.AddComponent<ImaginationVisual>();
        SetField(imagination, "landSequence", landSequence);
        SetField(imagination, "bridgeBuild", bridgeBuild);
        SetField(imagination, "ghostFarmRoot", BuildGhostFarm(graybox, landPlots));
        SetField(imagination, "ghostBridgeRoot", ghostBridge);
        SetArray(imagination, "imaginedAwayTreeArray", fieldTreeArray);

        Day3CameraVisual cameraVisual = BuildCameras(director, day3, landSequence, treeClearing, bridgeBuild, rigel, distributor, distributorPosition, smashPoint, bridgePoint);
        SetField(cameraVisual, "rigelBridgeConfrontation", confrontation);
        SetField(cameraVisual, "confrontCamera", confrontCamera);
        SmashImpactVisual impact = director.GetComponent<SmashImpactVisual>();
        SetField(impact, "treeClearing", treeClearing);
        SetField(impact, "bridgeBuild", bridgeBuild);
        SetField(impact, "bridgePoint", bridgePoint);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Day2GrayboxBuilder.AddToBuildProfile(SCENE_PATH);
        Day3GrayboxBuilder.WireBedroom(day3SO);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(SCENE_PATH);
        Debug.Log($"Day 4 built into {SCENE_PATH} on the CozyFarm map. Press Play in it to test Day 4 on its own.");
    }

    // ---------- the new field ----------

    // four bare plots in a square, hidden (and sunk by LandRiseVisual) until the MC pounds them up
    private static (GameObject plotsRoot, FarmPlot[] landPlots) BuildField(Transform graybox)
    {
        GameObject plotsRoot = Root("LandPlots", graybox);
        GameObject plot = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/Interactables/FarmPlot.prefab");
        Vector3[] positionArray = PlotPositionArray();
        FarmPlot[] plots = new FarmPlot[positionArray.Length];
        for (int i = 0; i < positionArray.Length; i++)
        {
            plots[i] = Spawn(plot, plotsRoot.transform, positionArray[i], 0f).GetComponent<FarmPlot>();
            SerializedObject so = new SerializedObject(plots[i]);
            so.FindProperty("startState").enumValueIndex = (int)FarmPlot.State.Empty;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        plotsRoot.SetActive(false);
        return (plotsRoot, plots);
    }

    // all four on the ground height at the field's centre, so they rise as one level block
    private static Vector3[] PlotPositionArray()
    {
        float y = Ground(FieldCentre).y;
        float half = PLOT_SPACING * .5f;
        return new[]
        {
            new Vector3(FieldCentre.x - half, y, FieldCentre.z + half),
            new Vector3(FieldCentre.x + half, y, FieldCentre.z + half),
            new Vector3(FieldCentre.x - half, y, FieldCentre.z - half),
            new Vector3(FieldCentre.x + half, y, FieldCentre.z - half),
        };
    }

    // what the MC pictures on the grass: the plots with ripe Lv.2 palms on them, models only (ImaginationVisual makes them see-through)
    private static GameObject BuildGhostFarm(Transform graybox, FarmPlot[] landPlots)
    {
        GameObject root = Root("GhostFarm", graybox);
        GameObject plot = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS}/Interactables/FarmPlot.prefab");
        foreach (Vector3 position in PlotPositionArray())
        {
            GameObject ghostPlot = ModelCopy(plot, root.transform, position);
            // FarmPlotVisual decides what grows on a real plot; the picture shows a ripe palm on every one
            foreach (string part in new[] { "Visual/Crop", "Visual/SaplingParent" })
            {
                Transform child = ghostPlot.transform.Find(part);
                if (child != null) Object.DestroyImmediate(child.gameObject);
            }
            ModelCopy(Day2DataBuilder.SawitLv2.ripePrefab.gameObject, root.transform, position + Vector3.up * .1f);
        }
        root.SetActive(false);
        return root;
    }

    // ---------- trees ----------

    private static TreeClearing BuildTreeClearing(Transform director, Transform graybox, Day3SO day3SO, LandQTE landQTE, CutsceneActor playerActor,
        Transform dust, out Transform[] fieldTreeArray)
    {
        Transform targets = Root("PunchTargets", graybox).transform;
        PunchableTree[] marked = MarkedTreePathArray.Select(path => PunchTarget(targets, path, true, dust)).ToArray();
        PunchableTree[] extra = ExtraTreePathArray.Select(path => PunchTarget(targets, path, false, dust)).ToArray();
        fieldTreeArray = marked.Concat(extra).Select(t => t.GetTreeTransform()).ToArray();

        TreeClearing treeClearing = director.gameObject.AddComponent<TreeClearing>();
        SetArray(treeClearing, "treeObjectiveArray", day3SO.treeObjectiveArray);
        SetField(treeClearing, "treesIdeaDialogueSO", day3SO.treesIdeaDialogueSO);
        SetField(treeClearing, "firstTreeDialogueSO", day3SO.firstTreeDialogueSO);
        SetField(treeClearing, "treesClearedDialogueSO", day3SO.treesClearedDialogueSO);
        SetField(treeClearing, "landQTE", landQTE);
        SetField(treeClearing, "playerActor", playerActor);
        SetArray(treeClearing, "markedTreeArray", marked);
        SetArray(treeClearing, "extraTreeArray", extra);
        return treeClearing;
    }

    // the interactable sits at the trunk's foot (the model's pivot can be anywhere); punchable ones get a trigger and an outline
    private static PunchableTree PunchTarget(Transform parent, string treePath, bool isPunchable, Transform dust) =>
        PunchTarget(parent, GameObject.Find(treePath), isPunchable, dust);

    // also Day 5's (FixSceneDay5Builder), on trees it picks by position
    public static PunchableTree PunchTarget(Transform parent, GameObject tree, bool isPunchable, Transform dust)
    {
        // it falls at runtime, so it can't be static-batched
        GameObjectUtility.SetStaticEditorFlags(tree, 0);
        Bounds bounds = tree.GetComponent<Renderer>().bounds;
        GameObject target = Root($"PunchTarget_{tree.name}", parent);
        target.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        PunchableTree punchable = target.AddComponent<PunchableTree>();
        SetField(punchable, "treeTransform", tree.transform);
        // the gameplay camera looks south from the north: the MC punches from the north side, so the tree topples away from it
        Transform punchPoint = Root("PunchPoint", target.transform).transform;
        punchPoint.position = Ground(target.transform.position + Vector3.forward * PUNCH_DISTANCE);
        SetField(punchable, "punchPoint", punchPoint);
        Transform anchor = Root("PromptAnchor", target.transform).transform;
        anchor.localPosition = Vector3.up * 2.2f;
        SetField(punchable, "promptAnchor", anchor);
        TreeFallVisual fall = target.AddComponent<TreeFallVisual>();
        SetField(fall, "punchableTree", punchable);
        SetField(fall, "dustPrefab", dust);
        if (!isPunchable) return punchable;

        target.layer = LayerMask.NameToLayer("Interactable");
        CapsuleCollider trunk = target.AddComponent<CapsuleCollider>();
        trunk.isTrigger = true;
        trunk.radius = 1f;
        trunk.height = 2.6f;
        trunk.center = Vector3.up * 1.3f;
        InteractableOutlineVisual outline = target.AddComponent<InteractableOutlineVisual>();
        SetField(outline, "interactableGameObject", target);
        SetArray(outline, "outlineGameObjectArray", AddOutlineHulls(tree.transform, GrayboxPalette.Outline, .04f));
        return punchable;
    }

    // ---------- the second bridge ----------

    private static BridgeBuild BuildBridge(Transform director, Transform graybox, Day3SO day3SO, LandQTE landQTE, CutsceneActor playerActor,
        out Transform bridgePoint, out GameObject ghostBridge)
    {
        GameObject source = GameObject.Find(BRIDGE_SOURCE_PATH);
        Bounds sourceBounds = source.GetComponent<Renderer>().bounds;
        // same span over the river, moved downstream; raised or lowered by how much the banks differ there
        float shiftZ = BRIDGE_Z - sourceBounds.center.z;
        float bankRise = BankHeight(sourceBounds, shiftZ) - BankHeight(sourceBounds, 0f);
        Vector3 shift = new Vector3(0f, bankRise, shiftZ);

        GameObject bridgeRoot = Root("NewBridge", graybox);
        CopyMesh(source, bridgeRoot.transform, shift, true);
        bridgeRoot.SetActive(false);
        ghostBridge = Root("GhostBridge", graybox);
        CopyMesh(source, ghostBridge.transform, shift, false);
        ghostBridge.SetActive(false);

        CutFenceGap(new Bounds(sourceBounds.center + shift, sourceBounds.size));

        // on the field's bank, at the bridge's east end, facing across the river
        Vector3 eastEnd = new Vector3(sourceBounds.max.x + .7f, 0f, BRIDGE_Z);
        bridgePoint = Day2GrayboxBuilder.Point(director.Find("Points"), "BridgePoint", Ground(eastEnd), -90f);

        BridgeBuild bridgeBuild = director.gameObject.AddComponent<BridgeBuild>();
        SetField(bridgeBuild, "day3SO", day3SO);
        SetField(bridgeBuild, "landQTE", landQTE);
        SetField(bridgeBuild, "playerActor", playerActor);
        SetField(bridgeBuild, "bridgeRoot", bridgeRoot);
        SetField(bridgeBuild, "bridgePoint", bridgePoint);
        BridgeRiseVisual rise = director.gameObject.AddComponent<BridgeRiseVisual>();
        SetField(rise, "bridgeBuild", bridgeBuild);
        SetField(rise, "bridgeRoot", bridgeRoot.transform);
        return bridgeBuild;
    }

    // the garden's riverside fence runs right across the bridge's west end: this scene's fence gets a copy of the map's fence
    // mesh with the rails and posts there taken out (FixScene and Day 2 keep the whole fence)
    private static void CutFenceGap(Bounds bridge)
    {
        GameObject fence = GameObject.Find(FENCE_PATH);
        Mesh source = fence.GetComponent<MeshFilter>().sharedMesh;
        Mesh cut = Object.Instantiate(source);
        cut.name = source.name + "_BridgeGap";
        Vector3[] vertexArray = source.vertices;
        float minX = bridge.min.x - 1f;
        float maxX = bridge.min.x + FENCE_GAP_REACH;
        // a rail spans post to post, so the gap runs a little past the deck on both sides
        float minZ = bridge.min.z - FENCE_GAP_MARGIN;
        float maxZ = bridge.max.z + FENCE_GAP_MARGIN;
        int removed = 0;
        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            int[] triangleArray = source.GetTriangles(sub);
            List<int> keptList = new List<int>(triangleArray.Length);
            for (int i = 0; i < triangleArray.Length; i += 3)
            {
                Vector3 centre = fence.transform.TransformPoint((vertexArray[triangleArray[i]] + vertexArray[triangleArray[i + 1]] + vertexArray[triangleArray[i + 2]]) / 3f);
                if (centre.x > minX && centre.x < maxX && centre.z > minZ && centre.z < maxZ)
                {
                    removed++;
                    continue;
                }
                keptList.Add(triangleArray[i]);
                keptList.Add(triangleArray[i + 1]);
                keptList.Add(triangleArray[i + 2]);
            }
            cut.SetTriangles(keptList, sub);
        }
        string path = $"{MODELS_FOLDER}/{cut.name}.asset";
        if (!AssetDatabase.IsValidFolder(MODELS_FOLDER)) AssetDatabase.CreateFolder("Assets/_Project/Art/Models", "Day3");
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(cut, path);
        fence.GetComponent<MeshFilter>().sharedMesh = cut;
        if (fence.TryGetComponent(out MeshCollider collider)) collider.sharedMesh = cut;
        Debug.Log($"Cut {removed} fence triangles out where the new bridge lands.");
    }

    // the average ground height just past both ends of the bridge, moved 'shiftZ' along the river
    private static float BankHeight(Bounds bridge, float shiftZ)
    {
        float z = bridge.center.z + shiftZ;
        return (Ground(new Vector3(bridge.min.x - .5f, 0f, z)).y + Ground(new Vector3(bridge.max.x + .5f, 0f, z)).y) * .5f;
    }

    // a plain copy (mesh, materials, optionally the collider) of a piece of the imported map, moved by 'shift'
    private static void CopyMesh(GameObject source, Transform parent, Vector3 shift, bool hasCollider)
    {
        GameObject copy = Root(source.name, parent);
        copy.transform.SetPositionAndRotation(source.transform.position + shift, source.transform.rotation);
        copy.transform.localScale = source.transform.lossyScale;
        Mesh mesh = source.GetComponent<MeshFilter>().sharedMesh;
        copy.AddComponent<MeshFilter>().sharedMesh = mesh;
        copy.AddComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
        if (hasCollider) copy.AddComponent<MeshCollider>().sharedMesh = mesh;
        else copy.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    // the flashback is the day the Fabricator arrived (Day 1): the old 3x3 garden with empty beds, no ICAN crate, no Rigel
    public static void SetFlashbackSet(Transform director, FarmPlot[] gardenPlots, SellCrate crate, Npc rigel)
    {
        Transform gameplay = director.parent;
        List<Object> presentList = new List<Object> { gameplay.Find(EXPANDED_GARDEN_PATH).gameObject, crate.gameObject, rigel.gameObject };
        presentList.AddRange(gardenPlots.Select(plot => (Object)plot.gameObject));
        FlashbackVisual flashback = director.GetComponent<FlashbackVisual>();
        SetArray(flashback, "presentOnlyArray", presentList.ToArray());
        SetArray(flashback, "pastOnlyArray", new Object[] { GameObject.Find("CozyFarm").transform.Find(OLD_GARDEN_NAME).gameObject });
    }

    // Rigel steps on at the bridge's garden (west) end and stops the MC halfway across, filmed side-on from downstream
    private static RigelBridgeConfrontation BuildConfrontation(Transform director, Day3SO day3SO, Npc rigel, Transform bridgePoint, out CinemachineCamera confrontCamera)
    {
        Bounds sourceBounds = GameObject.Find(BRIDGE_SOURCE_PATH).GetComponent<Renderer>().bounds;
        Transform points = director.Find("Points");
        float midX = sourceBounds.center.x;
        // the deck is off at build time, so heights come from the east bank; Rigel's stop takes the MC's height at runtime
        float deckY = bridgePoint.position.y;
        Transform mid = Day2GrayboxBuilder.Point(points, "BridgeMidPoint", new Vector3(midX, deckY, BRIDGE_Z), -90f);
        Transform entry = Day2GrayboxBuilder.Point(points, "RigelBridgeEntryPoint", Ground(new Vector3(sourceBounds.min.x - .7f, 0f, BRIDGE_Z)), 90f);
        Transform stop = Day2GrayboxBuilder.Point(points, "RigelBridgeStopPoint", new Vector3(midX - RIGEL_STOP_DISTANCE, deckY, BRIDGE_Z), 90f);

        RigelBridgeConfrontation confrontation = director.gameObject.AddComponent<RigelBridgeConfrontation>();
        SetField(confrontation, "day3SO", day3SO);
        SetField(confrontation, "rigelNpc", rigel);
        SetField(confrontation, "rigelActor", rigel.GetComponent<CutsceneActor>());
        SetField(confrontation, "bridgeMidPoint", mid);
        SetField(confrontation, "rigelEntryPoint", entry);
        SetField(confrontation, "rigelStopPoint", stop);

        // downstream (south) and above the railing: the two of them face to face, whole, above the dialogue box
        Vector3 between = new Vector3(midX - RIGEL_STOP_DISTANCE / 2f, deckY + .9f, BRIDGE_Z);
        confrontCamera = Day3GrayboxBuilder.FixedCamera(director, "CM_Day3BridgeConfront", between + new Vector3(0f, 3.2f, -6.5f), between + Vector3.down * .3f, 40f);
        confrontCamera.gameObject.AddComponent<CinemachineDeoccluder>();
        return confrontation;
    }

    // ---------- cameras ----------

    private static Day3CameraVisual BuildCameras(Transform director, Day3Director day3, LandSequence landSequence, TreeClearing treeClearing, BridgeBuild bridgeBuild, Npc rigel, Npc distributor,
        Vector3 distributorPosition, Transform smashPoint, Transform bridgePoint)
    {
        Day3CameraVisual cameraVisual = director.gameObject.AddComponent<Day3CameraVisual>();
        SetField(cameraVisual, "director", day3);
        SetField(cameraVisual, "landSequence", landSequence);
        SetField(cameraVisual, "brain", Camera.main.GetComponent<CinemachineBrain>());
        // the evening: from the field's bank corner, over the new plots and the pond into the forest
        SetField(cameraVisual, "forestCamera", Day3GrayboxBuilder.FixedCamera(director, "CM_Day3Forest", Ground(new Vector3(11.2f, 0f, -35f)) + Vector3.up * 5f,
            new Vector3(24f, 1.8f, -26f), 45f));
        // from above the garden's bank, across the river at the forest grass the MC is looking at from the crate
        SetField(cameraVisual, "emptyLandCamera", Day3GrayboxBuilder.FixedCamera(director, "CM_Day3EmptyLand", new Vector3(3f, 5.5f, -36f),
            Ground(FieldCentre) + Vector3.up * .5f, 40f));

        // Day 1's meeting framing: side-on to the pair, the camera on the south-east side of the line between them
        CinemachineTargetGroup pair = Root("FlashbackPair", director).AddComponent<CinemachineTargetGroup>();
        Transform player = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include).transform;
        pair.AddMember(player, 1f, .6f);
        pair.AddMember(distributor.transform, 1f, .6f);
        Vector3 line = Vector3.right;
        Vector3 side = Vector3.Cross(Vector3.up, line).normalized;
        if (Vector3.Dot(side, MeetingOpeningSide) < 0f) side = -side;
        CinemachineCamera two = Day3GrayboxBuilder.ChaseCamera(director, "CM_Day3FlashbackTwo", pair.transform, side * MEETING_DISTANCE + Vector3.up * MEETING_HEIGHT, MEETING_FOV);
        CinemachineRotationComposer composer = two.GetComponent<CinemachineRotationComposer>();
        composer.TargetOffset = MeetingTargetOffset;
        composer.Damping = new Vector2(.3f, .3f);
        two.GetComponent<CinemachineFollow>().TrackerSettings.PositionDamping = new Vector3(.6f, .6f, .6f);
        // like CM_Meeting: never behind a roof or inside a wall
        two.gameObject.AddComponent<CinemachineDeoccluder>();
        two.gameObject.AddComponent<CinemachineDecollider>();
        SetField(cameraVisual, "flashbackTwoCamera", two);

        // low on the bank between the MC and the river, looking back at them and the plots behind
        Vector3 smash = smashPoint.position;
        Vector3 forward = smashPoint.forward;
        Vector3 aside = Vector3.Cross(forward, Vector3.up);
        Day3GrayboxBuilder.AddSharedCameras(director, cameraVisual, rigel, distributorPosition,
            smash + forward * 2.25f + aside * 1.4f + Vector3.up * 1.05f, smash + Vector3.up * .55f - forward * .4f,
            smash + forward * 1.9f + aside * 1.2f + Vector3.up * .75f, smash + Vector3.up * .45f);

        // the river from the field's south-east corner, the garden beyond: where the bridge should go
        SetField(cameraVisual, "bridgeBuild", bridgeBuild);
        Vector3 bridgeMiddle = new Vector3(3.2f, bridgePoint.position.y, BRIDGE_Z);
        CinemachineCamera idea = Day3GrayboxBuilder.FixedCamera(director, "CM_Day3BridgeIdea", Ground(new Vector3(15f, 0f, -40.5f)) + Vector3.up * 6.5f, bridgeMiddle, 45f);
        // from downstream, the MC crouched on the right and the bridge climbing out of the water toward the garden
        CinemachineCamera smashCam = Day3GrayboxBuilder.FixedCamera(director, "CM_Day3BridgeSmash", Ground(new Vector3(13.5f, 0f, -40.5f)) + Vector3.up * 2.8f,
            new Vector3(9.5f, bridgePoint.position.y + .3f, BRIDGE_Z), 50f);
        SetField(cameraVisual, "bridgeIdeaCamera", idea);
        SetField(cameraVisual, "bridgeSmashCamera", smashCam);

        // due west of the MC (the river side) while he punches a tree (he stands north of it, it falls south): the trunk stays off the sight line; aimed between the
        // two: the gameplay camera would look straight through the canopy he's standing under
        SetField(cameraVisual, "treeClearing", treeClearing);
        CinemachineCamera treePunch = Day3GrayboxBuilder.ChaseCamera(director, "CM_Day3TreePunch", player, new Vector3(-6f, 1.7f, .6f), 50f);
        treePunch.GetComponent<CinemachineRotationComposer>().TargetOffset = new Vector3(0f, 1.1f, .8f);
        // other trees stand all round: slide in front of any between the camera and the MC
        treePunch.gameObject.AddComponent<CinemachineDeoccluder>();
        treePunch.gameObject.AddComponent<CinemachineDecollider>();
        SetField(cameraVisual, "treePunchCamera", treePunch);

        SmashImpactVisual impact = director.GetComponent<SmashImpactVisual>();
        SerializedObject so = new SerializedObject(impact);
        SerializedProperty punchCameras = so.FindProperty("punchCameraArray");
        foreach (CinemachineCamera cam in new[] { smashCam, treePunch })
        {
            punchCameras.arraySize++;
            punchCameras.GetArrayElementAtIndex(punchCameras.arraySize - 1).objectReferenceValue = cam;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        Day3GrayboxBuilder.AddImpulseListeners();
        return cameraVisual;
    }

    // ---------- helpers ----------

    // a prefab's models only: no logic, colliders or prefab link, so it can be shown as a picture
    public static GameObject ModelCopy(GameObject prefab, Transform parent, Vector3 position)
    {
        GameObject copy = Spawn(prefab, parent, position, 0f);
        PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (MonoBehaviour behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
        foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        foreach (Renderer part in copy.GetComponentsInChildren<Renderer>(true)) part.shadowCastingMode = ShadowCastingMode.Off;
        SetLayerRecursively(copy, 0);
        return copy;
    }

    private static Vector3 Ground(Vector3 position) => FixSceneDay2Builder.Ground(position);

    private static Day3SO LoadData()
    {
        Day2DataBuilder.LoadAll();
        GameDataBuilder.EmptyBottle = GameDataBuilder.Load<ItemDataSO>("Items", "EmptyBottle");
        GameDataBuilder.Water = GameDataBuilder.Load<ItemDataSO>("Items", "Water");
        GameDataBuilder.Flower = GameDataBuilder.Load<ItemDataSO>("Items", "Flower");
        GameDataBuilder.SawitLv1 = GameDataBuilder.Load<CropDataSO>("Crops", "SawitLv1");
        return Day3DataBuilder.BuildCozyFarm();
    }
}
