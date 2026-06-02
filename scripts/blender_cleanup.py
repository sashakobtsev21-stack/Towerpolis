import bpy
# wipe everything except the camera (removes strays, old residents, demo dupes)
n = 0
for o in list(bpy.data.objects):
    if o.type != 'CAMERA':
        bpy.data.objects.remove(o, do_unlink=True); n += 1
# purge orphan meshes/materials
for blk in (bpy.data.meshes, bpy.data.materials):
    for d in list(blk):
        if d.users == 0:
            try: blk.remove(d)
            except Exception: pass
print('scene wiped: removed %d objects (kept camera)' % n)
