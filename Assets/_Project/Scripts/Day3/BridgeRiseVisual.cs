using UnityEngine;

// The new bridge climbs out of the river a stage per blow, shuddering each time, like the new field's plots.
public class BridgeRiseVisual : MonoBehaviour
{
    [SerializeField] private BridgeBuild bridgeBuild;
    [SerializeField] private Transform bridgeRoot;
    [Tooltip("How deep the bridge starts below its place (under the water surface).")]
    [SerializeField] private float sunkDepth = 2.6f;
    [SerializeField] private float riseSpeed = 1.6f;
    [SerializeField] private float shudderDuration = .35f;
    [SerializeField] private float shudderAmount = .15f;

    private float height;
    private float targetHeight;
    private float shudderTimer;
    private Vector3 restPosition;

    private void Start()
    {
        bridgeBuild.OnSmash += BridgeBuild_OnSmash;
        restPosition = bridgeRoot.localPosition;
        bridgeRoot.localPosition = restPosition + Vector3.down * sunkDepth;
    }

    private void Update()
    {
        if (height >= 1f && shudderTimer <= 0f) return;
        height = Mathf.MoveTowards(height, targetHeight, riseSpeed * Time.deltaTime);
        shudderTimer -= Time.deltaTime;
        Vector3 shudder = shudderTimer > 0f ? new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * shudderAmount : Vector3.zero;
        bridgeRoot.localPosition = restPosition + Vector3.down * sunkDepth * (1f - height) + shudder;
    }

    private void BridgeBuild_OnSmash(object sender, BridgeBuild.OnSmashEventArgs e)
    {
        targetHeight = e.progressNormalized;
        shudderTimer = shudderDuration;
    }
}
