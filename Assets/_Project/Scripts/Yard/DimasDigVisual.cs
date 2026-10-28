using System;
using UnityEngine;

// Dimas hacking at the river bank with Darto's hoe while he has it: both arms swing it up over his head and chop it down, each
// blow throwing up dirt and growing the pile beside him. He holds still mid-swing while he talks; once he has swapped the hoe
// for the seed it leaves his hands and he stops (the pile stays).
public class DimasDigVisual : MonoBehaviour
{
    // slow lift (0 .. .55), fast chop (.55 .. .68), the blade rests in the soil, then a small tug back up before the next lift
    private const float LIFT_END = .55f;
    private const float STRIKE_END = .68f;

    [SerializeField] private YardErrands errands;
    [SerializeField] private Npc dimasNpc;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject hoeModel;
    [SerializeField] private Transform strikePoint;
    [SerializeField] private Transform dirtMound;
    [SerializeField] private Transform dirtPrefab;
    [SerializeField] private float swingPeriod = 1.3f;
    // -X swings an arm forward from hanging: raised over the head, then the hoe's blade in the soil
    [SerializeField] private float raisedArmAngle = -165f;
    [SerializeField] private float strikeArmAngle = -55f;
    // the hands meet on the handle
    [SerializeField] private float armInwardAngle = 14f;
    [SerializeField] private int blowsToFullMound = 12;

    private Transform armL;
    private Transform armR;
    private float swingTimer;
    private bool isTalking;
    private int blowCount;
    private Vector3 moundFullScale;

    private void Start()
    {
        DialogueManager.Instance.OnDialogueStarted += DialogueManager_OnDialogueStarted;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        armL = animator.transform.Find("Hips/ArmL");
        armR = animator.transform.Find("Hips/ArmR");
        moundFullScale = dirtMound.localScale;
        dirtMound.localScale = moundFullScale * .4f;
    }

    // after the Animator, so the swing wins over the idle clip
    private void LateUpdate()
    {
        if (!errands.HasDimasGotHoe())
        {
            if (hoeModel.activeSelf) hoeModel.SetActive(false);
            return;
        }
        if (!isTalking) Swing();
        float t = swingTimer / swingPeriod;
        SetArms(isTalking ? strikeArmAngle : GetArmAngle(t));
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.OnDialogueStarted -= DialogueManager_OnDialogueStarted;
        DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
    }

    private void DialogueManager_OnDialogueStarted(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (e.npc == dimasNpc) isTalking = true;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (e.npc == dimasNpc) isTalking = false;
    }

    private void Swing()
    {
        float before = swingTimer / swingPeriod;
        swingTimer = (swingTimer + Time.deltaTime) % swingPeriod;
        float after = swingTimer / swingPeriod;
        // the blade bites at the bottom of the chop
        if (before < STRIKE_END && (after >= STRIKE_END || after < before)) Strike();
    }

    private float GetArmAngle(float t)
    {
        if (t < LIFT_END) return Mathf.Lerp(strikeArmAngle, raisedArmAngle, Mathf.SmoothStep(0f, 1f, t / LIFT_END));
        if (t < STRIKE_END)
        {
            float chop = (t - LIFT_END) / (STRIKE_END - LIFT_END);
            return Mathf.Lerp(raisedArmAngle, strikeArmAngle, chop * chop);
        }
        return strikeArmAngle - Mathf.Sin((t - STRIKE_END) / (1f - STRIKE_END) * Mathf.PI) * 8f;
    }

    private void SetArms(float armAngle)
    {
        if (armL == null || armR == null) return;
        armL.localRotation = Quaternion.Euler(armAngle, 0f, armInwardAngle);
        armR.localRotation = Quaternion.Euler(armAngle, 0f, -armInwardAngle);
    }

    private void Strike()
    {
        Instantiate(dirtPrefab, strikePoint.position, Quaternion.identity);
        blowCount = Math.Min(blowCount + 1, blowsToFullMound);
        dirtMound.localScale = moundFullScale * Mathf.Lerp(.4f, 1f, (float)blowCount / blowsToFullMound);
    }
}
