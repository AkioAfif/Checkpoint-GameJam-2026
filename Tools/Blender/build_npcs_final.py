# The six chosen villagers (one archetype each from build_npc_concepts.py) with raised 3D clothing details, rendered for a
# colour/placement check: T-pose front/back/below plus the resting pose. Uses the villager kit in build_npc_concepts.py.
# Run inside Blender: exec(open(r"<repo>/Tools/Blender/build_npcs_final.py").read(), {"__name__": "mc"}).
# Writes Exports/NPC_Final/<npc>_{tpose_front,tpose_back,tpose_below,pose_front,pose_back}.png and Tools/Blender/NPC_Final.blend.
import math
import os

import bpy
from mathutils import Vector

REPO = r"D:\Akio\GameDev Project\Checkpoint GameJam 2026"
kit = {"__name__": "npc_kit"}
exec(open(os.path.join(REPO, "Tools", "Blender", "build_npc_concepts.py")).read(), kit)
OUT = os.path.join(REPO, "Exports", "NPC_Final")

# npc -> (chosen variant letter, extra raised details)
CHOSEN = {
    "Ratna": ("B", [("pocket", "#C99A35", .12, ("waist", -.01), .08, .07, .02), ("pocket", "#C99A35", -.12, ("waist", -.01), .08, .07, .02),
                    ("buttons", "#F7F3EA", 0, ("waist", .05), ("chest_z", .04), 3, 0), ("cuff", "#C99A35", "wrist")]),
    "Darto": ("A", [("pocket", "#D2B06E", .09, ("chest_z", -.01), .08, .08, 0), ("buttons", "#7A4A2A", 0, ("waist", .05), ("neck_base", -.06), 4, 0),
                    ("cuff", "#D2B06E", "sleeve")]),
    "Dimas": ("A", [("legcuff", "#2E3A57", .12)]),
    "Ijah": ("A", [("hemfrill", "#F4E4C1"), ("cuff", "#F4E4C1", "wrist")]),
    "Sari": ("C", [("hood", "#D29A3A"), ("drawstrings", "#F7F3EA"), ("pocket", "#B97F22", 0, ("waist", .04), .2, .09, 0),
                   ("cuff", "#D29A3A", "wrist"), ("legpocket", "#566A48", .2)]),
    "Rigel": ("A", [("legcuff", "#A3B163", .17), ("pocket", "#7C8B42", .1, ("waist", -.07), .07, .06, 0)]),
}
EXTRA_OVERLAYS = {"Rigel": [("belt", "#6B4A2A")]}


def chosen_specs():
    for npc, (letter, details) in CHOSEN.items():
        display, persona, variants = kit["NPCS"][npc]
        title, blurb, P, L = variants["ABC".index(letter)]
        L = dict(L, details=details, overlays=L.get("overlays", []) + EXTRA_OVERLAYS.get(npc, []))
        yield npc, display, title, P, L


def main():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    for block in (bpy.data.meshes, bpy.data.materials):
        for item in list(block):
            if item.users == 0:
                block.remove(item)
    kit["_mats"].clear()
    os.makedirs(OUT, exist_ok=True)
    cam = kit["setup_scene"]()
    ground = bpy.data.objects["Ground"]
    built = []
    for i, (npc, display, title, P, L) in enumerate(chosen_specs()):
        posed, info = kit["build_villager"](f"{npc}", P, L)
        posed.location = (i * 2.0, 0, 0)
        tpose, _ = kit["build_villager"](f"{npc}_TPose", P, L, tpose=True)
        tpose.location = (i * 2.0, 6, 0)
        built.append((npc, posed, tpose, info))

    everything = [o for _, a, b, _ in built for o in (a, b)]
    for npc, posed, tpose, info in built:
        for o in everything:
            o.hide_render = o is not tpose
        h = info["top"]
        t = Vector(tpose.location) + Vector((0, 0, h * .5))
        p = Vector(posed.location) + Vector((0, 0, h * .5))
        size = max(1.3, h * 1.15)
        kit["shot"](cam, os.path.join(OUT, f"{npc}_tpose_front.png"), t, 0, 0, size, (700, 700))
        kit["shot"](cam, os.path.join(OUT, f"{npc}_tpose_back.png"), t, math.pi, 0, size, (700, 700))
        ground.hide_render = True
        kit["shot"](cam, os.path.join(OUT, f"{npc}_tpose_below.png"), t, .25, -1.1, size, (700, 700))
        ground.hide_render = False
        for o in everything:
            o.hide_render = o is not posed
        kit["shot"](cam, os.path.join(OUT, f"{npc}_pose_front.png"), p, .45, .1, size * .9, (700, 700))
        kit["shot"](cam, os.path.join(OUT, f"{npc}_pose_back.png"), p, math.pi - .5, .1, size * .9, (700, 700))
    # group lineup, resting pose, with Meadow for scale
    meadow = kit["meadow_reference"]()
    meadow.location = (-2.0, 0, 0)
    for o in everything:
        o.hide_render = o not in [p for _, p, _, _ in built]
    kit["shot"](cam, os.path.join(OUT, "Lineup.png"), (4.0, 0, .8), .3, .12, 13.5, (1800, 700))
    for o in everything:
        o.hide_render = False
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(REPO, "Tools", "Blender", "NPC_Final.blend"))
    return {npc: {"tris": info["tris"], "height": round(info["top"], 2)} for npc, _, _, info in built}


if __name__ == "mc":
    result = main()
