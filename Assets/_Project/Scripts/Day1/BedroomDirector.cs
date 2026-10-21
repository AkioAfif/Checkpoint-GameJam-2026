using System;
using UnityEngine;

// The morning in the HouseInterior scene: wake up, read what lies on the bedside table, think it over, then the door
// leads out to that day's village scene. Day 1 is the debt letter; from Day 2 on it is the savings book.
public class BedroomDirector : MonoBehaviour
{
    [SerializeField] private Day1SO day1SO;
    [SerializeField] private Day2SO day2SO;
    [SerializeField] private BedsideLetter bedsideLetter;
    [SerializeField] private BedsideLetter savingsBook;
    [SerializeField] private SavingsBookUI savingsBookUI;
    [SerializeField] private BedroomDoor bedroomDoor;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private CutsceneActor playerActor;
    [SerializeField] private float wakeUpDuration = 2.4f;
    [Tooltip("Testing: play the Day 2 morning even though Day 1 wasn't played (Day 2's scene then fakes yesterday's palms).")]
    [SerializeField] private bool isTestingDay2;

    private float wakeUpTimer;
    private bool hasStarted;
    private bool isDay2;
    // today's thing to read on the bedside table
    private BedsideLetter readable;

    private void Start()
    {
        if (isTestingDay2 && !WorldState.IsDay1Complete()) WorldState.CompleteDay1(new Vector3[0]);
        isDay2 = WorldState.IsDay1Complete();
        readable = isDay2 ? savingsBook : bedsideLetter;
        (isDay2 ? bedsideLetter : savingsBook).gameObject.SetActive(false);
        readable.OnClosed += Readable_OnClosed;
        savingsBook.OnOpened += SavingsBook_OnOpened;
        bedroomDoor.OnPassed += BedroomDoor_OnPassed;
        screenFade.OnBlack += ScreenFade_OnBlack;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;

        bedroomDoor.SetLocked(true);
        if (isDay2) WorldState.OpenAccounts(day2SO.startingSavings, day2SO.startingDebt, day2SO.daysUntilDue);
        TaskManager.Instance.SetObjectives(isDay2 ? day2SO.morningObjectiveArray : day1SO.letterObjectiveArray);
    }

    private void Update()
    {
        // GameManager.Start puts the game in Playing, so the wake-up cutscene begins on the first Update
        if (!hasStarted)
        {
            hasStarted = true;
            wakeUpTimer = wakeUpDuration;
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            playerActor.PlayEmote(Emote.WakeUp);
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
        DialogueManager.Instance.StartDialogue(null, isDay2 ? day2SO.morningDialogueSO : day1SO.monologueDialogueSO);
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

    private void ScreenFade_OnBlack(object sender, EventArgs e) => Loader.Load(isDay2 ? Loader.Scene.Level01_Day2 : Loader.Scene.Level01_VillagePrototype);
}
