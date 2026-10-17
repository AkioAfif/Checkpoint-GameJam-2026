using UnityEngine;

public static class CanvasGroupExtensions
{
    // interactable + blocksRaycasts off too, so a hidden panel can't be clicked or reached by gamepad navigation
    public static void SetVisible(this CanvasGroup canvasGroup, bool isVisible)
    {
        canvasGroup.alpha = isVisible ? 1f : 0f;
        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;
    }
}
