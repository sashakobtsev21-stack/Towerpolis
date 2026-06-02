import bpy
sc = bpy.context.scene
# tone-map so emissive windows don't blow out
try:
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.exposure = -0.8
    sc.view_settings.look = 'None'
except Exception as e: print('viewxform', e)
try:
    w = bpy.data.worlds[0]; bg = w.node_tree.nodes.get('Background')
    if bg: bg.inputs[1].default_value = 0.45
except Exception: pass
s = bpy.data.objects.get('TP_Sun')
if s: s.data.energy = 2.2
g = bpy.data.materials.get('TP_Glass')
if g:
    b = g.node_tree.nodes.get('Principled BSDF')
    if b and 'Emission Strength' in b.inputs: b.inputs['Emission Strength'].default_value = 1.0

# clear old demo tower
for o in list(bpy.data.objects):
    if o.name.startswith('Demo_'):
        bpy.data.objects.remove(o, do_unlink=True)

def dup(src, z, nm):
    s = bpy.data.objects[src]
    o = s.copy(); o.data = s.data.copy(); o.name = nm
    bpy.context.collection.objects.link(o)
    o.location = (-6, 0, z)
    return o

# stack: Base(0->.6) Standard(.6->1.6) Balcony(1.6->2.6) Premium(2.6->3.6) Roof(3.6->4.0)
stack = [('Base_Ground',0.0),('Floor_Standard',0.6),('Floor_Balcony',1.6),('Floor_Premium',2.6),('Roof_Cap',3.6)]
demo = [dup(src, z, 'Demo_%d' % i) for i,(src,z) in enumerate(stack)]

bpy.ops.object.select_all(action='DESELECT')
for o in demo: o.select_set(True)
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        try: area.spaces[0].shading.type = 'RENDERED'
        except Exception: pass
        for region in area.regions:
            if region.type == 'WINDOW':
                try:
                    with bpy.context.temp_override(area=area, region=region):
                        bpy.ops.view3d.view_selected()
                except Exception as e: print('frame', e)
print('showcase tower: %d parts stacked' % len(demo))
