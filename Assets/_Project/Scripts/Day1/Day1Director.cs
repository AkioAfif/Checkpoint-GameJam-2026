using System;
using UnityEngine;

// Runs "Day 1: The Crisis" in the village, from the truck's arrival to the front door at night (the bedroom morning is
// BedroomDirector in HouseInterior). It listens to the existing systems (truck, Fabricator, tasks, dialogue) and moves
// the story on; characters, camera and lighting react to OnStateChanged.
public class Day1Director : MonoBehaviour
{
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }

    // Stroll: a few free steps out of the door. Arrival: truck cutscene + intro. Tutorial1: gather 3 plants, first press.
    // Tutorial2: plant, gather, press 3 more, then say goodbye. Departure: Distributor leaves. Sari: warning. Night: back to the front door.
    public enum State { Stroll, Arrival, Tutorial1, Tutorial2, Departure, Sari, Night, Done }

    // the sub-steps of the three cutscene-like states
    private enum Step { None, Startled, WaitingForTruck, Flipping, Intro, WalkingToTruck, Lifting, Carrying, Placing, Waving, Walking, Hopping, Leaving }

    [SerializeField] private Day1SO day1SO;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private DistributorTruck truck;
    [SerializeField] private FabricatorController fabricator;
    [SerializeField] private FabricatorDelivery fabricatorDelivery;
    [SerializeField] private Npc distributorNpc;
    [SerializeField] private CutsceneActor distributorActor;
    [SerializeField] private CutsceneActor playerActor;
    [SerializeField] private Npc sariNpc;
    [SerializeField] private GameObject motherGameObject;
    [SerializeField] private HouseDoor houseDoor;
    [SerializeField] private Transform yardPoint;
    [Tooltip("Where the Fabricator rides in the Distributor's arms.")]
    [SerializeField] private Transform distributorCarryPoint;
    [SerializeField] private Transform nightPlayerPoint;
    [SerializeField] private float strollDistance = 3f;
    [SerializeField] private float startleDuration = .7f;
    [SerializeField] private float flipDuration = .9f;
    [Tooltip("The Distributor lands this far from wherever the player stopped, so every intro is framed the same.")]
    [SerializeField] private float talkDistance = 1.8f;
    [SerializeField] private float carryWalkSpeed = 1.6f;
    [Tooltip("How far from the machine's spot he stops to set it down.")]
    [SerializeField] private float placeStandDistance = 1.9f;
    [SerializeField] private float waveDuration = 1.8f;
    [SerializeField] private float walkSpeed = 2.2f;
    [SerializeField] private float hopDuration = .6f;

    private State state;
    private Step step;
    private float stepTimer = -1f;
    // the only fade in the village hides nightfall
    private bool isNightFadePlaying;
    private bool isPressedDialoguePending;
    private bool isFarewellPending;
    private bool isFarewellReady;
    private bool isPressedDialogueActive;
    private int plantedCount;

    private void Start()
    {
        screenFade.OnBlack += ScreenFade_OnBlack;
        screenFade.OnFinished += ScreenFade_OnFinished;
        truck.OnStateChanged += Truck_OnStateChanged;
        distributorActor.OnMoveFinished += DistributorActor_OnMoveFinished;
        fabricatorDelivery.OnPickedUp += FabricatorDelivery_OnPickedUp;
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
        // no "Finish today's tasks first" over the player's head during the opening
        houseDoor.SetHidden(true);
        // the letter was read in the bedroom (HouseInterior): show it ticked off until the tutorial list replaces it
        TaskManager.Instance.SetObjectives(day1SO.letterObjectiveArray);
        TaskManager.Instance.CompleteStory();
        // just stepped out of the front door, facing the camera
        Player.Instance.Teleport(yardPoint.position);
        Player.Instance.FaceTowards(yardPoint.position + yardPoint.forward);
        SetState(State.Stroll);
    }

    private void Update()
    {
        // a few free steps out of the door, then the truck comes roaring in
        if (state == State.Stroll && GameManager.Instance.IsGamePlaying() && Vector3.Distance(Player.Instance.transform.position, yardPoint.position) >= strollDistance)
            BeginArrival();

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
        else if (isFarewellPending)
        {
            isFarewellPending = false;
            isFarewellReady = true;
            distributorNpc.SetOverrideDialogue(day1SO.farewellDialogueSO);
            TaskManager.Instance.SetObjectives(day1SO.talkObjectiveArray);
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

    // the player hears the truck coming in from the west and jumps
    private void BeginArrival()
    {
        GameManager.Instance.SetState(GameManager.State.Cutscene);
        Player.Instance.FaceTowards(Player.Instance.transform.position + Vector3.left);
        playerActor.PlayEmote(Emote.Surprised);
        playerActor.MoveTo(Player.Instance.transform.position, .45f, .5f);
        step = Step.Startled;
        stepTimer = startleDuration;
    }

    private void Truck_OnStateChanged(object sender, DistributorTruck.OnStateChangedEventArgs e)
    {
        if (e.state == DistributorTruck.State.Parked && step == Step.WaitingForTruck)
        {
            // the Distributor tumbles out of the cab and lands a talking distance from the player
            Vector3 door = truck.GetDoorPoint().position;
            Vector3 playerPosition = Player.Instance.transform.position;
            Vector3 toDoor = door - playerPosition;
            toDoor.y = 0f;
            if (toDoor.sqrMagnitude < .01f) toDoor = Vector3.left;
            distributorNpc.transform.position = door;
            distributorNpc.gameObject.SetActive(true);
            distributorActor.PlayEmote(Emote.GoofyFlip);
            distributorActor.MoveTo(playerPosition + toDoor.normalized * talkDistance, flipDuration, 1.4f);
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
        else if (step == Step.WalkingToTruck)
        {
            // at the tailgate: lift the machine out of the bed
            step = Step.Lifting;
            distributorActor.FaceTowards(truck.GetCargoAnchor().position);
            distributorActor.SetCarrying(true);
            fabricatorDelivery.PickUp(distributorCarryPoint);
        }
        else if (step == Step.Carrying)
        {
            step = Step.Placing;
            distributorActor.FaceTowards(fabricatorDelivery.GetPlacePoint().position);
            fabricatorDelivery.PutDown();
        }
        else if (step == Step.Walking)
        {
            // at the cab door: one hop and he's in
            step = Step.Hopping;
            distributorActor.PlayEmote(Emote.Hop);
            distributorActor.MoveTo(distributorNpc.transform.position, hopDuration, 1f);
        }
        else if (step == Step.Hopping)
        {
            // aboard: gone from the village, the truck burns rubber
            distributorNpc.gameObject.SetActive(false);
            step = Step.Leaving;
            truck.Leave();
        }
    }

    private void FabricatorDelivery_OnPickedUp(object sender, EventArgs e)
    {
        // carry it to the front of the house, stopping just short of its spot
        step = Step.Carrying;
        Vector3 place = fabricatorDelivery.GetPlacePoint().position;
        Vector3 fromPlace = distributorNpc.transform.position - place;
        fromPlace.y = 0f;
        Vector3 dropPosition = place + fromPlace.normalized * placeStandDistance;
        distributorActor.MoveTo(dropPosition, Vector3.Distance(distributorNpc.transform.position, dropPosition) / carryWalkSpeed, 0f);
    }

    private void FabricatorDelivery_OnDelivered(object sender, EventArgs e)
    {
        distributorActor.SetCarrying(false);
        fabricator.SetPlaced(true);
        houseDoor.SetHidden(false);
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
            // he walks round to the tailgate to fetch the machine himself
            step = Step.WalkingToTruck;
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            Vector3 tailgate = truck.GetTailgatePoint().position;
            distributorActor.MoveTo(tailgate, Vector3.Distance(distributorNpc.transform.position, tailgate) / walkSpeed, 0f);
        }
        else if (state == State.Tutorial1 && e.npc == distributorNpc && isPressedDialogueActive)
        {
            isPressedDialogueActive = false;
            distributorNpc.SetOverrideDialogue(day1SO.waitingTutorial2DialogueSO);
            TaskManager.Instance.SetObjectives(day1SO.gatherObjectiveArray);
            SetState(State.Tutorial2);
        }
        else if (state == State.Tutorial2 && e.npc == distributorNpc && isFarewellReady)
        {
            isFarewellReady = false;
            // leave Tutorial2 first so completing the talk objective doesn't re-arm the farewell
            BeginDeparture();
            TaskManager.Instance.CompleteStory();
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
        if (state == State.Tutorial2 && !isFarewellReady) isFarewellPending = true;
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
        if (step == Step.Startled)
        {
            truck.gameObject.SetActive(true);
            step = Step.WaitingForTruck;
            SetState(State.Arrival);
            truck.Drive();
        }
        else if (step == Step.Waving)
        {
            // he strolls back to the cab door
            Vector3 door = truck.GetDoorPoint().position;
            step = Step.Walking;
            distributorActor.MoveTo(door, Vector3.Distance(distributorNpc.transform.position, door) / walkSpeed, 0f);
        }
    }

    // ---------- night ----------

    private void HouseDoor_OnStateChanged(object sender, HouseDoor.OnStateChangedEventArgs e)
    {
        if (state != State.Night || e.state != HouseDoor.State.IrisClosing) return;
        WorldState.CompleteDay1(plantedCount);
        SetState(State.Done);
    }

    public State GetState() => state;
    // the Distributor is fetching, carrying or setting down the Fabricator
    public bool IsDelivering() => step == Step.WalkingToTruck || step == Step.Lifting || step == Step.Carrying || step == Step.Placing;

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }
}
