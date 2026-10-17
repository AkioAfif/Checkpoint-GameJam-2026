using UnityEngine;

[CreateAssetMenu()]
public class NpcSO : ScriptableObject
{
    public string npcName;
    // the character rig shown in the dialogue portrait
    public Transform portraitPrefab;
    public DialogueSO firstDialogueSO;
    // used from the second talk on
    public DialogueSO repeatDialogueSO;
    public Color portraitBackground = new Color(.98f, .86f, .86f);
}
