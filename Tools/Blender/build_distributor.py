# Builds "The Distributor" (Level01 antagonist), cozyfarm look "Travelling Salesman": one skinned mesh, Chibi-compatible
# armature, face shape keys, baked head normal + base map. Every clothing detail (lapels, buttons, pockets, cuffs, belt,
# coat skirt, hat, soles, ledger) is real geometry standing off the body. Re-runnable, headless:
#   "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup -P Tools/Blender/build_distributor.py [-- <previewDir>]
# Units = Unity rig space at scale 1 (about 1.6 m tall; the NPC prefab scales it x1.3). Blender Z up, character faces -Y,
# character's left = +X.
import math
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector, noise
from mathutils.bvhtree import BVHTree

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(REPO, "Assets", "_Project", "Art", "Models", "Distributor")
BLEND_PATH = os.path.join(REPO, "Tools", "Blender", "Distributor.blend")
PREVIEW_DIR = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else None
TEX_SIZE = 1024
MAX_TRIS = 7500

HEAD_C = Vector((0.0, 0.0, 0.98))
DZ = HEAD_C.z - 1.06  # feature heights below are authored for a head centre at 1.06
HEAD_R = Vector((0.52, 0.50, 0.56))
LENS_C = (0.20, 1.13 + DZ)
LENS_AB = (0.15, 0.10)

BONES = ["Hips", "Head", "ArmL", "ArmR", "LegL", "LegR"]
# material slots, in order; Unity remaps them by name
# colours are the NPC concept palette (sRGB); CharacterBuilder.DistributorMaterial mirrors them in Unity
SKIN_RGB = (.788, .557, .392)
SLOTS = [("SkinHead", SKIN_RGB), ("Skin", SKIN_RGB), ("Hair", (.165, .141, .125)), ("Frame", (.29, .18, .11)),
         ("Lens", (.969, .953, .918)), ("Lips", (.722, .416, .361)), ("Cloth", (.71, .337, .184)), ("Dark", (.11, .07, .06)),
         ("Accent", (.878, .69, .29)), ("Trim", (.561, .247, .133)), ("Charcoal", (.231, .22, .212)), ("Leather", (.478, .29, .165)),
         ("EyeWhite", (.969, .953, .918))]
SKINHEAD, SKIN, HAIR, FRAME, LENS, LIPS, CLOTH, DARK, ACCENT, TRIM, CHARCOAL, LEATHER, EYEWHITE = range(len(SLOTS))
# per-vertex part tags (used for weights / shape keys, removed before export)
P_SKIN, P_HAIR, P_FRAME, P_LENS, P_STACHE, P_BODY, P_EYE = range(7)
EYE_C = (LENS_C[0], LENS_C[1] - .005)  # eyeball centre (x for the left eye, z): in the socket, behind the lens centre


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


def bvh_of(*bms):
    verts, polys = [], []
    for bm in bms:
        bm.verts.index_update()
        polys += [[len(verts) + v.index for v in f.verts] for f in bm.faces]
        verts += [v.co.copy() for v in bm.verts]
    return BVHTree.FromPolygons(verts, polys)


def surface_front(bvh, x, z):
    hit = bvh.ray_cast(Vector((x, -3.0, z)), Vector((0, 1, 0)))
    return hit[0].y if hit[0] is not None else HEAD_C.y - HEAD_R.y


def superellipse(t, n=3.5):
    c, s = math.cos(t), math.sin(t)
    return math.copysign(abs(c) ** (2 / n), c), math.copysign(abs(s) ** (2 / n), s)


def surface_side(bvh, side, y, z):
    hit = bvh.ray_cast(Vector((side * 3.0, y, z)), Vector((-side, 0, 0)))
    return hit[0].x if hit[0] is not None else 0.0  # nothing beside this point: no constraint


def lens_outline(t):
    """rounded panto oval (wider at the top, the lower inner corner pulled in), in lens-local (u, v); u > 0 = outer."""
    sx, sz = superellipse(t, 2.4)
    if sz < 0 and sx < 0:
        sx *= 1 - .3 * -sz  # leaves room for the nose
    return sx * LENS_AB[0], sz * LENS_AB[1] * (1.06 if sz > 0 else 1.0)


def build_glasses(bvh):
    # a rigid frame like real glasses: seen from above it is a box. One straight front (flat rims + end pieces reaching past
    # the widest part of the head), square corners at the hinges, temples running straight back, parallel, to the ears.
    # It rests on the nose pads and stands off the cheeks and temples instead of hugging the face.
    bm_frame, bm_lens = bmesh.new(), bmesh.new()
    count, rim_w, rim_d, clear, side_clear = 24, .016, .02, .015, .035
    tilt = math.radians(8)
    cz = LENS_C[1]

    def basis(side):
        u = Vector((side, 0, 0))                                    # outward, flat across: no wrap
        v = Vector((0, -math.sin(tilt), math.cos(tilt)))            # up, top leaning forward (bottom toward the cheeks)
        n = u.cross(v).normalized()
        return u, v, (n if n.y < 0 else -n)                         # n = forward

    def place(side, y0, lu, lv, ln):
        u, v, n = basis(side)
        return Vector((side * LENS_C[0], y0, cz)) + u * lu + v * lv + n * ln

    # push the whole frame forward until no part of its back face is closer than `clear` to the face
    y0 = 0.0
    for _ in range(2):
        need = 0.0
        for side in (1, -1):
            for j in range(count):
                lu, lv = lens_outline(2 * math.pi * j / count)
                for grow in (0.0, rim_w):
                    k = (math.hypot(lu, lv) + grow) / max(1e-6, math.hypot(lu, lv))
                    p = place(side, y0, lu * k, lv * k, -rim_d / 2)
                    need = min(need, surface_front(bvh, p.x, p.z) - clear - p.y)
        y0 += need

    for side in (1, -1):
        u, v, n = basis(side)
        centre = place(side, y0, 0, 0, 0)

        def at(lu, lv, ln=0.0):
            return centre + u * lu + v * lv + n * ln

        outline = [lens_outline(2 * math.pi * j / count) for j in range(count)]
        rings = []
        for lu, lv in outline:
            r = math.hypot(lu, lv)
            du, dv = lu / r, lv / r
            mid = r + rim_w / 2
            rings.append([at(du * (mid + rim_w / 2 * math.cos(a)), dv * (mid + rim_w / 2 * math.cos(a)), rim_d / 2 * math.sin(a))
                          for a in (2 * math.pi * k / 6 for k in range(6))])
        tube(bm_frame, rings, True, False)
        hub = bm_lens.verts.new(at(0, 0))
        ov = [bm_lens.verts.new(at(lu, lv)) for lu, lv in outline]
        for j in range(count):
            bm_lens.faces.new((hub, ov[j], ov[(j + 1) % count]))

        # end piece: the front bar carries straight on past the rim to clear the widest part of the head
        lu, lv = LENS_AB[0] + rim_w, LENS_AB[1] * .45
        start = at(lu - .004, lv)
        ear_y = .06
        reach = max(side * surface_side(bvh, side, start.y + (ear_y - start.y) * i / 10, start.z) for i in range(11))
        hinge = Vector((side * max(reach + side_clear, side * start.x + .03), start.y, start.z))
        bar = []
        for p in (start, hinge + Vector((side * .006, 0, 0))):
            bar.append([p + n * dn + Vector((0, 0, dz)) for dn, dz in ((rim_d / 2, .012), (-rim_d / 2, .012), (-rim_d / 2, -.012), (rim_d / 2, -.012))])
        tube(bm_frame, bar, False, True)
        # square corner, then the temple straight back (constant x and height) and down behind the ear
        path = [Vector((hinge.x, hinge.y + (ear_y - hinge.y) * i / 6, hinge.z)) for i in range(7)]
        for dy, dz, dx in ((.07, -.04, .006), (.11, -.1, .015)):          # ear bend: down and slightly in
            path.append(Vector((hinge.x - side * dx, ear_y + dy, hinge.z + dz)))
        temple = []
        for i, p in enumerate(path):
            fwd = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
            out = fwd.cross(Vector((0, 0, 1))).normalized() * side
            up = out.cross(fwd).normalized() * side
            h = .013 if i < len(path) - 2 else .011                    # thin wire, a little thinner past the bend
            temple.append([p + up * h + out * .006, p - up * h + out * .006, p - up * h - out * .006, p + up * h - out * .006])
        tube(bm_frame, temple, False, True)

        # nose pad on a little arm from the inner rim
        inner = at(-(LENS_AB[0] * .82 + rim_w), -LENS_AB[1] * .25, -rim_d / 2)
        px, pz = side * .055, inner.z - .02
        pad = Vector((px, surface_front(bvh, px, pz) - .01, pz))
        tube(bm_frame, [[p + Vector(o) for o in ((0, 0, .004), (.004, 0, 0), (0, 0, -.004), (-.004, 0, 0))] for p in (inner, pad)], False, True)
        geom = bmesh.ops.create_uvsphere(bm_frame, u_segments=8, v_segments=5, radius=1.0)
        for vert in geom["verts"]:
            vert.co = pad + Vector((vert.co.x * .007, vert.co.y * .012, vert.co.z * .02))

    # arched bridge (keyhole) between the inner tops of the rims
    bridge = []
    a0 = place(1, y0, -(LENS_AB[0] * .55 + rim_w * .5), LENS_AB[1] * .9, 0)
    for i in range(9):
        t = i / 8
        p = a0.lerp(Vector((-a0.x, a0.y, a0.z)), t)
        p.z += .025 * math.sin(math.pi * t)
        p.y -= .008 * math.sin(math.pi * t)
        bridge.append([p + Vector(o) for o in ((0, 0, .007), (0, -.008, 0), (0, 0, -.007), (0, .008, 0))])
    tube(bm_frame, bridge, False, True)

    set_material(bm_frame, FRAME)
    bmesh.ops.recalc_face_normals(bm_frame, faces=bm_frame.faces[:])
    set_material(bm_lens, LENS)
    bm_lens.normal_update()  # new faces carry no normal yet; without this the right lens kept facing backwards (culled in Unity)
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

    parts["Torso"] = torso
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
        set_material(leg, CHARCOAL)
        set_material(foot, LEATHER)
        parts["Arm" + arm_bone[-1]], parts["Hand" + arm_bone[-1]] = arm, hand
        parts["Leg" + leg_bone[-1]], parts["Foot" + leg_bone[-1]] = leg, foot
    parts.update(build_coat(bvh))
    return parts


# ---------- outfit: surface-hugging solids ----------

def torso_hit(bvh, az, z):
    """(point, normal) on the torso, raying horizontally toward the Z axis; az 0 = front."""
    d = Vector((math.sin(az), -math.cos(az), 0))
    loc, nor, _, _ = bvh.ray_cast(Vector((0, 0, z)) + d * 2, -d)
    return (loc, nor) if loc is not None else None


def slab(bm, grid, off, thick, closed_u=False):
    """grid[v][u] = (point, normal) → a closed solid between point+n*off and point+n*(off+thick)."""
    nv, nu = len(grid), len(grid[0])
    inner = [[bm.verts.new(p + n * off) for p, n in row] for row in grid]
    outer = [[bm.verts.new(p + n * (off + thick)) for p, n in row] for row in grid]
    ucount = nu if closed_u else nu - 1
    for j in range(nv - 1):
        for i in range(ucount):
            k = (i + 1) % nu
            bm.faces.new((outer[j][i], outer[j][k], outer[j + 1][k], outer[j + 1][i]))
            bm.faces.new((inner[j][i], inner[j + 1][i], inner[j + 1][k], inner[j][k]))
    for j in (0, nv - 1):
        for i in range(ucount):
            k = (i + 1) % nu
            bm.faces.new((inner[j][i], inner[j][k], outer[j][k], outer[j][i]))
    if not closed_u:
        for i in (0, nu - 1):
            for j in range(nv - 1):
                bm.faces.new((inner[j][i], outer[j][i], outer[j + 1][i], inner[j + 1][i]))


def panel(bm, bvh, az0, az1, z0, z1, off, thick, nu=6, nv=4):
    """a patch on the torso between two angles (callables of t, 0 at z0 → 1 at z1, allowed) and two heights."""
    rows = []
    for j in range(nv + 1):
        t = j / nv
        a0, a1 = (az0(t) if callable(az0) else az0), (az1(t) if callable(az1) else az1)
        row = [torso_hit(bvh, a0 + (a1 - a0) * i / nu, z0 + (z1 - z0) * t) for i in range(nu + 1)]
        if all(row):
            rows.append(row)
    slab(bm, rows, off, thick)


def band(bm, bvh, z0, z1, off, thick, nu=24):
    slab(bm, [[torso_hit(bvh, 2 * math.pi * i / nu, z) for i in range(nu)] for z in (z0, z1)], off, thick, closed_u=True)


def disc(bm, centre, normal, r, depth, seg=10):
    rot = Vector((0, 0, 1)).rotation_difference(normal)
    for v in bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=r, depth=depth)["verts"]:
        v.co = centre + rot @ v.co


def block(bm, centre, size, facing=None):
    rot = Vector((0, -1, 0)).rotation_difference(facing) if facing is not None else None
    for v in bmesh.ops.create_cube(bm, size=1.0)["verts"]:
        p = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
        v.co = centre + (rot @ p if rot is not None else p)


def ring(bm, centre, axis, r_in, r_out, h, seg=14):
    """short thick tube around an axis (cuffs, hems, hat brim and band)."""
    rot = Vector((0, 0, 1)).rotation_difference(axis)
    rings = [[centre + rot @ Vector((rr * math.cos(a), rr * math.sin(a), zz)) for a in (2 * math.pi * k / seg for k in range(seg))]
             for rr, zz in ((r_in, -h / 2), (r_out, -h / 2), (r_out, h / 2), (r_in, h / 2))]
    tube(bm, [list(c) for c in zip(*rings)], True, False)


def arm_point(s, along):
    """point on the sleeve axis, `along` metres down from the shoulder pivot."""
    t = math.radians(28) * s
    return Vector((.32 * s + along * math.sin(t), 0, .56 - along * math.cos(t)))


def solid(slot, *builders):
    bm = bmesh.new()
    for build in builders:
        build(bm)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    set_material(bm, slot)
    return bm


def build_coat(bvh):
    """rust frock coat: lapels, flared skirt open at the front, double gold buttons, flap pockets, sash belt; plus cuffs,
    trouser hems, soles and the ledger case. Keys are body_weights names (side = last letter)."""
    def lapels(bm):
        for s in (1, -1):
            panel(bm, bvh, lambda t, s=s: s * (.05 + .25 * t), lambda t, s=s: s * (.3 + .1 * t), .3, .6, .004, .02, 4, 4)

    def pocket_flaps(bm):
        for s in (1, -1):
            p, n = torso_hit(bvh, s * 1.25, .22)
            block(bm, Vector((p.x * 1.12, p.y * 1.12, .135)) + n * .03, (.12, .02, .035), n)

    def skirt(bm):
        rows = []
        for j, z in enumerate((.22, .15, .08)):
            flare = 1 + .18 * j
            row = []
            for i in range(19):
                p, n = torso_hit(bvh, .35 + (2 * math.pi - .7) * i / 18, .22)
                row.append((Vector((p.x * flare, p.y * flare, z)), Vector((n.x, n.y, 0)).normalized()))
            rows.append(row)
        slab(bm, rows, .004, .02)

    def buttons(bm):
        for s in (1, -1):
            for z in (.24, .3):
                p, n = torso_hit(bvh, s * .2, z)
                disc(bm, p + n * .028, n, .015, .012)

    def sash(bm):
        band(bm, bvh, .2, .24, .022, .014)
        p, n = torso_hit(bvh, 0, .22)
        block(bm, p + n * .042, (.045, .012, .045))

    parts = {"Coat": solid(CLOTH, skirt), "CoatTrim": solid(TRIM, lapels, pocket_flaps), "CoatGold": solid(ACCENT, buttons, sash)}
    for s, side in ((1, "L"), (-1, "R")):
        axis = Vector((math.sin(math.radians(28) * s), 0, -math.cos(math.radians(28) * s)))
        parts["HandCuff" + side] = solid(ACCENT, lambda bm, s=s, axis=axis: ring(bm, arm_point(s, .31), axis, .09, .118, .06))
        parts["LegHem" + side] = solid(CHARCOAL, lambda bm, s=s: ring(bm, Vector((.13 * s, 0, .13)), Vector((0, 0, 1)), .07, .088, .045))
        parts["FootSole" + side] = solid(HAIR, lambda bm, s=s: ellipsoid(bm, Vector((.13 * s, -.045, .014)), (.112, .148, .02), 12, 6))
    grip = arm_point(-1, .44)
    parts["HandLedgerR"] = solid(LEATHER, lambda bm: block(bm, grip + Vector((0, 0, -.05)), (.05, .24, .17)))
    parts["HandClaspR"] = solid(ACCENT, lambda bm: block(bm, grip + Vector((0, 0, .04)), (.02, .08, .02)))
    return parts


def build_hat(hair_bm):
    """brown bowler resting on the hair: dome crown, brim, band."""
    bvh = BVHTree.FromBMesh(hair_bm)
    hair_top = max(v.co.z for v in hair_bm.verts)
    z = 1.8
    while z > 1.0:  # lowest height where the hair is as wide as the crown's opening
        width = 0.0
        for k in range(8):
            d = Vector((math.cos(k * math.pi / 4), math.sin(k * math.pi / 4), 0))
            loc = bvh.ray_cast(Vector((0, 0, z)) + d * 2, -d)[0]
            if loc is not None:
                width = max(width, Vector((loc.x, loc.y)).length)
        if width >= .41:
            break
        z -= .005
    top = Vector((0, 0, z))

    def crown(bm):
        bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=10, radius=.43,
                                  matrix=Matrix.Translation(top) @ Matrix.Diagonal((1, 1, (hair_top - top.z + .06) / .43, 1)))
        ring(bm, top + Vector((0, 0, -.02)), Vector((0, 0, 1)), .4, .6, .03, 28)

    return {"Hat": solid(LEATHER, crown),
            "HatBand": solid(CHARCOAL, lambda bm: ring(bm, top + Vector((0, 0, .04)), Vector((0, 0, 1)), .415, .44, .06, 28))}


def build_mouth_cavity(bvh, head_bm):
    """dark pocket just behind the lips: whatever MouthOpen opens shows dark, not stretched skin."""
    mouth = [v.co for f in head_bm.faces if f.material_index == DARK for v in f.verts]
    z = sum(c.z for c in mouth) / len(mouth)
    bm = bmesh.new()
    verts = ellipsoid(bm, Vector((0, surface_front(bvh, 0, z), z - .03)), (.17, .035, .07), 10, 4)  # hidden behind the lips: low poly
    push = max(surface_front(bvh, v.co.x, v.co.z) + .008 - v.co.y for v in verts)  # every point just behind the skin
    for v in verts:
        v.co.y += push
    set_material(bm, DARK)
    return bm


def build_eyes(bvh):
    """small round eyes sitting in the sockets behind the lenses: white + dark pupil."""
    whites, pupils = bmesh.new(), bmesh.new()
    for s in (1, -1):
        x, z = s * EYE_C[0], EYE_C[1]
        y = surface_front(bvh, x, z) + .012
        ellipsoid(whites, Vector((x, y, z)), (.06, .035, .05), 14, 8)
        ellipsoid(pupils, Vector((x - s * .006, y - .03, z - .004)), (.024, .01, .03), 10, 6)
    set_material(whites, EYEWHITE)
    set_material(pupils, HAIR)
    return whites, pupils


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
    tan, red, dark, stubble = Vector(lin(SKIN_RGB)), Vector(lin((.83, .45, .36))), Vector(lin((.59, .39, .29))), Vector(lin((.39, .29, .23)))
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
    mouth = Vector()  # MouthOpen needs the mouth's centre line: built in add_shape_keys
    sag = Vector()
    if part == P_SKIN:
        w = smoothstep(.8, .55, z) * (1 - g2(x, 0, .12, 1) * front * .5)
        radial = Vector((x, y, 0)).normalized() if abs(x) + abs(y) > 1e-4 else Vector()
        sag = radial * .02 * w + Vector((0, 0, -.025 * w))
    blink, squint = Vector(), Vector()
    if part == P_EYE:  # squash each eye (white + pupil) toward its centre line: a dark slit when shut
        dz = EYE_C[1] - DZ - z
        blink = Vector((0, 0, .9 * dz))
        squint = Vector((0, 0, .55 * dz - .006))
    return {"BrowAngry": brow, "Sneer": sneer, "MouthOpen": mouth, "JowlSag": sag, "EyeBlink": blink, "EyeSquint": squint}


def add_shape_keys(obj):
    mesh = obj.data
    tags = [d.value for d in mesh.attributes["part"].data]
    full = [v.co.copy() for v in mesh.vertices]
    deltas = [shape_deltas(t, c) for t, c in zip(tags, full)]
    # MouthOpen = a jaw drop: everything under the mouth's centre line (lower lip, the lower half of the dark inside, chin)
    # drops, the upper lip lifts a touch, so the dark inside stretches open between them
    inside = sorted({v for p in mesh.polygons if p.material_index == DARK and tags[p.vertices[0]] == P_SKIN for v in p.vertices},
                    key=lambda i: full[i].x)

    def unrecessed_z(i):
        # build_head pulls the slit in to .95 of the radius, which lifts it ~1 cm: compare heights before that pull
        return HEAD_C.z + (full[i].z - HEAD_C.z) / .95 if i in inside_set else full[i].z

    inside_set = set(inside)
    # the slit has an upper and a lower row: its centre line is their midpoint, column by column
    columns = {}
    for i in inside:
        columns.setdefault(round(full[i].x / .02), []).append(unrecessed_z(i))
    line_x = np.array([k * .02 for k in sorted(columns)])
    line_z = np.array([(max(columns[k]) + min(columns[k])) / 2 for k in sorted(columns)])
    for i, (t, c) in enumerate(zip(tags, full)):
        if t != P_SKIN or c.y > -.15:
            continue
        mouth_z = float(np.interp(c.x, line_x, line_z))  # the slit curves down at the corners
        across = math.exp(-(c.x / .2) ** 2)
        if unrecessed_z(i) < mouth_z - 1e-4:
            deltas[i]["MouthOpen"] = Vector((0, .004, -.05 * across * smoothstep(mouth_z - .3, mouth_z - .08, c.z)))
        else:
            deltas[i]["MouthOpen"] = Vector((0, 0, .008 * across * smoothstep(mouth_z + .06, mouth_z, c.z)))
    # the lenses rest shrunk into the top of their rim (clear glasses, eyes visible); LensGlare grows them back as opaque
    # white discs that hide the eyes
    lens = [i for i, t in enumerate(tags) if t == P_LENS]
    base = list(full)
    for side in (1, -1):
        ids = [i for i in lens if full[i].x * side > 0]
        top = max((full[i] for i in ids), key=lambda c: c.z) + Vector((0, 0, .008))
        for i in ids:
            base[i] = top + (full[i] - top) * .03
            mesh.vertices[i].co = base[i]
    obj.shape_key_add(name="Basis", from_mix=False)
    for name in ("BrowAngry", "Sneer", "MouthOpen", "JowlSag", "EyeBlink", "EyeSquint"):
        key = obj.shape_key_add(name=name, from_mix=False)
        for i, d in enumerate(deltas):
            key.data[i].co = base[i] + d[name]
    key = obj.shape_key_add(name="LensGlare", from_mix=False)
    for i in lens:
        key.data[i].co = full[i]


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
    hat = build_hat(hair_bm)
    head_bm.normal_update()
    bvh = BVHTree.FromBMesh(head_bm)
    white_bm, pupil_bm = build_eyes(bvh)
    frame_bm, lens_bm = build_glasses(bvh_of(head_bm, white_bm))  # the glasses clear the eyeballs as well as the skin
    stache_bm = build_mustache(bvh)
    cavity_bm = build_mouth_cavity(bvh, head_bm)

    objects = [make_object("Head", head_bm, mats, P_SKIN, lambda co: head_weights(P_SKIN, co))]
    bake_head(objects[0], mats)
    head_parts = [("Hair", hair_bm, P_HAIR), ("Frames", frame_bm, P_FRAME), ("Lenses", lens_bm, P_LENS), ("Mustache", stache_bm, P_STACHE),
                  ("EyeWhites", white_bm, P_EYE), ("Pupils", pupil_bm, P_EYE), ("MouthCavity", cavity_bm, P_HAIR)] + [(n, bm, P_HAIR) for n, bm in hat.items()]
    for name, bm, part in head_parts:
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
    assert head_tris / tris >= .55, "head (face, glasses, eyes, hat) should hold most of the geometry"

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
