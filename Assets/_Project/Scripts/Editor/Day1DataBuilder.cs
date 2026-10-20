using UnityEditor;
using UnityEngine;

// Day 1's ScriptableObjects: dialogue sequences 1-4, the letter, the per-state checklists and Sari. The text only seeds the assets;
// designers edit the .asset files afterwards (re-running refreshes the text, keeps the GUIDs).
public static class Day1DataBuilder
{
    public static Day1SO Day1;
    public static NpcSO Sari;

    public static void Build()
    {
        DialogueSO.DialogueLine N(string text, Emote npcEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Npc, text = text, npcEmote = npcEmote };
        DialogueSO.DialogueLine P(string text, Emote playerEmote = Emote.None) => new DialogueSO.DialogueLine { speaker = DialogueSO.Speaker.Player, text = text, playerEmote = playerEmote };

        // Sari is a regular villager: her first line is Dialogue Sequence 3
        DialogueSO sariFirst = Dialogue("Sari_First",
            N("Did you see that?! He nearly flattened me!", Emote.Surprised),
            N("Who drives like that over a bridge? Reckless!", Emote.Shake),
            N("Hey! What's that giant tube in front of your house?", Emote.Wave),
            P("A Fabricator. A man from the Instansi gave it to me. For free."),
            N("For free?", Emote.TiltHead),
            N("Nothing's free. Not even my grandmother's advice, and she gives that to everyone."),
            P("It makes seeds. Good ones. It could pay off the debt, Sari."),
            N("Then I hope it does."),
            N("Just read the small print, okay?", Emote.WideSmile),
            P("I'll get back to work now.", Emote.Wave));
        DialogueSO sariRepeat = Dialogue("Sari_Repeat", N("Read the small print, okay?", Emote.Nod));
        Sari = GameDataBuilder.Npc("Sari", "Sari", CharacterBuilder.RigPrefab(CharacterBuilder.SariLook), sariFirst, sariRepeat, new Color(.80f, .94f, .92f));

        Day1 = GameDataBuilder.Asset<Day1SO>("Day1", "Day1");
        Day1.letterTitle = "FINAL NOTICE";
        Day1.letterBody = "The harvest loan on your field is overdue.\n\nThe rains took the last crop, but the debt did not wash away. Settle the full amount, or the land goes to the lender.";
        Day1.debtAmount = "48.500.000 Koin";
        Day1.letterCloseHint = "Press [{0}] to put it down";
        Day1.nightCaption = "That evening...";

        Day1.monologueDialogueSO = Dialogue("Day1_Monologue",
            P("How am I supposed to pay off a debt this huge..."),
            P("Maybe there are still plenty of crops I can harvest today. I'll go check the field. Maybe something survived.", Emote.HeavySigh));

        Day1.introDialogueSO = Dialogue("Day1_Intro",
            N("Good morning! You must be the one who works this field. I've heard about you.", Emote.Wave),
            P("Heard what?"),
            N("That you work harder than anyone in this village, and that the rain has not been kind to you.", Emote.Nod),
            N("I represent the Instansi. We help hard-working families get back on their feet. This is a Fabricator. It's yours."),
            P("Mine? Are you serious? How much does it cost?", Emote.Surprised),
            N("Nothing. Not one coin.", Emote.WideSmile),
            N("Put plants in, and it gives you sawit seeds. Sawit grows fast and sells well. I'll buy every one."),
            P("Why would you give this away for free?"),
            N("Because when you do well, we do well. Simple. Let's try it together. Bring me three plants from your field."));

        Day1.pressedDialogueSO = Dialogue("Day1_FirstPress",
            N("Look at that! First try. You have a gift for this, partner.", Emote.Cheer),
            P("That was easy.", Emote.Surprised),
            N("The best things usually are. Plant two seeds, and tomorrow you'll see what I mean.", Emote.Nod),
            N("Now, try gathering some water, plants, and flowers from around the village. Let's try making 3 more for today."));

        Day1.eveningWhatDialogueSO = Dialogue("Day1_EveningWhat", P("What?!", Emote.Surprised));
        Day1.eveningMonologueDialogueSO = Dialogue("Day1_EveningMonologue",
            P("They've sprouted already... in one afternoon?", Emote.TiltHead),
            P("If sawit grows this fast, I'll earn way more than I thought."),
            P("I could pay off the debt much sooner."),
            P("I need to work even harder!", Emote.Cheer));

        Day1.farewellDialogueSO = Dialogue("Day1_Farewell",
            N("Look at you, a natural! The village will be talking about this field soon.", Emote.Cheer),
            P("I still can't believe it actually works.", Emote.WideSmile),
            N("Believe it, partner. Keep the seeds coming and I'll be back to buy every one.", Emote.Nod),
            N("Well, I've got other families to help. Until next time!", Emote.Wave));

        Day1.waitingTutorial1DialogueSO = Dialogue("Distributor_Waiting1", N("Whenever you're ready. No pressure.", Emote.Nod));
        Day1.waitingTutorial2DialogueSO = Dialogue("Distributor_Waiting2", N("Take your time, partner. The machine isn't going anywhere.", Emote.Nod));

        ItemDataSO crop = GameDataBuilder.Crop, water = GameDataBuilder.Water, flower = GameDataBuilder.Flower;
        TaskObjectiveSO read = GameDataBuilder.Task("D1_ReadLetter", TaskObjectiveSO.Type.Story, "Read the letter on the bedside table", null, 1);
        TaskObjectiveSO collectCrops = GameDataBuilder.Task("D1_Collect3Plants", TaskObjectiveSO.Type.CollectItem, "Collect 3 plants from your field", crop, 3);
        TaskObjectiveSO fabricate = GameDataBuilder.Task("D1_FirstPress", TaskObjectiveSO.Type.FabricateSeed, "Make sawit seeds in the Fabricator", null, 1);
        TaskObjectiveSO plant = GameDataBuilder.Task("D1_Plant2", TaskObjectiveSO.Type.PlantCrop, "Plant 2 sawit seeds", null, 2);
        TaskObjectiveSO gatherWater = GameDataBuilder.Task("D1_Gather3Water", TaskObjectiveSO.Type.CollectItem, "Gather 3 water", water, 3);
        TaskObjectiveSO gatherFlowers = GameDataBuilder.Task("D1_Gather3Flowers", TaskObjectiveSO.Type.CollectItem, "Gather 3 flowers", flower, 3);
        TaskObjectiveSO gatherPlants = GameDataBuilder.Task("D1_Gather3MorePlants", TaskObjectiveSO.Type.CollectItem, "Gather 3 more plants", crop, 3);
        TaskObjectiveSO fabricateMore = GameDataBuilder.Task("D1_Make3Seeds", TaskObjectiveSO.Type.FabricateSeed, "Make 3 more sawit seeds", null, 3);
        TaskObjectiveSO talk = GameDataBuilder.Task("D1_TalkDistributor", TaskObjectiveSO.Type.Story, "Talk to the Distributor", null, 1);
        TaskObjectiveSO sari = GameDataBuilder.Task("D1_SaySari", TaskObjectiveSO.Type.Story, "Check on Sari on the bridge", null, 1);
        TaskObjectiveSO home = GameDataBuilder.Task("D1_GoHome", TaskObjectiveSO.Type.EnterHouse, "Go home", null, 1);
        Day1.letterObjectiveArray = new[] { read };
        Day1.tutorialObjectiveArray = new[] { collectCrops, fabricate, plant };
        Day1.gatherObjectiveArray = new[] { plant, gatherWater, gatherFlowers, gatherPlants, fabricateMore };
        Day1.talkObjectiveArray = new[] { talk };
        Day1.sariObjectiveArray = new[] { sari };
        Day1.nightObjectiveArray = new[] { home };
        EditorUtility.SetDirty(Day1);
        AssetDatabase.SaveAssets();
    }

    private static DialogueSO Dialogue(string file, params DialogueSO.DialogueLine[] lines) => GameDataBuilder.Dialogue(file, default, lines);
}
