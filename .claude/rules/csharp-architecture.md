---
paths:
  - "Assets/Scripts/**/*.cs"
---

# C# architecture (Kitchen Chaos style)

Small, single-purpose components that talk through C# events. Templates: `code-templates.md`. Error traps: `csharp-pitfalls.md`.

## 1. One class, one job
- Split each gameplay object into **logic**, **visual**, **sound** and **UI** components, each in its own file:
  `Checkpoint` (rules + state) → `CheckpointVisual` (flag anim/particles), `CheckpointSound`, `CheckpointUI`.
- Logic classes never reference `Animator`, `AudioSource`, `ParticleSystem`, `Renderer`/materials, or UI types.
- Visual/sound/UI classes contain no game rules. They read state through the logic class's public getters and events.
- When a class starts doing two things (rule of thumb: >150 lines, or two unrelated `[SerializeField]` groups), split it.

## 2. Events up, method calls down
- The logic object raises events. Listeners hold a `[SerializeField]` reference to it (or use its `Instance`) and subscribe.
- The publisher never knows its listeners. No `FindObjectOfType` and no "manager calls every visual".
- Use direct method calls only toward something you own or command (`player` → `checkpoint.Activate()`, UI button → `GameManager.Instance.TogglePause()`).
- Event signatures always use `EventHandler` / `EventHandler<TArgs>`. Never `Action`, `UnityEvent` or `delegate` for gameplay events. **Exception:** a one-shot callback parameter like `Show(Action onClosed)`.
- Payload goes in a nested `public class OnXEventArgs : EventArgs { public T field; }` declared in the publisher (or in the interface, for interface events).
- Raise an event from exactly **one** place: wrap state change + invoke in a private `SetState(...)`.
- Events raised every frame (progress, timers) reuse one cached `private readonly` args object and only update its field before invoking. Listeners read the args inside the handler and never store them.
- A listener that needs the publisher casts `sender` with a hard cast `(Checkpoint)sender`, never `as`: a wrong sender then fails on that line instead of becoming a null later.

## 3. Inheritance for shared behaviour
- Put common behaviour in a base class with `virtual` methods (`BaseInteractable.Interact(Player player)`). Subclasses `override` only what differs.
- Logic shared by 2+ subclasses moves **up** into the base as a `protected` helper. Never copy-paste between siblings.
- An empty base `virtual` method is fine when most subclasses don't need it (KC's `InteractAlternate`). Don't `Debug.LogError` in the base unless calling it really is a bug.
- Max depth: base → concrete. No 3-level hierarchies in a jam.

## 4. Interfaces for capabilities across unrelated classes
- When unrelated classes share a role, use an interface: `IHasProgress` (anything that drives a progress bar), `IDamageable`, `IInteractable`, `IKitchenObjectParent`-style "holder" interfaces.
- Generic UI and visuals depend on the interface, not a concrete class. Unity can't serialize interfaces, so serialize a `GameObject` and resolve it in `Start` with `TryGetComponent(out IHasProgress p)` plus a `Debug.LogError` if it's missing.
- Interface events: `event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;` with the args class nested inside the interface.

## 5. Data in ScriptableObjects
- Content definitions (items, enemies, levels, recipes, audio clip lists) and shared tuning values are ScriptableObjects. See `scriptable-objects.md`.
- MonoBehaviours reference SOs. They never hardcode content lists or names.
- SOs are read-only at runtime. Runtime state lives on the MonoBehaviour.

## 6. Objects own their own lifecycle
- Spawning uses a **static factory** on the class: `public static Pickup Spawn(PickupSO so, Vector3 pos)`.
- Removal is `DestroySelf()` on the object, which first unlinks itself from whoever holds it (`parent.ClearX()`), then `Destroy(gameObject)`.
- Parenting is two-sided: `SetParent(newParent)` clears the old holder, sets the new holder, and sets `transform.parent` + `localPosition`.
- Type checks go through `TryGetX(out X x)` (KC `TryGetPlate`), not `is`/`as` scattered around callers.

## 7. Singletons only for global systems
- Allowed: `GameManager`, `GameInput`, `SoundManager`, `MusicManager`, and objects that exist exactly once per scene (e.g. `Player`, `CheckpointManager`).
- Shape: `public static T Instance { get; private set; }`, assigned in `Awake` with a duplicate check, reset by `RuntimeInitializeOnLoadMethod` (domain reload is off).
- Not a generic `Singleton<T>` base class. Write the 3 lines per class (KC style, and easier to read).
- No `DontDestroyOnLoad` unless the user asks. Each scene has its own managers, and state is passed through a static class or PlayerPrefs (KC `Loader`).

## 8. Input only through `GameInput`
- `GameInput` is the **only** script that touches `UnityEngine.InputSystem`. Everyone else uses its events (`OnJumpAction`, `OnInteractAction`, `OnPauseAction`) and polling getters (`GetMovementVectorNormalized()`, `IsSprintPressed()`). See `input.md`.

## 9. Game flow = enum state machine
- `GameManager` holds `private enum State { WaitingToStart, CountdownToStart, GamePlaying, GameOver }` (adjust to the game), driven by `switch (state)` in `Update`.
- It exposes `IsGamePlaying()`, `IsGameOver()`, `GetXTimerNormalized()`, plus `OnStateChanged`, `OnGamePaused`, `OnGameUnpaused`.
- Gameplay input handlers start with `if (!GameManager.Instance.IsGamePlaying()) return;`.
- Other stateful objects (stove, enemy, door) use the same pattern: a public `enum State`, a private `state` field, `SetState`, and `OnStateChanged` carrying the state in its args.

## 10. Static "any instance" events
- Used when a global listener (usually `SoundManager`) cares about **every** instance: `public static event EventHandler OnAnyCheckpointReached;` with `sender` = the instance.
- Every class with static events or fields has a `ResetStaticData()` that nulls them, marked `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` (covers each Play session, since domain reload is off). Subclasses that add their own use `new public static void ResetStaticData()` with the same attribute.
- Scene reloads are covered because every listener unsubscribes in `OnDestroy`. So we **don't** use KC's `ResetStaticDataManager`.

## 11. Audio
- One-shot SFX: `SoundManager` subscribes to gameplay events (instance + static `OnAny…`) and plays clips from `AudioClipRefsSO` with `AudioSource.PlayClipAtPoint`. Gameplay code doesn't play one-shots itself. A component like `PlayerSound` may call public `SoundManager.Instance.PlayFootstepSound(pos)` methods.
- Looping or stateful per-object audio goes in that object's `XSound` component with its own `AudioSource` (KC `StoveCounterSound`).
- Music lives in `MusicManager`. Volumes persist in PlayerPrefs with `const string` keys.

## 12. Scene loading
- Use the static `Loader.Load(Loader.Scene.X)` with an enum. `SceneManager.LoadScene("string")` appears nowhere else.
- Reset `Time.timeScale = 1f` whenever a new scene starts (main menu `Awake`, or `Loader.Load`).

## Naming & layout
- PascalCase for classes, methods, events, enums and public members. camelCase for private fields and locals. No `m_` or `_` prefixes. `UPPER_SNAKE` for `const string`.
- Events are named `OnXxx` (past or present tense: `OnStateChanged`, `OnJumped`), static ones `OnAnyXxx`. Handler methods are named `Publisher_OnEvent` (`GameInput_OnJumpAction`).
- Accessors are methods, not properties: `GetX()`, `IsX()`, `HasX()`, `TryGetX(out T)`. (The only property is `Instance`.)
- Class name = file name, one top-level class per file. Visual/sound/UI suffixes: `XVisual`, `XSound`, `XUI`, `XSingleUI` (one row of a list).
- Arrays named `xArray`, lists `xList`, SO fields `xSO`, SO arrays `xSOArray`.
- Folders: `Assets/Scripts/Core/` (GameManager, GameInput, Loader, LoaderCallback, SoundManager, MusicManager), `Assets/Scripts/Player/`, `Assets/Scripts/<Feature>/` (logic + its Visual/Sound together), `Assets/Scripts/UI/`, `Assets/Scripts/ScriptableObjects/` (SO class definitions), `Assets/Scripts/Interfaces/`.
- Global namespace (KC style). Before creating a class, grep the project so its name doesn't collide with an existing class, Synty (`Generic_*`) or a Unity type (`Camera`, `Light`, `Input`, `Random`, `Object`, `Debug`, `Time`, `Physics`, `Collider`, `Animation`).

## Member order inside a class
1. `const` fields
2. `public static` Instance / static events
3. instance events + nested EventArgs classes + public enums
4. `[SerializeField] private` fields
5. private state fields
6. Unity messages in order: `Awake`, `OnEnable`, `Start`, `Update`, `FixedUpdate`, `LateUpdate`, `OnDisable`, `OnDestroy`
7. event handlers
8. public methods
9. private helpers
10. `ResetStaticData` (if any)

## Lean code (game jam)
- Build the smallest version that works. Skip object pooling, DI, generic event buses, save systems, addressables, async/await and service locators until a real need appears.
- No dead code, no commented-out blocks, no `// TODO later` scaffolding.
- Comments only for *why* (KC-style intent comments such as `// player is carrying a plate` are OK for branchy `Interact` logic).
