# MC bedroom concepts in the CozyFarm style (Ilkham's village: flat colours, low-poly timber cottage, same material names).
# Three rooms on the gameplay layout HouseInteriorBuilder uses (Unity coords): bed (-1.6, -.3) head north, bedside table + letter
# (-1.6, 1.6), door in the north wall at x 1.2, window in the east wall at z .4, south side open for the fixed camera.
# Blender: x = Unity x, y = Unity z, z up. Headless, from the repo root:
#   "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup -P Tools/Blender/build_bedroom_concepts.py
# Writes Exports/Bedroom_Concepts/Bedroom_<A|B|C>_<game|overview>.png and Tools/Blender/Bedroom_Concepts.blend, then exports the
# chosen concept C (2026-10-27) to Assets/_Project/Art/Models/Bedroom/Bedroom.fbx: meshes Shell, Door, Bed, BedsideTable, Decor,
# Rug, each centred on its prefab's pivot (Unity coords, see PIVOTS), materials named "Bedroom_<sRGB hex>" (BedroomModelBuilder).
import math
import os
import sys

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import day1_common as common  # noqa: E402

OUT = os.path.join(common.REPO, "Exports", "Bedroom_Concepts")
BLEND_PATH = os.path.join(common.REPO, "Tools", "Blender", "Bedroom_Concepts.blend")
# linear colours copied from the CozyFarm .glb materials, plus a few fabrics
COLORS = {
    "M_Wall": (.9, .77, .56), "M_Wood_Dark": (.16, .07, .03), "M_Wood": (.39, .19, .07), "M_Plank": (.49, .23, .08),
    "M_Stone": (.42, .38, .33), "M_Roof": (.58, .1, .05), "M_Roof_Dark": (.36, .05, .03), "M_Shutter": (.08, .26, .25),
    "M_Glass": (.38, .72, .83), "M_Metal": (.05, .04, .04), "M_Lamp_Glow": (1, .69, .25), "M_Terracotta": (.58, .17, .07),
    "M_Leaf_Small": (.11, .42, .06), "M_Flower_Pink": (.89, .33, .45), "M_Flower_White": (.96, .92, .84),
    "M_Basket": (.55, .32, .11), "M_Cabbage": (.26, .53, .14), "M_Carrot": (.81, .25, .04), "M_Fruit_Red": (.71, .07, .05),
    "F_Mustard": (.72, .45, .08), "F_Teal": (.14, .42, .38), "F_Cream": (.86, .8, .66), "F_Rug": (.45, .14, .07),
}
BEVEL = .012
BED, TABLE, DOOR_X, WINDOW_Y = (-1.6, -.3), (-1.6, 1.6), 1.2, .4
OUT_FBX = os.path.join(common.REPO, "Assets", "_Project", "Art", "Models", "Bedroom", "Bedroom.fbx")
# where each mesh's origin goes (Blender x, y, z); Bed sinks .08 so its mattress top meets the player's spawn height (.52)
PIVOTS = {"Bed": (BED[0], BED[1], .08), "BedsideTable": (TABLE[0], TABLE[1], 0), "Door": (DOOR_X, 2.1, 0)}


def materials():
    mats = {}
    for name, rgb in COLORS.items():
        m = bpy.data.materials.new(name)
        m.diffuse_color = (*rgb, 1.0)
        mats[name] = m
    return mats


class Room:
    """One concept: every part goes into named Builders, parented under one empty."""

    def __init__(self, mats, name, offset):
        self.mats, self.name, self.offset = mats, name, Vector(offset)
        self.root = bpy.data.objects.new(name, None)
        bpy.context.scene.collection.objects.link(self.root)
        self.root.location = self.offset
        self.parts = {}
        self.objects = {}

    def b(self, part):
        if part not in self.parts:
            self.parts[part] = common.Builder(self.mats)
        return self.parts[part]

    def finish(self):
        tris = 0
        for part, builder in self.parts.items():
            obj = builder.build(f"{self.name}_{part}")
            obj.parent = self.root
            self.objects[part] = obj
            tris += sum(len(p.vertices) - 2 for p in obj.data.polygons)
        print(f"BEDROOM {self.name} tris={tris}")


def box(b, size, loc, mat, rot=(0, 0, 0), bevel=BEVEL):
    # only the soft bevels (bedding) survive: CozyFarm edges are sharp and small bevels triple the triangle count
    b.box(size, loc, mat, bevel=bevel if bevel >= .02 else 0, rot=rot)


# ---------- shared pieces ----------

def plank_floor(b, mats=("M_Plank", "M_Wood"), width=.4):
    box(b, (5.6, 4.6, .3), (0, 0, -.16), "M_Stone", bevel=.02)
    n = int(5.2 / width)
    for i in range(n):
        x = -2.6 + width * (i + .5)
        box(b, (width - .015, 4.4, .04), (x, -.1, .0), mats[i % len(mats)], bevel=.006)


def letter(b, x, y, z):
    box(b, (.34, .24, .015), (x, y, z + .008), "M_Flower_White", rot=(0, 0, 18), bevel=.003)
    b.cylinder(.035, .02, (x + .02, y, z + .02), "M_Fruit_Red", segments=8)


def door(b, x, wall_y, frame="M_Wood_Dark", leaf="M_Wood"):
    y = wall_y - .11
    box(b, (1.4, .1, 2.2), (x, y, 1.1), frame)
    for i in range(4):
        box(b, (.27, .06, 1.98), (x - .405 + i * .27, y - .06, 1.01), leaf, bevel=.006)
    for z in (.45, 1.6):
        box(b, (.9, .04, .07), (x, y - .1, z), "M_Metal", bevel=.005)
    b.sphere(.05, (x + .4, y - .14, 1.0), "M_Metal")


def east_window(b, shutters=True, sill_items=True, y=WINDOW_Y, z=1.35):
    x = 2.48
    box(b, (.08, 1.3, 1.1), (x, y, z), "M_Glass", bevel=.005)
    for dy in (-.68, .68):
        box(b, (.12, .1, 1.3), (x - .02, y + dy, z), "M_Wood_Dark")
    for dz in (-.62, .62):
        box(b, (.12, 1.46, .1), (x - .02, y, z + dz), "M_Wood_Dark")
    box(b, (.1, .06, 1.1), (x - .04, y, z), "M_Wood_Dark")
    box(b, (.1, 1.3, .06), (x - .04, y, z), "M_Wood_Dark")
    box(b, (.26, 1.6, .06), (x - .1, y, z - .69), "M_Wood")
    if shutters:  # thrown open into the room, CozyFarm teal
        for side in (-1, 1):
            d = Vector((-math.sin(math.radians(70)), side * math.cos(math.radians(70)), 0))
            centre = Vector((x - .06, y + side * .74, z)) + d * .31
            yaw = math.degrees(math.atan2(-d.x, d.y))
            box(b, (.04, .6, 1.06), centre, "M_Shutter", rot=(0, 0, yaw))
            for dz in (-.3, .3):
                box(b, (.06, .5, .06), centre + Vector((0, 0, dz)), "M_Wood_Dark", rot=(0, 0, yaw))
    if sill_items:
        flower_pot(b, x - .12, y + .4, z - .66, .7)


def flower_pot(b, x, y, z, s=1.0, flower="M_Flower_Pink"):
    b.cylinder(.13 * s, .2 * s, (x, y, z + .1 * s), "M_Terracotta", segments=10, radius2=.17 * s)
    b.sphere(.15 * s, (x, y, z + .27 * s), "M_Leaf_Small", scale=(1, 1, .7))
    for a in range(5):
        ang = a * 1.257
        b.sphere(.045 * s, (x + math.cos(ang) * .09 * s, y + math.sin(ang) * .09 * s, z + .34 * s), flower if a % 2 else "M_Flower_White")


def lantern(b, x, y, z, hang=0.0):
    if hang:
        b.cylinder(.008, hang, (x, y, z + .2 + hang / 2), "M_Metal", segments=6)
    b.cylinder(.1, .04, (x, y, z), "M_Metal", segments=8)
    b.cylinder(.075, .2, (x, y, z + .12), "M_Lamp_Glow", segments=8)
    b.cylinder(.11, .05, (x, y, z + .245), "M_Metal", segments=8, radius2=.04)


def crate(b, x, y, s=.6, z=0.0, rot=0):
    box(b, (s, s, s * .86), (x, y, z + s * .43), "M_Plank", rot=(0, 0, rot))
    for dz in (.12, .74):
        box(b, (s + .02, s + .02, .06), (x, y, z + s * dz), "M_Wood_Dark", rot=(0, 0, rot))


def barrel(b, x, y, s=1.0):
    b.cylinder(.3 * s, .8 * s, (x, y, .4 * s), "M_Wood", segments=12)
    for z in (.15, .65):
        b.torus(.305 * s, .02, (x, y, z * s), "M_Metal", major_seg=12, minor_seg=4)


def basket(b, x, y, z=0.0):
    b.cylinder(.26, .3, (x, y, z + .15), "M_Basket", segments=12, radius2=.32)
    for i, m in enumerate(("M_Fruit_Red", "M_Cabbage", "M_Carrot", "M_Fruit_Red", "M_Cabbage")):
        a = i * 1.3
        b.sphere(.09, (x + math.cos(a) * .12, y + math.sin(a) * .12, z + .33), m)


def rug(b, x, y, rx, ry, outer="F_Rug", inner="F_Mustard"):
    """Oval rug: two discs squashed by (rx, ry). Call it first on its Builder, it scales every vertex already in there."""
    b.cylinder(1, .02, (x, y, .03), outer, segments=20)
    b.cylinder(.78, .022, (x, y, .032), inner, segments=20)
    for v in b.bm.verts:
        v.co.x = x + (v.co.x - x) * rx
        v.co.y = y + (v.co.y - y) * ry
    for f in b.bm.faces:
        f.smooth = False


def bed(b, frame, quilt, quilt2=None, posts=True, patch=False):
    x, y = BED
    box(b, (1.3, 2.2, .3), (x, y, .25), frame)
    for dx in (-.6, .6):
        for dy in (-1.05, 1.05):
            box(b, (.1, .1, .16 if not posts else (.95 if dy > 0 else .6)), (x + dx, y + dy, (.95 if dy > 0 else .6) / 2 if posts else .08), "M_Wood_Dark")
    box(b, (1.2, .08, .55 if posts else .8), (x, y + 1.06, .68 if posts else .5), frame)
    box(b, (1.2, 2.0, .2), (x, y, .5), "M_Flower_White", bevel=.04)
    box(b, (.85, .42, .14), (x, y + .72, .66), "M_Flower_White", bevel=.05)
    if patch:  # patchwork quilt: 3 x 4 squares
        for i in range(3):
            for j in range(4):
                m = (quilt, quilt2, "F_Cream")[(i + j) % 3]
                box(b, (.42, .36, .06), (x - .42 + i * .42, y - .82 + j * .36, .62), m, bevel=.01)
        box(b, (1.3, .06, .3), (x, y - .99, .5), quilt, bevel=.01)
    else:
        box(b, (1.28, 1.45, .07), (x, y - .27, .62), quilt, bevel=.02)
        box(b, (1.3, .06, .32), (x, y - .99, .48), quilt, bevel=.01)
        for side in (-1, 1):
            box(b, (.05, 1.45, .3), (x + side * .64, y - .27, .48), quilt, bevel=.01)
        if quilt2:
            box(b, (1.29, .22, .075), (x, y + .36, .625), quilt2, bevel=.02)


def bedside_table(b, style="drawer", has_letter=True):
    x, y = TABLE
    if style == "crate":
        crate(b, x, y, .6)
        top = .52
    else:
        box(b, (.6, .55, .06), (x, y, .57), "M_Wood")
        for dx in (-.25, .25):
            for dy in (-.22, .22):
                box(b, (.06, .06, .54), (x + dx, y + dy, .27), "M_Wood_Dark")
        box(b, (.5, .45, .2), (x, y, .42), "M_Plank")
        b.sphere(.03, (x, y - .24, .42), "M_Metal")
        box(b, (.5, .45, .04), (x, y, .1), "M_Wood")
        top = .6
    if has_letter:
        letter(b, x - .05, y - .05, top)
    return x, y, top


# ---------- A: Timber Cottage (the house's outside, brought in) ----------

def concept_a(mats, offset):
    r = Room(mats, "Bedroom_A", offset)
    shell, furn, deco = r.b("Shell"), r.b("Furniture"), r.b("Decor")
    plank_floor(shell)
    for size, loc in (((5.4, .2, 2.8), (0, 2.2, 1.4)), ((.2, 4.6, 2.8), (-2.6, 0, 1.4)), ((.2, 4.6, 2.8), (2.6, 0, 1.4))):
        box(shell, size, loc, "M_Wall")
    # timber frame + stone skirting, like the cottage walls outside
    box(shell, (5.4, .1, .25), (0, 2.08, .125), "M_Stone")
    for x in (-2.48, 2.48):
        box(shell, (.1, 4.6, .25), (x, 0, .125), "M_Stone")
    box(shell, (5.4, .14, .18), (0, 2.06, 2.71), "M_Wood_Dark")
    for x in (-2.46, 2.46):
        box(shell, (.14, 4.6, .18), (x, 0, 2.71), "M_Wood_Dark")
    for x in (-2.44, -.6, 2.44):
        box(shell, (.16, .16, 2.8), (x, 2.04, 1.4), "M_Wood_Dark")
    for y in (-2.25, -.5, 2.04):
        for x in (-2.44, 2.44):
            box(shell, (.16, .16, 2.8), (x, y, 1.4), "M_Wood_Dark")
    box(shell, (.14, 2.4, .12), (-2.47, .4, 1.4), "M_Wood_Dark", rot=(38, 0, 0))
    door(shell, DOOR_X, 2.2)
    east_window(shell)

    bed(furn, "M_Wood_Dark", "M_Roof", "F_Cream")
    tx, ty, top = bedside_table(furn)
    lantern(deco, tx + .17, ty + .05, top)
    # wardrobe left of the door
    box(furn, (1.1, .55, 2.0), (-.05, 1.8, 1.0), "M_Wood")
    for dx in (-.27, .27):
        box(furn, (.5, .04, 1.7), (-.05 + dx, 1.51, 1.05), "M_Plank")
        box(furn, (.03, .03, .2), (-.05 + dx * .2, 1.48, 1.05), "M_Metal")
    box(furn, (1.18, .6, .08), (-.05, 1.8, 2.03), "M_Wood_Dark")
    basket(deco, -.25, 1.8, 2.07)
    # chest at the foot of the bed
    box(furn, (1.0, .5, .45), (-1.6, -1.75, .225), "M_Wood")
    box(furn, (1.04, .54, .1), (-1.6, -1.75, .48), "M_Wood_Dark")
    box(furn, (.12, .04, .14), (-1.6, -2.01, .38), "M_Metal")
    # shelf over the bed with jars and a picture
    box(deco, (.25, 1.2, .05), (-2.4, -.1, 1.6), "M_Wood")
    for i, m in enumerate(("M_Glass", "M_Carrot", "M_Glass", "M_Cabbage")):
        deco.cylinder(.06, .16, (-2.4, -.55 + i * .26, 1.71), m, segments=8)
    box(deco, (.04, .6, .45), (-2.47, -.3, 2.15), "M_Wood_Dark")
    box(deco, (.04, .5, .35), (-2.45, -.3, 2.15), "M_Glass")
    rug(r.b("Rug"), .4, -.6, 1.15, .8)
    flower_pot(deco, 2.15, -1.9, 0, 1.3)
    r.finish()
    return r


# ---------- B: Attic Loft (under the red roof) ----------

def concept_b(mats, offset):
    r = Room(mats, "Bedroom_B", offset)
    shell, furn, deco = r.b("Shell"), r.b("Furniture"), r.b("Decor")
    plank_floor(shell, ("M_Wood", "M_Plank", "M_Wood_Dark"), .3)
    knee, ridge = 1.1, 3.5
    slope = math.degrees(math.atan2(ridge - knee, 2.6))
    run = math.hypot(2.6, ridge - knee)
    for side in (-1, 1):
        box(shell, (.2, 4.6, knee), (side * 2.6, 0, knee / 2), "M_Wall")
        # roof underside: wood boards between dark rafters
        cx, cz = side * 1.3, (knee + ridge) / 2 + .1
        box(shell, (run, 4.6, .08), (cx, 0, cz + .06), "M_Plank", rot=(0, side * slope, 0))
        for y in (-2.2, -1.1, 0, 1.1, 2.1):
            box(shell, (run, .14, .16), (cx, y, cz - .04), "M_Wood_Dark", rot=(0, side * slope, 0))
    box(shell, (.18, 4.6, .2), (0, 0, ridge + .02), "M_Wood_Dark")
    # gable wall (north): a wall box cut to the roof line by stacking shrinking strips
    for i in range(12):
        z0 = i * ridge / 12
        w = 5.2 if z0 < knee else 5.2 * (ridge - z0) / (ridge - knee)
        box(shell, (w, .2, ridge / 12 + .01), (0, 2.2, z0 + ridge / 24), "M_Wall", bevel=0)
    # round window high in the gable
    shell.cylinder(.42, .14, (-.6, 2.12, 2.55), "M_Wood_Dark", axis="Y", segments=16)
    shell.cylinder(.34, .16, (-.6, 2.11, 2.55), "M_Glass", axis="Y", segments=16)
    box(shell, (.68, .17, .05), (-.6, 2.1, 2.55), "M_Wood_Dark")
    box(shell, (.05, .17, .68), (-.6, 2.1, 2.55), "M_Wood_Dark")
    door(shell, DOOR_X, 2.2, leaf="M_Shutter")
    # skylight in the east slope where the window was
    cx, cz = 1.3, (knee + ridge) / 2 + .2
    box(shell, (1.1, 1.2, .1), (cx, WINDOW_Y, cz), "M_Glass", rot=(0, slope, 0), bevel=.005)
    box(shell, (1.24, 1.34, .08), (cx - .01, WINDOW_Y, cz - .02), "M_Wood_Dark", rot=(0, slope, 0))

    bed(furn, "M_Wood", "F_Teal", "F_Mustard", posts=False)
    tx, ty, top = bedside_table(furn, "crate")
    deco.cylinder(.05, .12, (tx + .18, ty + .1, top + .06), "M_Flower_White", segments=8)
    deco.sphere(.03, (tx + .18, ty + .1, top + .15), "M_Lamp_Glow", scale=(1, 1, 1.5))
    lantern(deco, 0, -.8, 2.55, hang=.75)
    # trunk + books under the east slope
    box(furn, (.6, 1.1, .5), (2.15, -1.3, .25), "M_Roof_Dark")
    box(furn, (.64, 1.14, .1), (2.15, -1.3, .53), "M_Wood_Dark")
    for dy in (-.3, .3):
        box(furn, (.66, .06, .52), (2.15, -1.3 + dy, .27), "M_Metal")
    for i, m in enumerate(("F_Teal", "M_Roof", "F_Mustard")):
        box(deco, (.3, .22, .06), (2.15, -1.2, .61 + i * .06), m, rot=(0, 0, i * 14))
    # low desk + stool under the skylight
    box(furn, (.6, 1.2, .06), (2.1, WINDOW_Y + .1, .72), "M_Wood")
    for dy in (-.5, .5):
        box(furn, (.5, .06, .7), (2.1, WINDOW_Y + .1 + dy, .35), "M_Wood_Dark")
    furn.cylinder(.18, .05, (1.55, WINDOW_Y, .45), "M_Wood", segments=10)
    for a in range(3):
        furn.cylinder(.025, .45, (1.55 + math.cos(a * 2.09) * .12, WINDOW_Y + math.sin(a * 2.09) * .12, .22), "M_Wood_Dark", segments=6)
    flower_pot(deco, 2.1, WINDOW_Y + .45, .75, .6)
    box(deco, (.32, .24, .015), (2.05, WINDOW_Y - .15, .76), "M_Flower_White", rot=(0, 0, -10), bevel=.003)
    # drawings pinned to the west knee wall, a beanbag cushion by the door
    for i, m in enumerate(("M_Flower_White", "F_Cream", "M_Flower_White")):
        box(deco, (.03, .3, .38), (-2.48, -1.6 + i * .5, .7 + (i % 2) * .08), m, rot=(i * 6 - 6, 0, 0), bevel=.004)
    deco.sphere(.4, (.3, 1.4, .25), "F_Mustard", scale=(1, 1, .6))
    rug(r.b("Rug"), .2, -.7, 1.0, .9, "F_Teal", "F_Cream")
    r.finish()
    return r


# ---------- C: Farmhand's Den (lived-in, the money worries on the wall) ----------

def concept_c(mats, offset):
    r = Room(mats, "Bedroom_C", offset)
    shell, furn, deco = r.b("Shell"), r.b("Furniture"), r.b("Decor")
    plank_floor(shell, ("M_Wood", "M_Plank"), .5)
    for size, loc in (((5.4, .2, 2.8), (0, 2.2, 1.4)), ((.2, 4.6, 2.8), (-2.6, 0, 1.4)), ((.2, 4.6, 2.8), (2.6, 0, 1.4))):
        box(shell, size, loc, "M_Wall")
    # dark vertical-plank wainscot up to 1 m with a rail
    for i in range(27):
        x = -2.5 + i * .2 + .1
        box(shell, (.19, .05, 1.0), (x, 2.08, .5), ("M_Wood", "M_Plank")[i % 2], bevel=.004)
    for side in (-1, 1):
        for i in range(23):
            y = -2.3 + i * .2 + .1
            box(shell, (.05, .19, 1.0), (side * 2.48, y, .5), ("M_Wood", "M_Plank")[i % 2], bevel=.004)
        box(shell, (.1, 4.6, .08), (side * 2.46, 0, 1.03), "M_Wood_Dark")
    box(shell, (5.4, .1, .08), (0, 2.05, 1.03), "M_Wood_Dark")
    box(shell, (5.4, .16, .16), (0, 2.05, 2.72), "M_Wood_Dark")
    door(r.b("Door"), DOOR_X, 2.2)
    east_window(shell, shutters=False, sill_items=False)
    # herb planter on the sill
    box(deco, (.22, 1.2, .18), (2.3, WINDOW_Y, .86), "M_Plank")
    for i in range(4):
        deco.sphere(.1, (2.3, WINDOW_Y - .42 + i * .28, 1.0), ("M_Leaf_Small", "M_Cabbage")[i % 2], scale=(1, 1, 1.3))
    # curtains
    for side in (-1, 1):
        box(deco, (.06, .3, 1.4), (2.4, WINDOW_Y + side * .85, 1.3), "F_Mustard", bevel=.02)
    box(deco, (.04, 2.1, .04), (2.42, WINDOW_Y, 2.02), "M_Metal")

    bed(r.b("Bed"), "M_Wood", "F_Teal", "F_Mustard", posts=False, patch=True)
    tx, ty, top = bedside_table(r.b("BedsideTable"), has_letter=False)
    lantern(r.b("BedsideTable"), tx + .17, ty + .08, top)
    # work desk (CozyFarm workbench) along the east wall, south of the window
    box(furn, (.7, 1.4, .07), (2.2, -1.2, .82), "M_Wood")
    for dx in (-.3, .3):
        for dy in (-.62, .62):
            box(furn, (.07, .07, .8), (2.2 + dx, -1.2 + dy, .4), "M_Wood_Dark")
    box(furn, (.6, 1.3, .05), (2.2, -1.2, .2), "M_Plank")
    for i, m in enumerate(("M_Glass", "M_Glass", "M_Glass")):
        deco.cylinder(.06, .18, (2.35, -1.75 + i * .16, .95), m, segments=8)
        deco.sphere(.04, (2.35, -1.75 + i * .16, .93), ("M_Carrot", "M_Cabbage", "M_Fruit_Red")[i])
    box(deco, (.34, .26, .015), (2.1, -1.1, .865), "M_Flower_White", rot=(0, 0, 8), bevel=.003)
    box(deco, (.34, .26, .015), (2.15, -1.05, .88), "F_Cream", rot=(0, 0, -12), bevel=.003)
    deco.cylinder(.05, .1, (2.25, -.7, .9), "M_Terracotta", segments=8)
    furn.cylinder(.2, .05, (1.6, -1.3, .48), "M_Wood", segments=10)
    for a in range(3):
        furn.cylinder(.025, .46, (1.6 + math.cos(a * 2.09) * .13, -1.3 + math.sin(a * 2.09) * .13, .23), "M_Wood_Dark", segments=6)
    # calendar on the north wall: 30 days, the first ones crossed out in red
    box(deco, (.6, .03, .7), (-.3, 2.08, 1.75), "M_Flower_White", bevel=.004)
    box(deco, (.6, .035, .14), (-.3, 2.075, 2.05), "M_Roof", bevel=.004)
    for d in range(30):
        cx, cz = -.52 + (d % 6) * .088, 1.9 - (d // 6) * .1
        box(deco, (.07, .04, .07), (cx, 2.06, cz), "M_Fruit_Red" if d < 3 else "F_Cream", bevel=.0)
    # pegs: straw hat + rake by the door
    for x in (.25, .45):
        deco.cylinder(.02, .12, (x, 2.06, 1.6), "M_Wood_Dark", axis="Y", segments=6)
    deco.cylinder(.24, .02, (.35, 2.02, 1.5), "F_Mustard", axis="Y", segments=14)
    deco.cylinder(.13, .1, (.35, 1.99, 1.5), "F_Mustard", axis="Y", segments=12)
    box(deco, (.04, .04, 1.6), (2.25, 1.95, .8), "M_Wood", rot=(0, -8, 0))
    box(deco, (.45, .06, .05), (2.37, 1.95, 1.6), "M_Metal", rot=(0, -8, 0))
    # farm clutter: seed sacks, barrel, basket
    barrel(deco, -.5, 1.75, .9)
    basket(deco, -.5, 1.75, .72)
    for i, (x, y) in enumerate(((.2, 1.8), (.62, 1.85))):
        deco.sphere(.3, (x, y, .28), "F_Cream", scale=(1, .8, 1.1))
        deco.cylinder(.08, .12, (x, y, .64), "F_Cream", segments=8)
    rug(r.b("Rug"), .3, -.3, 1.2, .75, "F_Rug", "F_Teal")
    r.finish()
    return r


# ---------- render ----------

def look(cam, pos, target):
    cam.location = pos
    cam.rotation_euler = (Vector(target) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()


def render(rooms):
    os.makedirs(OUT, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    shading = scene.display.shading
    shading.light, shading.color_type = "STUDIO", "MATERIAL"
    shading.show_shadows, shading.shadow_intensity = True, .45
    shading.show_cavity, shading.cavity_type = True, "BOTH"
    scene.display.light_direction = (-.35, -.55, .76)
    scene.world = bpy.data.worlds.new("Sky")
    scene.world.color = (.6, .74, .84)
    scene.render.resolution_x, scene.render.resolution_y = 1280, 720
    scene.render.film_transparent = False
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.sensor_fit, cam.data.angle = "VERTICAL", math.radians(46)
    scene.collection.objects.link(cam)
    scene.camera = cam
    for room in rooms:
        for other in rooms:
            for obj in [other.root, *other.root.children]:
                obj.hide_render = other is not room
        o = room.offset
        views = {"game": ((.9, -4.6, 3.3), (-.2, .3, .6)), "overview": ((4.2, -6.4, 6.2), (-.1, .1, .6))}
        for view, (pos, target) in views.items():
            look(cam, o + Vector(pos), o + Vector(target))
            scene.render.filepath = os.path.join(OUT, f"{room.name}_{view}.png")
            bpy.ops.render.render(write_still=True)
    for room in rooms:
        for obj in [room.root, *room.root.children]:
            obj.hide_render = False


def linear_to_srgb_hex(c):
    return "".join(f"{round(255 * (1.055 * v ** (1 / 2.4) - .055 if v > .0031308 else 12.92 * v)):02X}" for v in c)


def export_chosen(room):
    for mat in bpy.data.materials:
        if mat.name in COLORS:
            mat.name = "Bedroom_" + linear_to_srgb_hex(COLORS[mat.name])
    objs = []
    for part, obj in room.objects.items():
        obj.parent = None
        pivot = Vector(PIVOTS.get(part, (0, 0, 0)))
        # mesh relative to its pivot, turned 180 degrees so the FBX axis swap lands Blender (x, y, z) on Unity (x, z, y)
        obj.data.transform(Matrix.Rotation(math.pi, 4, "Z") @ Matrix.Translation(obj.location - pivot))
        obj.location = (0, 0, 0)
        obj.name = obj.data.name = part
        objs.append(obj)
    print(f"BEDROOM export tris={sum(len(p.vertices) - 2 for o in objs for p in o.data.polygons)}")
    common.export_fbx(objs, OUT_FBX)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mats = materials()
    rooms = [concept_a(mats, (0, 0, 0)), concept_b(mats, (8, 0, 0)), concept_c(mats, (16, 0, 0))]
    render(rooms)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    export_chosen(rooms[2])
    print("BEDROOM concepts done")


main()
