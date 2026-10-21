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

        // the same bunch model and icon as Lv.1's, but ICAN pays more for it
        ItemDataSO lv1Bunch = Day2DataBuilder.SawitBunch;
        SawitBunchLv2 = GameDataBuilder.Item("SawitBunchLv2", "Sawit Bunch Lv.2", "A dark, heavy bunch from the resin palms. ICAN pays extra, and asks no questions.", lv1Bunch.prefab.gameObject, new Color(.62f, .12f, .14f));
        SawitBunchLv2.icon = lv1Bunch.icon;
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
            P("I feel... really like a different person right now.", Emote.Cheer),
            N("...."),
            N("This guy man...", Emote.Shake));
        Day3.rigelPretendDialogueSO = Dialogue("Day3_RigelPretend",
            N("I'll pretend nothing is happening...", Emote.HeavySigh));
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

    private static DialogueSO Dialogue(string file, params DialogueSO.DialogueLine[] lines) => GameDataBuilder.Dialogue(file, default, lines);
}
