import bpy
# clear old demo
for o in list(bpy.data.objects):
    if o.name.startswith('Demo_'):
        bpy.data.objects.remove(o, do_unlink=True)

def dup(src, z, nm):
    s = bpy.data.objects[src]
    o = s.copy(); o.data = s.data.copy(); o.name = nm
    bpy.context.collection.objects.link(o); o.location = (-6, 0, z)
    return o

order = ['Base_Ground','Floor_Standard','Floor_Balcony','Floor_Standard','Floor_Premium']
z = 0.0; demo = []
for i, nm in enumerate(order):
    src = bpy.data.objects[nm]
    demo.append(dup(nm, z, 'Demo_%d' % i))
    z += src.dimensions.z
print('showcase tower: %d floors, height %.2f' % (len(demo), z))
