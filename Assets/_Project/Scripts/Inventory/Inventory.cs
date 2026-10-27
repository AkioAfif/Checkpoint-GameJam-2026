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

    [SerializeField] private ItemAmount[] startingItemArray;

    private readonly Dictionary<ItemDataSO, int> countDictionary = new Dictionary<ItemDataSO, int>();
    private readonly List<ItemDataSO> itemOrderList = new List<ItemDataSO>();
    private readonly HashSet<ItemDataSO> discoveredSet = new HashSet<ItemDataSO>();

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one Inventory", this);
        Instance = this;
        // starting items are known already, so they never pop the discovery card
        foreach (ItemAmount itemAmount in startingItemArray)
        {
            discoveredSet.Add(itemAmount.itemDataSO);
            SetCount(itemAmount.itemDataSO, itemAmount.amount, itemAmount.amount);
        }
    }

    public void AddItem(ItemDataSO itemDataSO, int amount = 1)
    {
        SetCount(itemDataSO, GetCount(itemDataSO) + amount, amount);
        if (discoveredSet.Add(itemDataSO)) OnItemDiscovered?.Invoke(this, new OnItemDiscoveredEventArgs { itemDataSO = itemDataSO });
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

    // developer tools: every item ever held, with its count
    public ItemAmount[] GetCountArray()
    {
        ItemAmount[] countArray = new ItemAmount[itemOrderList.Count];
        for (int i = 0; i < countArray.Length; i++) countArray[i] = new ItemAmount { itemDataSO = itemOrderList[i], amount = GetCount(itemOrderList[i]) };
        return countArray;
    }

    // developer tools: sets the bag to exactly these counts. Delta is 0, so no discovery card and no task progress; the HUD still refreshes.
    public void RestoreCounts(ItemAmount[] countArray)
    {
        foreach (ItemDataSO itemDataSO in itemOrderList) SetCount(itemDataSO, 0, 0);
        foreach (ItemAmount itemAmount in countArray) SetCount(itemAmount.itemDataSO, itemAmount.amount, 0);
    }

    public int GetCount(ItemDataSO itemDataSO) => countDictionary.TryGetValue(itemDataSO, out int count) ? count : 0;

    // items in the order they were first obtained (drives the HUD strip)
    public List<ItemDataSO> GetItemOrderList() => itemOrderList;

    private void SetCount(ItemDataSO itemDataSO, int count, int delta)
    {
        if (!countDictionary.ContainsKey(itemDataSO)) itemOrderList.Add(itemDataSO);
        countDictionary[itemDataSO] = count;
        OnItemChanged?.Invoke(this, new OnItemChangedEventArgs { itemDataSO = itemDataSO, count = count, delta = delta });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
