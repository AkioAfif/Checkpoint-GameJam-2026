using UnityEngine;
using UnityEngine.UI;

// Placeholder for the house interior (next level not built yet).
public class StubSceneUI : MonoBehaviour
{
    [SerializeField] private Button replayButton;

    private void Awake()
    {
        Time.timeScale = 1f;
        replayButton.onClick.AddListener(() => Loader.Load(Loader.Scene.Level01_VillagePrototype));
    }

    private void Start()
    {
        replayButton.Select();
    }
}
