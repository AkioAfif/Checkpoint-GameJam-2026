using System;
using Unity.Cinemachine;
using UnityEngine;

// A line that talks about someone else (its focusNpcSO) cuts to them, so the player sees where they are; the next line
// without one, or the end of the talk, blends back to the follow camera.
public class DialogueFocusCameraVisual : MonoBehaviour
{
    [SerializeField] private CinemachineCamera focusCamera;
    [SerializeField] private Npc[] npcArray;

    private void Start()
    {
        DialogueManager.Instance.OnLineChanged += DialogueManager_OnLineChanged;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        focusCamera.enabled = false;
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.OnLineChanged -= DialogueManager_OnLineChanged;
        DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
    }

    private void DialogueManager_OnLineChanged(object sender, DialogueManager.OnLineChangedEventArgs e)
    {
        Npc target = FindNpc(e.line.focusNpcSO);
        if (target == null)
        {
            focusCamera.enabled = false;
            return;
        }
        // a fresh cut frames the target at once (the brain blends to it); a second target in a row slides over
        if (!focusCamera.enabled) focusCamera.PreviousStateIsValid = false;
        focusCamera.Follow = target.transform;
        focusCamera.LookAt = target.transform;
        focusCamera.enabled = true;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e) => focusCamera.enabled = false;

    private Npc FindNpc(NpcSO npcSO)
    {
        if (npcSO == null) return null;
        foreach (Npc npc in npcArray)
            if (npc.GetNpcSO() == npcSO) return npc;
        return null;
    }
}
