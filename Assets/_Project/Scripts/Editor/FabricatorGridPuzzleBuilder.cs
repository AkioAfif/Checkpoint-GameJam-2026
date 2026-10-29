using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static GrayboxBuild;
using static UiBuilder;

// The Fabricator's grid puzzle (the Sawit-Fabricator gameplay): the board skin, the booth's components and the puzzle HUD.
// The scene builders call SetUpBooth / BuildHud; Tools > Fabricator > Install Grid Puzzle In Open Scene swaps them into a
// scene that still has the old liquid puzzle (re-runnable).
public static class FabricatorGridPuzzleBuilder
{
    private const string SPRITES = "Assets/_Project/Art/Sprites/FabricatorBoard";
    private const string SPRITE_MATERIAL = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    // 1 cell = 1 unit, as in Sawit-Fabricator (the puzzle camera fits whatever size the board is)
    private const float CELL_SIZE = 1f;
    // Sawit-Fabricator's camera background, dark gunmetal
    private static readonly Color BackgroundColor = new Color32(0x14, 0x18, 0x1E, 0xFF);

    [MenuItem("Tools/Fabricator/Install Grid Puzzle In Open Scene")]
    public static void InstallInOpenScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop Play mode first.");
            return;
        }
        FabricatorMiniGame miniGame = Object.FindAnyObjectByType<FabricatorMiniGame>(FindObjectsInactive.Include);
        FabricatorController fabricator = Object.FindAnyObjectByType<FabricatorController>(FindObjectsInactive.Include);
        FabricatorUI oldUi = Object.FindAnyObjectByType<FabricatorUI>(FindObjectsInactive.Include);
        if (miniGame == null || fabricator == null || oldUi == null)
        {
            Debug.LogError("This scene has no Fabricator puzzle booth (FabricatorMiniGame, FabricatorController and FabricatorUI).");
            return;
        }
        SetField(fabricator, "levelListSO", FabricatorLevelBuilder.Build());
        Camera puzzleCamera = miniGame.GetComponentInChildren<FabricatorPuzzleCamera>(true).GetComponent<Camera>();
        SetUpBooth(miniGame, puzzleCamera);

        Transform canvas = oldUi.transform.parent;
        // the first press's yellow hint ring is gone
        Transform hintUi = canvas.Find("FabricatorHintUI");
        if (hintUi != null) Object.DestroyImmediate(hintUi.gameObject);
        int siblingIndex = oldUi.transform.GetSiblingIndex();
        Object.DestroyImmediate(oldUi.gameObject);
        BuildHud(canvas, fabricator, miniGame).SetSiblingIndex(siblingIndex);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(miniGame.gameObject.scene);
        EditorSceneManager.SaveScene(miniGame.gameObject.scene);
        Debug.Log($"Grid puzzle installed in {miniGame.gameObject.scene.name}.");
    }

    // the logic and its visuals all live on the booth root (Puzzle layer), filmed flat-on in 2D like Sawit-Fabricator.
    // The old liquid puzzle's tilemap and particles and the 3D glass-tube diorama with its light go.
    public static void SetUpBooth(FabricatorMiniGame miniGame, Camera puzzleCamera)
    {
        GameObject booth = miniGame.gameObject;
        Grid grid = booth.GetComponentInChildren<Grid>(true);
        if (grid == null) grid = Root("Grid", booth.transform).AddComponent<Grid>();
        grid.gameObject.layer = booth.layer;
        grid.cellSize = new Vector3(CELL_SIZE, CELL_SIZE, 0f);
        for (int i = grid.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(grid.transform.GetChild(i).gameObject);
        foreach (string leftover in new[] { "Particles", "Diorama", "PuzzleLight" })
        {
            Transform child = booth.transform.Find(leftover);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        puzzleCamera.orthographic = true;
        puzzleCamera.clearFlags = CameraClearFlags.SolidColor;
        puzzleCamera.backgroundColor = BackgroundColor;
        // the camera used to sit on a swaying pivot; FabricatorPuzzleCamera now places it itself
        puzzleCamera.transform.parent.localRotation = Quaternion.identity;
        SetField(puzzleCamera.GetComponent<FabricatorPuzzleCamera>(), "miniGame", miniGame);

        SetField(miniGame, "grid", grid);
        SetField(miniGame, "puzzleCamera", puzzleCamera);
        FabricatorBoardSkinSO skinSO = Skin();
        WireVisual(GetOrAdd<FabricatorMiniGameVisual>(booth), miniGame, skinSO);
        WireVisual(GetOrAdd<FabricatorUnitVisual>(booth), miniGame, skinSO);
        WireVisual(GetOrAdd<FabricatorRootsVisual>(booth), miniGame, skinSO);
        WireVisual(GetOrAdd<FabricatorMistVisual>(booth), miniGame, skinSO);
    }

    // the note right of the board: level, energy, embryos left, slides vs par, controls; banners for solved and lost
    public static RectTransform BuildHud(Transform canvas, FabricatorController fabricator, FabricatorMiniGame miniGame)
    {
        RectTransform root = Rect(canvas, "FabricatorUI", Vector2.zero, Vector2.one, Centre, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<CanvasGroup>();
        RectTransform panel = Panel(root, "Panel", new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-40f, 0f), new Vector2(460f, 560f), Cream);
        TextMeshProUGUI title = Text(panel, "Title", "Fabricator", 40, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(title.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -50f), new Vector2(400f, 60f));
        TextMeshProUGUI level = Text(panel, "Level", "Level 1/12", 26, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(level.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -100f), new Vector2(420f, 36f));
        TextMeshProUGUI hint = Text(panel, "Hint", "Click the Tech Bug, then an arrow: it slides until something stops it. Each slide costs 1 energy.\nCorrupt every Seed Embryo! Energy Cells recharge; Seed Burrs and thorns destroy the bug.", 21, Ink, TextAlignmentOptions.Center);
        hint.textWrappingMode = TextWrappingModes.Normal;
        Place(hint.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -190f), new Vector2(420f, 140f));
        TextMeshProUGUI energy = Text(panel, "Energy", "Energy: 0 / 0", 34, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(energy.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -295f), new Vector2(420f, 46f));
        TextMeshProUGUI embryos = Text(panel, "Embryos", "Seed Embryos left: 0", 26, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(embryos.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -350f), new Vector2(420f, 40f));
        TextMeshProUGUI moves = Text(panel, "Moves", "Slides: 0", 24, Ink, TextAlignmentOptions.Center);
        moves.textWrappingMode = TextWrappingModes.Normal;
        Place(moves.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -405f), new Vector2(420f, 60f));
        TextMeshProUGUI controls = Text(panel, "Controls", "[Mouse] Click    [R] Restart    [Esc] Leave", 22, Ink, TextAlignmentOptions.Center);
        Place(controls.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, 40f), new Vector2(430f, 40f));

        CanvasGroup solvedGroup = Banner(root, "SolvedBanner", "Seed ready!", 88, new Vector2(-150f, 330f), out TextMeshProUGUI _, out UIPop solvedPop);
        CanvasGroup lostGroup = Banner(root, "LostBanner", "Stuck!", 64, new Vector2(-150f, 330f), out TextMeshProUGUI lostText, out UIPop lostPop);

        FabricatorUI ui = root.gameObject.AddComponent<FabricatorUI>();
        SetField(ui, "fabricator", fabricator);
        SetField(ui, "miniGame", miniGame);
        SetField(ui, "levelText", level);
        SetField(ui, "energyText", energy);
        SetField(ui, "embryoText", embryos);
        SetField(ui, "movesText", moves);
        SetField(ui, "solvedBanner", solvedGroup);
        SetField(ui, "solvedPop", solvedPop);
        SetField(ui, "lostBanner", lostGroup);
        SetField(ui, "lostText", lostText);
        SetField(ui, "lostPop", lostPop);
        return root;
    }

    // the board's sprites (copied from Sawit-Fabricator, 512 px = 1 cell) and tints; tints set by hand survive a re-run
    public static FabricatorBoardSkinSO Skin()
    {
        FabricatorBoardSkinSO skinSO = GameDataBuilder.Asset<FabricatorBoardSkinSO>("Puzzles", "FabricatorBoardSkin");
        skinSO.spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(SPRITE_MATERIAL);
        skinSO.floorSprite = LoadSprite("cytoplasm_tile");
        skinSO.wallSprite = LoadSprite("cellulose_wall");
        skinSO.snareSprite = LoadSprite("stop_tile");
        skinSO.frameSprite = LoadSprite("fabricator_frame");
        skinSO.gateOpenSprite = LoadSprite("gate_open");
        skinSO.gateClosedSprite = LoadSprite("gate_closed");
        skinSO.routerNodeSprite = LoadSprite("valve_node");
        skinSO.routerArrowSprite = LoadSprite("valve_arrow");
        skinSO.injectorIdleSprite = LoadSprite("chemical_valve_idle");
        skinSO.injectorReleasedSprite = LoadSprite("chemical_valve_released");
        skinSO.mistSprite = LoadSprite("chemical_mist");
        skinSO.bugSprite = LoadSprite("cancer_cell");
        skinSO.embryoSprite = LoadSprite("core_cell");
        skinSO.sentinelSprite = LoadSprite("defensive_cell");
        skinSO.energyCellSprite = LoadSprite("energy_cell");
        skinSO.movingCellSprite = LoadSprite("moving_cell");
        skinSO.hostileMovingCellSprite = LoadSprite("moving_cell_hostile");
        skinSO.rootStemSprite = LoadSprite("vine_stem");
        skinSO.thornyRootStemSprite = LoadSprite("vine_stem_thorny");
        skinSO.rootKnotSprite = LoadSprite("vine_knot");
        skinSO.rootTipSprite = LoadSprite("vine_tip");
        skinSO.arrowSprite = LoadSprite("move_arrow");
        EditorUtility.SetDirty(skinSO);
        return skinSO;
    }

    private static CanvasGroup Banner(RectTransform root, string name, string text, float size, Vector2 position, out TextMeshProUGUI label, out UIPop pop)
    {
        RectTransform banner = Rect(root, name, Centre, Centre, Centre, position, new Vector2(900f, 130f));
        CanvasGroup group = banner.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        label = Text(banner, "Text", text, size, Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
        label.outlineWidth = .22f;
        label.outlineColor = new Color32(70, 55, 50, 255);
        pop = banner.gameObject.AddComponent<UIPop>();
        return group;
    }

    private static void WireVisual(Component visual, FabricatorMiniGame miniGame, FabricatorBoardSkinSO skinSO)
    {
        SetField(visual, "miniGame", miniGame);
        SetField(visual, "skinSO", skinSO);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static Sprite LoadSprite(string file)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES}/{file}.png");
        if (sprite == null) Debug.LogError($"{SPRITES}/{file}.png is missing or not imported as a Sprite");
        return sprite;
    }
}
