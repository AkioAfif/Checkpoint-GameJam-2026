---
paths:
  - "**/*.mat"
  - "**/*.shader"
  - "**/*.shadergraph"
  - "**/*.hlsl"
  - "Assets/Settings/**"
  - "**/*.renderTexture"
---

# Rendering (URP 17)

- Our new materials use `Universal Render Pipeline/Lit` (or `/Unlit`, `/Particles/Unlit`) or a Synty Shader Graph. **Never `Standard`, `Legacy Shaders/*` or Built-in particle shaders**: they render pink/magenta in URP.
- Synty look: copy an existing Synty material (they use `PolygonShader` / `Generic_Standard` Shader Graphs with the shared atlas texture) into `Assets/_Project/Art/Materials/`, then tweak the copy.
- Material properties from code: URP Lit uses `_BaseColor` / `_BaseMap` (not `_Color` / `_MainTex`), and `_EmissionColor` + `material.EnableKeyword("_EMISSION")`. Synty shader graphs use their own property names, so check them with `get_shader_properties` before writing code.
- Changing color per instance at runtime: use `renderer.material` (creates an instance) only on a few objects. Otherwise use a `MaterialPropertyBlock`. Never modify `sharedMaterial` at runtime (it edits the asset).
- Post-processing = Volume overrides on the scene's `Global Volume` (Bloom, Tonemapping, Color Adjustments, Vignette). The camera needs *Post Processing* enabled. Don't add the old Post Processing Stack v2.
- Quality tiers: `PC` → `PC_RPAsset` (editor default), `Mobile` → `Mobile_RPAsset`. Change a renderer feature on **both** renderers if it should apply on every tier.
- Custom shaders: Shader Graph first. Hand-written HLSL only if asked; it must be a URP shader (`"RenderPipeline"="UniversalPipeline"` tag, `Core.hlsl` include).
- Particles: `Universal Render Pipeline/Particles/Unlit` or `/Lit`, or Synty's `Generic_ParticlesUnlit` / `Generic_ParticlesLit`.
