using UnityEngine;

// The main menu's villager clock: everything the villagers do runs this many times faster, while the camera and UI keep
// real time (Time.timeScale stays 1).
public class MenuTimeLapse : MonoBehaviour
{
    public static MenuTimeLapse Instance { get; private set; }

    [SerializeField] private float timeScale = 4f;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one MenuTimeLapse", this);
        Instance = this;
    }

    public float GetTimeScale() => timeScale;
    public float GetDeltaTime() => Time.deltaTime * timeScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
