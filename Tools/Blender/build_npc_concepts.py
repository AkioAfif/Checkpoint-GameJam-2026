# NPC concept models: six villagers x three archetypes, all built from one parametric "villager kit" in the Meadow style
# (one continuous upper body grown from a skin-modifier skeleton, straight legs under a flat hem, clothes as colour regions,
# 2D face-sheet eyes/mouth). Each variant differs in head shape, body proportions/scale, clothes, hair, hat and props.
# Run inside Blender: exec(open(r"<repo>/Tools/Blender/build_npc_concepts.py").read(), {"__name__": "mc"}).
# Writes Exports/NPC_Concepts/<npc>_body.png, <npc>_<variant>_face.png, specs.json and Tools/Blender/NPC_Concepts.blend.
import json
import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

REPO = r"D:\Akio\GameDev Project\Checkpoint GameJam 2026"
sys.path.insert(0, os.path.join(REPO, "Tools", "Blender"))
from day1_common import srgb_to_linear  # noqa: E402

OUT = os.path.join(REPO, "Exports", "NPC_Concepts")
FACE_SHEET = os.path.join(REPO, "Assets", "_Project", "Art", "Models", "Meadow", "MC_Face.png")
MEADOW_BLEND = os.path.join(REPO, "Tools", "Blender", "MC_Meadow_Rigged.blend")
EYES = ["Open", "HalfBlink", "Closed", "Happy", "Surprised", "Angry", "Sad", "Squint"]
MOUTHS = ["Smile", "TalkSmall", "TalkWide", "Grin", "Frown", "Flat", "Shock", "Wavy"]

SKIN = {"light": "#F6D2B8", "fair": "#E8B48E", "tan": "#C98E64", "brown": "#9A6440", "deep": "#6E4529"}

# adult defaults (metres, before the variant's overall scale)
BASE = dict(hem=.34, torso_h=.44, hip=.22, belly=.22, chest=.19, depth=.85, shoulder_x=.12, arm_len=.36, arm_r=.058, hand_r=.068,
            leg_r=.066, leg_x=.095, neck=.06, neck_r=.075, head_r=.34, head_shape="round", arm_down=62, eye_sp=.34, eye_h=-.03,
            eye_size=.55, scale=1.0, shoe=(.085, .12, .058))
KID = dict(hem=.22, torso_h=.3, hip=.15, belly=.16, chest=.14, shoulder_x=.09, arm_len=.25, arm_r=.045, hand_r=.055, leg_r=.05,
           leg_x=.07, neck=.03, neck_r=.055, head_r=.3, eye_size=.62, shoe=(.065, .09, .045))
ELDER = dict(hem=.28, torso_h=.38, hip=.22, belly=.22, chest=.18, shoulder_x=.1, arm_len=.3, head_r=.31, neck=.03, eye_size=.5, scale=.9)

# name -> (title, persona, [(variant title, one-liner, body params, look)])
NPCS = {
    "Ratna": ("Bu Ratna", "warm, generous neighbour who gardens", [
        ("Garden Mum", "Round face, pear body, coral blouse under a cream apron, long sage skirt, bun with a flower, a basket of blooms.",
         dict(hip=.26, belly=.25, chest=.2, head_shape="round"),
         dict(skin=SKIN["fair"], top="#E07A5F", sleeve="short", bottom="#81B29A", dress=True, overlays=[("apron", "#F4E4C1")],
              hair=("bun", "#4A2E1C"), hat=("flower", "#F2B48C"), props=[("basket", "#C8955A")])),
        ("Market Auntie", "Heart face, sturdy barrel build, mustard cardigan over a white blouse, navy skirt, bob, round glasses, bead necklace.",
         dict(hip=.25, belly=.27, chest=.23, head_shape="heart", scale=.98),
         dict(skin=SKIN["tan"], top="#F7F3EA", sleeve="long", sleeve_color="#E0B04A", bottom="#3E4C6E", dress=True,
              overlays=[("vest", "#E0B04A")], hair=("bob", "#2A2420"), props=[("glasses", "#3B3836"), ("necklace", "#F7F3EA")])),
        ("Blossom Neighbour", "Oval face, tall and slim, lavender dress with a white collar, auburn curls with a pink bow.",
         dict(hip=.21, belly=.2, chest=.18, torso_h=.48, head_shape="oval", scale=1.03),
         dict(skin=SKIN["light"], top="#A99BC9", sleeve="short", bottom="#A99BC9", dress=True,
              overlays=[("collar", "#F7F3EA")], hair=("curly", "#9A4A22"), hat=("bow", "#E8A0A8"))),
    ]),
    "Darto": ("Pak Darto", "easy-going older handyman who built the bridge", [
        ("Carpenter", "Square face, stocky and broad, wheat work shirt, brown trousers and belt, olive flat cap, grey moustache, hammer.",
         dict(hip=.23, belly=.25, chest=.25, shoulder_x=.14, arm_r=.068, head_shape="square", head_r=.35, neck=.04, scale=1.04),
         dict(skin=SKIN["tan"], top="#E8C88A", sleeve="short", bottom="#7A4A2A", overlays=[("belt", "#4A2E1C")],
              hair=("bald", "#A8A49C"), hat=("flatcap", "#8A9A4B"), brows=("#A8A49C", 0), props=[("mustache", "#A8A49C"), ("hammer", "#7A4A2A")])),
        ("Napper", "Long face, tall and lanky, sleepy eyes, long-sleeve shirt under an olive vest, charcoal trousers, rust beanie, grey beard.",
         dict(hip=.19, belly=.2, chest=.18, torso_h=.5, hem=.4, leg_r=.055, head_shape="long", head_r=.32, scale=1.06),
         dict(skin=SKIN["fair"], top="#F7F3EA", sleeve="long", bottom="#3B3836", overlays=[("vest", "#6B7F5B")], eyes="HalfBlink", mouth="Flat",
              hair=("short", "#A8A49C"), hat=("beanie", "#B5562F"), brows=("#A8A49C", -8), props=[("beard", "#A8A49C")])),
        ("Bridge Builder", "Wide-jaw triangle face, big round belly, sky-blue shirt under a leather work apron, navy trousers, square glasses, neck towel.",
         dict(hip=.24, belly=.3, chest=.23, head_shape="triangle", head_r=.36),
         dict(skin=SKIN["brown"], top="#8EC5D6", sleeve="short", bottom="#3E4C6E", overlays=[("apron", "#7A4A2A")],
              hair=("bald", "#6E6A64"), brows=("#3B3836", 0), props=[("mustache", "#3B3836"), ("glasses_square", "#3B3836"), ("towel", "#F4E4C1")])),
    ]),
    "Dimas": ("Dimas", "hyperactive kid who races everyone and counts flowers", [
        ("Little Sprinter", "Round face, small and wiry, striped tank jersey, navy shorts, yellow sneakers, spiky hair, red headband and wristbands.",
         dict(KID, head_shape="round", scale=.95),
         dict(skin=SKIN["tan"], top="#E07A5F", sleeve="none", bottom="#3E4C6E", legs="shorts", shoes="#E0B04A", wrist="#B5485D",
              overlays=[("stripes", "#F7F3EA")], mouth="Grin", hair=("spiky", "#2A2420"), hat=("headband", "#B5485D"))),
        ("Young Explorer", "Oval face, striped sage tee, brown shorts, backwards yellow cap, rust backpack, plaster on the cheek.",
         dict(KID, head_shape="oval", scale=.95),
         dict(skin=SKIN["light"], top="#81B29A", sleeve="short", bottom="#7A4A2A", legs="shorts", shoes="#B5485D",
              overlays=[("stripes", "#F4E4C1")], hair=("short", "#9A4A22"), hat=("cap_back", "#E0B04A"), props=[("backpack", "#B5562F"), ("plaster", "#F4E4C1")])),
        ("Flower Counter", "Heart face, oversized sky hoodie, cream shorts and long socks, bowl cut, round glasses, a little notebook.",
         dict(KID, head_shape="heart", scale=.92),
         dict(skin=SKIN["fair"], top="#8EC5D6", sleeve="long", bottom="#F4E4C1", legs="shorts", socks="#F7F3EA", shoes="#4A2E1C",
              hair=("bowl", "#4A2E1C"), props=[("glasses", "#B5485D"), ("hood", "#8EC5D6"), ("notebook", "#E07A5F")])),
    ]),
    "Ijah": ("Nek Ijah", "patient, wise grandmother", [
        ("Shawl Granny", "Triangle face, small and soft, lavender dress under a cream shawl, white bun, gold round glasses, smiling eyes, cane.",
         dict(ELDER, head_shape="triangle"),
         dict(skin=SKIN["fair"], top="#A99BC9", sleeve="long", bottom="#A99BC9", dress=True, overlays=[("shawl", "#F4E4C1")],
              eyes="Happy", hair=("bun", "#E9E5DC"), props=[("glasses", "#B08D57"), ("cane", "#7A4A2A")])),
        ("Tea-time Grandma", "Round face, rose cardigan, white apron, plum skirt, white curls, bead necklace, basket.",
         dict(ELDER, head_shape="round", belly=.25),
         dict(skin=SKIN["light"], top="#E8A0A8", sleeve="long", bottom="#6E5A7E", dress=True, overlays=[("apron", "#F7F3EA")],
              hair=("curly", "#E9E5DC"), props=[("necklace", "#F7F3EA"), ("basket", "#C8955A")])),
        ("Herb Keeper", "Diamond face, sage dress with a belt, mustard scarf, silver bob, tall walking staff.",
         dict(ELDER, head_shape="diamond", hip=.2, chest=.17, scale=.92),
         dict(skin=SKIN["brown"], top="#81B29A", sleeve="long", bottom="#81B29A", dress=True, overlays=[("belt", "#7A4A2A")],
              hair=("bob", "#D9D4CA"), props=[("scarf", "#E0B04A"), ("staff", "#8A6E4B")])),
    ]),
    "Sari": ("Sari", "sharp, sceptical young woman", [
        ("Courier", "Heart face with a pointed chin, slim, white tee under a denim vest, satchel strap, charcoal trousers, high ponytail.",
         dict(hip=.2, belly=.19, chest=.18, torso_h=.46, hem=.38, leg_r=.058, head_shape="heart", head_r=.33),
         dict(skin=SKIN["fair"], top="#F7F3EA", sleeve="short", bottom="#3B3836", shoes="#B5485D",
              overlays=[("vest", "#5E7FA3"), ("strap", "#7A4A2A")], brows=("#2A2420", 10), hair=("ponytail", "#2A2420"), props=[("bag", "#B5562F")])),
        ("Village Clerk", "Oval face, cream blouse with a navy collar, navy skirt, neat bob, square glasses, clipboard.",
         dict(hip=.21, belly=.2, chest=.18, torso_h=.46, hem=.36, head_shape="oval", head_r=.33),
         dict(skin=SKIN["light"], top="#F4E4C1", sleeve="long", bottom="#3E4C6E", dress=True, overlays=[("collar", "#3E4C6E")],
              hair=("bob", "#4A2E1C"), brows=("#4A2E1C", 6), props=[("glasses_square", "#3B3836"), ("clipboard", "#B08D57")])),
        ("Tinkerer", "Square face, mustard hoodie, olive cargo shorts, boots, pigtails, goggles on the forehead, red neckerchief.",
         dict(hip=.21, belly=.21, chest=.19, torso_h=.45, hem=.36, head_shape="square", head_r=.33),
         dict(skin=SKIN["brown"], top="#E0B04A", sleeve="long", bottom="#6B7F5B", legs="shorts", shoes="#4A2E1C", boots=True,
              mouth="Grin", hair=("pigtails", "#2A2420"), hat=("goggles", "#8EC5D6"), props=[("scarf", "#B5485D")])),
    ]),
    "Rigel": ("Rigel", "proud rival rice farmer (turns buff later)", [
        ("Rice Rival", "Wide-jaw triangle face, V-shaped build, white tank top, olive work trousers, spiky hair, towel round the neck, rake.",
         dict(hip=.2, belly=.22, chest=.27, shoulder_x=.15, arm_r=.07, hand_r=.075, torso_h=.5, hem=.36, head_shape="triangle", scale=1.06),
         dict(skin=SKIN["brown"], top="#F7F3EA", sleeve="none", bottom="#8A9A4B", mouth="Flat", brows=("#2A2420", 14),
              hair=("spiky", "#2A2420"), props=[("towel", "#E0B04A"), ("rake", "#8A6E4B")])),
        ("Proud Farmer", "Long face, tall and lean, rust striped long-sleeve shirt, jeans with a belt, boots, sage cap.",
         dict(hip=.2, belly=.21, chest=.21, torso_h=.52, hem=.4, head_shape="long", head_r=.33, scale=1.08),
         dict(skin=SKIN["tan"], top="#B5562F", sleeve="long", bottom="#3E4C6E", shoes="#4A2E1C", boots=True,
              overlays=[("stripes", "#E8C88A"), ("belt", "#4A2E1C")], brows=("#4A2E1C", 8), hair=("short", "#4A2E1C"), hat=("cap", "#81B29A"))),
        ("Field Strongman", "Round face, barrel-chested and broad, mustard tee, brown trousers and suspenders, top knot, short beard.",
         dict(hip=.25, belly=.28, chest=.29, shoulder_x=.15, arm_r=.075, hand_r=.078, head_shape="round", scale=1.04),
         dict(skin=SKIN["fair"], top="#E0B04A", sleeve="short", bottom="#7A4A2A", overlays=[("suspenders", "#7A4A2A")],
              brows=("#2A2420", 6), hair=("topbun", "#2A2420"), props=[("beard", "#2A2420")])),
    ]),
}


# ---------------------------------------------------------------- materials + mesh helpers
_mats = {}


def mat(color):
    """Flat material per sRGB hex colour (shared)."""
    if color not in _mats:
        lin = srgb_to_linear(tuple(int(color[i:i + 2], 16) / 255 for i in (1, 3, 5)))
        m = bpy.data.materials.new("NPC_" + color[1:])
        m.diffuse_color = (*lin, 1)
        bsdf = m.node_tree.nodes["Principled BSDF"]
        bsdf.inputs["Base Color"].default_value = (*lin, 1)
        bsdf.inputs["Roughness"].default_value = .9
        _mats[color] = m
    return _mats[color]


def face_material():
    if "NPC_Face" in _mats:
        return _mats["NPC_Face"]
    m = bpy.data.materials.new("NPC_Face")
    nt = m.node_tree
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(FACE_SHEET, check_existing=True)
    tex.extension = "CLIP"
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = .9
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
    m.surface_render_method = "DITHERED"
    _mats["NPC_Face"] = m
    return m


def to_object(name, bm, color_of_face, smooth=True):
    """bmesh -> object; color_of_face(face) returns a hex colour or a material."""
    keys = []
    for f in bm.faces:
        k = color_of_face(f)
        if k not in keys:
            keys.append(k)
        f.material_index = keys.index(k)
        f.smooth = smooth
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for k in keys:
        me.materials.append(mat(k) if isinstance(k, str) else k)
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    return o


def solid(name, bm, color, smooth=True):
    return to_object(name, bm, lambda f: color, smooth)


def place(bm, verts, loc, rot=None, scale=(1, 1, 1)):
    m = Matrix.Translation(loc) @ (rot.to_4x4() if rot is not None else Matrix.Identity(4)) @ Matrix.Diagonal((*scale, 1))
    bmesh.ops.transform(bm, matrix=m, verts=verts)


def sphere(bm, r, loc, scale=(1, 1, 1), rot=None, u=10, v=6):
    verts = bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)["verts"]
    place(bm, verts, loc, rot, scale)


def cyl(bm, r1, r2, depth, loc, rot=None, seg=10):
    verts = bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg, radius1=r1, radius2=r2, depth=depth)["verts"]
    place(bm, verts, loc, rot)


def box(bm, size, loc, rot=None):
    verts = bmesh.ops.create_cube(bm, size=1)["verts"]
    place(bm, verts, loc, rot, size)


def torus(bm, R, r, loc, rot=None, seg=16, mseg=6):
    ring = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        ring.append([bm.verts.new(((R + r * math.cos(b)) * math.cos(a), (R + r * math.cos(b)) * math.sin(a), r * math.sin(b)))
                     for b in (2 * math.pi * j / mseg for j in range(mseg))])
    for i in range(seg):
        for j in range(mseg):
            bm.faces.new((ring[i][j], ring[(i + 1) % seg][j], ring[(i + 1) % seg][(j + 1) % mseg], ring[i][(j + 1) % mseg]))
    place(bm, [v for row in ring for v in row], loc, rot)


def tube(bm, rings, x, y, seg=10):
    """Vertical tube through (z, radius) rings, capped."""
    loops = [[bm.verts.new((x + r * math.cos(2 * math.pi * i / seg), y + r * math.sin(2 * math.pi * i / seg), z)) for i in range(seg)]
             for z, r in rings]
    for a, b in zip(loops, loops[1:]):
        for i in range(seg):
            bm.faces.new((a[i], a[(i + 1) % seg], b[(i + 1) % seg], b[i]))
    bm.faces.new(list(reversed(loops[0])))
    bm.faces.new(loops[-1])


def facing(n):
    """Rotation whose Z axis points along n (for things lying on a surface)."""
    return n.to_track_quat("Z", "Y").to_matrix()


# ---------------------------------------------------------------- body
def build_body(P, L):
    hem, th, sx, al = P["hem"], P["torso_h"], P["shoulder_x"], P["arm_len"]
    hip = P["hip"] + (.03 if L.get("dress") else 0)  # skirts flare
    skirt_hem = hem - (.1 if L.get("dress") else 0)  # and hang down over the top of the legs
    sz = hem + th * .86
    joints = {"pelvis": ((0, 0, skirt_hem - .03), (hip, hip * P["depth"])), "belly": ((0, 0, hem + th * .42), (P["belly"], P["belly"] * P["depth"])),
              "chest": ((0, 0, hem + th * .78), (P["chest"], P["chest"] * P["depth"])), "neck": ((0, 0, hem + th + P["neck"] + .04), (P["neck_r"],) * 2)}
    for s, k in (("L", 1), ("R", -1)):
        joints["shoulder" + s] = ((k * sx, 0, sz), (P["arm_r"] * 1.3,) * 2)
        joints["elbow" + s] = ((k * (sx + al * .5), 0, sz), (P["arm_r"],) * 2)
        joints["wrist" + s] = ((k * (sx + al * .86), -.005, sz), (P["arm_r"] * .92,) * 2)
        joints["hand" + s] = ((k * (sx + al), -.01, sz), (P["hand_r"] * 1.05, P["hand_r"]))
    bones = [("pelvis", "belly"), ("belly", "chest"), ("chest", "neck")]
    for s in "LR":
        bones += [("chest", "shoulder" + s), ("shoulder" + s, "elbow" + s), ("elbow" + s, "wrist" + s), ("wrist" + s, "hand" + s)]
    names = list(joints)
    me = bpy.data.meshes.new("SkinSrc")
    me.from_pydata([joints[n][0] for n in names], [(names.index(a), names.index(b)) for a, b in bones], [])
    src = bpy.data.objects.new("SkinSrc", me)
    bpy.context.scene.collection.objects.link(src)
    skin = src.modifiers.new("Skin", "SKIN")
    skin.branch_smoothing = .6
    for i, n in enumerate(names):
        sv = me.skin_vertices[0].data[i]
        sv.radius = joints[n][1]
        sv.use_root = n == "pelvis"
    sub = src.modifiers.new("Sub", "SUBSURF")
    sub.levels = sub.render_levels = 1
    dg = bpy.context.evaluated_depsgraph_get()
    baked = bpy.data.meshes.new_from_object(src.evaluated_get(dg))
    bpy.data.objects.remove(src)
    bm = bmesh.new()
    bm.from_mesh(baked)
    bpy.data.meshes.remove(baked)
    for v in bm.verts:
        v.co.z = max(v.co.z, skirt_hem)

    segs = [(Vector(joints[a][0]), Vector(joints[b][0]), max(joints[a][1]), max(joints[b][1]), b) for a, b in bones]
    waist, chest_z, neck_base = hem + th * .42, hem + th * .78, hem + th

    def nearest(c):
        """(limb part, position along it) of the segment whose skin surface is closest."""
        best = None
        for a, b, ra, rb, nb in segs:
            ab = b - a
            t = max(0, min(1, (c - a).dot(ab) / ab.length_squared))
            d = (c - (a + ab * t)).length - (ra + (rb - ra) * t)
            if best is None or d < best[0]:
                best = (d, nb.rstrip("LR"), t)
        return best[1], best[2]

    def arm_weight(c):
        """0 = torso (stays), 1 = arm (swings); the shoulder blends so wide chests are not dragged along."""
        part, t = nearest(c)
        return {"shoulder": t * t, "elbow": 1, "wrist": 1, "hand": 1}.get(part, 0)

    # paint in T-pose (arms far from the body), then swing the arms down around the shoulders
    cols = [body_color(f.calc_center_median(), *nearest(f.calc_center_median()), L, waist, chest_z, neck_base) for f in bm.faces]
    B = dict(waist=waist, chest_z=chest_z, neck_base=neck_base, hip=hip, skirt_hem=skirt_hem, sz=sz, joints=joints, arm_weight=arm_weight)
    garments = build_garments(P, L, BVHTree.FromBMesh(bm), B)  # raised clothing layers, also built in T-pose
    rots = {1: Matrix.Rotation(math.radians(P["arm_down"]), 3, "Y"), -1: Matrix.Rotation(-math.radians(P["arm_down"]), 3, "Y")}

    def swing(co, u):
        if u <= 0:
            return co
        k = 1 if co.x > 0 else -1
        pivot = Vector((k * sx, 0, sz))
        return co.lerp(rots[k] @ (co - pivot) + pivot, u)

    for v in bm.verts:
        v.co = swing(v.co, arm_weight(v.co))
    for gbm, _, _ in garments:
        for v in gbm.verts:
            v.co = swing(v.co, arm_weight(v.co))
    it = iter(cols)
    parts = [to_object("Body", bm, lambda f: next(it))]
    parts += [solid("Garment", gbm, color, smooth) for gbm, color, smooth in garments]
    B["hands"] = {k: swing(Vector(joints["hand" + s][0]), 1) for s, k in (("L", 1), ("R", -1))}
    return parts, B


# ---------------------------------------------------------------- raised clothing (real geometry on top of the painted base)
def wrap_panel(bvh, th0, th1, z0, z1, offset, thickness, top=None, step_deg=15, step_z=.04):
    """Clothing panel wrapped round the body: rays from the vertical axis outward (theta 0 = front, -Y), lifted by
    'offset' along the surface normal and given 'thickness'. top(theta) can lower the upper edge (arm holes)."""
    nth, nz = max(2, round(abs(th1 - th0) / step_deg)), max(2, round((z1 - z0) / step_z))
    bm = bmesh.new()
    grid = []
    for j in range(nz + 1):
        row, radius = [], .2
        for i in range(nth + 1):
            th = math.radians(th0 + (th1 - th0) * i / nth)
            upper = z1 if top is None else min(z1, top(math.degrees(th)))
            z = z0 + (upper - z0) * j / nz
            d = Vector((math.sin(th), -math.cos(th), 0))
            loc, n, _, _ = bvh.ray_cast(Vector((0, 0, z)), d, .4)  # capped: in T-pose a sideways ray would run down the arm
            if loc is None:
                loc, n = Vector((0, 0, z)) + d * radius, d
            radius = (loc - Vector((0, 0, z))).length
            row.append(bm.verts.new(loc + n * offset))
        grid.append(row)
    for j in range(nz):
        for i in range(nth):
            f = bm.faces.new((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]))
            if f.calc_center_median().xy.dot(f.normal.xy) < 0:  # face outward so the thickness goes inward
                f.normal_flip()
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=thickness)
    return bm


def front_panel(bvh, x, z, w, h, offset, thickness, steps=4):
    """Small panel (pocket, patch) projected onto the body from the front."""
    bm = bmesh.new()
    grid = []
    for j in range(steps + 1):
        row = []
        for i in range(steps + 1):
            o = Vector((x + (i / steps - .5) * w, -3, z + (j / steps - .5) * h))
            loc, n, _, _ = bvh.ray_cast(o, Vector((0, 1, 0)))
            row.append(bm.verts.new(loc + n * offset))
        grid.append(row)
    for j in range(steps):
        for i in range(steps):
            f = bm.faces.new((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]))
            if f.normal.y > 0:
                f.normal_flip()
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=thickness)
    return bm


def surface_front(bvh, x, z):
    loc, n, _, _ = bvh.ray_cast(Vector((x, -3, z)), Vector((0, 1, 0)))
    return loc, n


def limb_ring(B, P, part, t, R_extra, minor):
    """Ring round an arm in T-pose (cuff, wristband) at position t along shoulder->elbow ('sleeve') or elbow->wrist ('wrist')."""
    bm = bmesh.new()
    a, b = ("shoulder", "elbow") if part == "sleeve" else ("elbow", "wrist")
    radius = P["arm_r"] * (1.15 if part == "sleeve" else .95) + R_extra
    for s in "LR":
        pa, pb = Vector(B["joints"][a + s][0]), Vector(B["joints"][b + s][0])
        torus(bm, radius, minor, pa.lerp(pb, t), Matrix.Rotation(math.radians(90), 3, "Y"), seg=14, mseg=6)
    return bm


GARMENT_LAYERS = {"vest", "apron", "shawl", "belt", "collar"}  # painted overlays that also get raised geometry


def build_garments(P, L, bvh, B):
    """[(bmesh, colour, smooth)] of clothing that sticks out of the body: the garment overlays plus L['details']."""
    waist, chest_z, neck_base = B["waist"], B["chest_z"], B["neck_base"]

    def Z(v):
        return B[v[0]] + v[1] if isinstance(v, tuple) else v
    out = []
    for kind, color in L.get("overlays", []):
        if kind not in GARMENT_LAYERS:
            continue
        if kind == "vest":  # two fronts that leave the blouse showing, lower at the sides for the arm holes
            low = waist - (.08 if L.get("dress") else .03)
            top = lambda th: chest_z - .03 if 55 < abs(th) < 125 else neck_base - .01
            for th0, th1 in ((14, 180), (-180, -14)):
                out.append((wrap_panel(bvh, th0, th1, low, neck_base - .01, .006, .02, top), color, True))
        elif kind == "apron":
            out.append((wrap_panel(bvh, -50, 50, B["skirt_hem"] + .04, chest_z + .02, .008, .012), color, True))
            out.append((wrap_panel(bvh, -180, 180, waist - .012, waist + .012, .02, .01), color, True))  # waist tie
        elif kind == "shawl":
            sides = lambda th: chest_z - .02 if 60 < abs(th) < 120 else neck_base + .005  # stays clear of the arm roots
            out.append((wrap_panel(bvh, -180, 180, chest_z - .08, neck_base + .005, .008, .03, sides), color, True))
            loc, n = surface_front(bvh, 0, chest_z - .06)
            bm = bmesh.new()
            sphere(bm, .045, loc + n * .04, (1, .7, 1), u=10, v=6)  # knot
            out.append((bm, color, True))
        elif kind == "belt":
            out.append((wrap_panel(bvh, -180, 180, waist - .022, waist + .022, .004, .016), color, True))
            loc, n = surface_front(bvh, 0, waist)
            bm = bmesh.new()
            box(bm, (.07, .02, .055), loc + n * .022, facing(n) @ Matrix.Rotation(math.radians(90), 3, "X"))
            out.append((bm, "#C9A45C", False))  # brass buckle
        elif kind == "collar":
            out.append((wrap_panel(bvh, -70, 70, neck_base - .045, neck_base + .01, .006, .012), color, True))
    if L.get("wrist"):
        out.append((limb_ring(B, P, "wrist", .7, .006, .016), L["wrist"], True))
    for d in L.get("details", []):
        kind, color = d[0], d[1]
        if kind == "pocket":  # (x, z, w, h, lift) - lift > 0 sits it on a raised layer (cardigan)
            x, z, w, h, lift = d[2:]
            out.append((front_panel(bvh, x, Z(z), w, h, .006 + lift, .012), color, False))
        elif kind == "buttons":  # (x, z_low, z_high, count, lift)
            x, z0, z1, count, lift = d[2:]
            z0, z1 = Z(z0), Z(z1)
            bm = bmesh.new()
            for i in range(count):
                loc, n = surface_front(bvh, x, z0 + (z1 - z0) * i / max(1, count - 1))
                sphere(bm, .014, loc + n * (.008 + lift), (1, 1, .5), facing(n), u=8, v=4)
            out.append((bm, color, True))
        elif kind == "cuff":  # ("sleeve" | "wrist")
            part = d[2]
            out.append((limb_ring(B, P, part, .55 if part == "sleeve" else .82, .004, .014), color, True))
        elif kind == "hood":  # bunched hood round the back of the neck
            out.append((wrap_panel(bvh, 115, 245, neck_base - .01, neck_base + .08, .015, .04), color, True))
        elif kind == "drawstrings":
            bm = bmesh.new()
            for k in (1, -1):
                loc, n = surface_front(bvh, k * .035, neck_base - .06)
                cyl(bm, .006, .006, .1, loc + n * .012, seg=6)
                sphere(bm, .011, loc + n * .012 + Vector((0, 0, -.05)), u=6, v=4)
            out.append((bm, color, True))
        elif kind == "hemfrill":  # ruffle round the skirt hem
            out.append((wrap_panel(bvh, -180, 180, B["skirt_hem"] + .02, B["skirt_hem"] + .055, .012, .014), color, True))
        elif kind not in LEG_DETAILS:
            raise ValueError(kind)
    return out


LEG_DETAILS = {"legcuff", "legpocket"}


def body_color(c, part, t, L, waist, chest_z, neck_base):
    skin, top = L["skin"], L["top"]
    sleeve, sleeve_col = L.get("sleeve", "short"), L.get("sleeve_color", L["top"])
    if part == "hand":
        return skin
    if part == "wrist":
        if sleeve == "long" and t < .8:
            return sleeve_col
        return L["wrist"] if L.get("wrist") and t > .55 else skin
    if part == "elbow":
        return sleeve_col if sleeve == "long" or (sleeve == "short" and t < .55) else skin
    if part == "neck":
        return skin if c.z > neck_base + .015 else top
    if part == "shoulder":
        col = skin if sleeve == "none" and t > .2 else (sleeve_col if t > .6 else top)
        if t > .3:
            return col  # garment overlays stay on the torso: in T-pose the arms sit at chest height
    else:
        col = top if c.z > waist else L["bottom"]
    for kind, color in L.get("overlays", []):
        if overlay_hit(kind, c, part, waist, chest_z, neck_base):
            col = color
    return col


def overlay_hit(kind, c, part, waist, chest_z, neck_base):
    on_torso = part != "shoulder"
    if kind == "vest":
        return c.z > waist - .02 and not (c.y < 0 and abs(c.x) < .035) and (on_torso or abs(c.x) < .2)
    if kind == "apron":
        return on_torso and c.y < -.02 and abs(c.x) < .16 and c.z < chest_z + .02
    if kind == "shawl":
        return c.z > chest_z - .07
    if kind == "stripes":
        return c.z > waist and int(c.z / .07) % 2 == 0
    if kind == "belt":
        return on_torso and abs(c.z - waist) < .022
    if kind == "collar":
        return on_torso and c.z > neck_base - .05 and abs(c.x) < .11 and c.y < 0
    if kind == "strap":
        return on_torso and c.z > waist and abs(c.x * .72 + (c.z - (waist + chest_z) / 2) * .7) < .028
    if kind == "suspenders":
        return on_torso and c.z > waist and abs(abs(c.x) - .07) < .02
    raise ValueError(kind)


def build_legs(P, L, hem):
    knee = hem * .5
    bm = bmesh.new()
    for k in (1, -1):
        r = P["leg_r"]
        tube(bm, [(.03, r * .95), (.1, r * .97), (.135, r), (knee, r), (knee + .02, r * 1.02), (hem + .05, r * 1.05)], k * P["leg_x"], 0)
    kind, skin = L.get("legs", "skirt" if L.get("dress") else "pants"), L["skin"]

    def color_of(f):
        z = f.calc_center_median().z
        if L.get("boots") and z < .135:
            return L.get("shoes", "#4A2E1C")
        if kind == "pants":
            return L["bottom"]
        if kind == "shorts" and z > knee + .01:
            return L["bottom"]
        if L.get("socks") and z < knee - .02:
            return L["socks"]
        return L.get("tights", skin)
    legs = to_object("Legs", bm, color_of)
    bm = bmesh.new()
    for k in (1, -1):
        sphere(bm, 1, Vector((k * P["leg_x"], -.03, P["shoe"][2] * .9)), P["shoe"], u=12, v=8)
    out = [legs, solid("Shoes", bm, L.get("shoes", "#7A4A2A"))]
    rings = [(z, color) for kind, color, z in (d for d in L.get("details", []) if d[0] == "legcuff")]
    if L.get("boots"):
        rings.append((.135, L.get("shoes", "#4A2E1C")))
    for z, color in rings:
        bm = bmesh.new()
        for k in (1, -1):
            torus(bm, P["leg_r"] + .008, .018, Vector((k * P["leg_x"], 0, z)), seg=14, mseg=6)
        out.append(solid("Cuff", bm, color))
    for kind, color, z in (d for d in L.get("details", []) if d[0] == "legpocket"):
        bm = bmesh.new()
        for k in (1, -1):
            box(bm, (.025, .085, .09), Vector((k * (P["leg_x"] + P["leg_r"] + .008), 0, z)))
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=.006, segments=1, affect="EDGES")
        out.append(solid("CargoPocket", bm, color, smooth=False))
    return out


# ---------------------------------------------------------------- head
SHAPES = {  # width profile over height (-1 chin .. 1 crown), height ratio, squareness
    "round": (lambda z: 1, .95, 0), "oval": (lambda z: .86, 1.18, 0), "long": (lambda z: .78, 1.3, .3),
    "square": (lambda z: 1, .96, .65), "triangle": (lambda z: 1 - .3 * z, 1.0, .2),
    "heart": (lambda z: (1 + .16 * z) * (1 - .75 * max(0, -z - .3)), 1.06, 0), "diamond": (lambda z: 1.14 - .42 * z * z, 1.1, 0),
}


def unit_sphere(u=18, v=12):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=1)
    return bm


def shape_head(bm, H, inflate=0.0):
    w, hz, sq = SHAPES[H["shape"]]
    for vert in bm.verts:
        d = vert.co.normalized()
        if sq:
            d = d.lerp(d / max(abs(d.x), abs(d.y), abs(d.z)) * .85, sq)
        k = w(d.z)
        vert.co = Vector((d.x * (H["r"] * k + inflate), d.y * (H["r"] * .92 + inflate), d.z * (H["r"] * hz + inflate))) + H["c"]


def head_info(P, neck_base):
    hz = SHAPES[P["head_shape"]][1]
    top_of_neck = neck_base + P["neck"] + .04
    return dict(shape=P["head_shape"], r=P["head_r"], hz=hz, c=Vector((0, 0, top_of_neck + P["head_r"] * hz * .78)))


def head_width(H, nz):
    """Head half-width at unit height nz (-1..1)."""
    return H["r"] * SHAPES[H["shape"]][0](nz) * math.sqrt(max(0, 1 - nz * nz))


def build_head(H, skin):
    bm = unit_sphere()
    shape_head(bm, H)
    bvh = BVHTree.FromBMesh(bm)
    return solid("Head", bm, skin), bvh


def surface(bvh, x, z):
    loc, n, _, _ = bvh.ray_cast(Vector((x, -3, z)), Vector((0, 1, 0)))
    return loc, n


def face_quad(name, bvh, x, z, size, cell, mirror, push=.004, grid=4):
    col, row = cell % 4, cell // 4
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    verts = []
    for j in range(grid + 1):
        for i in range(grid + 1):
            loc, n = surface(bvh, x + (i / grid - .5) * size, z + (j / grid - .5) * size)
            verts.append(bm.verts.new(loc + n * push))
    for j in range(grid):
        for i in range(grid):
            a = j * (grid + 1) + i
            f = bm.faces.new((verts[a], verts[a + 1], verts[a + grid + 2], verts[a + grid + 1]))
            for loop in f.loops:
                idx = verts.index(loop.vert)
                u, v = (idx % (grid + 1)) / grid, (idx // (grid + 1)) / grid
                loop[uv].uv = ((col + ((1 - u) if mirror else u)) * .25, 1 - (row + 1) * .25 + v * .25)
    return to_object(name, bm, lambda f: face_material())


def build_face(P, L, H, bvh):
    r, hz, c = H["r"], H["hz"], H["c"]
    ex, ez, es = r * P["eye_sp"], c.z + r * hz * P["eye_h"], r * P["eye_size"]
    eye = EYES.index(L.get("eyes", "Open"))
    mouth_z = c.z - r * hz * .36
    parts = [face_quad("EyeR", bvh, -ex, ez, es, eye, False), face_quad("EyeL", bvh, ex, ez, es, eye, True),
             face_quad("Mouth", bvh, 0, mouth_z, r * .45, 8 + MOUTHS.index(L.get("mouth", "Smile")), False, push=.04 if has(L, "beard") else .004)]
    bm = bmesh.new()
    for k in (1, -1):
        loc, n = surface(bvh, k * r * .58, c.z - r * hz * .28)
        sphere(bm, 1, loc + n * .002, (r * .14, r * .085, .006), facing(n), u=8, v=4)
    parts.append(solid("Blush", bm, "#F08A80"))
    if L.get("brows"):
        color, tilt = L["brows"]
        bm = bmesh.new()
        for k in (1, -1):
            loc, n = surface(bvh, k * ex, ez + es * .42)
            spin = Matrix.Rotation(math.radians(-tilt * k), 3, "Z")  # positive tilt = inner ends down (stern)
            sphere(bm, 1, loc + n * .006, (es * .26, es * .05, .008), facing(n) @ spin, u=8, v=4)
        parts.append(solid("Brows", bm, color))
    return parts, dict(eye_x=ex, eye_z=ez, eye_size=es, mouth_z=mouth_z)


# ---------------------------------------------------------------- hair + hats
def shell(H, keep, inflate, thickness):
    """Part of a slightly inflated head shell: keep(unit_direction) decides per face; given thickness so it never z-fights."""
    bm = unit_sphere(28, 18)  # finer than the head so hair lines and hat edges read clean
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if not keep(f.calc_center_median().normalized())], context="FACES")
    shape_head(bm, H, inflate)
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=-thickness)
    return bm


def face_hole(n, cut=.22):
    return n.y < -.3 and n.z < cut and abs(n.x) < .8


HAIR_KEEP = {
    "short": lambda n: not (face_hole(n) or n.z < -.35 or (n.z < -.05 and n.y < .3)),
    "bob": lambda n: not (face_hole(n, .25) or n.z < -.62 or (n.z < -.05 and n.y < -.25)),
    "bowl": lambda n: not ((n.y < -.2 and n.z < .32) or n.z < -.2),
}


def build_hair(H, style, color):
    r, hz, c = H["r"], H["hz"], H["c"]
    base = {"bun": "short", "topbun": "short", "ponytail": "short", "pigtails": "bob", "spiky": "short", "curly": "short"}.get(style, style)
    top = c + Vector((0, 0, r * hz))
    if style == "bald":  # soft tufts above the ears and round the back (a shell band read as earmuffs)
        bm = shell(H, lambda n: -.5 < n.z < .25 and n.y > -.05, .012, .02)  # crescent round the back, curved with the skull
        for k in (1, -1):
            sphere(bm, 1, c + Vector((k * (head_width(H, -.05) - r * .04), r * .12, -r * hz * .05)), (r * .09, r * .26, r * .2), u=12, v=8)
        return [solid("Hair", bm, color)]
    bm = shell(H, HAIR_KEEP[base], .018, .022)
    if style == "bun":
        sphere(bm, r * .33, c + Vector((0, r * .55, r * hz * .78)), u=12, v=8)
    elif style == "topbun":
        sphere(bm, r * .26, top + Vector((0, 0, r * .12)), u=12, v=8)
    elif style == "ponytail":
        sphere(bm, r * .2, c + Vector((0, r * .88, r * hz * .45)), u=10, v=6)
        sphere(bm, 1, c + Vector((0, r * 1.02, -r * hz * .05)), (r * .15, r * .15, r * .5), u=10, v=6)
    elif style == "pigtails":
        for k in (1, -1):
            sphere(bm, r * .27, c + Vector((k * (head_width(H, -.15) + r * .1), r * .15, -r * hz * .15)), u=10, v=6)
    elif style == "spiky":
        for i in range(7):
            a = 2 * math.pi * i / 7
            d = Vector((math.cos(a) * .55, math.sin(a) * .55 + .1, .83)).normalized()
            base_pt = c + Vector((d.x * head_width(H, d.z) / max(1e-3, math.sqrt(1 - d.z * d.z)), d.y * r * .92, d.z * r * hz))
            cyl(bm, r * .16, 0, r * .45, base_pt + d * r * .12, facing(d), seg=6)
    elif style == "curly":
        for i in range(14):
            a = 2 * math.pi * i / 14
            nz = .55 if i % 2 else .25
            d = Vector((math.cos(a), math.sin(a) * .92, 0)) * math.sqrt(1 - nz * nz) + Vector((0, 0, nz))
            if d.y < -.3 and nz < .5:
                continue  # keep the face clear
            sphere(bm, r * .2, c + Vector((d.x * r * SHAPES[H["shape"]][0](nz), d.y * r * .92, d.z * r * hz)) * 1 + d * r * .05, u=8, v=6)
        sphere(bm, r * .24, top, u=8, v=6)
    return [solid("Hair", bm, color)]


def build_hat(H, kind, color):
    r, hz, c = H["r"], H["hz"], H["c"]
    front_y = -r * .92
    bm = bmesh.new()
    if kind in ("cap", "cap_back"):
        bm = shell(H, lambda n: n.z > .18, .04, .018)
        side = -1 if kind == "cap" else 1
        box(bm, (r * 1.15, r * .7, .025), Vector((0, side * (r * .92 + r * .25), c.z + r * hz * .3)), Matrix.Rotation(math.radians(8 * -side), 3, "X"))
        sphere(bm, r * .08, c + Vector((0, 0, r * hz + .03)), u=8, v=4)
    elif kind == "flatcap":
        bm = shell(H, lambda n: n.z > .25, .05, .02)
        for v in bm.verts:
            v.co.y -= .02 * max(0, (v.co.z - c.z) / (r * hz))  # leans forward over the brow
        box(bm, (r * 1.1, r * .45, .025), Vector((0, front_y - r * .12, c.z + r * hz * .33)), Matrix.Rotation(math.radians(-14), 3, "X"))
    elif kind == "beanie":
        bm = shell(H, lambda n: n.z > .3 or (n.y > .1 and n.z > -.15), .035, .03)
        nz = .3
        torus(bm, head_width(H, nz) + .035, .035, c + Vector((0, 0, r * hz * nz)), seg=18)
        sphere(bm, r * .17, c + Vector((0, 0, r * hz + .05)), u=10, v=6)
    elif kind == "headband":
        nz = .42
        torus(bm, head_width(H, nz) + .022, .022, c + Vector((0, 0, r * hz * nz)), seg=20)
    elif kind == "goggles":
        nz = .45
        torus(bm, head_width(H, nz) + .02, .018, c + Vector((0, 0, r * hz * nz)), seg=20)
        for k in (1, -1):
            d = Vector((k * .38, -.92, nz)).normalized()
            cyl(bm, r * .17, r * .17, .05, c + Vector((d.x * r, d.y * r, nz * r * hz)) + d * .02, facing(d), seg=12)
        bm2 = bmesh.new()
        for k in (1, -1):
            d = Vector((k * .38, -.92, nz)).normalized()
            cyl(bm2, r * .13, r * .13, .01, c + Vector((d.x * r, d.y * r, nz * r * hz)) + d * .05, facing(d), seg=12)
        return [solid("Hat", bm, "#4A2E1C"), solid("Lenses", bm2, color)]
    elif kind == "flower":
        center = c + Vector((head_width(H, .45) * .95, -r * .25, r * hz * .5))
        for i in range(5):
            a = 2 * math.pi * i / 5
            sphere(bm, r * .085, center + Vector((0, math.cos(a), math.sin(a))) * r * .1, u=8, v=4)
        bm2 = bmesh.new()
        sphere(bm2, r * .06, center + Vector((.01, 0, 0)), u=8, v=4)
        return [solid("Hat", bm, color), solid("FlowerCenter", bm2, "#E0B04A")]
    elif kind == "bow":
        center = c + Vector((head_width(H, .55) * .8, r * .1, r * hz * .62))
        for k in (1, -1):
            cyl(bm, r * .14, r * .03, r * .28, center + Vector((0, k * r * .14, 0)), Matrix.Rotation(math.radians(90 * k), 3, "X"), seg=8)
        sphere(bm, r * .06, center, u=8, v=4)
    return [solid("Hat", bm, color)]


# ---------------------------------------------------------------- props
def has(L, name):
    return any(p[0] == name for p in L.get("props", []))


def build_props(P, L, H, F, B, bvh):
    r, hz, c = H["r"], H["hz"], H["c"]
    hands, out = B["hands"], []
    for kind, color in L.get("props", []):
        bm = bmesh.new()
        if kind in ("glasses", "glasses_square"):
            seg = 4 if kind == "glasses_square" else 16
            for k in (1, -1):
                loc, n = surface(bvh, k * F["eye_x"], F["eye_z"])
                torus(bm, F["eye_size"] * .4, .008, loc + n * .02, facing(n) @ Matrix.Rotation(math.radians(45 if seg == 4 else 0), 3, "Z"), seg=seg, mseg=4)
            a, _ = surface(bvh, F["eye_x"] * .55, F["eye_z"])
            cyl(bm, .006, .006, F["eye_x"] * 1.1, Vector((0, a.y - .02, F["eye_z"])), Matrix.Rotation(math.radians(90), 3, "Y"), seg=6)
        elif kind == "mustache":
            for k in (1, -1):
                loc, n = surface(bvh, k * r * .13, F["mouth_z"] + r * hz * .13)
                sphere(bm, 1, loc + n * .01, (r * .16, r * .06, .025), facing(n) @ Matrix.Rotation(math.radians(-12 * k), 3, "Z"), u=10, v=6)
        elif kind == "beard":
            # rounded chin + jaw under the mouth (a cut shell read as a mask)
            loc, n = surface(bvh, 0, c.z - r * hz * .6)
            sphere(bm, 1, loc + Vector((0, r * .2, -r * .05)), (r * .62, r * .45, r * .36), u=14, v=8)
            for k in (1, -1):
                loc, n = surface(bvh, k * r * .62, c.z - r * hz * .3)
                sphere(bm, 1, loc + Vector((0, r * .18, 0)), (r * .16, r * .3, r * .32), u=10, v=6)
        elif kind == "plaster":
            loc, n = surface(bvh, -r * .5, c.z - r * hz * .12)
            box(bm, (r * .22, r * .09, .01), loc + n * .006, facing(n) @ Matrix.Rotation(math.radians(20), 3, "Z"))
        elif kind == "necklace":
            R = P["neck_r"] + .04
            for i in range(16):
                a = 2 * math.pi * i / 16
                sphere(bm, .016, Vector((math.cos(a) * R * 1.25, math.sin(a) * R, B["neck_base"] - .03 - .03 * max(0, -math.sin(a)))), u=6, v=4)
        elif kind in ("scarf", "towel"):
            thick = .045 if kind == "scarf" else .03
            torus(bm, P["neck_r"] + thick * .9, thick, Vector((0, 0, B["neck_base"] + .02)), seg=14)
            for k in ((1,) if kind == "scarf" else ()):
                sphere(bm, 1, Vector((k * .08, -P["chest"] * P["depth"] - .01, B["neck_base"] - .1)), (.045, .025, .1), u=8, v=6)
        elif kind == "hood":
            torus(bm, P["neck_r"] + .06, .055, Vector((0, .05, B["neck_base"] + .02)), Matrix.Rotation(math.radians(-25), 3, "X"), seg=14)
        elif kind == "backpack":
            box(bm, (P["chest"] * 1.5, .12, P["torso_h"] * .62), Vector((0, P["chest"] * P["depth"] + .05, B["chest_z"] - .04)))
            bmesh.ops.bevel(bm, geom=list(bm.edges), offset=.03, segments=2, affect="EDGES")
        elif kind == "bag":
            box(bm, (.06, .2, .16), Vector((-B["hip"] - .03, 0, B["waist"] - .03)))
            bmesh.ops.bevel(bm, geom=list(bm.edges), offset=.015, segments=2, affect="EDGES")
        elif kind == "hammer":
            x = B["hip"] + .03
            cyl(bm, .014, .014, .22, Vector((x, -.03, B["waist"] - .05)))
            box(bm, (.035, .1, .04), Vector((x, -.03, B["waist"] + .07)))
        elif kind == "basket":
            h = hands[-1] + Vector((0, -.02, -.1))
            cyl(bm, .1, .08, .1, h, seg=12)
            torus(bm, .085, .01, h + Vector((0, 0, .06)), Matrix.Rotation(math.radians(90), 3, "Y"), seg=12, mseg=4)
            bm2 = bmesh.new()
            for i, dx in enumerate((-.04, .03, 0)):
                sphere(bm2, .035, h + Vector((dx, (i - 1) * .03, .06)), u=8, v=4)
            out.append(solid("Blooms", bm2, "#E8A0A8"))
        elif kind in ("cane", "staff"):
            h = hands[-1] + Vector((0, -.04, 0))
            length = h.z + (.25 if kind == "staff" else .02)
            cyl(bm, .016, .016, length, Vector((h.x, h.y, length / 2)), seg=8)
            sphere(bm, .028, Vector((h.x, h.y, length)), u=8, v=4)
        elif kind == "rake":
            h = hands[-1] + Vector((0, -.04, 0))
            cyl(bm, .016, .016, 1.35, Vector((h.x, h.y, .67)), seg=8)
            box(bm, (.3, .03, .03), Vector((h.x, h.y, 1.35)))
            for i in range(5):
                cyl(bm, .006, .006, .07, Vector((h.x - .12 + i * .06, h.y, 1.31)), seg=4)
        elif kind in ("clipboard", "notebook"):
            h = hands[1] + Vector((0, -.06, .02))
            size = (.16, .015, .21) if kind == "clipboard" else (.1, .02, .13)
            box(bm, size, h, Matrix.Rotation(math.radians(-12), 3, "X"))
        else:
            raise ValueError(kind)
        out.append(solid(kind.title(), bm, color, smooth=kind not in ("clipboard", "notebook", "bag", "hammer", "plaster")))
    return out


# ---------------------------------------------------------------- one villager
HAND_PROPS = {"basket", "cane", "staff", "rake", "clipboard", "notebook"}
# which bone each part follows when rigged (object base name -> group); Body/Garment are weighted per vertex
PART_GROUPS = {"Head": "Head", "EyeR": "Head", "EyeL": "Head", "Mouth": "Head", "Blush": "Head", "Brows": "Head", "Hair": "Head",
               "Hat": "Head", "Lenses": "Head", "FlowerCenter": "Head", "Glasses": "Head", "Glasses_Square": "Head", "Mustache": "Head",
               "Beard": "Head", "Plaster": "Head", "Legs": "Legs", "Shoes": "Legs", "Cuff": "Legs", "CargoPocket": "Legs",
               "Basket": "HandR", "Blooms": "HandR", "Cane": "HandR", "Staff": "HandR", "Rake": "HandR", "Clipboard": "HandL", "Notebook": "HandL",
               "Body": "Body", "Garment": "Body", "Necklace": "Torso", "Scarf": "Torso", "Towel": "Torso", "Hood": "Torso", "Backpack": "Torso",
               "Bag": "Torso", "Hammer": "Torso"}


def build_villager(name, P, L, tpose=False, keep_hand_props=False):
    P = dict(BASE, **P)
    if tpose:
        P["arm_down"] = 0
        if not keep_hand_props:
            L = dict(L, props=[p for p in L.get("props", []) if p[0] not in HAND_PROPS])
    body_parts, B = build_body(P, L)
    parts = [*body_parts, *build_legs(P, L, P["hem"])]
    H = head_info(P, B["neck_base"])
    head, bvh = build_head(H, L["skin"])
    face, F = build_face(P, L, H, bvh)
    parts += [head, *face]
    if L.get("hair"):
        parts += build_hair(H, *L["hair"])
    if L.get("hat"):
        parts += build_hat(H, *L["hat"])
    parts += build_props(P, L, H, F, B, bvh)
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in parts:
        o.select_set(True)
        o.vertex_groups.new(name="Part_" + PART_GROUPS[o.name.split(".")[0]]).add(range(len(o.data.vertices)), 1, "REPLACE")
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = obj.data.name = name
    obj.data.uv_layers[0].active_render = True
    obj.scale = (P["scale"],) * 3
    head_top = (H["c"].z + H["r"] * H["hz"]) * P["scale"]
    return obj, dict(head_z=H["c"].z * P["scale"], head_r=H["r"] * P["scale"], top=head_top, P=P, B=B,
                     tris=sum(len(p.vertices) - 2 for p in obj.data.polygons))


# ---------------------------------------------------------------- scene + renders
def meadow_reference():
    with bpy.data.libraries.load(MEADOW_BLEND) as (src, dst):
        dst.objects = ["MC_Meadow"]
    o = dst.objects[0]
    bpy.context.scene.collection.objects.link(o)
    o.parent = None
    for m in list(o.modifiers):
        o.modifiers.remove(m)
    return o


def setup_scene():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.view_settings.view_transform = "Standard"
    scene.eevee.taa_render_samples = 32
    img = scene.render.image_settings
    if hasattr(img, "media_type"):
        img.media_type = "IMAGE"
    img.file_format = "PNG"
    scene.render.use_file_extension = True
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (.55, .62, .7, 1)
    bg.inputs["Strength"].default_value = .55
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
    scene.collection.objects.link(sun)
    sun.data.energy = 2.6
    sun.rotation_euler = (math.radians(50), 0, math.radians(25))
    ground = bpy.data.objects.new("Ground", bpy.data.meshes.new("Ground"))
    scene.collection.objects.link(ground)
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=60)
    bm.to_mesh(ground.data)
    bm.free()
    ground.data.materials.append(mat("#7CB342"))
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    scene.collection.objects.link(cam)
    cam.data.type = "ORTHO"
    scene.camera = cam
    return cam


def shot(cam, path, target, az, el, ortho, res):
    scene = bpy.context.scene
    d = Vector((math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)))
    cam.location = Vector(target) + d * 20
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = ortho
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def main():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    for block in (bpy.data.meshes, bpy.data.materials):
        for item in list(block):
            if item.users == 0:
                block.remove(item)
    _mats.clear()
    os.makedirs(OUT, exist_ok=True)
    cam = setup_scene()
    meadow = meadow_reference()
    rows, specs = {}, {}
    for row, (npc, (display, persona, variants)) in enumerate(NPCS.items()):
        y = row * 5.0
        rows[npc] = []
        specs[npc] = {"name": display, "persona": persona, "variants": []}
        for i, (title, blurb, P, L) in enumerate(variants):
            obj, info = build_villager(f"{npc}_{'ABC'[i]}", P, L)
            obj.location = (i * 1.25, y, 0)
            info.update(x=i * 1.25, y=y)
            rows[npc].append((obj, info))
            specs[npc]["variants"].append({"letter": "ABC"[i], "title": title, "blurb": blurb, "tris": info["tris"],
                                           "height": round(info["top"], 2)})

    everything = [o for r in rows.values() for o, _ in r]
    for npc, row in rows.items():
        y = row[0][1]["y"]
        meadow.location = (-1.35, y, 0)
        for o in everything:
            o.hide_render = o not in [r[0] for r in row]
        shot(cam, os.path.join(OUT, f"{npc}_body.png"), (.55, y, .85), .32, .12, 5.0, (1500, 820))
        for obj, info in row:
            shot(cam, os.path.join(OUT, f"{obj.name}_face.png"), (info["x"], y, info["head_z"]), .18, .05, info["head_r"] * 3.6, (420, 420))
    for o in everything:
        o.hide_render = False
    with open(os.path.join(OUT, "specs.json"), "w", encoding="utf-8") as fh:
        json.dump(specs, fh, indent=1)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(REPO, "Tools", "Blender", "NPC_Concepts.blend"))
    return {npc: [(v["title"], v["tris"], v["height"]) for v in s["variants"]] for npc, s in specs.items()}


if __name__ == "mc":
    result = main()
