# Towerpolis — build the 5 droppable blocks (Direction 1 "Tower Bloxx revival" palette).
# Sent to Blender via scripts/blender_send.js --code. Idempotent (clears prior blocks).
# Spec: footprint 2x2, height 1, ORIGIN at bottom-center, low-poly, material slots.
import bpy, bmesh

PAL = {
    'body_std':  (1.00, 0.42, 0.37, 1),   # coral  #FF6B5E
    'body_bal':  (0.40, 0.73, 0.42, 1),   # mint   #66BB6A
    'body_prem': (0.12, 0.23, 0.37, 1),   # navy   #1F3A5F
    'window':    (1.00, 0.95, 0.77, 1),   # glass  #FFF3C4 (emissive)
    'trim':      (1.00, 0.835, 0.31, 1),  # sunny  #FFD54F
    'roof':      (0.31, 0.76, 0.97, 1),   # sky    #4FC3F7
    'base':      (0.97, 0.97, 0.95, 1),   # offwht #F7F7F2
}

def mat(name, rgba, emis=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    m.diffuse_color = rgba  # solid-view color
    b = m.node_tree.nodes.get('Principled BSDF')
    if b:
        if 'Base Color' in b.inputs: b.inputs['Base Color'].default_value = rgba
        if 'Roughness' in b.inputs: b.inputs['Roughness'].default_value = 0.35
        if 'Emission Color' in b.inputs:           # Blender 4.x
            b.inputs['Emission Color'].default_value = rgba
            if 'Emission Strength' in b.inputs: b.inputs['Emission Strength'].default_value = emis
        elif 'Emission' in b.inputs:               # Blender 3.x
            b.inputs['Emission'].default_value = rgba
            if 'Emission Strength' in b.inputs: b.inputs['Emission Strength'].default_value = emis
    return m

def clear(name):
    o = bpy.data.objects.get(name)
    if o: bpy.data.objects.remove(o, do_unlink=True)

def box(name, sx, sy, sz, loc=(0,0,0)):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0))
    o = bpy.context.active_object
    o.name = name
    o.scale = (sx, sy, sz)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.context.scene.cursor.location = (0.0, 0.0, -sz/2.0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    o.location = loc
    return o

def windows(o, body_mat, win_mat, thickness=0.18, depth=-0.04):
    o.data.materials.clear()
    o.data.materials.append(body_mat)
    o.data.materials.append(win_mat)
    me = o.data
    bm = bmesh.new(); bm.from_mesh(me)
    bm.faces.ensure_lookup_table()
    side = [f for f in bm.faces if abs(f.normal.z) < 0.5]
    res = bmesh.ops.inset_individual(bm, faces=side, thickness=thickness, depth=depth)
    for f in res['faces']:
        f.material_index = 1
    bm.to_mesh(me); bm.free()
    me.update()

def join(main, *parts):
    bpy.ops.object.select_all(action='DESELECT')
    for p in (main,) + parts: p.select_set(True)
    bpy.context.view_layer.objects.active = main
    bpy.ops.object.join()
    return main

# --- materials ---
m_std  = mat('TP_Body_Coral', PAL['body_std'])
m_bal  = mat('TP_Body_Mint',  PAL['body_bal'])
m_prem = mat('TP_Body_Navy',  PAL['body_prem'])
m_win  = mat('TP_Window',     PAL['window'], emis=0.6)
m_trim = mat('TP_Trim_Gold',  PAL['trim'])
m_roof = mat('TP_Roof_Sky',   PAL['roof'])
m_base = mat('TP_Base',       PAL['base'])

NAMES = ['Floor_Standard','Floor_Balcony','Floor_Premium','Roof_Cap','Base_Ground']
for n in NAMES + ['__tmp_ledge','__tmp_rail','__tmp_cornice','__tmp_band']:
    clear(n)

log = []

# 1) Floor_Standard — coral + windows
o = box('Floor_Standard', 2, 2, 1, loc=(0,0,0))
windows(o, m_std, m_win)
log.append('Floor_Standard ok')

# 2) Floor_Balcony — mint + windows + front ledge + rail (houses 3)
o = box('Floor_Balcony', 2, 2, 1, loc=(3,0,0))
windows(o, m_bal, m_win)
ledge = box('__tmp_ledge', 1.3, 0.35, 0.12, loc=(3, 1.08, 0.10))
rail  = box('__tmp_rail',  1.3, 0.06, 0.28, loc=(3, 1.24, 0.22))
join(o, ledge, rail)
log.append('Floor_Balcony ok')

# 3) Floor_Premium — navy + windows + gold cornice (houses 4)
o = box('Floor_Premium', 2, 2, 1, loc=(6,0,0))
windows(o, m_prem, m_win, thickness=0.14)
cornice = box('__tmp_cornice', 2.2, 2.2, 0.12, loc=(6, 0, 0.9))
cornice.data.materials.clear(); cornice.data.materials.append(m_trim)
join(o, cornice)
log.append('Floor_Premium ok')

# 4) Roof_Cap — sky, tapered top
o = box('Roof_Cap', 2, 2, 0.5, loc=(9,0,0))
o.data.materials.clear(); o.data.materials.append(m_roof)
me = o.data; bm = bmesh.new(); bm.from_mesh(me)
for v in bm.verts:
    if v.co.z > 0.4:   # top verts (z near 0.5)
        v.co.x *= 0.78; v.co.y *= 0.78
bm.to_mesh(me); bm.free(); me.update()
log.append('Roof_Cap ok')

# 5) Base_Ground — off-white plinth + gold band
o = box('Base_Ground', 2.4, 2.4, 0.6, loc=(12,0,0))
o.data.materials.clear(); o.data.materials.append(m_base)
band = box('__tmp_band', 2.5, 2.5, 0.1, loc=(12, 0, 0.5))
band.data.materials.clear(); band.data.materials.append(m_trim)
join(o, band)
log.append('Base_Ground ok')

# report
bpy.context.view_layer.update()
for n in NAMES:
    ob = bpy.data.objects.get(n)
    if ob:
        d = ob.dimensions
        log.append("  %-15s tris~%d  dims=(%.2f,%.2f,%.2f) origin@%s" % (
            n, sum(len(p.vertices)-2 for p in ob.data.polygons), d.x, d.y, d.z,
            tuple(round(c,2) for c in ob.location)))
print("BUILT %d/5 blocks:\n%s" % (sum(1 for n in NAMES if bpy.data.objects.get(n)), "\n".join(log)))
