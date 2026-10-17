using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static GrayboxBuild;

// Creates (or refreshes) every ScriptableObject + item model the level needs. Content text lives here only to seed the assets;
// designers edit the .asset files afterwards.
public static class GameDataBuilder
{
    private const string SO = "Assets/_Project/ScriptableObjects";

    public static ItemDataSO EmptyBottle, Water, Flower, Crop, SawitSeed;
    public static CropDataSO SawitLv1;
    public static FabricatorPuzzleSO PuzzleTutorial, PuzzleNormal;
    public static TaskObjectiveSO[] Tasks;
    public static NpcSO Ratna, Darto, Dimas, Ijah;
    public static AudioClipRefsSO AudioRefs;
    public static TileBase DirtTile, RockTile;
    public static PhysicsMaterial2D LiquidPhysics;

    public static readonly Color WaterColor = new Color(.35f, .65f, .95f);
    public static readonly Color NectarColor = new Color(.98f, .52f, .72f);
    public static readonly Color JuiceColor = new Color(.55f, .85f, .35f);

    public static void BuildAll()
    {
        EmptyBottle = Item("EmptyBottle", "Empty Bottle", "A sturdy glass potion bottle. Fill it at the river.", BottleModel("Item_EmptyBottle", null), Color.white);
        Water = Item("Water", "River Water", "Cool, clear water scooped from the village river.", BottleModel("Item_Water", GrayboxPalette.Lit("LiquidWater", WaterColor, .9f)), WaterColor);
        Flower = Item("Flower", "Wildflower", "A soft pink bloom from a villager's garden. Smells a little like honey.", FlowerModel("Item_Flower", 1), NectarColor);
        Crop = Item("Crop", "Leafy Greens", "A plump, leafy vegetable from your own field.", CropModel("Item_Crop", .7f), JuiceColor);
        SawitSeed = Item("SawitSeed", "Sawit Seed Lv.1", "A warm, glowing seed pressed by the Fabricator. Plant it in an empty plot.", SeedModel(), new Color(1f, .8f, .35f));

        SawitLv1 = Asset<CropDataSO>("Crops", "SawitLv1");
        SawitLv1.cropName = "Sawit Lv.1";
        SawitLv1.seedItemDataSO = SawitSeed;
        SawitLv1.saplingPrefab = SaplingModel().transform;
        SawitLv1.qteCheckCount = 2;
        EditorUtility.SetDirty(SawitLv1);

        PuzzleTutorial = Puzzle("Puzzle_01_Tutorial", "First Press",
            "RRRRRRRRRRRRRR\nRWWWRFFFFRCCCR\nRWWWRFFFFRCCCR\nR###R####R###R\nR############R\nR############R\nR############R\nR############R\nR############R\nR############R\nR############R\nR############R\nR############R\nR############R\nR############R\nR##RMMMMMMR##R\nR##RMMMMMMR##R\nR##RMMMMMMR##R\nR##RRRRRRRR##R\nRRRRRRRRRRRRRR");
        PuzzleNormal = Puzzle("Puzzle_02_Rocks", "Rocky Road",
            "RRRRRRRRRRRRRR\nRWWWRFFFFRCCCR\nRWWWRFFFFRCCCR\nR###R####R###R\nR############R\nR###RRRRRR###R\nR############R\nR############R\nRRRR######RRRR\nR############R\nR##RR####RR##R\nR############R\nR#####RR#####R\nR############R\nR############R\nR##RMMMMMMR##R\nR##RMMMMMMR##R\nR##RMMMMMMR##R\nR##RRRRRRRR##R\nRRRRRRRRRRRRRR");

        Tasks = new[]
        {
            Task("01_Water", TaskObjectiveSO.Type.CollectItem, "Scoop river water", Water, 3),
            Task("02_Flower", TaskObjectiveSO.Type.CollectItem, "Pick flowers near the houses", Flower, 3),
            Task("03_Crop", TaskObjectiveSO.Type.CollectItem, "Harvest crops from your farm", Crop, 3),
            Task("04_Fabricate", TaskObjectiveSO.Type.FabricateSeed, "Press Sawit seeds in the Fabricator", null, 3),
            Task("05_Plant", TaskObjectiveSO.Type.PlantCrop, "Plant Sawit seeds in your farm", null, 3),
            Task("06_GoHome", TaskObjectiveSO.Type.EnterHouse, "Go home", null, 1),
        };

        BuildNpcs();

        AudioRefs = Asset<AudioClipRefsSO>("", "AudioClipRefs");

        DirtTile = PuzzleTile("PuzzleDirtTile");
        RockTile = PuzzleTile("PuzzleRockTile");
        LiquidPhysics = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>($"{SO}/Puzzles/LiquidPhysics.asset");
        if (LiquidPhysics == null)
        {
            LiquidPhysics = new PhysicsMaterial2D("LiquidPhysics") { friction = .02f, bounciness = 0f };
            AssetDatabase.CreateAsset(LiquidPhysics, $"{SO}/Puzzles/LiquidPhysics.asset");
        }

        AssetDatabase.SaveAssets();
    }

    public static void BakeIcons() => GrayboxIconBaker.BakeIcons(new[] { EmptyBottle, Water, Flower, Crop, SawitSeed });

    private static void BuildNpcs()
    {
        DialogueSO.DialogueLine N(string text, Emote npcEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Npc, text = text, npcEmote = npcEmote };
        DialogueSO.DialogueLine P(string text, Emote playerEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Player, text = text, playerEmote = playerEmote };

        DialogueSO ratnaFirst = Dialogue("Ratna_First", new ItemAmount { itemDataSO = EmptyBottle, amount = 2 },
            N("Oh! Good morning, neighbour! Out tending the farm again?", Emote.Wave),
            P("Morning, Bu Ratna! Just gathering a few things.", Emote.Nod),
            N("Then take these spare bottles. The river water is lovely and cool today.", Emote.Hop),
            P("Two bottles? Thank you so much!", Emote.Cheer),
            N("Don't mention it. Come by any time!", Emote.Nod));
        DialogueSO ratnaRepeat = Dialogue("Ratna_Repeat", default,
            N("The flowers by the houses grow back after a little while. Help yourself!", Emote.Wave));
        DialogueSO dartoFirst = Dialogue("Darto_First", default,
            N("Hm? Ah, it's you.", Emote.Surprised),
            P("Hello, Pak Darto! Nice weather today.", Emote.Wave),
            N("Nice weather for a nap, yes.", Emote.Nod),
            N("That bridge creaks a little, but it's sturdy. Built it myself!", Emote.Hop));
        DialogueSO dartoRepeat = Dialogue("Darto_Repeat", default,
            N("Mind the river edge. It's deeper than it looks.", Emote.Shake));
        DialogueSO dimasFirst = Dialogue("Dimas_First", default,
            N("Hey hey! Wanna see how fast I can run?", Emote.Hop),
            P("Maybe later, Dimas!", Emote.Shake),
            N("Aww... okay! I'll practise more!", Emote.Sad));
        DialogueSO dimasRepeat = Dialogue("Dimas_Repeat", default,
            N("I counted all the flowers in the village. There are a LOT.", Emote.Surprised));
        DialogueSO ijahFirst = Dialogue("Ijah_First", default,
            N("Come here, dear. Let me look at you.", Emote.Wave),
            P("Hello, Nek Ijah!", Emote.Nod),
            N("Your farm looks healthy. Plants grow best when they're tended with patience.", Emote.Nod),
            P("I'll remember that!", Emote.Cheer));
        DialogueSO ijahRepeat = Dialogue("Ijah_Repeat", default,
            N("Patience, dear. Good things grow slowly.", Emote.Nod));

        Ratna = Npc("Ratna", "Bu Ratna", CharacterBuilder.RatnaLook, ratnaFirst, ratnaRepeat, new Color(.99f, .87f, .88f));
        Darto = Npc("Darto", "Pak Darto", CharacterBuilder.DartoLook, dartoFirst, dartoRepeat, new Color(.86f, .94f, .86f));
        Dimas = Npc("Dimas", "Dimas", CharacterBuilder.DimasLook, dimasFirst, dimasRepeat, new Color(1f, .91f, .80f));
        Ijah = Npc("Ijah", "Nek Ijah", CharacterBuilder.IjahLook, ijahFirst, ijahRepeat, new Color(.90f, .86f, .97f));
    }

    // ---------- item / prop models (visual only, no colliders) ----------

    private static GameObject BottleModel(string name, Material liquid)
    {
        GameObject root = Root(name);
        if (liquid != null) Part(root.transform, "Liquid", GrayboxMeshFactory.Blob(.13f, .22f), liquid, new Vector3(0f, .03f, 0f));
        Part(root.transform, "Body", GrayboxMeshFactory.Blob(.16f, .3f), GrayboxPalette.Glass, Vector3.zero);
        Part(root.transform, "Neck", GrayboxMeshFactory.Cylinder(.065f, .14f, .02f, 12), GrayboxPalette.Glass, new Vector3(0f, .26f, 0f));
        Part(root.transform, "Cork", GrayboxMeshFactory.Cylinder(.07f, .08f, .02f, 12), GrayboxPalette.Wood, new Vector3(0f, .38f, 0f));
        return SavePrefab(root, "Items", name);
    }

    // a small clump of `count` flowers
    public static GameObject FlowerModel(string name, int count)
    {
        GameObject root = Root(name);
        Vector3[] offsets = { Vector3.zero, new Vector3(.22f, 0f, .12f), new Vector3(-.18f, 0f, .16f) };
        float[] heights = { .48f, .38f, .32f };
        Mesh stem = GrayboxMeshFactory.Cylinder(.025f, 1f, .01f, 8);
        Mesh petal = GrayboxMeshFactory.Blob(.085f, .07f, 10, 4);
        Mesh centre = GrayboxMeshFactory.Blob(.07f, .09f, 10, 5);
        for (int f = 0; f < count; f++)
        {
            Transform flower = Root("Flower", root.transform).transform;
            flower.localPosition = offsets[f];
            Material petalMaterial = f == 1 ? GrayboxPalette.PetalYellow : GrayboxPalette.PetalPink;
            GameObject stemGo = Part(flower, "Stem", stem, GrayboxPalette.CropLeaf, Vector3.zero);
            stemGo.transform.localScale = new Vector3(1f, heights[f], 1f);
            for (int p = 0; p < 5; p++)
            {
                float a = p * 72f * Mathf.Deg2Rad;
                Part(flower, "Petal", petal, petalMaterial, new Vector3(Mathf.Cos(a) * .09f, heights[f] - .03f, Mathf.Sin(a) * .09f));
            }
            Part(flower, "Centre", centre, GrayboxPalette.Accent, new Vector3(0f, heights[f] - .03f, 0f));
        }
        if (count > 1) Part(root.transform, "Leaves", GrayboxMeshFactory.Blob(.32f, .22f), GrayboxPalette.Foliage, new Vector3(0f, 0f, .05f));
        return SavePrefab(root, "Items", name);
    }

    public static GameObject CropModel(string name, float scale)
    {
        GameObject root = Root(name);
        Transform model = Root("Model", root.transform).transform;
        model.localScale = Vector3.one * scale;
        Part(model, "Head", GrayboxMeshFactory.Blob(.3f, .42f), GrayboxPalette.Lit("CropHead", new Color(.76f, .88f, .55f)), Vector3.zero);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f;
            PartCentered(model, "Leaf", GrayboxMeshFactory.Blob(.22f, .1f, 10, 4), GrayboxPalette.CropLeaf,
                Quaternion.Euler(0f, a, 0f) * new Vector3(0f, .14f, .26f), new Vector3(-35f, a, 0f));
        }
        return SavePrefab(root, "Items", name);
    }

    private static GameObject SeedModel()
    {
        GameObject root = Root("Item_SawitSeed");
        Part(root.transform, "Seed", GrayboxMeshFactory.Blob(.15f, .24f), GrayboxPalette.Accent, Vector3.zero);
        Part(root.transform, "Sprout", GrayboxMeshFactory.Cylinder(.02f, .14f, .01f, 8), GrayboxPalette.CropLeaf, new Vector3(0f, .22f, 0f));
        PartCentered(root.transform, "LeafL", GrayboxMeshFactory.Blob(.06f, .03f, 8, 4), GrayboxPalette.CropLeaf, new Vector3(-.05f, .36f, 0f), new Vector3(0f, 0f, 25f));
        PartCentered(root.transform, "LeafR", GrayboxMeshFactory.Blob(.06f, .03f, 8, 4), GrayboxPalette.CropLeaf, new Vector3(.05f, .37f, 0f), new Vector3(0f, 0f, -25f));
        return SavePrefab(root, "Items", "Item_SawitSeed");
    }

    // young oil palm: short trunk + drooping fronds
    private static GameObject SaplingModel()
    {
        GameObject root = Root("Sapling_SawitLv1");
        Material leaf = GrayboxPalette.Lit("SawitLeaf", new Color(.56f, .74f, .36f));
        Part(root.transform, "Trunk", GrayboxMeshFactory.Taper(.11f, .07f, .55f, .03f, 10), GrayboxPalette.Trunk, Vector3.zero);
        Mesh frond = GrayboxMeshFactory.RoundedBox(new Vector3(.12f, .035f, .62f), .015f);
        for (int i = 0; i < 7; i++)
        {
            Transform pivot = Root("Frond", root.transform).transform;
            pivot.localPosition = new Vector3(0f, .52f, 0f);
            pivot.localEulerAngles = new Vector3(i % 2 == 0 ? 28f : -12f, i * (360f / 7f), 0f);
            PartCentered(pivot, "Leaf", frond, leaf, new Vector3(0f, 0f, .3f));
        }
        Part(root.transform, "Glow", GrayboxMeshFactory.Blob(.09f, .12f), GrayboxPalette.Accent, new Vector3(0f, .5f, 0f));
        return SavePrefab(root, "Items", "Sapling_SawitLv1");
    }

    // ---------- asset helpers ----------

    private static ItemDataSO Item(string file, string itemName, string description, GameObject prefab, Color liquidColor)
    {
        ItemDataSO item = Asset<ItemDataSO>("Items", file);
        item.itemName = itemName;
        item.description = description;
        item.prefab = prefab.transform;
        item.liquidColor = liquidColor;
        EditorUtility.SetDirty(item);
        return item;
    }

    private static FabricatorPuzzleSO Puzzle(string file, string puzzleName, string layout)
    {
        FabricatorPuzzleSO puzzle = Asset<FabricatorPuzzleSO>("Puzzles", file);
        puzzle.puzzleName = puzzleName;
        puzzle.layout = layout;
        puzzle.particlesPerLiquid = 24; // 6-cell pockets x 4 blobs per cell
        puzzle.requiredFraction = .5f;
        EditorUtility.SetDirty(puzzle);
        return puzzle;
    }

    private static TaskObjectiveSO Task(string file, TaskObjectiveSO.Type type, string description, ItemDataSO item, int count)
    {
        TaskObjectiveSO task = Asset<TaskObjectiveSO>("Tasks", file);
        task.type = type;
        task.description = description;
        task.itemDataSO = item;
        task.requiredCount = count;
        EditorUtility.SetDirty(task);
        return task;
    }

    private static DialogueSO Dialogue(string file, ItemAmount reward, params DialogueSO.DialogueLine[] lines)
    {
        DialogueSO dialogue = Asset<DialogueSO>("Dialogues", file);
        dialogue.lineArray = lines;
        dialogue.reward = reward;
        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    private static NpcSO Npc(string file, string npcName, CharacterBuilder.Look look, DialogueSO first, DialogueSO repeat, Color background)
    {
        NpcSO npc = Asset<NpcSO>("Npcs", file);
        npc.npcName = npcName;
        npc.portraitPrefab = CharacterBuilder.RigPrefab(look).transform;
        npc.firstDialogueSO = first;
        npc.repeatDialogueSO = repeat;
        npc.portraitBackground = background;
        EditorUtility.SetDirty(npc);
        return npc;
    }

    private static TileBase PuzzleTile(string file)
    {
        Tile tile = Asset<Tile>("Puzzles", file);
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static T Asset<T>(string folder, string file) where T : ScriptableObject
    {
        string dir = folder.Length > 0 ? $"{SO}/{folder}" : SO;
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(SO, folder);
        string path = $"{dir}/{file}.asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
