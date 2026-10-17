using UnityEngine;

// Shows the inverted-hull outline while the player targets this interactable.
public class InteractableOutlineVisual : MonoBehaviour
{
    [SerializeField] private GameObject interactableGameObject; // interfaces can't be serialized
    [SerializeField] private GameObject[] outlineGameObjectArray;

    private IInteractable interactable;

    private void Start()
    {
        if (!interactableGameObject.TryGetComponent(out interactable))
        {
            Debug.LogError($"{interactableGameObject.name} has no IInteractable component", this);
            return;
        }
        PlayerInteraction.Instance.OnSelectedChanged += PlayerInteraction_OnSelectedChanged;
        SetOutlineVisible(false);
    }

    private void OnDestroy()
    {
        if (PlayerInteraction.Instance != null) PlayerInteraction.Instance.OnSelectedChanged -= PlayerInteraction_OnSelectedChanged;
    }

    private void PlayerInteraction_OnSelectedChanged(object sender, PlayerInteraction.OnSelectedChangedEventArgs e)
    {
        SetOutlineVisible(e.selected == interactable);
    }

    private void SetOutlineVisible(bool isVisible)
    {
        foreach (GameObject outline in outlineGameObjectArray) outline.SetActive(isVisible);
    }
}
