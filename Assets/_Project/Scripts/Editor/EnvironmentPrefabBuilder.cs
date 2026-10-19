using UnityEngine;
using static GrayboxBuild;

// Builds the static environment prefabs (houses, trees, fence, bridge, props). Re-running overwrites them in place.
public static class EnvironmentPrefabBuilder
{
    private const string FOLDER = "Environment";

    public static GameObject VillagerHouse(string name, Material roof)
    {
        GameObject house = BuildHouse(name, 5f, 2.8f, 5f, GrayboxPalette.Wall, roof);
        // hand-tuned in the editor (2026-10-19): steeper roof slabs set lower on the gable, slightly uneven per house
        Transform visual = house.transform.Find("Visual");
        switch (name)
        {
            case "VillagerHouse_A": SetRoof(visual, new Vector3(0f, 4.155389f, -1.27f), -44.808f, new Vector3(0f, 4.17f, 1.31f), 44.921f); break;
            case "VillagerHouse_B": SetRoof(visual, new Vector3(0f, 4.12f, -1.2f), -47.024f, new Vector3(0f, 4.14f, 1.24f), 44.461f); break;
            case "VillagerHouse_C": SetRoof(visual, new Vector3(0f, 4.13f, -1.21f), -46.186f, new Vector3(0f, 4.19f, 1.21f), 45.183f); break;
            case "VillagerHouse_D": SetRoof(visual, new Vector3(0f, 4.1f, -1.31f), -43.88f, new Vector3(0f, 4.16f, 1.2f), 44.919f); break;
        }
        return SavePrefab(house, FOLDER, name);
    }

    private static void SetRoof(Transform visual, Vector3 frontPosition, float frontAngle, Vector3 backPosition, float backAngle)
    {
        visual.Find("RoofFront").SetLocalPositionAndRotation(frontPosition, Quaternion.Euler(frontAngle, 0f, 0f));
        visual.Find("RoofBack").SetLocalPositionAndRotation(backPosition, Quaternion.Euler(backAngle, 0f, 0f));
    }

    // gable house, ridge along X, door facing -Z; root has a solid collider and a DoorAnchor child in front of the door
    public static GameObject BuildHouse(string name, float width, float height, float depth, Material wall, Material roof)
    {
        GameObject root = Root(name);
        Transform visual = Root("Visual", root.transform).transform;
        const float BASE = .25f;
        const float ROOF_ANGLE = 42f;
        float half = depth * .5f + .4f;
        float rise = half * Mathf.Tan(ROOF_ANGLE * Mathf.Deg2Rad);
        float slabLength = half / Mathf.Cos(ROOF_ANGLE * Mathf.Deg2Rad) + .15f;
        float top = BASE + height;

        Part(visual, "Base", GrayboxMeshFactory.RoundedBox(new Vector3(width + .5f, BASE, depth + .5f), .08f), GrayboxPalette.Stone, Vector3.zero);
        Part(visual, "Body", GrayboxMeshFactory.RoundedBox(new Vector3(width, height, depth), .18f), wall, new Vector3(0f, BASE, 0f));
        float gable = (depth - .2f) / Mathf.Sqrt(2f);
        PartCentered(visual, "Gable", GrayboxMeshFactory.RoundedBox(new Vector3(width - .1f, gable, gable), .1f), wall, new Vector3(0f, top, 0f), new Vector3(45f, 0f, 0f));
        Mesh slab = GrayboxMeshFactory.RoundedBox(new Vector3(width + .9f, .32f, slabLength), .12f);
        PartCentered(visual, "RoofFront", slab, roof, new Vector3(0f, top + rise * .5f + .12f, -half * .5f), new Vector3(-ROOF_ANGLE, 0f, 0f));
        PartCentered(visual, "RoofBack", slab, roof, new Vector3(0f, top + rise * .5f + .12f, half * .5f), new Vector3(ROOF_ANGLE, 0f, 0f));
        Part(visual, "Chimney", GrayboxMeshFactory.RoundedBox(new Vector3(.6f, 1.6f, .6f), .1f), GrayboxPalette.Stone, new Vector3(width * .28f, top + rise * .45f, half * .35f));

        float front = -depth * .5f;
        Part(visual, "DoorFrame", GrayboxMeshFactory.RoundedBox(new Vector3(1.4f, 2.15f, .2f), .08f), GrayboxPalette.WoodDark, new Vector3(0f, BASE, front - .02f));
        Part(visual, "Door", GrayboxMeshFactory.RoundedBox(new Vector3(1.1f, 1.95f, .2f), .08f), GrayboxPalette.Wood, new Vector3(0f, BASE, front - .1f));
        Part(visual, "Knob", GrayboxMeshFactory.Blob(.07f, .14f), GrayboxPalette.Accent, new Vector3(.38f, BASE + .95f, front - .22f));
        Part(visual, "Step", GrayboxMeshFactory.RoundedBox(new Vector3(1.8f, .14f, .8f), .06f), GrayboxPalette.Stone, new Vector3(0f, 0f, front - .55f));
        foreach (float x in new[] { -width * .3f, width * .3f })
        {
            Part(visual, "WindowFrame", GrayboxMeshFactory.RoundedBox(new Vector3(1.15f, 1.15f, .14f), .06f), GrayboxPalette.WoodDark, new Vector3(x, BASE + 1.1f, front - .02f));
            Part(visual, "Window", GrayboxMeshFactory.RoundedBox(new Vector3(.9f, .9f, .14f), .05f), GrayboxPalette.Window, new Vector3(x, BASE + 1.22f, front - .07f));
            Part(visual, "FlowerBox", GrayboxMeshFactory.RoundedBox(new Vector3(1.1f, .22f, .3f), .06f), GrayboxPalette.Wood, new Vector3(x, BASE + .95f, front - .2f));
        }
        foreach (float side in new[] { -1f, 1f })
        {
            Part(visual, "SideWindow", GrayboxMeshFactory.RoundedBox(new Vector3(.14f, .9f, .9f), .05f), GrayboxPalette.Window, new Vector3(side * (width * .5f + .03f), BASE + 1.2f, 0f));
        }

        Box(root, new Vector3(0f, top * .5f, 0f), new Vector3(width + .2f, top, depth + .2f));
        GameObject doorAnchor = Root("DoorAnchor", root.transform);
        doorAnchor.transform.localPosition = new Vector3(0f, 0f, front - 1.1f);
        return root;
    }

    public static GameObject RoundTree()
    {
        GameObject root = Root("TreeRound");
        Transform visual = Root("Visual", root.transform).transform;
        Part(visual, "Trunk", GrayboxMeshFactory.Taper(.3f, .2f, 1.6f, .06f, 12), GrayboxPalette.Trunk, Vector3.zero);
        Part(visual, "Crown", GrayboxMeshFactory.Blob(1.35f, 2f), GrayboxPalette.Foliage, new Vector3(0f, 1.15f, 0f));
        Part(visual, "CrownTop", GrayboxMeshFactory.Blob(.9f, 1.3f), GrayboxPalette.Foliage, new Vector3(.35f, 2.45f, -.15f));
        Part(visual, "CrownSide", GrayboxMeshFactory.Blob(.8f, 1.1f), GrayboxPalette.Foliage, new Vector3(-.75f, 1.6f, .3f));
        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1.2f, 0f);
        capsule.radius = .55f;
        capsule.height = 2.4f;
        return SavePrefab(root, FOLDER, "TreeRound");
    }

    public static GameObject PineTree()
    {
        GameObject root = Root("TreePine");
        Transform visual = Root("Visual", root.transform).transform;
        Part(visual, "Trunk", GrayboxMeshFactory.Taper(.26f, .18f, 1.1f, .05f, 12), GrayboxPalette.Trunk, Vector3.zero);
        Part(visual, "Tier1", GrayboxMeshFactory.Taper(1.5f, .15f, 1.9f, .12f, 14), GrayboxPalette.FoliageDark, new Vector3(0f, .8f, 0f));
        Part(visual, "Tier2", GrayboxMeshFactory.Taper(1.15f, .1f, 1.7f, .1f, 14), GrayboxPalette.FoliageDark, new Vector3(0f, 1.85f, 0f));
        Part(visual, "Tier3", GrayboxMeshFactory.Taper(.8f, .06f, 1.5f, .08f, 14), GrayboxPalette.FoliageDark, new Vector3(0f, 2.8f, 0f));
        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1.2f, 0f);
        capsule.radius = .55f;
        capsule.height = 2.4f;
        return SavePrefab(root, FOLDER, "TreePine");
    }

    public static GameObject Bush()
    {
        GameObject root = Root("Bush");
        Transform visual = Root("Visual", root.transform).transform;
        Part(visual, "A", GrayboxMeshFactory.Blob(.6f, .85f), GrayboxPalette.Foliage, Vector3.zero);
        Part(visual, "B", GrayboxMeshFactory.Blob(.45f, .65f), GrayboxPalette.Foliage, new Vector3(.55f, 0f, .1f));
        Part(visual, "C", GrayboxMeshFactory.Blob(.4f, .55f), GrayboxPalette.Foliage, new Vector3(-.45f, 0f, -.15f));
        Box(root, new Vector3(0f, .4f, 0f), new Vector3(1.8f, .8f, 1.1f));
        return SavePrefab(root, FOLDER, "Bush");
    }

    public static GameObject Rock()
    {
        GameObject root = Root("Rock");
        Transform visual = Root("Visual", root.transform).transform;
        Part(visual, "A", GrayboxMeshFactory.Blob(.7f, .6f, 10, 6), GrayboxPalette.Stone, Vector3.zero, new Vector3(0f, 20f, 6f));
        Part(visual, "B", GrayboxMeshFactory.Blob(.4f, .4f, 10, 6), GrayboxPalette.Stone, new Vector3(.6f, 0f, .2f));
        Box(root, new Vector3(.15f, .3f, 0f), new Vector3(1.5f, .6f, 1.3f));
        return SavePrefab(root, FOLDER, "Rock");
    }

    // one 2 m span along local +X: a post at x=0 and two rails
    public static GameObject FenceSpan()
    {
        GameObject root = Root("FenceSpan");
        Transform visual = Root("Visual", root.transform).transform;
        Part(visual, "Post", GrayboxMeshFactory.RoundedBox(new Vector3(.24f, 1.15f, .24f), .07f), GrayboxPalette.Wood, Vector3.zero);
        Part(visual, "PostCap", GrayboxMeshFactory.Taper(.16f, .02f, .16f, .02f, 8), GrayboxPalette.Wood, new Vector3(0f, 1.15f, 0f));
        Mesh rail = GrayboxMeshFactory.RoundedBox(new Vector3(2f, .14f, .1f), .04f);
        Part(visual, "RailLow", rail, GrayboxPalette.Wood, new Vector3(1f, .4f, 0f));
        Part(visual, "RailHigh", rail, GrayboxPalette.Wood, new Vector3(1f, .82f, 0f));
        Box(root, new Vector3(1f, .9f, 0f), new Vector3(2.05f, 1.8f, .4f));
        return SavePrefab(root, FOLDER, "FenceSpan");
    }

    // arched bridge crossing along X, 8 m long, 3.2 m wide
    public static GameObject Bridge()
    {
        GameObject root = Root("Bridge");
        Transform visual = Root("Visual", root.transform).transform;
        const int PLANKS = 9;
        const float LENGTH = 8f;
        Mesh plank = GrayboxMeshFactory.RoundedBox(new Vector3(.84f, .18f, 3f), .06f);
        for (int i = 0; i < PLANKS; i++)
        {
            float t = (i + .5f) / PLANKS;
            float x = Mathf.Lerp(-LENGTH * .5f, LENGTH * .5f, t);
            float y = .05f + .45f * Mathf.Sin(Mathf.PI * t);
            float slope = Mathf.Atan(.45f * Mathf.PI / LENGTH * Mathf.Cos(Mathf.PI * t)) * Mathf.Rad2Deg;
            PartCentered(visual, "Plank", plank, GrayboxPalette.Wood, new Vector3(x, y, 0f), new Vector3(0f, 0f, slope));
        }
        Mesh beam = GrayboxMeshFactory.RoundedBox(new Vector3(LENGTH + .4f, .28f, .28f), .08f);
        Part(visual, "BeamL", beam, GrayboxPalette.WoodDark, new Vector3(0f, -.25f, -1.25f));
        Part(visual, "BeamR", beam, GrayboxPalette.WoodDark, new Vector3(0f, -.25f, 1.25f));
        Mesh post = GrayboxMeshFactory.RoundedBox(new Vector3(.22f, 1.1f, .22f), .07f);
        float[] postXs = { -3.7f, -1.25f, 1.25f, 3.7f };
        foreach (float side in new[] { -1.55f, 1.55f })
        {
            for (int i = 0; i < postXs.Length; i++)
            {
                float y = .1f + .45f * Mathf.Sin(Mathf.PI * (postXs[i] / LENGTH + .5f));
                Part(visual, "Post", post, GrayboxPalette.WoodDark, new Vector3(postXs[i], y, side));
                if (i == 0) continue;
                float prevY = .1f + .45f * Mathf.Sin(Mathf.PI * (postXs[i - 1] / LENGTH + .5f));
                Vector3 a = new Vector3(postXs[i - 1], prevY + 1f, side), b = new Vector3(postXs[i], y + 1f, side);
                float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                PartCentered(visual, "Rail", GrayboxMeshFactory.RoundedBox(new Vector3(Vector3.Distance(a, b) + .2f, .14f, .14f), .05f), GrayboxPalette.Wood, (a + b) * .5f, new Vector3(0f, 0f, angle));
            }
            Box(root, new Vector3(0f, .8f, side * 1.07f), new Vector3(LENGTH, 1.6f, .25f));
        }
        Box(root, new Vector3(0f, .12f, 0f), new Vector3(LENGTH + .6f, .44f, 3f));
        return SavePrefab(root, FOLDER, "Bridge");
    }
}
