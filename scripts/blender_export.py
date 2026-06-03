# Towerpolis — headless export of the gameplay blocks to committable game-ready assets.
# Runs the canonical build recipe (blender_build_blocks.py), then writes:
#   art/blocks/Towerpolis_Blocks.blend       master editable source (all 6 blocks, staged)
#   art/blocks/glb/<Name>.glb                 one self-contained GLB per block (bottom-center @ origin)
#   art/blocks/fbx/<Name>.fbx                 Unity-native FBX per block (same pivot, Y-up/-Z fwd)
#   art/blocks/glb/Towerpolis_Blocks_all.glb  all 6 in one file (authored layout)
#   art/blocks/preview.png                    EEVEE render of the staged blocks (visual reference)
# Usage:  & "<blender.exe>" --background --python scripts/blender_export.py
import bpy, os

REPO  = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BUILD = os.path.join(REPO, "scripts", "blender_build_blocks.py")
OUT   = os.path.join(REPO, "art", "blocks")
GLB   = os.path.join(OUT, "glb")
FBX   = os.path.join(OUT, "fbx")
for d in (OUT, GLB, FBX):
    os.makedirs(d, exist_ok=True)

NAMES = ['Floor_Standard', 'Floor_Balcony', 'Floor_Balcony_2',
         'Floor_Premium', 'Base_Ground', 'Base_Ground_2']

log = []
def note(s):
    log.append(s); print(s)

# --- 0. clean default scene (background mode ships a cube/cam/light) ---
for o in list(bpy.data.objects):
    bpy.data.objects.remove(o, do_unlink=True)

# --- 1. make sure the IO addons are on (they are by default, but be safe) ---
for addon in ("io_scene_fbx", "io_scene_gltf2"):
    try: bpy.ops.preferences.addon_enable(module=addon)
    except Exception as e: note("addon %s: %r" % (addon, e))

# --- 2. run the canonical build recipe in this interpreter ---
with open(BUILD, encoding="utf-8") as f:
    exec(compile(f.read(), BUILD, "exec"), {})
bpy.context.view_layer.update()
built = [n for n in NAMES if bpy.data.objects.get(n)]
note("built: %s" % ", ".join(built))

def select_only(objs):
    bpy.ops.object.select_all(action='DESELECT')
    objs = [o for o in objs if o]
    for o in objs: o.select_set(True)
    if objs: bpy.context.view_layer.objects.active = objs[0]
    return objs

def export_glb(path, sel_only=True):
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB',
        use_selection=sel_only, export_apply=True, export_yup=True)

def export_fbx(path):
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True,
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z', axis_up='Y', use_mesh_modifiers=True,
        bake_space_transform=True, object_types={'MESH'})

# --- 3. master .blend (authored layout, keeps stage so we can re-render/edit) ---
blend_path = os.path.join(OUT, "Towerpolis_Blocks.blend")
try:
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    note("saved blend: %s" % blend_path)
except Exception as e:
    note("blend save ERR %r" % e)

# --- 4. combined GLB (all 6, authored positions) ---
try:
    select_only([bpy.data.objects.get(n) for n in NAMES])
    export_glb(os.path.join(GLB, "Towerpolis_Blocks_all.glb"))
    note("combined glb ok")
except Exception as e:
    note("combined glb ERR %r" % e)

# --- 5. per-block GLB + FBX, each moved to world origin (bottom-center pivot) ---
for n in NAMES:
    ob = bpy.data.objects.get(n)
    if not ob:
        note("%-16s MISSING" % n); continue
    ob.location = (0.0, 0.0, 0.0)
    select_only([ob])
    tri = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    okg = okf = ""
    try: export_glb(os.path.join(GLB, n + ".glb")); okg = "glb"
    except Exception as e: okg = "glbERR %r" % e
    try: export_fbx(os.path.join(FBX, n + ".fbx")); okf = "fbx"
    except Exception as e: okf = "fbxERR %r" % e
    note("%-16s tris~%-4d %s %s" % (n, tri, okg, okf))

# --- 6. preview render (non-fatal) ---
try:
    sc = bpy.context.scene
    for eng in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE'):
        try: sc.render.engine = eng; break
        except Exception: pass
    # restore blocks to authored layout for a nice group shot
    with open(BUILD, encoding="utf-8") as f:
        exec(compile(f.read(), BUILD, "exec"), {})
    try:
        w = bpy.data.worlds[0]; w.use_nodes = True
        bg = w.node_tree.nodes.get('Background')
        if bg: bg.inputs[0].default_value = (0.45, 0.72, 1.0, 1); bg.inputs[1].default_value = 0.7
    except Exception: pass
    cam_data = bpy.data.cameras.new('TP_Cam'); cam = bpy.data.objects.new('TP_Cam', cam_data)
    bpy.context.collection.objects.link(cam); sc.camera = cam
    cam.location = (9.0, -16.0, 9.0); cam.rotation_euler = (1.05, 0.0, 0.5)
    cam_data.lens = 40
    sc.render.resolution_x = 1280; sc.render.resolution_y = 640
    sc.render.film_transparent = False
    sc.render.filepath = os.path.join(OUT, "preview.png")
    bpy.ops.render.render(write_still=True)
    note("preview render ok")
except Exception as e:
    note("preview render ERR %r" % e)

print("\n=== EXPORT DONE ===\n" + "\n".join(log))
