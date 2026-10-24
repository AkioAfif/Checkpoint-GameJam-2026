using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Placeholder for the next level (not built yet). It reports what the days so far left in the world.
public class StubSceneUI : MonoBehaviour
{
    [SerializeField] private Button replayButton;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private string dayCompleteFormat = "Day 1 complete!\nThe Fabricator stays by your house and {0} sawit saplings are growing in the field.";
    // {0} debt left, {1} days until it is due
    [SerializeField] private string day2CompleteFormat = "Day 2 complete!\nThe new sawit is growing, and Rigel is watching.\n{0} Koin of debt left, {1} days to go.";

    private void Awake()
    {
        Time.timeScale = 1f;
        replayButton.onClick.AddListener(() =>
        {
            WorldState.Clear();
            Loader.Load(Loader.Scene.HouseInterior);
        });
    }

    private void Start()
    {
        if (WorldState.IsDay2Complete()) bodyText.text = string.Format(day2CompleteFormat, Koin.Format(WorldState.GetDebt()), WorldState.GetDaysUntilDue());
        else if (WorldState.IsDay1Complete()) bodyText.text = string.Format(dayCompleteFormat, WorldState.GetSaplingCount());
        replayButton.Select();
    }
}
