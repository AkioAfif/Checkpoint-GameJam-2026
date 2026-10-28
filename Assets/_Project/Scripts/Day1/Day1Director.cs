using System;
using System.Collections.Generic;
using UnityEngine;

// Runs "Day 1: The Crisis" in the village, from the truck's arrival to the evening at the farm and the front door (the bedroom
// morning is BedroomDirector in HouseInterior). It listens to the existing systems (truck, Fabricator, tasks, dialogue) and moves
// the story on; characters, camera and lighting react to OnStateChanged.
public class Day1Director : MonoBehaviour
{
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }

    // Stroll: a few free steps out of the door. Arrival: truck cutscene + intro. Tutorial1: gather 3 plants, first press.
    // Tutorial2: plant, gather, press 3 more, then say goodbye. Departure: Distributor leaves, Sari dives out of his way.
    // Sari: her complaint and warning. Night: the evening cutscene at the grown field, then back to the front door.
    public enum State { Stroll, Arrival, Tutorial1, Tutorial2, Departure, Sari, Night, Done }

    // the sub-steps of the cutscene-like states
    private enum Step
    {
        None, Startled, WaitingForTruck, Flipping, Intro, WalkingToTruck, Lifting, Carrying, Placing, Waving, Walking, Hopping, Leaving,
        EveningWalkIn, EveningWhat, EveningPan, EveningWalkToFarm, EveningMonologue,
        // the night walk up the path, filmed by the gameplay camera
        EveningPathWalk,
    }

    [SerializeField] private Day1SO day1SO;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private DistributorTruck truck;
    [SerializeField] private FabricatorController fabricator;
    [SerializeField] private FabricatorDelivery fabricatorDelivery;
    [SerializeField] private Npc distributorNpc;
    [SerializeField] private CutsceneActor distributorActor;
    [SerializeField] private CutsceneActor playerActor;
    [SerializeField] private Npc sariNpc;
    [SerializeField] private CutsceneActor sariActor;
    [SerializeField] private HouseDoor houseDoor;
    [SerializeField] private Transform yardPoint;
    [Tooltip("Where the Fabricator rides in the Distributor's arms.")]
    [SerializeField] private Transform distributorCarryPoint;
    [Tooltip("Every plot: the planted ones have grown by the evening.")]
    [SerializeField] private FarmPlot[] farmPlotArray;
    [Tooltip("West of the house front, out of the evening shot; the MC walks in from here.")]
    [SerializeField] private Transform eveningStartPoint;
    [SerializeField] private Transform eveningStopPoint;
    [SerializeField] private Transform farmInspectPoint;
    [Tooltip("Optional night walk (2+ points along the path, ending by the garden): the MC starts on the first point and walks them in order while the gameplay camera follows; after the \"What?!\" the shot pans to the plants that grew. Empty: the walk-in from eveningStartPoint with the house shot.")]
    [SerializeField] private Transform[] eveningPathPointArray;
    [SerializeField] private float strollDistance = 3f;
    [Tooltip("Optional: when set, the truck comes once the player walks within arrivalTriggerRadius of this point (the garden) instead of strollDistance from the yard.")]
    [SerializeField] private Transform arrivalTriggerPoint;
    [SerializeField] private float arrivalTriggerRadius = 6f;
    [Tooltip("Optional: the opening task list (\"Check your garden\"), ticked off when the player reaches the arrival trigger. Empty: the ticked-off letter task shows instead.")]
    [SerializeField] private TaskObjectiveSO[] strollObjectiveArray;
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
    [Tooltip("Sari dives once the leaving truck is this close to her.")]
    [SerializeField] private float sariDodgeLeadDistance = 8f;
    [SerializeField] private float sariDodgeSide = 1.1f;
    [SerializeField] private float sariDodgeDuration = .35f;
    [Tooltip("How long the camera takes to pan from the house to the field before the MC walks back into the shot.")]
    [SerializeField] private float farmPanDuration = 2.5f;

    private State state;
    private Step step;
    private float stepTimer = -1f;
    // the only fade in the village hides nightfall
    private bool isNightFadePlaying;
    private bool isPressedDialoguePending;
    private bool isFarewellPending;
    private bool isFarewellReady;
    private bool isPressedDialogueActive;
    private bool hasSariDodged;
    private int eveningPathIndex;
    // centre of the plants that grew by nightfall; the night walk's last shot frames it
    private bool hasGardenFocus;
    private Vector3 gardenFocus;

    private void Start()
    {
        screenFade.OnBlack += ScreenFade_OnBlack;
        screenFade.OnFinished += ScreenFade_OnFinished;
        truck.OnStateChanged += Truck_OnStateChanged;
        distributorActor.OnMoveFinished += DistributorActor_OnMoveFinished;
        playerActor.OnMoveFinished += PlayerActor_OnMoveFinished;
        fabricatorDelivery.OnPickedUp += FabricatorDelivery_OnPickedUp;
        fabricatorDelivery.OnDelivered += FabricatorDelivery_OnDelivered;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        TaskManager.Instance.OnRequiredTasksCompleted += TaskManager_OnRequiredTasksCompleted;
        houseDoor.OnStateChanged += HouseDoor_OnStateChanged;
        FabricatorController.OnAnySeedFabricated += FabricatorController_OnAnySeedFabricated;

        // everything that arrives later waits out of sight
        truck.gameObject.SetActive(false);
        distributorNpc.gameObject.SetActive(false);
        sariNpc.gameObject.SetActive(false);
        fabricator.SetPlaced(false);
        fabricatorDelivery.AttachTo(truck.GetCargoAnchor());
        houseDoor.SetLocked(true);
        // no "Finish today's tasks first" over the player's head during the opening
        houseDoor.SetHidden(true);
        if (strollObjectiveArray.Length > 0)
        {
            TaskManager.Instance.SetObjectives(strollObjectiveArray);
        }
        else
        {
            // the letter was read in the bedroom (HouseInterior): show it ticked off until the tutorial list replaces it
            TaskManager.Instance.SetObjectives(day1SO.letterObjectiveArray);
            TaskManager.Instance.CompleteStory();
        }
        // just stepped out of the front door, facing the camera
        Player.Instance.Teleport(yardPoint.position);
        Player.Instance.FaceTowards(yardPoint.position + yardPoint.forward);
        SetState(State.Stroll);
    }

    private void Update()
    {
        // a few free steps out of the door (or, with a trigger point, a walk over to the garden), then the truck comes roaring in
        if (state == State.Stroll && GameManager.Instance.IsGamePlaying() && HasStrolled())
            BeginArrival();

        // the leaving truck is about to run Sari over
        if (step == Step.Leaving && !hasSariDodged && truck.transform.position.x < sariNpc.transform.position.x + sariDodgeLeadDistance)
            DodgeTruck();

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
        if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
        if (TaskManager.Instance != null) TaskManager.Instance.OnRequiredTasksCompleted -= TaskManager_OnRequiredTasksCompleted;
    }

    // ---------- night fade ----------

    private void ScreenFade_OnBlack(object sender, EventArgs e)
    {
        if (!isNightFadePlaying) return;
        sariNpc.gameObject.SetActive(false);
        foreach (FarmPlot farmPlot in farmPlotArray) farmPlot.Grow();
        // set before the state so the camera already has its first shot when Night begins
        if (HasEveningPath())
        {
            // the bottom of the path; the gameplay camera follows the walk
            Player.Instance.Teleport(eveningPathPointArray[0].position);
            Player.Instance.FaceTowards(eveningPathPointArray[1].position);
            FindGardenFocus();
            eveningPathIndex = 0;
            step = Step.EveningPathWalk;
        }
        else
        {
            Player.Instance.Teleport(eveningStartPoint.position);
            Player.Instance.FaceTowards(eveningStopPoint.position);
            // the camera already frames the house front
            step = Step.EveningWalkIn;
        }
        SetState(State.Night);
    }

    // the screen clears, still a cutscene: the MC walks up the path (or into the house-front shot from the west)
    private void ScreenFade_OnFinished(object sender, EventArgs e)
    {
        if (!isNightFadePlaying) return;
        isNightFadePlaying = false;
        if (HasEveningPath()) WalkEveningPath();
        else WalkPlayerTo(eveningStopPoint.position);
    }

    // ---------- Arrival ----------

    // the player hears the truck coming in from the west and jumps
    private bool HasStrolled()
    {
        Vector3 playerPosition = Player.Instance.transform.position;
        if (arrivalTriggerPoint != null) return Vector3.Distance(playerPosition, arrivalTriggerPoint.position) <= arrivalTriggerRadius;
        return Vector3.Distance(playerPosition, yardPoint.position) >= strollDistance;
    }

    private void BeginArrival()
    {
        // reached the garden: "Check your garden" is done
        if (strollObjectiveArray.Length > 0) TaskManager.Instance.CompleteStory();
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
            // distributorActor.PlayEmote(Emote.GoofyFlip);
            distributorActor.MoveTo(playerPosition + toDoor.normalized * talkDistance, flipDuration, 1.4f);
            step = Step.Flipping;
        }
        else if (e.state == DistributorTruck.State.Hidden && state == State.Departure)
        {
            truck.gameObject.SetActive(false);
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

    private void PlayerActor_OnMoveFinished(object sender, EventArgs e)
    {
        if (step == Step.EveningPathWalk)
        {
            if (eveningPathIndex < eveningPathPointArray.Length - 1)
            {
                WalkEveningPath();
                return;
            }
            // at the garden, still on the gameplay camera: the shot moves to the plants only once the "What?!" is closed
            step = Step.EveningWhat;
            DialogueManager.Instance.StartDialogue(null, day1SO.eveningWhatDialogueSO);
        }
        else if (step == Step.EveningWalkIn)
        {
            step = Step.EveningWhat;
            DialogueManager.Instance.StartDialogue(null, day1SO.eveningWhatDialogueSO);
        }
        else if (step == Step.EveningWalkToFarm)
        {
            step = Step.EveningMonologue;
            Player.Instance.FaceTowards(farmInspectPoint.position + farmInspectPoint.forward);
            DialogueManager.Instance.StartDialogue(null, day1SO.eveningMonologueDialogueSO);
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
        else if (state == State.Night && step == Step.EveningWhat)
        {
            // the camera leaves the MC and pans over to the field
            step = Step.EveningPan;
            GameManager.Instance.SetState(GameManager.State.Cutscene);
            stepTimer = farmPanDuration;
        }
        else if (state == State.Night && step == Step.EveningMonologue)
        {
            // back in control: walk home to end the day
            step = Step.None;
            houseDoor.SetLocked(false);
            // to bed: Day 2 starts in the bedroom
            houseDoor.SetNextScene(Loader.Scene.HouseInterior);
            TaskManager.Instance.SetObjectives(day1SO.nightObjectiveArray);
        }
    }

    // ---------- tutorial ----------

    private void FabricatorController_OnAnySeedFabricated(object sender, EventArgs e)
    {
        if (state == State.Tutorial1) isPressedDialoguePending = true;
    }

    private void TaskManager_OnRequiredTasksCompleted(object sender, EventArgs e)
    {
        if (state == State.Tutorial2 && !isFarewellReady) isFarewellPending = true;
    }

    private void BeginDeparture()
    {
        GameManager.Instance.SetState(GameManager.State.Cutscene);
        Player.Instance.FaceTowards(distributorNpc.transform.position);
        step = Step.Waving;
        // Sari has come onto the bridge, right in the truck's way out
        sariNpc.gameObject.SetActive(true);
        sariActor.FaceTowards(truck.transform.position);
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
        else if (step == Step.EveningPan)
        {
            step = Step.EveningWalkToFarm;
            WalkPlayerTo(farmInspectPoint.position);
        }
    }

    // she dives off the road (z 0) to the south side of the bridge, still facing the truck
    private void DodgeTruck()
    {
        hasSariDodged = true;
        sariActor.MoveTo(sariNpc.transform.position + Vector3.back * sariDodgeSide, sariDodgeDuration, .5f);
        sariActor.FaceTowards(truck.transform.position);
        sariActor.PlayEmote(Emote.Dodge);
    }

    private void WalkPlayerTo(Vector3 target)
    {
        playerActor.MoveTo(target, Vector3.Distance(Player.Instance.transform.position, target) / walkSpeed, 0f);
    }

    private bool HasEveningPath() => eveningPathPointArray != null && eveningPathPointArray.Length >= 2;

    // the next leg of the night walk
    private void WalkEveningPath()
    {
        eveningPathIndex++;
        WalkPlayerTo(eveningPathPointArray[eveningPathIndex].position);
    }

    // the middle of the plants that grew; with none, the camera keeps its fixed farm shot
    private void FindGardenFocus()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (FarmPlot farmPlot in farmPlotArray)
        {
            if (farmPlot.GetState() != FarmPlot.State.Grown) continue;
            sum += farmPlot.transform.position;
            count++;
        }
        hasGardenFocus = count > 0;
        if (hasGardenFocus) gardenFocus = sum / count;
    }

    // ---------- night ----------

    private void HouseDoor_OnStateChanged(object sender, HouseDoor.OnStateChangedEventArgs e)
    {
        if (state != State.Night || e.state != HouseDoor.State.IrisClosing) return;
        // every sapling in the ground tonight, including any planted after dusk, stands ripe in the Day 2 scene
        List<Vector3> plantedPositionList = new List<Vector3>();
        foreach (FarmPlot farmPlot in farmPlotArray)
            if (farmPlot.GetState() == FarmPlot.State.Planted || farmPlot.GetState() == FarmPlot.State.Grown) plantedPositionList.Add(farmPlot.transform.position);
        WorldState.CompleteDay1(plantedPositionList);
        SetState(State.Done);
    }

    public State GetState() => state;
    // the Distributor is fetching, carrying or setting down the Fabricator
    public bool IsDelivering() => step == Step.WalkingToTruck || step == Step.Lifting || step == Step.Carrying || step == Step.Placing;
    // the evening at the farm, filmed from above: first the house front, then the field
    // (the night walk up the path and its "What?!" are not: the gameplay camera films them)
    public bool IsEveningCutscene() => step == Step.EveningWalkIn || (step == Step.EveningWhat && !HasEveningPath()) || IsFarmShot();
    public bool IsFarmShot() => step == Step.EveningPan || step == Step.EveningWalkToFarm || step == Step.EveningMonologue;
    // the night walk and its "What?!", filmed by the gameplay camera before the shot pans to the garden
    public bool IsEveningFollowShot() => step == Step.EveningPathWalk || (step == Step.EveningWhat && HasEveningPath());
    public bool TryGetGardenFocus(out Vector3 focus)
    {
        focus = gardenFocus;
        return hasGardenFocus;
    }

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }
}
