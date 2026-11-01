using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static GrayboxBuild;

// Builds the level HUD canvas (1920x1080 reference) and wires every UI script.
public static class UiBuilder
{
    public static readonly Color Cream = new Color(1f, .97f, .91f, .97f);
    public static readonly Color Ink = new Color(.30f, .25f, .23f);
    public static readonly Color Accent = new Color(.98f, .80f, .42f);
    public static readonly Color Sky = new Color(.52f, .67f, .88f);

    public struct Refs
    {
        public FabricatorController fabricator;
        public FabricatorMiniGame miniGame;
        public HouseDoor houseDoor;
        public RenderTexture playerPortrait;
        public RenderTexture npcPortrait;
    }

    public static void BuildEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    public static Transform Canvas(string name, int sortOrder = 0)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        go.AddComponent<GraphicRaycaster>();
        return go.transform;
    }

    public static void BuildLevelHud(Refs refs)
    {
        BuildEventSystem();
        Transform canvas = Canvas("HUD");
        RectTransform hud = Rect(canvas, "Hud", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        hud.gameObject.AddComponent<CanvasGroup>();
        hud.gameObject.AddComponent<HudUI>();
        BuildTasks(hud);
        BuildInventory(hud);
        BuildPrompt(canvas);
        BuildQte(canvas);
        BuildSeedChoice(canvas);
        // scenes without a Fabricator or a house door (HouseInterior) skip the panels that listen to them
        if (refs.fabricator != null) FabricatorGridPuzzleBuilder.BuildHud(canvas, refs.fabricator, refs.miniGame);
        BuildDialogue(canvas, refs);
        BuildDiscovery(canvas);
        BuildPause(canvas);
        if (refs.fabricator != null) BuildFlash(canvas, refs);
        if (refs.houseDoor != null) BuildIris(canvas, refs);
    }

    // ---------- panels ----------

    private static void BuildTasks(Transform canvas)
    {
        RectTransform panel = Panel(canvas, "TaskUI", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(610f, 0f), Cream);
        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 20, 22);
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        TextMeshProUGUI header = Text(panel, "Header", "Quest", 34, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        UIPop headerPop = header.gameObject.AddComponent<UIPop>();
        TextMeshProUGUI debt = Text(panel, "Debt", "Remaining debt: 0 Koin", 24, Ink, TextAlignmentOptions.Left);

        RectTransform row = Rect(panel, "RowTemplate", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(.5f, 1f), Vector2.zero, new Vector2(0f, 40f));
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        Image checkbox = Img(row, "Checkbox", GrayboxTextures.Circle, Color.white, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(0f, 0f), new Vector2(30f, 30f));
        Outline(checkbox.gameObject);
        Image check = Img(checkbox.rectTransform, "Check", GrayboxTextures.Circle, Ink, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(14f, 14f));
        TextMeshProUGUI description = Text(row, "Description", "Task", 26, Ink, TextAlignmentOptions.Left);
        Stretch(description.rectTransform, new Vector2(44f, 0f), new Vector2(-64f, 0f));
        TextMeshProUGUI count = Text(row, "Count", "0/3", 26, Ink, TextAlignmentOptions.Right);
        Stretch(count.rectTransform, new Vector2(470f, 0f), Vector2.zero);
        TaskSingleUI single = row.gameObject.AddComponent<TaskSingleUI>();
        SetField(single, "descriptionText", description);
        SetField(single, "countText", count);
        SetField(single, "checkboxImage", checkbox);
        SetField(single, "checkmark", check.gameObject);
        SetField(single, "pop", row.gameObject.AddComponent<UIPop>());

        TaskUI taskUI = panel.gameObject.AddComponent<TaskUI>();
        SetField(taskUI, "container", panel);
        SetField(taskUI, "rowTemplate", single);
        SetField(taskUI, "headerPop", headerPop);
        SetField(taskUI, "debtText", debt);
    }

    private static void BuildInventory(Transform canvas)
    {
        RectTransform strip = Rect(canvas, "InventoryUI", new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 28f), new Vector2(900f, 112f));
        HorizontalLayoutGroup layout = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        // as wide as its slots around the centred pivot: more items grow it to both sides evenly
        strip.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform slot = Panel(strip, "SlotTemplate", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(104f, 104f), Cream);
        Image icon = Img(slot, "Icon", null, Color.white, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 4f), new Vector2(84f, 84f));
        icon.preserveAspect = true;
        RectTransform badge = Panel(slot, "Badge", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(.5f, .5f), new Vector2(-14f, 14f), new Vector2(46f, 36f), Accent);
        TextMeshProUGUI count = Text(badge, "Count", "1", 24, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(count.rectTransform, Vector2.zero, Vector2.zero);
        InventorySingleUI single = slot.gameObject.AddComponent<InventorySingleUI>();
        SetField(single, "iconImage", icon);
        SetField(single, "countText", count);
        SetField(single, "pop", slot.gameObject.AddComponent<UIPop>());

        InventoryUI inventoryUI = strip.gameObject.AddComponent<InventoryUI>();
        SetField(inventoryUI, "container", strip);
        SetField(inventoryUI, "slotTemplate", single);
    }

    // above the hotbar while the player picks which seed to plant
    public static void BuildSeedChoice(Transform canvas)
    {
        RectTransform panel = Panel(canvas, "SeedChoiceUI", new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 170f), new Vector2(560f, 300f), Cream);
        TextMeshProUGUI title = Text(panel, "Title", "Plant which seed?", 34, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(title.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -40f), new Vector2(500f, 50f));
        Image icon = Img(panel, "SeedIcon", null, Color.white, new Vector2(.5f, 1f), new Vector2(.5f, 1f), Centre, new Vector2(-140f, -118f), new Vector2(72f, 72f));
        icon.preserveAspect = true;
        TextMeshProUGUI seedName = Text(panel, "SeedName", "Sawit Seed", 30, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        Place(seedName.rectTransform, new Vector2(.5f, 1f), new Vector2(50f, -102f), new Vector2(300f, 44f));
        TextMeshProUGUI seedCount = Text(panel, "SeedCount", "You have 1   (1/2)", 24, Ink, TextAlignmentOptions.Left);
        Place(seedCount.rectTransform, new Vector2(.5f, 1f), new Vector2(50f, -140f), new Vector2(300f, 34f));
        Button previous = Button(panel, "PreviousSeedButton", "<", new Vector2(-235f, 157f), new Vector2(56f, 56f));
        Button next = Button(panel, "NextSeedButton", ">", new Vector2(235f, 157f), new Vector2(56f, 56f));
        Button plant = Button(panel, "PlantButton", "Plant", new Vector2(-100f, 52f), new Vector2(180f, 60f));
        Button cancel = Button(panel, "CancelButton", "Cancel", new Vector2(100f, 52f), new Vector2(180f, 60f));
        TextMeshProUGUI hint = Text(panel, "Hint", "A / D  seed    E  plant    Esc  cancel", 18, Ink, TextAlignmentOptions.Center);
        Place(hint.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, 24f), new Vector2(520f, 28f));
        SeedChoiceUI seedChoiceUI = panel.gameObject.AddComponent<SeedChoiceUI>();
        SetField(seedChoiceUI, "seedIconImage", icon);
        SetField(seedChoiceUI, "seedNameText", seedName);
        SetField(seedChoiceUI, "seedCountText", seedCount);
        SetField(seedChoiceUI, "previousSeedButton", previous);
        SetField(seedChoiceUI, "nextSeedButton", next);
        SetField(seedChoiceUI, "plantButton", plant);
        SetField(seedChoiceUI, "cancelButton", cancel);
    }

    private static void BuildPrompt(Transform canvas)
    {
        RectTransform prompt = Panel(canvas, "PromptUI", Vector2.zero, Vector2.zero, new Vector2(.5f, 0f), Vector2.zero, new Vector2(0f, 56f), Cream);
        prompt.gameObject.AddComponent<CanvasGroup>();
        HorizontalLayoutGroup layout = prompt.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        prompt.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        Outline(prompt.gameObject);
        TextMeshProUGUI text = Text(prompt, "Text", "Press [E] to collect", 28, Ink, TextAlignmentOptions.Center);
        PromptUI promptUI = prompt.gameObject.AddComponent<PromptUI>();
        SetField(promptUI, "promptText", text);
        SetField(promptUI, "pop", prompt.gameObject.AddComponent<UIPop>());
    }

    private static void BuildQte(Transform canvas)
    {
        RectTransform root = Rect(canvas, "QTEUI", Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        RectTransform ring = Rect(root, "RingGroup", Vector2.zero, Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, new Vector2(10f, 10f));
        Image hitZone = Img(ring, "HitZone", GrayboxTextures.Circle, new Color(1f, 1f, 1f, .22f), Centre, Centre, Centre, Vector2.zero, new Vector2(170f, 170f));
        Image perfectZone = Img(ring, "PerfectZone", GrayboxTextures.Ring, new Color(1f, .82f, .3f, .9f), Centre, Centre, Centre, Vector2.zero, new Vector2(150f, 150f));
        Img(ring, "Target", GrayboxTextures.Ring, Cream, Centre, Centre, Centre, Vector2.zero, new Vector2(140f, 140f));
        Image shrinking = Img(ring, "ShrinkingRing", GrayboxTextures.Ring, Color.white, Centre, Centre, Centre, Vector2.zero, new Vector2(420f, 420f));
        Image keyBg = Img(ring, "KeyBg", GrayboxTextures.Circle, Cream, Centre, Centre, Centre, Vector2.zero, new Vector2(70f, 70f));
        TextMeshProUGUI key = Text(keyBg.rectTransform, "Key", "E", 38, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(key.rectTransform, Vector2.zero, Vector2.zero);
        TextMeshProUGUI feedback = Text(ring, "Feedback", "", 44, Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(feedback.rectTransform, Centre, new Vector2(0f, 205f), new Vector2(400f, 60f));
        feedback.outlineWidth = .2f;
        feedback.outlineColor = new Color32(70, 55, 50, 255);
        RectTransform pips = Rect(ring, "Pips", Centre, Centre, Centre, new Vector2(0f, -130f), new Vector2(300f, 30f));
        HorizontalLayoutGroup pipLayout = pips.gameObject.AddComponent<HorizontalLayoutGroup>();
        pipLayout.spacing = 12f;
        pipLayout.childAlignment = TextAnchor.MiddleCenter;
        pipLayout.childControlWidth = false;
        pipLayout.childControlHeight = false;
        pipLayout.childForceExpandWidth = false;
        Image[] pipArray = new Image[5];
        for (int i = 0; i < pipArray.Length; i++)
        {
            pipArray[i] = Img(pips, "Pip", GrayboxTextures.Circle, Color.white, Centre, Centre, Centre, Vector2.zero, new Vector2(26f, 26f));
            Outline(pipArray[i].gameObject);
        }

        RectTransform banner = Rect(canvas, "QTEBanner", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 260f), new Vector2(900f, 120f));
        CanvasGroup bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.blocksRaycasts = false;
        TextMeshProUGUI bannerText = Text(banner, "Text", "Planted!", 84, Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(bannerText.rectTransform, Vector2.zero, Vector2.zero);
        bannerText.outlineWidth = .22f;
        bannerText.outlineColor = new Color32(70, 55, 50, 255);

        QTEUI qteUI = root.gameObject.AddComponent<QTEUI>();
        SetField(qteUI, "ringGroup", ring);
        SetField(qteUI, "shrinkingRing", shrinking.rectTransform);
        SetField(qteUI, "shrinkingRingImage", shrinking);
        SetField(qteUI, "hitZone", hitZone.rectTransform);
        SetField(qteUI, "perfectZone", perfectZone.rectTransform);
        SetField(qteUI, "keyText", key);
        SetField(qteUI, "feedbackText", feedback);
        SetField(qteUI, "feedbackPop", feedback.gameObject.AddComponent<UIPop>());
        SetArray(qteUI, "pipImageArray", pipArray);
        SetField(qteUI, "bannerText", bannerText);
        SetField(qteUI, "bannerPop", banner.gameObject.AddComponent<UIPop>());
        SetField(qteUI, "bannerCanvasGroup", bannerGroup);
    }

    private static void BuildDialogue(Transform canvas, Refs refs)
    {
        RectTransform root = Rect(canvas, "DialogueUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        RectTransform box = Panel(root, "Box", new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 40f), new Vector2(1320f, 270f), Cream);
        Outline(box.gameObject);
        RectTransform playerFrame = Portrait(box, "PlayerPortrait", refs.playerPortrait, new Vector2(0f, 1f), new Vector2(150f, 10f));
        RectTransform npcFrame = Portrait(box, "NpcPortrait", refs.npcPortrait, new Vector2(1f, 1f), new Vector2(-150f, 10f));

        RectTransform nameTag = Panel(box, "NameTag", new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, 0f), new Vector2(300f, 56f), Sky);
        TextMeshProUGUI name = Text(nameTag, "Name", "Name", 30, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(name.rectTransform, Vector2.zero, Vector2.zero);
        TextMeshProUGUI line = Text(box, "Line", "Hello!", 34, Ink, TextAlignmentOptions.TopLeft);
        Stretch(line.rectTransform, new Vector2(320f, 30f), new Vector2(-320f, -50f));
        line.textWrappingMode = TextWrappingModes.Normal;
        Image arrow = Img(box, "Continue", GrayboxTextures.Circle, Accent, new Vector2(1f, 0f), new Vector2(1f, 0f), Centre, new Vector2(-330f, 40f), new Vector2(22f, 22f));

        DialogueUI ui = root.gameObject.AddComponent<DialogueUI>();
        SetField(ui, "lineText", line);
        SetField(ui, "nameText", name);
        SetField(ui, "nameTagImage", nameTag.GetComponent<Image>());
        SetField(ui, "playerPortraitFrame", playerFrame.gameObject);
        SetField(ui, "npcPortraitFrame", npcFrame.gameObject);
        SetField(ui, "playerPortraitImage", playerFrame.GetComponentInChildren<RawImage>());
        SetField(ui, "npcPortraitImage", npcFrame.GetComponentInChildren<RawImage>());
        SetField(ui, "continueIndicator", Wrap(arrow.gameObject));
        SetField(ui, "boxPop", box.gameObject.AddComponent<UIPop>());
    }

    // round frame + masked RawImage showing a portrait RenderTexture, overlapping the top edge of the box
    private static RectTransform Portrait(RectTransform box, string name, RenderTexture texture, Vector2 anchor, Vector2 position)
    {
        RectTransform frame = Panel(box, name, anchor, anchor, new Vector2(.5f, .3f), position, new Vector2(250f, 250f), Cream);
        frame.GetComponent<Image>().sprite = GrayboxTextures.Circle;
        frame.GetComponent<Image>().type = Image.Type.Simple;
        Outline(frame.gameObject);
        Image mask = Img(frame, "Mask", GrayboxTextures.Circle, Color.white, Vector2.zero, Vector2.one, Centre, Vector2.zero, new Vector2(-16f, -16f));
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        RawImage raw = new GameObject("Portrait", typeof(RectTransform)).AddComponent<RawImage>();
        raw.transform.SetParent(mask.transform, false);
        raw.texture = texture;
        raw.raycastTarget = false;
        Stretch(raw.rectTransform, Vector2.zero, Vector2.zero);
        return frame;
    }

    private static void BuildDiscovery(Transform canvas)
    {
        RectTransform root = Rect(canvas, "DiscoveryPopupUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        Image dim = Img(root, "Dim", null, new Color(.15f, .12f, .12f, .45f), Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        dim.raycastTarget = true;
        RectTransform card = Panel(root, "Card", Centre, Centre, Centre, Vector2.zero, new Vector2(640f, 560f), Cream);
        Outline(card.gameObject);
        RectTransform ribbon = Panel(card, "Ribbon", new Vector2(.5f, 1f), new Vector2(.5f, 1f), Centre, Vector2.zero, new Vector2(340f, 70f), Accent);
        TextMeshProUGUI header = Text(ribbon, "Header", "New item!", 36, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(header.rectTransform, Vector2.zero, Vector2.zero);
        RectTransform spin = Rect(card, "IconSpin", new Vector2(.5f, 1f), new Vector2(.5f, 1f), Centre, new Vector2(0f, -175f), new Vector2(220f, 220f));
        Img(spin, "Glow", GrayboxTextures.SoftCircle, new Color(1f, .9f, .55f, .8f), Vector2.zero, Vector2.one, Centre, Vector2.zero, new Vector2(80f, 80f));
        Image icon = Img(spin, "Icon", null, Color.white, Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        icon.preserveAspect = true;
        TextMeshProUGUI itemName = Text(card, "Name", "Item", 44, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(itemName.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -325f), new Vector2(580f, 60f));
        TextMeshProUGUI description = Text(card, "Description", "Description", 28, Ink, TextAlignmentOptions.Top);
        description.textWrappingMode = TextWrappingModes.Normal;
        Place(description.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -400f), new Vector2(560f, 100f));
        Button button = Button(card, "ContinueButton", "Press [E] to continue", new Vector2(0f, 50f), new Vector2(420f, 64f));
        DiscoveryPopupUI ui = root.gameObject.AddComponent<DiscoveryPopupUI>();
        SetField(ui, "iconImage", icon);
        SetField(ui, "nameText", itemName);
        SetField(ui, "descriptionText", description);
        SetField(ui, "continueText", button.GetComponentInChildren<TextMeshProUGUI>());
        SetField(ui, "continueButton", button);
        SetField(ui, "cardPop", card.gameObject.AddComponent<UIPop>());
        SetField(ui, "iconSpin", spin);
    }

    private static void BuildPause(Transform canvas)
    {
        RectTransform root = Rect(canvas, "PauseUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        Image dim = Img(root, "Dim", null, new Color(.15f, .12f, .12f, .55f), Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        dim.raycastTarget = true;
        RectTransform card = Panel(root, "Card", Centre, Centre, Centre, Vector2.zero, new Vector2(560f, 545f), Cream);
        TextMeshProUGUI title = Text(card, "Title", "Paused", 52, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(title.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -70f), new Vector2(500f, 70f));
        Button resume = Button(card, "ResumeButton", "Resume", new Vector2(0f, 335f), new Vector2(420f, 74f));
        Button unstuck = Button(card, "UnstuckButton", "Unstuck Me", new Vector2(0f, 240f), new Vector2(420f, 74f));
        Button rebind = Button(card, "RebindButton", "Interact key: [E]", new Vector2(0f, 145f), new Vector2(420f, 74f));
        Button reset = Button(card, "ResetButton", "Reset keys", new Vector2(0f, 50f), new Vector2(420f, 74f));
        RectTransform overlay = Panel(card, "PressKeyOverlay", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero, Accent);
        TextMeshProUGUI overlayText = Text(overlay, "Text", "Press a key for Interact...\n<size=26>(Esc to cancel)</size>", 38, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(overlayText.rectTransform, Vector2.zero, Vector2.zero);
        PauseUI ui = root.gameObject.AddComponent<PauseUI>();
        SetField(ui, "resumeButton", resume);
        SetField(ui, "unstuckButton", unstuck);
        SetField(ui, "unstuckButtonText", unstuck.GetComponentInChildren<TextMeshProUGUI>());
        SetField(ui, "rebindButton", rebind);
        SetField(ui, "resetButton", reset);
        SetField(ui, "rebindButtonText", rebind.GetComponentInChildren<TextMeshProUGUI>());
        SetField(ui, "pressKeyOverlay", overlay.gameObject);
    }

    private static void BuildFlash(Transform canvas, Refs refs)
    {
        Image flash = Img(canvas, "FadeFlashUI", null, Color.white, Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        flash.gameObject.AddComponent<CanvasGroup>();
        SetField(flash.gameObject.AddComponent<FadeFlashUI>(), "fabricator", refs.fabricator);
    }

    private static void BuildIris(Transform canvas, Refs refs)
    {
        RectTransform root = Rect(canvas, "IrisWipeUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        Image hole = Img(root, "Hole", GrayboxTextures.IrisHole, Color.black, Vector2.zero, Vector2.zero, Centre, Vector2.zero, new Vector2(4000f, 4000f));
        // four huge bars glued to the hole's edges cover everything outside it
        Img(hole.rectTransform, "Top", null, Color.black, new Vector2(0f, 1f), Vector2.one, new Vector2(.5f, 0f), Vector2.zero, new Vector2(20000f, 10000f));
        Img(hole.rectTransform, "Bottom", null, Color.black, Vector2.zero, new Vector2(1f, 0f), new Vector2(.5f, 1f), Vector2.zero, new Vector2(20000f, 10000f));
        Img(hole.rectTransform, "Left", null, Color.black, Vector2.zero, new Vector2(0f, 1f), new Vector2(1f, .5f), Vector2.zero, new Vector2(10000f, 0f));
        Img(hole.rectTransform, "Right", null, Color.black, new Vector2(1f, 0f), Vector2.one, new Vector2(0f, .5f), Vector2.zero, new Vector2(10000f, 0f));
        IrisWipeUI ui = root.gameObject.AddComponent<IrisWipeUI>();
        SetField(ui, "houseDoor", refs.houseDoor);
        SetField(ui, "hole", hole.rectTransform);
        SetField(ui, "canvasGroup", group);
    }

    // ---------- widget helpers ----------

    public static readonly Vector2 Centre = new Vector2(.5f, .5f);

    public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static RectTransform Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = Rect(parent, name, anchorMin, anchorMax, pivot, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = GrayboxTextures.RoundedPanel;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    public static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        Image image = Rect(parent, name, anchorMin, anchorMax, pivot, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        TextMeshProUGUI tmp = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(parent, false);
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    public static Button Button(Transform parent, string name, string label, Vector2 bottomPosition, Vector2 size)
    {
        RectTransform rect = Panel(parent, name, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(.5f, 0f), bottomPosition, size, Accent);
        Image image = rect.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, .93f, .75f);
        colors.selectedColor = new Color(1f, .93f, .75f);
        colors.pressedColor = new Color(.9f, .72f, .38f);
        button.colors = colors;
        TextMeshProUGUI text = Text(rect, "Label", label, 30, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        return button;
    }

    public static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Outline(GameObject go)
    {
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(.3f, .22f, .18f, .25f);
        shadow.effectDistance = new Vector2(0f, -5f);
    }

    // the continue dot bobs via localPosition, so it sits inside a holder that keeps the layout position
    private static GameObject Wrap(GameObject go)
    {
        RectTransform holder = Rect(go.transform.parent, go.name + "Holder", ((RectTransform)go.transform).anchorMin, ((RectTransform)go.transform).anchorMax, Centre, ((RectTransform)go.transform).anchoredPosition, Vector2.zero);
        go.transform.SetParent(holder, false);
        ((RectTransform)go.transform).anchorMin = Centre;
        ((RectTransform)go.transform).anchorMax = Centre;
        ((RectTransform)go.transform).anchoredPosition = Vector2.zero;
        return go;
    }
}
