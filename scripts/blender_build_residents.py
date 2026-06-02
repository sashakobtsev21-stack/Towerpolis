# Towerpolis residents (placeholder, low-poly, cute):
# 3 with umbrellas + 3 with parachutes, each a different bright color.
import bpy

def mat(name,rgba,rough=0.4,emis=0.0):
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

def box(sx,sy,sz,c,m=None,bevel=0.0):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1,location=c)
    o=bpy.context.active_object; o.scale=(sx,sy,sz); bpy.ops.object.transform_apply(scale=True)
    if m: o.data.materials.append(m)
    if bevel:
        md=o.modifiers.new('b','BEVEL'); md.width=bevel; md.segments=2
        bpy.context.view_layer.objects.active=o
        try: bpy.ops.object.modifier_apply(modifier='b')
        except Exception: pass
    return o

def sphere(r,c,m=None,segs=14,squash=1.0):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs,ring_count=max(6,segs//2),radius=r,location=c)
    o=bpy.context.active_object
    if squash!=1.0: o.scale=(1,1,squash); bpy.ops.object.transform_apply(scale=True)
    if m: o.data.materials.append(m)
    return o

def cone(r,depth,c,m=None,verts=14):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r,radius2=0,depth=depth,location=c)
    o=bpy.context.active_object
    if m: o.data.materials.append(m)
    return o

def join(main,parts):
    bpy.ops.object.select_all(action='DESELECT'); main.select_set(True)
    for p in parts:
        if p: p.select_set(True)
    bpy.context.view_layer.objects.active=main; bpy.ops.object.join(); return main

m_skin=mat('R_Skin',(0.98,0.80,0.66,1)); m_pants=mat('R_Pants',(0.25,0.28,0.36,1)); m_stick=mat('R_Stick',(0.5,0.5,0.55,1))

def person(bx,by,cloth):
    """Return (body_obj, parts, shoulder_z) for a cute chibi resident at (bx,by)."""
    legs=box(0.24,0.20,0.22,(bx,by,0.11),m_pants,bevel=0.04)
    torso=box(0.34,0.26,0.42,(bx,by,0.44),cloth,bevel=0.06)
    head=sphere(0.17,(bx,by,0.80),m_skin)
    armL=box(0.09,0.09,0.34,(bx-0.21,by,0.46),cloth,bevel=0.03)
    armR=box(0.09,0.09,0.34,(bx+0.21,by,0.46),cloth,bevel=0.03)
    return legs,[torso,head,armL,armR],0.66

def umbrella_resident(name,bx,by,cloth,canopy):
    clear(name)
    legs,parts,_=person(bx,by,cloth)
    stick=box(0.03,0.03,0.62,(bx+0.21,by,1.05),m_stick)            # held in right hand
    dome=cone(0.46,0.26,(bx+0.21,by,1.45),canopy)                  # umbrella canopy
    tip=box(0.03,0.03,0.08,(bx+0.21,by,1.62),m_stick)
    o=join(legs,parts+[stick,dome,tip]); o.name=name; return o

def parachute_resident(name,bx,by,cloth,chute):
    clear(name)
    legs,parts,_=person(bx,by,cloth)
    dome=sphere(0.72,(bx,by,1.95),chute,segs=18,squash=0.55)       # round parachute
    strings=[]
    for (sx,sy) in [(-0.5,-0.5),(0.5,-0.5),(-0.5,0.5),(0.5,0.5)]:
        strings.append(box(0.02,0.02,1.0,(bx+sx*0.5,by+sy*0.5,1.35),m_stick))
    o=join(legs,parts+[dome]+strings); o.name=name; return o

# 6 bright colors
COL=[(1.0,0.30,0.30,1),(0.25,0.55,1.0,1),(0.30,0.80,0.40,1),
     (1.0,0.62,0.18,1),(0.72,0.40,0.95,1),(1.0,0.45,0.75,1)]
def C(i,name): return mat('R_C%d'%i,COL[i])

log=[]
for i in range(3):  # umbrellas at y=-5
    umbrella_resident('Resident_Umbrella_%d'%(i+1), -5+i*1.6, -5, C(i,'cloth'), C(i,'can'))
    log.append('umbrella %d'%(i+1))
for i in range(3):  # parachutes at y=-7
    parachute_resident('Resident_Parachute_%d'%(i+1), -5+i*1.6, -7.5, C(i+3,'cloth'), C(i+3,'chute'))
    log.append('parachute %d'%(i+1))
bpy.context.view_layer.update()
print('RESIDENTS built:\n  '+'\n  '.join(log))
