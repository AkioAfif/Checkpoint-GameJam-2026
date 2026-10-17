using UnityEngine;
using UnityEngine.AI;

// Strolls between a few points near home; stops and turns to the player while talking.
[RequireComponent(typeof(NavMeshAgent))]
public class NpcWander : MonoBehaviour
{
    [SerializeField] private Npc npc;
    [SerializeField] private Transform[] wanderPointArray;
    [SerializeField] private Vector2 waitTimeRange = new Vector2(2f, 5f);
    [SerializeField] private float turnSpeed = 8f;

    private NavMeshAgent agent;
    private int pointIndex;
    private float waitTimer;
    private bool isTalking;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        DialogueManager.Instance.OnDialogueStarted += DialogueManager_OnDialogueStarted;
        DialogueManager.Instance.OnDialogueEnded += DialogueManager_OnDialogueEnded;
        waitTimer = Random.Range(waitTimeRange.x, waitTimeRange.y);
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
        if (wanderPointArray.Length == 0 || agent.pathPending || agent.remainingDistance > agent.stoppingDistance) return;

        waitTimer -= Time.deltaTime;
        if (waitTimer > 0f) return;
        waitTimer = Random.Range(waitTimeRange.x, waitTimeRange.y);
        pointIndex = (pointIndex + 1) % wanderPointArray.Length;
        agent.SetDestination(wanderPointArray[pointIndex].position);
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.OnDialogueStarted -= DialogueManager_OnDialogueStarted;
        DialogueManager.Instance.OnDialogueEnded -= DialogueManager_OnDialogueEnded;
    }

    private void DialogueManager_OnDialogueStarted(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (e.npc != npc) return;
        isTalking = true;
        agent.isStopped = true;
        agent.updateRotation = false;
    }

    private void DialogueManager_OnDialogueEnded(object sender, DialogueManager.OnDialogueEventArgs e)
    {
        if (e.npc != npc) return;
        isTalking = false;
        agent.isStopped = false;
        agent.updateRotation = true;
    }

    public float GetSpeedNormalized() => agent.velocity.magnitude / agent.speed;
}
