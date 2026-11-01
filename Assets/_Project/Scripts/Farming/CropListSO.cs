using System.Collections.Generic;
using UnityEngine;

// Every crop the player can plant; a plot offers whichever of their seeds are in the bag. One asset only → no CreateAssetMenu.
public class CropListSO : ScriptableObject
{
    public List<CropDataSO> cropDataSOList;
}
