using UnityEngine;

// All of Day 1's text and checklists, so the director holds no strings.
[CreateAssetMenu()]
public class Day1SO : ScriptableObject
{
    [Header("Letter")]
    public string letterTitle;
    [TextArea(3, 8)] public string letterBody;
    public string debtAmount;
    public string letterCloseHint;

    [Header("Dialogue")]
    public DialogueSO monologueDialogueSO;
    public DialogueSO introDialogueSO;
    public DialogueSO pressedDialogueSO;
    // the goodbye after the Tutorial 2 list; he leaves when it ends
    public DialogueSO farewellDialogueSO;
    // the evening at the farm: the shock, then the MC's plans once the grown sawit is in view
    public DialogueSO eveningWhatDialogueSO;
    public DialogueSO eveningMonologueDialogueSO;
    // what the Distributor says when talked to while he waits
    public DialogueSO waitingTutorial1DialogueSO;
    public DialogueSO waitingTutorial2DialogueSO;

    [Header("Checklists")]
    public TaskObjectiveSO[] letterObjectiveArray;
    public TaskObjectiveSO[] tutorialObjectiveArray;
    public TaskObjectiveSO[] gatherObjectiveArray;
    public TaskObjectiveSO[] talkObjectiveArray;
    public TaskObjectiveSO[] sariObjectiveArray;
    public TaskObjectiveSO[] nightObjectiveArray;

    [Header("Screens")]
    public string nightCaption;
}
