using System;
using UnityEngine;

// A gatherable spot: a flower (hides, then regrows) or the river bank (never runs out, needs an empty bottle).
public class ResourceNode : MonoBehaviour, IInteractable
{
    public static event EventHandler OnAnyCollected;

    public event EventHandler OnCollected;
    public event EventHandler OnRespawned;

    [SerializeField] private ItemDataSO itemDataSO;
    [SerializeField] private int amount = 1;
    [Tooltip("Consumed on collect (e.g. the empty bottle for water). Leave empty if not needed.")]
    [SerializeField] private ItemDataSO requiredItemDataSO;
    [Tooltip("Infinite nodes stay available after collecting (the river).")]
    [SerializeField] private bool isInfinite;
    [Tooltip("Seconds until it grows back; 0 = never.")]
    [SerializeField] private float respawnTime = 30f;
    [SerializeField] private string promptVerb = "collect";
    [SerializeField] private string blockedReason = "Need an empty bottle";
    [SerializeField] private Transform promptAnchor;

    private bool isAvailable = true;
    private float respawnTimer;

    private void Update()
    {
        if (isAvailable || respawnTime <= 0f) return;
        respawnTimer -= Time.deltaTime;
        if (respawnTimer > 0f) return;
        isAvailable = true;
        OnRespawned?.Invoke(this, EventArgs.Empty);
    }

    public bool IsSelectable() => isAvailable;

    public bool CanInteract(Player player) => requiredItemDataSO == null || Inventory.Instance.GetCount(requiredItemDataSO) > 0;

    public void Interact(Player player)
    {
        if (requiredItemDataSO != null && !Inventory.Instance.TryRemoveItem(requiredItemDataSO)) return;
        Inventory.Instance.AddItem(itemDataSO, amount);
        if (!isInfinite)
        {
            isAvailable = false;
            respawnTimer = respawnTime;
        }
        OnCollected?.Invoke(this, EventArgs.Empty);
        OnAnyCollected?.Invoke(this, EventArgs.Empty);
    }

    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => blockedReason;
    public Transform GetPromptAnchor() => promptAnchor;
    public ItemDataSO GetItemDataSO() => itemDataSO;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStaticData() => OnAnyCollected = null;
}
