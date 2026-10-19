using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GrayboxBuild;
using static UiBuilder;

// Day 1's extra HUD panels, added to the existing HUD canvas: the debt letter (HouseInterior), the Fabricator's
// ghost-cursor hint (Level01) and the screen fade (both).
public static class Day1UiBuilder
{
    private static readonly Color Paper = new Color(.98f, .95f, .86f);
    private static readonly Color Debt = new Color(.78f, .22f, .20f);

    public static ScreenFadeUI BuildVillage(Transform canvas, FabricatorMiniGame miniGame)
    {
        BuildHint(canvas, miniGame);
        return BuildFade(canvas); // last, so it draws over everything
    }

    public static ScreenFadeUI BuildBedroom(Transform canvas, BedsideLetter letter, Day1SO day1SO)
    {
        BuildLetter(canvas, letter, day1SO);
        return BuildFade(canvas);
    }

    private static void BuildLetter(Transform canvas, BedsideLetter letter, Day1SO day1SO)
    {
        RectTransform root = Rect(canvas, "LetterUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        Image dim = Img(root, "Dim", null, new Color(.15f, .12f, .12f, .55f), Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        dim.raycastTarget = true;
        RectTransform card = Panel(root, "Card", Centre, Centre, Centre, Vector2.zero, new Vector2(820f, 700f), Paper);
        RectTransform ribbon = Panel(card, "Ribbon", new Vector2(.5f, 1f), new Vector2(.5f, 1f), Centre, Vector2.zero, new Vector2(420f, 76f), Debt);
        TextMeshProUGUI title = Text(ribbon, "Title", "FINAL NOTICE", 40, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(title.rectTransform, Vector2.zero, Vector2.zero);
        TextMeshProUGUI body = Text(card, "Body", "Body", 32, Ink, TextAlignmentOptions.TopLeft);
        body.textWrappingMode = TextWrappingModes.Normal;
        Place(body.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -250f), new Vector2(700f, 280f));
        TextMeshProUGUI debt = Text(card, "Debt", "0 Koin", 76, Debt, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(debt.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, 190f), new Vector2(760f, 110f));
        TextMeshProUGUI hint = Text(card, "ContinueHint", "Press [E] to put it down", 28, Ink, TextAlignmentOptions.Center);
        Place(hint.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, 60f), new Vector2(700f, 44f));

        LetterUI ui = root.gameObject.AddComponent<LetterUI>();
        SetField(ui, "bedsideLetter", letter);
        SetField(ui, "day1SO", day1SO);
        SetField(ui, "titleText", title);
        SetField(ui, "bodyText", body);
        SetField(ui, "debtText", debt);
        SetField(ui, "continueText", hint);
        SetField(ui, "cardPop", card.gameObject.AddComponent<UIPop>());
    }

    // the ring is moved around by FabricatorHintUI, so it lives on its own full-screen layer
    private static void BuildHint(Transform canvas, FabricatorMiniGame miniGame)
    {
        RectTransform root = Rect(canvas, "FabricatorHintUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        Image ring = Img(root, "Ring", GrayboxTextures.Ring, new Color(1f, .93f, .55f, .95f), Centre, Centre, Centre, Vector2.zero, new Vector2(130f, 130f));
        Img(ring.rectTransform, "Dot", GrayboxTextures.Circle, new Color(1f, .93f, .55f, .5f), Centre, Centre, Centre, Vector2.zero, new Vector2(60f, 60f));
        FabricatorHintUI ui = root.gameObject.AddComponent<FabricatorHintUI>();
        SetField(ui, "miniGame", miniGame);
        SetField(ui, "ring", ring.rectTransform);
    }

    private static ScreenFadeUI BuildFade(Transform canvas)
    {
        Image black = Img(canvas, "ScreenFadeUI", null, Color.black, Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        black.gameObject.AddComponent<CanvasGroup>();
        TextMeshProUGUI caption = Text(black.rectTransform, "Caption", string.Empty, 84, Cream, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(caption.rectTransform, Vector2.zero, Vector2.zero);
        ScreenFadeUI fade = black.gameObject.AddComponent<ScreenFadeUI>();
        SetField(fade, "captionText", caption);
        return fade;
    }
}
