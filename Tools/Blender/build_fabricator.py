# Builds the Fabricator machine: small enough for the Distributor to lift with both hands (about 1.3 m tall, 1.1 m wide).
# Separate objects (origin = their pivot) so Unity can animate them: Base, Tube (glass), Lid, Hopper, Lamp, Tray, Panel,
# RingLow, RingHigh, HandleL, HandleR. Re-runnable, headless:
#   "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup -P Tools/Blender/build_fabricator.py [-- <previewDir>]
# 1 unit = 1 m, base on the floor at the origin, faces -Y in Blender (+Z in Unity), transforms applied, writes
# Assets/_Project/Art/Models/Fabricator/Fabricator.fbx and Tools/Blender/Fabricator.blend.
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import day1_common as common  # noqa: E402

OUT_FBX = os.path.join(common.REPO, "Assets", "_Project", "Art", "Models", "Fabricator", "Fabricator.fbx")
BLEND_PATH = os.path.join(common.REPO, "Tools", "Blender", "Fabricator.blend")
COLORS = {
    "Metal": (.62, .66, .72), "Glass": (.80, .94, 1.0), "Accent": (.98, .80, .42), "Dark": (.26, .27, .32),
}
TUBE_BOTTOM, TUBE_TOP = .24, .98


def single(mats, name, pivot, fill):
    b = common.Builder(mats)
    fill(b)
    return b.build(name, pivot=pivot)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mats = common.make_materials(COLORS)
    root = bpy.data.objects.new("Fabricator", None)
    bpy.context.scene.collection.objects.link(root)

    parts = [
        single(mats, "Base", (0, 0, 0), lambda b: (b.cylinder(.58, .24, (0, 0, .12), "Metal", segments=28, bevel=.05),
                                                   b.cylinder(.5, .06, (0, 0, .27), "Dark", segments=28))),
        single(mats, "Tube", (0, 0, TUBE_BOTTOM), lambda b: b.cylinder(.42, TUBE_TOP - TUBE_BOTTOM, (0, 0, (TUBE_TOP + TUBE_BOTTOM) / 2), "Glass", segments=28)),
        single(mats, "RingLow", (0, 0, TUBE_BOTTOM), lambda b: b.torus(.43, .035, (0, 0, TUBE_BOTTOM), "Accent")),
        single(mats, "RingHigh", (0, 0, TUBE_TOP), lambda b: b.torus(.43, .035, (0, 0, TUBE_TOP), "Accent")),
        single(mats, "Lid", (0, 0, TUBE_TOP), lambda b: b.cylinder(.5, .14, (0, 0, TUBE_TOP + .07), "Metal", segments=28, bevel=.04)),
        single(mats, "Hopper", (0, 0, TUBE_TOP + .14), lambda b: b.cylinder(.3, .24, (0, 0, TUBE_TOP + .26), "Metal", segments=24, radius2=.15)),
        single(mats, "Lamp", (.32, 0, TUBE_TOP + .14), lambda b: b.sphere(.07, (.32, 0, TUBE_TOP + .19), "Accent")),
        single(mats, "Tray", (0, -.52, .3), lambda b: b.box((.5, .34, .07), (0, -.52, .3), "Accent", bevel=.025)),
        single(mats, "Panel", (.4, -.32, .42), lambda b: (b.box((.3, .12, .3), (.4, -.32, .42), "Dark", bevel=.03, rot=(0, 0, 35)),
                                                          b.sphere(.04, (.43, -.4, .5), "Accent"))),
        single(mats, "HandleL", (-.5, 0, .55), lambda b: b.torus(.1, .028, (-.5, 0, .55), "Dark", axis="Y", major_seg=16)),
        single(mats, "HandleR", (.5, 0, .55), lambda b: b.torus(.1, .028, (.5, 0, .55), "Dark", axis="Y", major_seg=16)),
    ]
    for part in parts:
        part.parent = root
    print(f"FABRICATOR tris={sum(len(p.vertices) - 2 for o in parts for p in o.data.polygons)}")
    common.export_fbx([root], OUT_FBX)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    if common.PREVIEW_DIR:
        common.render_previews(common.PREVIEW_DIR, "fabricator", (0, 0, .7), 8, 2.2)
    print("FABRICATOR done")


main()
