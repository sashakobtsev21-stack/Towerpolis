import bpy
print('COUNT', len(bpy.data.objects))
for o in bpy.data.objects:
    if o.type == 'MESH':
        zs = [(o.matrix_world @ v.co).z for v in o.data.vertices]
        ys = [(o.matrix_world @ v.co).y for v in o.data.vertices]
        xs = [(o.matrix_world @ v.co).x for v in o.data.vertices]
        print('%-24s x %.1f..%.1f  y %.1f..%.1f  z %.2f..%.2f' %
              (o.name, min(xs), max(xs), min(ys), max(ys), min(zs), max(zs)))
