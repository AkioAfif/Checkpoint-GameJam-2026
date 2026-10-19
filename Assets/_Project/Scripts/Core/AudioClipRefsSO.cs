using UnityEngine;

// Drop clips in here and they play; empty arrays stay silent. (Hooked up in SoundManager.)
[CreateAssetMenu()]
public class AudioClipRefsSO : ScriptableObject
{
    public AudioClip[] collectArray;
    public AudioClip[] dialogueLineArray;
    public AudioClip[] qteHitArray;
    public AudioClip[] qtePerfectArray;
    public AudioClip[] qteMissArray;
    public AudioClip[] plantSuccessArray;
    public AudioClip[] plantFailArray;
    public AudioClip[] seedFabricatedArray;
    public AudioClip[] houseEnterArray;
    public AudioClip[] truckSkidArray;
    public AudioClip[] truckBurnoutArray;
}
