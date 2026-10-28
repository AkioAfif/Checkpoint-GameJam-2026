using UnityEngine;

// The Fabricator board's art (the Sawit-Fabricator sprites, 512 px = 1 cell) and tints. One asset only → no CreateAssetMenu.
public class FabricatorBoardSkinSO : ScriptableObject
{
    [Tooltip("Sprite-Unlit-Default: sprites in this 3D renderer shouldn't pick up the diorama's light.")]
    public Material spriteMaterial;

    [Header("Board")]
    public Sprite floorSprite;
    public Sprite wallSprite;
    public Sprite frameSprite;
    public Color floorColor = Color.white;
    public Color floorOffsetColor = new Color(.95f, .91f, .84f);

    [Header("Clickable tiles")]
    public Sprite gateOpenSprite;
    public Sprite gateClosedSprite;
    public Sprite routerNodeSprite;
    public Sprite routerArrowSprite;
    public Sprite injectorIdleSprite;
    public Sprite injectorReleasedSprite;
    public Color areaInjectorColor = new Color(.75f, .9f, 1f);
    public Sprite mistSprite;
    // the gland's cyan: Sawit's pale cream vanishes on this amber board
    public Color mistColor = new Color(.45f, .9f, 1f);
    [Range(0f, 1f)] public float mistAlpha = .4f;

    [Header("Units")]
    public Sprite bugSprite;
    public Sprite embryoSprite;
    public Sprite sentinelSprite;
    public Color thornySentinelColor = new Color(1f, .62f, .62f);

    [Header("Roots")]
    public Sprite rootStemSprite;
    public Sprite thornyRootStemSprite;
    public Sprite rootKnotSprite;
    public Sprite rootTipSprite;

    [Header("Arrows")]
    public Sprite arrowSprite;
    public Color arrowColor = new Color(1f, .85f, .2f, .9f);
    public Color dangerArrowColor = new Color(1f, .25f, .3f, .95f);
    public Color hoverColor = Color.white;
    public Color selectedColor = new Color(.25f, .9f, .95f, .8f);
}
