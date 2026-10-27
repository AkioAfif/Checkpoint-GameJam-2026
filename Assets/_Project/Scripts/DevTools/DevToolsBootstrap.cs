#if DEVELOPER_TOOLS || UNITY_EDITOR
using UnityEngine;

// Adds the developer tools to every scene, so no scene needs editing. Compiled out of builds unless DEVELOPER_TOOLS is defined
// (Player Settings > Scripting Define Symbols) or the build runs in the Editor.
public static class DevToolsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateDevTools()
    {
        new GameObject("DevTools").AddComponent<DevTools>();
    }
}
#endif
