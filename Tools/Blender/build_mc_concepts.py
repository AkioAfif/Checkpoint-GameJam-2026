# Three main-character concepts (big-head, short-limb villager proportions) next to Ilkham's farm props for a style check.
# Run inside Blender: exec(open(r"<repo>/Tools/Blender/build_mc_concepts.py").read()). Writes Exports/MC_Concepts/*.png + Tools/Blender/MC_Concepts.blend.
# Units = metres, Z up, characters face -Y. Height ~1.6 m to fit the Player CharacterController (h 1.6).
import math
import os
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

REPO = r"D:\Akio\GameDev Project\Checkpoint GameJam 2026"
sys.path.insert(0, os.path.join(REPO, "Tools", "Blender"))
from day1_common import Builder  # noqa: E402

OUT = os.path.join(REPO, "Exports", "MC_Concepts")

# Linear colours lifted from the farm glbs (M_Wall, M_Roof, M_Shutter, ...) so the characters share the environment palette.
FARM = {
    "Cream": (.896, .768, .558), "Red": (.584, .095, .05), "RedDark": (.361, .053, .028), "Teal": (.076, .262, .254),
    "Brown": (.254, .091, .03), "Wood": (.392, .188, .068), "WoodDark": (.156, .068, .025), "Leaf": (.109, .402, .048),
    "Moss": (.205, .479, .06), "Sun": (1, .687, .25), "Sky": (.381, .716, .831), "Stone": (.418, .381, .328), "Ink": (.048, .042, .037),
    "Straw": (.82, .6, .26), "Blush": (.9, .35, .3), "White": (.9, .88, .82),
    "SkinA": (.9, .55, .36), "SkinB": (.42, .2, .1), "SkinC": (.95, .66, .48),
    "HairA": (.156, .068, .025), "HairB": (.048, .042, .037), "HairC": (.584, .25, .07),
}


def materials():
    mats = {}
    for name, lin in FARM.items():
        m = bpy.data.materials.get("MC_" + name) or bpy.data.materials.new("MC_" + name)
        m.diffuse_color = (*lin, 1)
        m.roughness = .9
        mats[name] = m
    return mats


def _rot(b, part, mat, loc, rot, smooth):
    b._append(part, mat, Vector(loc), rot, smooth)


def sph(b, r, loc, mat, scale=(1, 1, 1), rot=(0, 0, 0), u=12, v=8):
    part = bmesh.new()
    bmesh.ops.create_uvsphere(part, u_segments=u, v_segments=v, radius=r)
    for vert in part.verts:
        vert.co = Vector((vert.co.x * scale[0], vert.co.y * scale[1], vert.co.z * scale[2]))
    _rot(b, part, mat, loc, rot, True)


def cyl(b, r1, r2, depth, loc, mat, rot=(0, 0, 0), seg=12, smooth=True):
    part = bmesh.new()
    bmesh.ops.create_cone(part, cap_ends=True, cap_tris=False, segments=seg, radius1=r1, radius2=r2, depth=depth)
    _rot(b, part, mat, loc, rot, smooth)


# ---------------------------------------------------------------- shared body
HEAD_Z, HEAD_R = 1.12, .37


def body(b, skin, top, bottom, shoe, hair, eye="Ink", torso_len=.44, flare=0.0, arm_mat=None, fringe=True):
    """Villager base: 2.2-head-tall body, oversized round head, stubby limbs, mitten hands."""
    # feet + legs
    for s in (1, -1):
        sph(b, .085, (s * .1, -.03, .05), shoe, scale=(1, 1.35, .62))
        cyl(b, .065, .07, .26, (s * .1, 0, .2), bottom, seg=8)
    # torso: short barrel, wider at the hips (flare widens it into a coat/smock)
    cyl(b, .24 + flare, .19, torso_len, (0, 0, .3 + torso_len / 2), top, seg=14)
    cyl(b, .19, .13, .08, (0, 0, .3 + torso_len + .03), top, seg=14)
    # arms angled slightly out, sleeve + mitten
    a = 16
    for s in (1, -1):
        sh = Vector((s * .2, 0, .3 + torso_len - .06))
        d = Vector((s * math.sin(math.radians(a)), 0, -math.cos(math.radians(a))))
        cyl(b, .07, .062, .3, sh + d * .15, arm_mat or top, rot=(0, -s * a, 0), seg=8)
        sph(b, .075, sh + d * .33, skin, scale=(1, 1, 1.05))
    # head + face
    sph(b, HEAD_R, (0, 0, HEAD_Z), skin, scale=(1, .93, .92), u=16, v=10)
    fy = -HEAD_R * .93
    for s in (1, -1):
        sph(b, .042, (s * .125, fy * .93, HEAD_Z - .01), eye, scale=(.75, .4, 1.15), u=8, v=6)
        sph(b, .05, (s * .22, fy * .8, HEAD_Z - .1), "Blush", scale=(1, .25, .6), u=8, v=6)
    sph(b, .028, (0, fy * .99, HEAD_Z - .12), "RedDark", scale=(1.3, .4, .55), u=8, v=4)  # small mouth
    # hair: shell set well behind the face (no z-fighting) + a fringe puff over the forehead
    sph(b, HEAD_R * 1.06, (0, .1, HEAD_Z + .05), hair, scale=(1, .9, .93), u=16, v=10)
    if fringe:
        sph(b, .3, (0, -.17, HEAD_Z + .22), hair, scale=(1.05, .6, .4), u=12, v=6)


# ---------------------------------------------------------------- concepts
def concept_a(mats):
    """'Meadow' - wide-brim sun hat, overalls over a plain tee, boots."""
    b = Builder(mats)
    body(b, "SkinA", "Teal", "Teal", "Brown", "HairA", arm_mat="Cream")
    cyl(b, .2, .2, .2, (0, 0, .62), "Cream", seg=14)  # tee shows above the bib
    b.box((.22, .04, .16), (0, -.21, .62), "Teal", bevel=.02)  # bib
    for s in (1, -1):
        b.box((.04, .02, .2), (s * .09, -.195, .74), "Teal", bevel=.008, rot=(-10, 0, 0))  # straps
        sph(b, .02, (s * .08, -.225, .66), "Sun", u=6, v=4)  # buttons
    # side tufts
    for s in (1, -1):
        sph(b, .09, (s * .33, -.05, HEAD_Z - .12), "HairA", scale=(.6, .8, 1), u=8, v=6)
    # sun hat
    hz = HEAD_Z + .3
    cyl(b, .56, .5, .035, (0, 0, hz), "Straw", seg=20, smooth=False)
    cyl(b, .27, .24, .2, (0, 0, hz + .11), "Straw", seg=14, smooth=False)
    cyl(b, .275, .272, .05, (0, 0, hz + .04), "Red", seg=14, smooth=False)
    return b.build("MC_A_Meadow")


def concept_b(mats):
    """'Trail' - rounded cap with a short bill, zip jacket, shorts, sneakers."""
    b = Builder(mats)
    body(b, "SkinB", "Red", "WoodDark", "White", "HairB", arm_mat="Red", fringe=False)
    b.box((.03, .02, .3), (0, -.205, .56), "Cream", bevel=.008)  # zip line
    cyl(b, .245, .24, .06, (0, 0, .32), "RedDark", seg=14)  # jacket hem
    for s in (1, -1):
        cyl(b, .071, .071, .03, (s * .1, 0, .07), "White", seg=8)  # socks
    # cap: dome over the hair + front panel + downturned bill + button
    sph(b, HEAD_R * 1.12, (0, .05, HEAD_Z + .13), "Sun", scale=(1, .97, .72), u=14, v=8)
    b.box((.4, .26, .03), (0, -.42, HEAD_Z + .15), "Sun", bevel=.012, rot=(8, 0, 0))
    cyl(b, HEAD_R * 1.12, HEAD_R * 1.1, .04, (0, .05, HEAD_Z + .15), "Red", seg=14)
    sph(b, .035, (0, .05, HEAD_Z + .43), "Red", u=8, v=4)
    return b.build("MC_B_Trail")


def concept_c(mats):
    """'Drizzle' - soft bucket hat, long smock coat, tall boots."""
    b = Builder(mats)
    body(b, "SkinC", "Sun", "Stone", "Teal", "HairC", torso_len=.46, flare=.05, arm_mat="Sun")
    for s in (1, -1):
        cyl(b, .078, .078, .16, (s * .1, 0, .13), "Teal", seg=8)  # boot shafts
        b.box((.08, .03, .07), (s * .12, -.235, .45), "WoodDark", bevel=.01)  # pockets
    for z in (.68, .58):
        sph(b, .022, (0, -.215, z), "WoodDark", u=6, v=4)  # toggles
    # bob hair ends
    for s in (1, -1):
        sph(b, .12, (s * .3, .02, HEAD_Z - .14), "HairC", scale=(.7, .9, 1), u=8, v=6)
    # bucket hat: sloping brim + soft crown + band
    hz = HEAD_Z + .27
    cyl(b, .5, .35, .1, (0, 0, hz), "Leaf", seg=18, smooth=False)
    cyl(b, .33, .29, .2, (0, 0, hz + .14), "Leaf", seg=14, smooth=False)
    sph(b, .29, (0, 0, hz + .24), "Leaf", scale=(1, 1, .3), u=14, v=6)
    cyl(b, .335, .33, .045, (0, 0, hz + .07), "Moss", seg=14, smooth=False)
    return b.build("MC_C_Drizzle")


# ---------------------------------------------------------------- scene + renders
def import_refs():
    """Returns the imported reference objects so turnarounds can hide them."""
    imported = []
    refs = {"houses/house.glb": (-6.5, 6), "trees/tree_1.glb": (6, 6), "props/prop_barrel.glb": (-2.4, .6),
            "props/prop_mailbox.glb": (2.4, .4), "props/prop_lamp.glb": (3.4, 1.4), "bushes/bush_1.glb": (-3.6, 1.6)}
    for rel, (x, y) in refs.items():
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=os.path.join(REPO, "Assets", "Ilkham", "models", rel))
        for o in set(bpy.data.objects) - before:
            imported.append(o)
            if o.parent is None:
                o.location = (x, y, 0)
    return imported


def tri_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def render(name, loc, rot, ortho):
    scene = bpy.context.scene
    cam = scene.camera
    cam.location, cam.rotation_euler = loc, rot
    cam.data.ortho_scale = ortho
    scene.render.filepath = os.path.join(OUT, name + ".png")
    bpy.ops.render.render(write_still=True)


def main():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    os.makedirs(OUT, exist_ok=True)
    mats = materials()
    chars = [concept_a(mats), concept_b(mats), concept_c(mats)]
    for c, x in zip(chars, (-1.2, 0, 1.2)):
        c.location.x = x
    refs = import_refs()
    ground = bpy.data.objects.new("Ground", bpy.data.meshes.new("Ground"))
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=30)
    bm.to_mesh(ground.data)
    bm.free()
    gm = bpy.data.materials.new("MC_Ground")
    gm.diffuse_color = (*FARM["Moss"], 1)
    ground.data.materials.append(gm)
    bpy.context.scene.collection.objects.link(ground)

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_cavity = True
    scene.display.shading.show_object_outline = True
    scene.display.shading.show_shadows = True
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    cam = bpy.data.objects.new("ConceptCam", bpy.data.cameras.new("ConceptCam"))
    cam.data.type = "ORTHO"
    scene.collection.objects.link(cam)
    scene.camera = cam

    def aim(target, az, el, dist):
        d = Vector((math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)))
        return Vector(target) + d * dist, (-d).to_track_quat("-Z", "Y").to_euler()

    render("00_lineup_with_farm", *aim((0, 2, 2.2), .35, .3, 30), 17)
    render("01_lineup_closeup", *aim((0, 0, .85), .3, .18, 12), 4.6)
    # per-concept turnaround (front / 3-4 / side / back), others hidden
    scene.render.resolution_x, scene.render.resolution_y = 900, 900
    for o in refs:
        o.hide_render = True
    for c in chars:
        for o in chars:
            o.hide_render = o is not c
        for view, az in (("front", 0), ("threequarter", .75), ("side", 1.5708), ("back", 3.1416)):
            render(f"{c.name}_{view}", *aim(c.location + Vector((0, 0, .85)), az, .12, 10), 2.1)
    for o in chars + refs:
        o.hide_render = False

    stats = {c.name: {"tris": tri_count(c), "height": round(c.dimensions.z, 3)} for c in chars}
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(REPO, "Tools", "Blender", "MC_Concepts.blend"))
    return stats


result = main()
