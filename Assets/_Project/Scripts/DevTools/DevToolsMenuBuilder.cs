#if DEVELOPER_TOOLS || UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Creates the developer menu's uGUI objects in code, so the tools need no scene or prefab edits.
public static class DevToolsMenuBuilder
{
    private const float PANEL_WIDTH = 380f;
    private const float BUTTON_HEIGHT = 32f;
    private static readonly Color PANEL_COLOR = new Color(.06f, .06f, .08f, .92f);

    public static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasGameObject = CreateObject("DevToolsCanvas", parent, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGameObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGameObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        return canvas;
    }

    // top-left column; its height follows its content
    public static RectTransform CreatePanel(Transform canvasTransform)
    {
        GameObject panel = CreateObject("Panel", canvasTransform, typeof(Image), typeof(CanvasGroup), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.GetComponent<Image>().color = PANEL_COLOR;
        panel.GetComponent<Image>().raycastTarget = true;
        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(16f, -16f);
        rect.sizeDelta = new Vector2(PANEL_WIDTH, 0f);
        return rect;
    }

    // one-line banner at the top centre, shown for a moment after every action (also while the menu is closed)
    public static TextMeshProUGUI CreateToast(Transform canvasTransform, out CanvasGroup canvasGroup)
    {
        GameObject toast = CreateObject("Toast", canvasTransform, typeof(CanvasGroup));
        canvasGroup = toast.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        RectTransform rect = toast.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, 1f);
        rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -16f);
        rect.sizeDelta = new Vector2(900f, 40f);
        TextMeshProUGUI text = CreateText("ToastText", toast.transform, 26f, Color.white);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    public static TextMeshProUGUI CreateTitle(Transform parent, string title)
    {
        TextMeshProUGUI text = CreateText("Title", parent, 22f, Color.white);
        text.text = title;
        text.fontStyle = FontStyles.Bold;
        return text;
    }

    public static GameObject CreateColumn(string name, Transform parent)
    {
        GameObject column = CreateObject(name, parent, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        column.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return column;
    }

    // a button with its label, plus a small status line under it
    public static Button CreateButton(Transform parent, string labelText, out TextMeshProUGUI label, out TextMeshProUGUI status)
    {
        GameObject row = CreateColumn("Row", parent);
        GameObject buttonObject = CreateObject("Button", row.transform, typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.GetComponent<Image>().color = new Color(.22f, .24f, .3f, 1f);
        buttonObject.GetComponent<LayoutElement>().minHeight = BUTTON_HEIGHT;
        label = CreateText("Label", buttonObject.transform, 18f, Color.white);
        label.text = labelText;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 0f);
        labelRect.offsetMax = new Vector2(-10f, 0f);
        label.raycastTarget = false;
        status = CreateText("Status", row.transform, 14f, new Color(.7f, .75f, .8f));
        status.raycastTarget = false;
        return buttonObject.GetComponent<Button>();
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, float fontSize, Color color)
    {
        GameObject textObject = CreateObject(name, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    // every menu object is a RectTransform, so layout and anchors work on all of them
    private static GameObject CreateObject(string name, Transform parent, params System.Type[] componentTypes)
    {
        System.Type[] allTypes = new System.Type[componentTypes.Length + 1];
        allTypes[0] = typeof(RectTransform);
        componentTypes.CopyTo(allTypes, 1);
        GameObject gameObject = new GameObject(name, allTypes);
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }
}
#endif
