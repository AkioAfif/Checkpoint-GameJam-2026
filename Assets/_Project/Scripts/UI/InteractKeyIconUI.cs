using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The drawn key cap while Interact is bound to the key it shows; any other binding (a rebind, a gamepad) falls back to
// the plain cap with the key name as text.
public class InteractKeyIconUI : MonoBehaviour
{
    [SerializeField] private Image keyImage;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private Sprite drawnKeySprite;
    [SerializeField] private Sprite plainKeySprite;
    [SerializeField] private string drawnKey = "E";

    private void Start()
    {
        GameInput.Instance.OnBindingDisplayChanged += GameInput_OnBindingDisplayChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnBindingDisplayChanged -= GameInput_OnBindingDisplayChanged;
    }

    private void GameInput_OnBindingDisplayChanged(object sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        bool isDrawn = GameInput.Instance.GetInteractBindingDisplay() == drawnKey;
        keyImage.sprite = isDrawn ? drawnKeySprite : plainKeySprite;
        keyText.enabled = !isDrawn;
    }
}
