import bpy, json, os, sys
project=sys.argv[sys.argv.index('--')+1]
rows=[]
for name in ['KOD_House','KOD_Human_Swordsman','KOD_Human_Worker','KOD_Outpost','KOD_TradingWagon','KOD_Wolf']:
    bpy.ops.wm.open_mainfile(filepath=os.path.join(project,'ArtSource','Blender',name+'.blend'))
    meshes=[o for o in bpy.data.objects if o.type=='MESH' and '_LOD' in o.name]
    row={'name':name,'fps':bpy.context.scene.render.fps,'actions':[a.name for a in bpy.data.actions], 'lods':[]}
    for level in range(3):
        objects=[o for o in meshes if '_LOD'+str(level) in o.name]
        triangles=0;degen=0;materials=set();unweighted=0;quads=0;polygons=0;outside=0
        for obj in objects:
            obj.data.calc_loop_triangles();triangles+=len(obj.data.loop_triangles)
            degen+=sum(t.area<1e-10 for t in obj.data.loop_triangles)
            materials.update(m.name for m in obj.data.materials if m)
            polygons+=len(obj.data.polygons);quads+=sum(len(p.vertices)==4 for p in obj.data.polygons)
            if obj.data.uv_layers.active:outside+=sum(uv.uv.x<-.0001 or uv.uv.x>1.0001 or uv.uv.y<-.0001 or uv.uv.y>1.0001 for uv in obj.data.uv_layers.active.data)
            if any(m.type=='ARMATURE' for m in obj.modifiers):unweighted+=sum(not any(g.weight>0 for g in v.groups) for v in obj.data.vertices)
        row['lods'].append({'level':level,'triangles':triangles,'objects':len(objects),'materials':sorted(materials),'uv':all(bool(o.data.uv_layers) for o in objects),'uv_outside_01':outside,'degenerate_triangles':degen,'unweighted_vertices':unweighted,'quad_faces':quads,'faces':polygons})
    row['armatures']=[{'name':o.name,'bones':len(o.data.bones)} for o in bpy.data.objects if o.type=='ARMATURE']
    rows.append(row)
out=os.path.join(project,'ArtSource','Blender','SourceAudit.json')
with open(out,'w') as f:json.dump(rows,f,indent=2)
print('SOURCE_AUDIT_COMPLETE',out)
