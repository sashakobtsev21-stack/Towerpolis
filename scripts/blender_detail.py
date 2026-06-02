import bpy
sel = ['Resident_Umbrella_1','Resident_Umbrella_2','Resident_Umbrella_3','Resident_Parachute_1','Resident_Parachute_2','Resident_Parachute_3']
bpy.ops.object.select_all(action='DESELECT')
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        sp = area.spaces[0]
        sp.shading.type = 'SOLID'; sp.shading.color_type = 'MATERIAL'
        try:
            sp.shading.show_shadows = True; sp.shading.show_cavity = True; sp.shading.cavity_type = 'BOTH'
        except Exception: pass
        sp.overlay.show_overlays = False
        for region in area.regions:
            if region.type == 'WINDOW':
                with bpy.context.temp_override(area=area, region=region):
                    for n in sel:
                        o = bpy.data.objects.get(n)
                        if o: o.select_set(True)
                    bpy.ops.view3d.view_selected()
                    bpy.ops.object.select_all(action='DESELECT')
print('detail ready')
