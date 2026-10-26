using UnityEngine;

// The main character's 2D face: shows face-sheet cells (Art/Models/Meadow/MC_Face.png, 4x4) for the emote the Animator is playing,
// blinks, and flaps the mouth while the player's own dialogue line types out. It reads the Animator state, so every emote source
// (dialogue, cutscenes, planting, QTE) drives it. Sits on Rig_Player, so the dialogue portrait copy gets it too.
[RequireComponent(typeof(Animator))]
public class PlayerFaceVisual : MonoBehaviour
{
    // cell order of the sheet (Tools/Blender/make_face_sheet.py): eyes fill rows 0-1, mouths rows 2-3
    private enum Eyes { Open, HalfBlink, Closed, Happy, Surprised, Angry, Sad, Squint }
    private enum Mouth { Smile, TalkSmall, TalkWide, Grin, Frown, Flat, Shock, Wavy }

    private static readonly int BASE_MAP_ST = Shader.PropertyToID("_BaseMap_ST");

    [SerializeField] private SkinnedMeshRenderer faceRenderer;
    [SerializeField] private Material eyeLeftMaterial;
    [SerializeField] private Material eyeRightMaterial;
    [SerializeField] private Material mouthMaterial;
    [SerializeField] private float blinkIntervalMin = 2f;
    [SerializeField] private float blinkIntervalMax = 5f;
    [SerializeField] private float blinkDuration = .15f;
    [SerializeField] private float talkFrameTime = .09f;

    private Animator animator;
    // the player's, for held poses; the portrait booth copy has none
    private CutsceneActor cutsceneActor;
    private MaterialPropertyBlock block;
    private int eyeLeftIndex, eyeRightIndex, mouthIndex;
    private Emote[] emoteArray;
    private int[] emoteStateHashArray;
    private float blinkTimer;
    private Eyes shownEyes;
    private Mouth shownMouth;
    private bool hasShownFace;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        cutsceneActor = GetComponentInParent<CutsceneActor>();
        block = new MaterialPropertyBlock();
        if (faceRenderer == null)
        {
            Debug.LogError($"{name}: faceRenderer not assigned (Tools > Characters > Rebuild Player Rig wires it)", this);
            enabled = false;
            return;
        }
        eyeLeftIndex = System.Array.IndexOf(faceRenderer.sharedMaterials, eyeLeftMaterial);
        eyeRightIndex = System.Array.IndexOf(faceRenderer.sharedMaterials, eyeRightMaterial);
        mouthIndex = System.Array.IndexOf(faceRenderer.sharedMaterials, mouthMaterial);
        if (eyeLeftIndex < 0 || eyeRightIndex < 0 || mouthIndex < 0) Debug.LogError($"{name}: face materials are not on the face renderer", this);
        // emote states in Chibi.controller are named after their triggers
        emoteArray = (Emote[])System.Enum.GetValues(typeof(Emote));
        emoteStateHashArray = new int[emoteArray.Length];
        for (int i = 0; i < emoteArray.Length; i++) emoteStateHashArray[i] = Animator.StringToHash(emoteArray[i].ToString());
        blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
    }

    private void Update()
    {
        GetFace(GetPlayingEmote(), out Eyes eyes, out Mouth mouth);
        // the Surprised emote ends long before the swept-away drowning does: keep its face for the whole drift
        if (cutsceneActor != null && cutsceneActor.GetPose() == CutscenePose.Drown) GetFace(Emote.Surprised, out eyes, out mouth);

        blinkTimer -= Time.deltaTime;
        if (blinkTimer < 0f)
        {
            // half -> closed -> half, only from open eyes
            float t = -blinkTimer / blinkDuration;
            if (eyes == Eyes.Open) eyes = t > .3f && t < .7f ? Eyes.Closed : Eyes.HalfBlink;
            if (t >= 1f) blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
        }

        if (IsPlayerSpeaking())
        {
            int frame = Mathf.FloorToInt(Time.time / talkFrameTime) % 3;
            mouth = frame == 0 ? Mouth.TalkWide : frame == 1 ? Mouth.TalkSmall : mouth;
        }

        if (hasShownFace && eyes == shownEyes && mouth == shownMouth) return;
        hasShownFace = true;
        shownEyes = eyes;
        shownMouth = mouth;
        SetCell(eyeLeftIndex, (int)eyes);
        SetCell(eyeRightIndex, (int)eyes);
        SetCell(mouthIndex, 8 + (int)mouth);
    }

    private Emote GetPlayingEmote()
    {
        // during the blend into an emote, show its face already
        AnimatorStateInfo state = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
        for (int i = 0; i < emoteArray.Length; i++)
        {
            if (emoteStateHashArray[i] == state.shortNameHash) return emoteArray[i];
        }
        return Emote.None;
    }

    private static void GetFace(Emote emote, out Eyes eyes, out Mouth mouth)
    {
        switch (emote)
        {
            case Emote.Nod: eyes = Eyes.Happy; mouth = Mouth.Smile; break;
            case Emote.Hop:
            case Emote.Wave:
            case Emote.Cheer:
            case Emote.WideSmile:
            case Emote.GoofyFlip: eyes = Eyes.Happy; mouth = Mouth.Grin; break;
            case Emote.Shake: eyes = Eyes.Closed; mouth = Mouth.Flat; break;
            case Emote.Surprised:
            case Emote.Dodge: eyes = Eyes.Surprised; mouth = Mouth.Shock; break;
            case Emote.Sad: eyes = Eyes.Sad; mouth = Mouth.Frown; break;
            case Emote.Dig: eyes = Eyes.Squint; mouth = Mouth.Flat; break;
            case Emote.GoofySetup: eyes = Eyes.Squint; mouth = Mouth.Wavy; break;
            case Emote.WakeUp: eyes = Eyes.HalfBlink; mouth = Mouth.Wavy; break;
            case Emote.HeavySigh: eyes = Eyes.Closed; mouth = Mouth.Shock; break;
            case Emote.TiltHead: eyes = Eyes.Open; mouth = Mouth.Wavy; break;
            default: eyes = Eyes.Open; mouth = Mouth.Smile; break;
        }
    }

    private static bool IsPlayerSpeaking()
    {
        DialogueManager dialogue = DialogueManager.Instance;
        return dialogue != null && dialogue.IsActive() && dialogue.GetCurrentLine().speaker == DialogueSO.Speaker.Player && !dialogue.IsLineFullyRevealed();
    }

    // the patch UVs sit on cell 0; the offset moves them to 'cell' (column = cell % 4, row = cell / 4, rows counted from the top)
    private void SetCell(int materialIndex, int cell)
    {
        block.Clear();
        block.SetVector(BASE_MAP_ST, new Vector4(1f, 1f, (cell % 4) * .25f, -(cell / 4) * .25f));
        faceRenderer.SetPropertyBlock(block, materialIndex);
    }
}
