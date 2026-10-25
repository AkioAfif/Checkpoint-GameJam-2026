using UnityEngine;

// The Distributor's face shape keys: glares, squints and talks through his dialogue lines, blinks, jowls breathe while idle,
// and his glasses flash opaque white on lines marked isNpcGlaring.
public class DistributorFaceVisual : MonoBehaviour
{
    private const string BROW_ANGRY = "BrowAngry";
    private const string SNEER = "Sneer";
    private const string MOUTH_OPEN = "MouthOpen";
    private const string JOWL_SAG = "JowlSag";
    private const string EYE_BLINK = "EyeBlink";
    private const string EYE_SQUINT = "EyeSquint";
    private const string LENS_GLARE = "LensGlare";

    [SerializeField] private SkinnedMeshRenderer faceRenderer;
    [SerializeField] private float easeSpeed = 8f;
    [SerializeField] private float blinkIntervalMin = 2f;
    [SerializeField] private float blinkIntervalMax = 5f;
    [SerializeField] private float blinkDuration = .14f;
    [SerializeField] private float talkingSquint = 35f;

    // null on the dialogue portrait copy, which only exists during his own conversation
    private Npc npc;
    private int browIndex, sneerIndex, mouthIndex, jowlIndex, blinkIndex, squintIndex, glareIndex;
    private float brow, sneer, mouth, squint, glare;
    private float blinkTimer;
    private bool isTalking;

    private void Awake()
    {
        npc = GetComponentInParent<Npc>();
        Mesh mesh = faceRenderer.sharedMesh;
        browIndex = mesh.GetBlendShapeIndex(BROW_ANGRY);
        sneerIndex = mesh.GetBlendShapeIndex(SNEER);
        mouthIndex = mesh.GetBlendShapeIndex(MOUTH_OPEN);
        jowlIndex = mesh.GetBlendShapeIndex(JOWL_SAG);
        blinkIndex = mesh.GetBlendShapeIndex(EYE_BLINK);
        squintIndex = mesh.GetBlendShapeIndex(EYE_SQUINT);
        glareIndex = mesh.GetBlendShapeIndex(LENS_GLARE);
        if (browIndex < 0 || sneerIndex < 0 || mouthIndex < 0 || jowlIndex < 0 || blinkIndex < 0 || squintIndex < 0 || glareIndex < 0)
            Debug.LogError($"{name}: face mesh is missing shape keys", this);
        blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
    }

    private void Start()
    {
        DialogueManager.Instance.OnLineChanged += DialogueManager_OnLineChanged;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        // the portrait copy is spawned after its conversation's first line was already raised
        isTalking = npc == null && DialogueManager.Instance.IsActive();
        if (isTalking) sneer = 100f;
    }

    private void Update()
    {
        float t = Time.time;
        DialogueManager dialogue = DialogueManager.Instance;
        bool isInLine = isTalking && dialogue.IsActive();
        bool isSpeaking = isInLine && dialogue.GetCurrentLine().speaker == DialogueSO.Speaker.Npc && !dialogue.IsLineFullyRevealed();
        bool isGlaring = isInLine && dialogue.GetCurrentLine().isNpcGlaring;
        float ease = 1f - Mathf.Exp(-easeSpeed * Time.deltaTime);
        brow = Mathf.Lerp(brow, isTalking ? 100f : 0f, ease);
        sneer = Mathf.Lerp(sneer, 0f, ease * .25f);
        mouth = Mathf.Lerp(mouth, isSpeaking ? 50f + 50f * Mathf.Sin(t * 18f) : 0f, ease * 2f);
        squint = Mathf.Lerp(squint, isTalking ? talkingSquint : 0f, ease);
        glare = Mathf.Lerp(glare, isGlaring ? 100f : 0f, ease * 2f);

        blinkTimer -= Time.deltaTime;
        if (blinkTimer < -blinkDuration) blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);

        faceRenderer.SetBlendShapeWeight(browIndex, brow);
        faceRenderer.SetBlendShapeWeight(sneerIndex, sneer);
        faceRenderer.SetBlendShapeWeight(mouthIndex, Mathf.Max(0f, mouth));
        faceRenderer.SetBlendShapeWeight(jowlIndex, 30f + 30f * Mathf.Sin(t * 1.6f));
        faceRenderer.SetBlendShapeWeight(blinkIndex, blinkTimer < 0f ? 100f : 0f);
        faceRenderer.SetBlendShapeWeight(squintIndex, squint);
        faceRenderer.SetBlendShapeWeight(glareIndex, glare);
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.OnLineChanged -= DialogueManager_OnLineChanged;
        DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
    }

    private void DialogueManager_OnLineChanged(object sender, DialogueManager.OnLineChangedEventArgs e)
    {
        if (npc != null && e.npc != npc) return;
        isTalking = true;
        if (e.line.speaker == DialogueSO.Speaker.Npc) sneer = 100f;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (npc != null && e.npc != npc) return;
        isTalking = false;
    }
}
