using UnityEditor;
using UnityEngine;
using static GrayboxBuild;

// Day 1 prefabs: the pickup truck (Tools/Blender/build_truck.py), Sari on the bridge and Mother in the doorway.
public static class Day1PrefabBuilder
{
    private const string TRUCK_FBX = "Assets/_Project/Art/Models/Truck/DistributorTruck.fbx";

    // a plain copy of an imported model (so children can be added to it) with its materials swapped by slot name
    public static GameObject ImportModel(string fbxPath, Transform parent, System.Func<string, Material> materialForSlot)
    {
        GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath), parent);
        model.name = "Model";
        foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>())
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = materialForSlot(materials[i].name);
            renderer.sharedMaterials = materials;
        }
        return model;
    }

    // front = local +Z. Start/stop/exit points are wired per scene; the Fabricator rides on the CargoAnchor.
    public static GameObject Truck()
    {
        GameObject root = Root("DistributorTruck");
        DistributorTruck truck = root.AddComponent<DistributorTruck>();
        GameObject visual = Root("Visual", root.transform);
        GameObject model = ImportModel(TRUCK_FBX, visual.transform, TruckMaterial);
        DistributorTruckVisual truckVisual = visual.AddComponent<DistributorTruckVisual>();
        SetField(truckVisual, "truck", truck);
        SetField(truckVisual, "body", model.transform);
        SetArray(truckVisual, "wheelArray", new Object[] { model.transform.Find("WheelFL"), model.transform.Find("WheelFR"), model.transform.Find("WheelRL"), model.transform.Find("WheelRR") });
        SetField(truck, "cargoAnchor", Anchor(root, "CargoAnchor", new Vector3(0f, 1.05f, -1.55f)));
        // the cab door on the left, where the Distributor tumbles out
        SetField(truck, "doorPoint", Anchor(root, "DoorPoint", new Vector3(-1.9f, 0f, .1f)));
        return SavePrefab(root, "Day1", "DistributorTruck");
    }

    // a villager who keeps waving
    public static GameObject Sari(NpcSO npcSO)
    {
        GameObject prefab = InteractablePrefabBuilder.Villager("Npc_Sari", CharacterBuilder.SariLook, npcSO);
        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        Transform visual = contents.transform.Find("Visual");
        EmoteLoopVisual loop = visual.gameObject.AddComponent<EmoteLoopVisual>();
        SetField(loop, "animator", visual.GetComponentInChildren<Animator>());
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // not interactable: no collider, not on the Interactable layer. She just nods now and then.
    public static GameObject Mother()
    {
        GameObject root = Root("Npc_Mother");
        GameObject visual = Root("Visual", root.transform);
        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(CharacterBuilder.RigPrefab(CharacterBuilder.MotherLook), visual.transform);
        EmoteLoopVisual loop = visual.AddComponent<EmoteLoopVisual>();
        SetField(loop, "animator", rig.GetComponent<Animator>());
        SerializedObject so = new SerializedObject(loop);
        so.FindProperty("emote").enumValueIndex = (int)Emote.Nod;
        so.FindProperty("interval").floatValue = 3.6f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return SavePrefab(root, "Day1", "Npc_Mother");
    }

    private static Material TruckMaterial(string slot)
    {
        switch (slot)
        {
            case "Paint": return GrayboxPalette.Lit("TruckPaint", new Color(.93f, .46f, .24f));
            case "PaintTrim": return GrayboxPalette.Lit("TruckTrim", new Color(.99f, .83f, .42f));
            case "Dark": return GrayboxPalette.DarkGray;
            case "Glass": return GrayboxPalette.Lit("TruckGlass", new Color(.72f, .88f, .96f), .8f);
            case "Chrome": return GrayboxPalette.Metal;
            case "Light": return GrayboxPalette.Lit("TruckLight", new Color(1f, .93f, .55f), .6f);
            case "Rubber": return GrayboxPalette.Lit("TruckRubber", new Color(.14f, .14f, .17f), .15f);
        }
        Debug.LogError($"DistributorTruck.fbx has an unknown material slot '{slot}'");
        return GrayboxPalette.DarkGray;
    }

    private static Transform Anchor(GameObject parent, string name, Vector3 localPosition)
    {
        GameObject anchor = Root(name, parent.transform);
        anchor.transform.localPosition = localPosition;
        return anchor.transform;
    }
}
