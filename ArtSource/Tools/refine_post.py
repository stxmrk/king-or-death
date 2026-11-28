bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'KOD_Outpost.blend'))
original=bpy.data.objects.get('KOD_Outpost_LOD0');new_parts=[]
for flag in [False,True]:
    polys=[p for p in original.data.polygons if ('BlueCloth' in original.data.materials[p.material_index].name)==flag]
    used=sorted({v for p in polys for v in p.vertices});mapping={v:i for i,v in enumerate(used)}
    data=bpy.data.meshes.new('CaptureFlagMesh' if flag else 'OutpostStructureMesh');data.from_pydata([original.data.vertices[v].co[:] for v in used],[],[tuple(mapping[v] for v in p.vertices) for p in polys]);data.update()
    for mat in original.data.materials:data.materials.append(mat)
    for target,source in zip(data.polygons,polys):target.material_index=source.material_index
    obj=bpy.data.objects.new('CaptureFlag' if flag else 'OutpostStructure',data);bpy.context.collection.objects.link(obj);new_parts.append(obj)
for obj in list(bpy.data.objects):
    if obj not in new_parts:bpy.data.objects.remove(obj,do_unlink=True)
PARTS=[new_parts[0]];export_asset('KOD_Outpost','Buildings',9,(0,0,4),attachments=[new_parts[1]])
print('SEPARATE_CAPTURE_FLAG_COMPLETE')
