using System;
using UnityEngine;

// "COMBO!" pops up over the puzzle when every liquid reached the mould at once (the label's text is set in the scene).
[RequireComponent(typeof(CanvasGroup))]
public class FabricatorComboUI : MonoBehaviour
{
    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private UIPop labelPop;
    [SerializeField] private float showDuration = 1.5f;

    private CanvasGroup canvasGroup;
    private float timer = -1f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.SetVisible(false);
    }

    private void Start()
    {
        miniGame.OnSolved += MiniGame_OnSolved;
    }

    private void Update()
    {
        if (timer < 0f) return;
        timer -= Time.deltaTime;
        if (timer < 0f) canvasGroup.SetVisible(false);
    }

    private void MiniGame_OnSolved(object sender, EventArgs e)
    {
        if (!miniGame.IsCombo()) return;
        canvasGroup.SetVisible(true);
        labelPop.Play(0f);
        timer = showDuration;
    }
}
