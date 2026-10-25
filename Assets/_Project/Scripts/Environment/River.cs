using System;
using UnityEngine;

// The river's current. The player wades straight through the water surface (its collider only marks where the river is);
// once their feet are drownDepth under it, the current takes them: they bob up to float chest-deep and are swept downstream
// (CutscenePose.Drown thrashes, then sinks them), the screen goes black with a warning, and they wash up on the bank where
// they last stood on dry ground.
[RequireComponent(typeof(Collider))]
public class River : MonoBehaviour
{
    // the glb river mesh is a strip: rows of this many vertices straight across the water, the first row upstream
    private const int ROW_VERTEX_COUNT = 11;

    // Drifting and FadingOut: swept away; WashedUp: back on the bank behind the black screen
    private enum State { Flowing, Drifting, FadingOut, WashedUp }

    [SerializeField] private ScreenFadeUI screenFade;
    [TextArea]
    [SerializeField] private string warningCaption = "Don't play in rivers with strong currents";
    [Tooltip("Speed of the current (m/s), from the first row of the river mesh toward the last. RiverFlowVisual scrolls the water to match.")]
    [SerializeField] private float currentSpeed = 2f;
    [Tooltip("How far (m) the player's feet sink under the surface before the current takes them.")]
    [SerializeField] private float drownDepth = .25f;
    [Tooltip("While drifting the feet float this far (m) under the surface: head and arms out of the water.")]
    [SerializeField] private float floatDepth = .7f;
    [Tooltip("How fast (m/s) the drifting player is pulled into the middle of the river, away from the banks.")]
    [SerializeField] private float centrePull = 1.2f;
    [Tooltip("Ground at least this far (m) above the surface counts as dry bank to respawn on.")]
    [SerializeField] private float safeHeight = .4f;
    [Tooltip("Seconds swept along before the screen fades (CutscenePoseVisual sinks them near the end).")]
    [SerializeField] private float driftDuration = 3.2f;
    [SerializeField] private float warningDuration = 2.5f;

    private Collider waterCollider;
    private Vector3[] centrelineArray;
    private CharacterController playerController;
    private CutsceneActor playerActor;
    private State state;
    private float timer;
    private float surfaceHeight;
    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;

    private void Awake()
    {
        waterCollider = GetComponent<Collider>();
        centrelineArray = Centreline(GetComponent<MeshFilter>().sharedMesh.vertices);
    }

    private void Start()
    {
        playerController = Player.Instance.GetComponent<CharacterController>();
        playerActor = Player.Instance.GetComponent<CutsceneActor>();
        // the surface no longer holds the player up: they walk down the bank into the water
        Physics.IgnoreCollision(playerController, waterCollider);
        lastSafePosition = Player.Instance.transform.position;
        lastSafeRotation = Player.Instance.transform.rotation;
        screenFade.OnBlack += ScreenFade_OnBlack;
        screenFade.OnFinished += ScreenFade_OnFinished;
    }

    private void Update()
    {
        Transform player = Player.Instance.transform;
        switch (state)
        {
            case State.Flowing:
                if (!GameManager.Instance.IsGamePlaying()) return;
                bool isOverWater = TryGetSurfaceHeight(player.position, out float height);
                if (isOverWater && player.position.y < height - drownDepth)
                {
                    surfaceHeight = height;
                    BeginDrifting();
                }
                else if (playerController.isGrounded && (!isOverWater || player.position.y > height + safeHeight))
                {
                    // dry ground (the bank, or the bridge over the water)
                    lastSafePosition = player.position;
                    lastSafeRotation = player.rotation;
                }
                break;
            case State.Drifting:
                Drift(player);
                timer -= Time.deltaTime;
                if (timer > 0f) return;
                state = State.FadingOut;
                screenFade.Play(warningCaption, warningDuration);
                break;
            case State.FadingOut:
                Drift(player);
                break;
        }
    }

    private void ScreenFade_OnBlack(object sender, EventArgs e)
    {
        if (state != State.FadingOut) return;
        state = State.WashedUp;
        // Teleport turns the controller back on
        Player.Instance.Teleport(lastSafePosition);
        Player.Instance.transform.rotation = lastSafeRotation;
        playerActor.SetPose(CutscenePose.None);
    }

    private void ScreenFade_OnFinished(object sender, EventArgs e)
    {
        if (state != State.WashedUp) return;
        state = State.Flowing;
        GameManager.Instance.SetState(GameManager.State.Playing);
    }

    public float GetCurrentSpeed() => currentSpeed;

    private void BeginDrifting()
    {
        state = State.Drifting;
        timer = driftDuration;
        GameManager.Instance.SetState(GameManager.State.Cutscene);
        // the current moves the player now, not the controller (and the riverbed can't catch them)
        playerController.enabled = false;
        playerActor.PlayEmote(Emote.Surprised);
        playerActor.SetPose(CutscenePose.Drown);
    }

    // carried downstream along the river's centreline, drawn into the middle, bobbing up to float chest-deep
    private void Drift(Transform player)
    {
        Vector3 position = player.position;
        int segment = ClosestSegment(position);
        Vector3 from = centrelineArray[segment];
        Vector3 downstream = centrelineArray[segment + 1] - from;
        downstream.y = 0f;
        downstream.Normalize();
        Vector3 toCentre = from - position;
        toCentre.y = 0f;
        toCentre -= Vector3.Dot(toCentre, downstream) * downstream;

        position += downstream * currentSpeed * Time.deltaTime + Vector3.ClampMagnitude(toCentre, centrePull * Time.deltaTime);
        position.y = Mathf.MoveTowards(position.y, surfaceHeight - floatDepth, Time.deltaTime);
        player.position = position;
        // the current turns them round to face where it takes them
        player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(downstream), 2f * Time.deltaTime);
    }

    // index of the centreline segment nearest this point (the last row has no segment after it)
    private int ClosestSegment(Vector3 position)
    {
        int closest = 0;
        float closestSqrDistance = float.MaxValue;
        for (int i = 0; i < centrelineArray.Length - 1; i++)
        {
            Vector3 offset = centrelineArray[i] - position;
            offset.y = 0f;
            if (offset.sqrMagnitude >= closestSqrDistance) continue;
            closestSqrDistance = offset.sqrMagnitude;
            closest = i;
        }
        return closest;
    }

    // world-space middle of each row of the river mesh, upstream to downstream
    private Vector3[] Centreline(Vector3[] vertexArray)
    {
        Vector3[] centreArray = new Vector3[vertexArray.Length / ROW_VERTEX_COUNT];
        for (int row = 0; row < centreArray.Length; row++)
        {
            Vector3 middle = (vertexArray[row * ROW_VERTEX_COUNT] + vertexArray[row * ROW_VERTEX_COUNT + ROW_VERTEX_COUNT - 1]) * .5f;
            centreArray[row] = transform.TransformPoint(middle);
        }
        return centreArray;
    }

    // the water surface straight above or below this point, if the point is over the river
    private bool TryGetSurfaceHeight(Vector3 position, out float height)
    {
        Ray ray = new Ray(position + Vector3.up * 5f, Vector3.down);
        bool isOverWater = waterCollider.Raycast(ray, out RaycastHit hit, 10f);
        height = hit.point.y;
        return isOverWater;
    }
}
