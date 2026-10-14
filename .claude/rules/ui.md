---
paths:
  - "Assets/Scripts/UI/**/*.cs"
  - "Assets/Prefabs/UI/**"
---

# UI (uGUI + TextMeshPro)

## Scene setup (once per scene that has UI)
- Canvas: *Screen Space - Overlay*, CanvasScaler = *Scale With Screen Size*, reference 1920×1080, match 0.5.
- **EventSystem with `InputSystemUIInputModule`** (namespace `UnityEngine.InputSystem.UI`). `StandaloneInputModule` throws errors because legacy input is disabled. Neither scene has an EventSystem yet.
- World-space UI (progress bars over objects) uses its own World Space Canvas on the prefab plus a `LookAtCamera` component.
- TMP Essential Resources must be imported before the first TMP object (see `project-settings.md`).

## Rules
- Each UI script controls **only its own panel**: it subscribes to logic events and calls getters. It never changes game state except through public manager methods (`GameManager.Instance.TogglePauseGame()`).
- Text uses `TextMeshProUGUI` (`using TMPro;`). Images, buttons and sliders use `using UnityEngine.UI;`.
- Wire buttons in `Awake` with lambdas: `resumeButton.onClick.AddListener(() => { GameManager.Instance.TogglePauseGame(); });`. Don't use Inspector OnClick.
- Show/Hide = private methods toggling `gameObject.SetActive`. The panel must be **active in the saved scene** and calls `Hide()` at the end of `Start`, because an inactive object never runs `Start` and never subscribes.
- C# event handlers still run while the panel is inactive, but its `Update` doesn't. So show panels from **event handlers**, never from the panel's own `Update`.
- On `Show()`, call `firstButton.Select()` for gamepad navigation.
- Panels that open another panel pass a callback: `optionsUI.Show(Show);` → `Show(Action onCloseButtonAction)` (KC pattern).
- Progress/timer bars: `Image` type *Filled*, set `fillAmount` (0–1) from an `IHasProgress` event or a `GetXNormalized()` getter.
- Unscaled time for anything that animates while paused.
- Subscriptions to singleton events must be removed in `OnDestroy` (see `csharp-pitfalls.md`).

## Template: panel driven by game state
```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Button retryButton;

    private void Awake()
    {
        retryButton.onClick.AddListener(() => { Loader.Load(Loader.Scene.GameScene); });
    }

    private void Start()
    {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        Hide();
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
        gameObject.SetActive(true);
        retryButton.Select();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
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

public class ProgressBarUI : MonoBehaviour
{
    [SerializeField] private GameObject hasProgressGameObject; // interfaces can't be serialized
    [SerializeField] private Image barImage;

    private IHasProgress hasProgress;

    private void Start()
    {
        if (!hasProgressGameObject.TryGetComponent(out hasProgress))
        {
            Debug.LogError($"{hasProgressGameObject.name} has no IHasProgress component", this);
            return;
        }
        hasProgress.OnProgressChanged += HasProgress_OnProgressChanged;
        barImage.fillAmount = 0f;
        Hide();
    }

    private void HasProgress_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e)
    {
        barImage.fillAmount = e.progressNormalized;
        if (e.progressNormalized <= 0f || e.progressNormalized >= 1f) Hide();
        else Show();
    }

    private void Show() => gameObject.SetActive(true);
    private void Hide() => gameObject.SetActive(false);
}
```
