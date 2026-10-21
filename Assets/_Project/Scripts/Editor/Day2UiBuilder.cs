using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GrayboxBuild;
using static UiBuilder;

// Day 2's extra HUD panels, added to the existing HUD canvas under the screen fade: the savings book (both scenes) and
// the "+X Koin" sale popup (Level01).
public static class Day2UiBuilder
{
    private static readonly Color Paper = new Color(.98f, .95f, .86f);
    private static readonly Color BookGreen = new Color(.30f, .55f, .40f);

    public static void RemovePrevious()
    {
        foreach (SavingsBookUI ui in Object.FindObjectsByType<SavingsBookUI>(FindObjectsInactive.Include)) Object.DestroyImmediate(ui.gameObject);
        foreach (MoneyPopupUI ui in Object.FindObjectsByType<MoneyPopupUI>(FindObjectsInactive.Include)) Object.DestroyImmediate(ui.gameObject);
    }

    public static SavingsBookUI BuildSavingsBook(Transform canvas, Day2SO day2SO)
    {
        RectTransform root = Rect(canvas, "SavingsBookUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        Image dim = Img(root, "Dim", null, new Color(.12f, .14f, .12f, .55f), Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        dim.raycastTarget = true;
        RectTransform card = Panel(root, "Card", Centre, Centre, Centre, Vector2.zero, new Vector2(900f, 560f), Paper);
        RectTransform ribbon = Panel(card, "Ribbon", new Vector2(.5f, 1f), new Vector2(.5f, 1f), Centre, Vector2.zero, new Vector2(460f, 76f), BookGreen);
        TextMeshProUGUI title = Text(ribbon, "Title", "SAVINGS BOOK", 40, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(title.rectTransform, Vector2.zero, Vector2.zero);
        TextMeshProUGUI body = Text(card, "Body", "Body", 40, Ink, TextAlignmentOptions.TopLeft);
        body.textWrappingMode = TextWrappingModes.NoWrap;
        body.lineSpacing = 30f;
        Place(body.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -300f), new Vector2(760f, 340f));
        TextMeshProUGUI hint = Text(card, "ContinueHint", "Press [E] to close the book", 28, Ink, TextAlignmentOptions.Center);
        Place(hint.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, 60f), new Vector2(700f, 44f));
        PutUnderFade(root);

        SavingsBookUI ui = root.gameObject.AddComponent<SavingsBookUI>();
        SetField(ui, "day2SO", day2SO);
        SetField(ui, "titleText", title);
        SetField(ui, "bodyText", body);
        SetField(ui, "continueText", hint);
        SetField(ui, "cardPop", card.gameObject.AddComponent<UIPop>());
        return ui;
    }

    public static void BuildMoneyPopup(Transform canvas)
    {
        RectTransform root = Rect(canvas, "MoneyPopupUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        TextMeshProUGUI label = Text(root, "Amount", "+0 Koin", 64, new Color(.55f, .9f, .55f), TextAlignmentOptions.Center, FontStyles.Bold);
        Place(label.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -170f), new Vector2(900f, 90f));
        label.outlineWidth = .22f;
        label.outlineColor = new Color32(40, 60, 40, 255);
        PutUnderFade(root);
        MoneyPopupUI ui = root.gameObject.AddComponent<MoneyPopupUI>();
        SetField(ui, "amountText", label);
        SetField(ui, "amountPop", label.gameObject.AddComponent<UIPop>());
    }

    // the fade must stay the last child so it draws over everything
    private static void PutUnderFade(RectTransform panel)
    {
        ScreenFadeUI fade = Object.FindAnyObjectByType<ScreenFadeUI>(FindObjectsInactive.Include);
        if (fade != null && fade.transform.parent == panel.parent) panel.SetSiblingIndex(fade.transform.GetSiblingIndex());
    }
}
