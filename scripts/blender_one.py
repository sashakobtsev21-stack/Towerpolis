import bpy
from mathutils import Quaternion
NAME = 'Floor_Standard'
for o in bpy.data.objects:
    if o.name.startswith('Resident_'):
        try: o.hide_set(True)
        except Exception: pass
ob = bpy.data.objects.get(NAME)
# report bounding box (world Z range) to diagnose floating parts
zs = [(ob.matrix_world @ v.co).z for v in ob.data.vertices]
print('%s world Z: min=%.3f max=%.3f  (floor height ~1.5)' % (NAME, min(zs), max(zs)))
bpy.ops.object.select_all(action='DESELECT')
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        sp = area.spaces[0]
        sp.shading.type = 'SOLID'; sp.shading.color_type = 'MATERIAL'
        try: sp.shading.show_cavity = True; sp.shading.cavity_type = 'BOTH'
        except Exception: pass
        sp.overlay.show_overlays = False
        r3d = sp.region_3d
        r3d.view_perspective = 'PERSP'
        r3d.view_rotation = Quaternion((0.80, 0.45, 0.17, 0.36)).normalized()
        for region in area.regions:
            if region.type == 'WINDOW':
                with bpy.context.temp_override(area=area, region=region):
                    ob.select_set(True); bpy.ops.view3d.view_selected(); ob.select_set(False)
print('framed', NAME)
