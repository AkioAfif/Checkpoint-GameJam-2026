using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static GrayboxBuild;

// LoadingScene + HouseInterior_Stub (placeholder for the next level) and the build scene list.
public static class ExtraScenesBuilder
{
    private const string FOLDER = "Assets/_Project/Scenes";

    [MenuItem("Tools/Level01/Rebuild Loading + Stub Scenes")]
    public static void BuildAll()
    {
        BuildLoadingScene();
        BuildStubScene();
        // the game starts in the bedroom
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(HouseInteriorBuilder.SCENE_PATH, true),
            new EditorBuildSettingsScene(Level01SceneBuilder.SCENE_PATH, true),
            new EditorBuildSettingsScene($"{FOLDER}/{Loader.Scene.LoadingScene}.unity", true),
            new EditorBuildSettingsScene($"{FOLDER}/{Loader.Scene.HouseInterior_Stub}.unity", true),
        };
    }

    private static void BuildLoadingScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BlackCamera();
        Transform canvas = UiBuilder.Canvas("Canvas");
        TextMeshProUGUI text = UiBuilder.Text(canvas, "Loading", "Loading...", 52, UiBuilder.Cream, TextAlignmentOptions.BottomRight, FontStyles.Bold);
        UiBuilder.Stretch(text.rectTransform, new Vector2(60f, 50f), new Vector2(-60f, -50f));
        Root("LoaderCallback").AddComponent<LoaderCallback>();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), $"{FOLDER}/{Loader.Scene.LoadingScene}.unity");
    }

    private static void BuildStubScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BlackCamera();
        UiBuilder.BuildEventSystem();
        Transform canvas = UiBuilder.Canvas("Canvas");
        RectTransform card = UiBuilder.Panel(canvas, "Card", UiBuilder.Centre, UiBuilder.Centre, UiBuilder.Centre, Vector2.zero, new Vector2(820f, 440f), UiBuilder.Cream);
        TextMeshProUGUI title = UiBuilder.Text(card, "Title", "Home sweet home", 56, UiBuilder.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        UiBuilder.Place(title.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -90f), new Vector2(760f, 80f));
        TextMeshProUGUI body = UiBuilder.Text(card, "Body", "The house interior (next level) isn't built yet.\nThis is where it will load.", 30, UiBuilder.Ink, TextAlignmentOptions.Center);
        UiBuilder.Place(body.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -200f), new Vector2(760f, 100f));
        Button replay = UiBuilder.Button(card, "ReplayButton", "Replay the village", new Vector2(0f, 50f), new Vector2(440f, 80f));
        StubSceneUI stubUI = Root("StubSceneUI").AddComponent<StubSceneUI>();
        SetField(stubUI, "replayButton", replay);
        SetField(stubUI, "bodyText", body);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), $"{FOLDER}/{Loader.Scene.HouseInterior_Stub}.unity");
    }

    private static void BlackCamera()
    {
        Camera camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
    }
}
