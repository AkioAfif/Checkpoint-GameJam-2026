using TMPro;
using UnityEditor;
using UnityEngine;
using static GrayboxBuild;

// Day 2 prefabs and models: the item models (resin, sawit bunch, Lv.2 seed), the palms (yesterday's ripe one, the Lv.2
// sapling and its grown form), ICAN's sell crate, the resin node at the forest edge, the savings book and Rigel.
public static class Day2PrefabBuilder
{
    private static int InteractableLayer => LayerMask.NameToLayer("Interactable");

    private static Material Amber => GrayboxPalette.Lit("Resin", new Color(.88f, .55f, .16f), .9f);
    private static Material PalmFruit => GrayboxPalette.Lit("SawitFruit", new Color(.82f, .28f, .14f), .5f);
    private static Material SawitLeaf => GrayboxPalette.Lit("SawitLeaf", new Color(.56f, .74f, .36f));
    // the forest-resin palm: darker, bluer leaves and a red heart instead of the warm glow
    private static Material SawitLeafLv2 => GrayboxPalette.Lit("SawitLeafLv2", new Color(.27f, .46f, .36f));
    private static Material GlowLv2 => GrayboxPalette.Lit("SawitGlowLv2", new Color(.78f, .16f, .2f), .7f);
    private static Material IcanBlue => GrayboxPalette.Lit("IcanBlue", new Color(.22f, .42f, .72f));

    // ---------- item models ----------

    public static GameObject ResinModel()
    {
        GameObject root = Root("Item_Resin");
        Part(root.transform, "Drop", GrayboxMeshFactory.Blob(.14f, .22f), Amber, Vector3.zero);
        Part(root.transform, "DropSmall", GrayboxMeshFactory.Blob(.08f, .12f), Amber, new Vector3(.14f, 0f, .05f));
        return SavePrefab(root, "Items", "Item_Resin");
    }

    public static GameObject BunchModel()
    {
        GameObject root = Root("Item_SawitBunch");
        Part(root.transform, "Core", GrayboxMeshFactory.Blob(.17f, .3f), GrayboxPalette.Lit("SawitFruitDark", new Color(.42f, .14f, .1f)), Vector3.zero);
        Mesh fruit = GrayboxMeshFactory.Blob(.075f, .1f, 10, 5);
        for (int i = 0; i < 9; i++)
        {
            float a = i * 40f * Mathf.Deg2Rad;
            float y = .05f + (i % 3) * .08f;
            Part(root.transform, "Fruit", fruit, PalmFruit, new Vector3(Mathf.Cos(a) * .15f, y, Mathf.Sin(a) * .15f));
        }
        Part(root.transform, "Stem", GrayboxMeshFactory.Cylinder(.025f, .12f, .01f, 8), GrayboxPalette.Trunk, new Vector3(0f, .28f, 0f));
        return SavePrefab(root, "Items", "Item_SawitBunch");
    }

    public static GameObject SeedLv2Model()
    {
        GameObject root = Root("Item_SawitSeedLv2");
        Part(root.transform, "Seed", GrayboxMeshFactory.Blob(.15f, .24f), GrayboxPalette.Lit("SeedLv2", new Color(.48f, .12f, .14f), .6f), Vector3.zero);
        Part(root.transform, "Vein", GrayboxMeshFactory.Blob(.06f, .2f, 8, 4), GlowLv2, new Vector3(0f, .03f, -.11f));
        Part(root.transform, "Sprout", GrayboxMeshFactory.Cylinder(.02f, .14f, .01f, 8), SawitLeafLv2, new Vector3(0f, .22f, 0f));
        return SavePrefab(root, "Items", "Item_SawitSeedLv2");
    }

    // ---------- palms ----------

    // Day 1's palm the next morning: the evening's size, with fruit bunches under the fronds
    public static GameObject RipePalmLv1() => Palm("Palm_SawitLv1_Ripe", .2f, .12f, 1.15f, new Vector3(.2f, .045f, 1.15f), 11, SawitLeaf, GrayboxPalette.Accent, true);

    // Sawit Lv.2 is the Blender "gloamplum" plant, as Lv.1 is the duskbell (same scales as Plant_DuskbellLv1_*): the sprout,
    // then one evening later the harvest-ready plant with its fruit hidden, and on Day 3 the same plant with its fruit
    public static GameObject SaplingLv2() => BlenderPlant("Sapling_GloamplumLv2", "plant_2_gloamplum_sprout", 2.2f, true);
    public static GameObject GrownPalmLv2() => BlenderPlant("Plant_GloamplumLv2_Grown", "plant_2_gloamplum_harvest_ready", 1.4f, false);
    public static GameObject RipePalmLv2() => BlenderPlant("Plant_GloamplumLv2_Ripe", "plant_2_gloamplum_harvest_ready", 1.4f, true);

    // the model under a "Model" child (the root stays unscaled for the plot), fruit meshes off until ripe
    private static GameObject BlenderPlant(string name, string model, float scale, bool hasFruit)
    {
        GameObject root = Root(name);
        GameObject glb = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Ilkham/models/plants/{model}.glb");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(glb, root.transform);
        instance.name = "Model";
        instance.transform.localScale = Vector3.one * scale;
        foreach (Transform part in instance.transform)
            if (part.name.Contains("_Fruit_")) part.gameObject.SetActive(hasFruit);
        return SavePrefab(root, "Items", name);
    }

    private static GameObject Palm(string name, float trunkBottom, float trunkTop, float trunkHeight, Vector3 frondSize, int frondCount, Material leaf, Material glow, bool hasFruit)
    {
        GameObject root = Root(name);
        Part(root.transform, "Trunk", GrayboxMeshFactory.Taper(trunkBottom, trunkTop, trunkHeight, .05f, 10), GrayboxPalette.Trunk, Vector3.zero);
        Mesh frond = GrayboxMeshFactory.RoundedBox(frondSize, .02f);
        float crown = trunkHeight * .96f;
        for (int i = 0; i < frondCount; i++)
        {
            Transform pivot = Root("Frond", root.transform).transform;
            pivot.localPosition = new Vector3(0f, crown, 0f);
            pivot.localEulerAngles = new Vector3(i % 2 == 0 ? 32f : -8f, i * (360f / frondCount), 0f);
            PartCentered(pivot, "Leaf", frond, leaf, new Vector3(0f, 0f, frondSize.z * .48f));
        }
        Part(root.transform, "Glow", GrayboxMeshFactory.Blob(trunkTop * 1.2f, trunkTop * 1.5f), glow, new Vector3(0f, crown - .04f, 0f));
        if (hasFruit)
        {
            Mesh bunch = GrayboxMeshFactory.Blob(.17f, .24f, 10, 6);
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + 30f) * Mathf.Deg2Rad;
                Part(root.transform, "Bunch", bunch, PalmFruit, new Vector3(Mathf.Cos(a) * .2f, crown - .35f, Mathf.Sin(a) * .2f));
            }
        }
        return SavePrefab(root, "Items", name);
    }

    // ---------- interactables ----------

    // ICAN's collection crate: a slatted crate with a blue placard
    public static GameObject SellCrate(ItemDataSO sellableItemDataSO)
    {
        GameObject root = Root("IcanCrate");
        SellCrate crate = root.AddComponent<SellCrate>();
        Box(root, new Vector3(0f, .45f, 0f), new Vector3(1.3f, .9f, 1f));
        Transform visual = Root("Visual", root.transform).transform;
        Part(visual, "Crate", GrayboxMeshFactory.RoundedBox(new Vector3(1.2f, .7f, .9f), .06f), GrayboxPalette.Wood, Vector3.zero);
        Mesh slat = GrayboxMeshFactory.RoundedBox(new Vector3(1.26f, .1f, .96f), .03f);
        foreach (float y in new[] { .12f, .35f, .58f }) Part(visual, "Slat", slat, GrayboxPalette.WoodDark, new Vector3(0f, y, 0f));
        Part(visual, "Post", GrayboxMeshFactory.RoundedBox(new Vector3(.1f, 1.4f, .1f), .03f), GrayboxPalette.WoodDark, new Vector3(-.45f, 0f, .3f));
        GameObject placard = Part(visual, "Placard", GrayboxMeshFactory.RoundedBox(new Vector3(.9f, .42f, .08f), .04f), IcanBlue, new Vector3(-.45f, 1.15f, .25f));
        TextMeshPro label = Root("Label", placard.transform).AddComponent<TextMeshPro>();
        label.text = "ICAN";
        label.fontSize = 3.2f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.rectTransform.sizeDelta = new Vector2(.9f, .4f);
        // the placard's front faces -Z (the camera side)
        label.rectTransform.localPosition = new Vector3(0f, .21f, -.05f);
        InteractablePrefabBuilder.AddOutline(root, visual, GrayboxPalette.Outline, .03f);
        SetArray(crate, "sellableItemDataSOArray", new Object[] { sellableItemDataSO });
        SetField(crate, "promptAnchor", InteractablePrefabBuilder.Anchor(root, "PromptAnchor", new Vector3(0f, 1.9f, 0f)));
        SetLayerRecursively(root, InteractableLayer);
        return SavePrefab(root, "Interactables", "IcanCrate");
    }

    // an old stump at the forest edge with amber resin oozing down its side; the stump stays, the resin regrows
    public static GameObject ResinNode(ItemDataSO resinItemDataSO, Transform burstPrefab)
    {
        GameObject root = Root("ResinNode");
        ResourceNode node = root.AddComponent<ResourceNode>();
        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = .8f;
        trigger.center = new Vector3(0f, .4f, 0f);
        Transform visual = Root("Visual", root.transform).transform;
        Part(visual, "Stump", GrayboxMeshFactory.Taper(.42f, .34f, .7f, .05f, 12), GrayboxPalette.Trunk, Vector3.zero);
        Part(visual, "Rings", GrayboxMeshFactory.Cylinder(.33f, .04f, .01f, 12), GrayboxPalette.Wood, new Vector3(0f, .69f, 0f));
        GameObject model = Root("Model", visual);
        Part(model.transform, "Ooze", GrayboxMeshFactory.Blob(.12f, .3f), Amber, new Vector3(0f, .3f, -.36f));
        Part(model.transform, "Drop", GrayboxMeshFactory.Blob(.08f, .12f), Amber, new Vector3(.1f, .02f, -.42f));
        ResourceNodeVisual nodeVisual = visual.gameObject.AddComponent<ResourceNodeVisual>();
        SetField(nodeVisual, "resourceNode", node);
        SetField(nodeVisual, "modelGameObject", model);
        SetField(nodeVisual, "burstPrefab", burstPrefab);
        InteractablePrefabBuilder.AddOutline(root, visual, GrayboxPalette.Outline, .035f);
        SetField(node, "itemDataSO", resinItemDataSO);
        SetValue(node, "respawnTime", 45f);
        InteractablePrefabBuilder.SetString(node, "promptVerb", "scrape resin");
        SetField(node, "promptAnchor", InteractablePrefabBuilder.Anchor(root, "PromptAnchor", new Vector3(0f, 1.4f, 0f)));
        SetLayerRecursively(root, InteractableLayer);
        return SavePrefab(root, "Interactables", "ResinNode");
    }

    // the bankbook on the bedside table (pivot at its underside); the BedsideLetter component opens the savings page
    public static GameObject SavingsBook()
    {
        GameObject root = Root("SavingsBook");
        root.layer = InteractableLayer;
        BedsideLetter book = root.AddComponent<BedsideLetter>();
        Box(root, new Vector3(0f, .3f, 0f), new Vector3(1f, 1.2f, 1.1f), true);
        Transform visual = Root("Visual", root.transform).transform;
        Part(visual, "Cover", GrayboxMeshFactory.RoundedBox(new Vector3(.24f, .05f, .32f), .012f), GrayboxPalette.Lit("BookCover", new Color(.30f, .55f, .40f)), Vector3.zero, new Vector3(0f, -14f, 0f));
        Part(visual, "Pages", GrayboxMeshFactory.RoundedBox(new Vector3(.22f, .03f, .3f), .008f), GrayboxPalette.Lit("LetterPaper", new Color(.98f, .95f, .86f)), new Vector3(.01f, .012f, 0f), new Vector3(0f, -14f, 0f));
        InteractablePrefabBuilder.AddOutline(root, visual, GrayboxPalette.Outline, .02f);
        InteractablePrefabBuilder.SetString(book, "promptVerb", "check the savings book");
        SetField(book, "promptAnchor", InteractablePrefabBuilder.Anchor(root, "PromptAnchor", new Vector3(0f, 1.1f, 0f)));
        return SavePrefab(root, "Interactables", "SavingsBook");
    }

    // the neighbour: a plain villager who stays by his fence
    public static GameObject Rigel(NpcSO npcSO) => InteractablePrefabBuilder.Villager("Npc_Rigel", CharacterBuilder.RigelLook, npcSO);
}
