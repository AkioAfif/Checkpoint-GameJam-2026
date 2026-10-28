using UnityEditor;
using UnityEngine;

// Day 2's ScriptableObjects: the new items and crop, Rigel, the dialogues, the savings book text and the per-state checklists.
// The text only seeds the assets; designers edit the .asset files afterwards (re-running refreshes the text, keeps the GUIDs).
// Needs GameDataBuilder.LoadAll() first.
public static class Day2DataBuilder
{
    public static Day2SO Day2;
    public static NpcSO Rigel;
    public static ItemDataSO Resin, SawitBunch, SawitSeedLv2;
    public static CropDataSO SawitLv2;

    public static void Build()
    {
        DialogueSO.DialogueLine N(string text, Emote npcEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Npc, text = text, npcEmote = npcEmote };
        DialogueSO.DialogueLine P(string text, Emote playerEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Player, text = text, playerEmote = playerEmote };

        Resin = GameDataBuilder.Item("Resin", "Forest Resin", "Sticky amber sap from the old stumps at the forest edge. It smells sharp, almost like medicine.", Day2PrefabBuilder.ResinModel(), new Color(.88f, .55f, .16f));
        SawitBunch = GameDataBuilder.Item("SawitBunch", "Sawit Bunch", "A heavy bunch of ripe oil palm fruit. ICAN buys every one.", Day2PrefabBuilder.BunchModel(), new Color(.82f, .28f, .14f));
        SawitBunch.sellPrice = 750000;
        EditorUtility.SetDirty(SawitBunch);
        SawitSeedLv2 = GameDataBuilder.Item("SawitSeedLv2", "Sawit Seed Lv.2", "A darker seed, pressed with forest resin. It feels warm, and a little too heavy.", Day2PrefabBuilder.SeedLv2Model(), new Color(.78f, .16f, .2f));

        // yesterday's palms bear fruit this morning
        CropDataSO lv1 = GameDataBuilder.SawitLv1;
        lv1.ripePrefab = Day2PrefabBuilder.RipePalmLv1().transform;
        lv1.harvestItemDataSO = SawitBunch;
        EditorUtility.SetDirty(lv1);

        // the ripe model and harvest come with Day 3 (Day3DataBuilder), when these palms first bear fruit
        SawitLv2 = GameDataBuilder.Asset<CropDataSO>("Crops", "SawitLv2");
        SawitLv2.cropName = "Sawit Lv.2";
        SawitLv2.seedItemDataSO = SawitSeedLv2;
        SawitLv2.saplingPrefab = Day2PrefabBuilder.SaplingLv2().transform;
        SawitLv2.grownPrefab = Day2PrefabBuilder.GrownPalmLv2().transform;
        SawitLv2.qteCheckCount = 3;
        SawitLv2.ringShrinkDuration = .95f;
        SawitLv2.hitWindow = .18f;
        SawitLv2.perfectWindow = .05f;
        EditorUtility.SetDirty(SawitLv2);

        DialogueSO rigelFirst = Dialogue("Rigel_First",
            N("Hey, neighbour! Is that sawit? It wasn't there two days ago.", Emote.Wave),
            P("It grows fast. Really fast."),
            N("Ten years I've farmed this soil. Nothing grows that fast.", Emote.TiltHead),
            N("Where'd you get the seeds?"),
            P("ICAN gave me a machine. The Fabricator."),
            N("A machine that makes seeds. Huh.", Emote.Shake),
            N("Tell you what. Let's see who brings in the better harvest this week.", Emote.WideSmile),
            N("Bet you can't grow anything better than that."),
            P("Watch me.", Emote.Cheer),
            P("Maybe something from the old forest will do the trick..."));
        DialogueSO rigelRepeat = Dialogue("Rigel_Repeat", N("My rice may be slow, but it's honest.", Emote.Nod));
        Rigel = GameDataBuilder.Npc("Rigel", "Rigel", CharacterBuilder.RigPrefab(CharacterBuilder.RigelLook), rigelFirst, rigelRepeat, new Color(.98f, .86f, .78f));

        Day2 = GameDataBuilder.Asset<Day2SO>("Day2", "Day2");
        Day2.startingSavings = 1000;
        Day2.startingDebt = 48500000;
        Day2.daysUntilDue = 30;
        Day2.bookTitle = "SAVINGS BOOK";
        Day2.bookMorningFormat = "Savings<pos=55%>{0} Koin\nDebt left<pos=55%>{1} Koin\nDue in<pos=55%>{2} days";
        Day2.bookEveningFormat = "Savings<pos=55%>{0} Koin\nEarned today<pos=55%><color=#3E8E5A>+{1} Koin</color>\nDebt left<pos=55%>{2} Koin\nDue in<pos=55%>{3} days";
        Day2.bookCloseHint = "Press [{0}] to close the book";
        Day2.nightCaption = "That evening...";

        Day2.morningDialogueSO = Dialogue("Day2_Morning",
            P("Forty-eight and a half million... and a thousand in the book.", Emote.HeavySigh),
            P("But the sawit grew overnight. Let's see what it's worth.", Emote.Nod));
        Day2.soldDialogueSO = Dialogue("Day2_Sold",
            P("That's... more than a whole season of rice.", Emote.Surprised),
            P("If I keep this up, I might actually make it.", Emote.Cheer));
        Day2.rigelSeesLv2DialogueSO = Dialogue("Rigel_SeesLv2",
            N("Those look different. What did you put in that thing?", Emote.TiltHead),
            P("Something from the forest. It's an experiment."),
            N("...Just don't poison the soil, alright?", Emote.WideSmile));
        Day2.eveningDialogueSO = Dialogue("Day2_EveningMonologue",
            P("Even better than yesterday."),
            P("Rigel's going to be sick when he sees this.", Emote.WideSmile));

        TaskObjectiveSO readBook = GameDataBuilder.Task("D2_ReadBook", TaskObjectiveSO.Type.Story, "Check your savings book", null, 1);
        TaskObjectiveSO harvest = GameDataBuilder.Task("D2_HarvestSawit", TaskObjectiveSO.Type.CollectItem, "Harvest 2 sawit bunches", SawitBunch, 2);
        TaskObjectiveSO sell = GameDataBuilder.Task("D2_SellSawit", TaskObjectiveSO.Type.Story, "Sell the sawit at the ICAN crate", null, 1);
        TaskObjectiveSO meetRigel = GameDataBuilder.Task("D2_MeetRigel", TaskObjectiveSO.Type.Story, "Talk to Rigel by his field", null, 1);
        TaskObjectiveSO gatherResin = GameDataBuilder.Task("D2_GatherResin", TaskObjectiveSO.Type.CollectItem, "Scrape 2 resin at the forest edge", Resin, 2);
        TaskObjectiveSO fabricate = GameDataBuilder.Task("D2_MakeLv2", TaskObjectiveSO.Type.FabricateSeed, "Press Sawit Lv.2 seeds in the Fabricator", SawitSeedLv2, 1);
        TaskObjectiveSO plant = GameDataBuilder.Task("D2_PlantLv2", TaskObjectiveSO.Type.PlantCrop, "Plant 2 Sawit Lv.2 seeds", SawitSeedLv2, 2);
        TaskObjectiveSO showRigel = GameDataBuilder.Task("D2_ShowRigel", TaskObjectiveSO.Type.Story, "Show Rigel your new sawit", null, 1);
        TaskObjectiveSO home = GameDataBuilder.Task("D2_GoHome", TaskObjectiveSO.Type.EnterHouse, "Go home", null, 1);
        Day2.morningObjectiveArray = new[] { readBook };
        Day2.harvestObjectiveArray = new[] { harvest, sell };
        Day2.rigelObjectiveArray = new[] { meetRigel };
        Day2.experimentObjectiveArray = new[] { gatherResin, fabricate, plant };
        Day2.showRigelObjectiveArray = new[] { showRigel };
        Day2.nightObjectiveArray = new[] { home };
        EditorUtility.SetDirty(Day2);
        AssetDatabase.SaveAssets();
    }

    // Day 2's assets as they are, for later days' builders (Build would re-save their prefabs and reset the scene instances)
    public static void LoadAll()
    {
        Day2 = GameDataBuilder.Load<Day2SO>("Day2", "Day2");
        Rigel = GameDataBuilder.Load<NpcSO>("Npcs", "Rigel");
        Resin = GameDataBuilder.Load<ItemDataSO>("Items", "Resin");
        SawitBunch = GameDataBuilder.Load<ItemDataSO>("Items", "SawitBunch");
        SawitSeedLv2 = GameDataBuilder.Load<ItemDataSO>("Items", "SawitSeedLv2");
        SawitLv2 = GameDataBuilder.Load<CropDataSO>("Crops", "SawitLv2");
    }

    public static void BakeIcons() => GrayboxIconBaker.BakeIcons(new[] { Resin, SawitBunch, SawitSeedLv2 });

    private static DialogueSO Dialogue(string file, params DialogueSO.DialogueLine[] lines) => GameDataBuilder.Dialogue(file, default, lines);
}
