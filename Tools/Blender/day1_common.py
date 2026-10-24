# Shared helpers for build_truck.py / build_fabricator.py (Day 1 hero props).
# Units = metres (1 Blender unit = 1 Unity unit). Blender Z up, the prop faces -Y, exported with the same axis settings as the
# Distributor (forward -Z, up Y, transforms baked) so it lands in Unity facing +Z with scale 1.
import math
import os
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PREVIEW_DIR = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else None


def srgb_to_linear(c):
    return tuple(((v + .055) / 1.055) ** 2.4 if v > .04045 else v / 12.92 for v in c)


def make_materials(colors):
    """colors: {name: (r, g, b)} in sRGB. Returns {name: material}; Unity remaps the slots by name."""
    mats = {}
    for name, rgb in colors.items():
        m = bpy.data.materials.new(name)
        m.diffuse_color = (*srgb_to_linear(rgb), 1.0)
        mats[name] = m
    return mats


class Builder:
    """Collects rounded primitives into one bmesh, with a material slot per part."""

    def __init__(self, mats):
        self.mats = mats
        self.slot_names = []
        self.bm = bmesh.new()

    def _slot(self, mat_name):
        if mat_name not in self.slot_names:
            self.slot_names.append(mat_name)
        return self.slot_names.index(mat_name)

    def _append(self, part, mat_name, loc, rot, smooth):
        slot = self._slot(mat_name)
        bmesh.ops.transform(part, matrix=Matrix.Translation(loc) @ Euler([math.radians(a) for a in rot], "XYZ").to_matrix().to_4x4(), verts=part.verts)
        bmesh.ops.recalc_face_normals(part, faces=part.faces)
        for f in part.faces:
            f.material_index = slot
            f.smooth = smooth
        tmp = bpy.data.meshes.new("tmp")
        part.to_mesh(tmp)
        part.free()
        self.bm.from_mesh(tmp)
        bpy.data.meshes.remove(tmp)

    def box(self, size, loc, mat, bevel=.05, rot=(0, 0, 0), smooth=False):
        part = bmesh.new()
        bmesh.ops.create_cube(part, size=2.0)
        for v in part.verts:
            v.co.x *= size[0] / 2
            v.co.y *= size[1] / 2
            v.co.z *= size[2] / 2
        if bevel > 0:
            bmesh.ops.bevel(part, geom=list(part.edges), offset=bevel, segments=2, affect="EDGES", profile=.5)
        self._append(part, mat, loc, rot, smooth)

    def cylinder(self, radius, depth, loc, mat, axis="Z", segments=24, radius2=None, bevel=0.0):
        """Cylinder (or cone frustum when radius2 is set). axis = world axis it points along."""
        part = bmesh.new()
        bmesh.ops.create_cone(part, cap_ends=True, cap_tris=False, segments=segments, radius1=radius, radius2=radius if radius2 is None else radius2, depth=depth)
        if bevel > 0:
            bmesh.ops.bevel(part, geom=[e for e in part.edges if len(e.link_faces) == 2 and (e.link_faces[0].normal - e.link_faces[1].normal).length > .3],
                            offset=bevel, segments=2, affect="EDGES", profile=.5)
        rot = {"Z": (0, 0, 0), "X": (0, 90, 0), "Y": (90, 0, 0)}[axis]
        self._append(part, mat, loc, rot, True)

    def sphere(self, radius, loc, mat, scale=(1, 1, 1)):
        part = bmesh.new()
        bmesh.ops.create_uvsphere(part, u_segments=16, v_segments=10, radius=radius)
        for v in part.verts:
            v.co.x *= scale[0]
            v.co.y *= scale[1]
            v.co.z *= scale[2]
        self._append(part, mat, loc, (0, 0, 0), True)

    def torus(self, major, minor, loc, mat, axis="Z", major_seg=28, minor_seg=8):
        part = bmesh.new()
        verts = []
        for i in range(major_seg):
            a = 2 * math.pi * i / major_seg
            for j in range(minor_seg):
                b = 2 * math.pi * j / minor_seg
                r = major + minor * math.cos(b)
                verts.append(part.verts.new((r * math.cos(a), r * math.sin(a), minor * math.sin(b))))
        for i in range(major_seg):
            for j in range(minor_seg):
                part.faces.new((verts[i * minor_seg + j], verts[((i + 1) % major_seg) * minor_seg + j],
                                verts[((i + 1) % major_seg) * minor_seg + (j + 1) % minor_seg], verts[i * minor_seg + (j + 1) % minor_seg]))
        rot = {"Z": (0, 0, 0), "X": (0, 90, 0), "Y": (90, 0, 0)}[axis]
        self._append(part, mat, loc, rot, True)

    def build(self, name, pivot=(0, 0, 0)):
        """Mesh object whose origin sits at 'pivot' (the geometry is shifted so it stays where it was modelled)."""
        mesh = bpy.data.meshes.new(name)
        shift = Vector(pivot)
        for v in self.bm.verts:
            v.co -= shift
        self.bm.to_mesh(mesh)
        self.bm.free()
        for slot in self.slot_names:
            mesh.materials.append(self.mats[slot])
        obj = bpy.data.objects.new(name, mesh)
        obj.location = shift
        bpy.context.scene.collection.objects.link(obj)
        return obj


def export_fbx(root_objects, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    for root in root_objects:
        for obj in [root, *root.children_recursive]:
            obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"EMPTY", "MESH"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, use_mesh_modifiers=False, mesh_smooth_type="OFF",
                             add_leaf_bones=False, bake_anim=False, path_mode="STRIP")


def render_previews(target, label, centre, distance, ortho_scale):
    """Three Workbench views (front / three-quarter / side) for a visual check."""
    os.makedirs(target, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.render.resolution_x = scene.render.resolution_y = 640
    cam = bpy.data.objects.new("PreviewCam", bpy.data.cameras.new("PreviewCam"))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = ortho_scale
    scene.collection.objects.link(cam)
    scene.camera = cam
    for view, az in (("front", 0.0), ("threequarter", .7), ("side", 1.5708)):
        d = Vector((math.sin(az) * math.cos(.25), -math.cos(az) * math.cos(.25), math.sin(.25)))
        cam.location = Vector(centre) + d * distance
        cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(target, f"{label}_{view}.png")
        bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)
