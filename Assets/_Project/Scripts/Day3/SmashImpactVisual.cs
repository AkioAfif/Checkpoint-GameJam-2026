using Unity.Cinemachine;
using UnityEngine;

// Makes every blow on the ground feel heavy: the world jolts (a Cinemachine impulse every camera listens to), the lens punches
// in and eases back out, time freezes for a few frames on contact (hit-stop), and a burst of dirt flies up. The last,
// double-fisted blow does all of it far harder.
public class SmashImpactVisual : MonoBehaviour
{
    [SerializeField] private LandSequence landSequence;
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

    private float[] baseFovArray;
    private float zoom;
    private float hitStopTimer;

    private void Start()
    {
        landSequence.OnSmash += LandSequence_OnSmash;
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

    private void LandSequence_OnSmash(object sender, LandSequence.OnSmashEventArgs e)
    {
        impulseSource.GenerateImpulseWithForce(e.isFinal ? finalShakeForce : shakeForce);
        zoom = e.isFinal ? finalZoomPunch : zoomPunch;

        Vector3 impact = smashPoint.position + smashPoint.forward * .45f;
        Transform dust = Instantiate(dustPrefab, impact, Quaternion.identity);
        if (e.isFinal)
        {
            dust.localScale = Vector3.one * finalDustScale;
            // a second, wider ring of dirt
            Instantiate(dustPrefab, impact, Quaternion.Euler(0f, 45f, 0f)).localScale = Vector3.one * finalDustScale * 1.5f;
        }

        if (GameManager.Instance.IsGamePaused()) return;
        Time.timeScale = .02f;
        hitStopTimer = e.isFinal ? finalHitStopDuration : hitStopDuration;
    }
}
