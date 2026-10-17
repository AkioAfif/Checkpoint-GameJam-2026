using UnityEngine;

// Walk/idle from the agent speed; mirrors the dialogue reactions on the world model too.
public class NpcVisual : MonoBehaviour
{
    [SerializeField] private Npc npc;
    [SerializeField] private NpcWander npcWander;
    [SerializeField] private Animator animator;

    private void Start()
    {
        DialogueManager.Instance.OnLineChanged += DialogueManager_OnLineChanged;
    }

    private void Update()
    {
        animator.SetFloat(ChibiAnimation.SPEED, npcWander.GetSpeedNormalized());
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnLineChanged -= DialogueManager_OnLineChanged;
    }

    private void DialogueManager_OnLineChanged(object sender, DialogueManager.OnLineChangedEventArgs e)
    {
        if (e.npc == npc) ChibiAnimation.PlayEmote(animator, e.line.npcEmote);
    }
}
