# Towerpolis blocks v2 — premium look: framed emissive windows, beveled edges (soft
# shadows/highlights), glossy + metallic-gold materials, a real glass balcony, roof &
# base detail. Direction 1 palette, brighter. Idempotent. Low-poly-ish (mobile OK).
import bpy, bmesh

def mat(name, rgba, rough=0.3, metal=0.0, emis=0.0, spec=0.6):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    m.diffuse_color = rgba
    b = m.node_tree.nodes.get('Principled BSDF')
    if b:
        I = b.inputs
        if 'Base Color' in I: I['Base Color'].default_value = rgba
        if 'Roughness' in I: I['Roughness'].default_value = rough
        if 'Metallic' in I: I['Metallic'].default_value = metal
        if 'Specular' in I: I['Specular'].default_value = spec
        elif 'Specular IOR Level' in I: I['Specular IOR Level'].default_value = spec
        if 'Emission Color' in I:
            I['Emission Color'].default_value = rgba
            if 'Emission Strength' in I: I['Emission Strength'].default_value = emis
        elif 'Emission' in I:
            I['Emission'].default_value = rgba
            if 'Emission Strength' in I: I['Emission Strength'].default_value = emis
    return m

def clear(name):
    o = bpy.data.objects.get(name)
    if o: bpy.data.objects.remove(o, do_unlink=True)

def add_box(sx, sy, sz, center, material=None):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    o = bpy.context.active_object
    o.scale = (sx, sy, sz); bpy.ops.object.transform_apply(scale=True)
    if material: o.data.materials.append(material)
    return o

def add_cyl(r, depth, center, material=None, verts=12):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth, location=center)
    o = bpy.context.active_object
    if material: o.data.materials.append(material)
    return o

def body_box(name, sx, sy, sz, base_x, body_mat, bevel=0.045):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0))
    o = bpy.context.active_object; o.name = name
    o.scale = (sx, sy, sz); bpy.ops.object.transform_apply(scale=True)
    bpy.context.scene.cursor.location = (0,0,-sz/2.0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.context.scene.cursor.location = (0,0,0)
    o.location = (base_x, 0, 0)
    o.data.materials.append(body_mat)
    if bevel:
        md = o.modifiers.new('bev','BEVEL'); md.width=bevel; md.segments=2; md.limit_method='ANGLE'
        bpy.context.view_layer.objects.active = o
        try: bpy.ops.object.modifier_apply(modifier='bev')
        except Exception: pass
    return o

def join(main, parts):
    bpy.ops.object.select_all(action='DESELECT')
    main.select_set(True)
    for p in parts:
        if p: p.select_set(True)
    bpy.context.view_layer.objects.active = main
    bpy.ops.object.join()
    return main

def windows_on_face(base_x, face, cols, rows, glass, frame, span=0.55, z0=0.20, z1=0.82):
    parts = []
    for c in range(cols):
        fx = (-span + 2*span*c/(cols-1)) if cols > 1 else 0.0
        for r in range(rows):
            z = z0 + (z1-z0)*(r/(rows-1) if rows > 1 else 0.5)
            if face in ('F','B'):
                y = 1.0 if face == 'F' else -1.0
                dy = 0.03 if face == 'F' else -0.03
                parts.append(add_box(0.46,0.10,0.42, (base_x+fx, y, z), frame))
                parts.append(add_box(0.34,0.12,0.30, (base_x+fx, y+dy, z), glass))
            else:
                x = 1.0 if face == 'R' else -1.0
                dx = 0.03 if face == 'R' else -0.03
                parts.append(add_box(0.10,0.46,0.42, (base_x+x, fx, z), frame))
                parts.append(add_box(0.12,0.34,0.30, (base_x+x+dx, fx, z), glass))
    return parts

def balcony(base_x, gold, glass2, planter):
    p = []
    p.append(add_box(1.5,0.55,0.09, (base_x,1.25,0.30), gold))            # slab
    p.append(add_box(1.5,0.04,0.40, (base_x,1.50,0.52), glass2))          # glass panel
    for px in (-0.70,0.70):
        p.append(add_box(0.07,0.55,0.55,(base_x+px,1.25,0.55), gold))     # posts
    p.append(add_box(1.56,0.58,0.06,(base_x,1.26,0.74), gold))            # top rail
    p.append(add_box(0.42,0.20,0.18,(base_x-0.5,1.30,0.39), planter))     # planter
    return p

# ---------- materials (brighter, glossy, premium) ----------
m_std   = mat('TP_Body_Coral',  (1.00,0.45,0.24,1), rough=0.20)
m_bal   = mat('TP_Body_Green',  (0.28,0.80,0.42,1), rough=0.20)
m_prem  = mat('TP_Body_Royal',  (0.40,0.34,1.00,1), rough=0.15, spec=0.9)
m_glass = mat('TP_Glass',       (1.00,0.90,0.58,1), rough=0.07, emis=1.8)
m_glassR= mat('TP_GlassRail',   (0.62,0.88,1.00,1), rough=0.05, emis=0.5)
m_gold  = mat('TP_Gold',        (1.00,0.80,0.26,1), rough=0.18, metal=1.0)
m_white = mat('TP_FrameWhite',  (0.99,0.99,0.97,1), rough=0.22)
m_roof  = mat('TP_Roof_Sky',    (0.31,0.76,0.97,1), rough=0.18)
m_base  = mat('TP_Base',        (1.00,0.97,0.90,1), rough=0.28)
m_plant = mat('TP_Planter',     (0.20,0.62,0.32,1), rough=0.5)
m_door  = mat('TP_Door',        (0.62,0.36,0.20,1), rough=0.3)
m_grd   = mat('TP_Ground',      (0.93,0.95,0.98,1), rough=0.6)

NAMES = ['Floor_Standard','Floor_Balcony','Floor_Premium','Roof_Cap','Base_Ground']
for n in NAMES + ['TP_GroundPlane']:
    clear(n)

log = []
def safe(label, fn):
    try: fn(); log.append(label+' ok')
    except Exception as e: log.append(label+' ERR: '+repr(e)[:120])

# 1) Standard — coral, framed windows all sides
def b1():
    o = body_box('Floor_Standard',2,2,1,0,m_std)
    parts=[]
    for f in ('F','B','L','R'): parts += windows_on_face(0,f,2,2,m_glass,m_white)
    join(o,parts)
safe('Floor_Standard', b1)

# 2) Balcony — green, glass balcony on front, windows elsewhere
def b2():
    o = body_box('Floor_Balcony',2,2,1,3,m_bal)
    parts=[]
    for f in ('B','L','R'): parts += windows_on_face(3,f,2,2,m_glass,m_white)
    parts += balcony(3,m_gold,m_glassR,m_plant)
    join(o,parts)
safe('Floor_Balcony', b2)

# 3) Premium — royal violet, gold frames+cornice, 3 window rows
def b3():
    o = body_box('Floor_Premium',2,2,1,6,m_prem)
    parts=[]
    for f in ('F','B','L','R'): parts += windows_on_face(6,f,2,3,m_glass,m_gold)
    parts.append(add_box(2.28,2.28,0.12,(6,0,0.95), m_gold))   # cornice
    parts.append(add_box(2.12,2.12,0.07,(6,0,0.04), m_gold))   # base band
    join(o,parts)
safe('Floor_Premium', b3)

# 4) Roof_Cap — tapered, parapet, water tank, antenna
def b4():
    o = body_box('Roof_Cap',2,2,0.4,9,m_roof,bevel=0.03)
    me=o.data; bm=bmesh.new(); bm.from_mesh(me)
    for v in bm.verts:
        if v.co.z>0.3: v.co.x*=0.8; v.co.y*=0.8
    bm.to_mesh(me); bm.free(); me.update()
    parts=[]
    for (dx,dy,sx,sy) in [(0,0.9,1.9,0.12),(0,-0.9,1.9,0.12),(0.9,0,0.12,1.9),(-0.9,0,0.12,1.9)]:
        parts.append(add_box(sx,sy,0.16,(9+dx,dy,0.34), m_white))  # parapet
    parts.append(add_cyl(0.26,0.5,(9,0.3,0.45), m_white))          # water tank
    parts.append(add_box(0.05,0.05,0.8,(9,-0.3,0.7), m_gold))      # antenna mast
    parts.append(add_box(0.18,0.18,0.06,(9,-0.3,1.05), m_glass))   # antenna light
    join(o,parts)
safe('Roof_Cap', b4)

# 5) Base_Ground — plinth, gold band, door, steps
def b5():
    o = body_box('Base_Ground',2.5,2.5,0.6,12,m_base)
    parts=[]
    parts.append(add_box(2.6,2.6,0.10,(12,0,0.52), m_gold))        # top band
    parts.append(add_box(0.6,0.12,0.5,(12,1.25,0.25), m_gold))     # door frame
    parts.append(add_box(0.44,0.14,0.42,(12,1.26,0.21), m_door))   # door
    for i,zz in enumerate((0.0,)):
        parts.append(add_box(1.2,0.5,0.12,(12,1.55,0.06), m_base)) # step
    for f in ('L','R','B'): parts += windows_on_face(12,f,2,1,m_glass,m_white,z0=0.30,z1=0.30)
    join(o,parts)
safe('Base_Ground', b5)

# ---------- lighting + ground + render shading for a good screenshot ----------
def stage():
    if not bpy.data.objects.get('TP_GroundPlane'):
        gp = add_box(60,60,0.1,(6,0,-0.05), m_grd); gp.name='TP_GroundPlane'
    if not bpy.data.objects.get('TP_Sun'):
        ld = bpy.data.lights.new('TP_Sun','SUN'); ld.energy=4.0
        try: ld.angle=0.15
        except Exception: pass
        sun = bpy.data.objects.new('TP_Sun', ld); bpy.context.collection.objects.link(sun)
        sun.rotation_euler=(0.6,0.1,0.5)
    try:
        w = bpy.data.worlds[0]; w.use_nodes=True
        bg = w.node_tree.nodes.get('Background')
        if bg: bg.inputs[0].default_value=(0.55,0.78,0.95,1); bg.inputs[1].default_value=1.0
    except Exception: pass
    try: bpy.context.scene.eevee.use_gtao=True
    except Exception: pass
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            try: area.spaces[0].shading.type='RENDERED'
            except Exception: pass
safe('stage', stage)

bpy.context.view_layer.update()
built = sum(1 for n in NAMES if bpy.data.objects.get(n))
for n in NAMES:
    ob=bpy.data.objects.get(n)
    if ob:
        tr=sum(len(p.vertices)-2 for p in ob.data.polygons)
        log.append("  %-15s tris~%d" % (n,tr))
print("BUILT %d/5 (v2 premium):\n%s" % (built,"\n".join(log)))
