using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    public event EventHandler<OnItemChangedEventArgs> OnItemChanged;
    public class OnItemChangedEventArgs : EventArgs { public ItemDataSO itemDataSO; public int count; public int delta; }
    // first time an item type is ever collected
    public event EventHandler<OnItemDiscoveredEventArgs> OnItemDiscovered;
    public class OnItemDiscoveredEventArgs : EventArgs { public ItemDataSO itemDataSO; }

    [Tooltip("What a new playthrough starts with. A scene loaded later carries on the items from WorldState instead.")]
    [SerializeField] private ItemAmount[] startingItemArray;

    private readonly Dictionary<ItemDataSO, int> countDictionary = new Dictionary<ItemDataSO, int>();
    private readonly List<ItemDataSO> itemOrderList = new List<ItemDataSO>();

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one Inventory", this);
        Instance = this;
        // the items live in WorldState so they cross scene loads; starting items count as known (no discovery card)
        WorldState.StartInventory(startingItemArray);
        foreach (ItemAmount itemAmount in WorldState.GetInventoryList())
        {
            countDictionary[itemAmount.itemDataSO] = itemAmount.amount;
            itemOrderList.Add(itemAmount.itemDataSO);
        }
    }

    public void AddItem(ItemDataSO itemDataSO, int amount = 1)
    {
        SetCount(itemDataSO, GetCount(itemDataSO) + amount, amount);
        if (WorldState.TryDiscoverItem(itemDataSO)) OnItemDiscovered?.Invoke(this, new OnItemDiscoveredEventArgs { itemDataSO = itemDataSO });
    }

    public bool TryRemoveItem(ItemDataSO itemDataSO, int amount = 1)
    {
        if (GetCount(itemDataSO) < amount) return false;
        SetCount(itemDataSO, GetCount(itemDataSO) - amount, -amount);
        return true;
    }

    public bool HasItems(ItemAmount[] itemAmountArray)
    {
        foreach (ItemAmount itemAmount in itemAmountArray)
            if (GetCount(itemAmount.itemDataSO) < itemAmount.amount) return false;
        return true;
    }

    public void RemoveItems(ItemAmount[] itemAmountArray)
    {
        foreach (ItemAmount itemAmount in itemAmountArray) TryRemoveItem(itemAmount.itemDataSO, itemAmount.amount);
    }

    public int GetCount(ItemDataSO itemDataSO) => countDictionary.TryGetValue(itemDataSO, out int count) ? count : 0;

    // items in the order they were first obtained (drives the HUD strip)
    public List<ItemDataSO> GetItemOrderList() => itemOrderList;

    private void SetCount(ItemDataSO itemDataSO, int count, int delta)
    {
        if (!countDictionary.ContainsKey(itemDataSO)) itemOrderList.Add(itemDataSO);
        countDictionary[itemDataSO] = count;
        WorldState.SetItemCount(itemDataSO, count);
        OnItemChanged?.Invoke(this, new OnItemChangedEventArgs { itemDataSO = itemDataSO, count = count, delta = delta });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
