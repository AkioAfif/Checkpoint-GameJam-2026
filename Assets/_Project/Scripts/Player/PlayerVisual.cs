using System;
using UnityEngine;

// Walk/idle animation, squash-and-stretch on start/stop, footstep dust, and the planting poses.
public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem dustParticleSystem;
    [SerializeField] private CutsceneActor cutsceneActor;
    [SerializeField] private float dustInterval = .22f;
    [SerializeField] private float squashAmount = .12f;
    [SerializeField] private float squashSpeed = 9f;

    private bool wasMoving;
    private float dustTimer;
    private float squash;

    private void Start()
    {
        PlantingController.Instance.OnStateChanged += PlantingController_OnStateChanged;
        PlantingController.Instance.OnPlantingFinished += PlantingController_OnPlantingFinished;
        QTEManager.Instance.OnCheckResolved += QTEManager_OnCheckResolved;
        DialogueManager.Instance.OnLineChanged += DialogueManager_OnLineChanged;
        if (cutsceneActor != null) cutsceneActor.OnEmoteRequested += CutsceneActor_OnEmoteRequested;
    }

    private void Update()
    {
        // the director walks the player through cutscenes without input
        bool isCutsceneWalking = cutsceneActor != null && cutsceneActor.IsWalking();
        bool isMoving = isCutsceneWalking || player.IsMoving();
        float speedNormalized = isCutsceneWalking ? 1f : player.GetSpeedNormalized();
        animator.SetFloat(ChibiAnimation.SPEED, speedNormalized);

        // a quick squash when starting to walk and a stretch when stopping
        if (isMoving != wasMoving) squash = isMoving ? -squashAmount : squashAmount;
        wasMoving = isMoving;
        squash = Mathf.MoveTowards(squash, 0f, squashSpeed * squashAmount * Time.deltaTime);
        transform.localScale = new Vector3(1f - squash * .5f, 1f + squash, 1f - squash * .5f);

        if (!isMoving) return;
        dustTimer -= Time.deltaTime * speedNormalized;
        if (dustTimer > 0f) return;
        dustTimer = dustInterval;
        dustParticleSystem.Emit(1);
    }

    private void OnDestroy()
    {
        if (PlantingController.Instance != null)
        {
            PlantingController.Instance.OnStateChanged -= PlantingController_OnStateChanged;
            PlantingController.Instance.OnPlantingFinished -= PlantingController_OnPlantingFinished;
        }
        if (QTEManager.Instance != null) QTEManager.Instance.OnCheckResolved -= QTEManager_OnCheckResolved;
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnLineChanged -= DialogueManager_OnLineChanged;
    }

    // the world model reacts like the portrait does
    private void DialogueManager_OnLineChanged(object sender, DialogueManager.OnLineChangedEventArgs e) => ChibiAnimation.PlayEmote(animator, e.line.playerEmote);

    private void CutsceneActor_OnEmoteRequested(object sender, CutsceneActor.OnEmoteRequestedEventArgs e) => ChibiAnimation.PlayEmote(animator, e.emote);

    private void PlantingController_OnStateChanged(object sender, PlantingController.OnStateChangedEventArgs e)
    {
        animator.SetBool(ChibiAnimation.IS_CROUCHING, PlantingController.Instance.IsCrouched());
    }

    private void PlantingController_OnPlantingFinished(object sender, PlantingController.OnPlantingFinishedEventArgs e)
    {
        ChibiAnimation.PlayEmote(animator, e.isSuccess ? Emote.Cheer : Emote.Sad);
    }

    private void QTEManager_OnCheckResolved(object sender, QTEManager.OnCheckResolvedEventArgs e)
    {
        if (e.result != QTEManager.Result.Miss) ChibiAnimation.PlayEmote(animator, Emote.Dig);
    }
}
