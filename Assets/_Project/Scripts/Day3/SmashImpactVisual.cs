using Unity.Cinemachine;
using UnityEngine;

// Makes every blow feel heavy: the world jolts (a Cinemachine impulse every camera listens to), the lens punches
// in and eases back out, time freezes for a few frames on contact (hit-stop), and a burst of dirt flies up. The last,
// double-fisted blow does all of it far harder. On the CozyFarm map the tree punches and the bridge's blows hit the same way.
public class SmashImpactVisual : MonoBehaviour
{
    [Tooltip("Day 4's land smash. Leave empty on Day 5.")]
    [SerializeField] private LandSequence landSequence;
    [Tooltip("Day 5: the punch that ends the dive, an earthquake (earthquakeShakeForce).")]
    [SerializeField] private PlantationSequence plantationSequence;
    [SerializeField] private float earthquakeShakeForce = 2.2f;
    [Tooltip("Optional (the CozyFarm map).")]
    [SerializeField] private TreeClearing treeClearing;
    [Tooltip("Optional (the CozyFarm map), with the bank it's pounded from.")]
    [SerializeField] private BridgeBuild bridgeBuild;
    [SerializeField] private Transform bridgePoint;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [Tooltip("The smash cameras: their field of view punches in on every blow.")]
    [SerializeField] private CinemachineCamera[] punchCameraArray;
    [SerializeField] private Transform smashPoint;
    [SerializeField] private Transform dustPrefab;
    [Tooltip("The smash cameras sit under the palms' canopy, so the jolt stays small enough not to push them into a trunk.")]
    [SerializeField] private float shakeForce = .2f;
    [SerializeField] private float finalShakeForce = .4f;
    [Tooltip("Degrees the lens zooms in on contact.")]
    [SerializeField] private float zoomPunch = 9f;
    [SerializeField] private float finalZoomPunch = 18f;
    [SerializeField] private float zoomRecoverSpeed = 4f;
    [Tooltip("Real seconds the world stands still when a fist lands.")]
    [SerializeField] private float hitStopDuration = .07f;
    [SerializeField] private float finalHitStopDuration = .18f;
    [SerializeField] private float finalDustScale = 2.2f;
    [Tooltip("Height on the trunk where a punch lands.")]
    [SerializeField] private float treeHitHeight = 1.1f;

    private float[] baseFovArray;
    private float zoom;
    private float hitStopTimer;

    private void Start()
    {
        if (landSequence != null) landSequence.OnSmash += LandSequence_OnSmash;
        if (plantationSequence != null) plantationSequence.OnImpact += PlantationSequence_OnImpact;
        if (treeClearing != null) treeClearing.OnHit += TreeClearing_OnHit;
        if (bridgeBuild != null) bridgeBuild.OnSmash += BridgeBuild_OnSmash;
        baseFovArray = new float[punchCameraArray.Length];
        for (int i = 0; i < punchCameraArray.Length; i++) baseFovArray[i] = punchCameraArray[i].Lens.FieldOfView;
    }

    private void Update()
    {
        if (hitStopTimer > 0f)
        {
            hitStopTimer -= Time.unscaledDeltaTime;
            // the pause menu owns timeScale while it's open
            if (hitStopTimer <= 0f && !GameManager.Instance.IsGamePaused()) Time.timeScale = 1f;
        }

        if (zoom <= 0f) return;
        zoom = Mathf.MoveTowards(zoom, 0f, zoom * zoomRecoverSpeed * Time.deltaTime + .01f);
        for (int i = 0; i < punchCameraArray.Length; i++)
        {
            LensSettings lens = punchCameraArray[i].Lens;
            lens.FieldOfView = baseFovArray[i] - zoom;
            punchCameraArray[i].Lens = lens;
        }
    }

    private void LandSequence_OnSmash(object sender, LandSequence.OnSmashEventArgs e) => Impact(smashPoint.position + smashPoint.forward * .45f, e.isFinal);

    private void TreeClearing_OnHit(object sender, TreeClearing.OnHitEventArgs e) => Impact(e.position + Vector3.up * treeHitHeight, e.isFinal);

    private void BridgeBuild_OnSmash(object sender, BridgeBuild.OnSmashEventArgs e) => Impact(bridgePoint.position + bridgePoint.forward * .45f, e.isFinal);

    private void PlantationSequence_OnImpact(object sender, PlantationSequence.OnImpactEventArgs e) => Impact(e.position, true, earthquakeShakeForce);

    private void Impact(Vector3 impact, bool isFinal) => Impact(impact, isFinal, isFinal ? finalShakeForce : shakeForce);

    private void Impact(Vector3 impact, bool isFinal, float force)
    {
        impulseSource.GenerateImpulseWithForce(force);
        zoom = isFinal ? finalZoomPunch : zoomPunch;

        Transform dust = Instantiate(dustPrefab, impact, Quaternion.identity);
        if (isFinal)
        {
            dust.localScale = Vector3.one * finalDustScale;
            // a second, wider ring of dirt
            Instantiate(dustPrefab, impact, Quaternion.Euler(0f, 45f, 0f)).localScale = Vector3.one * finalDustScale * 1.5f;
        }

        if (GameManager.Instance.IsGamePaused()) return;
        Time.timeScale = .02f;
        hitStopTimer = isFinal ? finalHitStopDuration : hitStopDuration;
    }
}
