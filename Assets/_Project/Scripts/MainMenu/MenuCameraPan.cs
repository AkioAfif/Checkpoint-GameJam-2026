using System;
using UnityEngine;

// Slides the menu's CinemachineCamera from the east point to the west point in real time (unaffected by the villagers'
// time-lapse), then fades to black, jumps back east and pans again.
public class MenuCameraPan : MonoBehaviour
{
    [SerializeField] private Transform eastPoint;
    [SerializeField] private Transform westPoint;
    [SerializeField] private float duration = 60f;
    [SerializeField] private ScreenFadeUI screenFadeUI;
    [SerializeField] private float fadeHoldDuration = .3f;

    private float timer;
    private bool isFading;

    private void Start()
    {
        if (screenFadeUI != null) screenFadeUI.OnBlack += ScreenFadeUI_OnBlack;
        Apply(0f);
    }

    private void Update()
    {
        if (isFading) return;
        timer += Time.unscaledDeltaTime;
        Apply(Mathf.Clamp01(timer / duration));
        if (timer < duration) return;
        if (screenFadeUI == null)
        {
            Restart();
            return;
        }
        isFading = true;
        screenFadeUI.Play("", fadeHoldDuration);
    }

    private void OnDestroy()
    {
        if (screenFadeUI != null) screenFadeUI.OnBlack -= ScreenFadeUI_OnBlack;
    }

    private void ScreenFadeUI_OnBlack(object sender, EventArgs e) => Restart();

    private void Restart()
    {
        isFading = false;
        timer = 0f;
        Apply(0f);
    }

    // linear: a steady drift (the fade hides the restart)
    private void Apply(float t)
    {
        transform.SetPositionAndRotation(
            Vector3.Lerp(eastPoint.position, westPoint.position, t),
            Quaternion.Slerp(eastPoint.rotation, westPoint.rotation, t));
    }
}
