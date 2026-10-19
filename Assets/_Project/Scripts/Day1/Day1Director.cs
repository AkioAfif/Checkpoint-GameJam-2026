using System;
using UnityEngine;

// Runs "Day 1: The Crisis" in the village, from the truck's arrival to the front door at night (the bedroom morning is
// BedroomDirector in HouseInterior). It listens to the existing systems (truck, Fabricator, tasks, dialogue) and moves
// the story on; characters, camera and lighting react to OnStateChanged.
public class Day1Director : MonoBehaviour
{
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }

    // Arrival: truck cutscene + intro. Tutorial1: gather 3 plants, first press.
    // Tutorial2: plant, gather, press 3 more. Departure: Distributor leaves. Sari: warning. Night: back to the front door.
    public enum State { Arrival, Tutorial1, Tutorial2, Departure, Sari, Night, Done }

    // the sub-steps of the three cutscene-like states
    private enum Step { None, WaitingForTruck, Flipping, Intro, Delivering, Waving, Hopping, Leaving }

    [SerializeField] private Day1SO day1SO;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private DistributorTruck truck;
    [SerializeField] private FabricatorController fabricator;
    [SerializeField] private FabricatorDelivery fabricatorDelivery;
    [SerializeField] private Npc distributorNpc;
    [SerializeField] private CutsceneActor distributorActor;
    [SerializeField] private Npc sariNpc;
    [SerializeField] private GameObject motherGameObject;
    [SerializeField] private HouseDoor houseDoor;
    [SerializeField] private Transform yardPoint;
    [SerializeField] private Transform distributorStandPoint;
    [SerializeField] private Transform nightPlayerPoint;
    [SerializeField] private float flipDuration = .9f;
    [SerializeField] private float waveDuration = 1.8f;
    [SerializeField] private float hopDuration = .8f;

    private State state;
    private Step step;
    private float stepTimer = -1f;
    private bool hasStarted;
    // the only fade in the village hides nightfall
    private bool isNightFadePlaying;
    private bool isPressedDialoguePending;
    private bool isDeparturePending;
    private bool isPressedDialogueActive;
    private int plantedCount;

    private void Start()
    {
        screenFade.OnBlack += ScreenFade_OnBlack;
        screenFade.OnFinished += ScreenFade_OnFinished;
        truck.OnStateChanged += Truck_OnStateChanged;
        distributorActor.OnMoveFinished += DistributorActor_OnMoveFinished;
        fabricatorDelivery.OnDelivered += FabricatorDelivery_OnDelivered;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        TaskManager.Instance.OnRequiredTasksCompleted += TaskManager_OnRequiredTasksCompleted;
        houseDoor.OnStateChanged += HouseDoor_OnStateChanged;
        FabricatorController.OnAnySeedFabricated += FabricatorController_OnAnySeedFabricated;
        FarmPlot.OnAnyPlanted += FarmPlot_OnAnyPlanted;

        // everything that arrives later waits out of sight
        truck.gameObject.SetActive(false);
        distributorNpc.gameObject.SetActive(false);
        sariNpc.gameObject.SetActive(false);
        motherGameObject.SetActive(false);
        fabricator.SetPlaced(false);
        fabricatorDelivery.AttachTo(truck.GetCargoAnchor());
        houseDoor.SetLocked(true);
        // the letter was read in the bedroom (HouseInterior): show it ticked off until the tutorial list replaces it
        TaskManager.Instance.SetObjectives(day1SO.letterObjectiveArray);
        TaskManager.Instance.CompleteStory();
    }

    private void Update()
    {
        // GameManager.Start puts the game in Playing, so the arrival cutscene begins on the first Update
        if (!hasStarted)
        {
            hasStarted = true;
            BeginArrival();
        }

        if (stepTimer >= 0f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer < 0f) StepTimerFinished();
        }

        // beats triggered during free play wait until nothing else (a discovery card, a planting) holds the player
        if (GameManager.Instance.GetState() != GameManager.State.Playing) return;
        if (isPressedDialoguePending)
        {
            isPressedDialoguePending = false;
            isPressedDialogueActive = true;
            DialogueManager.Instance.StartDialogue(distributorNpc, day1SO.pressedDialogueSO);
        }
        else if (isDeparturePending)
        {
            isDeparturePending = false;
            BeginDeparture();
        }
    }

    private void OnDestroy()
    {
        FabricatorController.OnAnySeedFabricated -= FabricatorController_OnAnySeedFabricated;
        FarmPlot.OnAnyPlanted -= FarmPlot_OnAnyPlanted;
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
        if (TaskManager.Instance != null) TaskManager.Instance.OnRequiredTasksCompleted -= TaskManager_OnRequiredTasksCompleted;
    }

    // ---------- night fade ----------

    private void ScreenFade_OnBlack(object sender, EventArgs e)
    {
        if (!isNightFadePlaying) return;
        Player.Instance.Teleport(nightPlayerPoint.position);
        Player.Instance.FaceTowards(houseDoor.transform.position);
        sariNpc.gameObject.SetActive(false);
        motherGameObject.SetActive(true);
        houseDoor.SetLocked(false);
        houseDoor.SetEntryDialogue(day1SO.nightDialogueSO);
        TaskManager.Instance.SetObjectives(day1SO.nightObjectiveArray);
        SetState(State.Night);
    }

    private void ScreenFade_OnFinished(object sender, EventArgs e)
    {
        if (!isNightFadePlaying) return;
        isNightFadePlaying = false;
        GameManager.Instance.SetState(GameManager.State.Playing);
    }

    // ---------- Arrival ----------

    // the player has just stepped out of the house (HouseInterior) as the truck comes in from the west
    private void BeginArrival()
    {
        GameManager.Instance.SetState(GameManager.State.Cutscene);
        Player.Instance.Teleport(yardPoint.position);
        Player.Instance.FaceTowards(yardPoint.position + Vector3.left);
        truck.gameObject.SetActive(true);
        step = Step.WaitingForTruck;
        SetState(State.Arrival);
        truck.Drive();
    }

    private void Truck_OnStateChanged(object sender, DistributorTruck.OnStateChangedEventArgs e)
    {
        if (e.state == DistributorTruck.State.Parked && step == Step.WaitingForTruck)
        {
            // the Distributor tumbles out of the cab and lands by where the machine will stand
            distributorNpc.transform.position = truck.GetDoorPoint().position;
            distributorNpc.gameObject.SetActive(true);
            distributorActor.PlayEmote(Emote.GoofyFlip);
            distributorActor.MoveTo(distributorStandPoint.position, flipDuration, 1.4f);
            step = Step.Flipping;
        }
        else if (e.state == DistributorTruck.State.Hidden && state == State.Departure)
        {
            truck.gameObject.SetActive(false);
            sariNpc.gameObject.SetActive(true);
            TaskManager.Instance.SetObjectives(day1SO.sariObjectiveArray);
            step = Step.None;
            SetState(State.Sari);
            GameManager.Instance.SetState(GameManager.State.Playing);
        }
    }

    private void DistributorActor_OnMoveFinished(object sender, EventArgs e)
    {
        if (step == Step.Flipping)
        {
            step = Step.Intro;
            Player.Instance.FaceTowards(distributorNpc.transform.position);
            DialogueManager.Instance.StartDialogue(distributorNpc, day1SO.introDialogueSO);
        }
        else if (step == Step.Hopping)
        {
            // aboard: gone from the village, the truck burns rubber
            distributorNpc.gameObject.SetActive(false);
            step = Step.Leaving;
            truck.Leave();
        }
    }

    private void FabricatorDelivery_OnDelivered(object sender, EventArgs e)
    {
        fabricator.SetPlaced(true);
        distributorNpc.SetOverrideDialogue(day1SO.waitingTutorial1DialogueSO);
        TaskManager.Instance.SetObjectives(day1SO.tutorialObjectiveArray);
        step = Step.None;
        SetState(State.Tutorial1);
        GameManager.Instance.SetState(GameManager.State.Playing);
    }

    // ---------- dialogue ----------

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (state == State.Arrival && step == Step.Intro && e.npc == distributorNpc)
        {
            step = Step.Delivering;
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            distributorActor.PlayEmote(Emote.GoofySetup);
            fabricatorDelivery.Deliver();
        }
        else if (state == State.Tutorial1 && e.npc == distributorNpc && isPressedDialogueActive)
        {
            isPressedDialogueActive = false;
            distributorNpc.SetOverrideDialogue(day1SO.waitingTutorial2DialogueSO);
            TaskManager.Instance.SetObjectives(day1SO.gatherObjectiveArray);
            SetState(State.Tutorial2);
        }
        else if (state == State.Sari && e.npc == sariNpc)
        {
            TaskManager.Instance.CompleteStory();
            isNightFadePlaying = true;
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            screenFade.Play(day1SO.nightCaption, 1.6f);
        }
    }

    // ---------- tutorial ----------

    private void FabricatorController_OnAnySeedFabricated(object sender, EventArgs e)
    {
        if (state == State.Tutorial1) isPressedDialoguePending = true;
    }

    private void FarmPlot_OnAnyPlanted(object sender, EventArgs e) => plantedCount++;

    private void TaskManager_OnRequiredTasksCompleted(object sender, EventArgs e)
    {
        if (state == State.Tutorial2) isDeparturePending = true;
    }

    private void BeginDeparture()
    {
        GameManager.Instance.SetState(GameManager.State.Cutscene);
        Player.Instance.FaceTowards(distributorNpc.transform.position);
        step = Step.Waving;
        SetState(State.Departure);
        distributorActor.PlayEmote(Emote.Wave);
        stepTimer = waveDuration;
    }

    private void StepTimerFinished()
    {
        if (step != Step.Waving) return;
        // he hops back aboard
        step = Step.Hopping;
        distributorActor.PlayEmote(Emote.Hop);
        distributorActor.MoveTo(truck.GetDoorPoint().position, hopDuration, 1f);
    }

    // ---------- night ----------

    private void HouseDoor_OnStateChanged(object sender, HouseDoor.OnStateChangedEventArgs e)
    {
        if (state != State.Night || e.state != HouseDoor.State.IrisClosing) return;
        WorldState.CompleteDay1(plantedCount);
        SetState(State.Done);
    }

    public State GetState() => state;

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }
}
