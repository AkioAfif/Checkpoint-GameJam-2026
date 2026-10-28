using System;
using UnityEngine;

[CreateAssetMenu()]
public class DialogueSO : ScriptableObject
{
    public enum Speaker { Player, Npc }

    [Serializable]
    public class DialogueLine
    {
        public Speaker speaker;
        [TextArea(2, 4)] public string text;
        // reactions played on the 3D portraits when this line appears (None = no animation)
        public Emote playerEmote;
        public Emote npcEmote;
        // every letter trembles (a shout in shock)
        public bool isShaking;
        // the Distributor's glasses flash opaque white (weird, uneasy, suspicious moments)
        public bool isNpcGlaring;
        // someone the line talks about: the camera cuts to them while it shows (empty = no cut)
        public NpcSO focusNpcSO;
    }

    public DialogueLine[] lineArray;
    // given once, when this dialogue ends (amount 0 = no reward)
    public ItemAmount reward;
}
