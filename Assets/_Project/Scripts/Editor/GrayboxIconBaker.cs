using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Renders each item's 3D model to a 256px PNG sprite (3/4 view, transparent background) and assigns it as the icon.
// Uses a throwaway scene + URP render request (PreviewRenderUtility stalls under URP).
public static class GrayboxIconBaker
{
    private const string FOLDER = "Assets/_Project/Art/Textures/Icons";
    private const int SIZE = 256;

    public static void BakeIcons(ItemDataSO[] itemArray)
    {
        if (!AssetDatabase.IsValidFolder(FOLDER)) AssetDatabase.CreateFolder("Assets/_Project/Art/Textures", "Icons");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.6f, .6f, .65f);

        GameObject lightGo = new GameObject("IconLight");
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightGo.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

        Camera camera = new GameObject("IconCamera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.fieldOfView = 24f;
        camera.nearClipPlane = .01f;
        camera.farClipPlane = 50f;
        RenderTexture target = new RenderTexture(SIZE, SIZE, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };

        foreach (ItemDataSO item in itemArray)
        {
            // the hand-drawn 2D icons win over a render (UiSkinApplier assigns them)
            if (UiSkinApplier.HasDrawnIcon(item)) continue;
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(item.prefab.gameObject);
            Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
            float distance = bounds.extents.magnitude / Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad) * 1.05f;
            Quaternion view = Quaternion.Euler(28f, -32f, 0f);
            camera.transform.SetPositionAndRotation(bounds.center - view * Vector3.forward * distance, view);

            RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest { destination = target };
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = target;
            Texture2D texture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, SIZE, SIZE), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            Object.DestroyImmediate(model);

            string path = $"{FOLDER}/Icon_{item.name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            EditorUtility.SetDirty(item);
        }
        target.Release();
        AssetDatabase.SaveAssets();
    }
}
