using System.Collections.Generic;
using UnityEngine;

// What the MC pictures on Day 3 (CozyFarm map): while he looks at the field across the river, a see-through copy of a farm
// fades in on it and the trees standing there fade out; while he thinks about a bridge, a see-through bridge fades in over
// the river. More solid than the occlusion fade, so it reads as a picture, not as scenery getting out of the way.
public class ImaginationVisual : MonoBehaviour
{
    [SerializeField] private LandSequence landSequence;
    [SerializeField] private BridgeBuild bridgeBuild;
    [Tooltip("Copies of farm plots and crops on the new field, models only.")]
    [SerializeField] private GameObject ghostFarmRoot;
    [Tooltip("A copy of the bridge in its final place, model only.")]
    [SerializeField] private GameObject ghostBridgeRoot;
    [Tooltip("The trees on the new field: gone from the picture while the farm is imagined.")]
    [SerializeField] private Transform[] imaginedAwayTreeArray;
    [SerializeField] private float ghostAlpha = .6f;
    [SerializeField] private float fadeSpeed = 1.5f;

    private class Ghost
    {
        public GameObject root;
        public List<SeeThroughRenderer> seeThroughList;
        // renderers that can't turn see-through are simply hidden or shown
        public List<Renderer> plainList;
        public float alpha;
    }

    private Ghost farmGhost;
    private Ghost bridgeGhost;
    private Ghost treeGhost;

    private void Start()
    {
        farmGhost = CreateGhost(ghostFarmRoot);
        bridgeGhost = CreateGhost(ghostBridgeRoot);
        treeGhost = new Ghost { seeThroughList = new List<SeeThroughRenderer>(), plainList = new List<Renderer>(), alpha = 1f };
        foreach (Transform tree in imaginedAwayTreeArray) AddRenderers(treeGhost, tree.gameObject);
        ghostFarmRoot.SetActive(false);
        ghostBridgeRoot.SetActive(false);
    }

    private void Update()
    {
        LandSequence.Step landStep = landSequence.GetStep();
        bool isFarmImagined = landStep == LandSequence.Step.Inspect || landStep == LandSequence.Step.Realise;
        bool isBridgeImagined = bridgeBuild.GetStep() == BridgeBuild.Step.Think || bridgeBuild.GetStep() == BridgeBuild.Step.Idea;

        UpdateGhost(farmGhost, isFarmImagined ? ghostAlpha : 0f);
        UpdateGhost(bridgeGhost, isBridgeImagined ? ghostAlpha : 0f);
        // the trees go while the farm is pictured; a felled one is already switched off by its fall
        UpdateGhost(treeGhost, isFarmImagined ? 0f : 1f);
    }

    private void OnDestroy()
    {
        foreach (Ghost ghost in new[] { farmGhost, bridgeGhost, treeGhost })
        {
            if (ghost == null) continue;
            foreach (SeeThroughRenderer seeThrough in ghost.seeThroughList) seeThrough.Dispose();
        }
    }

    private void UpdateGhost(Ghost ghost, float targetAlpha)
    {
        if (Mathf.Approximately(ghost.alpha, targetAlpha)) return;
        ghost.alpha = Mathf.MoveTowards(ghost.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        if (ghost.root != null) ghost.root.SetActive(ghost.alpha > 0f);
        foreach (SeeThroughRenderer seeThrough in ghost.seeThroughList) seeThrough.SetAlpha(ghost.alpha);
        foreach (Renderer plain in ghost.plainList) plain.enabled = ghost.alpha > .5f;
    }

    // a ghost is never solid: its renderers swap to their transparent copies right away, and any that can't stay hidden
    private static Ghost CreateGhost(GameObject root)
    {
        Ghost ghost = new Ghost { root = root, seeThroughList = new List<SeeThroughRenderer>(), plainList = new List<Renderer>() };
        AddRenderers(ghost, root);
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
