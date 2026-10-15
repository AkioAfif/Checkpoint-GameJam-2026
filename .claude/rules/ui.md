---
paths:
  - "Assets/Scripts/UI/**/*.cs"
  - "Assets/Prefabs/UI/**"
---

# UI (uGUI + TextMeshPro)

## Scene setup (once per scene that has UI)
- Canvas: *Screen Space - Overlay*, CanvasScaler = *Scale With Screen Size*, reference 1920×1080, match 0.5, plus a `GraphicRaycaster` (buttons fail silently without it).
- **Exactly one EventSystem, with `InputSystemUIInputModule`** (namespace `UnityEngine.InputSystem.UI`). `StandaloneInputModule` throws errors because legacy input is disabled. Neither scene has an EventSystem yet.
- World-space UI (progress bars over objects) uses its own World Space Canvas on the prefab plus a `LookAtCamera` component.
- TMP Essential Resources must be imported before the first TMP object (see `project-settings.md`).
- `Raycast Target` off on every non-interactive Image/Text, so backgrounds and labels don't swallow clicks meant for buttons.

## Rules
- Each UI script controls **only its own panel**: it subscribes to logic events and calls getters. It never changes game state except through public manager methods (`GameManager.Instance.TogglePauseGame()`).
- Text uses `TextMeshProUGUI` (`using TMPro;`). Images, buttons and sliders use `using UnityEngine.UI;`.
- Wire buttons in `Awake` with lambdas: `resumeButton.onClick.AddListener(() => { GameManager.Instance.TogglePauseGame(); });`. Don't use Inspector OnClick.
- Show/Hide a panel with its **`CanvasGroup`** (`[RequireComponent(typeof(CanvasGroup))]` + `canvasGroup.SetVisible(bool)`, template below), **never `gameObject.SetActive`**. The GameObject stays active, so `Start` always runs and the panel always subscribes, even if someone saves it hidden.
- Call `Hide()` in `Awake` (it only touches its own CanvasGroup), then subscribe in `Start`. A public `Show()` that another script calls before this panel's `Start` (e.g. `optionsUI.Show(...)`) is then never undone by a late `Hide()`.
- A hidden panel's `Update` still runs. Keep panel `Update`s empty or cheap, and show panels from **event handlers**.
- `SetActive` stays fine for plain child objects without listening scripts (particles, icons, the list item template).
- On `Show()`, call `firstButton.Select()` for gamepad navigation.
- Panels that open another panel pass a callback: `optionsUI.Show(Show);` → `Show(Action onCloseButtonAction)` (KC pattern).
- Progress/timer bars: `Image` type *Filled*, set `fillAmount` (0–1) from an `IHasProgress` event or a `GetXNormalized()` getter.
- Unscaled time for anything that animates while paused.
- Subscriptions to singleton events must be removed in `OnDestroy` (see `csharp-pitfalls.md`).

## Template: show/hide helper (one file, shared by every panel)
`Assets/Scripts/UI/CanvasGroupExtensions.cs`:
```csharp
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
```

## Template: panel driven by game state
```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Button retryButton;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        retryButton.onClick.AddListener(() => { Loader.Load(Loader.Scene.GameScene); });
        Hide();
    }

    private void Start()
    {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= GameManager_OnStateChanged;
    }

    private void GameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (GameManager.Instance.IsGameOver()) Show();
        else Hide();
    }

    private void Show()
    {
        canvasGroup.SetVisible(true);
        retryButton.Select();
    }

    private void Hide()
    {
        canvasGroup.SetVisible(false);
    }
}
```

## Template: dynamic list (template child pattern)
```csharp
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private Transform itemTemplate; // child of container, has InventorySingleUI

    private void Awake()
    {
        itemTemplate.gameObject.SetActive(false);
    }

    private void UpdateVisual()
    {
        foreach (Transform child in container)
        {
            if (child == itemTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach (ItemSO itemSO in Inventory.Instance.GetItemSOList())
        {
            Transform itemTransform = Instantiate(itemTemplate, container);
            itemTransform.gameObject.SetActive(true);
            itemTransform.GetComponent<InventorySingleUI>().SetItemSO(itemSO);
        }
    }
}
```
`XSingleUI` exposes one `SetX(XSO so)` that fills its own text/image. The parent list never touches the row's children.

## Template: generic progress bar (any IHasProgress)
```csharp
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ProgressBarUI : MonoBehaviour
{
    [SerializeField] private GameObject hasProgressGameObject; // interfaces can't be serialized
    [SerializeField] private Image barImage;

    private CanvasGroup canvasGroup;
    private IHasProgress hasProgress;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        barImage.fillAmount = 0f;
        Hide();
    }

    private void Start()
    {
        if (!hasProgressGameObject.TryGetComponent(out hasProgress))
        {
            Debug.LogError($"{hasProgressGameObject.name} has no IHasProgress component", this);
            return;
        }
        hasProgress.OnProgressChanged += HasProgress_OnProgressChanged;
    }

    private void HasProgress_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e)
    {
        barImage.fillAmount = e.progressNormalized;
        if (e.progressNormalized <= 0f || e.progressNormalized >= 1f) Hide();
        else Show();
    }

    private void Show() => canvasGroup.SetVisible(true);
    private void Hide() => canvasGroup.SetVisible(false);
}
```
