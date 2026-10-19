using UnityEngine;

// Walk/idle from the agent speed; mirrors the dialogue reactions on the world model too.
public class NpcVisual : MonoBehaviour
{
    [SerializeField] private Npc npc;
    [SerializeField] private NpcWander npcWander;
    [SerializeField] private Animator animator;
    [SerializeField] private CutsceneActor cutsceneActor;
    // arms reaching forward around a big load; the Z keeps them slightly apart
    [SerializeField] private Vector3 carryArmLEuler = new Vector3(-85f, 0f, -6f);
    [SerializeField] private Vector3 carryArmREuler = new Vector3(-85f, 0f, 6f);

    private Transform armL;
    private Transform armR;
    private float carryWeight;

    private void Start()
    {
        DialogueManager.Instance.OnLineChanged += DialogueManager_OnLineChanged;
        if (cutsceneActor != null) cutsceneActor.OnEmoteRequested += CutsceneActor_OnEmoteRequested;
        armL = animator.transform.Find("Hips/ArmL");
        armR = animator.transform.Find("Hips/ArmR");
    }

    private void Update()
    {
        // a cutscene walk moves the transform with the agent off
        bool isCutsceneMoving = cutsceneActor != null && cutsceneActor.IsMoving();
        animator.SetFloat(ChibiAnimation.SPEED, isCutsceneMoving ? 1f : npcWander.GetSpeedNormalized());
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
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnLineChanged -= DialogueManager_OnLineChanged;
    }

    private void DialogueManager_OnLineChanged(object sender, DialogueManager.OnLineChangedEventArgs e)
    {
        if (e.npc == npc) ChibiAnimation.PlayEmote(animator, e.line.npcEmote);
    }

    private void CutsceneActor_OnEmoteRequested(object sender, CutsceneActor.OnEmoteRequestedEventArgs e) => ChibiAnimation.PlayEmote(animator, e.emote);
}
