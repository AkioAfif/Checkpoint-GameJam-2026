using System.Collections.Generic;
using UnityEngine;

// What the MC pictures on the CozyFarm map: while he looks at land he wants to farm, a see-through copy of a farm fades in on
// it and what stands there (trees, weeds, a stump) fades out; while he thinks about a bridge (Day 4), a see-through bridge
// fades in over the river. The picture twinkles between faint and nearly solid, so it reads as imagined, not as scenery.
// Day 4 drives it from LandSequence and BridgeBuild, the yard day (Day 3) from YardDayDirector; the others stay empty.
public class ImaginationVisual : MonoBehaviour
{
    [SerializeField] private LandSequence landSequence;
    [SerializeField] private BridgeBuild bridgeBuild;
    [SerializeField] private YardDayDirector yardDayDirector;
    [Tooltip("Copies of farm plots and crops on the new land, models only.")]
    [SerializeField] private GameObject ghostFarmRoot;
    [Tooltip("Optional: a copy of the bridge in its final place, model only.")]
    [SerializeField] private GameObject ghostBridgeRoot;
    [Tooltip("What stands on the new land: gone from the picture while the farm is imagined.")]
    [SerializeField] private Transform[] imaginedAwayTreeArray;
    [Tooltip("The imagined farm or bridge twinkles between these opacities.")]
    [SerializeField] private float ghostMinAlpha = .2f;
    [SerializeField] private float ghostMaxAlpha = .85f;
    [Tooltip("Seconds from faint to solid and back.")]
    [SerializeField] private float twinklePeriod = 1.1f;
    [SerializeField] private float fadeSpeed = 1.5f;

    private class Ghost
    {
        public GameObject root;
        public List<SeeThroughRenderer> seeThroughList;
        // renderers that can't turn see-through are simply hidden or shown
        public List<Renderer> plainList;
        // how far it has faded in (0 gone, 1 there); the twinkle rides on top
        public float presence;
    }

    private Ghost farmGhost;
    private Ghost bridgeGhost;
    private Ghost treeGhost;

    private void Start()
    {
        farmGhost = CreateGhost(ghostFarmRoot);
        bridgeGhost = CreateGhost(ghostBridgeRoot);
        treeGhost = new Ghost { seeThroughList = new List<SeeThroughRenderer>(), plainList = new List<Renderer>(), presence = 1f };
        foreach (Transform tree in imaginedAwayTreeArray) AddRenderers(treeGhost, tree.gameObject);
        ghostFarmRoot.SetActive(false);
        if (ghostBridgeRoot != null) ghostBridgeRoot.SetActive(false);
    }

    private void Update()
    {
        bool isFarmImagined = yardDayDirector != null ? yardDayDirector.IsWishShot() : IsLandImagined();
        bool isBridgeImagined = bridgeBuild != null && (bridgeBuild.GetStep() == BridgeBuild.Step.Think || bridgeBuild.GetStep() == BridgeBuild.Step.Idea);

        float twinkle = Mathf.Lerp(ghostMinAlpha, ghostMaxAlpha, .5f + .5f * Mathf.Sin(Time.time * 2f * Mathf.PI / twinklePeriod));
        UpdateGhost(farmGhost, isFarmImagined ? 1f : 0f, twinkle);
        UpdateGhost(bridgeGhost, isBridgeImagined ? 1f : 0f, twinkle);
        // the trees go while the farm is pictured; a felled one is already switched off by its fall
        UpdateGhost(treeGhost, isFarmImagined ? 0f : 1f, 1f);
    }

    private void OnDestroy()
    {
        foreach (Ghost ghost in new[] { farmGhost, bridgeGhost, treeGhost })
        {
            if (ghost == null) continue;
            foreach (SeeThroughRenderer seeThrough in ghost.seeThroughList) seeThrough.Dispose();
        }
    }

    private bool IsLandImagined()
    {
        LandSequence.Step landStep = landSequence.GetStep();
        return landStep == LandSequence.Step.Inspect || landStep == LandSequence.Step.Realise;
    }

    // 'opacity' is what it shows once fully faded in: the twinkle for a ghost, 1 for what fades away
    private void UpdateGhost(Ghost ghost, float targetPresence, float opacity)
    {
        bool isSettled = Mathf.Approximately(ghost.presence, targetPresence);
        // a ghost on screen keeps twinkling; anything else only changes while it fades
        if (isSettled && (ghost.presence == 0f || Mathf.Approximately(opacity, 1f))) return;
        ghost.presence = Mathf.MoveTowards(ghost.presence, targetPresence, fadeSpeed * Time.deltaTime);
        float alpha = ghost.presence * opacity;
        if (ghost.root != null) ghost.root.SetActive(ghost.presence > 0f);
        foreach (SeeThroughRenderer seeThrough in ghost.seeThroughList) seeThrough.SetAlpha(alpha);
        foreach (Renderer plain in ghost.plainList) plain.enabled = alpha > .5f;
    }

    // a ghost is never solid: its renderers swap to their transparent copies right away, and any that can't stay hidden
    private static Ghost CreateGhost(GameObject root)
    {
        Ghost ghost = new Ghost { root = root, seeThroughList = new List<SeeThroughRenderer>(), plainList = new List<Renderer>() };
        if (root != null) AddRenderers(ghost, root);
        foreach (SeeThroughRenderer seeThrough in ghost.seeThroughList) seeThrough.SetAlpha(0f);
        foreach (Renderer plain in ghost.plainList) plain.enabled = false;
        ghost.plainList.Clear();
        return ghost;
    }

    private static void AddRenderers(Ghost ghost, GameObject root)
    {
        foreach (Renderer part in root.GetComponentsInChildren<Renderer>(true))
        {
            SeeThroughRenderer seeThrough = SeeThroughRenderer.TryCreate(part, true);
            if (seeThrough == null) ghost.plainList.Add(part);
            else ghost.seeThroughList.Add(seeThrough);
        }
    }
}
