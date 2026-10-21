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
        Day3.landPrice = 4000000;
        Day3.nightCaption = "That evening...";

        Day3.morningDialogueSO = Dialogue("Day3_Morning",
            P("Two weeks of harvests... and it barely dents the number.", Emote.HeavySigh),
            P("Sixteen days left. I need more land."));
        Day3.soldDialogueSO = Dialogue("Day3_Sold",
            P("The resin palms sell for even more than the old ones.", Emote.Surprised),
            P("If I had more field, I could double it.", Emote.Cheer));
        Day3.dartoSellsLandDialogueSO = Dialogue("Darto_SellsLand",
            N("So you're the one feeding half of ICAN's trucks!", Emote.Cheer),
            N("That strip by the trees was my father's. My knees can't work it anymore."),
            P("How much, Pak Darto?"),
            N("For you? Four million, and you keep the village proud.", Emote.Nod),
            P("Deal.", Emote.Cheer),
            N("Just stop where the old trees start. That forest was here before any of us."));
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
            P("A good day... and half of it went straight back into the land."),
            P("Two weeks of harvests barely moved the debt. Sixteen more days won't do it either."),
            P("The field is too small. Even with Darto's strip.", Emote.HeavySigh));
        Day3.forestHookDialogueSO = Dialogue("Day3_ForestHook",
            P("All that land... just sitting there."),
            P("Darto said stop at the trees. But the trees don't pay debts."));

        TaskObjectiveSO harvest = GameDataBuilder.Task("D3_HarvestSawit", TaskObjectiveSO.Type.CollectItem, "Harvest 4 Lv.2 sawit bunches", SawitBunchLv2, 4);
        TaskObjectiveSO sell = GameDataBuilder.Task("D3_SellSawit", TaskObjectiveSO.Type.Story, "Sell the sawit at the ICAN crate", null, 1);
        TaskObjectiveSO buyLand = GameDataBuilder.Task("D3_BuyLand", TaskObjectiveSO.Type.Story, "Talk to Darto by the For Sale sign", null, 1);
        TaskObjectiveSO fabricate = GameDataBuilder.Task("D3_MakeLv2", TaskObjectiveSO.Type.FabricateSeed, "Press Sawit Lv.2 seeds in the Fabricator", null, 1);
        TaskObjectiveSO plant = GameDataBuilder.Task("D3_PlantStrip", TaskObjectiveSO.Type.PlantCrop, "Plant 4 seeds in the new field", null, 4);
        TaskObjectiveSO home = GameDataBuilder.Task("D3_GoHome", TaskObjectiveSO.Type.EnterHouse, "Go home", null, 1);
        Day3.harvestObjectiveArray = new[] { harvest, sell };
        Day3.buyLandObjectiveArray = new[] { buyLand };
        Day3.plantObjectiveArray = new[] { fabricate, plant };
        Day3.nightObjectiveArray = new[] { home };
        EditorUtility.SetDirty(Day3);
        AssetDatabase.SaveAssets();
    }

    private static DialogueSO Dialogue(string file, params DialogueSO.DialogueLine[] lines) => GameDataBuilder.Dialogue(file, default, lines);
}
