using System;
using System.Collections.Generic;
using UnityEngine;

// Draws each Root Sentinel's bramble roots (the Sawit-Fabricator vine network): a cheapest-path search from the host through
// the cells it covers gives every cell a parent, so roots run straight and only cut corners when they must. Each cell gets
// half stems toward its parent and children plus a tip (end) or knot (bend/branch). Roots grow outward when they come out
// and pull back inward when an injector gland retracts them or the sentinel is captured.
public class FabricatorRootsVisual : MonoBehaviour
{
    private const int STRAIGHT_STEP_COST = 2;
    private const int DIAGONAL_STEP_COST = 5;
    private const float DIAGONAL_LENGTH = 1.41421356f;
    // the art is drawn thin; scaled up so a covered cell clearly reads as blocked
    private const float STEM_THICKNESS = 1.6f;
    private const float CENTRE_PIECE_SCALE = 1.3f;
    private const float GROW_DURATION = .25f;
    // extra delay per step away from the host
    private const float GROW_STAGGER = .06f;

    private static readonly Vector2Int[] NeighbourStepArray =
    {
        new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1),
    };

    private class RootSegment
    {
        public Transform transform;
        public int depth;
        public float scale;
        public float startScale;
        public float targetScale;
        public float startTime;
    }

    private class RootNetwork
    {
        public FabricatorBoardUnit sentinel;
        public readonly List<RootSegment> segmentList = new List<RootSegment>();
        public int maxDepth;
        public bool isShown;
    }

    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private FabricatorBoardSkinSO skinSO;

    private Transform rootsRoot;
    private readonly List<RootNetwork> networkList = new List<RootNetwork>();

    private void Start()
    {
        miniGame.OnBuilt += MiniGame_OnBuilt;
        miniGame.OnEnded += MiniGame_OnEnded;
    }

    private void Update()
    {
        if (rootsRoot == null) return;
        float cellSize = miniGame.GetCellSize();
        foreach (RootNetwork network in networkList)
        {
            bool shouldShow = network.sentinel.AreRootsOut();
            if (shouldShow != network.isShown) StartGrowing(network, shouldShow);
            foreach (RootSegment segment in network.segmentList)
            {
                float t = Mathf.Clamp01((Time.time - segment.startTime) / GROW_DURATION);
                // ease out, so tendrils snap into place
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                segment.scale = Mathf.Lerp(segment.startScale, segment.targetScale, eased);
                segment.transform.localScale = Vector3.one * segment.scale * cellSize;
                segment.transform.gameObject.SetActive(segment.scale > 0f);
            }
        }
    }

    private void MiniGame_OnBuilt(object sender, EventArgs e)
    {
        DestroyRoots();
        rootsRoot = new GameObject("Roots") { layer = gameObject.layer }.transform;
        rootsRoot.SetParent(transform, false);
        foreach (FabricatorBoardUnit unit in miniGame.GetBoard().GetUnitList())
            if (unit.type == FabricatorBoardUnit.Type.Sentinel) networkList.Add(BuildNetwork(unit));
    }

    private void MiniGame_OnEnded(object sender, EventArgs e) => DestroyRoots();

    private void StartGrowing(RootNetwork network, bool isGrowing)
    {
        network.isShown = isGrowing;
        foreach (RootSegment segment in network.segmentList)
        {
            segment.startScale = segment.scale;
            segment.targetScale = isGrowing ? 1f : 0f;
            // grow outward from the host, retract inward toward it
            segment.startTime = Time.time + (isGrowing ? segment.depth : network.maxDepth - segment.depth) * GROW_STAGGER;
        }
    }

    private RootNetwork BuildNetwork(FabricatorBoardUnit sentinel)
    {
        RootNetwork network = new RootNetwork { sentinel = sentinel };
        Vector2Int host = sentinel.cell;
        HashSet<Vector2Int> coveredSet = new HashSet<Vector2Int>(sentinel.rootCellList);

        Dictionary<Vector2Int, Vector2Int> parentDictionary = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> depthDictionary = new Dictionary<Vector2Int, int> { [host] = 0 };
        Dictionary<Vector2Int, int> costDictionary = new Dictionary<Vector2Int, int> { [host] = 0 };
        List<Vector2Int> openList = new List<Vector2Int> { host };
        while (openList.Count > 0)
        {
            // a handful of cells, so a linear scan for the cheapest is fine
            Vector2Int current = openList[0];
            foreach (Vector2Int candidate in openList)
                if (costDictionary[candidate] < costDictionary[current]) current = candidate;
            openList.Remove(current);
            foreach (Vector2Int step in NeighbourStepArray)
            {
                Vector2Int next = current + step;
                if (!coveredSet.Contains(next)) continue;
                int cost = costDictionary[current] + (step.x != 0 && step.y != 0 ? DIAGONAL_STEP_COST : STRAIGHT_STEP_COST);
                if (costDictionary.TryGetValue(next, out int knownCost) && knownCost <= cost) continue;
                costDictionary[next] = cost;
                parentDictionary[next] = current;
                depthDictionary[next] = depthDictionary[current] + 1;
                if (!openList.Contains(next)) openList.Add(next);
            }
        }
        // cells that don't touch the rest of the roots still get a stub pointing back toward the host
        foreach (Vector2Int cell in coveredSet)
        {
            if (depthDictionary.ContainsKey(cell)) continue;
            Vector2Int towardHost = host - cell;
            parentDictionary[cell] = cell + new Vector2Int(Math.Sign(towardHost.x), Math.Sign(towardHost.y));
            depthDictionary[cell] = Mathf.Max(Mathf.Abs(towardHost.x), Mathf.Abs(towardHost.y));
        }

        Dictionary<Vector2Int, List<Vector2Int>> childDictionary = new Dictionary<Vector2Int, List<Vector2Int>>();
        foreach (KeyValuePair<Vector2Int, Vector2Int> pair in parentDictionary)
        {
            if (!childDictionary.TryGetValue(pair.Value, out List<Vector2Int> childList)) childDictionary[pair.Value] = childList = new List<Vector2Int>();
            childList.Add(pair.Key - pair.Value);
        }

        Sprite stemSprite = sentinel.isThorny ? skinSO.thornyRootStemSprite : skinSO.rootStemSprite;
        // the host's own cell draws the stems leaving it (under the sentinel's sprite)
        if (childDictionary.TryGetValue(host, out List<Vector2Int> hostChildList)) AddSegment(network, host, Vector2Int.zero, hostChildList, 0, stemSprite);
        foreach (Vector2Int cell in coveredSet)
        {
            List<Vector2Int> childList = childDictionary.TryGetValue(cell, out List<Vector2Int> list) ? list : new List<Vector2Int>();
            AddSegment(network, cell, parentDictionary[cell] - cell, childList, depthDictionary[cell], stemSprite);
            network.maxDepth = Mathf.Max(network.maxDepth, depthDictionary[cell]);
        }
        return network;
    }

    private void AddSegment(RootNetwork network, Vector2Int cell, Vector2Int parentDirection, List<Vector2Int> childDirectionList, int depth, Sprite stemSprite)
    {
        Transform segmentTransform = new GameObject($"Root {cell}") { layer = gameObject.layer }.transform;
        segmentTransform.SetParent(rootsRoot, false);
        segmentTransform.position = miniGame.GetCellWorldPosition(cell);
        segmentTransform.localScale = Vector3.zero;
        segmentTransform.gameObject.SetActive(false);

        if (parentDirection != Vector2Int.zero) AddStem(segmentTransform, parentDirection, stemSprite);
        foreach (Vector2Int childDirection in childDirectionList) AddStem(segmentTransform, childDirection, stemSprite);
        if (parentDirection != Vector2Int.zero)
        {
            bool isTip = childDirectionList.Count == 0;
            bool isStraight = childDirectionList.Count == 1 && childDirectionList[0] == -parentDirection;
            // a tip points away from the stem it grows out of; bends and branches get a knot; straight pieces join seamlessly
            if (isTip) AddCentrePiece(segmentTransform, skinSO.rootTipSprite, -parentDirection);
            else if (!isStraight) AddCentrePiece(segmentTransform, skinSO.rootKnotSprite, Vector2Int.right);
        }
        network.segmentList.Add(new RootSegment { transform = segmentTransform, depth = depth });
    }

    // a half stem from the cell centre toward one neighbour; diagonals are stretched to reach the corner
    private void AddStem(Transform segmentTransform, Vector2Int direction, Sprite stemSprite)
    {
        float length = direction.x != 0 && direction.y != 0 ? DIAGONAL_LENGTH : 1f;
        SpriteRenderer stem = FabricatorSpriteFactory.Create("Stem", segmentTransform, stemSprite, skinSO.spriteMaterial, FabricatorSpriteFactory.ROOT_ORDER, Color.white);
        stem.transform.localRotation = Quaternion.Euler(0f, 0f, FabricatorSpriteFactory.GetAngle(direction));
        stem.transform.localScale = new Vector3(length, STEM_THICKNESS, 1f);
    }

    private void AddCentrePiece(Transform segmentTransform, Sprite sprite, Vector2Int facing)
    {
        SpriteRenderer piece = FabricatorSpriteFactory.Create("Centre", segmentTransform, sprite, skinSO.spriteMaterial, FabricatorSpriteFactory.ROOT_ORDER, Color.white);
        // a little closer to the camera than the stems, so it covers the joint
        piece.transform.localPosition = new Vector3(0f, 0f, -.01f);
        piece.transform.localRotation = Quaternion.Euler(0f, 0f, FabricatorSpriteFactory.GetAngle(facing));
        piece.transform.localScale = Vector3.one * CENTRE_PIECE_SCALE;
    }

    private void DestroyRoots()
    {
        networkList.Clear();
        if (rootsRoot != null) Destroy(rootsRoot.gameObject);
        rootsRoot = null;
    }
}
