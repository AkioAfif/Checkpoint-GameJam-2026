using UnityEditor;
using UnityEngine;
using static GrayboxBuild;

// The yard day's ScriptableObjects (Day 3 on the CozyFarm map): the borrowed tools and Ratna's compost (with small graybox
// models and baked icons), every dialogue, and the per-state checklists, in ScriptableObjects/Day3/Day3_Yard.asset.
// The text only seeds the assets; designers edit the .asset files afterwards (a rebuild re-seeds them).
// Bakes icons in a temporary scene, so run it before opening the scene being built. Needs Day 1's and Day 2's data to exist.
public static class YardDayDataBuilder
{
    public static YardDaySO Yard;
    public static ItemDataSO Sickle;
    public static ItemDataSO Hoe;
    public static ItemDataSO Rope;
    public static ItemDataSO Compost;

    public static void Build()
    {
        DialogueSO.DialogueLine N(string text, Emote npcEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Npc, text = text, npcEmote = npcEmote };
        DialogueSO.DialogueLine P(string text, Emote playerEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Player, text = text, playerEmote = playerEmote };
        ItemAmount Give(ItemDataSO item) => new ItemAmount { itemDataSO = item, amount = 1 };

        Sickle = GameDataBuilder.Item("Sickle", "Sickle", "Nek Ijah's late husband's arit. Old, but it still bites through weeds.", SickleModel(), new Color(.7f, .72f, .75f));
        Hoe = GameDataBuilder.Item("Hoe", "Hoe", "Pak Darto's cangkul, a little muddy from Dimas's treasure hunt.", HoeModel(), new Color(.6f, .5f, .4f));
        Rope = GameDataBuilder.Item("Rope", "Rope", "The rope Pak Darto built the bridge with. Thick enough for a stubborn stump.", RopeModel(), new Color(.75f, .62f, .42f));
        Compost = GameDataBuilder.Item("Compost", "Compost", "From Bu Ratna's garden. New soil is hungry soil.", CompostModel(), new Color(.35f, .26f, .18f));
        ItemDataSO lv1Bunch = GameDataBuilder.Load<ItemDataSO>("Items", "SawitBunch");

        Yard = GameDataBuilder.Asset<YardDaySO>("Day3", "Day3_Yard");
        Yard.afternoonCaption = "That afternoon...";
        Yard.nightCaption = "That evening...";
        Yard.ijahFlowerCount = 3;

        Yard.morningDialogueSO = Dialogue("Yard_Morning",
            P("It helped... but forty-six million doesn't care about helped.", Emote.HeavySigh),
            P("Twenty-nine days."));
        Yard.rigelMorningDialogueSO = Dialogue("Yard_RigelMorning",
            N("Those things grow overnight.", Emote.TiltHead),
            N("Still don't like it."));
        Yard.mathDialogueSO = Dialogue("Yard_Math",
            P("Nine plots. Even if every one pays every day... I'm short.", Emote.HeavySigh),
            P("And ICAN won't take the dark ones yet. I'll keep those two."),
            P("I need more land. And I've got a whole strip of it going to waste, right next to the garden."));
        Yard.wishDialogueSO = Dialogue("Yard_Wish",
            P("Dad always said that strip was too stubborn to farm."),
            P("Let's see about that.", Emote.Nod));
        Yard.bareHandsDialogueSO = Dialogue("Yard_BareHands",
            P("Ow! Okay...", Emote.Dodge),
            P("I need actual tools. Someone in the village must have some."));

        Yard.ijahAskDialogueSO = Dialogue("Yard_IjahAsk",
            N("A sickle? My husband's is still hanging by the door.", Emote.Nod),
            N("He cleared a strip like that once. Slowly, every evening, for a whole season."),
            N("Bring me three flowers for my jamu, and his sickle is yours."));
        Yard.ijahGiveDialogueSO = GameDataBuilder.Dialogue("Yard_IjahGive", Give(Sickle),
            N("Lovely. These will make a good jamu.", Emote.WideSmile),
            N("Here. Mind the edge."),
            N("Good things grow slowly, dear. Remember that.", Emote.Nod));
        Yard.ijahAfterDialogueSO = Dialogue("Yard_IjahAfter",
            N("Good things grow slowly, dear.", Emote.Nod));
        Yard.dartoHoeMissingDialogueSO = Dialogue("Yard_DartoHoeMissing",
            N("My hoe? Of course, take— ...where is it?", Emote.Surprised),
            N("That boy. He took it again. Dimas! DIMAS!", Emote.Shake),
            N("...He's off digging by the river. Probably."));
        Yard.dartoWhereDialogueSO = Dialogue("Yard_DartoWhere",
            N("Dimas has it. Up the river bank, digging for 'treasure'.", Emote.HeavySigh));
        Yard.dartoRopeDialogueSO = GameDataBuilder.Dialogue("Yard_DartoRope", Give(Rope),
            N("You got it back! Good lad."),
            N("Clearing a stump too? Then take this rope. I built that bridge with it.", Emote.Hop),
            N("A stump's like a stubborn goat. You don't lift it, you drag it."));
        Yard.dimasAskDialogueSO = Dialogue("Yard_DimasAsk",
            N("There's TREASURE under here, I just know it!", Emote.Hop),
            P("Dimas, that's Pak Darto's hoe. I need it."),
            N("Hmm... Give me one of your sawit seeds and it's yours!", Emote.WideSmile),
            N("The ones from your big machine!"));
        Yard.dimasTradeDialogueSO = GameDataBuilder.Dialogue("Yard_DimasTrade", Give(Hoe),
            N("A real sawit seed? Deal!", Emote.Cheer),
            N("I'm gonna grow the biggest one in the village!"));
        Yard.dimasAfterDialogueSO = Dialogue("Yard_DimasAfter",
            N("When my sawit grows, I'll be rich like you!", Emote.Hop));
        Yard.ratnaLunchDialogueSO = GameDataBuilder.Dialogue("Yard_RatnaLunch", Give(Compost),
            N("There you are! You've been running round the village all morning. Sit. Eat.", Emote.Wave),
            N("Since my husband passed, it's just me and Dimas. We get by."),
            N("ICAN pays well, doesn't it? Almost too well...", Emote.TiltHead),
            N("Here, compost from my garden. New soil is hungry soil.", Emote.Nod),
            P("Thank you, Bu Ratna. Really.", Emote.Nod));

        Yard.stumpAloneDialogueSO = Dialogue("Yard_StumpAlone",
            P("Come... ON!", Emote.Dodge));
        Yard.rigelHelpDialogueSO = Dialogue("Yard_RigelHelp",
            N("Move over."),
            N("On three. And stop pulling like a city boy.", Emote.Nod));
        Yard.rigelTalkDialogueSO = Dialogue("Yard_RigelTalk",
            N("Ha... ha...", Emote.HeavySigh),
            N("Ten years I've farmed next to you. First time I've seen you sweat.", Emote.WideSmile),
            N("That's how land is supposed to be made. With your back, not a machine."),
            P("...", Emote.HeavySigh));
        Yard.eveningDialogueSO = Dialogue("Yard_Evening",
            P("Three plots. One whole day, half the village... and Rigel's back.", Emote.HeavySigh),
            P("Twenty-nine days. I can't do this every day."));

        TaskObjectiveSO harvest = GameDataBuilder.Task("D3Y_Harvest", TaskObjectiveSO.Type.CollectItem, "Harvest the ripe sawit", lv1Bunch, 4);
        TaskObjectiveSO sell = GameDataBuilder.Task("D3Y_Sell", TaskObjectiveSO.Type.Story, "Sell the sawit at the ICAN crate", null, 1);
        TaskObjectiveSO clear = GameDataBuilder.Task("D3Y_Clear", TaskObjectiveSO.Type.Story, "Clear the strip by the garden", null, 1);
        TaskObjectiveSO sickle = GameDataBuilder.Task("D3Y_Sickle", TaskObjectiveSO.Type.CollectItem, "Borrow a sickle (Nek Ijah)", Sickle, 1);
        TaskObjectiveSO hoe = GameDataBuilder.Task("D3Y_Hoe", TaskObjectiveSO.Type.CollectItem, "Borrow a hoe (Pak Darto)", Hoe, 1);
        TaskObjectiveSO rope = GameDataBuilder.Task("D3Y_Rope", TaskObjectiveSO.Type.CollectItem, "Get a rope", Rope, 1);
        TaskObjectiveSO lunch = GameDataBuilder.Task("D3Y_Lunch", TaskObjectiveSO.Type.Story, "Bu Ratna is calling you over", null, 1);
        TaskObjectiveSO work = GameDataBuilder.Task("D3Y_Work", TaskObjectiveSO.Type.Story, "Cut the weeds and dig out the rocks", null, 7);
        TaskObjectiveSO stump = GameDataBuilder.Task("D3Y_Stump", TaskObjectiveSO.Type.Story, "Pull out the old stump", null, 1);
        TaskObjectiveSO till = GameDataBuilder.Task("D3Y_Till", TaskObjectiveSO.Type.Story, "Till 3 new plots", null, 3);
        TaskObjectiveSO plant = GameDataBuilder.Task("D3Y_Plant", TaskObjectiveSO.Type.PlantCrop, "Plant the new plots", null, 3);
        TaskObjectiveSO home = GameDataBuilder.Task("D3Y_GoHome", TaskObjectiveSO.Type.EnterHouse, "Go home", null, 1);
        Yard.harvestObjectiveArray = new[] { harvest, sell };
        Yard.clearObjectiveArray = new[] { clear };
        Yard.errandObjectiveArray = new[] { sickle, hoe, rope };
        Yard.lunchObjectiveArray = new[] { lunch };
        Yard.workObjectiveArray = new[] { work };
        Yard.stumpObjectiveArray = new[] { stump };
        Yard.plantObjectiveArray = new[] { till, plant };
        Yard.nightObjectiveArray = new[] { home };
        EditorUtility.SetDirty(Yard);
        AssetDatabase.SaveAssets();
        GrayboxIconBaker.BakeIcons(new[] { Sickle, Hoe, Rope, Compost });
    }

    // the assets as they are, for the scene builders (after a scene is opened)
    public static void LoadAll()
    {
        Yard = GameDataBuilder.Load<YardDaySO>("Day3", "Day3_Yard");
        Sickle = GameDataBuilder.Load<ItemDataSO>("Items", "Sickle");
        Hoe = GameDataBuilder.Load<ItemDataSO>("Items", "Hoe");
        Rope = GameDataBuilder.Load<ItemDataSO>("Items", "Rope");
        Compost = GameDataBuilder.Load<ItemDataSO>("Items", "Compost");
    }

    // ---------- item models (models only: they float up and get baked into icons) ----------

    private static GameObject SickleModel()
    {
        GameObject root = Root("Item_Sickle");
        Part(root.transform, "Handle", GrayboxMeshFactory.Cylinder(.035f, .3f, .01f, 10), GrayboxPalette.WoodDark, Vector3.zero);
        // a curved blade, out of three angled pieces
        Material steel = GrayboxPalette.Lit("ToolSteel", new Color(.72f, .74f, .78f), .6f, .5f);
        for (int i = 0; i < 3; i++)
        {
            float angle = 20f + i * 38f;
            Vector3 position = new Vector3(0f, .3f, 0f) + Quaternion.Euler(0f, 0f, -angle) * new Vector3(0f, .1f, 0f) + Vector3.right * i * .06f;
            Part(root.transform, "Blade", GrayboxMeshFactory.RoundedBox(new Vector3(.03f, .14f, .012f), .005f), steel, position, new Vector3(0f, 0f, -angle));
        }
        return SavePrefab(root, "Items", "Item_Sickle");
    }

    private static GameObject HoeModel()
    {
        GameObject root = Root("Item_Hoe");
        Part(root.transform, "Handle", GrayboxMeshFactory.Cylinder(.03f, .9f, .01f, 10), GrayboxPalette.Wood, Vector3.zero);
        Part(root.transform, "Blade", GrayboxMeshFactory.RoundedBox(new Vector3(.24f, .16f, .03f), .01f),
            GrayboxPalette.Lit("ToolSteel", new Color(.72f, .74f, .78f), .6f, .5f), new Vector3(0f, .86f, .1f), new Vector3(70f, 0f, 0f));
        return SavePrefab(root, "Items", "Item_Hoe");
    }

    private static GameObject RopeModel()
    {
        GameObject root = Root("Item_Rope");
        Material fibre = GrayboxPalette.Lit("RopeFibre", new Color(.78f, .64f, .44f));
        for (int i = 0; i < 3; i++) Part(root.transform, "Coil", GrayboxMeshFactory.Torus(.16f - i * .015f, .035f, 20, 8), fibre, new Vector3(0f, .04f + i * .06f, 0f));
        return SavePrefab(root, "Items", "Item_Rope");
    }

    private static GameObject CompostModel()
    {
        GameObject root = Root("Item_Compost");
        Part(root.transform, "Sack", GrayboxMeshFactory.Blob(.2f, .34f), GrayboxPalette.Lit("Burlap", new Color(.66f, .55f, .38f)), Vector3.zero);
        Part(root.transform, "Soil", GrayboxMeshFactory.Blob(.12f, .08f), GrayboxPalette.Lit("CompostSoil", new Color(.3f, .22f, .15f)), new Vector3(0f, .3f, 0f));
        return SavePrefab(root, "Items", "Item_Compost");
    }

    private static DialogueSO Dialogue(string file, params DialogueSO.DialogueLine[] lines) => GameDataBuilder.Dialogue(file, default, lines);
}
