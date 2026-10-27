#if DEVELOPER_TOOLS || UNITY_EDITOR
using UnityEngine;

// Skip and fast-forward for cutscenes and dialogue. Everything in the game already runs on Time.deltaTime, so a higher
// timeScale plays every sequence and its callbacks faster. A skip uses a very high one; dialogue lines are finished at once.
// Only cinematic states are sped up, so the rest of the game (movement, the Fabricator puzzle) keeps its normal speed.
public class DevCinematicControl
{
    private const float SKIP_SPEED = 40f;
    // a skip covers the chain of cutscenes and dialogue that follows; it ends after this much free play
    private const float SKIP_GRACE = 2f;

    private readonly DevToolsSettingsSO settings;
    private int speedIndex;
    private bool isSkipArmed;
    private float graceTimer;
    private bool isTimeScaleApplied;

    public DevCinematicControl(DevToolsSettingsSO settings)
    {
        this.settings = settings;
    }

    public bool IsCinematic()
    {
        GameManager.State state = GameManager.Instance.GetState();
        return state == GameManager.State.Cutscene || state == GameManager.State.Dialogue || state == GameManager.State.EnteringHouse;
    }

    public bool IsSkipArmed() => isSkipArmed;

    public float GetFastForwardSpeed() => settings.fastForwardSpeedArray[speedIndex];

    // returns the new speed
    public float CycleFastForward()
    {
        speedIndex = (speedIndex + 1) % settings.fastForwardSpeedArray.Length;
        return GetFastForwardSpeed();
    }

    public void StartSkip()
    {
        isSkipArmed = true;
        graceTimer = SKIP_GRACE;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive()) DialogueManager.Instance.SkipDialogue();
    }

    // called every frame with unscaled time, so the grace period does not depend on the speed
    public void Tick(float unscaledDeltaTime)
    {
        if (!isSkipArmed) return;
        if (IsCinematic()) graceTimer = SKIP_GRACE;
        else graceTimer -= unscaledDeltaTime;
        if (graceTimer <= 0f)
        {
            isSkipArmed = false;
            return;
        }
        // a dialogue that starts during the skip finishes at once, like the cutscene around it
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive()) DialogueManager.Instance.SkipDialogue();
    }

    // runs after the scene's own updates: GameManager owns the timeScale on state and pause changes, this only adds the speed on top
    public void ApplyTimeScale()
    {
        if (!IsCinematic() || GameManager.Instance.IsGamePaused()) return;
        float speed = isSkipArmed ? SKIP_SPEED : GetFastForwardSpeed();
        if (speed != 1f)
        {
            Time.timeScale = speed;
            isTimeScaleApplied = true;
        }
        else if (isTimeScaleApplied)
        {
            Time.timeScale = 1f;
            isTimeScaleApplied = false;
        }
    }

    public void Restore()
    {
        if (isTimeScaleApplied) Time.timeScale = 1f;
        isTimeScaleApplied = false;
        isSkipArmed = false;
    }
}
#endif
