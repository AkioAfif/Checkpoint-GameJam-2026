using System;
using TMPro;
using UnityEngine;

// Fade to black, hold (optionally with a caption like "That evening..."), fade back. The story cuts while the screen is black.
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFadeUI : MonoBehaviour
{
    public event EventHandler OnBlack;
    public event EventHandler OnFinished;

    private enum State { Idle, FadingOut, Holding, FadingIn }

    [SerializeField] private TextMeshProUGUI captionText;
    [SerializeField] private float fadeDuration = .5f;

    private CanvasGroup canvasGroup;
    private State state;
    private float timer;
    private float holdDuration;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        if (state == State.Idle) return;
        timer += Time.unscaledDeltaTime;
        switch (state)
        {
            case State.FadingOut:
                canvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
                if (timer < fadeDuration) break;
                SetState(State.Holding);
                OnBlack?.Invoke(this, EventArgs.Empty);
                break;
            case State.Holding:
                if (timer >= holdDuration) SetState(State.FadingIn);
                break;
            case State.FadingIn:
                canvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
                if (timer < fadeDuration) break;
                SetState(State.Idle);
                canvasGroup.blocksRaycasts = false;
                OnFinished?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    // caption may be empty; OnBlack fires once the screen is fully black
    public void Play(string caption, float holdDuration)
    {
        captionText.text = caption;
        this.holdDuration = holdDuration;
        canvasGroup.blocksRaycasts = true;
        SetState(State.FadingOut);
    }

    private void SetState(State state)
    {
        this.state = state;
        timer = 0f;
    }
}
