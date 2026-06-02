import bpy
demo = [o for o in bpy.data.objects if o.name.startswith('Demo_')]
bpy.ops.object.select_all(action='DESELECT')
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        sp = area.spaces[0]; sh = sp.shading
        sh.type = 'SOLID'
        sh.light = 'STUDIO'
        sh.color_type = 'MATERIAL'      # true palette colors (diffuse_color), no gloss wash
        try:
            sh.show_shadows = True; sh.shadow_intensity = 0.5
            sh.show_cavity = True; sh.cavity_type = 'BOTH'
            sh.curvature_ridge_factor = 1.0; sh.curvature_valley_factor = 1.0
        except Exception as e: print('sh', e)
        sp.overlay.show_overlays = False
        for region in area.regions:
            if region.type == 'WINDOW':
                try:
                    with bpy.context.temp_override(area=area, region=region):
                        for o in demo: o.select_set(True)
                        bpy.ops.view3d.view_selected()
                        bpy.ops.object.select_all(action='DESELECT')
                except Exception as e: print('frame', e)
print('hero2 ready')
