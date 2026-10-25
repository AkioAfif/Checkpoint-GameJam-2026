# Rigs Meadow for Unity and exports Assets/_Project/Art/Models/Meadow/{MC_Meadow.fbx, MC_Face.png}.
# Run inside Blender: exec(open(r"<repo>/Tools/Blender/export_mc_meadow.py").read(), {"__name__": "mc"}).
# Bones match the Chibi rig (Hips/Head/ArmL/ArmR/LegL/LegR, Hips at 0.42 m) so CharacterBuilder.PlayerRigPrefab() can re-bind
# the mesh to identity-rotation bones and Chibi.controller drives it. The Chibi clips treat identity as "arms hanging", so the
# T-pose mesh is exported with its arms baked down ARM_DOWN degrees.
import math
import os
import shutil

import bpy
from mathutils import Matrix, Vector

REPO = r"D:\Akio\GameDev Project\Checkpoint GameJam 2026"
OUT_DIR = os.path.join(REPO, "Assets", "_Project", "Art", "Models", "Meadow")
mc = {"__name__": "mc"}
exec(open(os.path.join(REPO, "Tools", "Blender", "build_mc_meadow.py")).read(), mc)

ARM_DOWN = 62  # hands end beside the overall's hem, clear of it
J = {n: Vector(p) for n, (p, _) in mc["mirrored"]()[0].items()}
HIPS = Vector((0, 0, .42))
NECK = Vector((0, 0, .8))
BONES = {  # name: (head, tail, parent); L = the character's left = Blender +X = Unity -X
    "Hips": (HIPS, NECK, None),
    "Head": (NECK, NECK + Vector((0, 0, .5)), "Hips"),
    "LegL": (Vector((.1, 0, .34)), Vector((.1, 0, .06)), None),
    "LegR": (Vector((-.1, 0, .34)), Vector((-.1, 0, .06)), None),
}


def arm_rotation(side):
    """Rotation that swings the T-pose arm on 'side' (+1 = L) down by ARM_DOWN around its shoulder."""
    return Matrix.Rotation(math.radians(ARM_DOWN * side), 3, "Y")


for s, side in (("L", 1), ("R", -1)):
    shoulder = J["shoulder" + s]
    BONES["Arm" + s] = (shoulder, shoulder + arm_rotation(side) @ (J["hand" + s] - shoulder), "Hips")


def weights(char):
    """Per-vertex {bone: weight}. Parts come from the build's Part_* groups; arms blend into the Hips across the shoulder."""
    part = {g.index: g.name for g in char.vertex_groups if g.name.startswith("Part_")}
    shoulder_x = J["shoulderL"].x
    out = []
    for v in char.data.vertices:
        p = next(part[g.group] for g in v.groups if g.group in part)
        co = v.co
        side = "L" if co.x > 0 else "R"
        if p == "Part_Head":
            out.append({"Head": 1})
        elif p == "Part_Legs":
            out.append({"Leg" + side: 1})
        elif co.z > .58 and abs(co.x) > shoulder_x - .03:
            u = min(1, (abs(co.x) - (shoulder_x - .03)) / .1)
            out.append({"Hips": 1 - u, "Arm" + side: u})
        else:
            h = max(0, min(1, (co.z - .74) / .08))  # neck blends into the head
            out.append({"Head": h, "Hips": 1 - h})
    return out


def bake_arms_down(char, ws):
    """Linear-blend-skin the T-pose arms down so the exported rest pose is the Chibi 'arms hanging' pose."""
    rots = {"ArmL": (arm_rotation(1), J["shoulderL"]), "ArmR": (arm_rotation(-1), J["shoulderR"])}
    for v, w in zip(char.data.vertices, ws):
        co = v.co.copy()
        new = Vector()
        for bone, wt in w.items():
            if bone in rots:
                r, pivot = rots[bone]
                new += wt * (r @ (co - pivot) + pivot)
            else:
                new += wt * co
        v.co = new
    char.data.update()


def build_armature():
    arm = bpy.data.objects.new("Armature", bpy.data.armatures.new("Armature"))
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for name, (h, t, _) in BONES.items():
        eb = arm.data.edit_bones.new(name)
        eb.head, eb.tail = h, t
    for name, (_, _, parent) in BONES.items():
        if parent:
            arm.data.edit_bones[name].parent = arm.data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def main():
    char = bpy.data.objects["MC_Meadow"]
    ws = weights(char)
    bake_arms_down(char, ws)
    for g in list(char.vertex_groups):
        char.vertex_groups.remove(g)
    groups = {n: char.vertex_groups.new(name=n) for n in BONES}
    for v, w in zip(char.data.vertices, ws):
        for bone, wt in w.items():
            if wt > 0:
                groups[bone].add([v.index], wt, "REPLACE")
    arm = build_armature()
    char.parent = arm
    char.modifiers.new("Armature", "ARMATURE").object = arm

    os.makedirs(OUT_DIR, exist_ok=True)
    for o in bpy.context.scene.objects:
        o.select_set(o in (arm, char))
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_DIR, "MC_Meadow.fbx"), use_selection=True, object_types={"ARMATURE", "MESH"},
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             use_mesh_modifiers=False, mesh_smooth_type="OFF", use_tspace=False, add_leaf_bones=False,
                             use_armature_deform_only=True, bake_anim=False, path_mode="STRIP")
    shutil.copyfile(mc["FACE_SHEET"], os.path.join(OUT_DIR, "MC_Face.png"))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(REPO, "Tools", "Blender", "MC_Meadow_Rigged.blend"))
    lowest_hand = min(v.co.z for v, w in zip(char.data.vertices, ws) if "ArmL" in w and w["ArmL"] > .9)
    return {"tris": sum(len(p.vertices) - 2 for p in char.data.polygons), "slots": [s.material.name for s in char.material_slots],
            "lowest_hand_z": round(lowest_hand, 3)}


result = main()
