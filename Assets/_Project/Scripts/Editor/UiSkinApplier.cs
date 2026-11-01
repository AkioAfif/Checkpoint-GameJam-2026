using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Puts the hand-drawn 2D UI kit (Art/Sprites/UI, Art/Textures/Icons2D) on the panels the builders made, in place: only
// sprites, colours and a few added child images change, so every UI script keeps its wiring. Re-runnable; run it again
// after any scene builder regenerates a scene.
public static class UiSkinApplier
{
    private const string UI_FOLDER = "Assets/_Project/Art/Sprites/UI";
    // the kit's frames were exported with their left/top edges cut off; Tools/UiSprites/restore_cropped_edges.py rebuilds
    // them here (same sub-folder and name), and those are used whenever they exist
    private const string RESTORED_FOLDER = UI_FOLDER + "/Restored";
    private const string ICONS_2D_FOLDER = "Assets/_Project/Art/Textures/Icons2D";
    private const string ITEMS_FOLDER = "Assets/_Project/ScriptableObjects/Items";
    private const string CURSOR_PATH = UI_FOLDER + "/Common/ui_common_cursor.png";

    // panel sprites are 9-sliced; corners never take more than this share of the element's smaller side
    private const float MAX_BORDER_SHARE = .9f;
    // the paper's folded corner is drawn smaller than native so text near the top right stays clear of it
    private const float PAPER_SCALE = 1.6f;

    private static readonly Color Ink = new Color(.30f, .25f, .23f);
    private static readonly Color Dimmed = new Color(.35f, .32f, .3f, .8f);

    private static Sprite paper, hintBar, buttonGold, buttonGreen, rowBg, bannerNewItem, bannerPlanted, bannerSeedReady,
        bannerDebtRecord, bannerSettings, keyE, keyR, keyEsc, mouseLeft, holdRing, barUnfilled, barFilled, koin;

    [MenuItem("Tools/UI/Apply UI Skin To All Scenes")]
    public static void ApplyToAllScenes()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string openScenePath = SceneManager.GetActiveScene().path;
        ImportSprites();
        LoadSprites();
        ApplyItemIcons();
        ApplyCursor();
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            ApplyToScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(openScenePath)) EditorSceneManager.OpenScene(openScenePath, OpenSceneMode.Single);
        Debug.Log("UI skin applied to every scene in the build list.");
    }

    public static bool HasDrawnIcon(ItemDataSO itemDataSO) =>
        itemDataSO.icon != null && AssetDatabase.GetAssetPath(itemDataSO.icon).StartsWith(ICONS_2D_FOLDER);

    // ---------- assets ----------

    private static void ImportSprites()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UI_FOLDER, ICONS_2D_FOLDER }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == CURSOR_PATH) continue;
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            // the art is drawn 2-7x larger than it shows on screen
            importer.mipmapEnabled = true;
            importer.spriteBorder = GetBorder(System.IO.Path.GetFileNameWithoutExtension(path), path.StartsWith(RESTORED_FOLDER));
            importer.SaveAndReimport();
        }
    }

    // left, bottom, right, top in source pixels; a restored copy has the rebuilt strip added on its left and top
    private static Vector4 GetBorder(string fileName, bool isRestored)
    {
        switch (fileName)
        {
            case "ui_common_panel_paper": return new Vector4(32f, 75f, 130f, 140f) + (isRestored ? new Vector4(3f, 0f, 0f, 5f) : Vector4.zero);
            case "ui_common_hintbar_bg": return new Vector4(48f, 48f, 48f, 48f) + (isRestored ? new Vector4(9f, 0f, 0f, 9f) : Vector4.zero);
            case "ui_common_button_gold":
            case "ui_common_button_green": return new Vector4(38f, 38f, 38f, 38f) + (isRestored ? new Vector4(9f, 0f, 0f, 5f) : Vector4.zero);
            case "ui_fabricator_row_bg": return new Vector4(40f, 40f, 40f, 40f);
            default: return Vector4.zero;
        }
    }

    private static void LoadSprites()
    {
        paper = UiSprite("Common/ui_common_panel_paper");
        hintBar = UiSprite("Common/ui_common_hintbar_bg");
        buttonGold = UiSprite("Common/ui_common_button_gold");
        buttonGreen = UiSprite("Common/ui_common_button_green");
        rowBg = UiSprite("Fabricator/ui_fabricator_row_bg");
        bannerNewItem = UiSprite("Banners/ui_banner_new_item");
        bannerPlanted = UiSprite("Banners/ui_banner_planted");
        bannerSeedReady = UiSprite("Banners/ui_banner_seed_ready");
        bannerDebtRecord = UiSprite("Banners/ui_banner_debt_record");
        bannerSettings = UiSprite("Banners/ui_banner_settings");
        keyE = UiSprite("Input/ui_common_key_e_white_idle");
        keyR = UiSprite("Input/ui_common_key_r_white_idle");
        keyEsc = UiSprite("Input/ui_common_key_esc_white_idle");
        mouseLeft = UiSprite("Input/ui_common_mouse_left");
        holdRing = UiSprite("QTE/ui_qte_hold_ring");
        barUnfilled = UiSprite("Loading/ui_loading_bar_unfilled");
        barFilled = UiSprite("Loading/ui_loading_bar_filled");
        koin = AssetDatabase.LoadAssetAtPath<Sprite>($"{ICONS_2D_FOLDER}/Icon_Koin.png");
    }

    private static Sprite UiSprite(string name)
    {
        Sprite restored = AssetDatabase.LoadAssetAtPath<Sprite>($"{RESTORED_FOLDER}/{name}.png");
        return restored != null ? restored : AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_FOLDER}/{name}.png");
    }

    // Icon_<asset name>.png replaces the baked 3D render; items without a drawing (tools, compost) keep theirs
    private static void ApplyItemIcons()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ItemDataSO", new[] { ITEMS_FOLDER }))
        {
            ItemDataSO itemDataSO = AssetDatabase.LoadAssetAtPath<ItemDataSO>(AssetDatabase.GUIDToAssetPath(guid));
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{ICONS_2D_FOLDER}/Icon_{itemDataSO.name}.png");
            if (icon == null || itemDataSO.icon == icon) continue;
            itemDataSO.icon = icon;
            EditorUtility.SetDirty(itemDataSO);
        }
    }

    private static void ApplyCursor()
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(CURSOR_PATH);
        importer.textureType = TextureImporterType.Cursor;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        // 337x419 source → about 51x64, a normal hardware cursor size
        importer.maxTextureSize = 64;
        importer.SaveAndReimport();
        PlayerSettings.defaultCursor = AssetDatabase.LoadAssetAtPath<Texture2D>(CURSOR_PATH);
        // the arrow's tip
        PlayerSettings.cursorHotspot = new Vector2(4f, 4f);
    }

    // ---------- scenes ----------

    private static void ApplyToScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (TaskUI ui in root.GetComponentsInChildren<TaskUI>(true)) Tasks(ui);
            foreach (InventoryUI ui in root.GetComponentsInChildren<InventoryUI>(true)) Inventory(ui);
            foreach (PromptUI ui in root.GetComponentsInChildren<PromptUI>(true)) Sliced(ui.GetComponent<Image>(), hintBar);
            foreach (QTEUI ui in root.GetComponentsInChildren<QTEUI>(true)) Qte(ui);
            foreach (SeedChoiceUI ui in root.GetComponentsInChildren<SeedChoiceUI>(true)) PaperCard(ui.GetComponent<Image>());
            foreach (FabricatorUI ui in root.GetComponentsInChildren<FabricatorUI>(true)) Fabricator(ui);
            foreach (FabricatorRecipeUI ui in root.GetComponentsInChildren<FabricatorRecipeUI>(true)) FabricatorRecipe(ui);
            foreach (DialogueUI ui in root.GetComponentsInChildren<DialogueUI>(true)) Dialogue(ui);
            foreach (DiscoveryPopupUI ui in root.GetComponentsInChildren<DiscoveryPopupUI>(true)) Discovery(ui);
            foreach (PauseUI ui in root.GetComponentsInChildren<PauseUI>(true)) Pause(ui);
            foreach (SavingsBookUI ui in root.GetComponentsInChildren<SavingsBookUI>(true)) SavingsBook(ui);
            foreach (LetterUI ui in root.GetComponentsInChildren<LetterUI>(true)) PaperCard(Field<UIPop>(ui, "cardPop").GetComponent<Image>());
            foreach (MoneyPopupUI ui in root.GetComponentsInChildren<MoneyPopupUI>(true)) MoneyPopup(ui);
            foreach (OfferChoiceUI ui in root.GetComponentsInChildren<OfferChoiceUI>(true)) PaperCard(ui.GetComponent<Image>());
            foreach (MainMenuUI ui in root.GetComponentsInChildren<MainMenuUI>(true)) MainMenu(ui);
            foreach (SettingsUI ui in root.GetComponentsInChildren<SettingsUI>(true)) Settings(ui);
            foreach (StubSceneUI ui in root.GetComponentsInChildren<StubSceneUI>(true)) Stub(ui);
            foreach (LandQTEUI ui in root.GetComponentsInChildren<LandQTEUI>(true)) LandQte(ui);
            foreach (PlantingDashUI ui in root.GetComponentsInChildren<PlantingDashUI>(true)) PlantingDash(ui);
            // every button on a paper card, wherever its panel came from
            foreach (Button button in root.GetComponentsInChildren<Button>(true)) SkinButton(button);
        }
    }

    private static void Tasks(TaskUI ui)
    {
        PaperCard(ui.GetComponent<Image>());
        // keep the rows' right-aligned counts clear of the folded corner
        VerticalLayoutGroup layout = ui.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 56, 24, 30);
        TextMeshProUGUI debtText = Field<TextMeshProUGUI>(ui, "debtText");
        debtText.margin = new Vector4(38f, 0f, 0f, 0f);
        Image icon = ChildImage(debtText.transform, "KoinIcon", koin);
        Place(icon.rectTransform, new Vector2(0f, .5f), new Vector2(15f, 0f), new Vector2(30f, 28f));
    }

    private static void Inventory(InventoryUI ui)
    {
        InventorySingleUI slot = Field<InventorySingleUI>(ui, "slotTemplate");
        Sliced(slot.GetComponent<Image>(), hintBar);
        Transform badge = slot.transform.Find("Badge");
        if (badge != null) Sliced(badge.GetComponent<Image>(), buttonGold);
    }

    private static void Qte(QTEUI ui)
    {
        TextMeshProUGUI keyText = Field<TextMeshProUGUI>(ui, "keyText");
        Image keyBg = keyText.transform.parent.GetComponent<Image>();
        InteractKeyIconUI keyIcon = keyBg.GetComponent<InteractKeyIconUI>();
        // a re-run keeps the plain cap recorded the first time
        Sprite plainKey = keyIcon != null ? Field<Sprite>(keyIcon, "plainKeySprite") : keyBg.sprite;
        if (keyIcon == null) keyIcon = keyBg.gameObject.AddComponent<InteractKeyIconUI>();
        keyBg.preserveAspect = true;
        keyBg.color = Color.white;
        keyBg.rectTransform.sizeDelta = new Vector2(74f, 78f);
        SetField(keyIcon, "keyImage", keyBg);
        SetField(keyIcon, "keyText", keyText);
        SetField(keyIcon, "drawnKeySprite", keyE);
        SetField(keyIcon, "plainKeySprite", plainKey);

        Transform banner = Field<CanvasGroup>(ui, "bannerCanvasGroup").transform;
        Image bannerImage = ChildImage(banner, "BannerImage", bannerPlanted);
        Place(bannerImage.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(640f, 181f));
        bannerImage.enabled = false;
        SetField(ui, "bannerImage", bannerImage);
    }

    private static void Fabricator(FabricatorUI ui)
    {
        Transform panel = Field<TextMeshProUGUI>(ui, "levelText").transform.parent;
        PaperCard(panel.GetComponent<Image>());
        Transform solved = Field<CanvasGroup>(ui, "solvedBanner").transform;
        DrawnBanner(solved, bannerSeedReady, new Vector2(620f, 175f));

        Transform controls = panel.Find("Controls");
        if (controls == null) return;
        controls.gameObject.SetActive(false);
        RectTransform row = ChildRect(panel, "ControlsRow");
        RectTransform controlsRect = (RectTransform)controls;
        row.anchorMin = controlsRect.anchorMin;
        row.anchorMax = controlsRect.anchorMax;
        row.pivot = controlsRect.pivot;
        row.anchoredPosition = controlsRect.anchoredPosition;
        row.sizeDelta = new Vector2(430f, 48f);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        ControlHint(row, "Click", mouseLeft, new Vector2(24f, 42f), "Click", 0);
        ControlHint(row, "Restart", keyR, new Vector2(38f, 40f), "Restart", 2);
        ControlHint(row, "Leave", keyEsc, new Vector2(60f, 40f), "Leave", 4);
    }

    private static void FabricatorRecipe(FabricatorRecipeUI ui)
    {
        PaperCard(Field<Button>(ui, "cancelButton").transform.parent.GetComponent<Image>());
        FabricatorRecipeSingleUI row = Field<FabricatorRecipeSingleUI>(ui, "inputRowTemplate");
        Image rowImage = row.GetComponent<Image>();
        if (rowImage == null) rowImage = row.gameObject.AddComponent<Image>();
        rowImage.raycastTarget = false;
        Sliced(rowImage, rowBg);
    }

    private static void Dialogue(DialogueUI ui)
    {
        Sliced(Field<TextMeshProUGUI>(ui, "lineText").transform.parent.GetComponent<Image>(), hintBar);
        // DialogueUI tints the tag per speaker; the cream bar takes the tint and keeps its outline
        Sliced(Field<Image>(ui, "nameTagImage"), hintBar, keepColor: true);
    }

    private static void Discovery(DiscoveryPopupUI ui)
    {
        Transform card = Field<UIPop>(ui, "cardPop").transform;
        PaperCard(card.GetComponent<Image>());
        Transform ribbon = card.Find("Ribbon");
        if (ribbon != null) RibbonToBanner(ribbon, bannerNewItem, new Vector2(440f, 125f));
        // three-line descriptions ran into the continue button, which now sits on the paper's footer band
        TextMeshProUGUI nameText = Field<TextMeshProUGUI>(ui, "nameText");
        nameText.rectTransform.anchoredPosition = new Vector2(0f, -312f);
        TextMeshProUGUI descriptionText = Field<TextMeshProUGUI>(ui, "descriptionText");
        descriptionText.rectTransform.anchoredPosition = new Vector2(0f, -382f);
        descriptionText.fontSize = 26f;
    }

    private static void Pause(PauseUI ui)
    {
        PaperCard(Field<Button>(ui, "resumeButton").transform.parent.GetComponent<Image>());
        Sliced(Field<GameObject>(ui, "pressKeyOverlay").GetComponent<Image>(), hintBar, keepColor: true);
    }

    private static void SavingsBook(SavingsBookUI ui)
    {
        PaperCard(Field<UIPop>(ui, "cardPop").GetComponent<Image>());
        RibbonToBanner(Field<TextMeshProUGUI>(ui, "titleText").transform.parent, bannerDebtRecord, new Vector2(500f, 141f));
    }

    private static void MoneyPopup(MoneyPopupUI ui)
    {
        // a child of the amount, so it pops with it
        Image icon = ChildImage(Field<TextMeshProUGUI>(ui, "amountText").transform, "KoinIcon", koin);
        Place(icon.rectTransform, new Vector2(.5f, .5f), new Vector2(0f, 90f), new Vector2(96f, 89f));
    }

    private static void MainMenu(MainMenuUI ui)
    {
        Image card = Field<Button>(ui, "startButton").transform.parent.GetComponent<Image>();
        // the menu card stays see-through over the village pan
        Sliced(card, paper, PAPER_SCALE, keepAlpha: true);
    }

    private static void Settings(SettingsUI ui)
    {
        Transform card = Field<Button>(ui, "backButton").transform.parent;
        PaperCard(card.GetComponent<Image>());
        Transform header = card.Find("Header");
        if (header == null) return;
        header.GetComponent<TextMeshProUGUI>().enabled = false;
        Image banner = ChildImage(header, "Banner", bannerSettings);
        Place(banner.rectTransform, new Vector2(.5f, .5f), new Vector2(0f, 10f), new Vector2(440f, 124f));
        Sliced(Field<GameObject>(ui, "pressKeyOverlay").GetComponent<Image>(), hintBar, keepColor: true);
    }

    private static void Stub(StubSceneUI ui)
    {
        Image card = Field<Button>(ui, "replayButton").transform.parent.GetComponent<Image>();
        if (card != null) PaperCard(card);
    }

    private static void LandQte(LandQTEUI ui)
    {
        Image ring = Field<GameObject>(ui, "ringGameObject").GetComponent<Image>();
        ring.sprite = holdRing;
        ring.type = Image.Type.Simple;
        ring.color = Dimmed;
        ring.rectTransform.sizeDelta = new Vector2(120f, 120f);
        Image fill = Field<Image>(ui, "fillImage");
        fill.sprite = holdRing;
        fill.color = Color.white;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;
    }

    private static void PlantingDash(PlantingDashUI ui)
    {
        Image fill = Field<Image>(ui, "chargeFillImage");
        Image bar = fill.transform.parent.GetComponent<Image>();
        bar.sprite = barUnfilled;
        bar.color = Color.white;
        // the drawn bar's own aspect, kept no taller than the old bar so it clears the dash counter below
        bar.rectTransform.sizeDelta = new Vector2(370f, 32f);
        fill.sprite = barFilled;
        fill.color = Color.white;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    // gold for everything, green for the button that goes ahead (start, make, plant, resume, continue, replay)
    private static void SkinButton(Button button)
    {
        Image image = button.targetGraphic as Image;
        if (image == null) return;
        string name = button.name;
        bool isGo = name.StartsWith("Start") || name.StartsWith("Make") || name.StartsWith("Plant") || name.StartsWith("Resume") || name.StartsWith("Continue") || name.StartsWith("Replay");
        Sliced(image, isGo ? buttonGreen : buttonGold);
        // the sprite carries the colour now: the states only dim/brighten it, so gamepad selection stays visible
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(.9f, .9f, .9f);
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(.72f, .72f, .72f);
        colors.disabledColor = new Color(.6f, .6f, .6f, .55f);
        button.colors = colors;
        foreach (TextMeshProUGUI label in button.GetComponentsInChildren<TextMeshProUGUI>(true)) label.color = Ink;
    }

    // ---------- helpers ----------

    private static void PaperCard(Image image) => Sliced(image, paper, PAPER_SCALE);

    private static void Sliced(Image image, Sprite sprite, float scale = 1f, bool keepColor = false, bool keepAlpha = false)
    {
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.fillCenter = true;
        Rect rect = image.rectTransform.rect;
        Vector4 border = sprite.border;
        float minSide = Mathf.Min(rect.width > 0f ? rect.width : float.MaxValue, rect.height > 0f ? rect.height : float.MaxValue);
        float borderSum = Mathf.Max(border.x + border.z, border.y + border.w);
        // layout-sized elements report 0 until laid out; size the corners on the side we do know
        if (minSide < float.MaxValue) scale = Mathf.Max(scale, borderSum / (minSide * MAX_BORDER_SHARE));
        image.pixelsPerUnitMultiplier = scale;
        if (!keepColor) image.color = new Color(1f, 1f, 1f, keepAlpha ? image.color.a : 1f);
        EditorUtility.SetDirty(image);
    }

    // a ribbon panel with header text becomes the drawn banner (the text stays wired, just hidden)
    private static void RibbonToBanner(Transform ribbon, Sprite banner, Vector2 size)
    {
        Image image = ribbon.GetComponent<Image>();
        image.sprite = banner;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.rectTransform.sizeDelta = size;
        foreach (TextMeshProUGUI text in ribbon.GetComponentsInChildren<TextMeshProUGUI>(true)) text.enabled = false;
    }

    // a banner made of big text gets the drawn banner instead (the text object stays, hidden)
    private static void DrawnBanner(Transform banner, Sprite sprite, Vector2 size)
    {
        foreach (TextMeshProUGUI text in banner.GetComponentsInChildren<TextMeshProUGUI>(true)) text.enabled = false;
        Image image = ChildImage(banner, "BannerImage", sprite);
        Place(image.rectTransform, new Vector2(.5f, .5f), Vector2.zero, size);
    }

    private static void ControlHint(RectTransform row, string name, Sprite sprite, Vector2 iconSize, string label, int siblingIndex)
    {
        Image icon = ChildImage(row, name + "Icon", sprite);
        icon.rectTransform.sizeDelta = iconSize;
        icon.transform.SetSiblingIndex(siblingIndex);
        Transform labelTransform = row.Find(name + "Label");
        TextMeshProUGUI text = labelTransform != null ? labelTransform.GetComponent<TextMeshProUGUI>() : null;
        if (text == null)
        {
            text = new GameObject(name + "Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(row, false);
        }
        text.text = label;
        text.fontSize = 22f;
        text.color = Ink;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(text.GetPreferredValues(label).x + 14f, 40f);
        text.transform.SetSiblingIndex(siblingIndex + 1);
    }

    private static Image ChildImage(Transform parent, string name, Sprite sprite)
    {
        RectTransform rect = ChildRect(parent, name);
        Image image = rect.GetComponent<Image>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform ChildRect(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return (RectTransform)child;
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static T Field<T>(Component component, string fieldName) where T : Object =>
        (T)new SerializedObject(component).FindProperty(fieldName).objectReferenceValue;

    private static void SetField(Component component, string fieldName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(component);
        serializedObject.FindProperty(fieldName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
