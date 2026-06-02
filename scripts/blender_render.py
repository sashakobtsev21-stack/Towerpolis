import bpy
sc = bpy.context.scene
# recolor the giant ground so it stops washing everything white; glass will reflect the sky
g = bpy.data.materials.get('TP_Ground')
if g:
    g.diffuse_color = (0.45,0.62,0.38,1)
    b = g.node_tree.nodes.get('Principled BSDF')
    if b and 'Base Color' in b.inputs: b.inputs['Base Color'].default_value = (0.45,0.62,0.38,1)
try:
    sc.view_settings.view_transform = 'Standard'; sc.view_settings.exposure = -0.5; sc.view_settings.look = 'None'
except Exception: pass
try:
    w = bpy.data.worlds[0]; w.use_nodes = True
    bg = w.node_tree.nodes.get('Background')
    if bg: bg.inputs[0].default_value = (0.45,0.72,1.0,1); bg.inputs[1].default_value = 0.7
except Exception: pass
s = bpy.data.objects.get('TP_Sun')
if s: s.data.energy = 2.6
ee = sc.eevee
for a,v in [('use_gtao',True),('use_ssr',True),('use_ssr_refraction',True),('taa_samples',16)]:
    try: setattr(ee,a,v)
    except Exception: pass
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        sp = area.spaces[0]; sp.shading.type = 'RENDERED'; sp.overlay.show_overlays = False
print('render set (grass ground)')
