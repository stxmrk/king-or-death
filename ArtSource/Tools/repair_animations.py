# Preserve all clips in editable .blend sources and re-export without changing geometry.
definitions=(Path(__file__).parent/'asset_models.py').read_text(encoding='utf-8').split('records=[]')[0]
for asset_name,category in [('KOD_Human_Swordsman','Characters'),('KOD_Human_Worker','Characters'),('KOD_Wolf','Wildlife')]:
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE/(asset_name+'.blend')))
    exec(definitions)
    rig=bpy.data.objects.get(asset_name+'_Rig');rig.animation_data_clear()
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    if asset_name=='KOD_Wolf':animate_wolf(rig,asset_name)
    else:animate_human(rig,asset_name,asset_name.endswith('Worker'))
    for action in bpy.data.actions:action.use_fake_user=True
    bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(asset_name+'.blend')))
    for obj in bpy.data.objects:obj.hide_set(False);obj.hide_render=False;obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(ART/'Models'/category/(asset_name+'.fbx')),use_selection=True,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,mesh_smooth_type='FACE',use_mesh_modifiers=True,path_mode='RELATIVE')
    path=SOURCE/(asset_name+'_metrics.json');record=json.loads(path.read_text());record['animations']=[a.name for a in bpy.data.actions];path.write_text(json.dumps(record,indent=2),encoding='utf-8')
    print('CLIPS_PRESERVED',asset_name,len(bpy.data.actions))
