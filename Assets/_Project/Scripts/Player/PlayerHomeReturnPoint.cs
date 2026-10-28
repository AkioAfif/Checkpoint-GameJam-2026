using UnityEngine;

// Where "Unstuck me" (pause menu) puts the player: in front of their house, facing this transform's forward.
// One per scene that has the house.
public class PlayerHomeReturnPoint : MonoBehaviour
{
    public static PlayerHomeReturnPoint Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one PlayerHomeReturnPoint", this);
        Instance = this;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * .5f, .5f);
        Gizmos.DrawRay(transform.position + Vector3.up * .5f, transform.forward);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
