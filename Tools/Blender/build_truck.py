# Builds the Distributor's cartoon pickup truck: one "Chassis" mesh (cab, hood, bed, bumpers, lights) with the four wheels
# as separate child objects whose origins sit on their axles, so Unity can spin the wheels and squash the body.
# Re-runnable, headless:
#   "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup -P Tools/Blender/build_truck.py [-- <previewDir>]
# 1 unit = 1 m (about 4.6 m long, 2.1 m wide), faces -Y in Blender (+Z in Unity), transforms applied, writes
# Assets/_Project/Art/Models/Truck/DistributorTruck.fbx and Tools/Blender/DistributorTruck.blend.
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import day1_common as common  # noqa: E402

OUT_FBX = os.path.join(common.REPO, "Assets", "_Project", "Art", "Models", "Truck", "DistributorTruck.fbx")
BLEND_PATH = os.path.join(common.REPO, "Tools", "Blender", "DistributorTruck.blend")
COLORS = {
    "Paint": (.93, .46, .24), "PaintTrim": (.99, .83, .42), "Dark": (.22, .22, .27), "Glass": (.72, .88, .96),
    "Chrome": (.82, .84, .88), "Light": (1.0, .93, .55), "Rubber": (.14, .14, .17),
}
WHEEL_RADIUS, WHEEL_WIDTH = .46, .42
WHEELS = {"WheelFL": (-1.08, -1.45), "WheelFR": (1.08, -1.45), "WheelRL": (-1.08, 1.55), "WheelRR": (1.08, 1.55)}


def build_chassis(mats):
    b = common.Builder(mats)
    # underbody + frame
    b.box((1.8, 4.3, .34), (0, 0, .62), "Dark", bevel=.06)
    # hood, cab and roof
    b.box((1.95, 1.55, .62), (0, -1.5, 1.1), "Paint", bevel=.14)
    b.box((1.95, 1.6, 1.05), (0, -.1, 1.48), "Paint", bevel=.16)
    b.box((2.05, 1.8, .14), (0, -.1, 2.05), "PaintTrim", bevel=.06)
    # windows (slightly proud of the cab walls)
    b.box((1.55, .08, .56), (0, -.9, 1.68), "Glass", bevel=.03)
    b.box((1.4, .08, .46), (0, .7, 1.68), "Glass", bevel=.03)
    for x in (-.99, .99):
        b.box((.08, 1.05, .5), (x, -.1, 1.68), "Glass", bevel=.03)
    # open bed with low walls
    b.box((1.9, 1.5, .14), (0, 1.55, .98), "Dark", bevel=.04)
    for x in (-.92, .92):
        b.box((.14, 1.5, .52), (x, 1.55, 1.3), "Paint", bevel=.05)
    b.box((1.9, .14, .52), (0, 2.28, 1.3), "Paint", bevel=.05)
    b.box((1.9, .14, .3), (0, .86, 1.2), "Paint", bevel=.05)
    # bumpers, grille and lights
    b.box((2.15, .3, .36), (0, -2.34, .62), "Chrome", bevel=.12)
    b.box((2.05, .26, .32), (0, 2.42, .62), "Chrome", bevel=.1)
    b.box((1.15, .08, .34), (0, -2.3, 1.02), "Dark", bevel=.03)
    for x in (-.68, .68):
        b.sphere(.21, (x, -2.28, 1.18), "Light", (1, .6, 1))
        b.sphere(.1, (x, 2.5, .95), "Light", (1, .6, 1))
    # fender flares over the wheels
    for _, (x, y) in WHEELS.items():
        b.box((.4, 1.1, .1), (x * .95, y, .98), "PaintTrim", bevel=.04)
    # mirrors, exhaust, antenna with a ball on top
    for x in (-1.12, 1.12):
        b.box((.14, .1, .26), (x, -.78, 1.7), "Dark", bevel=.03)
    b.cylinder(.08, .6, (-.7, 2.2, .45), "Chrome", axis="Z", segments=10)
    b.cylinder(.025, .8, (.85, .45, 2.5), "Dark", axis="Z", segments=6)
    b.sphere(.09, (.85, .45, 2.95), "Light")
    return b.build("Chassis")


def build_wheel(mats, name, x, y):
    b = common.Builder(mats)
    z = WHEEL_RADIUS
    b.cylinder(WHEEL_RADIUS, WHEEL_WIDTH, (x, y, z), "Rubber", axis="X", segments=20, bevel=.05)
    b.cylinder(.27, WHEEL_WIDTH + .04, (x, y, z), "Chrome", axis="X", segments=14)
    b.cylinder(.1, WHEEL_WIDTH + .09, (x, y, z), "Paint", axis="X", segments=10)
    return b.build(name, pivot=(x, y, z))


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mats = common.make_materials(COLORS)
    chassis = build_chassis(mats)
    for name, (x, y) in WHEELS.items():
        wheel = build_wheel(mats, name, x, y)
        wheel.parent = chassis
    print(f"TRUCK tris={sum(len(p.vertices) - 2 for o in bpy.data.objects for p in o.data.polygons)}")
    common.export_fbx([chassis], OUT_FBX)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    if common.PREVIEW_DIR:
        common.render_previews(common.PREVIEW_DIR, "truck", (0, 0, 1.2), 12, 7.5)
    print("TRUCK done")


main()
