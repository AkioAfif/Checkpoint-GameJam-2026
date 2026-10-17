using UnityEngine;

// Plants sway when the player brushes past them.
public class PlantWobbleVisual : MonoBehaviour
{
    [SerializeField] private float triggerDistance = 1.1f;
    [SerializeField] private float strength = 12f;
    [SerializeField] private float frequency = 9f;
    [SerializeField] private float decay = 3f;

    private float wobble;
    private float time;
    private bool wasNear;

    private void Update()
    {
        Vector3 toPlayer = Player.Instance.transform.position - transform.position;
        toPlayer.y = 0f;
        bool isNear = toPlayer.sqrMagnitude < triggerDistance * triggerDistance && Player.Instance.IsMoving();
        if (isNear && !wasNear) wobble = 1f;
        wasNear = isNear;
        if (wobble <= 0f) return;

        time += Time.deltaTime;
        wobble = Mathf.Max(0f, wobble - decay * Time.deltaTime);
        float angle = Mathf.Sin(time * frequency) * strength * wobble;
        transform.localRotation = Quaternion.Euler(angle, 0f, angle * .6f);
    }
}
