using System;
using UnityEngine;

// The morning in the HouseInterior scene: wake up, read what lies on the bedside table, think it over, then the door
// leads out to that day's village scene. Day 1 is the debt letter; from Day 2 on it is the savings book. Day 3 is the next
// morning (the yard day); Day 4 opens two weeks later, on a caption.
public class BedroomDirector : MonoBehaviour
{
    [SerializeField] private Day1SO day1SO;
    [SerializeField] private Day2SO day2SO;
    [Tooltip("Day 3, the yard day: its morning thought.")]
    [SerializeField] private YardDaySO yardDaySO;
    [Tooltip("Day 4 (the forest day, whose classes are still named Day3): the time skip and its morning thought.")]
    [SerializeField] private Day3SO day3SO;
    [SerializeField] private BedsideLetter bedsideLetter;
    [SerializeField] private BedsideLetter savingsBook;
    [SerializeField] private SavingsBookUI savingsBookUI;
    [SerializeField] private BedroomDoor bedroomDoor;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private CutsceneActor playerActor;
    [SerializeField] private float wakeUpDuration = 2.4f;
    [Tooltip("Testing: play the Day 2 morning even though Day 1 wasn't played (Day 2's scene then fakes yesterday's palms).")]
    [SerializeField] private bool isTestingDay2;
    [Tooltip("Testing: play the Day 3 (yard day) morning even though Days 1 and 2 weren't played.")]
    [SerializeField] private bool isTestingDay3;
    [Tooltip("Testing: play the Day 4 morning (two weeks later) even though Days 1 to 3 weren't played.")]
    [SerializeField] private bool isTestingDay4;

    private float wakeUpTimer;
    private bool hasStarted;
    // 1 to 4: which morning this is
    private int day;
    private bool isSkipCaptionPlaying;
    // today's thing to read on the bedside table
    private BedsideLetter readable;

    private void Start()
    {
        bool isTesting = isTestingDay2 || isTestingDay3 || isTestingDay4;
        if (isTesting && !WorldState.IsDay1Complete()) WorldState.CompleteDay1();
        if ((isTestingDay3 || isTestingDay4) && !WorldState.IsDay2Complete()) WorldState.CompleteDay2();
        if (isTestingDay4 && !WorldState.IsDay3Complete()) WorldState.CompleteDay3();
        day = WorldState.IsDay3Complete() ? 4 : WorldState.IsDay2Complete() ? 3 : WorldState.IsDay1Complete() ? 2 : 1;
        readable = day > 1 ? savingsBook : bedsideLetter;
        (day > 1 ? bedsideLetter : savingsBook).gameObject.SetActive(false);
        readable.OnClosed += Readable_OnClosed;
        savingsBook.OnOpened += SavingsBook_OnOpened;
        bedroomDoor.OnPassed += BedroomDoor_OnPassed;
        screenFade.OnBlack += ScreenFade_OnBlack;
        screenFade.OnFinished += ScreenFade_OnFinished;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;

        bedroomDoor.SetLocked(true);
        if (day > 1)
        {
            WorldState.OpenAccounts(day2SO.startingSavings, day2SO.startingDebt, day2SO.daysUntilDue);
            WorldState.StartDay(day);
        }
        if (day == 4) WorldState.SkipTime(day3SO.paidDuringSkip, day3SO.daysUntilDue);
        isSkipCaptionPlaying = day == 4;
        TaskManager.Instance.SetObjectives(day > 1 ? day2SO.morningObjectiveArray : day1SO.letterObjectiveArray);
    }

    private void Update()
    {
        // GameManager.Start puts the game in Playing, so the wake-up cutscene begins on the first Update
        if (!hasStarted)
        {
            hasStarted = true;
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            if (isSkipCaptionPlaying) screenFade.Play(day3SO.skipCaption, 2f);
            else WakeUp();
        }
        if (wakeUpTimer <= 0f) return;
        wakeUpTimer -= Time.deltaTime;
        if (wakeUpTimer <= 0f) GameManager.Instance.SetState(GameManager.State.Playing);
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
    }

    private void SavingsBook_OnOpened(object sender, EventArgs e) => savingsBookUI.Show(savingsBook.Close);

    private void Readable_OnClosed(object sender, EventArgs e)
    {
        DialogueSO thought = day == 4 ? day3SO.morningDialogueSO : day == 3 ? yardDaySO.morningDialogueSO : day == 2 ? day2SO.morningDialogueSO : day1SO.monologueDialogueSO;
        DialogueManager.Instance.StartDialogue(null, thought);
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (e.npc != null || !readable.IsRead()) return;
        // the monologue is over: the door opens
        bedroomDoor.SetLocked(false);
        TaskManager.Instance.CompleteStory();
    }

    private void BedroomDoor_OnPassed(object sender, EventArgs e)
    {
        GameManager.Instance.SetState(GameManager.State.Cutscene);
        screenFade.Play(string.Empty, .3f);
    }

    private void ScreenFade_OnBlack(object sender, EventArgs e)
    {
        // the same fade shows the time-skip caption first thing in the morning
        if (isSkipCaptionPlaying) return;
        Loader.Scene scene = day == 4 ? Loader.Scene.FixScene_Day4 : day == 3 ? Loader.Scene.FixScene_Day3 : day == 2 ? Loader.Scene.FixScene_Day2 : Loader.Scene.FixScene;
        Loader.Load(scene);
    }

    private void ScreenFade_OnFinished(object sender, EventArgs e)
    {
        if (!isSkipCaptionPlaying) return;
        isSkipCaptionPlaying = false;
        WakeUp();
    }

    private void WakeUp()
    {
        wakeUpTimer = wakeUpDuration;
        playerActor.PlayEmote(Emote.WakeUp);
    }
}
