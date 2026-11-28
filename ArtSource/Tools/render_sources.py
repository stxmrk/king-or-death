from pathlib import Path
exec((Path(__file__).parent/'asset_production.py').read_text(encoding='utf-8').split("if PHASE not in")[0])
for name,span,focus in [('KOD_House',7,(0,0,2.8)),('KOD_Human_Swordsman',1.6,(0,0,.95)),('KOD_Human_Worker',1.6,(0,0,.95)),('KOD_Outpost',9,(0,0,4)),('KOD_TradingWagon',4,(0,-.4,.9)),('KOD_Wolf',1.45,(0,0,.5))]:
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE/(name+'.blend')))
    for im in bpy.data.images:
        if im.source=='FILE':im.reload()
    for obj in bpy.data.objects:
        if obj.type=='MESH':obj.hide_render=not obj.name.endswith('_LOD0');obj.hide_set(False)
    bpy.context.scene.frame_set(1)
    camera_render(name,span,focus)
    print('FINAL_PREVIEW',name,flush=True)
