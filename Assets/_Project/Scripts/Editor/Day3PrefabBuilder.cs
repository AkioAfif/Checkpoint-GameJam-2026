using TMPro;
using UnityEngine;
using static GrayboxBuild;

// Day 3 prefabs: Darto's For Sale sign at the corner of his strip (the ripe Lv.2 palm lives with the other palms in
// Day2PrefabBuilder).
public static class Day3PrefabBuilder
{
    // a post with a hand-painted board; the board's front faces -Z (the camera side)
    public static GameObject ForSaleSign()
    {
        GameObject root = Root("ForSaleSign");
        Box(root, new Vector3(0f, .7f, 0f), new Vector3(1.2f, 1.4f, .2f));
        Part(root.transform, "Post", GrayboxMeshFactory.RoundedBox(new Vector3(.12f, 1.5f, .12f), .03f), GrayboxPalette.WoodDark, Vector3.zero);
        GameObject board = Part(root.transform, "Board", GrayboxMeshFactory.RoundedBox(new Vector3(1.1f, .6f, .08f), .04f), GrayboxPalette.Wood, new Vector3(0f, .95f, -.08f));
        TextMeshPro label = Root("Label", board.transform).AddComponent<TextMeshPro>();
        label.text = "DIJUAL\n<size=60%>For Sale</size>";
        label.fontSize = 2.2f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.55f, .14f, .1f);
        label.rectTransform.sizeDelta = new Vector2(1f, .55f);
        label.rectTransform.localPosition = new Vector3(0f, .3f, -.05f);
        return SavePrefab(root, "Environment", "ForSaleSign");
    }
}
