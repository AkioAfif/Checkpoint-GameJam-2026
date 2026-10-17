using System;
using TMPro;
using UnityEngine;

// "Press [E] to collect" floating above whatever the player targets. The key text follows rebinds and the active device.
[RequireComponent(typeof(CanvasGroup))]
public class PromptUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private UIPop pop;
    [SerializeField] private string promptFormat = "Press <b>[{0}]</b> to {1}";
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 30f);

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private IInteractable target;
    private bool wasInteractable;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = (RectTransform)transform;
        Hide();
    }

    private void Start()
    {
        PlayerInteraction.Instance.OnSelectedChanged += PlayerInteraction_OnSelectedChanged;
        GameInput.Instance.OnBindingDisplayChanged += GameInput_OnBindingDisplayChanged;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        // the "blocked" text can flip to the key prompt while targeted (e.g. a bottle was just received)
        if (target.CanInteract(Player.Instance) != wasInteractable) Refresh();
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(target.GetPromptAnchor().position);
        rectTransform.position = (Vector2)screenPoint + screenOffset;
    }

    private void OnDestroy()
    {
        if (PlayerInteraction.Instance != null) PlayerInteraction.Instance.OnSelectedChanged -= PlayerInteraction_OnSelectedChanged;
        if (GameInput.Instance != null) GameInput.Instance.OnBindingDisplayChanged -= GameInput_OnBindingDisplayChanged;
    }

    private void PlayerInteraction_OnSelectedChanged(object sender, PlayerInteraction.OnSelectedChangedEventArgs e)
    {
        target = e.selected;
        if (target == null)
        {
            Hide();
            return;
        }
        Refresh();
        canvasGroup.SetVisible(true);
        pop.Play(.6f);
    }

    private void GameInput_OnBindingDisplayChanged(object sender, EventArgs e)
    {
        if (target != null) Refresh();
    }

    private void Refresh()
    {
        wasInteractable = target.CanInteract(Player.Instance);
        promptText.text = wasInteractable
            ? string.Format(promptFormat, GameInput.Instance.GetInteractBindingDisplay(), target.GetPromptVerb())
            : target.GetBlockedReason();
    }

    private void Hide() => canvasGroup.SetVisible(false);
}
