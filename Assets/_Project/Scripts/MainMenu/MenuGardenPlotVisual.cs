using UnityEngine;

// Shows the plant model for the bed's stage (nothing when empty).
public class MenuGardenPlotVisual : MonoBehaviour
{
    [SerializeField] private MenuGardenPlot plot;
    [SerializeField] private GameObject sproutGameObject;
    [SerializeField] private GameObject grownGameObject;
    [SerializeField] private GameObject ripeGameObject;

    // in Awake: the plot announces its starting stage in its Start
    private void Awake()
    {
        plot.OnStateChanged += Plot_OnStateChanged;
    }

    private void Plot_OnStateChanged(object sender, MenuGardenPlot.OnStateChangedEventArgs e)
    {
        sproutGameObject.SetActive(e.state == MenuGardenPlot.State.Sprout);
        grownGameObject.SetActive(e.state == MenuGardenPlot.State.Grown);
        ripeGameObject.SetActive(e.state == MenuGardenPlot.State.Ripe);
    }
}
