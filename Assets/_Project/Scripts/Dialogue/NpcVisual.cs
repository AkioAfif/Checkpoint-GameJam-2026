using UnityEngine;

// Walk/idle from the agent speed; mirrors the dialogue reactions on the world model too. While the next talk still has an
// item to give, that item spins over the head.
public class NpcVisual : MonoBehaviour
{
    [SerializeField] private Npc npc;
    [SerializeField] private NpcWander npcWander;
    [SerializeField] private Animator animator;
    [SerializeField] private CutsceneActor cutsceneActor;
    // arms reaching forward around a big load; the Z keeps them slightly apart
    [SerializeField] private Vector3 carryArmLEuler = new Vector3(-85f, 0f, -6f);
    [SerializeField] private Vector3 carryArmREuler = new Vector3(-85f, 0f, 6f);
    [Tooltip("Height of the reward item relative to the prompt anchor.")]
    [SerializeField] private float rewardMarkerOffset = -.1f;

    private Transform armL;
    private Transform armR;
    private float carryWeight;
    private Transform rewardMarker;

    private void Start()
    {
        DialogueManager.Instance.OnLineChanged += DialogueManager_OnLineChanged;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        if (cutsceneActor != null) cutsceneActor.OnEmoteRequested += CutsceneActor_OnEmoteRequested;
        armL = animator.transform.Find("Hips/ArmL");
        armR = animator.transform.Find("Hips/ArmR");
        RefreshRewardMarker();
    }

    private void Update()
    {
        // a cutscene walk moves the transform with the agent off
        bool isCutsceneMoving = cutsceneActor != null && cutsceneActor.IsWalking();
        animator.SetFloat(ChibiAnimation.SPEED, isCutsceneMoving ? 1f : npcWander.GetSpeedNormalized());

        if (rewardMarker == null) return;
        rewardMarker.localPosition = Vector3.up * (rewardMarkerOffset + Mathf.Sin(Time.time * 3f) * .08f);
        rewardMarker.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
    }

    // after the Animator, so the carry pose wins over the walk swing (the rig has no carry clip)
    private void LateUpdate()
    {
        bool isCarrying = cutsceneActor != null && cutsceneActor.IsCarrying();
        carryWeight = Mathf.MoveTowards(carryWeight, isCarrying ? 1f : 0f, 5f * Time.deltaTime);
        if (carryWeight <= 0f || armL == null || armR == null) return;
        armL.localRotation = Quaternion.Slerp(armL.localRotation, Quaternion.Euler(carryArmLEuler), carryWeight);
        armR.localRotation = Quaternion.Slerp(armR.localRotation, Quaternion.Euler(carryArmREuler), carryWeight);
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.OnLineChanged -= DialogueManager_OnLineChanged;
        DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
    }

    private void DialogueManager_OnLineChanged(object sender, DialogueManager.OnLineChangedEventArgs e)
    {
        if (e.npc == npc) ChibiAnimation.PlayEmote(animator, e.line.npcEmote);
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (e.npc == npc) RefreshRewardMarker();
    }

    private void CutsceneActor_OnEmoteRequested(object sender, CutsceneActor.OnEmoteRequestedEventArgs e) => ChibiAnimation.PlayEmote(animator, e.emote);

    private void RefreshRewardMarker()
    {
        DialogueSO dialogueSO = npc.GetNextDialogue();
        bool hasReward = DialogueManager.Instance.HasPendingReward(dialogueSO) && dialogueSO.reward.itemDataSO.prefab != null;
        if (!hasReward)
        {
            if (rewardMarker != null) Destroy(rewardMarker.gameObject);
            rewardMarker = null;
            return;
        }
        // shortcut: keeps the first item shown if a later dialogue gives a different one; respawn it when that happens
        if (rewardMarker == null) rewardMarker = Instantiate(dialogueSO.reward.itemDataSO.prefab, npc.GetPromptAnchor());
    }
}
