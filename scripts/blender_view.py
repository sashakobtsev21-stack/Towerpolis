import bpy
names = ['Floor_Standard','Floor_Balcony','Floor_Premium','Roof_Cap','Base_Ground']
bpy.ops.object.select_all(action='DESELECT')
for n in names:
    o = bpy.data.objects.get(n)
    if o: o.select_set(True)
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        try: area.spaces[0].shading.type = 'RENDERED'
        except Exception: pass
        for region in area.regions:
            if region.type == 'WINDOW':
                try:
                    with bpy.context.temp_override(area=area, region=region):
                        bpy.ops.view3d.view_selected()
                except Exception as e:
                    print('frame err', e)
print('framed')
