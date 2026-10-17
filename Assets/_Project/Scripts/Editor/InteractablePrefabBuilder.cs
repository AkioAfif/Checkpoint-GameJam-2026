using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using static GrayboxBuild;

// Gameplay prefabs: flowers, river docks, farm plots, the Fabricator, villagers and the player's house door.
public static class InteractablePrefabBuilder
{
    private const string FOLDER = "Interactables";

    private static int InteractableLayer => LayerMask.NameToLayer("Interactable");

    public static GameObject FlowerNode()
    {
        GameObject root = Root("FlowerNode");
        ResourceNode node = root.AddComponent<ResourceNode>();
        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = .6f;
        trigger.center = new Vector3(0f, .3f, 0f);
        GameObject visual = Root("Visual", root.transform);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(GameDataBuilder.FlowerModel("FlowerClump", 3), visual.transform);
        model.name = "Model";
        model.AddComponent<PlantWobbleVisual>();
        ResourceNodeVisual nodeVisual = visual.AddComponent<ResourceNodeVisual>();
        SetField(nodeVisual, "resourceNode", node);
        SetField(nodeVisual, "modelGameObject", model);
        SetField(nodeVisual, "burstPrefab", VfxBuilder.CollectBurst().transform);
        AddOutline(root, visual.transform, GrayboxPalette.Outline, .035f);
        SetField(node, "itemDataSO", GameDataBuilder.Flower);
        SetField(node, "promptAnchor", Anchor(root, "PromptAnchor", new Vector3(0f, 1.1f, 0f)));
        SetLayerRecursively(root, InteractableLayer);
        return SavePrefab(root, FOLDER, "FlowerNode");
    }

    // a little wooden dock reaching over the water along local -X
    public static GameObject WaterSpot()
    {
        GameObject root = Root("WaterSpot");
        ResourceNode node = root.AddComponent<ResourceNode>();
        Box(root, new Vector3(-.4f, .3f, 0f), new Vector3(2.2f, 1.2f, 1.8f), true);
        GameObject visual = Root("Visual", root.transform);
        Mesh plank = GrayboxMeshFactory.RoundedBox(new Vector3(1.7f, .12f, .36f), .04f);
        for (int i = 0; i < 3; i++) Part(visual.transform, "Plank", plank, GrayboxPalette.Wood, new Vector3(-.55f, -.06f, -.4f + i * .4f));
        Mesh post = GrayboxMeshFactory.Cylinder(.07f, 1f, .02f, 8);
        foreach (float z in new[] { -.55f, .55f }) Part(visual.transform, "Post", post, GrayboxPalette.WoodDark, new Vector3(-1.3f, -.9f, z));
        GameObject ripple = Part(visual.transform, "Ripple", GrayboxMeshFactory.Torus(.35f, .03f, 20, 6), GrayboxPalette.Lit("Ripple", new Color(.85f, .95f, 1f)), new Vector3(-1.7f, -.47f, 0f));
        ResourceNodeVisual nodeVisual = visual.AddComponent<ResourceNodeVisual>();
        SetField(nodeVisual, "resourceNode", node);
        SetField(nodeVisual, "modelGameObject", ripple);
        SetField(nodeVisual, "burstPrefab", VfxBuilder.CollectBurst().transform);
        SetValue(nodeVisual, "hideOnCollect", 0f);
        AddOutline(root, visual.transform, GrayboxPalette.Outline, .035f);
        SetField(node, "itemDataSO", GameDataBuilder.Water);
        SetField(node, "requiredItemDataSO", GameDataBuilder.EmptyBottle);
        SetValue(node, "isInfinite", 1f);
        SetValue(node, "respawnTime", 0f);
        SetString(node, "promptVerb", "scoop water");
        SetString(node, "blockedReason", "Need an empty bottle");
        SetField(node, "promptAnchor", Anchor(root, "PromptAnchor", new Vector3(-.6f, 1.3f, 0f)));
        SetLayerRecursively(root, InteractableLayer);
        return SavePrefab(root, FOLDER, "WaterSpot");
    }

    public static GameObject FarmPlot()
    {
        GameObject root = Root("FarmPlot");
        FarmPlot plot = root.AddComponent<FarmPlot>();
        Box(root, new Vector3(0f, .4f, 0f), new Vector3(2f, .8f, 2f), true);
        GameObject visual = Root("Visual", root.transform);
        Transform soil = Root("Soil", visual.transform).transform;
        Part(soil, "Tile", GrayboxMeshFactory.RoundedBox(new Vector3(2f, .16f, 2f), .07f), GrayboxPalette.SoilTilled, new Vector3(0f, -.04f, 0f));
        Mesh furrow = GrayboxMeshFactory.RoundedBox(new Vector3(1.7f, .08f, .22f), .04f);
        foreach (float z in new[] { -.55f, 0f, .55f }) Part(soil, "Furrow", furrow, GrayboxPalette.Soil, new Vector3(0f, .1f, z));
        GameObject crop = (GameObject)PrefabUtility.InstantiatePrefab(GameDataBuilder.CropModel("Crop_OnPlot", 1f), visual.transform);
        crop.name = "Crop";
        crop.transform.localPosition = new Vector3(0f, .1f, 0f);
        crop.AddComponent<PlantWobbleVisual>();
        Transform saplingParent = Root("SaplingParent", visual.transform).transform;
        saplingParent.localPosition = new Vector3(0f, .1f, 0f);
        saplingParent.gameObject.AddComponent<PlantWobbleVisual>();

        FarmPlotVisual plotVisual = visual.AddComponent<FarmPlotVisual>();
        SetField(plotVisual, "farmPlot", plot);
        SetField(plotVisual, "cropGameObject", crop);
        SetField(plotVisual, "saplingParent", saplingParent);
        SetField(plotVisual, "burstPrefab", VfxBuilder.CollectBurst().transform);
        // only the soil tile gets the (green) plot outline, so the targeted tile reads clearly
        InteractableOutlineVisual outline = root.AddComponent<InteractableOutlineVisual>();
        SetField(outline, "interactableGameObject", root);
        SetArray(outline, "outlineGameObjectArray", AddOutlineHulls(soil, GrayboxPalette.OutlinePlot, .07f));

        SetField(plot, "cropItemDataSO", GameDataBuilder.Crop);
        SetField(plot, "plantableCropDataSO", GameDataBuilder.SawitLv1);
        SetField(plot, "promptAnchor", Anchor(root, "PromptAnchor", new Vector3(0f, 1.2f, 0f)));
        SetField(plot, "standPoint", Anchor(root, "StandPoint", new Vector3(0f, 0f, -1.45f)));
        SetLayerRecursively(root, InteractableLayer);
        return SavePrefab(root, FOLDER, "FarmPlot");
    }

    public static GameObject Fabricator()
    {
        GameObject root = Root("Fabricator");
        FabricatorController controller = root.AddComponent<FabricatorController>();
        CapsuleCollider body = root.AddComponent<CapsuleCollider>();
        body.center = new Vector3(0f, 1.5f, 0f);
        body.radius = 1f;
        body.height = 3f;
        GameObject visual = Root("Visual", root.transform);
        Transform v = visual.transform;
        Part(v, "Base", GrayboxMeshFactory.Cylinder(1f, .45f, .12f, 24), GrayboxPalette.Metal, Vector3.zero);
        Part(v, "Tube", GrayboxMeshFactory.Cylinder(.78f, 2f, .08f, 24), GrayboxPalette.Glass, new Vector3(0f, .42f, 0f));
        GameObject swirl = Part(v, "Swirl", GrayboxMeshFactory.Blob(.5f, .9f), GrayboxPalette.Lit("FabricatorSwirl", new Color(.75f, .9f, .6f), .8f), new Vector3(0f, .7f, 0f));
        Part(v, "RingLow", GrayboxMeshFactory.Torus(.8f, .07f), GrayboxPalette.Accent, new Vector3(0f, .42f, 0f));
        Part(v, "RingHigh", GrayboxMeshFactory.Torus(.8f, .07f), GrayboxPalette.Accent, new Vector3(0f, 2.3f, 0f));
        Part(v, "Cap", GrayboxMeshFactory.Cylinder(.92f, .3f, .1f, 24), GrayboxPalette.Metal, new Vector3(0f, 2.42f, 0f));
        Part(v, "Hopper", GrayboxMeshFactory.Taper(.28f, .5f, .45f, .05f, 18), GrayboxPalette.Metal, new Vector3(0f, 2.7f, 0f));
        Part(v, "Lamp", GrayboxMeshFactory.Blob(.14f, .2f), GrayboxPalette.Accent, new Vector3(.55f, 2.7f, 0f));
        Part(v, "Tray", GrayboxMeshFactory.RoundedBox(new Vector3(.8f, .14f, .55f), .05f), GrayboxPalette.Accent, new Vector3(0f, .42f, -1f));
        Part(v, "Panel", GrayboxMeshFactory.RoundedBox(new Vector3(.5f, .6f, .2f), .06f), GrayboxPalette.Metal, new Vector3(.75f, .45f, -.55f), new Vector3(0f, -35f, 0f));
        FabricatorVisual fabricatorVisual = visual.AddComponent<FabricatorVisual>();
        SetField(fabricatorVisual, "fabricator", controller);
        SetField(fabricatorVisual, "outputPoint", Anchor(root, "OutputPoint", new Vector3(0f, .6f, -1f)));
        SetField(fabricatorVisual, "seedPopPrefab", VfxBuilder.SeedPop().transform);
        SetField(fabricatorVisual, "bobTransform", swirl.transform);
        AddOutline(root, v, GrayboxPalette.Outline, .04f);

        SetItemAmounts(controller, "recipeArray", new[] { GameDataBuilder.Water, GameDataBuilder.Flower, GameDataBuilder.Crop });
        SetField(controller, "outputItemDataSO", GameDataBuilder.SawitSeed);
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("returnedItem.itemDataSO").objectReferenceValue = GameDataBuilder.EmptyBottle;
        so.FindProperty("returnedItem.amount").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
        SetArray(controller, "puzzleSOArray", new Object[] { GameDataBuilder.PuzzleTutorial, GameDataBuilder.PuzzleNormal });
        SetField(controller, "promptAnchor", Anchor(root, "PromptAnchor", new Vector3(0f, 3.4f, 0f)));
        SetLayerRecursively(root, InteractableLayer);
        return SavePrefab(root, FOLDER, "Fabricator");
    }

    public static GameObject Villager(string name, CharacterBuilder.Look look, NpcSO npcSO)
    {
        GameObject root = Root(name);
        Npc npc = root.AddComponent<Npc>();
        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, .8f, 0f);
        capsule.radius = .35f;
        capsule.height = 1.6f;
        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.radius = .35f;
        agent.height = 1.6f;
        agent.speed = 1.3f;
        agent.angularSpeed = 300f;
        agent.acceleration = 6f;
        agent.stoppingDistance = .2f;
        NpcWander wander = root.AddComponent<NpcWander>();
        SetField(wander, "npc", npc);

        GameObject visual = Root("Visual", root.transform);
        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(CharacterBuilder.RigPrefab(look), visual.transform);
        NpcVisual npcVisual = visual.AddComponent<NpcVisual>();
        SetField(npcVisual, "npc", npc);
        SetField(npcVisual, "npcWander", wander);
        SetField(npcVisual, "animator", rig.GetComponent<Animator>());
        AddOutline(root, rig.transform, GrayboxPalette.Outline, .025f);

        SetField(npc, "npcSO", npcSO);
        SetField(npc, "promptAnchor", Anchor(root, "PromptAnchor", new Vector3(0f, 2.15f * look.scale, 0f)));
        SetLayerRecursively(root, InteractableLayer);
        return SavePrefab(root, "Characters", name);
    }

    // the player's house with an interactable front door
    public static GameObject PlayerHouseWithDoor()
    {
        GameObject root = EnvironmentPrefabBuilder.BuildHouse("PlayerHouse", 6f, 3f, 5.5f, GrayboxPalette.WallPlayer, GrayboxPalette.RoofPlayer);
        Transform door = root.transform.Find("Visual/Door");
        Transform doorFrame = root.transform.Find("Visual/DoorFrame");
        GameObject doorInteract = Root("DoorInteract", root.transform);
        doorInteract.transform.localPosition = door.localPosition + new Vector3(0f, 1f, -.6f);
        doorInteract.layer = InteractableLayer;
        HouseDoor houseDoor = doorInteract.AddComponent<HouseDoor>();
        Box(doorInteract, Vector3.zero, new Vector3(1.8f, 2f, 1.4f), true);
        SetField(houseDoor, "promptAnchor", Anchor(doorInteract, "PromptAnchor", new Vector3(0f, 1.6f, 0f)));
        InteractableOutlineVisual outline = doorInteract.AddComponent<InteractableOutlineVisual>();
        SetField(outline, "interactableGameObject", doorInteract);
        GameObject[] hulls = AddOutlineHulls(door, GrayboxPalette.Outline, .08f);
        GameObject[] frameHulls = AddOutlineHulls(doorFrame, GrayboxPalette.Outline, .08f);
        SetArray(outline, "outlineGameObjectArray", new Object[] { hulls[0], frameHulls[0] });
        return SavePrefab(root, "Environment", "PlayerHouse");
    }

    private static void AddOutline(GameObject root, Transform visual, Material material, float thickness)
    {
        InteractableOutlineVisual outline = root.AddComponent<InteractableOutlineVisual>();
        SetField(outline, "interactableGameObject", root);
        SetArray(outline, "outlineGameObjectArray", AddOutlineHulls(visual, material, thickness));
    }

    private static Transform Anchor(GameObject parent, string name, Vector3 localPosition)
    {
        GameObject anchor = Root(name, parent.transform);
        anchor.transform.localPosition = localPosition;
        return anchor.transform;
    }

    private static void SetItemAmounts(Object target, string field, ItemDataSO[] items)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty array = so.FindProperty(field);
        array.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            array.GetArrayElementAtIndex(i).FindPropertyRelative("itemDataSO").objectReferenceValue = items[i];
            array.GetArrayElementAtIndex(i).FindPropertyRelative("amount").intValue = 1;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetString(Object target, string field, string value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
