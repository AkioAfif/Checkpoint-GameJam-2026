using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Placeholder for the house interior (next level not built yet). After Day 1 it reports what the day left in the world.
public class StubSceneUI : MonoBehaviour
{
    [SerializeField] private Button replayButton;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private string dayCompleteFormat = "Day 1 complete!\nThe Fabricator stays by your house and {0} sawit saplings are growing in the field.";

    private void Awake()
    {
        Time.timeScale = 1f;
        replayButton.onClick.AddListener(() => Loader.Load(Loader.Scene.Level01_VillagePrototype));
    }

    private void Start()
    {
        if (WorldState.IsDay1Complete()) bodyText.text = string.Format(dayCompleteFormat, WorldState.GetSaplingCount());
        replayButton.Select();
    }
}
