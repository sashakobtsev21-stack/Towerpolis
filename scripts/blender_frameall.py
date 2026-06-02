import bpy
meshes = [o for o in bpy.data.objects if o.type == 'MESH' and o.name != 'TP_GroundPlane']
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
                with bpy.context.temp_override(area=area, region=region):
                    for o in meshes: o.select_set(True)
                    bpy.ops.view3d.view_selected()
                    bpy.ops.object.select_all(action='DESELECT')
print('framed all: %d meshes' % len(meshes))
