using UnityEngine;

// The yard day's three errands, done in any order once the director opens them:
// - Nek Ijah lends her late husband's sickle for three flowers;
// - Pak Darto's hoe is missing (Dimas has it), and Dimas swaps it back for a sawit seed;
// - with the hoe in hand, Darto adds his rope.
// Each NPC's talk is picked from what is in the bag; the tool comes as the talk's reward, and what they asked for is taken
// once that talk ends. The checklist counts the tools (CollectItem objectives).
public class YardErrands : MonoBehaviour
{
    [SerializeField] private YardDaySO yardDaySO;
    [SerializeField] private Npc ijahNpc;
    [SerializeField] private Npc dartoNpc;
    [SerializeField] private Npc dimasNpc;
    [SerializeField] private ItemDataSO flowerItemDataSO;
    [SerializeField] private ItemDataSO seedItemDataSO;
    [SerializeField] private ItemDataSO sickleItemDataSO;
    [SerializeField] private ItemDataSO hoeItemDataSO;
    [SerializeField] private ItemDataSO ropeItemDataSO;

    private bool isOpen;
    private bool isHoeMissingKnown;
    private bool hasPaidIjah;
    private bool hasPaidDimas;

    private void Start()
    {
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
    }

    private void Update()
    {
        if (!isOpen || GameManager.Instance.GetState() == GameManager.State.Dialogue) return;
        ijahNpc.SetOverrideDialogue(Has(sickleItemDataSO) ? yardDaySO.ijahAfterDialogueSO
            : Inventory.Instance.GetCount(flowerItemDataSO) >= yardDaySO.ijahFlowerCount ? yardDaySO.ijahGiveDialogueSO : yardDaySO.ijahAskDialogueSO);
        dartoNpc.SetOverrideDialogue(!isHoeMissingKnown ? yardDaySO.dartoHoeMissingDialogueSO
            : Has(hoeItemDataSO) ? (Has(ropeItemDataSO) ? null : yardDaySO.dartoRopeDialogueSO) : yardDaySO.dartoWhereDialogueSO);
        // he only owns up to the hoe once Darto has noticed it's gone
        dimasNpc.SetOverrideDialogue(!isHoeMissingKnown ? null : Has(hoeItemDataSO) ? yardDaySO.dimasAfterDialogueSO
            : Has(seedItemDataSO) ? yardDaySO.dimasTradeDialogueSO : yardDaySO.dimasAskDialogueSO);
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
    }

    // the reward (the tool) is in the bag by now; take what they asked for in return
    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (!isOpen) return;
        if (e.npc == ijahNpc && Has(sickleItemDataSO) && !hasPaidIjah)
        {
            hasPaidIjah = true;
            Inventory.Instance.TryRemoveItem(flowerItemDataSO, yardDaySO.ijahFlowerCount);
        }
        else if (e.npc == dartoNpc) isHoeMissingKnown = true;
        else if (e.npc == dimasNpc && Has(hoeItemDataSO) && !hasPaidDimas)
        {
            hasPaidDimas = true;
            Inventory.Instance.TryRemoveItem(seedItemDataSO);
        }
    }

    public void Open() => isOpen = true;

    // until he swaps it for the seed
    public bool HasDimasGotHoe() => !hasPaidDimas;

    // everyone back to their usual talk
    public void Close()
    {
        isOpen = false;
        ijahNpc.SetOverrideDialogue(null);
        dartoNpc.SetOverrideDialogue(null);
        dimasNpc.SetOverrideDialogue(null);
    }

    private static bool Has(ItemDataSO itemDataSO) => Inventory.Instance.GetCount(itemDataSO) > 0;
}
