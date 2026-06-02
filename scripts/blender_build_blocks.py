# Towerpolis blocks v4 — bright reference colors, REAL framed windows (recessed glass +
# white frame + mullions), floors 50% taller (1.5), 3-sided balcony rails, premium with
# floor-to-ceiling windows + 2 side balconies. Single recolorable body + white trim.
import bpy

FH = 1.5  # floor height (was 1.0 -> +50%)

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

def clear(n):
    o = bpy.data.objects.get(n)
    if o: bpy.data.objects.remove(o, do_unlink=True)

def box(sx, sy, sz, c, m=None):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1, location=c)
    o = bpy.context.active_object; o.scale=(sx,sy,sz); bpy.ops.object.transform_apply(scale=True)
    if m: o.data.materials.append(m)
    return o

def body(name, sx, sy, sz, bx, m, bevel=0.06):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0))
    o = bpy.context.active_object; o.name=name; o.scale=(sx,sy,sz)
    bpy.ops.object.transform_apply(scale=True)
    bpy.context.scene.cursor.location=(0,0,-sz/2.0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.context.scene.cursor.location=(0,0,0); o.location=(bx,0,0); o.data.materials.append(m)
    md=o.modifiers.new('b','BEVEL'); md.width=bevel; md.segments=2; md.limit_method='ANGLE'
    bpy.context.view_layer.objects.active=o
    try: bpy.ops.object.modifier_apply(modifier='b')
    except Exception: pass
    return o

def join(main, parts):
    bpy.ops.object.select_all(action='DESELECT'); main.select_set(True)
    for p in parts:
        if p: p.select_set(True)
    bpy.context.view_layer.objects.active=main; bpy.ops.object.join(); return main

def win(bx, face, off, cz, W, H, white, glass, vm=1, hm=1):
    """A real framed window: recessed glass + white frame border + mullions."""
    p=[]; t=0.06
    if face in ('F','B'):
        y=1.0 if face=='F' else -1.0; n=1 if face=='F' else -1
        gy=y-n*0.06; fy=y+n*0.03
        p.append(box(W,0.05,H,(bx+off,gy,cz),glass))                       # glass recessed
        p.append(box(W+0.14,0.10,t,(bx+off,fy,cz+H/2),white))              # top
        p.append(box(W+0.14,0.10,t,(bx+off,fy,cz-H/2),white))              # bottom
        p.append(box(t,0.10,H+0.14,(bx+off-W/2,fy,cz),white))             # left
        p.append(box(t,0.10,H+0.14,(bx+off+W/2,fy,cz),white))             # right
        for k in range(vm):
            mx=-W/2+W*(k+1)/(vm+1); p.append(box(0.035,0.09,H,(bx+off+mx,fy,cz),white))
        for k in range(hm):
            mz=-H/2+H*(k+1)/(hm+1); p.append(box(W,0.09,0.035,(bx+off,fy,cz+mz),white))
    else:
        x=1.0 if face=='R' else -1.0; n=1 if face=='R' else -1
        gx=x-n*0.06; fx=x+n*0.03
        p.append(box(0.05,W,H,(bx+gx,off,cz),glass))
        p.append(box(0.10,W+0.14,t,(bx+fx,off,cz+H/2),white))
        p.append(box(0.10,W+0.14,t,(bx+fx,off,cz-H/2),white))
        p.append(box(0.10,t,H+0.14,(bx+fx,off-W/2,cz),white))
        p.append(box(0.10,t,H+0.14,(bx+fx,off+W/2,cz),white))
        for k in range(vm):
            my=-W/2+W*(k+1)/(vm+1); p.append(box(0.09,0.035,H,(bx+fx,off+my,cz),white))
        for k in range(hm):
            mz=-H/2+H*(k+1)/(hm+1); p.append(box(0.09,W,0.035,(bx+fx,off,cz+mz),white))
    return p

def windows(bx, faces, offs, cz, W, H, white, glass, vm=1, hm=1):
    p=[]
    for f in faces:
        for o in offs: p += win(bx,f,o,cz,W,H,white,glass,vm,hm)
    return p

def rim(bx, white, t=0.14):
    return [box(2.12,2.12,t,(bx,0,t/2.0),white)]

def balcony_front(bx, white, glass):
    p=[]; w=1.3; d=0.5; sh=0.16; rh=0.46; yo=1.0+d
    p.append(box(w,d,0.08,(bx,1.0+d/2,sh),white))
    for (px,py) in [(-w/2,1.05),(w/2,1.05),(-w/2,yo-0.05),(w/2,yo-0.05)]:
        p.append(box(0.06,0.06,rh,(bx+px,py,sh+rh/2),white))
    for px in (-w/4,0,w/4):
        p.append(box(0.04,0.04,rh,(bx+px,yo-0.05,sh+rh/2),white))
    p.append(box(w,0.05,0.05,(bx,yo-0.05,sh+rh),white))            # front rail
    p.append(box(0.05,d,0.05,(bx-w/2,1.0+d/2,sh+rh),white))        # left rail
    p.append(box(0.05,d,0.05,(bx+w/2,1.0+d/2,sh+rh),white))        # right rail
    p.append(box(0.6,0.10,1.15,(bx,1.0,0.62),glass))              # glass door
    return p

def balcony_side(bx, sgn, white, glass):
    p=[]; w=1.1; d=0.45; sh=0.16; rh=0.46; cx=sgn*1.0; xo=cx+sgn*d
    p.append(box(d,w,0.08,(bx+cx+sgn*d/2,0,sh),white))
    for (px,py) in [(cx+sgn*0.05,-w/2),(cx+sgn*0.05,w/2),(xo-sgn*0.05,-w/2),(xo-sgn*0.05,w/2)]:
        p.append(box(0.06,0.06,rh,(bx+px,py,sh+rh/2),white))
    for py in (-w/4,0,w/4):
        p.append(box(0.04,0.04,rh,(bx+xo-sgn*0.05,py,sh+rh/2),white))
    p.append(box(0.05,w,0.05,(bx+xo-sgn*0.05,0,sh+rh),white))      # outer rail
    p.append(box(d,0.05,0.05,(bx+cx+sgn*d/2,-w/2,sh+rh),white))    # side rail
    p.append(box(d,0.05,0.05,(bx+cx+sgn*d/2, w/2,sh+rh),white))    # side rail
    p.append(box(0.10,0.6,1.15,(bx+cx,0,0.62),glass))            # glass door
    return p

# ---- BRIGHT materials (reference red/yellow/blue presets; demo = bright red) ----
PRESETS = {'red':(1.00,0.22,0.20,1), 'yellow':(1.00,0.80,0.12,1), 'blue':(0.16,0.60,1.00,1)}
m_body  = mat('TP_Body',  PRESETS['red'], rough=0.42)
m_white = mat('TP_White', (0.98,0.98,0.95,1), rough=0.35)
m_glass = mat('TP_Glass', (0.55,0.82,1.00,1), rough=0.06, emis=0.6)
m_base  = mat('TP_Base',  (0.97,0.94,0.88,1), rough=0.5)
m_door  = mat('TP_Door',  (0.28,0.31,0.44,1), rough=0.35)

NAMES=['Floor_Standard','Floor_Balcony','Floor_Premium','Roof_Cap','Base_Ground']
for n in NAMES: clear(n)
log=[]
def safe(l,f):
    try: f(); log.append(l+' ok')
    except Exception as e: log.append(l+' ERR '+repr(e)[:110])

# Standard: 2 real windows / face (4 panes each)
def b1():
    o=body('Floor_Standard',2,2,FH,0,m_body)
    join(o, rim(0,m_white)+windows(0,('F','B','L','R'),(-0.44,0.44),0.85,0.6,1.0,m_white,m_glass,vm=1,hm=1))
safe('Floor_Standard',b1)

# Balcony: windows on back+sides, 3-sided-rail balcony on front
def b2():
    o=body('Floor_Balcony',2,2,FH,3,m_body)
    join(o, rim(3,m_white)+windows(3,('B','L','R'),(-0.44,0.44),0.9,0.6,1.0,m_white,m_glass)+balcony_front(3,m_white,m_glass))
safe('Floor_Balcony',b2)

# Premium: floor-to-ceiling windows front/back + 2 side balconies + corner posts
def b3():
    o=body('Floor_Premium',2,2,FH,6,m_body)
    p=rim(6,m_white)
    p+=windows(6,('F','B'),(0.0,),0.80,1.5,1.28,m_white,m_glass,vm=3,hm=2)   # floor-to-ceiling
    p+=balcony_side(6,-1,m_white,m_glass)+balcony_side(6,1,m_white,m_glass)  # left+right balconies
    for (px,py) in [(-0.95,-0.95),(0.95,-0.95),(-0.95,0.95),(0.95,0.95)]:
        p.append(box(0.12,0.12,FH,(6+px,py,FH/2),m_white))                   # slim corner posts
    join(o,p)
safe('Floor_Premium',b3)

# Roof: white parapet + penthouse + antenna
def b4():
    o=body('Roof_Cap',2,2,0.32,9,m_white,bevel=0.04)
    p=[]
    for (dx,dy,sx,sy) in [(0,0.95,2.08,0.14),(0,-0.95,2.08,0.14),(0.95,0,0.14,2.08),(-0.95,0,0.14,2.08)]:
        p.append(box(sx,sy,0.24,(9+dx,dy,0.32),m_white))
    p.append(box(0.95,0.95,0.6,(9,-0.15,0.6),m_white))
    p+=win(9,'F',0.0,0.6,0.5,0.5,m_white,m_glass)   # penthouse window (front, off relative to roof center)
    p.append(box(0.45,0.45,0.16,(9,0.55,0.55),m_white))
    p.append(box(0.05,0.05,0.55,(9.6,-0.6,0.7),m_white))
    join(o,p)
safe('Roof_Cap',b4)

# Base: taller cream base, entrance, steps, windows
def b5():
    o=body('Base_Ground',2.2,2.2,1.0,12,m_base)
    p=[box(2.3,2.3,0.1,(12,0,0.94),m_white)]
    p.append(box(0.72,0.12,0.78,(12,1.1,0.4),m_white))           # door frame
    p.append(box(0.56,0.14,0.66,(12,1.12,0.34),m_door))          # door
    p.append(box(1.0,0.6,0.1,(12,1.5,0.05),m_white))             # step
    for off in (-0.66,0.66):
        p+=win(12,'F',off,0.55,0.5,0.6,m_white,m_glass)
    for f in ('B','L','R'):
        p+=win(12,f,0.0,0.55,0.6,0.6,m_white,m_glass)
    join(o,p)
safe('Base_Ground',b5)

def stage():
    clear('TP_GroundPlane')
    gp=box(60,60,0.1,(6,0,-0.05),mat('TP_Ground',(0.93,0.95,0.98,1),rough=0.7)); gp.name='TP_GroundPlane'
    if not bpy.data.objects.get('TP_Sun'):
        ld=bpy.data.lights.new('TP_Sun','SUN'); ld.energy=3.0
        try: ld.angle=0.2
        except Exception: pass
        su=bpy.data.objects.new('TP_Sun',ld); bpy.context.collection.objects.link(su); su.rotation_euler=(0.6,0.1,0.5)
safe('stage',stage)

bpy.context.view_layer.update()
for n in NAMES:
    ob=bpy.data.objects.get(n)
    if ob: log.append("  %-15s tris~%d h=%.1f" % (n,sum(len(p.vertices)-2 for p in ob.data.polygons),ob.dimensions.z))
print("BUILT %d/5 (v4 bright+real-windows):\n%s" % (sum(1 for n in NAMES if bpy.data.objects.get(n)),"\n".join(log)))
