using System.Collections.Generic;
using UnityEngine;

// The Fabricator's levels in play order. Every press plays the next unsolved one (WorldState counts the solved ones across
// days); once all are solved a press makes its seeds straight away. One asset only → no CreateAssetMenu.
public class FabricatorLevelListSO : ScriptableObject
{
    public List<FabricatorPuzzleSO> puzzleSOList = new List<FabricatorPuzzleSO>();
}
