using UnityEngine;

// Bends the Chibi rig into the cutscene poses it has no clip for (eating, pounding the ground, punching a tree, adjusting glasses,
// drowning), after the Animator so the pose wins over idle/walk. A punch slams the raised arms down (or drives a fist forward) and
// lets them swing back.
public class CutscenePoseVisual : MonoBehaviour
{
    [SerializeField] private CutsceneActor cutsceneActor;
    [SerializeField] private Animator animator;
    [SerializeField] private float blendSpeed = 6f;
    [SerializeField] private float punchDuration = .3f;
    [Tooltip("How far the hips drop in a crouch (metres).")]
    [SerializeField] private float crouchDrop = .22f;
    [Tooltip("Seconds a drowning character thrashes at the surface before going under (River drifts them meanwhile).")]
    [SerializeField] private float drownSinkDelay = 2f;
    [Tooltip("How fast (m/s) and how far (m) the body then sinks.")]
    [SerializeField] private float drownSinkSpeed = .8f;
    [SerializeField] private float drownSinkDepth = 1.1f;

    private Transform hips;
    private Transform head;
    private Transform armL;
    private Transform armR;
    private Vector3 hipsRestLocalPosition;
    private Quaternion hipsRestLocalRotation;
    private CutscenePose shownPose;
    private float weight;
    private float punchTimer;
    private int punchCount;
    // seconds the shown pose has been held (drowning sinks over time)
    private float poseTimer;

    private void Start()
    {
        hips = animator.transform.Find("Hips");
        head = animator.transform.Find("Hips/Head");
        armL = animator.transform.Find("Hips/ArmL");
        armR = animator.transform.Find("Hips/ArmR");
        hipsRestLocalPosition = hips.localPosition;
        hipsRestLocalRotation = hips.localRotation;
        cutsceneActor.OnPunch += CutsceneActor_OnPunch;
    }

    private void LateUpdate()
    {
        CutscenePose pose = cutsceneActor.GetPose();
        // fade out of the last pose before showing a new one
        if (pose != shownPose && weight <= 0f)
        {
            shownPose = pose;
            poseTimer = 0f;
        }
        poseTimer += Time.deltaTime;
        weight = Mathf.MoveTowards(weight, pose == shownPose && pose != CutscenePose.None ? 1f : 0f, blendSpeed * Time.deltaTime);
        punchTimer = Mathf.Max(0f, punchTimer - Time.deltaTime);
        if (weight <= 0f) return;

        // 1 the instant a punch lands, back to 0 as the arm swings up again
        float punch = punchTimer / punchDuration;
        bool isLeftPunch = punchCount % 2 == 0;
        Vector3 headEuler = Vector3.zero, armLEuler = new Vector3(0f, 0f, -6f), armREuler = new Vector3(0f, 0f, 6f);
        float drop = 0f;
        float lean = 0f;
        switch (shownPose)
        {
            case CutscenePose.Realise:
                // looks up, one hand raised in front: an idea
                headEuler = new Vector3(-12f, 0f, 0f);
                armREuler = new Vector3(-125f, 0f, -12f);
                break;
            case CutscenePose.Eat:
                // both hands at the mouth, chewing
                headEuler = new Vector3(Mathf.Sin(Time.time * 18f) * 7f, 0f, 0f);
                armLEuler = new Vector3(-120f, 0f, 18f);
                armREuler = new Vector3(-120f, 0f, -18f);
                break;
            case CutscenePose.Crouch:
                // fists raised, one slams the ground per punch (left, right, left...)
                // the whole body drops into each blow
                drop = crouchDrop + punch * .12f;
                // hunched over the ground, thrown further forward by each blow
                lean = 20f + punch * 12f;
                headEuler = new Vector3(18f + punch * 12f, 0f, 0f);
                armLEuler = new Vector3(Mathf.Lerp(-150f, -25f, isLeftPunch ? punch : 0f), 0f, -10f);
                armREuler = new Vector3(Mathf.Lerp(-150f, -25f, isLeftPunch ? 0f : punch), 0f, 10f);
                break;
            case CutscenePose.OverheadSmash:
                // both fists high over the head, then the double-handed slam
                drop = crouchDrop * .5f + punch * .2f;
                lean = Mathf.Lerp(-12f, 20f, punch);
                headEuler = new Vector3(Mathf.Lerp(-10f, 22f, punch), 0f, 0f);
                armLEuler = new Vector3(Mathf.Lerp(-178f, -30f, punch), 0f, -4f);
                armREuler = new Vector3(Mathf.Lerp(-178f, -30f, punch), 0f, 4f);
                break;
            case CutscenePose.Jab:
                // standing guard, fists up; one fist drives straight out per punch (left, right, left...) and the body follows it
                lean = 6f + punch * 10f;
                headEuler = new Vector3(6f, 0f, 0f);
                armLEuler = Vector3.Lerp(new Vector3(-70f, 0f, -24f), new Vector3(-96f, 0f, 4f), isLeftPunch ? punch : 0f);
                armREuler = Vector3.Lerp(new Vector3(-70f, 0f, 24f), new Vector3(-96f, 0f, -4f), isLeftPunch ? 0f : punch);
                break;
            case CutscenePose.AdjustGlasses:
                // one finger pushing the glasses up the nose
                headEuler = new Vector3(6f, 0f, 0f);
                armREuler = new Vector3(-150f, 0f, -28f);
                break;
            case CutscenePose.Drown:
                // thrashing both arms over the head, face up gasping, bobbing on the current, then pulled under
                float thrash = Mathf.Sin(poseTimer * 16f);
                drop = Mathf.Sin(poseTimer * 5f) * .06f + Mathf.Clamp((poseTimer - drownSinkDelay) * drownSinkSpeed, 0f, drownSinkDepth);
                lean = -10f;
                headEuler = new Vector3(-28f, 0f, thrash * 8f);
                armLEuler = new Vector3(-155f + thrash * 35f, 0f, -22f);
                armREuler = new Vector3(-155f - thrash * 35f, 0f, 22f);
                break;
        }
        head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(headEuler), weight);
        armL.localRotation = Quaternion.Slerp(armL.localRotation, Quaternion.Euler(armLEuler), weight);
        armR.localRotation = Quaternion.Slerp(armR.localRotation, Quaternion.Euler(armREuler), weight);
        Vector3 crouched = hipsRestLocalPosition + hips.parent.InverseTransformVector(Vector3.down * drop);
        hips.localPosition = Vector3.Lerp(hips.localPosition, crouched, weight);
        hips.localRotation = Quaternion.Slerp(hips.localRotation, hipsRestLocalRotation * Quaternion.Euler(lean, 0f, 0f), weight);
    }

    private void CutsceneActor_OnPunch(object sender, System.EventArgs e)
    {
        punchCount++;
        punchTimer = punchDuration;
    }
}
