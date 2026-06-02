# Towerpolis residents v3 — human figures; UMBRELLAS look like umbrellas (ribbed cone +
# curved handle), PARACHUTES: 2 paraglider WINGS (arched) + 1 round DOME. Tidy row.
import bpy

def mat(name,rgba,rough=0.45):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes=True; m.diffuse_color=rgba
    b=m.node_tree.nodes.get('Principled BSDF')
    if b:
        if 'Base Color' in b.inputs: b.inputs['Base Color'].default_value=rgba
        if 'Roughness' in b.inputs: b.inputs['Roughness'].default_value=rough
    return m

def box(sx,sy,sz,c,m=None,rot=None,bevel=0.0):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1,location=c)
    o=bpy.context.active_object; o.scale=(sx,sy,sz); bpy.ops.object.transform_apply(scale=True)
    if rot: o.rotation_euler=rot; bpy.ops.object.transform_apply(rotation=True)
    if m: o.data.materials.append(m)
    if bevel:
        md=o.modifiers.new('b','BEVEL'); md.width=bevel; md.segments=2
        bpy.context.view_layer.objects.active=o
        try: bpy.ops.object.modifier_apply(modifier='b')
        except Exception: pass
    return o

def sphere(r,c,m=None,segs=14,squash=1.0):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs,ring_count=max(7,segs//2),radius=r,location=c)
    o=bpy.context.active_object
    if squash!=1.0: o.scale=(1,1,squash); bpy.ops.object.transform_apply(scale=True)
    if m: o.data.materials.append(m)
    return o

def cone(r,depth,c,m=None,verts=12,r2=0.0):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r,radius2=r2,depth=depth,location=c)
    o=bpy.context.active_object
    if m: o.data.materials.append(m)
    return o

def join(main,parts):
    bpy.ops.object.select_all(action='DESELECT'); main.select_set(True)
    for p in parts:
        if p: p.select_set(True)
    bpy.context.view_layer.objects.active=main; bpy.ops.object.join(); return main

m_skin=mat('R_Skin',(0.98,0.78,0.62,1)); m_shoe=mat('R_Shoe',(0.22,0.18,0.16,1))
m_pants=mat('R_Pants',(0.28,0.30,0.40,1)); m_eye=mat('R_Eye',(0.10,0.10,0.12,1))
m_stick=mat('R_Stick',(0.40,0.40,0.45,1))
HAIR=[mat('R_Hair0',(0.20,0.13,0.08,1)),mat('R_Hair1',(0.07,0.07,0.07,1)),mat('R_Hair2',(0.55,0.36,0.12,1))]

import math
def person(bx,by,cloth,hair):
    parts=[]
    for lx in (-0.09,0.09):
        parts.append(box(0.12,0.13,0.36,(bx+lx,by,0.18),m_pants,bevel=0.03))
        parts.append(box(0.14,0.22,0.08,(bx+lx,by+0.05,0.04),m_shoe,bevel=0.02))
    root=box(0.32,0.20,0.42,(bx,by,0.58),cloth,bevel=0.06)
    parts.append(box(0.34,0.21,0.10,(bx,by,0.40),cloth))
    parts.append(box(0.10,0.10,0.07,(bx,by,0.82),m_skin))
    parts.append(sphere(0.17,(bx,by,0.97),m_skin))
    parts.append(sphere(0.185,(bx,by-0.03,1.03),hair,squash=0.7))
    for ex in (-0.06,0.06):
        parts.append(box(0.035,0.02,0.045,(bx+ex,by+0.15,0.98),m_eye))
    for ax in (-0.21,0.21):
        parts.append(box(0.085,0.09,0.36,(bx+ax,by,0.58),cloth,bevel=0.03))
        parts.append(sphere(0.065,(bx+ax,by,0.38),m_skin))
    return root,parts

def umbrella_resident(name,bx,by,cloth,canopy,hair):
    for o in list(bpy.data.objects):
        if o.name==name: bpy.data.objects.remove(o,do_unlink=True)
    root,parts=person(bx,by,cloth,hair)
    hx=bx+0.21; hy=by+0.05
    parts.append(box(0.026,0.026,0.78,(hx,hy,1.06),m_stick))          # shaft
    parts.append(box(0.026,0.14,0.026,(hx,hy+0.07,0.68),m_stick))     # curved (J) handle base
    cap=cone(0.52,0.30,(hx,hy,1.55),canopy,verts=12)                  # POINTED umbrella canopy
    parts.append(cap)
    parts.append(box(0.022,0.022,0.12,(hx,hy,1.74),m_stick))          # ferrule tip
    for a in range(12):                                              # rib tips at canopy rim
        ang=a/12.0*2*math.pi
        parts.append(box(0.03,0.03,0.03,(hx+math.cos(ang)*0.5,hy+math.sin(ang)*0.5,1.42),canopy))
    o=join(root,parts); o.name=name; return o

def parachute_resident(name,bx,by,cloth,chute,hair):
    # like the umbrella but ~2x bigger and ROUND (a dome canopy), with suspension lines
    for o in list(bpy.data.objects):
        if o.name==name: bpy.data.objects.remove(o,do_unlink=True)
    root,parts=person(bx,by,cloth,hair)
    z=2.4
    parts.append(sphere(1.05,(bx,by,z),chute,segs=20,squash=0.56))   # big round canopy (~2x umbrella)
    for a in range(10):                                              # gore seams over the dome
        ang=a/10.0*2*math.pi
        parts.append(box(0.022,0.022,0.62,(bx+math.cos(ang)*0.62,by+math.sin(ang)*0.62,z+0.10),m_stick,
                         rot=(math.sin(ang)*0.5,-math.cos(ang)*0.5,0)))
    for (sx,sy) in [(-0.72,-0.72),(0.72,-0.72),(-0.72,0.72),(0.72,0.72),(-1.0,0),(1.0,0),(0,-1.0),(0,1.0)]:
        parts.append(box(0.016,0.016,1.35,(bx+sx*0.55,by+sy*0.55,z-0.9),m_stick))   # 8 suspension lines
    o=join(root,parts); o.name=name; return o

CLOTH=[(0.90,0.25,0.28,1),(0.20,0.45,0.85,1),(0.25,0.65,0.40,1),(0.95,0.55,0.18,1),(0.55,0.35,0.80,1),(0.90,0.40,0.62,1)]
CAN  =[(1.0,0.30,0.30,1),(0.25,0.6,1.0,1),(0.35,0.85,0.45,1),(1.0,0.65,0.2,1),(0.7,0.45,0.95,1),(1.0,0.5,0.75,1)]
def CM(i,t): return mat('R_%s%d'%(t,i),CLOTH[i] if t=='c' else CAN[i])

log=[]
# tidy row in front of the blocks (y = -3.5)
umbrella_resident('Resident_Umbrella_1',0,-3.5,CM(0,'c'),CM(0,'k'),HAIR[0]); log.append('umbrella 1')
umbrella_resident('Resident_Umbrella_2',2,-3.5,CM(1,'c'),CM(1,'k'),HAIR[1]); log.append('umbrella 2')
umbrella_resident('Resident_Umbrella_3',4,-3.5,CM(2,'c'),CM(2,'k'),HAIR[2]); log.append('umbrella 3')
parachute_resident('Resident_Parachute_1',6,-3.5,CM(3,'c'),CM(3,'k'),HAIR[0]); log.append('parachute 1')
parachute_resident('Resident_Parachute_2',8,-3.5,CM(4,'c'),CM(4,'k'),HAIR[1]); log.append('parachute 2')
parachute_resident('Resident_Parachute_3',10,-3.5,CM(5,'c'),CM(5,'k'),HAIR[2]); log.append('parachute 3')
bpy.context.view_layer.update()
print('RESIDENTS v3 (tidy):\n  '+'\n  '.join(log))
