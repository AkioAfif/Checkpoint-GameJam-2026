using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static GrayboxBuild;

// Chibi rig made of soft primitives + its keyframed clips and the shared Chibi.controller.
public static class CharacterBuilder
{
    private const string ANIMATION_FOLDER = "Assets/_Project/Art/Animations";
    private const string CONTROLLER_PATH = ANIMATION_FOLDER + "/Chibi.controller";
    private const float HIPS_Y = .42f;
    private const string DISTRIBUTOR_FOLDER = "Assets/_Project/Art/Models/Distributor";
    private const string DISTRIBUTOR_FBX = DISTRIBUTOR_FOLDER + "/Distributor.fbx";
    private const string GENERATED_FOLDER = "Assets/_Project/Art/Models/Generated";
    private const string MEADOW_FOLDER = "Assets/_Project/Art/Models/Meadow";
    private const string MEADOW_FBX = MEADOW_FOLDER + "/MC_Meadow.fbx";
    private const string MEADOW_FACE = MEADOW_FOLDER + "/MC_Face.png";
    private const string NPC_FOLDER = "Assets/_Project/Art/Models/Npcs";

    public enum Accessory { None, Bun, Cap }

    public struct Look
    {
        public string name;
        public Color cloth;
        public Color hair;
        public Accessory accessory;
        public float scale;
    }

    public static readonly Look RatnaLook = new Look { name = "Ratna", cloth = new Color(.93f, .63f, .69f), hair = new Color(.26f, .21f, .21f), accessory = Accessory.Bun, scale = 1f };
    public static readonly Look DartoLook = new Look { name = "Darto", cloth = new Color(.56f, .73f, .56f), hair = new Color(.62f, .62f, .64f), accessory = Accessory.Cap, scale = 1.05f };
    public static readonly Look DimasLook = new Look { name = "Dimas", cloth = new Color(.98f, .73f, .43f), hair = new Color(.2f, .18f, .18f), accessory = Accessory.None, scale = .85f };
    public static readonly Look IjahLook = new Look { name = "Ijah", cloth = new Color(.73f, .63f, .86f), hair = new Color(.93f, .93f, .93f), accessory = Accessory.Bun, scale = .95f };
    public static readonly Look SariLook = new Look { name = "Sari", cloth = new Color(.40f, .76f, .74f), hair = new Color(.15f, .12f, .13f), accessory = Accessory.None, scale = .92f };
    public static readonly Look RigelLook = new Look { name = "Rigel", cloth = new Color(.80f, .45f, .33f), hair = new Color(.12f, .10f, .10f), accessory = Accessory.Cap, scale = 1.02f };

    // rig prefab (Animator + body parts), nested inside the NPC prefabs and spawned in the portrait booth;
    // villagers remodelled in Blender use their FBX instead of the primitive chibi
    public static GameObject RigPrefab(Look look)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(NpcFbx(look.name)) != null) return NpcRigPrefab(look.name);
        GameObject rig = AnimatedRig($"Rig_{look.name}");
        rig.transform.localScale = Vector3.one * look.scale;

        Material cloth = GrayboxPalette.Cloth(look.name, look.cloth);
        Material hair = GrayboxPalette.Lit("Hair" + look.name, look.hair);
        Material cheek = GrayboxPalette.Lit("Cheek", new Color(.98f, .70f, .70f));

        Transform hips = Root("Hips", rig.transform).transform;
        hips.localPosition = new Vector3(0f, HIPS_Y, 0f);
        Part(hips, "Torso", GrayboxMeshFactory.Blob(.3f, .6f), cloth, new Vector3(0f, -.05f, 0f));
        Part(hips, "Collar", GrayboxMeshFactory.Torus(.17f, .045f, 16, 6), cloth, new Vector3(0f, .48f, 0f));

        Transform head = Root("Head", hips).transform;
        head.localPosition = new Vector3(0f, .52f, 0f);
        Part(head, "HeadMesh", GrayboxMeshFactory.Blob(.34f, .62f), GrayboxPalette.Skin, Vector3.zero);
        Part(head, "Hair", GrayboxMeshFactory.Blob(.36f, .4f), hair, new Vector3(0f, .29f, -.06f));
        Part(head, "Fringe", GrayboxMeshFactory.Blob(.26f, .16f), hair, new Vector3(0f, .48f, .16f), new Vector3(-30f, 0f, 0f));
        Mesh eye = GrayboxMeshFactory.Blob(.045f, .1f, 10, 6);
        Part(head, "EyeL", eye, GrayboxPalette.Eye, new Vector3(-.11f, .25f, .3f));
        Part(head, "EyeR", eye, GrayboxPalette.Eye, new Vector3(.11f, .25f, .3f));
        Mesh cheekMesh = GrayboxMeshFactory.Blob(.05f, .05f, 8, 4);
        Part(head, "CheekL", cheekMesh, cheek, new Vector3(-.2f, .19f, .26f));
        Part(head, "CheekR", cheekMesh, cheek, new Vector3(.2f, .19f, .26f));
        AddAccessory(head, look.accessory, hair);

        Mesh arm = GrayboxMeshFactory.Taper(.075f, .065f, .3f, .03f, 10);
        Mesh hand = GrayboxMeshFactory.Blob(.085f, .15f, 10, 6);
        foreach (float side in new[] { -1f, 1f })
        {
            Transform shoulder = Root(side < 0f ? "ArmL" : "ArmR", hips).transform;
            shoulder.localPosition = new Vector3(side * .3f, .38f, 0f);
            Part(shoulder, "Arm", arm, cloth, Vector3.zero, new Vector3(0f, 0f, 180f));
            Part(shoulder, "Hand", hand, GrayboxPalette.Skin, new Vector3(0f, -.39f, 0f));
        }

        Mesh leg = GrayboxMeshFactory.Taper(.085f, .075f, .3f, .03f, 10);
        Mesh foot = GrayboxMeshFactory.Blob(.11f, .12f, 10, 6);
        foreach (float side in new[] { -1f, 1f })
        {
            Transform hip = Root(side < 0f ? "LegL" : "LegR", rig.transform).transform;
            hip.localPosition = new Vector3(side * .12f, .4f, 0f);
            Part(hip, "Leg", leg, GrayboxPalette.DarkGray, Vector3.zero, new Vector3(0f, 0f, 180f));
            Part(hip, "Foot", foot, GrayboxPalette.DarkGray, new Vector3(0f, -.4f, .04f));
        }

        foreach (Renderer r in rig.GetComponentsInChildren<Renderer>()) ((MeshRenderer)r).receiveShadows = true;
        return SavePrefab(rig, "Characters", $"Rig_{look.name}");
    }

    // The antagonist: a skinned mesh made by Tools/Blender/build_distributor.py, driven by Chibi.controller like everyone else
    // (through DistributorController, which swaps in his own arm-raising emotes).
    public static GameObject DistributorRigPrefab()
    {
        ConfigureDistributorImport();
        GameObject rig = AnimatedRig("Rig_Distributor");
        rig.GetComponent<Animator>().runtimeAnimatorController = DistributorController();
        SkinnedMeshRenderer skinned = BindChibiSkin(rig, DISTRIBUTOR_FBX, "Distributor_", DistributorMaterial);
        DistributorFaceVisual face = rig.AddComponent<DistributorFaceVisual>();
        SetField(face, "faceRenderer", skinned);
        return SavePrefab(rig, "Characters", "Rig_Distributor");
    }

    // After re-running build_distributor.py: re-skins the existing Rig_Distributor and the outline hull inside Npc_Distributor
    // in place, so every scene instance (FixScene's hand-tuned one included) keeps its links and overrides.
    [MenuItem("Tools/Characters/Refresh Distributor Mesh")]
    public static void RefreshDistributorMesh()
    {
        ConfigureDistributorImport();
        GameObject scratch = Root("DistributorScratch");
        SkinnedMeshRenderer fresh = BindChibiSkin(scratch, DISTRIBUTOR_FBX, "Distributor_", DistributorMaterial);

        string rigPath = $"{PREFABS}/Characters/Rig_Distributor.prefab";
        GameObject rig = PrefabUtility.LoadPrefabContents(rigPath);
        rig.GetComponent<Animator>().runtimeAnimatorController = DistributorController();
        SkinnedMeshRenderer body = rig.transform.Find("Body").GetComponent<SkinnedMeshRenderer>();
        Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        foreach (Transform t in rig.GetComponentsInChildren<Transform>()) bones[t.name] = t;
        Transform[] boneArray = new Transform[fresh.bones.Length];
        for (int i = 0; i < boneArray.Length; i++) boneArray[i] = bones[fresh.bones[i].name];
        body.sharedMesh = fresh.sharedMesh;
        body.bones = boneArray;
        body.sharedMaterials = fresh.sharedMaterials;
        body.localBounds = fresh.localBounds;
        Mesh mesh = body.sharedMesh;
        Bounds bounds = body.localBounds;
        PrefabUtility.SaveAsPrefabAsset(rig, rigPath);
        PrefabUtility.UnloadPrefabContents(rig);
        Object.DestroyImmediate(scratch);

        string npcPath = $"{PREFABS}/Characters/Npc_Distributor.prefab";
        GameObject npc = PrefabUtility.LoadPrefabContents(npcPath);
        foreach (SkinnedMeshRenderer hull in npc.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (hull.name != "Outline") continue;
            hull.sharedMesh = GrayboxMeshFactory.Outline(mesh, .025f);
            hull.localBounds = bounds;
        }
        PrefabUtility.SaveAsPrefabAsset(npc, npcPath);
        PrefabUtility.UnloadPrefabContents(npc);
    }

    // His head is about twice a villager's, so the shared emotes that raise the arms overhead pass straight through it. This
    // override controller (same states and triggers as Chibi.controller) plays variants that raise them out to the side.
    private static RuntimeAnimatorController DistributorController()
    {
        string path = $"{ANIMATION_FOLDER}/Distributor.overrideController";
        AnimatorOverrideController controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
        if (controller == null)
        {
            controller = new AnimatorOverrideController(Controller());
            AssetDatabase.CreateAsset(controller, path);
        }
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        controller.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, DistributorClip(overrides[i].Key));
        controller.ApplyOverrides(overrides);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    // null = keep the shared clip. +Z raises ArmR out to the side (-Z ArmL), -X swings forward. His sleeves already hang 28 degrees
    // out in the mesh, so Z 80 puts the arm ~108 degrees from hanging: the highest the hand clears his jowls and cheeks.
    private static AnimationClip DistributorClip(AnimationClip shared)
    {
        switch (shared.name)
        {
            case "Wave":
                return ArmVariant(shared, clip => Rot(clip, "Hips/ArmR", new Vector4(0f, 0f, 0f, 6f), new Vector4(.15f, -25f, 0f, 78f), new Vector4(.3f, -25f, 0f, 58f),
                    new Vector4(.45f, -25f, 0f, 80f), new Vector4(.6f, -25f, 0f, 58f), new Vector4(.75f, -25f, 0f, 78f), new Vector4(1f, 0f, 0f, 6f)));
            case "Cheer":
                return ArmVariant(shared, clip =>
                {
                    // fists pump with the two hops
                    Rot(clip, "Hips/ArmL", new Vector4(0f, 0f, 0f, -6f), new Vector4(.12f, -20f, 0f, -80f), new Vector4(.3f, -20f, 0f, -62f), new Vector4(.45f, -20f, 0f, -80f), new Vector4(.72f, -20f, 0f, -76f), new Vector4(.9f, 0f, 0f, -6f));
                    Rot(clip, "Hips/ArmR", new Vector4(0f, 0f, 0f, 6f), new Vector4(.12f, -20f, 0f, 80f), new Vector4(.3f, -20f, 0f, 62f), new Vector4(.45f, -20f, 0f, 80f), new Vector4(.72f, -20f, 0f, 76f), new Vector4(.9f, 0f, 0f, 6f));
                });
            case "Hop": return ArmVariant(shared, clip => ArmsUp(clip, 0f, .28f, .6f, 75f));
            case "GoofyFlip": return ArmVariant(shared, clip => ArmsUp(clip, 0f, .3f, 1f, 75f));
        }
        return null;
    }

    // copy of a shared clip with new arm curves (shortcut: SaveClip keeps an existing asset, delete Distributor_*.anim to regenerate)
    private static AnimationClip ArmVariant(AnimationClip shared, System.Action<AnimationClip> arms)
    {
        return SaveClip("Distributor_" + shared.name, shared.isLooping, clip =>
        {
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(shared))
                if (!binding.path.StartsWith("Hips/Arm")) AnimationUtility.SetEditorCurve(clip, binding, AnimationUtility.GetEditorCurve(shared, binding));
            arms(clip);
        });
    }

    // The main character "Meadow" (Tools/Blender/export_mc_meadow.py), poured into the existing Rig_Player prefab: its root
    // (with the Animator) keeps its file ID, so the Player prefab, every scene's player and the portrait booth stay linked.
    [MenuItem("Tools/Characters/Rebuild Player Rig (Meadow)")]
    public static GameObject PlayerRigPrefab()
    {
        ConfigureMeadowImport();
        return PourIntoRig("Rig_Player", MEADOW_FBX, "MC_Meadow_", MeadowMaterial, AddPlayerFace);
    }

    // The remodelled villagers (Tools/Blender/export_npcs.py). Their size is baked into the mesh, so the rig root stays at scale 1.
    public static GameObject NpcRigPrefab(string name)
    {
        ConfigureMeadowImport();
        DisableAnimationImport(NpcFbx(name));
        return PourIntoRig($"Rig_{name}", NpcFbx(name), $"Npc_{name}_", NpcMaterial, null);
    }

    // re-skins every remodelled villager in place and refreshes its Npc_ prefab (hover outline + prompt height)
    [MenuItem("Tools/Characters/Rebuild NPC Rigs")]
    public static void RebuildNpcRigs()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { NPC_FOLDER }))
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
            NpcRigPrefab(name);
            string npcPath = $"{PREFABS}/Characters/Npc_{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(npcPath) != null) InteractablePrefabBuilder.RefreshVillagerVisual(npcPath);
        }
    }

    private static string NpcFbx(string name) => $"{NPC_FOLDER}/{name}.fbx";

    // Re-skins a rig prefab from an FBX. An existing prefab is refilled in place: its root (with the Animator) keeps its file ID,
    // so the prefabs nesting it, scene instances and the portrait booth stay linked.
    private static GameObject PourIntoRig(string rigName, string fbxPath, string meshPrefix, System.Func<string, Material> material,
                                          System.Action<GameObject, SkinnedMeshRenderer> finish)
    {
        string path = $"{PREFABS}/Characters/{rigName}.prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject rig = exists ? PrefabUtility.LoadPrefabContents(path) : AnimatedRig(rigName);
        for (int i = rig.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(rig.transform.GetChild(i).gameObject);
        rig.transform.localScale = Vector3.one;
        SkinnedMeshRenderer skinned = BindChibiSkin(rig, fbxPath, meshPrefix, material);
        finish?.Invoke(rig, skinned);
        if (!exists) return SavePrefab(rig, "Characters", rigName);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rig, path);
        PrefabUtility.UnloadPrefabContents(rig);
        return prefab;
    }

    private static void AddPlayerFace(GameObject rig, SkinnedMeshRenderer skinned)
    {
        PlayerFaceVisual face = rig.GetComponent<PlayerFaceVisual>();
        if (face == null) face = rig.AddComponent<PlayerFaceVisual>();
        SetField(face, "faceRenderer", skinned);
        SetField(face, "eyeLeftMaterial", MeadowMaterial("MC_Face_EyeL"));
        SetField(face, "eyeRightMaterial", MeadowMaterial("MC_Face_EyeR"));
        SetField(face, "mouthMaterial", MeadowMaterial("MC_Face_Mouth"));
    }

    private static GameObject AnimatedRig(string name)
    {
        GameObject rig = Root(name);
        Animator animator = rig.AddComponent<Animator>();
        animator.runtimeAnimatorController = Controller();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return rig;
    }

    // Re-binds an FBX's skinned mesh to identity-rotation Chibi bones under 'rig' (FBX rest rotations would fight the clips'
    // absolute euler keys). The FBX needs bones Hips (at HIPS_Y), Head, ArmL, ArmR, LegL, LegR; material(slot name) maps its slots.
    private static SkinnedMeshRenderer BindChibiSkin(GameObject rig, string fbxPath, string meshPrefix, System.Func<string, Material> material)
    {
        GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath));
        SkinnedMeshRenderer source = model.GetComponentInChildren<SkinnedMeshRenderer>();
        Dictionary<string, Transform> importedBones = new Dictionary<string, Transform>();
        foreach (Transform t in model.GetComponentsInChildren<Transform>()) importedBones[t.name] = t;

        Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        // parents first, so setting a child's world position sticks
        foreach (string boneName in new[] { "Hips", "LegL", "LegR", "Head", "ArmL", "ArmR" })
        {
            Transform parent = boneName == "Head" || boneName.StartsWith("Arm") ? bones["Hips"] : rig.transform;
            bones[boneName] = Root(boneName, parent).transform;
            bones[boneName].position = importedBones[boneName].position;
        }
        if (Mathf.Abs(bones["Hips"].localPosition.y - HIPS_Y) > .001f) Debug.LogError($"{fbxPath}: Hips at y {bones["Hips"].localPosition.y}, the Chibi clips expect {HIPS_Y}");

        Transform[] boneArray = new Transform[source.bones.Length];
        for (int i = 0; i < boneArray.Length; i++) boneArray[i] = bones[source.bones[i].name];
        GameObject body = Root("Body", rig.transform);
        SkinnedMeshRenderer skinned = body.AddComponent<SkinnedMeshRenderer>();
        skinned.sharedMesh = ReboundMesh(source, boneArray, fbxPath, meshPrefix);
        skinned.bones = boneArray;
        skinned.rootBone = bones["Hips"];
        skinned.localBounds = new Bounds(skinned.sharedMesh.bounds.center - bones["Hips"].position, skinned.sharedMesh.bounds.size * 1.3f);
        Material[] materials = new Material[source.sharedMaterials.Length];
        for (int i = 0; i < materials.Length; i++) materials[i] = material(source.sharedMaterials[i].name);
        skinned.sharedMaterials = materials;
        Object.DestroyImmediate(model);
        return skinned;
    }

    // copy of the imported mesh with bindposes for the new bones; named after the FBX hash so a re-export rebuilds it
    private static Mesh ReboundMesh(SkinnedMeshRenderer source, Transform[] boneArray, string fbxPath, string prefix)
    {
        string key = prefix + AssetDatabase.GetAssetDependencyHash(fbxPath).ToString().Substring(0, 8);
        foreach (string guid in AssetDatabase.FindAssets(prefix, new[] { GENERATED_FOLDER }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!System.IO.Path.GetFileName(path).StartsWith(key)) AssetDatabase.DeleteAsset(path); // stale export + its outline hull
        }
        string meshPath = $"{GENERATED_FOLDER}/{key}.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing != null) return existing;

        Mesh mesh = Object.Instantiate(source.sharedMesh);
        mesh.name = key;
        Matrix4x4[] bindposes = new Matrix4x4[boneArray.Length];
        for (int i = 0; i < boneArray.Length; i++) bindposes[i] = boneArray[i].worldToLocalMatrix * source.transform.localToWorldMatrix;
        mesh.bindposes = bindposes;
        AssetDatabase.CreateAsset(mesh, meshPath);
        return mesh;
    }

    // Blender material slot name → our palette, so he shares the villagers' lighting response
    // colours = SLOTS in build_distributor.py (NPC concept palette)
    private static Material DistributorMaterial(string slot)
    {
        switch (slot)
        {
            case "SkinHead":
                return GrayboxPalette.LitMapped("SkinDistributorHead",
                    AssetDatabase.LoadAssetAtPath<Texture2D>(DISTRIBUTOR_FOLDER + "/Distributor_Head_Base.png"),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(DISTRIBUTOR_FOLDER + "/Distributor_Head_Normal.png"));
            case "Skin": return GrayboxPalette.Lit("SkinDistributor", new Color(.788f, .557f, .392f));
            case "Hair": return GrayboxPalette.Lit("HairDistributor", new Color(.165f, .141f, .125f), .45f);
            case "Frame": return GrayboxPalette.Lit("GlassesFrame", new Color(.29f, .18f, .11f), .4f);
            // only seen as the white glare (LensGlare shape key; at rest the lens hides inside its rim): unlit, a flat flash
            case "Lens": return GrayboxPalette.Unlit("GlassesGlare", Color.white);
            case "Lips": return GrayboxPalette.Lit("LipsDistributor", new Color(.722f, .416f, .361f));
            case "Cloth": return GrayboxPalette.Cloth("Distributor", new Color(.71f, .337f, .184f));
            case "Trim": return GrayboxPalette.Cloth("DistributorTrim", new Color(.561f, .247f, .133f));
            case "Charcoal": return GrayboxPalette.Cloth("DistributorCharcoal", new Color(.231f, .22f, .212f));
            case "Leather": return GrayboxPalette.Lit("LeatherDistributor", new Color(.478f, .29f, .165f), .35f);
            case "Accent": return GrayboxPalette.Lit("AccentDistributor", new Color(.878f, .69f, .29f), .5f);
            // the inside of his mouth (slit + cavity): near-black so an open mouth reads as a hole
            case "Dark": return GrayboxPalette.Lit("DarkDistributor", new Color(.11f, .07f, .06f), 0f);
            case "EyeWhite": return GrayboxPalette.Lit("EyeWhiteDistributor", new Color(.969f, .953f, .918f), .6f);
        }
        Debug.LogError($"Distributor.fbx has an unknown material slot '{slot}'");
        return GrayboxPalette.DarkGray;
    }

    // Blender material slot name → material. Colours are the Blender (linear) values, so they match the farm assets they were taken from.
    private static Material MeadowMaterial(string slot)
    {
        switch (slot)
        {
            case "MC_Skin": return GrayboxPalette.Lit("SkinMeadow", Linear(.9f, .55f, .36f));
            case "MC_Tee": return GrayboxPalette.Lit("TeeMeadow", Linear(.896f, .768f, .558f));
            case "MC_Denim": return GrayboxPalette.Lit("DenimMeadow", Linear(.076f, .262f, .254f));
            case "MC_Boot": return GrayboxPalette.Lit("BootMeadow", Linear(.254f, .091f, .03f));
            case "MC_Hair": return GrayboxPalette.Lit("HairMeadow", Linear(.156f, .068f, .025f));
            case "MC_Straw": return GrayboxPalette.Lit("StrawMeadow", Linear(.82f, .6f, .26f));
            case "MC_Band": return GrayboxPalette.Lit("HatBandMeadow", Linear(.584f, .095f, .05f));
            case "MC_Button": return GrayboxPalette.Lit("ButtonMeadow", Linear(1f, .687f, .25f));
            case "MC_Blush": return GrayboxPalette.Lit("BlushMeadow", Linear(.9f, .35f, .3f));
            // face sheet: 4x4 cells, rows 0-1 eyes, rows 2-3 mouths; the UVs sit on cell 0, the offset picks the cell
            case "MC_Face_EyeL":
            case "MC_Face_EyeR":
                return GrayboxPalette.LitCutout("FaceMeadow" + slot.Substring(8), AssetDatabase.LoadAssetAtPath<Texture2D>(MEADOW_FACE), Vector2.zero);
            case "MC_Face_Mouth":
                return GrayboxPalette.LitCutout("FaceMeadowMouth", AssetDatabase.LoadAssetAtPath<Texture2D>(MEADOW_FACE), new Vector2(0f, -.5f));
        }
        Debug.LogError($"MC_Meadow.fbx has an unknown material slot '{slot}'");
        return GrayboxPalette.DarkGray;
    }

    private static Color Linear(float r, float g, float b) => new Color(r, g, b).gamma;

    // villager slots are named after their sRGB colour ("NPC_E07A5F", Blender may add ".001"), plus the shared face sheet
    private static Material NpcMaterial(string slot)
    {
        string key = slot.Split('.')[0];
        if (key == "NPC_Face") return GrayboxPalette.LitCutout("FaceNpc", AssetDatabase.LoadAssetAtPath<Texture2D>(MEADOW_FACE), Vector2.zero);
        if (key.StartsWith("NPC_") && ColorUtility.TryParseHtmlString("#" + key.Substring(4), out Color color)) return GrayboxPalette.Lit("Npc_" + key.Substring(4), color);
        Debug.LogError($"Villager FBX has an unknown material slot '{slot}'");
        return GrayboxPalette.DarkGray;
    }

    private static void DisableAnimationImport(string fbxPath)
    {
        ModelImporter model = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
        if (!model.importAnimation) return;
        model.importAnimation = false;
        model.SaveAndReimport();
    }

    private static void ConfigureMeadowImport()
    {
        DisableAnimationImport(MEADOW_FBX);
        TextureImporter face = (TextureImporter)AssetImporter.GetAtPath(MEADOW_FACE);
        if (face.alphaIsTransparency && face.wrapMode == TextureWrapMode.Clamp) return;
        face.alphaIsTransparency = true;
        face.wrapMode = TextureWrapMode.Clamp;
        face.maxTextureSize = 1024;
        face.SaveAndReimport();
    }

    private static void ConfigureDistributorImport()
    {
        ModelImporter model = (ModelImporter)AssetImporter.GetAtPath(DISTRIBUTOR_FBX);
        if (model.importAnimation || !model.importBlendShapes)
        {
            model.importAnimation = false;
            model.importBlendShapes = true;
            model.SaveAndReimport();
        }
        foreach (string map in new[] { "Base", "Normal" })
        {
            TextureImporter texture = (TextureImporter)AssetImporter.GetAtPath($"{DISTRIBUTOR_FOLDER}/Distributor_Head_{map}.png");
            TextureImporterType type = map == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (texture.textureType == type && texture.maxTextureSize == 1024) continue;
            texture.textureType = type;
            texture.maxTextureSize = 1024;
            texture.SaveAndReimport();
        }
    }

    private static void AddAccessory(Transform head, Accessory accessory, Material hair)
    {
        switch (accessory)
        {
            case Accessory.Bun:
                Part(head, "Bun", GrayboxMeshFactory.Blob(.14f, .24f), hair, new Vector3(0f, .52f, -.18f));
                break;
            case Accessory.Cap:
                Part(head, "Cap", GrayboxMeshFactory.Cylinder(.3f, .2f, .04f, 18), GrayboxPalette.DarkGray, new Vector3(0f, .5f, -.03f));
                break;
        }
    }

    public static RuntimeAnimatorController Controller()
    {
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (existing != null)
        {
            AddMissingEmotes(existing); // later story beats add emotes; the controller keeps its GUID so every rig stays linked
            return existing;
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
        controller.AddParameter(ChibiAnimation.SPEED, AnimatorControllerParameterType.Float);
        controller.AddParameter(ChibiAnimation.IS_CROUCHING, AnimatorControllerParameterType.Bool);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        AnimatorState idle = machine.AddState("Idle");
        idle.motion = IdleClip();
        AnimatorState walk = machine.AddState("Walk");
        walk.motion = WalkClip();
        walk.speedParameterActive = true;
        walk.speedParameter = ChibiAnimation.SPEED;
        AnimatorState crouch = machine.AddState("Crouch");
        crouch.motion = CrouchClip();
        machine.defaultState = idle;

        Transition(idle, walk, .1f, t => t.AddCondition(AnimatorConditionMode.Greater, .1f, ChibiAnimation.SPEED));
        Transition(walk, idle, .12f, t => t.AddCondition(AnimatorConditionMode.Less, .1f, ChibiAnimation.SPEED));
        Transition(idle, crouch, .25f, t => t.AddCondition(AnimatorConditionMode.If, 0f, ChibiAnimation.IS_CROUCHING));
        Transition(walk, crouch, .25f, t => t.AddCondition(AnimatorConditionMode.If, 0f, ChibiAnimation.IS_CROUCHING));
        Transition(crouch, idle, .25f, t => t.AddCondition(AnimatorConditionMode.IfNot, 0f, ChibiAnimation.IS_CROUCHING));

        AddMissingEmotes(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    // one trigger + state + transitions per Emote that the controller doesn't know yet
    private static void AddMissingEmotes(AnimatorController controller)
    {
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = null, crouch = null;
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state.name == "Idle") idle = child.state;
            else if (child.state.name == "Crouch") crouch = child.state;
        }
        bool hasChanged = false;
        foreach (KeyValuePair<Emote, AnimationClip> pair in EmoteClips())
        {
            string trigger = pair.Key.ToString();
            if (System.Array.Exists(controller.parameters, parameter => parameter.name == trigger)) continue;
            controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);
            AnimatorState state = machine.AddState(trigger);
            state.motion = pair.Value;
            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.duration = .1f;
            enter.canTransitionToSelf = false;
            // digging happens while crouched, so it returns to the crouch pose
            AnimatorStateTransition exit = state.AddTransition(pair.Key == Emote.Dig ? crouch : idle);
            exit.hasExitTime = true;
            exit.exitTime = .95f;
            exit.duration = .15f;
            hasChanged = true;
        }
        if (hasChanged) EditorUtility.SetDirty(controller);
    }

    private static void Transition(AnimatorState from, AnimatorState to, float duration, System.Action<AnimatorStateTransition> setup)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        setup(transition);
    }

    // ---------- clips (rotations in degrees; legs/arms hang down, -X swings them forward, ±Z raises arms sideways) ----------

    private static AnimationClip IdleClip()
    {
        return SaveClip("Idle", true, clip =>
        {
            PosY(clip, "Hips", new Vector2(0f, HIPS_Y), new Vector2(1f, HIPS_Y - .015f), new Vector2(2f, HIPS_Y));
            Rot(clip, "Hips/Head", new Vector4(0f, 0f, 0f, 0f), new Vector4(1f, 3f, 0f, 2f), new Vector4(2f, 0f, 0f, 0f));
            Rot(clip, "Hips/ArmL", new Vector4(0f, 0f, 0f, -6f), new Vector4(1f, 0f, 0f, -9f), new Vector4(2f, 0f, 0f, -6f));
            Rot(clip, "Hips/ArmR", new Vector4(0f, 0f, 0f, 6f), new Vector4(1f, 0f, 0f, 9f), new Vector4(2f, 0f, 0f, 6f));
        });
    }

    private static AnimationClip WalkClip()
    {
        return SaveClip("Walk", true, clip =>
        {
            const float L = .5f;
            PosY(clip, "Hips", new Vector2(0f, HIPS_Y), new Vector2(L * .25f, HIPS_Y + .04f), new Vector2(L * .5f, HIPS_Y), new Vector2(L * .75f, HIPS_Y + .04f), new Vector2(L, HIPS_Y));
            Swing(clip, "LegL", L, 32f, 0f);
            Swing(clip, "LegR", L, -32f, 0f);
            Swing(clip, "Hips/ArmL", L, -28f, -8f);
            Swing(clip, "Hips/ArmR", L, 28f, 8f);
            Rot(clip, "Hips", new Vector4(0f, 0f, 6f, 0f), new Vector4(L * .5f, 0f, -6f, 0f), new Vector4(L, 0f, 6f, 0f));
        });
    }

    private static AnimationClip CrouchClip()
    {
        return SaveClip("Crouch", true, clip =>
        {
            PosY(clip, "Hips", new Vector2(0f, .2f), new Vector2(1f, .19f), new Vector2(2f, .2f));
            Rot(clip, "Hips", new Vector4(0f, 18f, 0f, 0f), new Vector4(2f, 18f, 0f, 0f));
            Rot(clip, "LegL", new Vector4(0f, -75f, 0f, -8f), new Vector4(2f, -75f, 0f, -8f));
            Rot(clip, "LegR", new Vector4(0f, -75f, 0f, 8f), new Vector4(2f, -75f, 0f, 8f));
            Rot(clip, "Hips/ArmL", new Vector4(0f, -45f, 0f, -10f), new Vector4(2f, -45f, 0f, -10f));
            Rot(clip, "Hips/ArmR", new Vector4(0f, -45f, 0f, 10f), new Vector4(2f, -45f, 0f, 10f));
            Rot(clip, "Hips/Head", new Vector4(0f, 10f, 0f, 0f), new Vector4(2f, 10f, 0f, 0f));
        });
    }

    private static Dictionary<Emote, AnimationClip> EmoteClips()
    {
        return new Dictionary<Emote, AnimationClip>
        {
            { Emote.Nod, SaveClip("Nod", false, clip => Rot(clip, "Hips/Head", new Vector4(0f, 0f, 0f, 0f), new Vector4(.15f, 16f, 0f, 0f), new Vector4(.3f, 0f, 0f, 0f), new Vector4(.45f, 13f, 0f, 0f), new Vector4(.65f, 0f, 0f, 0f))) },
            { Emote.Shake, SaveClip("Shake", false, clip => Rot(clip, "Hips/Head", new Vector4(0f, 0f, 0f, 0f), new Vector4(.15f, 0f, 22f, 0f), new Vector4(.32f, 0f, -22f, 0f), new Vector4(.5f, 0f, 16f, 0f), new Vector4(.7f, 0f, 0f, 0f))) },
            { Emote.Hop, SaveClip("Hop", false, clip =>
                {
                    PosY(clip, "Hips", new Vector2(0f, HIPS_Y), new Vector2(.08f, HIPS_Y - .06f), new Vector2(.28f, HIPS_Y + .32f), new Vector2(.5f, HIPS_Y), new Vector2(.6f, HIPS_Y));
                    ArmsUp(clip, 0f, .28f, .6f, 140f);
                }) },
            { Emote.Surprised, SaveClip("Surprised", false, clip =>
                {
                    PosY(clip, "Hips", new Vector2(0f, HIPS_Y), new Vector2(.12f, HIPS_Y + .12f), new Vector2(.3f, HIPS_Y), new Vector2(.8f, HIPS_Y));
                    ArmsUp(clip, 0f, .12f, .8f, 70f);
                    Rot(clip, "Hips/Head", new Vector4(0f, 0f, 0f, 0f), new Vector4(.12f, -14f, 0f, 0f), new Vector4(.6f, -10f, 0f, 0f), new Vector4(.8f, 0f, 0f, 0f));
                }) },
            { Emote.Wave, SaveClip("Wave", false, clip =>
                Rot(clip, "Hips/ArmR", new Vector4(0f, 0f, 0f, 6f), new Vector4(.15f, 0f, 0f, 150f), new Vector4(.3f, 0f, 0f, 125f), new Vector4(.45f, 0f, 0f, 160f), new Vector4(.6f, 0f, 0f, 125f), new Vector4(.75f, 0f, 0f, 150f), new Vector4(1f, 0f, 0f, 6f))) },
            { Emote.Cheer, SaveClip("Cheer", false, clip =>
                {
                    PosY(clip, "Hips", new Vector2(0f, HIPS_Y), new Vector2(.15f, HIPS_Y + .25f), new Vector2(.3f, HIPS_Y), new Vector2(.45f, HIPS_Y + .25f), new Vector2(.6f, HIPS_Y), new Vector2(.9f, HIPS_Y));
                    ArmsUp(clip, 0f, .12f, .9f, 165f);
                }) },
            { Emote.Sad, SaveClip("Sad", false, clip =>
                {
                    PosY(clip, "Hips", new Vector2(0f, HIPS_Y), new Vector2(.3f, HIPS_Y - .04f), new Vector2(1.1f, HIPS_Y - .04f), new Vector2(1.3f, HIPS_Y));
                    Rot(clip, "Hips/Head", new Vector4(0f, 0f, 0f, 0f), new Vector4(.3f, 28f, 0f, 0f), new Vector4(.7f, 26f, 10f, 0f), new Vector4(1.1f, 28f, -8f, 0f), new Vector4(1.3f, 0f, 0f, 0f));
                    Rot(clip, "Hips", new Vector4(0f, 0f, 0f, 0f), new Vector4(.3f, 10f, 0f, 0f), new Vector4(1.1f, 10f, 0f, 0f), new Vector4(1.3f, 0f, 0f, 0f));
                }) },
            { Emote.WakeUp, SaveClip("WakeUp", false, WakeUpClip) },
            { Emote.HeavySigh, SaveClip("HeavySigh", false, HeavySighClip) },
            { Emote.GoofyFlip, SaveClip("GoofyFlip", false, GoofyFlipClip) },
            { Emote.GoofySetup, SaveClip("GoofySetup", false, GoofySetupClip) },
            { Emote.TiltHead, SaveClip("TiltHead", false, clip => Rot(clip, "Hips/Head", new Vector4(0f, 0f, 0f, 0f), new Vector4(.25f, 4f, 0f, 24f), new Vector4(.8f, 4f, 0f, 24f), new Vector4(1.05f, 0f, 0f, 0f))) },
            { Emote.WideSmile, SaveClip("WideSmile", false, WideSmileClip) },
            { Emote.Dodge, SaveClip("Dodge", false, DodgeClip) },
            { Emote.Dig, SaveClip("Dig", false, clip =>
                {
                    PosY(clip, "Hips", new Vector2(0f, .2f), new Vector2(.4f, .2f));
                    Rot(clip, "Hips", new Vector4(0f, 18f, 0f, 0f), new Vector4(.15f, 32f, 0f, 0f), new Vector4(.4f, 18f, 0f, 0f));
                    Rot(clip, "LegL", new Vector4(0f, -75f, 0f, -8f), new Vector4(.4f, -75f, 0f, -8f));
                    Rot(clip, "LegR", new Vector4(0f, -75f, 0f, 8f), new Vector4(.4f, -75f, 0f, 8f));
                    Rot(clip, "Hips/ArmL", new Vector4(0f, -45f, 0f, -10f), new Vector4(.15f, -95f, 0f, -5f), new Vector4(.4f, -45f, 0f, -10f));
                    Rot(clip, "Hips/ArmR", new Vector4(0f, -45f, 0f, 10f), new Vector4(.15f, -95f, 0f, 5f), new Vector4(.4f, -45f, 0f, 10f));
                }) },
        };
    }

    // lying on the back in bed, a grumbling shake of the head, then sitting up with a stretch
    private static void WakeUpClip(AnimationClip clip)
    {
        PosY(clip, "Hips", new Vector2(0f, HIPS_Y - .16f), new Vector2(.9f, HIPS_Y - .15f), new Vector2(1.6f, HIPS_Y + .02f), new Vector2(2.2f, HIPS_Y));
        Rot(clip, "Hips", new Vector4(0f, -85f, 0f, 0f), new Vector4(1f, -85f, 0f, 0f), new Vector4(1.6f, -5f, 0f, 0f), new Vector4(2.2f, 0f, 0f, 0f));
        Rot(clip, "LegL", new Vector4(0f, -85f, 0f, -6f), new Vector4(1f, -85f, 0f, -6f), new Vector4(1.6f, -40f, 0f, -6f), new Vector4(2.2f, 0f, 0f, 0f));
        Rot(clip, "LegR", new Vector4(0f, -85f, 0f, 6f), new Vector4(1f, -85f, 0f, 6f), new Vector4(1.6f, -40f, 0f, 6f), new Vector4(2.2f, 0f, 0f, 0f));
        Rot(clip, "Hips/Head", new Vector4(0f, 0f, 0f, 0f), new Vector4(.25f, 0f, 0f, 14f), new Vector4(.5f, 0f, 0f, -14f), new Vector4(.75f, 0f, 0f, 10f), new Vector4(1f, 0f, 0f, 0f), new Vector4(1.7f, -14f, 0f, 0f), new Vector4(2.2f, 0f, 0f, 0f));
        Rot(clip, "Hips/ArmL", new Vector4(0f, -10f, 0f, -20f), new Vector4(1f, -10f, 0f, -20f), new Vector4(1.7f, 0f, 0f, -150f), new Vector4(2.2f, 0f, 0f, -6f));
        Rot(clip, "Hips/ArmR", new Vector4(0f, -10f, 0f, 20f), new Vector4(1f, -10f, 0f, 20f), new Vector4(1.7f, 0f, 0f, 150f), new Vector4(2.2f, 0f, 0f, 6f));
    }

    // shoulders rise, then drop with the head hanging
    private static void HeavySighClip(AnimationClip clip)
    {
        PosY(clip, "Hips", new Vector2(0f, HIPS_Y), new Vector2(.7f, HIPS_Y + .035f), new Vector2(1.2f, HIPS_Y - .05f), new Vector2(1.9f, HIPS_Y - .04f), new Vector2(2.3f, HIPS_Y));
        Rot(clip, "Hips/Head", new Vector4(0f, 0f, 0f, 0f), new Vector4(.7f, -8f, 0f, 0f), new Vector4(1.2f, 30f, 0f, 0f), new Vector4(1.9f, 28f, 0f, 0f), new Vector4(2.3f, 0f, 0f, 0f));
        Rot(clip, "Hips/ArmL", new Vector4(0f, 0f, 0f, -6f), new Vector4(.7f, 0f, 0f, -34f), new Vector4(1.2f, 0f, 0f, -4f), new Vector4(2.3f, 0f, 0f, -6f));
        Rot(clip, "Hips/ArmR", new Vector4(0f, 0f, 0f, 6f), new Vector4(.7f, 0f, 0f, 34f), new Vector4(1.2f, 0f, 0f, 4f), new Vector4(2.3f, 0f, 0f, 6f));
        Rot(clip, "Hips", new Vector4(0f, 0f, 0f, 0f), new Vector4(1.2f, 9f, 0f, 0f), new Vector4(1.9f, 9f, 0f, 0f), new Vector4(2.3f, 0f, 0f, 0f));
    }

    // a hop with a full backflip (the end key snaps -360 to 0, which looks identical)
    private static void GoofyFlipClip(AnimationClip clip)
    {
        PosY(clip, "Hips", new Vector2(0f, HIPS_Y), new Vector2(.12f, HIPS_Y - .1f), new Vector2(.42f, HIPS_Y + .6f), new Vector2(.76f, HIPS_Y - .02f), new Vector2(.88f, HIPS_Y - .08f), new Vector2(1f, HIPS_Y));
        Rot(clip, "Hips", new Vector4(0f, 0f, 0f, 0f), new Vector4(.12f, 12f, 0f, 0f), new Vector4(.2f, 0f, 0f, 0f), new Vector4(.72f, -360f, 0f, 0f), new Vector4(.73f, 0f, 0f, 0f), new Vector4(1f, 0f, 0f, 0f));
        Rot(clip, "LegL", new Vector4(0f, 0f, 0f, 0f), new Vector4(.3f, -70f, 0f, -8f), new Vector4(.7f, -70f, 0f, -8f), new Vector4(.85f, 0f, 0f, 0f));
        Rot(clip, "LegR", new Vector4(0f, 0f, 0f, 0f), new Vector4(.3f, -70f, 0f, 8f), new Vector4(.7f, -70f, 0f, 8f), new Vector4(.85f, 0f, 0f, 0f));
        ArmsUp(clip, 0f, .3f, 1f, 165f);
    }

    // crouch, scoop the machine up, lift it overhead, set it down in front
    private static void GoofySetupClip(AnimationClip clip)
    {
        PosY(clip, "Hips", new Vector2(0f, HIPS_Y), new Vector2(.35f, .24f), new Vector2(.9f, HIPS_Y + .03f), new Vector2(1.5f, .28f), new Vector2(2f, HIPS_Y));
        Rot(clip, "Hips", new Vector4(0f, 0f, 0f, 0f), new Vector4(.35f, 20f, 0f, 0f), new Vector4(.9f, -6f, 0f, 0f), new Vector4(1.5f, 18f, 0f, 0f), new Vector4(2f, 0f, 0f, 0f));
        Rot(clip, "LegL", new Vector4(0f, 0f, 0f, 0f), new Vector4(.35f, -70f, 0f, -8f), new Vector4(.9f, 0f, 0f, -4f), new Vector4(1.5f, -70f, 0f, -8f), new Vector4(2f, 0f, 0f, 0f));
        Rot(clip, "LegR", new Vector4(0f, 0f, 0f, 0f), new Vector4(.35f, -70f, 0f, 8f), new Vector4(.9f, 0f, 0f, 4f), new Vector4(1.5f, -70f, 0f, 8f), new Vector4(2f, 0f, 0f, 0f));
        Rot(clip, "Hips/ArmL", new Vector4(0f, 0f, 0f, -6f), new Vector4(.35f, -75f, 0f, -12f), new Vector4(.9f, -170f, 0f, -12f), new Vector4(1.5f, -75f, 0f, -12f), new Vector4(2f, 0f, 0f, -6f));
        Rot(clip, "Hips/ArmR", new Vector4(0f, 0f, 0f, 6f), new Vector4(.35f, -75f, 0f, 12f), new Vector4(.9f, -170f, 0f, 12f), new Vector4(1.5f, -75f, 0f, 12f), new Vector4(2f, 0f, 0f, 6f));
        Rot(clip, "Hips/Head", new Vector4(0f, 0f, 0f, 0f), new Vector4(.9f, -14f, 0f, 0f), new Vector4(1.5f, 6f, 0f, 0f), new Vector4(2f, 0f, 0f, 0f));
    }

    // dives to the right (the arc comes from CutsceneActor), sprawls on the side, gets up and pats the dust off
    private static void DodgeClip(AnimationClip clip)
    {
        PosY(clip, "Hips", new Vector2(0f, HIPS_Y), new Vector2(.08f, HIPS_Y - .08f), new Vector2(.35f, HIPS_Y - .16f), new Vector2(1f, HIPS_Y - .16f), new Vector2(1.35f, HIPS_Y), new Vector2(1.9f, HIPS_Y));
        Rot(clip, "Hips", new Vector4(0f, 0f, 0f, 0f), new Vector4(.08f, 10f, 0f, 0f), new Vector4(.35f, 0f, 0f, -80f), new Vector4(1f, 0f, 0f, -78f), new Vector4(1.35f, 6f, 0f, 0f), new Vector4(1.5f, 0f, 0f, 0f));
        Rot(clip, "LegL", new Vector4(0f, 0f, 0f, 0f), new Vector4(.35f, -20f, 0f, -70f), new Vector4(1f, -20f, 0f, -70f), new Vector4(1.35f, 0f, 0f, 0f));
        Rot(clip, "LegR", new Vector4(0f, 0f, 0f, 0f), new Vector4(.35f, 10f, 0f, -80f), new Vector4(1f, 10f, 0f, -80f), new Vector4(1.35f, 0f, 0f, 0f));
        Rot(clip, "Hips/ArmL", new Vector4(0f, 0f, 0f, -6f), new Vector4(.2f, 0f, 0f, -160f), new Vector4(1f, 0f, 0f, -150f), new Vector4(1.35f, -10f, 0f, -6f),
            new Vector4(1.5f, -55f, 0f, -10f), new Vector4(1.6f, -25f, 0f, -10f), new Vector4(1.7f, -55f, 0f, -10f), new Vector4(1.9f, 0f, 0f, -6f));
        Rot(clip, "Hips/ArmR", new Vector4(0f, 0f, 0f, 6f), new Vector4(.2f, 0f, 0f, 160f), new Vector4(1f, 0f, 0f, 150f), new Vector4(1.35f, -10f, 0f, 6f),
            new Vector4(1.55f, -25f, 0f, 10f), new Vector4(1.65f, -55f, 0f, 10f), new Vector4(1.75f, -25f, 0f, 10f), new Vector4(1.9f, 0f, 0f, 6f));
        Rot(clip, "Hips/Head", new Vector4(0f, 0f, 0f, 0f), new Vector4(.35f, -10f, 0f, 10f), new Vector4(1f, -10f, 0f, 10f), new Vector4(1.45f, 20f, 0f, 0f), new Vector4(1.75f, 20f, 0f, 0f), new Vector4(1.9f, 0f, 0f, 0f));
    }

    // a happy little bounce with the head thrown back, arms out
    private static void WideSmileClip(AnimationClip clip)
    {
        PosY(clip, "Hips", new Vector2(0f, HIPS_Y), new Vector2(.15f, HIPS_Y + .1f), new Vector2(.3f, HIPS_Y), new Vector2(.45f, HIPS_Y + .08f), new Vector2(.6f, HIPS_Y), new Vector2(1f, HIPS_Y));
        Rot(clip, "Hips/Head", new Vector4(0f, 0f, 0f, 0f), new Vector4(.2f, -12f, 0f, 6f), new Vector4(.6f, -10f, 0f, -6f), new Vector4(1f, 0f, 0f, 0f));
        ArmsUp(clip, 0f, .2f, 1f, 40f);
    }

    // both arms raised sideways (amount degrees) at 'up', lowered again by 'end'
    private static void ArmsUp(AnimationClip clip, float start, float up, float end, float amount)
    {
        Rot(clip, "Hips/ArmL", new Vector4(start, 0f, 0f, -6f), new Vector4(up, 0f, 0f, -amount), new Vector4(end * .8f, 0f, 0f, -amount), new Vector4(end, 0f, 0f, -6f));
        Rot(clip, "Hips/ArmR", new Vector4(start, 0f, 0f, 6f), new Vector4(up, 0f, 0f, amount), new Vector4(end * .8f, 0f, 0f, amount), new Vector4(end, 0f, 0f, 6f));
    }

    private static void Swing(AnimationClip clip, string path, float length, float amount, float z)
    {
        Rot(clip, path, new Vector4(0f, amount, 0f, z), new Vector4(length * .5f, -amount, 0f, z), new Vector4(length, amount, 0f, z));
    }

    private static AnimationClip SaveClip(string name, bool loop, System.Action<AnimationClip> fill)
    {
        string path = $"{ANIMATION_FOLDER}/{name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null) return clip;
        clip = new AnimationClip { name = name };
        fill(clip);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void PosY(AnimationClip clip, string path, params Vector2[] keys)
    {
        Keyframe[] frames = new Keyframe[keys.Length];
        for (int i = 0; i < keys.Length; i++) frames[i] = new Keyframe(keys[i].x, keys[i].y);
        SetCurve(clip, path, "m_LocalPosition.y", frames);
    }

    // keys = (time, x, y, z) euler degrees
    private static void Rot(AnimationClip clip, string path, params Vector4[] keys)
    {
        string[] axes = { "x", "y", "z" };
        for (int axis = 0; axis < 3; axis++)
        {
            Keyframe[] frames = new Keyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++) frames[i] = new Keyframe(keys[i].x, keys[i][axis + 1]);
            SetCurve(clip, path, "localEulerAnglesRaw." + axes[axis], frames);
        }
    }

    private static void SetCurve(AnimationClip clip, string path, string property, Keyframe[] frames)
    {
        AnimationCurve curve = new AnimationCurve(frames);
        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
    }
}
