using UnityEditor;
using UnityEngine;

// Day 3's ScriptableObjects: the Lv.2 bunch (and the ripe Lv.2 palm that bears it), the dialogues, the money and the
// per-state checklists. The text only seeds the assets; designers edit the .asset files afterwards.
// Needs GameDataBuilder.LoadAll() and Day2DataBuilder.LoadAll() (or Build()) first.
public static class Day3DataBuilder
{
    public static Day3SO Day3;
    public static ItemDataSO SawitBunchLv2;

    public static void Build()
    {
        DialogueSO.DialogueLine N(string text, Emote npcEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Npc, text = text, npcEmote = npcEmote };
        DialogueSO.DialogueLine P(string text, Emote playerEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Player, text = text, playerEmote = playerEmote };

        // the gloamplum fruit (Lv.1's is the duskbell's); until its own icon is baked it borrows Lv.1's
        ItemDataSO lv1Bunch = Day2DataBuilder.SawitBunch;
        SawitBunchLv2 = GameDataBuilder.Item("SawitBunchLv2", "Sawit Bunch Lv.2", "A dark, heavy bunch from the resin palms. ICAN pays extra, and asks no questions.", Day2PrefabBuilder.BunchLv2Model(), new Color(.62f, .12f, .14f));
        if (SawitBunchLv2.icon == null) SawitBunchLv2.icon = lv1Bunch.icon;
        SawitBunchLv2.sellPrice = 1000000;
        EditorUtility.SetDirty(SawitBunchLv2);

        CropDataSO lv2 = Day2DataBuilder.SawitLv2;
        lv2.ripePrefab = Day2PrefabBuilder.RipePalmLv2().transform;
        lv2.harvestItemDataSO = SawitBunchLv2;
        EditorUtility.SetDirty(lv2);

        Day3 = GameDataBuilder.Asset<Day3SO>("Day3", "Day3");
        Day3.skipCaption = "Two weeks later...";
        Day3.paidDuringSkip = 9000000;
        Day3.daysUntilDue = 16;
        Day3.testStartingDebt = 46000000;
        Day3.nightCaption = "That evening...";

        Day3.morningDialogueSO = Dialogue("Day3_Morning",
            P("Two weeks of harvests... and it barely dents the number.", Emote.HeavySigh),
            P("Sixteen days left. I need more land."));
        Day3.landWishDialogueSO = Dialogue("Day3_LandWish",
            P("If only I could add more land there..."),
            P("But I don't have the strength or the time to build a new field.", Emote.HeavySigh));
        Day3.flashbackDialogueSO = Dialogue("Day3_Flashback",
            N("Besides selling it, you could just eat the harvest yourself.", Emote.Nod),
            N("You'll get a certain power that can get you out of any trouble."));
        Day3.tryEatDialogueSO = Dialogue("Day3_TryEat",
            P("Let me try eating one."));
        Day3.noBunchDialogueSO = Dialogue("Day3_NoBunch",
            P("I sold them all... The field should still have some."));
        DialogueSO.DialogueLine shout = N("WHAT ARE YOU TRYING TO DO", Emote.Surprised);
        shout.isShaking = true;
        Day3.rigelShoutDialogueSO = Dialogue("Day3_RigelShout", shout);
        Day3.buffDialogueSO = Dialogue("Day3_Buff",
            P("I feel... really like a different person right now.", Emote.Cheer));
        Day3.ratnaDialogueSO = Dialogue("Ratna_Day3",
            N("The whole village is talking about your sawit! My boy wants to be a farmer now.", Emote.WideSmile),
            P("Thank you, Bu Ratna!", Emote.Nod));
        Day3.ijahDialogueSO = Dialogue("Ijah_Day3",
            N("Ah, the young one who saved the farm. Come eat with us sometime.", Emote.Nod),
            P("I will, Nek Ijah. Once the debt is paid."));
        Day3.rigelDialogueSO = Dialogue("Rigel_Day3",
            N("Fine. You won the bet."),
            N("...This week.", Emote.TiltHead),
            P("Next week too.", Emote.WideSmile));
        Day3.eveningMathDialogueSO = Dialogue("Day3_EveningMath",
            P("A good day. And the new strip cost me nothing but one sawit."),
            P("Two weeks of harvests barely moved the debt. Sixteen more days won't do it either."),
            P("The field is still too small.", Emote.HeavySigh));
        Day3.forestHookDialogueSO = Dialogue("Day3_ForestHook",
            P("All that land... just sitting there."),
            P("With this strength... the trees wouldn't take long."));

        TaskObjectiveSO harvest = GameDataBuilder.Task("D3_HarvestSawit", TaskObjectiveSO.Type.CollectItem, "Harvest 4 Lv.2 sawit bunches", SawitBunchLv2, 4);
        TaskObjectiveSO sell = GameDataBuilder.Task("D3_SellSawit", TaskObjectiveSO.Type.Story, "Sell the sawit at the ICAN crate", null, 1);
        TaskObjectiveSO eat = GameDataBuilder.Task("D3_EatSawit", TaskObjectiveSO.Type.Story, "Eat a Lv.2 sawit bunch", null, 1);
        TaskObjectiveSO smash = GameDataBuilder.Task("D3_SmashLand", TaskObjectiveSO.Type.Story, "Go to the empty grass by the forest", null, 1);
        TaskObjectiveSO fabricate = GameDataBuilder.Task("D3_MakeLv2", TaskObjectiveSO.Type.FabricateSeed, "Press Sawit Lv.2 seeds in the Fabricator", null, 1);
        TaskObjectiveSO plant = GameDataBuilder.Task("D3_PlantStrip", TaskObjectiveSO.Type.PlantCrop, "Plant 4 seeds in the new field", null, 4);
        TaskObjectiveSO home = GameDataBuilder.Task("D3_GoHome", TaskObjectiveSO.Type.EnterHouse, "Go home", null, 1);
        Day3.harvestObjectiveArray = new[] { harvest, sell };
        Day3.eatObjectiveArray = new[] { eat };
        Day3.smashObjectiveArray = new[] { smash };
        Day3.plantObjectiveArray = new[] { fabricate, plant };
        Day3.nightObjectiveArray = new[] { home };
        EditorUtility.SetDirty(Day3);
        AssetDatabase.SaveAssets();
    }

    // Day 3 on the CozyFarm map: a copy of Day3 (refreshed each build) where the new field is the forest across the river from
    // the garden, so the MC punches its trees down first, and pounds a second bridge up afterwards. Only loads the existing data.
    public static Day3SO BuildCozyFarm()
    {
        DialogueSO.DialogueLine N(string text, Emote npcEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Npc, text = text, npcEmote = npcEmote };
        DialogueSO.DialogueLine P(string text, Emote playerEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Player, text = text, playerEmote = playerEmote };

        Day3 = GameDataBuilder.Load<Day3SO>("Day3", "Day3");
        SawitBunchLv2 = GameDataBuilder.Load<ItemDataSO>("Items", "SawitBunchLv2");
        Day3SO cozy = GameDataBuilder.Asset<Day3SO>("Day3", "Day3_CozyFarm");
        EditorUtility.CopySerialized(Day3, cozy);
        cozy.name = "Day3_CozyFarm";

        cozy.landWishDialogueSO = Dialogue("Day3_LandWish_CozyFarm",
            P("If only I could add more land there..."),
            P("But those trees are in the way, and I don't have the strength to clear them.", Emote.HeavySigh));
        // Rigel is already running home by now
        cozy.buffDialogueSO = Dialogue("Day3_Buff_CozyFarm",
            P("I feel... really like a different person right now.", Emote.Cheer));
        cozy.treesIdeaDialogueSO = Dialogue("Day3_TreesIdea",
            P("This strength... I bet I could knock those trees down.", Emote.WideSmile));
        cozy.firstTreeDialogueSO = Dialogue("Day3_FirstTree",
            P("One tree... with my bare hands?!", Emote.Surprised));
        cozy.treesClearedDialogueSO = Dialogue("Day3_TreesCleared",
            P("That's the last of them. Now there's room for a real field.", Emote.Cheer));
        cozy.bridgeIdeaDialogueSO = Dialogue("Day3_BridgeIdea",
            P("Walking all the way round to that old bridge every day is exhausting...", Emote.HeavySigh),
            P("What if there was a bridge right here, next to my farm?"));
        cozy.bridgeDoneDialogueSO = Dialogue("Day3_BridgeDone",
            P("Ha! Now the new field is only a few steps from home.", Emote.Cheer));
        // Rigel's suspicion is real, but he wanted that forest edge for his own paddy: he slips, the MC hears only jealousy
        cozy.rigelBridgeDialogueSO = Dialogue("Day3_RigelBridge",
            N("Stop right there.", Emote.Shake),
            P("Rigel? You ran off this morning like you'd seen a ghost.", Emote.TiltHead),
            N("I watched from my fence. Half that forest edge, gone before noon. And now a bridge that wasn't here an hour ago.", Emote.TiltHead),
            N("Nobody does that. Not with their hands. Not with a sawit."),
            P("Yesterday you said land should be made with your back. Well... it was.", Emote.WideSmile),
            N("Don't twist it. That was work. This is... something else.", Emote.Shake),
            N("Ten years the elders have said nobody clears that edge. Not for rice, not for anyone."),
            P("Funny. You never cared about those trees before today."),
            N("You don't know what I cared about.", Emote.HeavySigh),
            P("I know exactly what this is. My sawit outsold your rice, and now I've got more land than you."),
            P("You're jealous, Rigel. Just say it.", Emote.WideSmile),
            N("...Jealous."),
            N("That ground was supposed to be—", Emote.Surprised),
            N("Forget it. Enjoy your field while it lasts."));
        cozy.rigelBridgeAfterDialogueSO = Dialogue("Day3_RigelBridgeAfter",
            P("...Sore loser.", Emote.HeavySigh));
        cozy.eveningMathDialogueSO = Dialogue("Day3_EveningMath_CozyFarm",
            P("A good day. A new field and a new bridge, and all it cost me was one sawit."),
            P("Two weeks of harvests barely moved the debt. Sixteen more days won't do it either."),
            P("The field is still too small.", Emote.HeavySigh));
        cozy.forestHookDialogueSO = Dialogue("Day3_ForestHook_CozyFarm",
            P("There's still a whole forest out there..."),
            P("With this strength... it wouldn't take long."));

        cozy.treeObjectiveArray = new[] { GameDataBuilder.Task("D3_PunchTrees", TaskObjectiveSO.Type.Story, "Punch down the 3 trees across the river", null, 3) };
        cozy.smashObjectiveArray = new[] { GameDataBuilder.Task("D3_SmashClearing", TaskObjectiveSO.Type.Story, "Pound the cleared ground into a field", null, 1) };
        cozy.bridgeObjectiveArray = new[] { GameDataBuilder.Task("D3_BuildBridge", TaskObjectiveSO.Type.Story, "Go to the riverbank by the new field", null, 1) };
        cozy.crossBridgeObjectiveArray = new[] { GameDataBuilder.Task("D3_CrossBridge", TaskObjectiveSO.Type.Story, "Cross the new bridge home", null, 1) };
        EditorUtility.SetDirty(cozy);
        AssetDatabase.SaveAssets();
        return cozy;
    }

    private static DialogueSO Dialogue(string file, params DialogueSO.DialogueLine[] lines) => GameDataBuilder.Dialogue(file, default, lines);
}
