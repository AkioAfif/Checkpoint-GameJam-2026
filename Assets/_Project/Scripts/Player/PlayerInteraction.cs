using System;
using System.Collections.Generic;
using UnityEngine;

// Picks the interactable in front of the player and forwards [Interact] to it.
[RequireComponent(typeof(Player))]
public class PlayerInteraction : MonoBehaviour
{
    private const int MAX_HITS = 16;

    public static PlayerInteraction Instance { get; private set; }

    public event EventHandler<OnSelectedChangedEventArgs> OnSelectedChanged;
    public class OnSelectedChangedEventArgs : EventArgs { public IInteractable selected; }

    [SerializeField] private LayerMask interactableLayerMask;
    [SerializeField] private float reach = .7f;
    [SerializeField] private float radius = 1.3f;

    private Player player;
    private IInteractable selected;
    private readonly Collider[] hits = new Collider[MAX_HITS];
    private readonly Dictionary<Collider, IInteractable> interactableCache = new Dictionary<Collider, IInteractable>();

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one PlayerInteraction", this);
        Instance = this;
        player = GetComponent<Player>();
    }

    private void Start()
    {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    private void Update()
    {
        SetSelected(GameManager.Instance.IsGamePlaying() ? FindBest() : null);
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (!GameManager.Instance.IsGamePlaying()) return;
        if (selected == null || !selected.CanInteract(player)) return;
        selected.Interact(player);
    }

    // best = most in front of the player, then closest
    private IInteractable FindBest()
    {
        Vector3 centre = transform.position + transform.forward * reach + Vector3.up * .5f;
        int count = Physics.OverlapSphereNonAlloc(centre, radius, hits, interactableLayerMask, QueryTriggerInteraction.Collide);
        IInteractable best = null;
        float bestScore = float.MinValue;
        for (int i = 0; i < count; i++)
        {
            IInteractable interactable = GetInteractable(hits[i]);
            if (interactable == null || !interactable.IsSelectable()) continue;
            Vector3 toTarget = hits[i].transform.position - transform.position;
            toTarget.y = 0f;
            float score = Vector3.Dot(transform.forward, toTarget.normalized) - toTarget.magnitude * .35f;
            if (score <= bestScore) continue;
            bestScore = score;
            best = interactable;
        }
        return best;
    }

    private IInteractable GetInteractable(Collider hit)
    {
        if (!interactableCache.TryGetValue(hit, out IInteractable interactable))
        {
            hit.TryGetComponent(out interactable);
            interactableCache[hit] = interactable;
        }
        return interactable;
    }

    private void SetSelected(IInteractable interactable)
    {
        if (interactable == selected) return;
        selected = interactable;
        OnSelectedChanged?.Invoke(this, new OnSelectedChangedEventArgs { selected = selected });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
