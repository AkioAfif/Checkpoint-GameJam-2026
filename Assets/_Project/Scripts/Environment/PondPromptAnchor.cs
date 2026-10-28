using UnityEngine;

// A pond is a wide interactable: keep its prompt anchor on the pond's edge right next to the player instead of in the middle.
[RequireComponent(typeof(Collider))]
public class PondPromptAnchor : MonoBehaviour
{
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private float height = 1.3f;

    private Collider pondCollider;

    private void Awake()
    {
        pondCollider = GetComponent<Collider>();
    }

    private void LateUpdate()
    {
        Vector3 edge = pondCollider.ClosestPoint(Player.Instance.transform.position);
        promptAnchor.position = new Vector3(edge.x, transform.position.y + height, edge.z);
    }
}
