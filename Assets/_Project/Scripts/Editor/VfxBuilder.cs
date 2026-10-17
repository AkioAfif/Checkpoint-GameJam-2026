using UnityEngine;
using static GrayboxBuild;

// Soft particle effects. One-shot effects destroy themselves when finished (spawned at runtime with Instantiate).
public static class VfxBuilder
{
    // footstep dust, emitted manually by PlayerVisual
    public static ParticleSystem Dust(Transform parent)
    {
        ParticleSystem ps = Create(Root("Dust", parent), false, Alpha(new Color(.93f, .89f, .80f), .75f), Alpha(new Color(.93f, .89f, .80f), .75f));
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.45f, .7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.3f, .8f);
        main.startSize = new ParticleSystem.MinMaxCurve(.22f, .4f);
        main.gravityModifier = -.08f;
        main.loop = false;
        main.playOnAwake = false;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = .12f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        GrowAndFade(ps, .5f, 1.3f);
        return ps;
    }

    public static GameObject CollectBurst() => SaveOneShot("CollectBurst", Color.white, new Color(1f, .9f, .5f), 16, new Vector2(2f, 3.6f), new Vector2(.1f, .22f), .5f, true, 0f);
    public static GameObject SeedPop() => SaveOneShot("SeedPop", new Color(1f, .85f, .4f), new Color(.75f, 1f, .55f), 22, new Vector2(2.4f, 4f), new Vector2(.12f, .25f), .6f, true, 25f);
    public static GameObject PlantSuccess() => SaveOneShot("PlantSuccess", new Color(.55f, .95f, .45f), new Color(1f, .95f, .5f), 30, new Vector2(2.5f, 4.5f), new Vector2(.15f, .3f), .7f, true, 35f);
    public static GameObject PlantFail() => SaveOneShot("PlantFail", Alpha(new Color(.55f, .5f, .47f), .8f), Alpha(new Color(.72f, .68f, .62f), .8f), 12, new Vector2(.6f, 1.4f), new Vector2(.5f, .85f), -.05f, false, 30f);

    private static GameObject SaveOneShot(string name, Color colorA, Color colorB, int count, Vector2 speed, Vector2 size, float gravity, bool additive, float coneAngle)
    {
        GameObject root = Root(name);
        ParticleSystem ps = Create(root, additive, colorA, colorB);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.5f, .9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.gravityModifier = gravity;
        main.stopAction = ParticleSystemStopAction.Destroy;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        ParticleSystem.ShapeModule shape = ps.shape;
        if (coneAngle > 0f)
        {
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = coneAngle;
            shape.radius = .2f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .25f;
        }
        GrowAndFade(ps, additive ? 1f : .6f, additive ? .2f : 1.4f);
        return SavePrefab(root, "VFX", name);
    }

    private static ParticleSystem Create(GameObject go, bool additive, Color colorA, Color colorB)
    {
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.maxParticles = 64;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = GrayboxPalette.Particle(additive ? "ParticleAdditive" : "ParticleSoft", GrayboxTextures.SoftCircleTexture, additive);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return ps;
    }

    private static void GrowAndFade(ParticleSystem ps, float startScale, float endScale)
    {
        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, startScale, 1f, endScale));
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, .5f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;
    }

    private static Color Alpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
