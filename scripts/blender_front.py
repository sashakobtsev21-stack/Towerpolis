import bpy
name = 'Base_Ground'
o = bpy.data.objects.get(name)
bpy.ops.object.select_all(action='DESELECT')
if o: o.select_set(True)
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        sp = area.spaces[0]
        sp.shading.type = 'SOLID'; sp.shading.color_type = 'MATERIAL'
        try: sp.shading.show_cavity = True; sp.shading.cavity_type = 'BOTH'
        except Exception: pass
        sp.overlay.show_overlays = False
        for region in area.regions:
            if region.type == 'WINDOW':
                with bpy.context.temp_override(area=area, region=region):
                    bpy.ops.view3d.view_axis(type='BACK')
                    bpy.ops.view3d.view_selected()
print('front view set on', name)
