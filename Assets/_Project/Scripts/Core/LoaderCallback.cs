using UnityEngine;

// Lives in LoadingScene: waits one rendered frame (so the loading screen shows), then loads the target scene.
public class LoaderCallback : MonoBehaviour
{
    private bool isFirstUpdate = true;

    private void Update()
    {
        if (!isFirstUpdate) return;
        isFirstUpdate = false;
        Loader.LoaderCallback();
    }
}
