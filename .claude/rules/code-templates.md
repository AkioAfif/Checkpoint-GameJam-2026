---
paths:
  - "Assets/_Project/Scripts/**/*.cs"
---

# Code templates — copy the shape, rename, delete what you don't need

All templates follow `csharp-architecture.md` + `csharp-pitfalls.md` (domain reload off → static reset + `OnDestroy` unsubscribe).

## A. Global singleton manager with a state machine
```csharp
using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event EventHandler OnStateChanged;
    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnpaused;

    private enum State { WaitingToStart, CountdownToStart, GamePlaying, GameOver }

    [SerializeField] private float countdownToStartTimerMax = 3f;

    private State state;
    private float countdownToStartTimer;
    private bool isGamePaused;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one GameManager", this);
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        SetState(State.WaitingToStart);
    }

    private void Update()
    {
        switch (state)
        {
            case State.CountdownToStart:
                countdownToStartTimer -= Time.deltaTime;
                if (countdownToStartTimer < 0f) SetState(State.GamePlaying);
                break;
        }
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnPauseAction -= GameInput_OnPauseAction;
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e) => TogglePauseGame();

    public void StartCountdown()
    {
        countdownToStartTimer = countdownToStartTimerMax;
        SetState(State.CountdownToStart);
    }

    public void TogglePauseGame()
    {
        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0f : 1f;
        if (isGamePaused) OnGamePaused?.Invoke(this, EventArgs.Empty);
        else OnGameUnpaused?.Invoke(this, EventArgs.Empty);
    }

    public bool IsGamePlaying() => state == State.GamePlaying;
    public bool IsCountdownToStartActive() => state == State.CountdownToStart;
    public bool IsGameOver() => state == State.GameOver;
    public float GetCountdownToStartTimer() => countdownToStartTimer;

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
```

## B. Logic component + interface + static "any" event
```csharp
using System;

public interface IHasProgress
{
    event EventHandler<OnProgressChangedEventArgs> OnProgressChanged;
    public class OnProgressChangedEventArgs : EventArgs { public float progressNormalized; }
}
```
```csharp
using System;
using UnityEngine;

public class Checkpoint : MonoBehaviour, IHasProgress
{
    public static event EventHandler OnAnyCheckpointActivated;

    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }
    public enum State { Inactive, Activating, Active }

    [SerializeField] private float activateTimeMax = 1f;
    [SerializeField] private Transform respawnPoint;

    private State state;
    private float activateTimer;
    // raised every frame → reuse one args object instead of allocating garbage per frame
    private readonly IHasProgress.OnProgressChangedEventArgs progressChangedEventArgs = new IHasProgress.OnProgressChangedEventArgs();

    private void Update()
    {
        if (state != State.Activating) return;

        activateTimer += Time.deltaTime;
        SetProgress(activateTimer / activateTimeMax);
        if (activateTimer >= activateTimeMax)
        {
            SetState(State.Active);
            OnAnyCheckpointActivated?.Invoke(this, EventArgs.Empty);
        }
    }

    public void StartActivating()
    {
        if (state != State.Inactive) return;
        activateTimer = 0f;
        SetState(State.Activating);
    }

    public bool IsActive() => state == State.Active;
    public Vector3 GetRespawnPosition() => respawnPoint.position;

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }

    private void SetProgress(float progressNormalized)
    {
        progressChangedEventArgs.progressNormalized = progressNormalized;
        OnProgressChanged?.Invoke(this, progressChangedEventArgs);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStaticData() => OnAnyCheckpointActivated = null;
}
```

## C. Visual listener (same prefab as the logic → no unsubscribe needed)
```csharp
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CheckpointVisual : MonoBehaviour
{
    private const string IS_ACTIVE = "IsActive";

    [SerializeField] private Checkpoint checkpoint;
    [SerializeField] private GameObject activeParticlesGameObject;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        checkpoint.OnStateChanged += Checkpoint_OnStateChanged;
        activeParticlesGameObject.SetActive(false);
    }

    private void Checkpoint_OnStateChanged(object sender, Checkpoint.OnStateChangedEventArgs e)
    {
        bool isActive = e.state == Checkpoint.State.Active;
        animator.SetBool(IS_ACTIVE, isActive);
        activeParticlesGameObject.SetActive(isActive);
    }
}
```

## D. Global listener of static events (SoundManager) — must unsubscribe
```csharp
using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private const string PLAYER_PREFS_SOUND_EFFECTS_VOLUME = "SoundEffectsVolume";

    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioClipRefsSO audioClipRefsSO;

    private float volume = 1f;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one SoundManager", this);
        Instance = this;
        volume = PlayerPrefs.GetFloat(PLAYER_PREFS_SOUND_EFFECTS_VOLUME, 1f);
    }

    private void Start()
    {
        Checkpoint.OnAnyCheckpointActivated += Checkpoint_OnAnyCheckpointActivated;
    }

    private void OnDestroy()
    {
        Checkpoint.OnAnyCheckpointActivated -= Checkpoint_OnAnyCheckpointActivated;
    }

    private void Checkpoint_OnAnyCheckpointActivated(object sender, EventArgs e)
    {
        Checkpoint checkpoint = (Checkpoint)sender;
        PlaySound(audioClipRefsSO.checkpointArray, checkpoint.transform.position);
    }

    public void PlayFootstepSound(Vector3 position, float volumeMultiplier)
    {
        PlaySound(audioClipRefsSO.footstepArray, position, volumeMultiplier);
    }

    public void ChangeVolume()
    {
        volume += .1f;
        if (volume > 1f) volume = 0f;
        PlayerPrefs.SetFloat(PLAYER_PREFS_SOUND_EFFECTS_VOLUME, volume);
        PlayerPrefs.Save();
    }

    public float GetVolume() => volume;

    private void PlaySound(AudioClip[] audioClipArray, Vector3 position, float volumeMultiplier = 1f)
    {
        if (audioClipArray.Length == 0) return; // clips not assigned yet → stay silent instead of IndexOutOfRange
        PlaySound(audioClipArray[UnityEngine.Random.Range(0, audioClipArray.Length)], position, volumeMultiplier);
    }

    private void PlaySound(AudioClip audioClip, Vector3 position, float volumeMultiplier = 1f)
    {
        AudioSource.PlayClipAtPoint(audioClip, position, volumeMultiplier * volume);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
```

## E. Base class + override (shared logic lives in the base)
```csharp
using UnityEngine;

public abstract class BaseInteractable : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;

    public abstract void Interact(Player player);
    public virtual void InteractAlternate(Player player) { }

    public Transform GetHoldPoint() => holdPoint;

    // shared by several subclasses → lives here, not copy-pasted
    protected bool CanPlayerInteract(Player player) => GameManager.Instance.IsGamePlaying() && player != null;
}
```
```csharp
using UnityEngine;

public class Lever : BaseInteractable
{
    [SerializeField] private Door door;

    public override void Interact(Player player)
    {
        if (!CanPlayerInteract(player)) return;
        door.Toggle();
    }
}
```

## F. Static factory + DestroySelf
```csharp
using UnityEngine;

public class Pickup : MonoBehaviour
{
    [SerializeField] private PickupSO pickupSO;

    public PickupSO GetPickupSO() => pickupSO;

    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    public static Pickup SpawnPickup(PickupSO pickupSO, Vector3 position)
    {
        Transform pickupTransform = Instantiate(pickupSO.prefab, position, Quaternion.identity);
        return pickupTransform.GetComponent<Pickup>();
    }
}
```

## G. Loader (scene loading through a loading scene)
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
    // enum names must equal scene file names, and every scene must be in Build Profiles
    public enum Scene { MainMenuScene, LoadingScene, GameScene }

    private static Scene targetScene;

    public static void Load(Scene targetScene)
    {
        Loader.targetScene = targetScene;
        Time.timeScale = 1f;
        SceneManager.LoadScene(Scene.LoadingScene.ToString());
    }

    public static void LoaderCallback() => SceneManager.LoadScene(targetScene.ToString());
}
```
```csharp
using UnityEngine;

public class LoaderCallback : MonoBehaviour
{
    private bool isFirstUpdate = true;

    private void Update()
    {
        if (!isFirstUpdate) return;
        isFirstUpdate = false;
        Loader.LoaderCallback();
    }
}
```

UI templates → `ui.md`. GameInput → `input.md`. ScriptableObjects → `scriptable-objects.md`.
