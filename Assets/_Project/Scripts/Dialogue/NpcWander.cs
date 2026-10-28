using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

// Strolls between a few points near home; stops and turns to the player while talking. At night (NightCurfew) it walks
// home once and stays there until morning.
[RequireComponent(typeof(NavMeshAgent))]
public class NpcWander : MonoBehaviour
{
    [SerializeField] private Npc npc;
    [SerializeField] private Transform[] wanderPointArray;
    [SerializeField] private Vector2 waitTimeRange = new Vector2(2f, 5f);
    [SerializeField] private float turnSpeed = 8f;
    [Tooltip("Where this villager spends the night, by its own house. Empty: the spot it stands on when the scene starts.")]
    [SerializeField] private Transform homePoint;

    private NavMeshAgent agent;
    private int pointIndex;
    private float waitTimer;
    private bool isTalking;
    private bool isNight;
    // the walk home is ordered once, as soon as the agent is on the NavMesh (a cutscene move switches it off)
    private bool isHomeOrderPending;
    private Vector3 homePosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        homePosition = homePoint != null ? homePoint.position : transform.position;
    }

    private void Start()
    {
        DialogueManager.Instance.OnDialogueStarted += DialogueManager_OnDialogueStarted;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        waitTimer = Random.Range(waitTimeRange.x, waitTimeRange.y);
        if (NightCurfew.Instance != null)
        {
            NightCurfew.Instance.OnNightChanged += NightCurfew_OnNightChanged;
            SetNight(NightCurfew.Instance.IsNight());
        }
    }

    private void Update()
    {
        if (isTalking)
        {
            Vector3 toPlayer = Player.Instance.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), turnSpeed * Time.deltaTime);
            return;
        }
        if (isNight)
        {
            if (!isHomeOrderPending || !agent.isOnNavMesh) return;
            isHomeOrderPending = false;
            agent.SetDestination(homePosition);
            return;
        }
        if (wanderPointArray.Length == 0 || agent.pathPending || agent.remainingDistance > agent.stoppingDistance) return;

        waitTimer -= Time.deltaTime;
        if (waitTimer > 0f) return;
        waitTimer = Random.Range(waitTimeRange.x, waitTimeRange.y);
        pointIndex = (pointIndex + 1) % wanderPointArray.Length;
        agent.SetDestination(wanderPointArray[pointIndex].position);
    }

    private void OnDestroy()
    {
        if (NightCurfew.Instance != null) NightCurfew.Instance.OnNightChanged -= NightCurfew_OnNightChanged;
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.OnDialogueStarted -= DialogueManager_OnDialogueStarted;
        DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
    }

    private void DialogueManager_OnDialogueStarted(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (e.npc != npc) return;
        isTalking = true;
        // a cutscene move switches the agent off; it is re-seated (and moving again) when the move ends
        if (agent.isOnNavMesh) agent.isStopped = true;
        agent.updateRotation = false;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (e.npc != npc) return;
        isTalking = false;
        if (agent.isOnNavMesh) agent.isStopped = false;
        agent.updateRotation = true;
    }

    private void NightCurfew_OnNightChanged(object sender, EventArgs e) => SetNight(((NightCurfew)sender).IsNight());

    public float GetSpeedNormalized() => agent.velocity.magnitude / agent.speed;

    private void SetNight(bool isNight)
    {
        this.isNight = isNight;
        isHomeOrderPending = isNight;
        // morning: drop the walk home and pick the stroll up from wherever it stands
        if (!isNight && agent.isOnNavMesh) agent.ResetPath();
    }
}
