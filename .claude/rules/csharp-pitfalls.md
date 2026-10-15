---
paths:
  - "Assets/**/*.cs"
---

# C# / Unity error traps — check every item before you finish a .cs change

## Compile errors
- **File name = class name** for every `MonoBehaviour`/`ScriptableObject`, one per file. Otherwise you get "The associated script can not be loaded" / "No MonoBehaviour scripts in the file".
- **No `using UnityEditor;` in runtime scripts.** It compiles in the editor but **breaks the build**. Editor-only code goes in an `Editor/` folder or inside `#if UNITY_EDITOR … #endif`. KC had this bug in 3 files.
- Remove unused `using`s. Never leave IDE auto-imports like `using NUnit.Framework;`, `using UnityEngine.EventSystems;`, `using Unity.VisualScripting;`, `using static X;` or `using System.Security.Cryptography...` (all appear in KC by accident). NUnit doesn't exist in player builds.
- `using System;` + `using UnityEngine;` makes **`Random` and `Object` ambiguous**. Write `UnityEngine.Random.Range(...)`, `UnityEngine.Object`, or add `using Random = UnityEngine.Random;`.
- `EventHandler` / `EventArgs` need `using System;`. `List<T>` needs `using System.Collections.Generic;`. TMP types need `using TMPro;`. `Image`/`Button`/`Slider` need `using UnityEngine.UI;`. Scene loading needs `using UnityEngine.SceneManagement;`. Input needs `using UnityEngine.InputSystem;` (only in `GameInput`).
- C# 9 only: no `required`, no file-scoped `namespace X;`, no primary constructors, no collection expressions `[1, 2]`, no raw string literals `"""`.
- Integer division: `(float)current / max`, not `current / max`.
- Overriding needs `virtual`/`abstract` on the base and `override` on the child. Hiding a static method needs `new` (`new public static void ResetStaticData()`).

## Unity 6 API changes (old names are obsolete → warnings or errors)
- Rigidbody: `linearVelocity` (not `velocity`), `linearDamping` (not `drag`), `angularDamping` (not `angularDrag`).
- `Object.GetInstanceID()` is a **compile error** in 6000.6 (CS0619, verified). Use `GetEntityId()`, or better, compare the object references directly.
- Don't use `FindObjectOfType` / `FindObjectsOfType` (obsolete). Prefer serialized refs or `Instance`. If you really must search, use `FindAnyObjectByType<T>()`.
- Input: `UnityEngine.Input.*` **throws** in this project (Input System only). Go through `GameInput`.
- UI EventSystem: must use **`InputSystemUIInputModule`**, not `StandaloneInputModule`, which throws every frame.
- Text: TextMeshPro only (`TextMeshProUGUI` on a Canvas, `TextMeshPro` in world space). Never the legacy `UnityEngine.UI.Text`.

## Unity null checks
- `UnityEngine.Object` overrides `==`. **Never use `?.`, `??`, `??=`, `is null` or `is not null` on Unity objects** (GameObject, Component, MonoBehaviour, ScriptableObject). They skip Unity's destroyed-object check, so you get `MissingReferenceException` (KC had `gameObject?.SetActive`). Use `if (x != null)` or `if (x)`.
- `?.Invoke` on **events/delegates** is correct and required: `OnX?.Invoke(this, EventArgs.Empty);`.

## Domain reload is OFF (static state survives between Play sessions)
- Every `static` field, static event, `Instance` and static cache must be reset:
  ```csharp
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  public static void ResetStaticData() { OnAnyX = null; }
  ```
- `Instance` is reassigned in `Awake`. The duplicate check `if (Instance != null) Debug.LogError(...)` still works because a destroyed object compares `== null`.
- Subscribers to **static events** or **events on objects that outlive them** (singletons, the player) must unsubscribe in `OnDestroy`. Otherwise handlers pile up and fire on destroyed objects:
  ```csharp
  private void OnDestroy() { GameManager.Instance.OnStateChanged -= GameManager_OnStateChanged; }
  ```
  When the publisher may already be destroyed during scene unload, guard with `if (GameManager.Instance != null)`.
- Subscribing to a component **on the same GameObject or a child** doesn't need an unsubscribe, because they die together.
- Static `Instance` of singletons: also `Instance = null;` inside `ResetStaticData`.

## Initialization order
- `Awake`: own references only (`GetComponent`, `Instance = this`, `new List<>()`, button listeners).
- `Start`: subscribe to other objects' events and read other singletons. All `Awake`s have run by then.
- `GetComponent<T>()` in `Awake` → add `[RequireComponent(typeof(T))]` on the class so it can't be null.
- **Inactive GameObjects never run `Awake`/`Start`.** A UI panel that should start hidden must be **active in the scene** and call `Hide()` at the end of its own `Start` (KC pattern). Never save it disabled in the scene, or it will never subscribe to its events.
- Never rely on script execution order between two `Start`s. If needed, move the dependency into `Awake`.

## Serialization (Inspector values)
- Serialize with `[SerializeField] private`. Public fields are only for SO data classes and `[Serializable]` structs.
- Not serialized: properties (unless `[field: SerializeField]`), `static`, `readonly`, `const`, `Dictionary`, interfaces, multidimensional arrays, nested `List<List<T>>`. Use a `[Serializable] struct` list instead of a dictionary (KC `PlateCompleteVisual.KitchenObjectSO_GameObject`).
- Renaming a serialized field **wipes its Inspector value**. Add `[FormerlySerializedAs("oldName")]` (`using UnityEngine.Serialization;`) or re-wire it through Unity MCP.
- A field initializer (`= 5f`) is only the default for **new** components. Instances already in a scene/prefab keep their serialized value.
- After adding a new `[SerializeField]` reference, **assign it** in the scene/prefab (Unity MCP `set_serialized_field`) or validate in `Awake`: `if (x == null) Debug.LogError($"{name}: x not assigned", this);`.

## Physics
- Read input in `Update`, apply Rigidbody forces/velocity in `FixedUpdate`, move a `CharacterController` in `Update` with `Time.deltaTime`.
- **Teleporting a CharacterController** (checkpoint respawn!): `controller.enabled = false; transform.position = p; controller.enabled = true;`. Otherwise the move is overwritten on the next frame (`autoSyncTransforms` is off).
- Teleporting a Rigidbody: set `rb.position` plus `transform.position`, and zero `rb.linearVelocity`.
- `OnTriggerEnter` / `OnCollisionEnter` need a Rigidbody (or CharacterController) on at least one side. Triggers need `isTrigger = true` on the collider. Use the 3D versions, never `OnTriggerEnter2D`.
- Raycasts/casts against our objects pass an explicit `LayerMask` (serialized). The layer must exist first (see `project-settings.md`).
- Compare tags with `CompareTag("X")`, never `tag == "X"`. The tag must exist.
- Don't put a Rigidbody and a CharacterController on the same object.

## Time & pause
- Pause = `Time.timeScale = 0f`. `Update` still runs but `Time.deltaTime` is 0. Pause-menu animations use `Time.unscaledDeltaTime` and set Animator *Update Mode = Unscaled Time*.
- Reset `Time.timeScale = 1f` on every scene start.
- Coroutines stop when their GameObject is disabled. Prefer timer floats in `Update` (KC style) over coroutines for gameplay timers.

## Misc runtime
- Cache `GetComponent` results in `Awake`. Never call it every frame.
- `Camera.main` is fine to call (Unity caches it), but the camera must be tagged `MainCamera`.
- `Destroy(gameObject)`, never `DestroyImmediate` at runtime.
- Animator parameter names in `const string` must match the controller **exactly** (case-sensitive). A mismatch only logs a warning.
- Strings shown to the player: none hardcoded in logic. They go through UI or SO fields.
