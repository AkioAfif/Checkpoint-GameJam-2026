using UnityEngine;

// A splash where the MC falls into the river: a burst of droplets, and a second, bigger one turned 45° so the spray fills out.
// The splash prefab (VFX/RiverSplash) destroys itself when it is done.
public class RiverSplashVisual : MonoBehaviour
{
    [SerializeField] private River river;
    [SerializeField] private Transform splashPrefab;
    [Tooltip("Size of the second burst relative to the first.")]
    [SerializeField] private float sprayScale = 1.5f;

    private void Start()
    {
        river.OnPlayerFellIn += River_OnPlayerFellIn;
    }

    private void River_OnPlayerFellIn(object sender, River.OnPlayerFellInEventArgs e)
    {
        Instantiate(splashPrefab, e.position, Quaternion.identity);
        Instantiate(splashPrefab, e.position, Quaternion.Euler(0f, 45f, 0f)).localScale = Vector3.one * sprayScale;
    }
}
