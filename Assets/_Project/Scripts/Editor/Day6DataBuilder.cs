using UnityEditor;

// Day 6's ScriptableObjects ("The Offer"): the dialogues and checklists, and the savings book's paid-off page (Day2SO). The
// text only seeds the assets; designers edit the .asset files afterwards. The MC still never mentions the red: the
// Distributor does, and that is his slip.
public static class Day6DataBuilder
{
    // the first version's endings, replaced by the choice
    private static readonly string[] RetiredFileArray = { "Dialogues/Day6_Night", "Tasks/D6_CheckVillagers" };

    public const string BOOK_PAID_FORMAT = "Savings<pos=55%>{0} Koin\nDebt<pos=55%><color=#3E8E5A>PAID</color>";

    public static Day6SO Build(ItemDataSO bunchLv2)
    {
        DialogueSO.DialogueLine N(string text, Emote npcEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Npc, text = text, npcEmote = npcEmote };
        DialogueSO.DialogueLine P(string text, Emote playerEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Player, text = text, playerEmote = playerEmote };

        // every Day2SO (Day2 and its CozyFarm copy): the debt can hit 0 on any day the savings book opens, not just Day 6
        foreach (string guid in AssetDatabase.FindAssets("t:Day2SO"))
        {
            Day2SO day2 = AssetDatabase.LoadAssetAtPath<Day2SO>(AssetDatabase.GUIDToAssetPath(guid));
            day2.bookPaidFormat = BOOK_PAID_FORMAT;
            EditorUtility.SetDirty(day2);
        }

        foreach (string file in RetiredFileArray) AssetDatabase.DeleteAsset($"Assets/_Project/ScriptableObjects/{file}.asset");

        Day6SO day6 = GameDataBuilder.Asset<Day6SO>("Day6", "Day6");
        day6.daysUntilDue = 2;
        day6.skipCaption = "Two weeks later...";
        day6.eveningCaption = "That evening...";
        day6.nightCaption = "That night...";
        day6.choiceTitle = "The Lv.3 seeds";
        day6.expandChoiceText = "Take them";
        day6.stopChoiceText = "Refuse";

        day6.morningDialogueSO = Dialogue("Day6_Morning",
            P("Paid. Every last Koin.", Emote.Cheer),
            P("Nobody can take this house now.", Emote.WideSmile));
        day6.wideDialogueSO = Dialogue("Day6_Wide",
            P("Look at it..."),
            P("Half the forest, palms as far as the cliffs.", Emote.WideSmile));
        day6.profitDialogueSO = Dialogue("Day6_Profit",
            P("No lender. Every Koin of this is mine.", Emote.Cheer));
        day6.murkyWaterDialogueSO = Dialogue("Day6_MurkyWater",
            P("Brown?", Emote.TiltHead),
            P("...Silt from upstream. Rain must've come down the hills."));
        day6.offerDialogueSO = Dialogue("Day6_Offer",
            N("Well, well. Thirty days, and you did it in twenty-eight.", Emote.Cheer),
            N("I've never been wrong about a farmer."),
            P("...You came all this way to tell me that?", Emote.TiltHead),
            N("That, and this. Sawit Lv.3."),
            N("Four times the yield per palm. Your half of the forest, at Lv.3, earns in a week what it earned you in a month."),
            N("I'm not asking you to decide today."),
            N("But remember the first night: one empty bottle and a debt bigger than this village. Look how far you came."),
            GameDataBuilder.Glaring(N("And don't mind the red in the leaves. That's just the land learning to keep up with you.")),
            P("...I never told you about—", Emote.Surprised),
            GameDataBuilder.Glaring(N("Think it over. I'll be by the Fabricator.")));
        day6.doubtDialogueSO = Dialogue("Day6_Doubt",
            P("The red... He knew."),
            P("If he knew, what else is out there I haven't looked at?"));
        day6.distributorRepeatDialogueSO = Dialogue("Distributor_Day6",
            N("No rush. Good seeds keep."),
            N("Good farmers, though... they don't wait around."));
        day6.oldGardenDialogueSO = Dialogue("Day6_OldGarden",
            P("This was black mud a month ago."),
            P("Now it crumbles like bread crust.", Emote.HeavySigh));
        day6.dimasDialogueSO = Dialogue("Dimas_Day6",
            N("The worms are gone. Even the red ones.", Emote.Sad),
            N("Mum says don't put your feet in any more. It smells like rust."));
        day6.dartoDialogueSO = Dialogue("Darto_Day6",
            N("Half of last year's. Same seed, same field, same lazy farmer.", Emote.Cheer),
            N("...The ground's thirsty. I water it, it drinks, it's thirsty again.", Emote.HeavySigh));
        day6.ijahDialogueSO = Dialogue("Ijah_Day6",
            N("No jamu this week, child. Nothing to make it from."),
            N("Good things grow slowly. Bad things too, it seems. Slow, and then all at once.", Emote.HeavySigh));
        day6.rigelDialogueSO = Dialogue("Rigel_Day6",
            N("Come to see your handiwork?", Emote.Shake),
            P("Your garden...", Emote.Surprised),
            N("Dry. My water comes off the river. The river comes off the forest. You do the sums."),
            N("Ten years I asked the elders for that forest edge. For rice. Ten years they said no."),
            N("I thought they were old fools. They said the trees hold the water for the whole valley.", Emote.HeavySigh),
            N("Turns out they weren't fools. And you didn't ask anyone."),
            P("I paid off a debt bigger than this village, Rigel."),
            N("And the village is paying off yours now.", Emote.Shake),
            N("Stop planting. Let what's left of the forest stand. That's all I'm asking."),
            P("I... need to think."),
            N("Think fast. The river won't."));
        day6.ratnaDialogueSO = Dialogue("Ratna_Day6",
            N("ICAN pays us less for our cassava now. Too much on the market, they said."),
            N("Funny. It was always enough before your palms.", Emote.HeavySigh));
        day6.eveningBookDialogueSO = Dialogue("Day6_EveningBook",
            P("More money in one day than the whole village makes in a season."));
        day6.billDialogueSO = Dialogue("Day6_Bill",
            GameDataBuilder.Glaring(N("Rivers silt up. Soil tires. It all comes back, slowly.")),
            N("Money doesn't come back. You take it when it's offered."),
            N("Lv.3 tonight, and the rest of the forest by the end of the month."),
            N("The damage takes years. The profit takes a week."));
        day6.expandDialogueSO = Dialogue("Day6_Expand",
            N("I've never been wrong about a farmer.", Emote.Cheer));
        day6.stopDialogueSO = Dialogue("Day6_Stop",
            GameDataBuilder.Glaring(N("Pity.")),
            N("Farmers who stop tend to owe again. I'll be around when you do."));
        day6.expandNightDialogueSO = Dialogue("Day6_ExpandNight",
            P("Rigel's garden will come back. Everything comes back."),
            P("...It has to."));
        day6.stopNightDialogueSO = Dialogue("Day6_StopNight",
            P("Half the forest is still half the forest."),
            P("Tomorrow I find out how to give some of it back."));

        day6.harvestObjectiveArray = new[]
        {
            GameDataBuilder.Task("D6_HarvestLv2", TaskObjectiveSO.Type.HarvestFruit, "Harvest 6 Lv.2 sawit bunches", bunchLv2, 6),
            GameDataBuilder.Task("D6_Sell", TaskObjectiveSO.Type.SellItem, "Sell sawit at the ICAN crate", null, 1),
        };
        day6.investigateObjectiveArray = new[] { GameDataBuilder.Task("D6_SeeWhatHeMeant", TaskObjectiveSO.Type.Story, "See what he meant: the old garden, Dimas, Pak Darto, Nek Ijah, Rigel", null, 5) };
        day6.nightObjectiveArray = new[] { GameDataBuilder.Task("D6_GoHome", TaskObjectiveSO.Type.EnterHouse, "Go home", null, 1) };
        EditorUtility.SetDirty(day6);
        AssetDatabase.SaveAssets();
        return day6;
    }

    private static DialogueSO Dialogue(string file, params DialogueSO.DialogueLine[] lines) => GameDataBuilder.Dialogue(file, default, lines);
}
