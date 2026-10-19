using System;
using UnityEngine;

// The morning of Day 1 in the HouseInterior scene: wake up, read the debt letter, think it over, then the door leads out to the village.
public class BedroomDirector : MonoBehaviour
{
    [SerializeField] private Day1SO day1SO;
    [SerializeField] private BedsideLetter bedsideLetter;
    [SerializeField] private BedroomDoor bedroomDoor;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private CutsceneActor playerActor;
    [SerializeField] private float wakeUpDuration = 2.4f;

    private float wakeUpTimer;
    private bool hasStarted;

    private void Start()
    {
        bedsideLetter.OnClosed += BedsideLetter_OnClosed;
        bedroomDoor.OnPassed += BedroomDoor_OnPassed;
        screenFade.OnBlack += ScreenFade_OnBlack;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;

        bedroomDoor.SetLocked(true);
        TaskManager.Instance.SetObjectives(day1SO.letterObjectiveArray);
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

    private void BedsideLetter_OnClosed(object sender, EventArgs e)
    {
        DialogueManager.Instance.StartDialogue(null, day1SO.monologueDialogueSO);
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (e.npc != null || !bedsideLetter.IsRead()) return;
        // the monologue is over: the door opens
        bedroomDoor.SetLocked(false);
        TaskManager.Instance.CompleteStory();
    }

    private void BedroomDoor_OnPassed(object sender, EventArgs e)
    {
        GameManager.Instance.SetState(GameManager.State.Cutscene);
        screenFade.Play(string.Empty, .3f);
    }

    private void ScreenFade_OnBlack(object sender, EventArgs e) => Loader.Load(Loader.Scene.Level01_VillagePrototype);
}
