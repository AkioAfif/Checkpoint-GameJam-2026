using System;
using UnityEngine;

// ICAN's collection crate by the Fabricator: everything in the bag ICAN buys goes in at once, and the Koin go to the lender.
public class SellCrate : MonoBehaviour, IInteractable
{
    public static event EventHandler<OnAnySoldEventArgs> OnAnySold;
    public class OnAnySoldEventArgs : EventArgs { public long amount; }

    [SerializeField] private ItemDataSO[] sellableItemDataSOArray;
    [SerializeField] private string promptVerb = "sell to ICAN";
    [SerializeField] private string blockedReason = "Nothing ICAN buys";
    [SerializeField] private Transform promptAnchor;

    private bool isLocked;
    private string lockedReason;

    public bool IsSelectable() => true;
    public bool CanInteract(Player player) => !isLocked && GetSaleValue() > 0L;

    public void Interact(Player player)
    {
        long amount = GetSaleValue();
        foreach (ItemDataSO itemDataSO in sellableItemDataSOArray) Inventory.Instance.TryRemoveItem(itemDataSO, Inventory.Instance.GetCount(itemDataSO));
        WorldState.Earn(amount);
        OnAnySold?.Invoke(this, new OnAnySoldEventArgs { amount = amount });
    }

    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => isLocked ? lockedReason : blockedReason;

    // the day's story isn't at the selling yet
    public void SetLocked(bool isLocked, string lockedReason)
    {
        this.isLocked = isLocked;
        this.lockedReason = lockedReason;
    }
    public Transform GetPromptAnchor() => promptAnchor;

    private long GetSaleValue()
    {
        long value = 0L;
        foreach (ItemDataSO itemDataSO in sellableItemDataSOArray) value += Inventory.Instance.GetCount(itemDataSO) * itemDataSO.sellPrice;
        return value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStaticData() => OnAnySold = null;
}
