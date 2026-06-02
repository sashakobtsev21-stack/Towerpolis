import bpy
from mathutils import Quaternion
for o in bpy.data.objects:
    if o.name.startswith('Resident_'):
        try: o.hide_set(True)
        except Exception: pass
blocks = [bpy.data.objects[n] for n in ['Floor_Standard','Floor_Balcony','Floor_Premium','Base_Ground'] if bpy.data.objects.get(n)]
bpy.ops.object.select_all(action='DESELECT')
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        sp = area.spaces[0]
        sp.shading.type = 'SOLID'; sp.shading.color_type = 'MATERIAL'
        try: sp.shading.show_cavity = True; sp.shading.cavity_type = 'BOTH'; sp.shading.show_shadows = True
        except Exception: pass
        sp.overlay.show_overlays = False
        for region in area.regions:
            if region.type == 'WINDOW':
                r3d = sp.region_3d
                r3d.view_perspective = 'PERSP'
                r3d.view_rotation = Quaternion((0.766, 0.421, 0.168, 0.451)).normalized()  # 3/4 iso
                with bpy.context.temp_override(area=area, region=region):
                    for o in blocks: o.select_set(True)
                    bpy.ops.view3d.view_selected()
                    bpy.ops.object.select_all(action='DESELECT')
print('blocks 3/4 view (%d blocks, residents hidden)' % len(blocks))
