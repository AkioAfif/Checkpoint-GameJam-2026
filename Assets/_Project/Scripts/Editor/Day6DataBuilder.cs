using UnityEditor;

// Day 6's ScriptableObjects ("The Offer"): the dialogues and checklists, and the savings book's paid-off page (Day2SO). The
// text only seeds the assets; designers edit the .asset files afterwards. The MC still never mentions the red: the
// Distributor does, and that is his slip.
public static class Day6DataBuilder
{
    public const string BOOK_PAID_FORMAT = "Savings<pos=55%>{0} Koin\nDebt<pos=55%><color=#3E8E5A>PAID</color>";

    public static Day6SO Build()
    {
        DialogueSO.DialogueLine N(string text, Emote npcEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Npc, text = text, npcEmote = npcEmote };
        DialogueSO.DialogueLine P(string text, Emote playerEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Player, text = text, playerEmote = playerEmote };

        Day2SO day2 = GameDataBuilder.Load<Day2SO>("Day2", "Day2");
        day2.bookPaidFormat = BOOK_PAID_FORMAT;
        EditorUtility.SetDirty(day2);

        Day6SO day6 = GameDataBuilder.Asset<Day6SO>("Day6", "Day6");
        day6.daysUntilDue = 2;
        day6.skipCaption = "Two weeks later...";
        day6.nightCaption = "That evening...";

        day6.morningDialogueSO = Dialogue("Day6_Morning",
            P("Paid. Every last Koin.", Emote.Cheer),
            P("Nobody can take this house now.", Emote.WideSmile));
        day6.wideDialogueSO = Dialogue("Day6_Wide",
            P("Look at it..."),
            P("Half the forest, palms as far as the cliffs.", Emote.WideSmile));
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
        day6.distributorRepeatDialogueSO = Dialogue("Distributor_Day6",
            N("No rush. Good seeds keep."),
            N("Good farmers, though... they don't wait around."));
        day6.ijahDialogueSO = Dialogue("Ijah_Day6",
            N("Oh. Morning, child.", Emote.HeavySigh),
            N("I'll be inside today. The air feels heavy."));
        day6.ratnaDialogueSO = Dialogue("Ratna_Day6",
            N("Congratulations on the debt. Really."),
            N("...Dimas, inside. Now.", Emote.Shake));
        day6.dimasDialogueSO = Dialogue("Dimas_Day6",
            N("Mum says I can't go near your palms any more.", Emote.Sad),
            N("Not even to look."));
        day6.nightDialogueSO = Dialogue("Day6_Night",
            P("Lv.3..."),
            P("Four times the yield."),
            P("I'll sleep on it."));

        day6.errandObjectiveArray = new[] { GameDataBuilder.Task("D6_CheckVillagers", TaskObjectiveSO.Type.Story, "Tell Nek Ijah, Bu Ratna and Dimas the news", null, 3) };
        day6.nightObjectiveArray = new[] { GameDataBuilder.Task("D6_GoHome", TaskObjectiveSO.Type.EnterHouse, "Go home and think it over", null, 1) };
        EditorUtility.SetDirty(day6);
        AssetDatabase.SaveAssets();
        return day6;
    }

    private static DialogueSO Dialogue(string file, params DialogueSO.DialogueLine[] lines) => GameDataBuilder.Dialogue(file, default, lines);
}
