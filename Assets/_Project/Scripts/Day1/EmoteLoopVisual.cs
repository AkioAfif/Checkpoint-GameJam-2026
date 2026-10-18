using UnityEngine;

// A character that keeps repeating one reaction, e.g. Sari waving from the bridge.
public class EmoteLoopVisual : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Emote emote = Emote.Wave;
    [SerializeField] private float interval = 2.4f;

    private float timer;

    private void Start()
    {
        timer = .3f;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = interval;
        ChibiAnimation.PlayEmote(animator, emote);
    }
}
