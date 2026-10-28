using UnityEngine;

// Shared by the board visuals: every sprite on the board is a child SpriteRenderer on the Puzzle layer (so only the puzzle
// camera films it), drawn with the skin's unlit sprite material. Sorting order decides what draws on top.
public static class FabricatorSpriteFactory
{
    public const int FRAME_ORDER = -5;
    public const int TILE_ORDER = 0;
    public const int ROOT_ORDER = 1;
    public const int MIST_ORDER = 2;
    public const int TILE_GLYPH_ORDER = 3;
    public const int HIGHLIGHT_ORDER = 4;
    public const int UNIT_ORDER = 5;
    public const int ARROW_ORDER = 10;

    public static SpriteRenderer Create(string name, Transform parent, Sprite sprite, Material material, int sortingOrder, Color color)
    {
        GameObject spriteGameObject = new GameObject(name) { layer = parent.gameObject.layer };
        spriteGameObject.transform.SetParent(parent, false);
        SpriteRenderer spriteRenderer = spriteGameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sharedMaterial = material;
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.color = color;
        return spriteRenderer;
    }

    // Unity angle of a grid direction (0 = right)
    public static float GetAngle(Vector2Int direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
}
