using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Dialogue box with a typewriter line, speaker name, and the two 3D portraits (player top-left, NPC top-right).
[RequireComponent(typeof(CanvasGroup))]
public class DialogueUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lineText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image nameTagImage;
    [SerializeField] private GameObject continueIndicator;
    [SerializeField] private UIPop boxPop;
    [SerializeField] private string playerName = "You";
    [SerializeField] private Color playerTagColor = new Color(.52f, .67f, .88f);

    private CanvasGroup canvasGroup;
    private Color npcTagColor;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    private void Start()
    {
        DialogueManager.Instance.OnDialogueStarted += DialogueManager_OnDialogueStarted;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        DialogueManager.Instance.OnLineChanged += DialogueManager_OnLineChanged;
    }

    private void Update()
    {
        if (!DialogueManager.Instance.IsActive()) return;
        lineText.maxVisibleCharacters = DialogueManager.Instance.GetVisibleCharacterCount();
        bool isRevealed = DialogueManager.Instance.IsLineFullyRevealed();
        continueIndicator.SetActive(isRevealed);
        if (isRevealed) continueIndicator.transform.localPosition = new Vector3(0f, Mathf.Sin(Time.unscaledTime * 6f) * 4f, 0f);
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.OnDialogueStarted -= DialogueManager_OnDialogueStarted;
        DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
        DialogueManager.Instance.OnLineChanged -= DialogueManager_OnLineChanged;
    }

    private void DialogueManager_OnDialogueStarted(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        NpcSO npcSO = e.npc.GetNpcSO();
        npcTagColor = Color.Lerp(npcSO.portraitBackground, Color.black, .25f);
        canvasGroup.SetVisible(true);
        boxPop.Play(.7f);
    }

    private void DialogueManager_OnDialogueEnded(object sender, EventArgs e) => Hide();

    private void DialogueManager_OnLineChanged(object sender, DialogueManager.OnLineChangedEventArgs e)
    {
        bool isPlayer = e.line.speaker == DialogueSO.Speaker.Player;
        nameText.text = isPlayer ? playerName : e.npc.GetNpcSO().npcName;
        nameTagImage.color = isPlayer ? playerTagColor : npcTagColor;
        lineText.text = e.line.text;
        lineText.maxVisibleCharacters = 0;
    }

    private void Hide() => canvasGroup.SetVisible(false);
}
