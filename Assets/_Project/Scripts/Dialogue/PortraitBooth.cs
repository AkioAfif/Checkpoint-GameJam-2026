using UnityEngine;

// Hidden "photo studio" far below the map: copies of the player and the current NPC, filmed into RenderTextures.
public class PortraitBooth : MonoBehaviour
{
    [SerializeField] private Animator playerPortraitAnimator;
    [SerializeField] private Transform npcStand;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera npcCamera;
    [Tooltip("Degrees each copy turns from its camera toward the other portrait (player → screen right, NPC → screen left).")]
    [SerializeField] private float facingAngle = 30f;

    private Animator npcPortraitAnimator;

    private void Start()
    {
        DialogueManager.Instance.OnDialogueStarted += DialogueManager_OnDialogueStarted;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        DialogueManager.Instance.OnLineChanged += DialogueManager_OnLineChanged;
        // the player rig prefab can be rebuilt after the booth was, so re-tag its parts for the portrait cameras
        MoveToPortraitLayer(playerPortraitAnimator.transform);
        FaceAcross(playerPortraitAnimator.transform, playerCamera, 1f);
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
            FaceAcross(portrait, npcCamera, -1f);
            MoveToPortraitLayer(portrait);
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

    // faces the copy back at its camera, then turns it toward screen right (side 1) or left (-1), so the two portraits face each other
    private void FaceAcross(Transform copy, Camera portraitCamera, float side)
    {
        Vector3 toCamera = Vector3.ProjectOnPlane(portraitCamera.transform.position - copy.position, Vector3.up);
        Vector3 screenRight = Vector3.ProjectOnPlane(portraitCamera.transform.right, Vector3.up).normalized;
        Vector3 facing = toCamera.normalized * Mathf.Cos(facingAngle * Mathf.Deg2Rad) + screenRight * (side * Mathf.Sin(facingAngle * Mathf.Deg2Rad));
        copy.rotation = Quaternion.LookRotation(facing, Vector3.up);
    }

    private void MoveToPortraitLayer(Transform root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
    }

    private void SetCamerasEnabled(bool isEnabled)
    {
        playerCamera.enabled = isEnabled;
        npcCamera.enabled = isEnabled;
    }
}
