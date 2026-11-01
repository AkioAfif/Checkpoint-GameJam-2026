using UnityEditor;

// The chapter titles (ScriptableObjects/Chapters/ChapterList.asset), index = chapter number. The text only seeds the
// asset; designers edit the .asset file afterwards. The bedroom and Day 1's scene builders wire it.
public static class ChapterDataBuilder
{
    [MenuItem("Tools/Story/Build Chapter List")]
    public static ChapterListSO Build()
    {
        ChapterListSO chapterList = GameDataBuilder.Asset<ChapterListSO>("Chapters", "ChapterList");
        chapterList.chapterTitleArray = new[]
        {
            "Chapter 0 — The Inheritance",
            "Chapter 1 — The Gift",
            "Chapter 2 — More Land, More Money",
            "Chapter 3 — The First Trees Fall",
            "Chapter 4 — The Accusation",
            "Chapter 5 — The Perfect Recipe",
            "Chapter 6 — The Cost of Success",
            "Chapter 7 — Bought Silence",
            "Chapter 8 — The Missing",
            "Chapter 9 — The Harvest of Nothing",
        };
        chapterList.holdDuration = 2.5f;
        EditorUtility.SetDirty(chapterList);
        AssetDatabase.SaveAssets();
        return chapterList;
    }
}
