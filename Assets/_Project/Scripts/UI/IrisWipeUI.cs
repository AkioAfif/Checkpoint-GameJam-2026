using UnityEngine;

// Iris wipe for entering the house: a circle centred on the player shrinks until the screen is black.
// The hole is a sprite with a transparent circle; four black bars glued to its edges cover the rest of the screen.
public class IrisWipeUI : MonoBehaviour
{
    [SerializeField] private HouseDoor houseDoor;
    [SerializeField] private RectTransform hole;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Vector3 playerFocusOffset = new Vector3(0f, .9f, 0f);
    [SerializeField] private float holdSize = 300f;

    private float duration;
    private float timer = -1f;
    private float startSize;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private void Start()
    {
        houseDoor.OnStateChanged += HouseDoor_OnStateChanged;
    }

    private void LateUpdate()
    {
        if (timer < 0f) return;
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        hole.position = Camera.main.WorldToScreenPoint(Player.Instance.transform.position + playerFocusOffset);
        // shrink onto the character, hold a beat (classic cartoon ending), then close
        float size;
        if (t < .6f) size = Mathf.Lerp(startSize, holdSize, 1f - Mathf.Pow(1f - t / .6f, 3f));
        else if (t < .8f) size = holdSize;
        else size = Mathf.Lerp(holdSize, 0f, (t - .8f) / .2f);
        hole.sizeDelta = Vector2.one * size;
    }

    private void HouseDoor_OnStateChanged(object sender, HouseDoor.OnStateChangedEventArgs e)
    {
        if (e.state != HouseDoor.State.IrisClosing) return;
        duration = e.duration;
        timer = 0f;
        startSize = new Vector2(Screen.width, Screen.height).magnitude * 2.2f / transform.lossyScale.x;
        hole.sizeDelta = Vector2.one * startSize;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
}
