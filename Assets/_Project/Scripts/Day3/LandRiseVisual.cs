using UnityEngine;

// The new plots climb out of the grass a stage per blow on the ground, with a little shudder each time.
public class LandRiseVisual : MonoBehaviour
{
    [SerializeField] private LandSequence landSequence;
    [SerializeField] private Transform landPlotsRoot;
    [Tooltip("How deep the plots start below the grass (just under a tile's height, so every blow shows).")]
    [SerializeField] private float sunkDepth = .18f;
    [SerializeField] private float riseSpeed = 4f;
    [SerializeField] private float shudderDuration = .35f;
    [SerializeField] private float shudderAmount = .12f;

    private float height;
    private float targetHeight;
    private float shudderTimer;
    private Vector3 restPosition;

    private void Start()
    {
        landSequence.OnSmash += LandSequence_OnSmash;
        restPosition = landPlotsRoot.localPosition;
        landPlotsRoot.localPosition = restPosition + Vector3.down * sunkDepth;
    }

    private void Update()
    {
        if (height >= 1f && shudderTimer <= 0f) return;
        height = Mathf.MoveTowards(height, targetHeight, riseSpeed * Time.deltaTime);
        shudderTimer -= Time.deltaTime;
        Vector3 shudder = shudderTimer > 0f ? new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * shudderAmount : Vector3.zero;
        landPlotsRoot.localPosition = restPosition + Vector3.down * sunkDepth * (1f - height) + shudder;
    }

    private void LandSequence_OnSmash(object sender, LandSequence.OnSmashEventArgs e)
    {
        targetHeight = e.progressNormalized;
        shudderTimer = shudderDuration;
    }
}
