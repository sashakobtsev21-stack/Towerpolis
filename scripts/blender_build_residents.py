# Towerpolis residents v2 — more human (legs+shoes, torso, arms+hands, head+hair+eyes):
# 3 with umbrellas + parachutes = 2 paraglider WINGS + 1 round DOME. 6 bright colors.
import bpy

def mat(name,rgba,rough=0.45):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes=True; m.diffuse_color=rgba
    b=m.node_tree.nodes.get('Principled BSDF')
    if b:
        if 'Base Color' in b.inputs: b.inputs['Base Color'].default_value=rgba
        if 'Roughness' in b.inputs: b.inputs['Roughness'].default_value=rough
    return m

def clear(n):
    o=bpy.data.objects.get(n)
    if o: bpy.data.objects.remove(o,do_unlink=True)

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

def join(main,parts):
    bpy.ops.object.select_all(action='DESELECT'); main.select_set(True)
    for p in parts:
        if p: p.select_set(True)
    bpy.context.view_layer.objects.active=main; bpy.ops.object.join(); return main

m_skin =mat('R_Skin',(0.98,0.78,0.62,1))
m_shoe =mat('R_Shoe',(0.22,0.18,0.16,1))
m_pants=mat('R_Pants',(0.28,0.30,0.40,1))
m_eye  =mat('R_Eye',(0.10,0.10,0.12,1))
m_stick=mat('R_Stick',(0.45,0.45,0.5,1))
HAIR=[mat('R_Hair0',(0.20,0.13,0.08,1)),mat('R_Hair1',(0.08,0.07,0.07,1)),mat('R_Hair2',(0.55,0.36,0.12,1))]

def person(bx,by,cloth,hair):
    """Return (root, parts) — a cute but clearly-human figure standing at (bx,by)."""
    parts=[]
    for lx in (-0.09,0.09):                                   # legs + shoes
        parts.append(box(0.12,0.13,0.36,(bx+lx,by,0.18),m_pants,bevel=0.03))
        parts.append(box(0.14,0.22,0.08,(bx+lx,by+0.05,0.04),m_shoe,bevel=0.02))
    root=box(0.32,0.20,0.42,(bx,by,0.58),cloth,bevel=0.06)    # torso (root)
    parts.append(box(0.34,0.21,0.10,(bx,by,0.40),cloth))     # hips
    parts.append(box(0.10,0.10,0.07,(bx,by,0.82),m_skin))    # neck
    parts.append(sphere(0.17,(bx,by,0.97),m_skin))           # head
    parts.append(sphere(0.185,(bx,by-0.03,1.03),hair,squash=0.7))  # hair cap
    for ex in (-0.06,0.06):                                   # eyes
        parts.append(box(0.035,0.02,0.045,(bx+ex,by+0.15,0.98),m_eye))
    for ax in (-0.21,0.21):                                   # arms + hands
        parts.append(box(0.085,0.09,0.36,(bx+ax,by,0.58),cloth,bevel=0.03))
        parts.append(sphere(0.065,(bx+ax,by,0.38),m_skin))
    return root,parts

def umbrella_resident(name,bx,by,cloth,canopy,hair):
    clear(name)
    root,parts=person(bx,by,cloth,hair)
    parts.append(box(0.028,0.028,0.66,(bx+0.21,by+0.05,1.10),m_stick))   # pole from hand
    parts.append(sphere(0.46,(bx+0.21,by+0.05,1.5),canopy,squash=0.45))  # rounded umbrella dome
    parts.append(box(0.028,0.028,0.08,(bx+0.21,by+0.05,1.62),m_stick))   # tip
    o=join(root,parts); o.name=name; return o

def wing_resident(name,bx,by,cloth,wing,hair):
    """Paraglider — a wide arched WING canopy + suspension lines."""
    clear(name)
    root,parts=person(bx,by,cloth,hair)
    z=2.0
    parts.append(box(1.7,0.55,0.07,(bx,by,z),wing))                       # center span
    parts.append(box(0.55,0.55,0.07,(bx-1.0,by,z-0.16),wing,rot=(0,0.5,0)))   # left tip down
    parts.append(box(0.55,0.55,0.07,(bx+1.0,by,z-0.16),wing,rot=(0,-0.5,0)))  # right tip down
    for cx in (-0.6,-0.3,0,0.3,0.6):                                      # cell dividers
        parts.append(box(0.025,0.55,0.10,(bx+cx,by,z-0.05),m_skin))
    for cx in (-0.85,-0.45,0.45,0.85):                                    # suspension lines
        parts.append(box(0.015,0.015,1.05,(bx+cx*0.55,by,z-0.62),m_stick,rot=(0,cx*0.12,0)))
    o=join(root,parts); o.name=name; return o

def dome_resident(name,bx,by,cloth,chute,hair):
    """Round parachute (the one that flies into the premium floor)."""
    clear(name)
    root,parts=person(bx,by,cloth,hair)
    parts.append(sphere(0.78,(bx,by,2.05),chute,segs=18,squash=0.55))     # dome
    for (sx,sy) in [(-0.55,-0.55),(0.55,-0.55),(-0.55,0.55),(0.55,0.55)]:
        parts.append(box(0.018,0.018,1.1,(bx+sx*0.5,by+sy*0.4,1.45),m_stick))
    o=join(root,parts); o.name=name; return o

CLOTH=[(0.90,0.25,0.28,1),(0.20,0.45,0.85,1),(0.25,0.65,0.40,1),
       (0.95,0.55,0.18,1),(0.55,0.35,0.80,1),(0.90,0.40,0.62,1)]
CAN  =[(1.0,0.30,0.30,1),(0.25,0.6,1.0,1),(0.35,0.85,0.45,1),
       (1.0,0.65,0.2,1),(0.7,0.45,0.95,1),(1.0,0.5,0.75,1)]
def CM(i,t): return mat('R_%s%d'%(t,i),CLOTH[i] if t=='c' else CAN[i])

log=[]
for i in range(3):  # 3 umbrellas
    umbrella_resident('Resident_Umbrella_%d'%(i+1), -5+i*1.7, -5, CM(i,'c'), CM(i,'k'), HAIR[i%3]); log.append('umbrella %d'%(i+1))
wing_resident('Resident_Wing_1', -5,   -7.6, CM(3,'c'), CM(3,'k'), HAIR[0]); log.append('wing 1')
wing_resident('Resident_Wing_2', -3.3, -7.6, CM(4,'c'), CM(4,'k'), HAIR[1]); log.append('wing 2')
dome_resident('Resident_Dome_1', -1.6, -7.6, CM(5,'c'), CM(5,'k'), HAIR[2]); log.append('dome 1')
bpy.context.view_layer.update()
print('RESIDENTS v2:\n  '+'\n  '.join(log))
