using UnityEngine;

// The rope from the stump to the MC's hands, while he (and later Rigel) hauls on it; it sags less as the meter fills.
[RequireComponent(typeof(LineRenderer))]
public class RopeVisual : MonoBehaviour
{
    private const int POINT_COUNT = 12;

    [SerializeField] private YardDayDirector director;
    [SerializeField] private LandQTE landQTE;
    [Tooltip("Where the rope is tied on the stump.")]
    [SerializeField] private Transform stumpEnd;
    [SerializeField] private float handHeight = .75f;
    [SerializeField] private float handReach = .35f;
    [SerializeField] private float slackSag = .35f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = POINT_COUNT;
        lineRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        YardDayDirector.Step step = director.GetStep();
        bool isPulling = director.GetState() == YardDayDirector.State.Stump
            && (step == YardDayDirector.Step.PullAlone || step == YardDayDirector.Step.RigelWalk || step == YardDayDirector.Step.RigelHelp || step == YardDayDirector.Step.PullTogether);
        lineRenderer.enabled = isPulling;
        if (!isPulling) return;

        Transform player = Player.Instance.transform;
        Vector3 hands = player.position + Vector3.up * handHeight + player.forward * handReach;
        Vector3 tied = stumpEnd.position;
        float sag = slackSag * (1f - landQTE.GetProgress());
        for (int i = 0; i < POINT_COUNT; i++)
        {
            float t = (float)i / (POINT_COUNT - 1);
            lineRenderer.SetPosition(i, Vector3.Lerp(hands, tied, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * sag);
        }
    }
}
