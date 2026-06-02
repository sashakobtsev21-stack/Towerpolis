# Towerpolis blocks v3 — match the reference: clean isometric apartment tower.
# ONE pastel body color (recolorable per district) + WHITE trim everywhere
# (window frames, floor rims, balconies, parapet, base). Soft beveled forms, tidy.
import bpy, bmesh

def mat(name, rgba, rough=0.45, metal=0.0, emis=0.0, spec=0.5):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True; m.diffuse_color = rgba
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

def add_box(sx, sy, sz, c, material=None):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1, location=c)
    o = bpy.context.active_object; o.scale = (sx, sy, sz)
    bpy.ops.object.transform_apply(scale=True)
    if material: o.data.materials.append(material)
    return o

def body_box(name, sx, sy, sz, bx, body_mat, bevel=0.06):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0))
    o = bpy.context.active_object; o.name = name
    o.scale = (sx, sy, sz); bpy.ops.object.transform_apply(scale=True)
    bpy.context.scene.cursor.location = (0,0,-sz/2.0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.context.scene.cursor.location = (0,0,0)
    o.location = (bx, 0, 0); o.data.materials.append(body_mat)
    if bevel:
        md = o.modifiers.new('b','BEVEL'); md.width=bevel; md.segments=2; md.limit_method='ANGLE'
        bpy.context.view_layer.objects.active = o
        try: bpy.ops.object.modifier_apply(modifier='b')
        except Exception: pass
    return o

def join(main, parts):
    bpy.ops.object.select_all(action='DESELECT'); main.select_set(True)
    for p in parts:
        if p: p.select_set(True)
    bpy.context.view_layer.objects.active = main; bpy.ops.object.join(); return main

def windows(bx, faces, w=0.46, h=0.6, z=0.55, span=0.42, white=None, glass=None):
    p = []
    for face in faces:
        for off in (-span, span):
            if face in ('F','B'):
                y = 1.0 if face=='F' else -1.0; dy = 0.03 if face=='F' else -0.03
                p.append(add_box(w+0.14, 0.07, h+0.14, (bx+off, y, z), white))   # white frame plate
                p.append(add_box(w, 0.12, h, (bx+off, y+dy, z), glass))           # glass (proud)
            else:
                x = 1.0 if face=='R' else -1.0; dx = 0.03 if face=='R' else -0.03
                p.append(add_box(0.07, w+0.14, h+0.14, (bx+x, off, z), white))
                p.append(add_box(0.12, w, h, (bx+x+dx, off, z), glass))
    return p

def floor_rim(bx, white, t=0.12):
    return [add_box(2.1, 2.1, t, (bx, 0, t/2.0), white)]          # white band at floor base

def balcony(bx, white, glass, fy=1.0):
    p = []
    p.append(add_box(1.2, 0.42, 0.07, (bx, fy+0.2, 0.16), white))         # slab
    for px in (-0.55,-0.18,0.18,0.55):
        p.append(add_box(0.05,0.05,0.34,(bx+px, fy+0.39, 0.33), white))   # balusters
    p.append(add_box(1.26, 0.06, 0.05, (bx, fy+0.39, 0.50), white))       # top rail
    p.append(add_box(0.5, 0.10, 0.82, (bx, fy, 0.45), glass))             # glass door
    return p

# ---- materials: ONE body color (recolorable) + white trim + glass ----
m_body  = mat('TP_Body',  (0.91,0.46,0.42,1), rough=0.50)   # soft coral-red (per-district recolor)
m_white = mat('TP_White', (0.96,0.96,0.92,1), rough=0.38)   # trim/frames/rails/roof
m_glass = mat('TP_Glass', (0.80,0.90,0.97,1), rough=0.12, emis=0.35)
m_base  = mat('TP_Base',  (0.95,0.93,0.88,1), rough=0.5)     # ground floor cream
m_door  = mat('TP_Door',  (0.30,0.33,0.45,1), rough=0.35)

NAMES = ['Floor_Standard','Floor_Balcony','Floor_Premium','Roof_Cap','Base_Ground']
for n in NAMES: clear(n)
log = []
def safe(label, fn):
    try: fn(); log.append(label+' ok')
    except Exception as e: log.append(label+' ERR '+repr(e)[:110])

# Floor_Standard: body + white floor-rim + 2 windows/face
def b1():
    o = body_box('Floor_Standard',2,2,1,0,m_body)
    join(o, floor_rim(0,m_white) + windows(0,('F','B','L','R'),white=m_white,glass=m_glass))
safe('Floor_Standard', b1)

# Floor_Balcony: rim + windows on 3 sides + white balcony on front
def b2():
    o = body_box('Floor_Balcony',2,2,1,3,m_body)
    join(o, floor_rim(3,m_white) + windows(3,('B','L','R'),white=m_white,glass=m_glass)
            + balcony(3,m_white,m_glass))
safe('Floor_Balcony', b2)

# Floor_Premium: rim + taller windows + white corner pilasters (subtle upscale)
def b3():
    o = body_box('Floor_Premium',2,2,1,6,m_body)
    p = floor_rim(6,m_white) + windows(6,('F','B','L','R'),h=0.74,z=0.52,white=m_white,glass=m_glass)
    for (px,py) in [(-1.0,-1.0),(1.0,-1.0),(-1.0,1.0),(1.0,1.0)]:
        p.append(add_box(0.16,0.16,1.02,(6+px*0.95,py*0.95,0.5), m_white))   # corner pilasters
    join(o, p)
safe('Floor_Premium', b3)

# Roof_Cap: white flat roof + raised parapet + small penthouse + railing
def b4():
    o = body_box('Roof_Cap',2,2,0.3,9,m_white,bevel=0.04)
    p = []
    for (dx,dy,sx,sy) in [(0,0.95,2.06,0.14),(0,-0.95,2.06,0.14),(0.95,0,0.14,2.06),(-0.95,0,0.14,2.06)]:
        p.append(add_box(sx,sy,0.22,(9+dx,dy,0.30), m_white))         # parapet
    p.append(add_box(0.95,0.95,0.55,(9,-0.15,0.55), m_white))         # penthouse box
    p.append(add_box(0.5,0.1,0.34,(9,0.33,0.52), m_glass))            # penthouse door
    p.append(add_box(0.4,0.4,0.16,(9,0.55,0.5), m_white))             # rooftop unit
    p.append(add_box(0.05,0.05,0.5,(9.6,-0.6,0.6), m_white))          # antenna
    join(o, p)
safe('Roof_Cap', b4)

# Base_Ground: wider cream base + white rim + entrance + steps + 2 windows
def b5():
    o = body_box('Base_Ground',2.2,2.2,0.8,12,m_base)
    p = []
    p.append(add_box(2.28,2.28,0.1,(12,0,0.74), m_white))             # top rim
    p.append(add_box(0.66,0.12,0.62,(12,1.1,0.31), m_white))          # door frame
    p.append(add_box(0.5,0.14,0.52,(12,1.12,0.26), m_door))           # door
    p.append(add_box(0.9,0.6,0.1,(12,1.45,0.05), m_white))            # step
    for off in (-0.62,0.62):
        p.append(add_box(0.44,0.07,0.4,(12+off,1.1,0.42), m_white))   # window frame
        p.append(add_box(0.32,0.1,0.28,(12+off,1.12,0.42), m_glass))  # glass
    join(o, p)
safe('Base_Ground', b5)

# light + ground for preview
def stage():
    clear('TP_GroundPlane')
    gp = add_box(60,60,0.1,(6,0,-0.05), mat('TP_Ground',(0.93,0.95,0.98,1),rough=0.7)); gp.name='TP_GroundPlane'
    if not bpy.data.objects.get('TP_Sun'):
        ld = bpy.data.lights.new('TP_Sun','SUN'); ld.energy=3.0
        try: ld.angle=0.2
        except Exception: pass
        su = bpy.data.objects.new('TP_Sun', ld); bpy.context.collection.objects.link(su); su.rotation_euler=(0.6,0.1,0.5)
safe('stage', stage)

bpy.context.view_layer.update()
for n in NAMES:
    ob = bpy.data.objects.get(n)
    if ob: log.append("  %-15s tris~%d" % (n, sum(len(p.vertices)-2 for p in ob.data.polygons)))
print("BUILT %d/5 (v3 clean):\n%s" % (sum(1 for n in NAMES if bpy.data.objects.get(n)), "\n".join(log)))
