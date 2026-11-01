using System;
using TMPro;
using UnityEngine;

// Fade to black, hold (optionally with a caption like "That evening..."), fade back. The story cuts while the screen is black.
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFadeUI : MonoBehaviour
{
    private const float MAX_FRAME_STEP = .1f;

    public event EventHandler OnBlack;
    public event EventHandler OnFinished;

    private enum State { Idle, FadingOut, Holding, FadingIn }

    [SerializeField] private TextMeshProUGUI captionText;
    [SerializeField] private float fadeDuration = .5f;

    private CanvasGroup canvasGroup;
    private State state;
    private float timer;
    private float holdDuration;
    // held one after the other on the black
    private string[] captionArray;
    private int captionIndex;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        if (state == State.Idle) return;
        // capped: a scene's first frames hitch while it loads, and a title card shown from black would be skipped
        timer += Mathf.Min(Time.unscaledDeltaTime, MAX_FRAME_STEP);
        switch (state)
        {
            case State.FadingOut:
                canvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
                if (timer < fadeDuration) break;
                SetState(State.Holding);
                OnBlack?.Invoke(this, EventArgs.Empty);
                break;
            case State.Holding:
                if (timer < holdDuration) break;
                captionIndex++;
                if (captionIndex < captionArray.Length)
                {
                    captionText.text = captionArray[captionIndex];
                    timer = 0f;
                }
                else SetState(State.FadingIn);
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
        StartCaptions(new[] { caption }, holdDuration);
        SetState(State.FadingOut);
    }

    // a chapter's title card: the scene opens already black (no OnBlack), holds each caption in turn, then fades in
    public void PlayFromBlack(string[] captionArray, float holdDuration)
    {
        StartCaptions(captionArray, holdDuration);
        canvasGroup.alpha = 1f;
        SetState(State.Holding);
    }

    private void StartCaptions(string[] captionArray, float holdDuration)
    {
        this.captionArray = captionArray;
        captionIndex = 0;
        captionText.text = captionArray[0];
        this.holdDuration = holdDuration;
        canvasGroup.blocksRaycasts = true;
    }

    private void SetState(State state)
    {
        this.state = state;
        timer = 0f;
    }
}
