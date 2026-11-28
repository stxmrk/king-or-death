# Merge the anatomical wolf surfaces into a continuous skin, then rebind it.
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'KOD_Wolf.blend'))
rig=bpy.data.objects.get('KOD_Wolf_Rig');original=bpy.data.objects.get('KOD_Wolf_LOD0');new_parts=[]
for mat_index,mat in enumerate(original.data.materials):
    polys=[p for p in original.data.polygons if p.material_index==mat_index]
    used=sorted({v for p in polys for v in p.vertices});mapping={v:i for i,v in enumerate(used)}
    data=bpy.data.meshes.new('ContinuousWolf_'+mat.name);data.from_pydata([original.data.vertices[v].co[:] for v in used],[],[tuple(mapping[v] for v in p.vertices) for p in polys]);data.materials.append(mat);data.update();obj=bpy.data.objects.new('ContinuousWolf_'+mat.name,data);bpy.context.collection.objects.link(obj);new_parts.append(obj)
    if 'Fur' in mat.name:
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;obj.data.remesh_voxel_size=.025;bpy.ops.object.voxel_remesh()
        smooth=obj.modifiers.new('Sculpt surface relaxation','SMOOTH');smooth.factor=.8;smooth.iterations=6;bpy.ops.object.modifier_apply(modifier=smooth.name)
        obj.data.calc_loop_triangles();reduce=obj.modifiers.new('RTS silhouette budget','DECIMATE');reduce.ratio=min(1,4250/len(obj.data.loop_triangles));bpy.ops.object.modifier_apply(modifier=reduce.name)
        for p in obj.data.polygons:p.use_smooth=True
        for bone in rig.data.bones:obj.vertex_groups.new(name=bone.name)
        def distance_segment(p,a,b):
            delta=b-a;t=max(0,min(1,(p-a).dot(delta)/max(1e-8,delta.length_squared)));return (p-(a+delta*t)).length
        for vertex in obj.data.vertices:
            nearest=sorted([(distance_segment(vertex.co,b.head_local,b.tail_local),b.name) for b in rig.data.bones])[:2]
            weights=[1/(d+.035)**4 for d,n in nearest];total=sum(weights)
            for (d,n),w in zip(nearest,weights):obj.vertex_groups[n].add([vertex.index],w/total,'REPLACE')
    else:
        for vg in original.vertex_groups:obj.vertex_groups.new(name=vg.name)
        for old,new in mapping.items():
            for weight in original.data.vertices[old].groups:obj.vertex_groups[original.vertex_groups[weight.group].name].add([new],weight.weight,'REPLACE')
rig.parent=None
for obj in list(bpy.data.objects):
    if obj!=rig and obj not in new_parts:bpy.data.objects.remove(obj,do_unlink=True)
PARTS=new_parts
export_asset('KOD_Wolf','Wildlife',1.45,(0,0,.5),rig)
print('CONTINUOUS_WOLF_COMPLETE')
