using System;
using UnityEngine;

// The yard day's new beds: each tilled patch shows its bed (a piece cut from the 3x4 garden model, under its plot, so the
// patch's reveal shows it), and once the last one is tilled the old 3x3 garden gives way to the whole 3x4 model, outer fence and all.
public class GardenExpansionVisual : MonoBehaviour
{
    [SerializeField] private YardObstacle[] tillArray;
    [SerializeField] private GameObject oldGarden;
    [Tooltip("The 3x4 garden, hidden until the last patch is tilled.")]
    [SerializeField] private GameObject expandedGarden;
    [Tooltip("The bed pieces; the whole model replaces them.")]
    [SerializeField] private GameObject[] bedPieceArray;

    private void Start()
    {
        foreach (YardObstacle till in tillArray) till.OnCleared += Till_OnCleared;
    }

    private void Till_OnCleared(object sender, EventArgs e)
    {
        foreach (YardObstacle till in tillArray)
        {
            if (!till.IsCleared()) return;
        }
        oldGarden.SetActive(false);
        foreach (GameObject bedPiece in bedPieceArray) bedPiece.SetActive(false);
        expandedGarden.SetActive(true);
    }
}
