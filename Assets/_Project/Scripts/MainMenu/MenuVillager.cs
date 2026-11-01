using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

// A main-menu villager: walks a loop of activity spots and does something at each (an emote, repeated while it stays),
// all on the MenuTimeLapse clock so the village looks sped up.
[RequireComponent(typeof(NavMeshAgent))]
public class MenuVillager : MonoBehaviour
{
    public event EventHandler<OnActivityEventArgs> OnActivityStarted;
    public class OnActivityEventArgs : EventArgs
    {
        public Emote emote;
    }

    [Serializable]
    public struct Activity
    {
        public Transform point;
        public Emote emote;
        public float duration;
        [Tooltip("Optional: the garden bed this spot works, planted or harvested when the villager moves on.")]
        public MenuGardenPlot plot;
    }

    [SerializeField] private Activity[] activityArray;
    [Tooltip("Seconds (villager time) between repeats of the emote while it stays at a spot.")]
    [SerializeField] private float emoteRepeatInterval = 1.6f;
    [SerializeField] private float turnSpeed = 8f;
    [Tooltip("Random wait before the first walk, so the villagers don't all set off together.")]
    [SerializeField] private float startDelayMax = 3f;

    private NavMeshAgent agent;
    private int activityIndex;
    private bool isWalking;
    private float stayTimer;
    private float emoteTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        float timeScale = MenuTimeLapse.Instance.GetTimeScale();
        agent.speed *= timeScale;
        agent.acceleration *= timeScale * timeScale;
        agent.angularSpeed *= timeScale;
        // start mid-loop and wait a bit, so the town isn't in lockstep
        activityIndex = activityArray.Length > 0 ? Random.Range(0, activityArray.Length) : 0;
        stayTimer = Random.Range(0f, startDelayMax);
        if (activityArray.Length > 0 && activityArray[activityIndex].point != null) agent.Warp(activityArray[activityIndex].point.position);
    }

    private void Update()
    {
        if (activityArray.Length == 0 || !agent.isOnNavMesh) return;
        float deltaTime = MenuTimeLapse.Instance.GetDeltaTime();

        if (isWalking)
        {
            if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance) return;
            isWalking = false;
            Activity arrived = activityArray[activityIndex];
            stayTimer = arrived.duration;
            emoteTimer = 0f;
            return;
        }

        Activity activity = activityArray[activityIndex];
        if (activity.point != null)
        {
            Vector3 forward = activity.point.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward), turnSpeed * deltaTime);
        }

        emoteTimer -= deltaTime;
        if (emoteTimer <= 0f)
        {
            emoteTimer = emoteRepeatInterval;
            OnActivityStarted?.Invoke(this, new OnActivityEventArgs { emote = activity.emote });
        }

        stayTimer -= deltaTime;
        if (stayTimer > 0f) return;
        if (activity.plot != null) activity.plot.Tend();
        activityIndex = (activityIndex + 1) % activityArray.Length;
        if (activityArray[activityIndex].point == null) return;
        agent.SetDestination(activityArray[activityIndex].point.position);
        isWalking = true;
    }

    public float GetSpeedNormalized() => agent.velocity.magnitude / agent.speed;
}
