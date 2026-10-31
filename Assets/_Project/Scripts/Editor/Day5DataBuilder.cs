using UnityEditor;

// Day 5's ScriptableObjects ("Half the Forest"): the dialogues and the per-state checklists. The text only seeds the assets;
// designers edit the .asset files afterwards. Rule of the story: the MC never mentions the red, only the villagers do, so
// Day 6's Distributor can't have heard about it from the MC.
// Needs Day2DataBuilder.LoadAll() and the Lv.2 bunch (Day3DataBuilder) first.
public static class Day5DataBuilder
{
    // the punch-the-trees version's assets, gone since the plantation set piece replaced it
    private static readonly string[] RetiredFileArray =
    {
        "Dialogues/Day5_TreesIdea", "Dialogues/Day5_FirstTree", "Dialogues/Day5_TreesCleared",
        "Tasks/D5_ClearForest", "Tasks/D5_MakeLv2", "Tasks/D5_PlantClearing", "Tasks/D5_Harvest",
    };

    public static Day5SO Build(ItemDataSO bunchLv2)
    {
        DialogueSO.DialogueLine N(string text, Emote npcEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Npc, text = text, npcEmote = npcEmote };
        DialogueSO.DialogueLine P(string text, Emote playerEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Player, text = text, playerEmote = playerEmote };

        foreach (string file in RetiredFileArray) AssetDatabase.DeleteAsset($"Assets/_Project/ScriptableObjects/{file}.asset");

        Day5SO day5 = GameDataBuilder.Asset<Day5SO>("Day5", "Day5");
        day5.testStartingDebt = 28000000;
        day5.testDaysUntilDue = 16;
        day5.nightCaption = "That evening...";
        day5.crateLockedReason = "Not yet... I need every bunch.";

        day5.morningDialogueSO = Dialogue("Day5_Morning",
            P("Fifteen days. One field won't do it.", Emote.HeavySigh),
            P("But two arms might.", Emote.WideSmile));
        day5.wishDialogueSO = Dialogue("Day5_Wish",
            P("Four plots yesterday..."),
            P("What if all of that was palms?", Emote.WideSmile));
        day5.eatOneDialogueSO = Dialogue("Day5_EatOne",
            P("One sawit gave me the strength for four plots."),
            P("Let's see what one does today."));
        day5.tinyGrowthDialogueSO = Dialogue("Day5_TinyGrowth",
            P("...One bed?!", Emote.Surprised),
            P("That's it? Yesterday one sawit moved trees!"),
            P("I'll need to eat a lot more than that.", Emote.WideSmile));
        day5.noBunchDialogueSO = Dialogue("Day5_NoBunch",
            P("I need Lv.2 sawit for this. The palms should have some."));
        day5.revealDialogueSO = Dialogue("Day5_Reveal",
            P("...Whoa.", Emote.Surprised),
            P("A whole plantation. Ripe. In one punch.", Emote.Cheer));
        day5.fieldDoneDialogueSO = Dialogue("Day5_FieldDone",
            P("Half a day's work!", Emote.Cheer),
            P("Time to sell, and see how everyone in the village is doing."));
        day5.ijahDialogueSO = Dialogue("Ijah_Day5",
            N("The flowers by your new field went the colour of rust overnight.", Emote.TiltHead),
            N("Flowers don't do that, child."),
            P("New soil, Nek. It'll settle.", Emote.WideSmile));
        day5.ratnaDialogueSO = Dialogue("Ratna_Day5",
            N("Dimas brought home a leaf from the forest edge. It was red..."),
            N("And warm. Like a hand.", Emote.HeavySigh),
            P("Kids and their stories, Bu Ratna.", Emote.WideSmile));
        day5.dimasDialogueSO = Dialogue("Dimas_Day5",
            N("I dug up a worm by the river and it was RED. Glowing red!", Emote.Cheer),
            N("Is that treasure?"),
            P("Just the dry season, Dimas. Dig somewhere else for a while."));
        day5.eveningMathDialogueSO = Dialogue("Day5_EveningMath",
            P("The best day yet."),
            P("At this rate the debt is gone before the deadline.", Emote.Cheer));
        day5.nightDialogueSO = Dialogue("Day5_Night",
            P("Half the forest by the end of the month..."),
            P("Nobody in this village will ever owe anyone again."));

        day5.harvestObjectiveArray = new[] { GameDataBuilder.Task("D5_HarvestLv2", TaskObjectiveSO.Type.HarvestFruit, "Harvest 4 Lv.2 sawit bunches", bunchLv2, 4) };
        day5.eatOneObjectiveArray = new[] { GameDataBuilder.Task("D5_EatOne", TaskObjectiveSO.Type.Story, "Eat a Lv.2 sawit bunch", null, 1) };
        day5.eatManyObjectiveArray = new[] { GameDataBuilder.Task("D5_EatMany", TaskObjectiveSO.Type.Story, "Eat the rest of your Lv.2 sawit", null, 1) };
        day5.errandObjectiveArray = new[]
        {
            GameDataBuilder.Task("D5_Sell", TaskObjectiveSO.Type.SellItem, "Sell sawit at the ICAN crate", null, 1),
            GameDataBuilder.Task("D5_CheckVillagers", TaskObjectiveSO.Type.Story, "Check on Nek Ijah, Bu Ratna and Dimas", null, 3),
        };
        day5.nightObjectiveArray = new[] { GameDataBuilder.Task("D5_GoHome", TaskObjectiveSO.Type.EnterHouse, "Go home", null, 1) };
        EditorUtility.SetDirty(day5);
        AssetDatabase.SaveAssets();
        return day5;
    }

    private static DialogueSO Dialogue(string file, params DialogueSO.DialogueLine[] lines) => GameDataBuilder.Dialogue(file, default, lines);
}
