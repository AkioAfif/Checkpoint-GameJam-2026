using UnityEngine;

// The story's chapter titles, shown on black as each chapter opens. Index = chapter number (Chapter 0 is Day 1's
// bedroom morning, Chapter N is Day N's).
// one asset only → no CreateAssetMenu
public class ChapterListSO : ScriptableObject
{
    public string[] chapterTitleArray;
    [Tooltip("Seconds each line of the title card stays on the black screen.")]
    public float holdDuration = 2.5f;
}
