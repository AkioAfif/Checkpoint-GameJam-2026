using UnityEngine;

// Hidden "photo studio" far below the map: copies of the player and the current NPC, filmed into RenderTextures.
public class PortraitBooth : MonoBehaviour
{
    [SerializeField] private Animator playerPortraitAnimator;
    [SerializeField] private Transform npcStand;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera npcCamera;

    private Animator npcPortraitAnimator;

    private void Start()
    {
        DialogueManager.Instance.OnDialogueStarted += DialogueManager_OnDialogueStarted;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        DialogueManager.Instance.OnLineChanged += DialogueManager_OnLineChanged;
        SetCamerasEnabled(false);
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.OnDialogueStarted -= DialogueManager_OnDialogueStarted;
        DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
        DialogueManager.Instance.OnLineChanged -= DialogueManager_OnLineChanged;
    }

    // e.npc is null for the player's monologues: only the player portrait is filmed
    private void DialogueManager_OnDialogueStarted(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (e.npc != null)
        {
            NpcSO npcSO = e.npc.GetNpcSO();
            Transform portrait = Instantiate(npcSO.portraitPrefab, npcStand);
            portrait.localPosition = Vector3.zero;
            portrait.localRotation = Quaternion.identity;
            int portraitLayer = gameObject.layer;
            foreach (Transform t in portrait.GetComponentsInChildren<Transform>()) t.gameObject.layer = portraitLayer;
            npcPortraitAnimator = portrait.GetComponent<Animator>();
            npcCamera.backgroundColor = npcSO.portraitBackground;
        }
        SetCamerasEnabled(true);
        npcCamera.enabled = e.npc != null;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (npcPortraitAnimator != null) Destroy(npcPortraitAnimator.gameObject);
        npcPortraitAnimator = null;
        SetCamerasEnabled(false);
    }

    private void DialogueManager_OnLineChanged(object sender, DialogueManager.OnLineChangedEventArgs e)
    {
        ChibiAnimation.PlayEmote(playerPortraitAnimator, e.line.playerEmote);
        if (npcPortraitAnimator != null) ChibiAnimation.PlayEmote(npcPortraitAnimator, e.line.npcEmote);
    }

    private void SetCamerasEnabled(bool isEnabled)
    {
        playerCamera.enabled = isEnabled;
        npcCamera.enabled = isEnabled;
    }
}
