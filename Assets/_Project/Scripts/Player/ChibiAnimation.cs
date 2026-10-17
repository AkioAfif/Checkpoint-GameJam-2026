using UnityEngine;

// Parameter names of Chibi.controller, shared by the player, NPCs and the dialogue portraits.
public static class ChibiAnimation
{
    public const string SPEED = "Speed";
    public const string IS_CROUCHING = "IsCrouching";

    public static void PlayEmote(Animator animator, Emote emote)
    {
        if (emote == Emote.None) return;
        animator.SetTrigger(emote.ToString());
    }
}
