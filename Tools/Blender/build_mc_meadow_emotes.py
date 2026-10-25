# Preview video: Meadow plays every face-sheet expression with a little body emote.
# Run inside Blender: exec(open(r"<repo>/Tools/Blender/build_mc_meadow_emotes.py").read(), {"__name__": "mc"}).
# Rebuilds the character (build_mc_meadow.py), adds a preview skeleton + keyframes, saves Tools/Blender/MC_Meadow_Emotes.blend.
# Render it headless:  blender -b Tools/Blender/MC_Meadow_Emotes.blend -a   ->  Exports/MC_Meadow/Meadow_Emotes.mp4
# shortcut: rigid per-part weights (no weight painting), fine for a preview; the game rig gets proper skinning later.
import math
import os

import bpy
from bpy_extras.anim_utils import action_get_channelbag_for_slot
from mathutils import Matrix, Vector

REPO = r"D:\Akio\GameDev Project\Checkpoint GameJam 2026"
mc = {"__name__": "mc"}
exec(open(os.path.join(REPO, "Tools", "Blender", "build_mc_meadow.py")).read(), mc)

FPS = 24
SEG = 48  # frames per expression
ORDER = ["Neutral", "Talk", "Happy", "Surprised", "Angry", "Sad", "Nervous", "Tired", "Wink", "Blink"]

J = {n: Vector(p) for n, (p, _) in mc["mirrored"]()[0].items()}
BONES = {  # name: (head, tail, parent)
    "Root": ((0, 0, .3), (0, 0, .5), None),
    "Spine": ((0, 0, .5), (0, 0, .8), "Root"),
    "Head": ((0, 0, .8), (0, 0, 1.4), "Spine"),
    "Leg.L": ((.1, 0, .34), (.1, 0, .06), "Root"),
    "Leg.R": ((-.1, 0, .34), (-.1, 0, .06), "Root"),
}
for s, side in (("L", 1), ("R", -1)):
    BONES["UpperArm." + s] = (J["shoulder" + s], J["elbow" + s], "Spine")
    BONES["ForeArm." + s] = (J["elbow" + s], J["hand" + s], "UpperArm." + s)


def build_rig(char):
    arm = bpy.data.objects.new("MC_Meadow_Rig", bpy.data.armatures.new("MC_Meadow_Rig"))
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for name, (h, t, parent) in BONES.items():
        eb = arm.data.edit_bones.new(name)
        eb.head, eb.tail = h, t
        if parent:
            eb.parent = arm.data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")

    # weights: parts from the build's Part_* groups; body vertices blend across the shoulder and elbow
    groups = {n: char.vertex_groups.new(name=n) for n in BONES}
    part = {g.index: g.name for g in char.vertex_groups if g.name.startswith("Part_")}
    shoulder_x, elbow_x = J["shoulderL"].x, J["elbowL"].x

    def arm_weights(co):
        """T-pose arm: spine -> upper arm across the shoulder, upper -> forearm across the elbow (no armpit tearing)."""
        ax, s = abs(co.x), "L" if co.x > 0 else "R"
        if co.z < .58 or ax < shoulder_x - .03:
            return None
        u = min(1, (ax - (shoulder_x - .03)) / .1)
        f = max(0, min(1, (ax - (elbow_x - .03)) / .06))
        return [("Spine", 1 - u), ("UpperArm." + s, u * (1 - f)), ("ForeArm." + s, u * f)]

    for v in char.data.vertices:
        p = next(part[g.group] for g in v.groups if g.group in part)
        co = v.co
        if p == "Part_Head":
            groups["Head"].add([v.index], 1, "REPLACE")
        elif p == "Part_Legs":
            groups["Leg.L" if co.x > 0 else "Leg.R"].add([v.index], 1, "REPLACE")
        else:
            ws = arm_weights(co)
            if ws:
                for name, w in ws:
                    if w > 0:
                        groups[name].add([v.index], w, "REPLACE")
            else:
                h = max(0, min(1, (co.z - .74) / .08))  # neck blends into the head
                sp = max(0, min(1, (co.z - .4) / .12)) * (1 - h)
                for name, w in (("Head", h), ("Spine", sp), ("Root", 1 - h - sp)):
                    if w > 0:
                        groups[name].add([v.index], w, "REPLACE")
    mod = char.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    char.parent = arm
    return arm


# ---------------------------------------------------------------- keyframing helpers
def world_rot(pb, rots):
    """Rotations given in armature space (axis letter, degrees) -> the pose bone's local quaternion."""
    r = Matrix.Identity(3)
    for axis, deg in rots:
        r = Matrix.Rotation(math.radians(deg), 3, axis) @ r
    rest = pb.bone.matrix_local.to_3x3()
    return (rest.inverted() @ r @ rest).to_quaternion()


def key(arm, frame, bone, rots=(), loc=(0, 0, 0)):
    pb = arm.pose.bones[bone]
    pb.rotation_quaternion = world_rot(pb, rots)
    pb.location = pb.bone.matrix_local.to_3x3().inverted() @ Vector(loc)
    pb.keyframe_insert("rotation_quaternion", frame=frame)
    pb.keyframe_insert("location", frame=frame)


def rest(arm, frame):
    for b in BONES:
        key(arm, frame, b)
    arms(arm, frame)  # the mesh is in T-pose; resting = arms down


def face_key(frame, eye_l, eye_r, mouth):
    for mat, cell in (("MC_Face_EyeR", mc["EYES"].index(eye_l)), ("MC_Face_EyeL", mc["EYES"].index(eye_r)),
                      ("MC_Face_Mouth", 8 + mc["MOUTHS"].index(mouth))):
        sock = bpy.data.materials[mat].node_tree.nodes["CellOffset"].inputs["Location"]
        sock.default_value = ((cell % 4) * .25, -(cell // 4) * .25, 0)
        sock.keyframe_insert("default_value", frame=frame)


def expr(frame, name):
    face_key(frame, *mc["EXPRESSIONS"][name])


# Angles are measured from arms hanging down (ARM_DOWN below the T-pose). raise = up/out to the side, fwd = swing forward,
# bend = forearm folds forward at the elbow.
ARM_DOWN = 75


def arms(arm, f, raise_l=0, raise_r=0, fwd_l=0, fwd_r=0, bend_l=0, bend_r=0):
    key(arm, f, "UpperArm.L", [("Y", ARM_DOWN - raise_l), ("X", -fwd_l)])
    key(arm, f, "UpperArm.R", [("Y", raise_r - ARM_DOWN), ("X", -fwd_r)])
    key(arm, f, "ForeArm.L", [("Z", -bend_l)])
    key(arm, f, "ForeArm.R", [("Z", bend_r)])


# ---------------------------------------------------------------- the emotes (head nod = +X, tilt = Y, turn = Z)
def emote(arm, name, f):
    e = f + SEG
    rest(arm, f)
    if name == "Neutral":
        expr(f, "Neutral")
        key(arm, f + 14, "Head", [("Y", 6)])
        key(arm, f + 30, "Head", [("Y", -4)])
        face_key(f + 20, "Closed", "Closed", "Smile")
        expr(f + 23, "Neutral")
    elif name == "Talk":
        expr(f, "Talk")
        for i, m in enumerate(range(f + 2, e - 6, 3)):
            face_key(m, "Open", "Open", ("TalkWide", "TalkSmall", "Smile", "TalkWide", "TalkSmall")[i % 5])
        for i, k in enumerate(range(f + 6, e - 6, 8)):
            key(arm, k, "Head", [("X", 7 if i % 2 else -2)])
            arms(arm, k, raise_r=20, fwd_r=35 if i % 2 else 20, bend_r=55 if i % 2 else 35)
        face_key(e - 6, "Open", "Open", "Smile")
    elif name == "Happy":
        expr(f, "Happy")
        for i, k in enumerate((f + 6, f + 12, f + 18, f + 24, f + 30)):
            up = i % 2 == 1
            key(arm, k, "Root", loc=(0, 0, .16 if up else 0))
            arms(arm, k, raise_l=130 if up else 105, raise_r=130 if up else 105, fwd_l=15, fwd_r=15, bend_l=25, bend_r=25)
            key(arm, k, "Head", [("Y", 8 if up else -8)])
        arms(arm, f + 38, raise_l=90, raise_r=90, fwd_l=15, fwd_r=15)
    elif name == "Surprised":
        expr(f + 2, "Surprised")
        key(arm, f + 6, "Root", loc=(0, .1, .12))
        key(arm, f + 6, "Head", [("X", -14)])
        key(arm, f + 6, "Spine", [("X", -8)])
        arms(arm, f + 6, raise_l=75, raise_r=75, fwd_l=20, fwd_r=20, bend_l=40, bend_r=40)
        key(arm, f + 11, "Root", loc=(0, .12, 0))
        key(arm, f + 36, "Root", loc=(0, .12, 0))
        key(arm, f + 36, "Head", [("X", -10)])
        arms(arm, f + 36, raise_l=65, raise_r=65, fwd_l=20, fwd_r=20, bend_l=40, bend_r=40)
    elif name == "Angry":
        expr(f, "Angry")
        for i, k in enumerate(range(f + 6, e - 8, 4)):
            key(arm, k, "Root", loc=(.025 if i % 2 else -.025, 0, .03 if i % 4 == 0 else 0))
            key(arm, k, "Spine", [("X", 12)])
            key(arm, k, "Head", [("X", 6), ("Z", 4 if i % 2 else -4)])
            arms(arm, k, raise_l=14, raise_r=14, fwd_l=-15, fwd_r=-15, bend_l=70, bend_r=70)
    elif name == "Sad":
        expr(f, "Sad")
        key(arm, f + 14, "Head", [("X", 11), ("Y", 6)])
        key(arm, f + 14, "Spine", [("X", 6)])
        key(arm, f + 14, "Root", loc=(0, 0, -.025))
        arms(arm, f + 14, raise_l=-6, raise_r=-6, fwd_l=10, fwd_r=10)
        key(arm, f + 38, "Head", [("X", 13), ("Y", -4)])
        key(arm, f + 38, "Spine", [("X", 7)])
        key(arm, f + 38, "Root", loc=(0, 0, -.03))
        arms(arm, f + 38, raise_l=-6, raise_r=-6, fwd_l=10, fwd_r=10)
    elif name == "Nervous":
        expr(f, "Nervous")
        for i, k in enumerate(range(f + 6, e - 6, 3)):
            key(arm, k, "Head", [("Z", 9 if i % 2 else -9), ("X", 6)])
            key(arm, k, "Root", loc=(.012 if i % 2 else -.012, 0, 0))
            arms(arm, k, raise_l=10, raise_r=10, fwd_l=55, fwd_r=55, bend_l=85, bend_r=85)
    elif name == "Tired":
        expr(f, "Tired")
        key(arm, f + 18, "Spine", [("X", 6)])
        key(arm, f + 18, "Head", [("X", -10)])
        key(arm, f + 18, "Root", loc=(0, 0, .02))
        face_key(f + 22, "Closed", "Closed", "Shock")  # big sigh
        key(arm, f + 34, "Spine", [("X", 18)])
        key(arm, f + 34, "Head", [("X", 20)])
        key(arm, f + 34, "Root", loc=(0, 0, -.04))
        arms(arm, f + 34, fwd_l=8, fwd_r=8)
        expr(f + 34, "Tired")
    elif name == "Wink":
        expr(f + 4, "Wink")
        key(arm, f + 8, "Head", [("Y", -12)])
        for i, k in enumerate(range(f + 8, e - 6, 5)):
            key(arm, k, "UpperArm.L", [("Y", ARM_DOWN - 100), ("X", -35)])
            key(arm, k, "ForeArm.L", [("Y", -30 if i % 2 else 20)])
        key(arm, e - 8, "Head", [("Y", -10)])
    elif name == "Blink":
        expr(f, "Neutral")
        for b0 in (f + 10, f + 30):
            face_key(b0, "HalfBlink", "HalfBlink", "Smile")
            face_key(b0 + 2, "Closed", "Closed", "Smile")
            face_key(b0 + 4, "HalfBlink", "HalfBlink", "Smile")
            expr(b0 + 6, "Neutral")
        key(arm, f + 20, "Head", [("Z", 10)])
        key(arm, f + 40, "Head", [("Z", -6)])


def labels(cam):
    """One caption per segment, parented to the camera and shown only during its segment."""
    mat = mc["flat_material"]("MC_Label", (.05, .04, .035))
    for i, name in enumerate(ORDER):
        curve = bpy.data.curves.new("Label_" + name, "FONT")
        curve.body = name
        curve.align_x = "CENTER"
        curve.size = .16
        txt = bpy.data.objects.new("Label_" + name, curve)
        bpy.context.scene.collection.objects.link(txt)
        txt.data.materials.append(mat)
        txt.parent = cam
        txt.location = (0, -1.02, -3)
        f = 1 + i * SEG
        for frame, hidden in ((1, True), (f, False), (f + SEG, True)):
            txt.hide_render = txt.hide_viewport = hidden
            txt.keyframe_insert("hide_render", frame=frame)
            txt.keyframe_insert("hide_viewport", frame=frame)


def main():
    char = bpy.data.objects["MC_Meadow"]
    arm = build_rig(char)
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
    for i, name in enumerate(ORDER):
        emote(arm, name, 1 + i * SEG)
    end = 1 + len(ORDER) * SEG
    rest(arm, end)
    expr(end, "Neutral")
    # face swaps snap; body moves ease
    for m in ("MC_Face_EyeR", "MC_Face_EyeL", "MC_Face_Mouth"):
        ad = bpy.data.materials[m].node_tree.animation_data
        for fc in action_get_channelbag_for_slot(ad.action, ad.action_slot).fcurves:
            for kp in fc.keyframe_points:
                kp.interpolation = "CONSTANT"

    scene = bpy.context.scene
    scene.frame_start, scene.frame_end = 1, end + 8
    scene.render.fps = FPS
    scene.render.resolution_x = scene.render.resolution_y = 720
    scene.eevee.taa_render_samples = 16
    for o in list(scene.objects):
        if o.name.startswith(("ME_", "House", "Tree", "Prop", "Bush")) or (o.type == "MESH" and o.name not in ("MC_Meadow", "Ground")):
            o.hide_render = True
    cam = scene.camera
    d = Vector((math.sin(.3) * math.cos(.1), -math.cos(.3) * math.cos(.1), math.sin(.1)))
    cam.location = Vector((0, 0, .85)) + d * 12
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = 2.5
    labels(cam)

    img = scene.render.image_settings
    if hasattr(img, "media_type"):
        img.media_type = "VIDEO"
    img.file_format = "FFMPEG"
    scene.render.ffmpeg.format = "MPEG4"
    scene.render.ffmpeg.codec = "H264"
    scene.render.ffmpeg.constant_rate_factor = "HIGH"
    scene.render.use_file_extension = False
    scene.render.filepath = os.path.join(REPO, "Exports", "MC_Meadow", "Meadow_Emotes.mp4")
    scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(REPO, "Tools", "Blender", "MC_Meadow_Emotes.blend"))
    return {"frames": scene.frame_end, "seconds": round(scene.frame_end / FPS, 1), "blender": bpy.app.binary_path}


result = main()
