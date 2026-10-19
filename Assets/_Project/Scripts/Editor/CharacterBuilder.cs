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

    public enum Accessory { None, StrawHat, Bun, Cap }

    public struct Look
    {
        public string name;
        public Color cloth;
        public Color hair;
        public Accessory accessory;
        public float scale;
    }

    public static readonly Look PlayerLook = new Look { name = "Player", cloth = new Color(.52f, .67f, .88f), hair = new Color(.36f, .27f, .22f), accessory = Accessory.StrawHat, scale = 1f };
    public static readonly Look RatnaLook = new Look { name = "Ratna", cloth = new Color(.93f, .63f, .69f), hair = new Color(.26f, .21f, .21f), accessory = Accessory.Bun, scale = 1f };
    public static readonly Look DartoLook = new Look { name = "Darto", cloth = new Color(.56f, .73f, .56f), hair = new Color(.62f, .62f, .64f), accessory = Accessory.Cap, scale = 1.05f };
    public static readonly Look DimasLook = new Look { name = "Dimas", cloth = new Color(.98f, .73f, .43f), hair = new Color(.2f, .18f, .18f), accessory = Accessory.None, scale = .85f };
    public static readonly Look IjahLook = new Look { name = "Ijah", cloth = new Color(.73f, .63f, .86f), hair = new Color(.93f, .93f, .93f), accessory = Accessory.Bun, scale = .95f };
    public static readonly Look SariLook = new Look { name = "Sari", cloth = new Color(.40f, .76f, .74f), hair = new Color(.15f, .12f, .13f), accessory = Accessory.None, scale = .92f };

    // rig prefab (Animator + body parts), nested inside the Player/NPC prefabs and spawned in the portrait booth
    public static GameObject RigPrefab(Look look)
    {
        GameObject rig = Root($"Rig_{look.name}");
        Animator animator = rig.AddComponent<Animator>();
        animator.runtimeAnimatorController = Controller();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
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

    // The antagonist: a skinned mesh made by Tools/Blender/build_distributor.py, re-bound to identity-rotation bones
    // (FBX rest rotations would fight the clips' absolute euler keys) so Chibi.controller drives him like everyone else.
    public static GameObject DistributorRigPrefab()
    {
        ConfigureDistributorImport();
        GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(DISTRIBUTOR_FBX));
        SkinnedMeshRenderer source = model.GetComponentInChildren<SkinnedMeshRenderer>();
        Dictionary<string, Transform> importedBones = new Dictionary<string, Transform>();
        foreach (Transform t in model.GetComponentsInChildren<Transform>()) importedBones[t.name] = t;

        GameObject rig = Root("Rig_Distributor");
        Animator animator = rig.AddComponent<Animator>();
        animator.runtimeAnimatorController = Controller();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        // parents first, so setting a child's world position sticks
        foreach (string boneName in new[] { "Hips", "LegL", "LegR", "Head", "ArmL", "ArmR" })
        {
            Transform parent = boneName == "Head" || boneName.StartsWith("Arm") ? bones["Hips"] : rig.transform;
            bones[boneName] = Root(boneName, parent).transform;
            bones[boneName].position = importedBones[boneName].position;
        }
        if (Mathf.Abs(bones["Hips"].localPosition.y - HIPS_Y) > .001f) Debug.LogError($"Distributor Hips at y {bones["Hips"].localPosition.y}, the Chibi clips expect {HIPS_Y}");

        Transform[] boneArray = new Transform[source.bones.Length];
        for (int i = 0; i < boneArray.Length; i++) boneArray[i] = bones[source.bones[i].name];
        GameObject body = Root("Body", rig.transform);
        SkinnedMeshRenderer skinned = body.AddComponent<SkinnedMeshRenderer>();
        skinned.sharedMesh = DistributorMesh(source, boneArray);
        skinned.bones = boneArray;
        skinned.rootBone = bones["Hips"];
        skinned.localBounds = new Bounds(skinned.sharedMesh.bounds.center - bones["Hips"].position, skinned.sharedMesh.bounds.size * 1.3f);
        Material[] materials = new Material[source.sharedMaterials.Length];
        for (int i = 0; i < materials.Length; i++) materials[i] = DistributorMaterial(source.sharedMaterials[i].name);
        skinned.sharedMaterials = materials;
        Object.DestroyImmediate(model);

        DistributorFaceVisual face = rig.AddComponent<DistributorFaceVisual>();
        SetField(face, "faceRenderer", skinned);
        return SavePrefab(rig, "Characters", "Rig_Distributor");
    }

    // copy of the imported mesh with bindposes for the new bones; named after the FBX hash so a re-export rebuilds it
    private static Mesh DistributorMesh(SkinnedMeshRenderer source, Transform[] boneArray)
    {
        string key = "Distributor_" + AssetDatabase.GetAssetDependencyHash(DISTRIBUTOR_FBX).ToString().Substring(0, 8);
        foreach (string guid in AssetDatabase.FindAssets("Distributor_", new[] { GENERATED_FOLDER }))
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
    private static Material DistributorMaterial(string slot)
    {
        Color tan = new Color(.80f, .62f, .46f);
        switch (slot)
        {
            case "SkinHead":
                return GrayboxPalette.LitMapped("SkinDistributorHead",
                    AssetDatabase.LoadAssetAtPath<Texture2D>(DISTRIBUTOR_FOLDER + "/Distributor_Head_Base.png"),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(DISTRIBUTOR_FOLDER + "/Distributor_Head_Normal.png"));
            case "Skin": return GrayboxPalette.Lit("SkinDistributor", tan);
            case "Hair": return GrayboxPalette.Lit("HairDistributor", new Color(.10f, .09f, .10f), .45f);
            case "Frame": return GrayboxPalette.Lit("GlassesFrame", new Color(.12f, .11f, .12f), .4f);
            case "Lens": return GrayboxPalette.Lit("GlassesLens", new Color(.96f, .96f, .94f), .7f);
            case "Lips": return GrayboxPalette.Lit("LipsDistributor", new Color(.80f, .47f, .45f));
            case "Cloth": return GrayboxPalette.Cloth("Distributor", new Color(.80f, .66f, .36f));
            case "Accent": return GrayboxPalette.Accent;
            case "Dark": return GrayboxPalette.DarkGray;
        }
        Debug.LogError($"Distributor.fbx has an unknown material slot '{slot}'");
        return GrayboxPalette.DarkGray;
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
            case Accessory.StrawHat:
                Material straw = GrayboxPalette.Lit("Straw", new Color(.95f, .84f, .55f));
                Part(head, "HatBrim", GrayboxMeshFactory.Cylinder(.5f, .05f, .02f, 20), straw, new Vector3(0f, .5f, 0f), new Vector3(-8f, 0f, 0f));
                Part(head, "HatTop", GrayboxMeshFactory.Taper(.27f, .22f, .2f, .06f, 16), straw, new Vector3(0f, .52f, -.02f), new Vector3(-8f, 0f, 0f));
                Part(head, "HatBand", GrayboxMeshFactory.Torus(.255f, .025f, 16, 6), GrayboxPalette.Lit("HatBand", new Color(.86f, .45f, .40f)), new Vector3(0f, .53f, -.02f), new Vector3(-8f, 0f, 0f));
                break;
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
