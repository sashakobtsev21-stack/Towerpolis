# Towerpolis blocks v7:
# bright colors (green/yellow/blue, pink->green), bluer glossy glass, BLANK back wall on
# all floors, base has NO windows, window canopies (light-brown / marble on premium),
# wooden balcony (mid) + marble balcony (premium) on the FRONT only, entrance dark-brown
# canopy + grey steps. Every door has handles.
import bpy
FH=1.5; FB=0.04; MM=0.018

def mat(name,rgba,rough=0.3,metal=0.0,emis=0.0,spec=0.6):
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

def box(sx,sy,sz,c,m=None):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1,location=c)
    o=bpy.context.active_object; o.scale=(sx,sy,sz); bpy.ops.object.transform_apply(scale=True)
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

def win(bx,face,off,cz,W,H,white,glass,vm=1,hm=1,canopy=None):
    p=[]
    if face in ('F','B'):
        y=1.0 if face=='F' else -1.0; n=1 if face=='F' else -1
        p.append(box(W,0.06,H,(bx+off,y,cz),glass))
        p.append(box(W+0.08,0.06,FB,(bx+off,y+n*0.03,cz+H/2),white))
        p.append(box(W+0.08,0.06,FB,(bx+off,y+n*0.03,cz-H/2),white))
        p.append(box(FB,0.06,H+0.08,(bx+off-W/2,y+n*0.03,cz),white))
        p.append(box(FB,0.06,H+0.08,(bx+off+W/2,y+n*0.03,cz),white))
        for k in range(vm):
            mx=-W/2+W*(k+1)/(vm+1); p.append(box(MM,0.06,H,(bx+off+mx,y+n*0.03,cz),white))
        for k in range(hm):
            mz=-H/2+H*(k+1)/(hm+1); p.append(box(W,0.06,MM,(bx+off,y+n*0.03,cz+mz),white))
        if canopy:
            p.append(box(W+0.26,0.30,0.06,(bx+off,y+n*0.16,cz+H/2+0.16),canopy))
            for sx in (-W/2-0.04,W/2+0.04):
                p.append(box(0.05,0.28,0.16,(bx+off+sx,y+n*0.11,cz+H/2+0.07),canopy))
    else:
        x=1.0 if face=='R' else -1.0; n=1 if face=='R' else -1
        p.append(box(0.06,W,H,(bx+x,off,cz),glass))
        p.append(box(0.06,W+0.08,FB,(bx+x+n*0.03,off,cz+H/2),white))
        p.append(box(0.06,W+0.08,FB,(bx+x+n*0.03,off,cz-H/2),white))
        p.append(box(0.06,FB,H+0.08,(bx+x+n*0.03,off-W/2,cz),white))
        p.append(box(0.06,FB,H+0.08,(bx+x+n*0.03,off+W/2,cz),white))
        for k in range(vm):
            my=-W/2+W*(k+1)/(vm+1); p.append(box(0.06,MM,H,(bx+x+n*0.03,off+my,cz),white))
        for k in range(hm):
            mz=-H/2+H*(k+1)/(hm+1); p.append(box(0.06,W,MM,(bx+x+n*0.03,off,cz+mz),white))
        if canopy:
            p.append(box(0.30,W+0.26,0.06,(bx+x+n*0.16,off,cz+H/2+0.16),canopy))
            for sy in (-W/2-0.04,W/2+0.04):
                p.append(box(0.28,0.05,0.16,(bx+x+n*0.11,off+sy,cz+H/2+0.07),canopy))
    return p

def windows(bx,faces,offs,cz,W,H,white,glass,vm=1,hm=1,canopy=None):
    p=[]
    for f in faces:
        for o in offs: p+=win(bx,f,o,cz,W,H,white,glass,vm,hm,canopy)
    return p

def rim(bx,white,t=0.12):
    return [box(2.12,2.12,t,(bx,0,t/2.0),white)]

def door_unit(bx,face,off,dw,dh,white,panel,gold):
    p=[]; cz=dh/2.0+0.06
    if face in ('F','B'):
        y=1.0 if face=='F' else -1.0; n=1 if face=='F' else -1; fy=y+n*0.03
        p.append(box(dw,0.07,dh,(bx+off,y,cz),panel))
        p.append(box(dw+0.10,0.07,FB,(bx+off,fy,cz+dh/2),white))
        p.append(box(dw+0.10,0.07,FB,(bx+off,fy,cz-dh/2),white))
        p.append(box(FB,0.07,dh+0.10,(bx+off-dw/2,fy,cz),white))
        p.append(box(FB,0.07,dh+0.10,(bx+off+dw/2,fy,cz),white))
        p.append(box(MM,0.07,dh,(bx+off,fy,cz),white))
        p.append(box(dw,0.07,MM,(bx+off,fy,0.42),white))
        p.append(box(0.05,0.09,0.16,(bx+off-0.07,y+n*0.06,cz),gold))
        p.append(box(0.05,0.09,0.16,(bx+off+0.07,y+n*0.06,cz),gold))
    else:
        x=1.0 if face=='R' else -1.0; n=1 if face=='R' else -1; fx=x+n*0.03
        p.append(box(0.07,dw,dh,(bx+x,off,cz),panel))
        p.append(box(0.07,dw+0.10,FB,(bx+fx,off,cz+dh/2),white))
        p.append(box(0.07,dw+0.10,FB,(bx+fx,off,cz-dh/2),white))
        p.append(box(0.07,FB,dh+0.10,(bx+fx,off-dw/2,cz),white))
        p.append(box(0.07,FB,dh+0.10,(bx+fx,off+dw/2,cz),white))
        p.append(box(0.07,MM,dh,(bx+fx,off,cz),white))
        p.append(box(0.07,dw,MM,(bx+fx,off,0.42),white))
        p.append(box(0.09,0.05,0.16,(bx+x+n*0.06,off-0.07,cz),gold))
        p.append(box(0.09,0.05,0.16,(bx+x+n*0.06,off+0.07,cz),gold))
    return p

def balcony(bx,face,rail,w,d=0.45):
    p=[]; sh=0.16; rh=0.46
    if face in ('F','B'):
        n=1 if face=='F' else -1; yb=1.0*n; yo=yb+n*d
        p.append(box(w,d,0.08,(bx,yb+n*d/2,sh),rail))
        for px in (-w/2,w/2):
            p.append(box(0.06,0.06,rh,(bx+px,yb+n*0.06,sh+rh/2),rail))
            p.append(box(0.06,0.06,rh,(bx+px,yo-n*0.06,sh+rh/2),rail))
            p.append(box(0.05,d,0.05,(bx+px,yb+n*d/2,sh+rh),rail))
        for px in (-w/4,0,w/4):
            p.append(box(0.05,0.05,rh,(bx+px,yo-n*0.06,sh+rh/2),rail))
        p.append(box(w,0.06,0.06,(bx,yo-n*0.06,sh+rh),rail))
    return p

def entrance(bx,white,door,gold,canopy,steps):
    p=[]; dw=0.8; dh=1.0; cz=dh/2+0.04
    p.append(box(dw+0.14,0.10,dh+0.14,(bx,1.0,cz),white))
    p.append(box(dw,0.12,dh,(bx,1.0,cz),door))
    p.append(box(0.025,0.14,dh,(bx,1.02,cz),white))
    p.append(box(0.05,0.07,0.12,(bx-0.13,1.05,0.55),gold))
    p.append(box(0.05,0.07,0.12,(bx+0.13,1.05,0.55),gold))
    p.append(box(1.4,0.55,0.08,(bx,1.18,dh+0.16),canopy))             # dark-brown canopy
    p.append(box(0.07,0.5,0.22,(bx-0.6,1.2,dh+0.02),canopy))
    p.append(box(0.07,0.5,0.22,(bx+0.6,1.2,dh+0.02),canopy))
    p.append(box(1.3,0.55,0.1,(bx,1.5,0.05),steps))                  # grey steps
    p.append(box(1.05,0.3,0.18,(bx,1.4,0.12),steps))
    return p

# ---- materials ----
m_green = mat('TP_Green', (0.32,0.78,0.38,1), rough=0.25)   # standard (was pink)
m_yellow= mat('TP_Yellow',(1.00,0.82,0.15,1), rough=0.25)   # balcony
m_blue  = mat('TP_Blue',  (0.18,0.58,1.00,1), rough=0.25)   # premium
m_white = mat('TP_White', (0.99,0.99,0.97,1), rough=0.30)
m_glass = mat('TP_Glass', (0.20,0.48,1.00,1), rough=0.03, emis=0.5, spec=1.0)  # bluer + glossy
m_wood  = mat('TP_Wood',  (0.58,0.40,0.22,1), rough=0.5)    # mid balcony
m_marble= mat('TP_Marble',(0.92,0.92,0.95,1), rough=0.15)   # premium balcony + canopy
m_canlb = mat('TP_CanopyLB',(0.74,0.56,0.36,1), rough=0.5)  # light-brown window canopy
m_brick = mat('TP_Brick', (0.78,0.40,0.32,1), rough=0.65)
m_brickL= mat('TP_BrickLine',(0.62,0.30,0.23,1), rough=0.75)
m_dbrown= mat('TP_DarkBrown',(0.34,0.22,0.13,1), rough=0.45)# entrance door + canopy
m_steps = mat('TP_Steps', (0.60,0.62,0.65,1), rough=0.6)    # grey steps
m_gold  = mat('TP_Gold',  (1.00,0.82,0.30,1), rough=0.2, metal=1.0)
m_grey  = mat('TP_Grey',  (0.72,0.74,0.77,1), rough=0.6)

NAMES=['Floor_Standard','Floor_Balcony','Floor_Premium','Roof_Cap','Base_Ground']
for n in NAMES: clear(n)
log=[]; CZ=FH/2.0
def safe(l,f):
    try: f(); log.append(l+' ok')
    except Exception as e: log.append(l+' ERR '+repr(e)[:120])

def b1():  # Standard — bright green; windows F/L/R (blank back); light-brown canopies
    o=body('Floor_Standard',2,2,FH,0,m_green)
    join(o, rim(0,m_white)+windows(0,('F','L','R'),(-0.44,0.44),CZ,0.58,0.95,m_white,m_glass,1,1,m_canlb))
safe('Floor_Standard',b1)

def b2():  # Balcony — yellow; WOODEN balcony+door front; windows L/R (blank back)
    o=body('Floor_Balcony',2,2,FH,3,m_yellow)
    p=rim(3,m_white)+windows(3,('L','R'),(-0.44,0.44),CZ,0.58,0.95,m_white,m_glass,1,1,m_canlb)
    p+=balcony(3,'F',m_wood,1.3)+door_unit(3,'F',0.0,0.62,1.25,m_white,m_glass,m_gold)
    join(o,p)
safe('Floor_Balcony',b2)

def b3():  # Premium — blue; MARBLE balcony+door+2 windows FRONT; big window L/R; blank back
    o=body('Floor_Premium',2,2,FH,6,m_blue)
    p=rim(6,m_white)
    p+=balcony(6,'F',m_marble,1.5,d=0.5)
    p+=door_unit(6,'F',0.0,0.6,1.3,m_white,m_glass,m_gold)
    for s in (-0.74,0.74):
        p+=win(6,'F',s,0.82,0.28,1.0,m_white,m_glass,vm=0,hm=2,canopy=m_marble)
    p+=win(6,'L',0.0,CZ,1.0,1.1,m_white,m_glass,vm=1,hm=0,canopy=m_marble)
    p+=win(6,'R',0.0,CZ,1.0,1.1,m_white,m_glass,vm=1,hm=0,canopy=m_marble)
    for (px,py) in [(-0.95,-0.95),(0.95,-0.95),(-0.95,0.95),(0.95,0.95)]:
        p.append(box(0.1,0.1,FH,(6+px,py,FH/2),m_white))
    join(o,p)
safe('Floor_Premium',b3)

def b4():  # Roof — clean
    o=body('Roof_Cap',2,2,0.3,9,m_white,bevel=0.04)
    p=[]
    for (dx,dy,sx,sy) in [(0,0.94,2.0,0.12),(0,-0.94,2.0,0.12),(0.94,0,0.12,2.0),(-0.94,0,0.12,2.0)]:
        p.append(box(sx,sy,0.22,(9+dx,dy,0.30),m_white))
    p.append(box(0.9,0.9,0.5,(9,-0.2,0.55),m_white))
    p.append(box(0.36,0.36,0.18,(9.45,0.45,0.39),m_grey))
    p.append(box(0.3,0.5,0.14,(8.6,0.4,0.37),m_grey))
    p.append(box(0.05,0.05,0.5,(9.6,-0.6,0.55),m_grey))
    join(o,p)
safe('Roof_Cap',b4)

def b5():  # Base — brick, NO windows, entrance (dark-brown canopy + grey steps)
    o=body('Base_Ground',2.0,2.0,1.5,12,m_brick)
    p=[box(2.08,2.08,0.1,(12,0,1.45),m_white)]
    for z in (0.4,0.75,1.1):
        p.append(box(2.04,2.04,0.02,(12,0,z),m_brickL))
    p+=entrance(12,m_white,m_dbrown,m_gold,m_dbrown,m_steps)
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
print("BUILT %d/5 (v7):\n%s"%(sum(1 for n in NAMES if bpy.data.objects.get(n)),"\n".join(log)))
