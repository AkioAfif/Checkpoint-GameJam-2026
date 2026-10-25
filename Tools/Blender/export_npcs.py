# Rigs the six chosen villagers (build_npcs_final.py) for Unity and exports Assets/_Project/Art/Models/Npcs/<Name>.fbx.
# Run inside Blender: exec(open(r"<repo>/Tools/Blender/export_npcs.py").read(), {"__name__": "mc"}).
# Same contract as export_mc_meadow.py: Chibi bone names (Hips at 0.42 m, Head, ArmL/ArmR at the shoulders, LegL/LegR), arms
# resting down so identity = Chibi "arms hanging", the variant's scale baked into the mesh so the rig root stays at scale 1.
# Weights come from a T-pose twin with identical topology (built with the hand props kept), copied per vertex index.
import os

import bpy
from mathutils import Matrix, Vector

REPO = r"D:\Akio\GameDev Project\Checkpoint GameJam 2026"
OUT_DIR = os.path.join(REPO, "Assets", "_Project", "Art", "Models", "Npcs")
final = {"__name__": "npc_final"}
exec(open(os.path.join(REPO, "Tools", "Blender", "build_npcs_final.py")).read(), final)
kit = final["kit"]
HIPS_Y = .42


def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    for block in (bpy.data.meshes, bpy.data.armatures):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def weights(tpose, info):
    """Per-vertex {bone: weight}, from the T-pose twin (arms out, so arm vs torso is unambiguous)."""
    B = info["B"]
    part = {g.index: g.name[5:] for g in tpose.vertex_groups if g.name.startswith("Part_")}
    neck, neck_len = B["neck_base"], info["P"]["neck"] + .04
    out = []
    for v in tpose.data.vertices:
        group = next(part[g.group] for g in v.groups if g.group in part)
        co, side = v.co, ("L" if v.co.x > 0 else "R")
        if group == "Head":
            out.append({"Head": 1})
        elif group == "Legs":
            out.append({"Leg" + side: 1})
        elif group in ("HandL", "HandR"):
            out.append({"Arm" + group[-1]: 1})
        elif group == "Torso":
            out.append({"Hips": 1})
        else:  # body + raised garments: the arm blends in across the shoulder, the neck into the head
            u = B["arm_weight"](co)
            h = max(0, min(1, (co.z - neck) / neck_len))
            out.append({"Arm" + side: u, "Hips": (1 - u) * (1 - h), "Head": (1 - u) * h})
    return out


def bones(info):
    P, B, s = info["P"], info["B"], info["P"]["scale"]
    head = Vector((0, 0, B["neck_base"] * s))
    out = {"Hips": (Vector((0, 0, HIPS_Y)), head, None), "Head": (head, head + Vector((0, 0, .5 * s)), "Hips")}
    for side, k in (("L", 1), ("R", -1)):
        shoulder = Vector((k * P["shoulder_x"], 0, B["sz"])) * s
        out["Arm" + side] = (shoulder, Vector(B["hands"][k]) * s, "Hips")
        out["Leg" + side] = (Vector((k * P["leg_x"], 0, P["hem"] + .03)) * s, Vector((k * P["leg_x"], 0, .05)) * s, None)
    return out


def build_armature(spec):
    arm = bpy.data.objects.new("Armature", bpy.data.armatures.new("Armature"))
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for name, (h, t, _) in spec.items():
        eb = arm.data.edit_bones.new(name)
        eb.head, eb.tail = h, t
    for name, (_, _, parent) in spec.items():
        if parent:
            arm.data.edit_bones[name].parent = arm.data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def export_one(npc, P, L):
    clear_scene()
    kit["_mats"].clear()
    posed, info = kit["build_villager"](npc, P, L)
    tpose, tinfo = kit["build_villager"](npc + "_T", P, L, tpose=True, keep_hand_props=True)
    assert len(posed.data.vertices) == len(tpose.data.vertices), f"{npc}: T-pose twin topology differs"
    ws = weights(tpose, tinfo)
    bpy.data.objects.remove(tpose)

    s = info["P"]["scale"]
    posed.data.transform(Matrix.Scale(s, 4))  # bake the size so the rig root (and the Chibi Hips height) stays unscaled
    posed.scale = (1, 1, 1)
    for g in list(posed.vertex_groups):
        posed.vertex_groups.remove(g)
    spec = bones(info)
    groups = {n: posed.vertex_groups.new(name=n) for n in spec}
    for v, w in zip(posed.data.vertices, ws):
        for bone, wt in w.items():
            if wt > 0:
                groups[bone].add([v.index], wt, "REPLACE")
    arm = build_armature(spec)
    posed.parent = arm
    posed.modifiers.new("Armature", "ARMATURE").object = arm

    for o in bpy.context.scene.objects:
        o.select_set(o in (arm, posed))
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_DIR, f"{npc}.fbx"), use_selection=True, object_types={"ARMATURE", "MESH"},
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             use_mesh_modifiers=False, mesh_smooth_type="OFF", use_tspace=False, add_leaf_bones=False,
                             use_armature_deform_only=True, bake_anim=False, path_mode="STRIP")
    top = max(v.co.z for v in posed.data.vertices)
    unweighted = sum(1 for v in posed.data.vertices if not v.groups)
    return {"tris": info["tris"], "height": round(top, 2), "unweighted": unweighted, "slots": len(posed.material_slots)}


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    report = {npc: export_one(npc, P, L) for npc, _, _, P, L in final["chosen_specs"]()}
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(REPO, "Tools", "Blender", "NPC_Rigged_Last.blend"))
    return report


result = main()
