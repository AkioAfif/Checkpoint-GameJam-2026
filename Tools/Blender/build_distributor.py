# Builds "The Distributor" (Level01 antagonist): one skinned mesh, Chibi-compatible armature, face shape keys,
# baked head normal + base map. Re-runnable, headless:
#   "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup -P Tools/Blender/build_distributor.py [-- <previewDir>]
# Units = Unity rig space at scale 1 (about 1.6 m tall; the NPC prefab scales it x1.3). Blender Z up, character faces -Y,
# character's left = +X.
import math
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Vector, noise
from mathutils.bvhtree import BVHTree

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(REPO, "Assets", "_Project", "Art", "Models", "Distributor")
BLEND_PATH = os.path.join(REPO, "Tools", "Blender", "Distributor.blend")
PREVIEW_DIR = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else None
TEX_SIZE = 1024
MAX_TRIS = 6000

HEAD_C = Vector((0.0, 0.0, 0.98))
DZ = HEAD_C.z - 1.06  # feature heights below are authored for a head centre at 1.06
HEAD_R = Vector((0.52, 0.50, 0.56))
LENS_C = (0.20, 1.13 + DZ)
LENS_AB = (0.15, 0.10)

BONES = ["Hips", "Head", "ArmL", "ArmR", "LegL", "LegR"]
# material slots, in order; Unity remaps them by name
SLOTS = [("SkinHead", (.80, .62, .46)), ("Skin", (.80, .62, .46)), ("Hair", (.10, .09, .10)), ("Frame", (.12, .11, .12)),
         ("Lens", (.96, .96, .94)), ("Lips", (.80, .47, .45)), ("Cloth", (.80, .66, .36)), ("Dark", (.36, .36, .40)), ("Accent", (.98, .80, .42))]
SKINHEAD, SKIN, HAIR, FRAME, LENS, LIPS, CLOTH, DARK, ACCENT = range(len(SLOTS))
# per-vertex part tags (used for weights / shape keys, removed before export)
P_SKIN, P_HAIR, P_FRAME, P_LENS, P_STACHE, P_BODY = range(6)


# ---------- math helpers ----------

def clamp01(x):
    return max(0.0, min(1.0, x))


def smoothstep(e0, e1, x):
    t = clamp01((x - e0) / (e1 - e0))
    return t * t * (3 - 2 * t)


def g2(dx, dy, sx, sy):
    return math.exp(-(dx / sx) ** 2 - (dy / sy) ** 2)


def azel(d):
    return math.atan2(d.x, -d.y), math.asin(max(-1.0, min(1.0, d.z)))


def dir_from(az, el):
    return Vector((math.cos(el) * math.sin(az), -math.cos(el) * math.cos(az), math.sin(el)))


def lin(c):
    return tuple(((v + .055) / 1.055) ** 2.4 if v > .04045 else v / 12.92 for v in c)


# ---------- head shape (unit direction → offset from HEAD_C) ----------

def head_shape(d):
    az, el = azel(d)
    p = Vector((d.x * HEAD_R.x, d.y * HEAD_R.y, d.z * HEAD_R.z))
    low = smoothstep(0.0, -0.9, d.z)
    p.x *= 1 + .16 * low
    p.y *= 1 + .10 * low
    n = Vector((p.x / HEAD_R.x ** 2, p.y / HEAD_R.y ** 2, p.z / HEAD_R.z ** 2)).normalized()
    disp = 0.0
    for s in (-1, 1):
        disp += .085 * g2(az - s * .95, el + .62, .45, .32)  # heavy jowls
        disp += .045 * g2(az - s * .75, el + .15, .30, .25)  # cheeks
        disp += .04 * g2(az - s * .37, el - .33, .28, .08)   # brow ridge
        disp += .03 * g2(az - s * .11, el + .20, .06, .05)   # nose wings
    disp += .08 * g2(az, el + .9, .5, .22)     # double chin
    disp += .13 * g2(az, el + .16, .11, .10)   # bulbous nose
    disp += .022 * g2(az, el + .40, .16, .04)  # upper lip
    disp += .04 * g2(az, el + .51, .14, .05)   # fat lower lip
    disp -= .02 * g2(az, el + .62, .20, .05)   # chin groove
    p.z -= .1 * low * max(0.0, -d.y)           # front of the jowls sags onto the chest
    p.z -= .04 * g2(az - .8, el + .4, .45, .35)   # asymmetry: left cheek/jowl droops
    return p + n * disp


def head_point(az, el):
    return HEAD_C + head_shape(dir_from(az, el))


def hairline(az):
    return .62 + (-.45 - .62) * smoothstep(.6, 2.5, abs(az))


# ---------- bmesh builders ----------

def quad_sphere(cuts):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=cuts, use_grid_fill=True)
    for v in bm.verts:
        x, y, z = v.co
        v.co = Vector((x * math.sqrt(max(0, 1 - y * y / 2 - z * z / 2 + y * y * z * z / 3)),
                       y * math.sqrt(max(0, 1 - z * z / 2 - x * x / 2 + z * z * x * x / 3)),
                       z * math.sqrt(max(0, 1 - x * x / 2 - y * y / 2 + x * x * y * y / 3))))
    return bm


def faces_in(bm, az0, az1, el0, el1):
    out = []
    for f in bm.faces:
        az, el = azel(f.calc_center_median().normalized())
        if az0 <= az <= az1 and el0 <= el <= el1:
            out.append(f)
    return out


def inset(bm, faces, thickness):
    return bmesh.ops.inset_region(bm, faces=faces, thickness=thickness, depth=0.0, use_even_offset=True)["faces"]


def tube(bm, rings, close_path, cap):
    """rings: list of lists of Vector (all the same length, each a closed cross-section)."""
    vrings = [[bm.verts.new(p) for p in ring] for ring in rings]
    n = len(rings[0])
    count = len(vrings) if close_path else len(vrings) - 1
    for i in range(count):
        a, b = vrings[i], vrings[(i + 1) % len(vrings)]
        for j in range(n):
            bm.faces.new((a[j], a[(j + 1) % n], b[(j + 1) % n], b[j]))
    if cap and not close_path:
        bm.faces.new(vrings[0])
        bm.faces.new(list(reversed(vrings[-1])))


def orient_outward(bm, centre):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    inward = sum(1 for f in bm.faces if f.normal.dot(f.calc_center_median() - centre) < 0)
    if inward > len(bm.faces) / 2:
        for f in bm.faces:
            f.normal_flip()


def set_material(bm, index):
    for f in bm.faces:
        f.material_index = index


# ---------- head ----------

def build_head():
    bm = quad_sphere(11)
    for s in (1, -1):  # eye sockets under the lenses: two concentric loops, recessed centre
        region = faces_in(bm, min(s * .12, s * .62), max(s * .12, s * .62), 0.0, .30)
        inset(bm, region, .035)
        inset(bm, region, .03)
        for v in {v for f in region for v in f.verts}:
            v.co *= .965
    nose = faces_in(bm, -.14, .14, -.28, -.02)
    inset(bm, nose, .03)
    mouth = faces_in(bm, -.25, .25, -.52, -.39)
    inset(bm, mouth, .03)
    lips = inset(bm, mouth, .025)
    set_material(bm, SKINHEAD)
    for f in lips:
        f.material_index = LIPS
    mouth_verts = {v for f in mouth for v in f.verts}
    lip_verts = {v for f in lips for v in f.verts} | mouth_verts
    el_centre = sum(azel(v.co.normalized())[1] for v in mouth_verts) / len(mouth_verts)
    for v in lip_verts:  # lips: a narrower band; inner faces: a recessed slit
        az, el = azel(v.co.normalized())
        squash, depth = (.12, .95) if v in mouth_verts else (.7, 1.0)
        v.co = dir_from(az * .85, el_centre + (el - el_centre) * squash) * v.co.length * depth
    for f in mouth:
        f.material_index = DARK

    az_layer = bm.verts.layers.float.new("az")
    el_layer = bm.verts.layers.float.new("el")
    for v in bm.verts:
        r = v.co.length
        d = v.co / r
        v[az_layer], v[el_layer] = azel(d)
        v.co = HEAD_C + head_shape(d) * r
    bm.normal_update()
    orient_outward(bm, HEAD_C)
    return bm


def head_uvs(bm):
    # warped spherical map: more texels on the face, seam at the back of the head
    uv = bm.loops.layers.uv.verify()
    az_layer, el_layer = bm.verts.layers.float["az"], bm.verts.layers.float["el"]
    for f in bm.faces:
        coords = [(.5 + .5 * math.sin(l.vert[az_layer] / 2), .5 + .5 * math.sin(l.vert[el_layer])) for l in f.loops]
        us = [c[0] for c in coords]
        if max(us) - min(us) > .5:  # face straddles the back seam
            coords = [(u + 1 if u < .5 else u, v) for u, v in coords]
        for l, c in zip(f.loops, coords):
            l[uv].uv = c


def build_hair(head_bm):
    bm = head_bm.copy()
    az_layer, el_layer = bm.verts.layers.float["az"], bm.verts.layers.float["el"]

    def margin(v):
        return v[el_layer] - hairline(v[az_layer])

    keep = {f for f in bm.faces if sum(margin(v) for v in f.verts) / len(f.verts) > 0 and f.material_index == SKINHEAD}
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f not in keep], context="FACES")
    boundary = {v for e in bm.edges if e.is_boundary for v in e.verts}
    for v in bm.verts:
        az, el = v[az_layer], v[el_layer]
        if v in boundary:  # snap the stepped grid edge onto the smooth hairline
            el = max(hairline(az), -1.4)
            v[el_layer] = el
        h = .025 + .045 * g2(az + .15, el - .95, .7, .35)          # slick pompadour
        h += .004 * math.sin(az * 30) * smoothstep(.5, 1.0, el)     # comb lines running back
        h -= .02 * math.exp(-((az - .42) / .05) ** 2) * smoothstep(.55, .8, el) * (1 - smoothstep(1.0, 1.5, abs(az)))  # side part
        d = dir_from(az, el)
        v.co = HEAD_C + head_shape(d) + d * h
    edges = [e for e in bm.edges if e.is_boundary]
    rim = bmesh.ops.extrude_edge_only(bm, edges=edges)["geom"]
    for v in [g for g in rim if isinstance(g, bmesh.types.BMVert)]:
        v.co = HEAD_C + (v.co - HEAD_C) * .93  # tuck the edge into the skull
    set_material(bm, HAIR)
    orient_outward(bm, HEAD_C)
    # skin faces fully covered by hair are never seen: drop them from the head to save triangles
    covered = [f for f in head_bm.faces if f.material_index == SKINHEAD and all(margin2(v, head_bm) > .25 for v in f.verts)]
    bmesh.ops.delete(head_bm, geom=covered, context="FACES")
    return bm


def margin2(v, bm):
    return v[bm.verts.layers.float["el"]] - hairline(v[bm.verts.layers.float["az"]])


def surface_front(bvh, x, z):
    hit = bvh.ray_cast(Vector((x, -3.0, z)), Vector((0, 1, 0)))
    return hit[0].y if hit[0] is not None else HEAD_C.y - HEAD_R.y


def superellipse(t, n=3.5):
    c, s = math.cos(t), math.sin(t)
    return math.copysign(abs(c) ** (2 / n), c), math.copysign(abs(s) ** (2 / n), s)


def build_glasses(bvh):
    bm_frame, bm_lens = bmesh.new(), bmesh.new()
    count = 20
    for side in (1, -1):
        cx, cz = side * LENS_C[0], LENS_C[1]
        scale = 1.08 if side < 0 else 1.0  # asymmetry: right lens a touch bigger
        a, b = LENS_AB[0] * scale, LENS_AB[1] * scale
        rings = [[], [], [], []]
        inner_back = []
        for j in range(count):
            sx, sz = superellipse(2 * math.pi * j / count)
            w = .03 + .025 * max(0.0, sz)  # thick top bar doubles as a heavy brow
            ix, iz = cx + a * sx, cz + b * sz
            ox, oz = cx + (a + w) * sx, cz + (b + w) * sz
            yi, yo = surface_front(bvh, ix, iz) - .03, surface_front(bvh, ox, oz) - .03
            rings[0].append(Vector((ox, yo - .035, oz)))
            rings[1].append(Vector((ix, yi - .035, iz)))
            rings[2].append(Vector((ix, yi, iz)))
            rings[3].append(Vector((ox, yo, oz)))
            inner_back.append(Vector((ix, yi - .006, iz)))
        # the 4 corner lines of the frame bar, revolved around the lens → a closed square-section ring
        tube(bm_frame, [list(r) for r in zip(*rings)], True, False)
        centre_y = sum(p.y for p in inner_back) / count - .012  # slight dome
        mid = [Vector((cx + (p.x - cx) * .55, (p.y + centre_y) * .5 - .006, cz + (p.z - cz) * .55)) for p in inner_back]
        centre = bm_lens.verts.new(Vector((cx, centre_y, cz)))
        mv = [bm_lens.verts.new(p) for p in mid]
        ov = [bm_lens.verts.new(p) for p in inner_back]
        for j in range(count):
            k = (j + 1) % count
            bm_lens.faces.new((centre, mv[j], mv[k]))
            bm_lens.faces.new((mv[j], ov[j], ov[k], mv[k]))
        # temple arm wrapping back to the ear
        az0 = math.atan2(cx + side * (a + .035), -(surface_front(bvh, cx + side * (a + .03), cz) - .03))
        el = math.asin((cz + .02 - HEAD_C.z) / HEAD_R.z)
        path = []
        for i in range(6):
            az = az0 + (side * 1.55 - az0) * i / 5
            path.append(head_point(az, el) + dir_from(az, el) * .035)
        tube(bm_frame, [[p + Vector((dx, dy, dz)) for dx, dy, dz in ((0, 0, .012), (.012 * side, 0, 0), (0, 0, -.012), (-.012 * side, 0, 0))] for p in path], False, True)
    # bridge over the nose
    inner = LENS_C[0] - LENS_AB[0]
    y = surface_front(bvh, 0.0, LENS_C[1] + .03) - .035
    box = [Vector((x, yy, z)) for x, yy, z in ((-inner, y - .03, LENS_C[1] + .02), (inner, y - .03, LENS_C[1] + .02), (inner, y, LENS_C[1] + .02), (-inner, y, LENS_C[1] + .02))]
    tube(bm_frame, [[p + Vector((0, 0, .028)) for p in box], box], False, True)
    set_material(bm_frame, FRAME)
    orient_outward(bm_frame, HEAD_C)
    set_material(bm_lens, LENS)
    for f in bm_lens.faces:
        if f.normal.y > 0:
            f.normal_flip()
    return bm_frame, bm_lens


def build_mustache(bvh):
    bm = bmesh.new()
    rings = []
    for i in range(15):
        x = -.19 + .38 * i / 14
        t = abs(x) / .2
        z = .89 + DZ - .065 * t ** 2.2
        rv = .012 + .03 * max(0.0, 1 - t * t) ** .7
        rd = .01 + .022 * max(0.0, 1 - t * t) ** .7
        y = surface_front(bvh, x, z) - rd * .6
        rings.append([Vector((x, y - rd * math.cos(a), z + rv * math.sin(a))) for a in (2 * math.pi * k / 8 for k in range(8))])
    tube(bm, rings, False, True)
    set_material(bm, HAIR)
    orient_outward(bm, Vector((0, 0, .9)))
    return bm


# ---------- body ----------

def ellipsoid(bm, centre, radii, u=16, v=10):
    geom = bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=1.0)
    for vert in geom["verts"]:
        vert.co = centre + Vector((vert.co.x * radii[0], vert.co.y * radii[1], vert.co.z * radii[2]))
    return geom["verts"]


def build_body():
    parts = {}
    torso = bmesh.new()
    for v in ellipsoid(torso, Vector((0, 0, .48)), (.34, .30, .30)):
        belly = 1 + .12 * smoothstep(.48, .25, v.co.z)  # pear: heavier at the bottom
        v.co.x *= belly
        v.co.y *= belly
    set_material(torso, CLOTH)
    bvh = BVHTree.FromBMesh(torso)

    def torso_hit(x, z):
        hit = bvh.ray_cast(Vector((x, -3, z)), Vector((0, 1, 0)))
        return hit[0].y

    # belt (follows the torso surface) + two chest pockets with flaps + gold pin
    belt = bmesh.new()
    path = []
    for i in range(20):
        a = 2 * math.pi * i / 20
        dirxy = Vector((math.sin(a), -math.cos(a), 0))
        hit = bvh.ray_cast(Vector((0, 0, .22)) + dirxy * 2, -dirxy)
        path.append((hit[0] + dirxy * .012, dirxy))
    tube(belt, [[p + d * (.012 * math.cos(t)) + Vector((0, 0, .028 * math.sin(t))) for t in (2 * math.pi * k / 5 for k in range(5))] for p, d in path], True, False)
    set_material(belt, DARK)
    orient_outward(belt, Vector((0, 0, .22)))
    pockets = bmesh.new()
    for s in (1, -1):
        for w, h, z, depth in ((.085, .05, .30, .03), (.095, .018, .345, .036)):  # pocket, flap
            y = torso_hit(s * .13, z) + .006
            box = bmesh.ops.create_cube(pockets, size=1.0)["verts"]
            for v in box:
                v.co = Vector((s * .13 + v.co.x * w * 2, y + v.co.y * depth, z + v.co.z * h * 2))
    set_material(pockets, CLOTH)
    pin = bmesh.new()
    y = torso_hit(.13, .345) - .022
    for v in bmesh.ops.create_cone(pin, cap_ends=True, segments=10, radius1=.018, radius2=.018, depth=.008)["verts"]:
        v.co = Vector((.13 + v.co.x, y + v.co.z, .345 + v.co.y))
    set_material(pin, ACCENT)
    parts["Torso"], parts["Belt"], parts["Pockets"], parts["Pin"] = torso, belt, pockets, pin

    for s, arm_bone, leg_bone in ((1, "ArmL", "LegL"), (-1, "ArmR", "LegR")):
        arm = bmesh.new()
        geom = bmesh.ops.create_cone(arm, cap_ends=True, segments=10, radius1=.085, radius2=.1, depth=.34)
        side_edges = [e for e in arm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > .1]
        bmesh.ops.subdivide_edges(arm, edges=side_edges, cuts=2)  # loops for the shoulder/elbow bend
        tilt = math.radians(28) * s
        for v in arm.verts:
            x, z = v.co.x, v.co.z - .17  # top of the sleeve at the pivot
            v.co = Vector((.32 * s + x * math.cos(tilt) - z * math.sin(tilt), v.co.y, .56 + x * math.sin(tilt) + z * math.cos(tilt)))
        set_material(arm, CLOTH)
        hand = bmesh.new()
        tip = Vector((.32 * s + .36 * math.sin(tilt), 0, .56 - .36 * math.cos(tilt)))
        ellipsoid(hand, tip, (.095, .08, .11), 10, 6)
        set_material(hand, SKIN)
        leg = bmesh.new()
        bmesh.ops.create_cone(leg, cap_ends=True, segments=10, radius1=.07, radius2=.075, depth=.3)
        for v in leg.verts:
            v.co += Vector((.13 * s, 0, .25))
        foot = bmesh.new()
        ellipsoid(foot, Vector((.13 * s, -.04, .05)), (.1, .13, .055), 10, 6)
        set_material(leg, DARK)
        set_material(foot, DARK)
        parts["Arm" + arm_bone[-1]], parts["Hand" + arm_bone[-1]] = arm, hand
        parts["Leg" + leg_bone[-1]], parts["Foot" + leg_bone[-1]] = leg, foot
    return parts


def body_weights(name, co):
    side = name[-1]
    if name.startswith("Arm"):
        w = smoothstep(.56, .48, co.z)  # the top ring is shared with the body
        return {"Arm" + side: .5 + .5 * w, "Hips": .5 - .5 * w}
    if name.startswith("Hand"):
        return {"Arm" + side: 1.0}
    if name.startswith(("Leg", "Foot")):
        return {"Leg" + side: 1.0}
    return {"Hips": 1.0}


def head_weights(part, co):
    if part != P_SKIN:
        return {"Head": 1.0}
    w = smoothstep(.40, .60, co.z)  # the jowls rest on (and move with) the torso
    return {"Head": w, "Hips": 1 - w} if w < 1 else {"Head": 1.0}


# ---------- objects ----------

def materials():
    mats = []
    for name, color in SLOTS:
        mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        if mat.node_tree is None:
            mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf is not None:
            bsdf.inputs["Base Color"].default_value = (*lin(color), 1)
            bsdf.inputs["Roughness"].default_value = .75
        mat.diffuse_color = (*lin(color), 1)
        mats.append(mat)
    return mats


def make_object(name, bm, mats, part, weights):
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
    bm.loops.layers.uv.verify()
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    mesh.shade_smooth()
    for m in mats:
        mesh.materials.append(m)
    for attr in ("az", "el"):
        if attr in mesh.attributes:
            mesh.attributes.remove(mesh.attributes[attr])
    tag = mesh.attributes.new("part", "INT", "POINT")
    for i in range(len(mesh.vertices)):
        tag.data[i].value = part
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    groups = {b: obj.vertex_groups.new(name=b) for b in BONES}
    for v in mesh.vertices:
        for bone, w in weights(v.co).items():
            groups[bone].add([v.index], w, "REPLACE")
    return obj


# ---------- detail bake ----------

def crease(x, y, z):
    """analytic skin creases in metres; returns negative depth (≤ 0)."""
    z -= DZ
    front = smoothstep(-.15, -.35, y)
    if front <= 0:
        return 0.0
    d = 0.0
    zz = z - .12 * x * x
    for zc in (1.30, 1.335, 1.37):  # forehead furrows
        d -= .0035 * math.exp(-((zz - zc) / .004) ** 2) * smoothstep(.3, .22, abs(x))
    for s in (-1, 1):
        d -= .003 * g2(x - s * .025, z - 1.22, .004, .04)  # scowl lines between the lenses
        for off in (0.0, .022):  # eye bags
            zc = .975 - off - 1.0 * (x - s * .2) ** 2
            d -= .003 * math.exp(-((z - zc) / .004) ** 2) * smoothstep(.13, .09, abs(x - s * .2))
        for (x0, z0), (x1, z1), depth in ((((.085, .97), (.17, .80), .006)), (((.12, .80), (.16, .68), .004))):  # nasolabial, marionette
            ax, az_ = s * x0, z0
            bx, bz = s * x1, z1
            t = clamp01(((x - ax) * (bx - ax) + (z - az_) * (bz - az_)) / ((bx - ax) ** 2 + (bz - az_) ** 2))
            dist = math.hypot(x - (ax + t * (bx - ax)), z - (az_ + t * (bz - az_)))
            d -= depth * math.exp(-(dist / .006) ** 2)
    d -= .004 * math.exp(-((z - .755) / .005) ** 2) * smoothstep(.1, .07, abs(x))           # under the lower lip
    d -= .007 * math.exp(-((z - (.66 + .4 * x * x)) / .01) ** 2) * smoothstep(.3, .22, abs(x))  # double-chin fold
    return d * front


BLEMISHES = [(.33, .92, .012, .005), (-.28, 1.27, .008, .003), (.05, .99, .007, .003), (-.36, .85, .01, .004), (.22, .78, .006, .002)]


def bake_head(head_obj, mats):
    scene = bpy.context.scene
    high_mesh = head_obj.data.copy()
    high = bpy.data.objects.new("HeadHigh", high_mesh)
    scene.collection.objects.link(high)
    mod = high.modifiers.new("Subsurf", "SUBSURF")
    mod.levels = mod.render_levels = 3
    depsgraph = bpy.context.evaluated_depsgraph_get()
    dense = bpy.data.meshes.new_from_object(high.evaluated_get(depsgraph))
    high.modifiers.clear()
    high.data = dense

    dense.calc_loop_triangles()
    col = dense.color_attributes.new("Col", "FLOAT_COLOR", "POINT")
    tan, red, dark, stubble = Vector(lin((.80, .62, .46))), Vector(lin((.84, .50, .42))), Vector(lin((.60, .45, .37))), Vector(lin((.40, .34, .31)))
    normals = [v.normal.copy() for v in dense.vertices]
    for v, n in zip(dense.vertices, normals):
        x, y, z = v.co
        c = crease(x, y, z)
        z -= DZ
        pore = noise.noise(v.co * 220) * .0004 + noise.noise(v.co * 90) * .0006
        pore *= 1 + 2 * g2(x, z - .98, .08, .07)  # rough nose
        bump = sum(h * math.exp(-((x - bx) ** 2 + (z - bz) ** 2) / r ** 2) for bx, bz, r, h in BLEMISHES) * smoothstep(-.2, -.35, y)
        v.co += n * (c + pore + bump)
        color = tan.copy()
        color = color.lerp(red, .55 * g2(x, z - .98, .07, .06) + .3 * (g2(x - .3, z - .9, .1, .1) + g2(x + .3, z - .9, .1, .1)))
        color = color.lerp(dark, .4 * (g2(x - .2, z - .97, .12, .03) + g2(x + .2, z - .97, .12, .03)))
        jaw = smoothstep(.86, .80, z) * smoothstep(-.1, -.3, y) * smoothstep(.45, .3, abs(x))
        if noise.noise(v.co * 400) > .3:
            color = color.lerp(stubble, .5 * jaw)  # 5 o'clock shadow
        color = color.lerp(dark, min(1.0, bump * 120))
        color *= max(.72, 1 + 50 * c)  # cavity: creases go darker
        col.data[v.index].color = (*color, 1)

    src = bpy.data.materials.new("BakeSource")
    if src.node_tree is None:
        src.use_nodes = True
    nodes = src.node_tree.nodes
    nodes.clear()
    attr = nodes.new("ShaderNodeVertexColor")
    attr.layer_name = "Col"
    emit = nodes.new("ShaderNodeEmission")
    out = nodes.new("ShaderNodeOutputMaterial")
    src.node_tree.links.new(attr.outputs["Color"], emit.inputs["Color"])
    src.node_tree.links.new(emit.outputs["Emission"], out.inputs["Surface"])
    dense.materials.clear()
    dense.materials.append(src)

    normal_img = bpy.data.images.new("Distributor_Head_Normal", TEX_SIZE, TEX_SIZE, alpha=False)
    normal_img.colorspace_settings.name = "Non-Color"
    base_img = bpy.data.images.new("Distributor_Head_Base", TEX_SIZE, TEX_SIZE, alpha=False)
    image_nodes = []
    for mat in mats:
        node = mat.node_tree.nodes.new("ShaderNodeTexImage")
        mat.node_tree.nodes.active = node
        image_nodes.append(node)

    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 1
    bake = scene.render.bake
    bake.use_selected_to_active = True
    bake.cage_extrusion = .04
    bake.max_ray_distance = .1
    bake.margin = 8
    for obj in scene.objects:
        obj.select_set(False)
    high.select_set(True)
    head_obj.select_set(True)
    bpy.context.view_layer.objects.active = head_obj
    for img, kind in ((normal_img, "NORMAL"), (base_img, "EMIT")):
        for node in image_nodes:
            node.image = img
        bpy.ops.object.bake(type=kind, normal_space="TANGENT")
        img.filepath_raw = os.path.join(OUT_DIR, img.name + ".png")
        img.file_format = "PNG"
        img.save()
    for mat, node in zip(mats, image_nodes):
        mat.node_tree.nodes.remove(node)
    bpy.data.objects.remove(high)
    bpy.data.meshes.remove(high_mesh)


# ---------- rig + shape keys ----------

def build_armature():
    data = bpy.data.armatures.new("Armature")
    arm = bpy.data.objects.new("Armature", data)
    bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    spec = {"Hips": ((0, 0, .42), (0, 0, .6), None), "Head": ((0, 0, .6), (0, 0, 1.1), "Hips"),
            "ArmL": ((.32, 0, .56), (.49, 0, .24), "Hips"), "ArmR": ((-.32, 0, .56), (-.49, 0, .24), "Hips"),
            "LegL": ((.13, 0, .4), (.13, 0, .05), None), "LegR": ((-.13, 0, .4), (-.13, 0, .05), None)}
    for name in BONES:
        bone = data.edit_bones.new(name)
        bone.head, bone.tail = Vector(spec[name][0]), Vector(spec[name][1])
    for name in BONES:
        if spec[name][2]:
            data.edit_bones[name].parent = data.edit_bones[spec[name][2]]
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def shape_deltas(part, co):
    x, y, z = co
    z -= DZ
    front = smoothstep(-.1, -.3, y)
    brow = Vector()
    if part == P_SKIN:
        w = g2(x, z - 1.24, .32, .09) * front
        brow = Vector((0, -.01 * w, -.03 * w * (1 - .5 * min(1.0, abs(x) / .3))))
    elif part in (P_FRAME, P_LENS) and y < 0:
        brow = Vector((0, 0, -.025 * smoothstep(LENS_C[1] - DZ - .1, LENS_C[1] - DZ + .12, z) * (1 - min(1.0, abs(x) / .38))))
    sneer = Vector()
    if part == P_SKIN:
        w = (g2(x - .09, z - .86, .07, .06) + .5 * g2(x - .2, z - .95, .1, .08)) * front
        sneer = Vector((0, -.005 * w, .025 * w))
    elif part == P_STACHE:
        sneer = Vector((0, 0, .022 * math.exp(-((x - .12) / .08) ** 2) * smoothstep(0, .06, x)))
    mouth = Vector()
    if part == P_SKIN and y < -.15:
        w = smoothstep(.818, .80, z) * smoothstep(.55, .72, z) * math.exp(-(x / .22) ** 2)
        mouth = Vector((0, 0, -.05 * w))
    sag = Vector()
    if part == P_SKIN:
        w = smoothstep(.8, .55, z) * (1 - g2(x, 0, .12, 1) * front * .5)
        radial = Vector((x, y, 0)).normalized() if abs(x) + abs(y) > 1e-4 else Vector()
        sag = radial * .02 * w + Vector((0, 0, -.025 * w))
    return {"BrowAngry": brow, "Sneer": sneer, "MouthOpen": mouth, "JowlSag": sag}


def add_shape_keys(obj):
    mesh = obj.data
    tags = [d.value for d in mesh.attributes["part"].data]
    base = [v.co.copy() for v in mesh.vertices]
    obj.shape_key_add(name="Basis", from_mix=False)
    deltas = [shape_deltas(t, c) for t, c in zip(tags, base)]
    for name in ("BrowAngry", "Sneer", "MouthOpen", "JowlSag"):
        key = obj.shape_key_add(name=name, from_mix=False)
        for i, d in enumerate(deltas):
            key.data[i].co = base[i] + d[name]


# ---------- previews ----------

def render_previews(target):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.light = "STUDIO"
    scene.render.resolution_x = scene.render.resolution_y = 640
    cam = bpy.data.objects.new("PreviewCam", bpy.data.cameras.new("PreviewCam"))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 1.9
    scene.collection.objects.link(cam)
    scene.camera = cam
    for label, az in (("front", 0.0), ("threequarter", .7), ("side", 1.5708)):
        d = dir_from(az, .12)
        cam.location = Vector((0, 0, .82)) + d * 6
        cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(target, f"distributor_{label}.png")
        bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


# ---------- main ----------

def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    os.makedirs(OUT_DIR, exist_ok=True)
    mats = materials()

    head_bm = build_head()
    head_uvs(head_bm)
    hair_bm = build_hair(head_bm)
    head_bm.normal_update()
    bvh = BVHTree.FromBMesh(head_bm)
    frame_bm, lens_bm = build_glasses(bvh)
    stache_bm = build_mustache(bvh)

    objects = [make_object("Head", head_bm, mats, P_SKIN, lambda co: head_weights(P_SKIN, co))]
    bake_head(objects[0], mats)
    for name, bm, part in (("Hair", hair_bm, P_HAIR), ("Frames", frame_bm, P_FRAME), ("Lenses", lens_bm, P_LENS), ("Mustache", stache_bm, P_STACHE)):
        objects.append(make_object(name, bm, mats, part, lambda co, p=part: head_weights(p, co)))
    for name, bm in build_body().items():
        objects.append(make_object(name, bm, mats, P_BODY, lambda co, n=name: body_weights(n, co)))

    for obj in bpy.context.scene.objects:
        obj.select_set(obj in objects)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    body = bpy.context.view_layer.objects.active
    body.name = body.data.name = "Distributor"

    mesh = body.data
    tags = [d.value for d in mesh.attributes["part"].data]
    head_tris = sum(len(p.vertices) - 2 for p in mesh.polygons if tags[p.vertices[0]] != P_BODY)
    tris = sum(len(p.vertices) - 2 for p in mesh.polygons)
    print(f"DISTRIBUTOR tris={tris} head={head_tris} ({head_tris / tris:.0%}) verts={len(mesh.vertices)}")
    assert tris <= MAX_TRIS, f"{tris} triangles is over the {MAX_TRIS} budget"
    assert head_tris / tris >= .55, "head should hold most of the geometry"

    add_shape_keys(body)
    mesh.attributes.remove(mesh.attributes["part"])
    arm = build_armature()
    body.parent = arm
    body.modifiers.new("Armature", "ARMATURE").object = arm

    for obj in bpy.context.scene.objects:
        obj.select_set(obj in (arm, body))
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_DIR, "Distributor.fbx"), use_selection=True, object_types={"ARMATURE", "MESH"},
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             use_mesh_modifiers=False, mesh_smooth_type="OFF", use_tspace=True, add_leaf_bones=False,
                             use_armature_deform_only=True, bake_anim=False, path_mode="STRIP")
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    if PREVIEW_DIR:
        render_previews(PREVIEW_DIR)
    print("DISTRIBUTOR done")


main()
