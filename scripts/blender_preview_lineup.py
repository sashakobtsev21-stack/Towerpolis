# Towerpolis — clean front 3/4 reference render of all 6 blocks in an even row.
# Front faces (windows/doors/balconies) are on +Y, so the camera sits on the +Y side.
#   & "<blender.exe>" --background --python scripts/blender_preview_lineup.py
import bpy, os, math

REPO  = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BUILD = os.path.join(REPO, "scripts", "blender_build_blocks.py")
OUT   = os.path.join(REPO, "art", "blocks", "preview.png")

for o in list(bpy.data.objects):
    bpy.data.objects.remove(o, do_unlink=True)

with open(BUILD, encoding="utf-8") as f:
    exec(compile(f.read(), BUILD, "exec"), {})

# even, tidy row: 3 units apart, front (+Y) toward camera
ORDER = ['Base_Ground', 'Base_Ground_2', 'Floor_Standard',
         'Floor_Balcony', 'Floor_Balcony_2', 'Floor_Premium']
for i, n in enumerate(ORDER):
    ob = bpy.data.objects.get(n)
    if ob: ob.location = (i * 3.0, 0.0, 0.0)
cx = (len(ORDER) - 1) * 3.0 / 2.0   # row centre x

sc = bpy.context.scene
# Workbench in MATERIAL mode = flat per-material diffuse_color (no lighting washout) —
# the right engine for a clean low-poly colour reference.
sc.render.engine = 'BLENDER_WORKBENCH'
try: sc.view_settings.view_transform = 'Standard'
except Exception: pass
sh = sc.display.shading
sh.light = 'STUDIO'
sh.color_type = 'MATERIAL'
sh.show_shadows = True
sh.shadow_intensity = 0.30
try:
    sh.show_cavity = True
    sh.cavity_type = 'WORLD'
except Exception: pass
try:
    w = bpy.data.worlds[0]; w.use_nodes = True
    bg = w.node_tree.nodes.get('Background')
    if bg:
        bg.inputs[0].default_value = (0.86, 0.90, 0.96, 1)
        bg.inputs[1].default_value = 1.0
except Exception: pass

# target empty + tracked camera on the FRONT (+Y) side, elevated for a 3/4 view
tgt = bpy.data.objects.new('TP_Target', None)
bpy.context.collection.objects.link(tgt); tgt.location = (cx, 0.0, 0.9)
cam_data = bpy.data.cameras.new('TP_Cam'); cam_data.lens = 40
cam = bpy.data.objects.new('TP_Cam', cam_data)
bpy.context.collection.objects.link(cam); sc.camera = cam
cam.location = (cx + 2.0, 21.0, 8.5)
con = cam.constraints.new('TRACK_TO'); con.target = tgt
con.track_axis = 'TRACK_NEGATIVE_Z'; con.up_axis = 'UP_Y'

sc.render.resolution_x = 1600; sc.render.resolution_y = 700
sc.render.film_transparent = False
sc.render.filepath = OUT
bpy.ops.render.render(write_still=True)
print("preview lineup ok ->", OUT)
