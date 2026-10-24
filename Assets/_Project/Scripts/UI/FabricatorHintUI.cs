using UnityEngine;

// Tutorial ghost cursor: a pulsing ring sweeps over the dirt until the player digs for the first time.
[RequireComponent(typeof(CanvasGroup))]
public class FabricatorHintUI : MonoBehaviour
{
    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private RectTransform ring;
    [SerializeField] private float sweepDuration = 1.4f;

    private CanvasGroup canvasGroup;
    private float timer;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.SetVisible(false);
    }

    private void Update()
    {
        bool isActive = miniGame.IsHintActive();
        canvasGroup.SetVisible(isActive);
        if (!isActive)
        {
            timer = 0f;
            return;
        }
        timer += Time.unscaledDeltaTime;
        ring.position = miniGame.GetHintScreenPoint(Mathf.SmoothStep(0f, 1f, Mathf.PingPong(timer / sweepDuration, 1f)));
        ring.localScale = Vector3.one * (1f + Mathf.Sin(timer * 8f) * .08f);
    }
}
