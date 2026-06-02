# Towerpolis blocks v8: full-width premium balcony + single continuous canopy; all
# canopies 2x narrower+thinner, no vertical brackets; full-width canopy on standard;
# glass much bluer + glossy clearcoat; NO roof.
import bpy, math
FH=1.5; FB=0.04; MM=0.018

def mat(name,rgba,rough=0.3,metal=0.0,emis=0.0,spec=0.6,coat=0.0):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes=True; m.diffuse_color=rgba
    b=m.node_tree.nodes.get('Principled BSDF')
    if b:
        I=b.inputs
        if 'Base Color' in I: I['Base Color'].default_value=rgba
        if 'Roughness' in I: I['Roughness'].default_value=rough
        if 'Metallic' in I: I['Metallic'].default_value=metal
        if 'Specular' in I: I['Specular'].default_value=spec
        elif 'Specular IOR Level' in I: I['Specular IOR Level'].default_value=spec
        if 'Coat Weight' in I: I['Coat Weight'].default_value=coat
        elif 'Clearcoat' in I: I['Clearcoat'].default_value=coat
        if 'Emission Color' in I:
            I['Emission Color'].default_value=rgba
            if 'Emission Strength' in I: I['Emission Strength'].default_value=emis
        elif 'Emission' in I:
            I['Emission'].default_value=rgba
            if 'Emission Strength' in I: I['Emission Strength'].default_value=emis
    return m

def clear(n):
    o=bpy.data.objects.get(n)
    if o: bpy.data.objects.remove(o,do_unlink=True)

def box(sx,sy,sz,c,m=None,rot=None):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1,location=c)
    o=bpy.context.active_object; o.scale=(sx,sy,sz)
    if rot: o.rotation_euler=rot
    # keep location=False so origin stays at c -> rotation bakes about the box CENTER (not world 0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if m: o.data.materials.append(m)
    return o

def body(name,sx,sy,sz,bx,m,bevel=0.06):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,0))
    o=bpy.context.active_object; o.name=name; o.scale=(sx,sy,sz); bpy.ops.object.transform_apply(scale=True)
    bpy.context.scene.cursor.location=(0,0,-sz/2.0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.context.scene.cursor.location=(0,0,0); o.location=(bx,0,0); o.data.materials.append(m)
    md=o.modifiers.new('b','BEVEL'); md.width=bevel; md.segments=2; md.limit_method='ANGLE'
    bpy.context.view_layer.objects.active=o
    try: bpy.ops.object.modifier_apply(modifier='b')
    except Exception: pass
    return o

def join(main,parts):
    bpy.ops.object.select_all(action='DESELECT'); main.select_set(True)
    for p in parts:
        if p: p.select_set(True)
    bpy.context.view_layer.objects.active=main; bpy.ops.object.join(); return main

def awn(bx,face,off,topz,wid,m,proj=0.09,th=0.025,tilt=0.7):
    # sloped awning whose BACK-TOP edge is anchored at (wall, topz); slopes ONLY down-&-out
    # (its highest point is topz -> never pokes up through the floor top; sits above the frame)
    co=proj/2*math.cos(tilt); so=proj/2*math.sin(tilt); tco=th/2*math.cos(tilt); tso=th/2*math.sin(tilt)
    if face in ('F','B'):
        n=1 if face=='F' else -1
        return [box(wid,proj,th,(bx+off, 1.0*n+n*(co-tso), topz-(so+tco)),m,rot=(-n*tilt,0,0))]
    sgn=1 if face=='R' else -1
    return [box(proj,wid,th,(bx+1.0*sgn+sgn*(co-tso), off, topz-(so+tco)),m,rot=(0,sgn*tilt,0))]

def win(bx,face,off,cz,W,H,white,glass,vm=1,hm=1,canopy=None):
    p=[]
    if face in ('F','B'):
        y=1.0 if face=='F' else -1.0; n=1 if face=='F' else -1
        gy=y-n*0.015; fy=y                                 # glass slightly proud (no z-fight), frame recessed
        p.append(box(W,0.06,H,(bx+off,gy,cz),glass))
        p.append(box(W+0.08,0.05,FB,(bx+off,fy,cz+H/2),white))
        p.append(box(W+0.08,0.05,FB,(bx+off,fy,cz-H/2),white))
        p.append(box(FB,0.05,H+0.08,(bx+off-W/2,fy,cz),white))
        p.append(box(FB,0.05,H+0.08,(bx+off+W/2,fy,cz),white))
        for k in range(vm):
            mx=-W/2+W*(k+1)/(vm+1); p.append(box(MM,0.05,H,(bx+off+mx,fy,cz),white))
        for k in range(hm):
            mz=-H/2+H*(k+1)/(hm+1); p.append(box(W,0.05,MM,(bx+off,fy,cz+mz),white))
        if canopy: p+=awn(bx,face,off,cz+H/2+0.07,W+0.16,canopy)
    else:
        x=1.0 if face=='R' else -1.0; n=1 if face=='R' else -1
        gx=x-n*0.015; fx=x
        p.append(box(0.06,W,H,(bx+gx,off,cz),glass))
        p.append(box(0.05,W+0.08,FB,(bx+fx,off,cz+H/2),white))
        p.append(box(0.05,W+0.08,FB,(bx+fx,off,cz-H/2),white))
        p.append(box(0.05,FB,H+0.08,(bx+fx,off-W/2,cz),white))
        p.append(box(0.05,FB,H+0.08,(bx+fx,off+W/2,cz),white))
        for k in range(vm):
            my=-W/2+W*(k+1)/(vm+1); p.append(box(0.05,MM,H,(bx+fx,off+my,cz),white))
        for k in range(hm):
            mz=-H/2+H*(k+1)/(hm+1); p.append(box(0.05,W,MM,(bx+fx,off,cz+mz),white))
        if canopy: p+=awn(bx,face,off,cz+H/2+0.07,W+0.16,canopy)
    return p

def windows(bx,faces,offs,cz,W,H,white,glass,vm=1,hm=1,canopy=None):
    p=[]
    for f in faces:
        for o in offs: p+=win(bx,f,o,cz,W,H,white,glass,vm,hm,canopy)
    return p

def full_canopy(bx,face,m,z0,w=2.08,proj=0.28,th=0.04,tilt=0.5):
    # sloped awning whose INNER edge stays flush on the wall (anchored, not floating)
    co=(proj/2)*math.cos(tilt); so=(proj/2)*math.sin(tilt)
    if face in ('F','B'):
        n=1 if face=='F' else -1
        return [box(w,proj,th,(bx, 1.0*n+n*co, z0-so),m,rot=(-n*tilt,0,0))]
    sgn=1 if face=='R' else -1
    return [box(proj,w,th,(bx+1.0*sgn+sgn*co, 0, z0-so),m,rot=(0,sgn*tilt,0))]

def rim(bx,white,t=0.12):
    return [box(2.12,2.12,t,(bx,0,t/2.0),white)]

def door_unit(bx,face,off,dw,dh,white,panel,gold):
    p=[]; cz=dh/2.0+0.06
    if face in ('F','B'):
        y=1.0 if face=='F' else -1.0; n=1 if face=='F' else -1; fy=y       # frame recessed (half in wall)
        p.append(box(dw,0.07,dh,(bx+off,y-n*0.015,cz),panel))             # glass slightly proud
        p.append(box(dw+0.10,0.07,FB,(bx+off,fy,cz+dh/2),white))
        p.append(box(dw+0.10,0.07,FB,(bx+off,fy,cz-dh/2),white))
        p.append(box(FB,0.07,dh+0.10,(bx+off-dw/2,fy,cz),white))
        p.append(box(FB,0.07,dh+0.10,(bx+off+dw/2,fy,cz),white))
        p.append(box(MM,0.07,dh,(bx+off,fy,cz),white))
        p.append(box(dw,0.07,MM,(bx+off,fy,0.42),white))
        p.append(box(0.04,0.06,0.11,(bx+off-0.07,y+n*0.06,cz),gold))
        p.append(box(0.04,0.06,0.11,(bx+off+0.07,y+n*0.06,cz),gold))
    else:
        x=1.0 if face=='R' else -1.0; n=1 if face=='R' else -1; fx=x         # frame recessed (half in wall)
        p.append(box(0.07,dw,dh,(bx+x-n*0.015,off,cz),panel))               # glass slightly proud
        p.append(box(0.07,dw+0.10,FB,(bx+fx,off,cz+dh/2),white))
        p.append(box(0.07,dw+0.10,FB,(bx+fx,off,cz-dh/2),white))
        p.append(box(0.07,FB,dh+0.10,(bx+fx,off-dw/2,cz),white))
        p.append(box(0.07,FB,dh+0.10,(bx+fx,off+dw/2,cz),white))
        p.append(box(0.07,MM,dh,(bx+fx,off,cz),white))
        p.append(box(0.07,dw,MM,(bx+fx,off,0.42),white))
        p.append(box(0.06,0.04,0.11,(bx+x+n*0.06,off-0.07,cz),gold))
        p.append(box(0.06,0.04,0.11,(bx+x+n*0.06,off+0.07,cz),gold))
    return p

def balcony(bx,face,rail,w,d=0.45):
    p=[]; sh=0.16; rh=0.46
    n=1 if face=='F' else -1; yb=1.0*n; yo=yb+n*d
    p.append(box(w,d,0.08,(bx,yb+n*d/2,sh),rail))
    for px in (-w/2,w/2):
        p.append(box(0.06,0.06,rh,(bx+px,yb+n*0.06,sh+rh/2),rail))
        p.append(box(0.06,0.06,rh,(bx+px,yo-n*0.06,sh+rh/2),rail))
        p.append(box(0.05,d,0.05,(bx+px,yb+n*d/2,sh+rh),rail))
    nb=int(w/0.33)
    for i in range(nb+1):
        px=-w/2+w*i/nb; p.append(box(0.05,0.05,rh,(bx+px,yo-n*0.06,sh+rh/2),rail))
    p.append(box(w,0.06,0.06,(bx,yo-n*0.06,sh+rh),rail))
    return p

def entrance(bx,white,door,gold,canopy,steps,rail):
    p=[]; dw=0.8; dh=1.0; bz=0.22; cz=bz+dh/2   # door RAISED (bottom at bz)
    p.append(box(dw+0.14,0.10,dh+0.14,(bx,1.0,cz),white))
    p.append(box(dw,0.12,dh,(bx,1.0,cz),door))
    p.append(box(0.025,0.14,dh,(bx,1.02,cz),white))
    p.append(box(0.035,0.05,0.09,(bx-0.13,1.05,cz),gold))
    p.append(box(0.035,0.05,0.09,(bx+0.13,1.05,cz),gold))
    p+=awn(bx,'F',0.0,dh+bz+0.20,1.5,canopy)          # awning above entrance door — raised higher
    # steps pulled IN toward the house
    p.append(box(1.2,0.34,0.12,(bx,1.18,0.06),steps))
    p.append(box(1.0,0.22,0.20,(bx,1.12,0.14),steps))
    # handrails beside the steps
    for sx in (-0.62,0.62):
        p.append(box(0.05,0.05,0.42,(bx+sx,1.16,0.30),rail))   # post in
        p.append(box(0.05,0.05,0.42,(bx+sx,1.34,0.24),rail))   # post out
        p.append(box(0.05,0.34,0.05,(bx+sx,1.25,0.46),rail))   # rail
    return p

# ---- materials ----
m_green = mat('TP_Green', (0.32,0.78,0.38,1), rough=0.25)
m_yellow= mat('TP_Yellow',(1.00,0.82,0.15,1), rough=0.25)
m_blue  = mat('TP_Blue',  (0.18,0.58,1.00,1), rough=0.25)
m_white = mat('TP_White', (0.99,0.99,0.97,1), rough=0.30)
m_glass = mat('TP_Glass', (0.10,0.40,1.00,1), rough=0.02, emis=0.6, spec=1.0, coat=0.6)  # much bluer + glossy
m_wood  = mat('TP_Wood',  (0.58,0.40,0.22,1), rough=0.5)
m_marble= mat('TP_Marble',(0.92,0.92,0.95,1), rough=0.12, coat=0.4)
m_canlb = mat('TP_CanopyLB',(0.74,0.56,0.36,1), rough=0.5)
m_brick = mat('TP_Brick', (0.78,0.40,0.32,1), rough=0.65)
m_brickL= mat('TP_BrickLine',(0.62,0.30,0.23,1), rough=0.75)
m_dbrown= mat('TP_DarkBrown',(0.34,0.22,0.13,1), rough=0.45)
m_steps = mat('TP_Steps', (0.60,0.62,0.65,1), rough=0.6)
m_gold  = mat('TP_Gold',  (1.00,0.82,0.30,1), rough=0.2, metal=1.0)

NAMES=['Floor_Standard','Floor_Balcony','Floor_Premium','Base_Ground']  # NO roof
for n in NAMES+['Roof_Cap']: clear(n)
log=[]; CZ=FH/2.0
def safe(l,f):
    try: f(); log.append(l+' ok')
    except Exception as e: log.append(l+' ERR '+repr(e)[:120])

def b1():  # Standard green; windows F/L/R; ONE full-width canopy per window-face
    o=body('Floor_Standard',2,2,FH,0,m_green)
    p=rim(0,m_white)+windows(0,('F','L','R'),(-0.44,0.44),CZ,0.58,0.95,m_white,m_glass,1,1,m_canlb)
    join(o,p)
safe('Floor_Standard',b1)

def b2():  # Balcony yellow; wooden balcony+door front; windows L/R + full-width canopy
    o=body('Floor_Balcony',2,2,FH,3,m_yellow)
    p=rim(3,m_white)+windows(3,('L','R'),(-0.44,0.44),CZ,0.58,0.95,m_white,m_glass,1,1,m_canlb)
    p+=balcony(3,'F',m_wood,1.3)+door_unit(3,'F',0.0,0.62,1.25,m_white,m_glass,m_gold)
    p+=awn(3,'F',0.0,1.38,1.7,m_canlb)                # awning above balcony door — wide (a bit narrower)
    join(o,p)
safe('Floor_Balcony',b2)

def b3():  # Premium blue; FULL-WIDTH marble balcony + door + 2 windows + ONE continuous canopy; big side windows
    o=body('Floor_Premium',2,2,FH,6,m_blue)
    p=rim(6,m_white)
    p+=balcony(6,'F',m_marble,1.92,d=0.5)
    p+=door_unit(6,'F',0.0,0.6,1.3,m_white,m_glass,m_gold)
    for s in (-0.74,0.74):
        p+=win(6,'F',s,0.82,0.28,1.0,m_white,m_glass,vm=0,hm=2,canopy=m_marble)
    p+=awn(6,'F',0.0,1.43,0.92,m_marble)              # awning above premium door
    p+=win(6,'L',0.0,CZ,1.0,1.1,m_white,m_glass,vm=1,hm=0,canopy=m_marble)
    p+=win(6,'R',0.0,CZ,1.0,1.1,m_white,m_glass,vm=1,hm=0,canopy=m_marble)
    join(o,p)   # (premium corner posts removed)
safe('Floor_Premium',b3)

def b5():  # Base brick, no windows, entrance (dark-brown canopy + grey steps)
    o=body('Base_Ground',2.0,2.0,1.5,12,m_brick)
    p=[box(2.08,2.08,0.1,(12,0,1.45),m_white)]
    for z in (0.4,0.75,1.1):
        p.append(box(2.04,2.04,0.02,(12,0,z),m_brickL))
    p+=entrance(12,m_white,m_dbrown,m_gold,m_dbrown,m_steps,m_white)
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
    if ob: log.append("  %-15s tris~%d"%(n,sum(len(p.vertices)-2 for p in ob.data.polygons)))
print("BUILT %d/4 (v8, no roof):\n%s"%(sum(1 for n in NAMES if bpy.data.objects.get(n)),"\n".join(log)))
