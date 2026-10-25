# Main character "Meadow", modelled from scratch. Upper body = one continuous surface grown from a skin-modifier skeleton, so the arms
# and hands flow out of the torso; its bottom is cut flat into an overall hem that straight legs tuck up into. Clothing = material regions.
# Run inside Blender: exec(open(r"<repo>/Tools/Blender/build_mc_meadow.py").read(), {"__name__": "mc"}).
# Writes Exports/MC_Meadow/*.png + Tools/Blender/MC_Meadow.blend. Units = metres, Z up, faces -Y, ~1.63 m with the hat.
import math
import os

import bmesh
import bpy
from mathutils import Matrix, Vector

REPO = r"D:\Akio\GameDev Project\Checkpoint GameJam 2026"
OUT = os.path.join(REPO, "Exports", "MC_Meadow")

# Linear colours from the farm glbs (M_Wall, M_Shutter, M_Roof, ...) plus skin/hair/straw.
COLORS = {
    "Skin": (.9, .55, .36), "Tee": (.896, .768, .558), "Denim": (.076, .262, .254), "Boot": (.254, .091, .03),
    "Hair": (.156, .068, .025), "Straw": (.82, .6, .26), "Band": (.584, .095, .05), "Button": (1, .687, .25),
    "Blush": (.9, .35, .3),
}

# Face sheet made by make_face_sheet.py (same cell order). Each eye/mouth patch is UV-mapped to cell 0 and has its own material;
# an expression = a UV offset per material (Blender: Mapping node; Unity: _BaseMap offset).
FACE_SHEET = os.path.join(OUT, "MC_Face.png")
EYES = ["Open", "HalfBlink", "Closed", "Happy", "Surprised", "Angry", "Sad", "Squint"]
MOUTHS = ["Smile", "TalkSmall", "TalkWide", "Grin", "Frown", "Flat", "Shock", "Wavy"]
EXPRESSIONS = {  # name -> (viewer-left eye, viewer-right eye, mouth)
    "Neutral": ("Open", "Open", "Smile"), "Blink": ("Closed", "Closed", "Smile"), "Talk": ("Open", "Open", "TalkWide"),
    "Happy": ("Happy", "Happy", "Grin"), "Surprised": ("Surprised", "Surprised", "Shock"), "Angry": ("Angry", "Angry", "Frown"),
    "Sad": ("Sad", "Sad", "Frown"), "Nervous": ("Squint", "Squint", "Wavy"), "Tired": ("HalfBlink", "HalfBlink", "Flat"),
    "Wink": ("Open", "Happy", "TalkSmall"),
}

HEAD_C = Vector((0, 0, 1.06))  # head bottom (~0.73) sinks onto the neck so it never floats
HEAD_R = Vector((.36, .33, .33))  # wider than tall/deep, like a villager head

# Skeleton for the skin modifier: name -> (position, (radius x, radius y)). Right side is mirrored from the "L" entries.
JOINTS = {
    "pelvis": ((0, 0, .28), (.25, .21)),
    "belly": ((0, 0, .47), (.24, .2)),
    "chest": ((0, 0, .63), (.2, .17)),
    "neck": ((0, 0, .86), (.08, .08)),  # reaches up inside the head
    # arms straight out (T-pose): far from the belly so no forearm face is mistaken for torso, and the standard rig bind pose
    "shoulderL": ((.13, 0, .68), (.08, .08)),
    "elbowL": ((.3, 0, .68), (.062, .062)),
    "wristL": ((.43, -.005, .68), (.055, .055)),
    "handL": ((.5, -.01, .68), (.07, .075)),
}
BONES = [("pelvis", "belly"), ("belly", "chest"), ("chest", "neck"), ("chest", "shoulderL"), ("shoulderL", "elbowL"),
         ("elbowL", "wristL"), ("wristL", "handL")]
HEM_Z = .3  # the torso is cut flat here; the legs start just inside it


def flat_material(name, lin):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*lin, 1)
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*lin, 1)
    bsdf.inputs["Roughness"].default_value = .9
    return m


def materials():
    return {name: flat_material("MC_" + name, lin) for name, lin in COLORS.items()}


def mirrored():
    joints = {}
    for n, (p, r) in JOINTS.items():
        joints[n] = (Vector(p), r)
        if n.endswith("L"):
            joints[n[:-1] + "R"] = (Vector((-p[0], p[1], p[2])), r)
    bones = list(BONES) + [(a[:-1] + "R" if a.endswith("L") else a, b[:-1] + "R") for a, b in BONES if b.endswith("L")]
    return joints, bones


def new_object(name, mesh):
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def bake(obj, name):
    """Evaluated (modifiers applied) copy of obj as a new mesh object; the source is removed."""
    dg = bpy.context.evaluated_depsgraph_get()
    mesh = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    mesh.name = name
    bpy.data.objects.remove(obj)
    return new_object(name, mesh)


def assign(obj, mats, pick):
    """pick(face_centre, face_normal) -> material name."""
    names = list(dict.fromkeys(COLORS))
    for n in names:
        obj.data.materials.append(mats[n])
    mw = obj.matrix_world
    for f in obj.data.polygons:
        f.material_index = names.index(pick(mw @ f.center, (mw.to_3x3() @ f.normal).normalized()))


# ---------------------------------------------------------------- body
def body(mats):
    joints, bones = mirrored()
    names = list(joints)
    mesh = bpy.data.meshes.new("SkinSrc")
    mesh.from_pydata([joints[n][0] for n in names], [(names.index(a), names.index(b)) for a, b in bones], [])
    obj = new_object("SkinSrc", mesh)
    skin = obj.modifiers.new("Skin", "SKIN")
    skin.branch_smoothing = .6
    skin.use_smooth_shade = True
    for i, n in enumerate(names):
        sv = mesh.skin_vertices[0].data[i]
        sv.radius = joints[n][1]
        sv.use_root = n == "pelvis"
    sub = obj.modifiers.new("Subsurf", "SUBSURF")
    sub.levels = sub.render_levels = 1
    obj = bake(obj, "MC_Meadow_Body")
    for v in obj.data.vertices:
        v.co.z = max(v.co.z, HEM_Z)
    obj.data.shade_smooth()

    segs = [(joints[a][0], joints[b][0], max(joints[a][1]), max(joints[b][1]), a, b) for a, b in bones]

    def nearest(p):
        """Segment whose skin surface is closest (distance minus radius), so torso flanks beside a hanging arm stay torso."""
        best = None
        for a, b, ra, rb, na, nb in segs:
            ab = b - a
            t = max(0, min(1, (p - a).dot(ab) / ab.length_squared))
            d = (p - (a + ab * t)).length - (ra + (rb - ra) * t)
            if best is None or d < best[0]:
                best = (d, na, nb, t)
        return best[1:]

    def pick(c, n):
        a, b, t = nearest(c)
        b0 = b.rstrip("LR")  # segment named by its lower joint: "elbow" = upper arm, "wrist" = forearm
        if b0 == "shoulder" or (b0 == "elbow" and t < .6):
            return "Tee"  # short sleeve
        if b0 in ("elbow", "wrist", "hand"):
            return "Skin"
        if b0 == "neck":
            return "Skin" if c.z > .74 else "Tee"
        # torso: overall trousers + front bib + two straps over the tee
        if c.z < .5:
            return "Denim"
        if c.y < 0 and abs(c.x) < .13 and c.z < .64:
            return "Denim"
        if abs(abs(c.x) - .085) < .03 and c.z < .74:
            return "Denim"
        return "Tee"

    assign(obj, mats, pick)
    return obj


def legs(mats):
    """Straight overall legs that disappear up under the hem, on rounded boots."""
    bm = bmesh.new()
    for s in (1, -1):
        leg = bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=10, radius1=.068, radius2=.072, depth=.3)["verts"]
        bmesh.ops.translate(bm, vec=(s * .1, 0, .2), verts=leg)
        boot = bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=1)["verts"]
        bmesh.ops.transform(bm, matrix=Matrix.Translation((s * .1, -.03, .055)) @ Matrix.Diagonal((.088, .12, .058, 1)), verts=boot)
    mesh = bpy.data.meshes.new("MC_Meadow_Legs")
    bm.to_mesh(mesh)
    bm.free()
    obj = new_object("MC_Meadow_Legs", mesh)
    obj.data.shade_smooth()
    assign(obj, mats, lambda c, n: "Boot" if c.z < .1 else "Denim")
    return obj


# ---------------------------------------------------------------- head, face, hair
def ellipsoid_point(x, z):
    """Front surface point of the head at (x, z) world, and its normal."""
    lx, lz = x / HEAD_R.x, (z - HEAD_C.z) / HEAD_R.z
    ly = -math.sqrt(max(0, 1 - lx * lx - lz * lz))
    p = Vector((x, HEAD_C.y + ly * HEAD_R.y, z))
    n = Vector((lx / HEAD_R.x, ly / HEAD_R.y, lz / HEAD_R.z)).normalized()
    return p, n


def head(mats):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=18, v_segments=12, radius=1)
    for v in bm.verts:
        v.co = Vector((v.co.x * HEAD_R.x, v.co.y * HEAD_R.y, v.co.z * HEAD_R.z)) + HEAD_C
    # flatten the lower face a touch so cheeks read
    for v in bm.verts:
        if v.co.y < -.2 and v.co.z < HEAD_C.z:
            v.co.y += .03 * (HEAD_C.z - v.co.z) / HEAD_R.z
    mesh = bpy.data.meshes.new("MC_Meadow_Head")
    bm.to_mesh(mesh)
    bm.free()
    obj = new_object("MC_Meadow_Head", mesh)
    obj.data.shade_smooth()
    assign(obj, mats, lambda c, n: "Skin")
    return obj


def head_front(x, z, push):
    """Point on the head's front (matching head()'s lower-face flattening), pushed out along the normal."""
    p, n = ellipsoid_point(x, z)
    if p.y < -.2 and z < HEAD_C.z:
        p.y += .03 * (HEAD_C.z - z) / HEAD_R.z
    return p + n * push


def face_material(name):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    nt = m.node_tree
    nt.nodes.clear()
    uv = nt.nodes.new("ShaderNodeTexCoord")
    mapping = nt.nodes.new("ShaderNodeMapping")
    mapping.name = "CellOffset"
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(FACE_SHEET, check_existing=True)
    tex.image.reload()
    tex.extension = "CLIP"
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = .9
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(uv.outputs["UV"], mapping.inputs["Vector"])
    nt.links.new(mapping.outputs["Vector"], tex.inputs["Vector"])
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    m.surface_render_method = "DITHERED"
    return m


def face_quad(name, x, z, size, mirror, grid=4, push=.004):
    """Square patch curved onto the head, UVs filling sheet cell 0 (top-left); mirror flips it for the right eye."""
    mesh = bpy.data.meshes.new(name)
    verts, faces, uvs = [], [], []
    for j in range(grid + 1):
        for i in range(grid + 1):
            verts.append(head_front(x + (i / grid - .5) * size, z + (j / grid - .5) * size, push))
    for j in range(grid):
        for i in range(grid):
            a = j * (grid + 1) + i
            faces.append((a, a + 1, a + grid + 2, a + grid + 1))
    mesh.from_pydata(verts, [], faces)
    layer = mesh.uv_layers.new(name="UVMap")
    for loop in mesh.loops:
        i, j = loop.vertex_index % (grid + 1), loop.vertex_index // (grid + 1)
        u = i / grid
        layer.data[loop.index].uv = (((1 - u) if mirror else u) * .25, .75 + j / grid * .25)
    obj = new_object(name, mesh)
    obj.data.materials.append(face_material("MC_Face_" + name))
    return obj


def set_expression(name):
    eye_l, eye_r, mouth = EXPRESSIONS[name]
    for mat, cell in (("MC_Face_EyeR", EYES.index(eye_l)), ("MC_Face_EyeL", EYES.index(eye_r)), ("MC_Face_Mouth", 8 + MOUTHS.index(mouth))):
        bpy.data.materials[mat].node_tree.nodes["CellOffset"].inputs["Location"].default_value = ((cell % 4) * .25, -(cell // 4) * .25, 0)


def face_patch(name, x, z, size, mat, mats, push=.004):
    """Flat oval disc sitting on the head surface (blush)."""
    p, n = ellipsoid_point(x, z)
    bm = bmesh.new()
    bmesh.ops.create_circle(bm, cap_ends=True, segments=10, radius=1)
    for v in bm.verts:
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], 0))
    rot = n.to_track_quat("Z", "Y").to_matrix().to_4x4()
    bmesh.ops.transform(bm, matrix=Matrix.Translation(p + n * push) @ rot, verts=bm.verts)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = new_object(name, mesh)
    assign(obj, mats, lambda c, nn: mat)
    return obj


def face(mats):
    # named from the character's side: EyeR sits on the viewer's left (-X, cell as drawn), EyeL on the viewer's right (+X, mirrored)
    parts = [face_quad("EyeR", -.125, HEAD_C.z - .01, .2, False), face_quad("EyeL", .125, HEAD_C.z - .01, .2, True),
             face_quad("Mouth", 0, HEAD_C.z - .135, .17, False)]
    for s in (1, -1):
        parts.append(face_patch("BlushL" if s > 0 else "BlushR", s * .2, HEAD_C.z - .1, (.05, .03), "Blush", mats))
    set_expression("Neutral")
    return parts


def hair(mats):
    """Head shell with the face cut out, pushed out and given thickness, so it hugs the skull with no gaps."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=18, v_segments=12, radius=1)
    kill = []
    for f in bm.faces:
        n = f.calc_center_median().normalized()
        face_oval = n.y < -.3 and n.z < .22 and abs(n.x) < .8
        under = n.z < -.35 or (n.z < -.05 and n.y < .3)
        if face_oval or under:
            kill.append(f)
    bmesh.ops.delete(bm, geom=kill, context="FACES")
    for v in bm.verts:
        d = v.co.normalized()
        v.co = Vector((d.x * (HEAD_R.x + .025), d.y * (HEAD_R.y + .025), d.z * (HEAD_R.z + .03))) + HEAD_C
    mesh = bpy.data.meshes.new("MC_Meadow_Hair")
    bm.to_mesh(mesh)
    bm.free()
    obj = new_object("MC_Meadow_Hair", mesh)
    obj.data.shade_smooth()
    sol = obj.modifiers.new("Solidify", "SOLIDIFY")
    sol.thickness = .025
    sol.offset = 1
    obj = bake(obj, "MC_Meadow_Hair")
    obj.data.shade_smooth()
    assign(obj, mats, lambda c, n: "Hair")
    return obj


# ---------------------------------------------------------------- hat (one lathed surface)
def hat(mats):
    base = HEAD_C.z + .25
    # (radius, height) closed profile: brim underside -> brim lip -> brim top -> crown wall -> crown top -> inside
    prof = [(0, -.01), (.25, -.01), (.3, -.03), (.54, -.05), (.57, -.035), (.56, -.015), (.32, .015), (.27, .03),
            (.26, .1), (.25, .2), (.2, .225), (0, .23)]
    seg = 20
    bm = bmesh.new()
    rings = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        rings.append([bm.verts.new((r * math.cos(a), r * math.sin(a), base + h)) for r, h in prof])
    for i in range(seg):
        r0, r1 = rings[i], rings[(i + 1) % seg]
        for j in range(len(prof) - 1):
            bm.faces.new((r0[j], r1[j], r1[j + 1], r0[j + 1]))
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new("MC_Meadow_Hat")
    bm.to_mesh(mesh)
    bm.free()
    obj = new_object("MC_Meadow_Hat", mesh)
    hz = base
    assign(obj, mats, lambda c, n: "Band" if hz + .03 < c.z < hz + .1 and abs(n.z) < .5 else "Straw")
    return obj


def buttons(mats, body_obj):
    parts = []
    for s in (1, -1):
        # snap onto the bib by casting from the front at the strap's x
        hit, loc, _, _ = body_obj.ray_cast(Vector((s * .085, -1, .615)), Vector((0, 1, 0)))
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=5, radius=.022)
        bmesh.ops.transform(bm, matrix=Matrix.Translation(loc + Vector((0, -.004, 0))) @ Matrix.Diagonal((1, .5, 1, 1)), verts=bm.verts)
        mesh = bpy.data.meshes.new("Button")
        bm.to_mesh(mesh)
        bm.free()
        o = new_object("Button", mesh)
        assign(o, mats, lambda c, n: "Button")
        parts.append(o)
    return parts


def join(objs, name):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
        # remember which part each vertex came from (Part_Body / Part_Legs / Part_Head) for skinning
        part = "Legs" if "Legs" in o.name else "Body" if ("Body" in o.name or o.name.startswith("Button")) else "Head"
        o.vertex_groups.new(name="Part_" + part).add(range(len(o.data.vertices)), 1, "REPLACE")
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = obj.data.name = name
    # drop duplicate material slots created by joining
    bpy.ops.object.material_slot_remove_unused()
    obj.data.uv_layers["UVMap"].active_render = True  # the face sheet samples it
    return obj


# ---------------------------------------------------------------- scene + renders
def import_refs():
    imported = []
    refs = {"houses/house.glb": (-6.5, 6), "trees/tree_1.glb": (6, 6), "props/prop_barrel.glb": (-1.4, .6),
            "props/prop_mailbox.glb": (1.4, .4), "bushes/bush_1.glb": (-2.6, 1.6)}
    for rel, (x, y) in refs.items():
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=os.path.join(REPO, "Assets", "Ilkham", "models", rel))
        for o in set(bpy.data.objects) - before:
            imported.append(o)
            if o.parent is None:
                o.location = (x, y, 0)
    return imported


def main():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    for m in list(bpy.data.meshes):
        if m.users == 0:
            bpy.data.meshes.remove(m)
    os.makedirs(OUT, exist_ok=True)
    mats = materials()
    body_obj = body(mats)
    char = join([body_obj, legs(mats), head(mats), hair(mats), hat(mats), *face(mats), *buttons(mats, body_obj)], "MC_Meadow")
    refs = import_refs()

    ground = new_object("Ground", bpy.data.meshes.new("Ground"))
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=30)
    bm.to_mesh(ground.data)
    bm.free()
    ground.data.materials.append(flat_material("MC_Ground", (.205, .479, .06)))

    # EEVEE (Workbench ignores the face sheet's UV offsets)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.view_settings.view_transform = "Standard"
    img = scene.render.image_settings
    if hasattr(img, "media_type"):
        img.media_type = "IMAGE"  # the emote script leaves the scene on video output
    img.file_format = "PNG"
    scene.render.use_file_extension = True
    scene.eevee.taa_render_samples = 32
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (.55, .62, .7, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = .55
    sun = new_object("Sun", bpy.data.lights.new("Sun", "SUN"))
    sun.data.energy = 2.6
    sun.rotation_euler = (math.radians(50), 0, math.radians(25))
    cam = new_object("Cam", bpy.data.cameras.new("Cam"))
    cam.data.type = "ORTHO"
    scene.camera = cam

    def shot(name, target, az, el, ortho, res):
        d = Vector((math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)))
        cam.location = Vector(target) + d * 12
        cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        cam.data.ortho_scale = ortho
        scene.render.resolution_x, scene.render.resolution_y = res
        scene.render.filepath = os.path.join(OUT, name + ".png")
        bpy.ops.render.render(write_still=True)

    shot("00_with_farm", (0, 1.5, 1), .35, .28, 7, (1600, 900))
    for o in refs:
        o.hide_render = True
    for view, az in (("front", 0), ("threequarter", .75), ("side", 1.5708), ("back", 3.1416)):
        shot("Meadow_" + view, (0, 0, .82), az, .1, 2.0, (900, 900))
    shot("Meadow_arm_detail", (.2, 0, .55), .9, .15, .8, (900, 900))
    for name in EXPRESSIONS:
        set_expression(name)
        shot("Face_" + name, (0, 0, HEAD_C.z - .02), .22, .05, .95, (512, 512))
    set_expression("Neutral")
    for o in refs:
        o.hide_render = False

    tris = sum(len(p.vertices) - 2 for p in char.data.polygons)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(REPO, "Tools", "Blender", "MC_Meadow.blend"))
    return {"tris": tris, "height": round(char.dimensions.z, 3), "slots": [s.material.name for s in char.material_slots]}


result = main()
