using System;
using System.Collections.Generic;
using UnityEngine;

// Bottom HUD strip: one slot per item type the player owns, in the order they were obtained. An item at 0 loses its slot.
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private InventorySingleUI slotTemplate;

    private readonly Dictionary<ItemDataSO, InventorySingleUI> slotDictionary = new Dictionary<ItemDataSO, InventorySingleUI>();

    private void Awake()
    {
        slotTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        Inventory.Instance.OnItemChanged += Inventory_OnItemChanged;
        foreach (ItemDataSO itemDataSO in Inventory.Instance.GetItemOrderList())
            if (Inventory.Instance.GetCount(itemDataSO) > 0) GetSlot(itemDataSO).SetItem(itemDataSO, Inventory.Instance.GetCount(itemDataSO), false);
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null) Inventory.Instance.OnItemChanged -= Inventory_OnItemChanged;
    }

    private void Inventory_OnItemChanged(object sender, Inventory.OnItemChangedEventArgs e)
    {
        if (e.count > 0)
        {
            GetSlot(e.itemDataSO).SetItem(e.itemDataSO, e.count, e.delta > 0);
            return;
        }
        if (!slotDictionary.TryGetValue(e.itemDataSO, out InventorySingleUI slot)) return;
        slotDictionary.Remove(e.itemDataSO);
        Destroy(slot.gameObject);
    }

    private InventorySingleUI GetSlot(ItemDataSO itemDataSO)
    {
        if (slotDictionary.TryGetValue(itemDataSO, out InventorySingleUI slot)) return slot;
        slot = Instantiate(slotTemplate, container);
        slot.gameObject.SetActive(true);
        slotDictionary[itemDataSO] = slot;
        return slot;
    }
}
