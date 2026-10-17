using UnityEngine;

// Task list + inventory strip: hidden during dialogue, the Fabricator puzzle and the house-entry cutscene.
[RequireComponent(typeof(CanvasGroup))]
public class HudUI : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= GameManager_OnStateChanged;
    }

    private void GameManager_OnStateChanged(object sender, GameManager.OnStateChangedEventArgs e)
    {
        GameManager.State state = GameManager.Instance.GetState();
        canvasGroup.SetVisible(state != GameManager.State.Dialogue && state != GameManager.State.Fabricating && state != GameManager.State.EnteringHouse);
    }
}
