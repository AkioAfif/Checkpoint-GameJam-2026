using UnityEngine;

// The "got it!" effect: a copy of the item pops, spins and floats up, then shrinks away. Spawns a burst VFX too.
public class ItemFloatUp : MonoBehaviour
{
    private const float DURATION = 1f;
    private const float RISE = 1.4f;

    private Vector3 startPosition;
    private Vector3 startScale;
    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / DURATION;
        float rise = 1f - (1f - t) * (1f - t); // ease out
        transform.position = startPosition + Vector3.up * rise * RISE;
        transform.Rotate(0f, 360f * Time.deltaTime, 0f, Space.World);
        float pop = t < .2f ? Mathf.Lerp(1f, 1.35f, t / .2f) : Mathf.Lerp(1.35f, 0f, Mathf.InverseLerp(.6f, 1f, t));
        transform.localScale = startScale * pop;
        if (timer >= DURATION) Destroy(gameObject);
    }

    public static void Spawn(Transform itemPrefab, Vector3 position, Transform burstPrefab)
    {
        Instantiate(burstPrefab, position + Vector3.up * .3f, Quaternion.identity);
        if (itemPrefab == null) return;
        Transform item = Instantiate(itemPrefab, position, Quaternion.identity);
        ItemFloatUp floatUp = item.gameObject.AddComponent<ItemFloatUp>();
        floatUp.startPosition = position;
        floatUp.startScale = item.localScale;
    }
}
