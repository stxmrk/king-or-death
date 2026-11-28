"""KING OR DEATH asset pack 001. Reproducible Blender 4.2 authoring.
Run: blender -b --python asset_production.py -- PROJECT_PATH house|core
All geometry and texture fields are authored here; no downloaded meshes.
"""
import bpy, math, os, sys, json, random
import numpy as np
from mathutils import Vector
from pathlib import Path
ARGS=sys.argv[sys.argv.index('--')+1:]
ROOT=Path(ARGS[0]); PHASE=ARGS[1] if len(ARGS)>1 else 'house'
ART=ROOT/'Assets/_Game/Art'; SOURCE=ROOT/'ArtSource/Blender'; PREVIEW=ROOT/'Docs/Screenshots/AssetPack001'
for folder in [SOURCE,PREVIEW,ART/'Textures',ART/'Materials',ART/'Animations',ART/'Prefabs']+[ART/'Models'/s for s in ['Characters','Buildings','Wildlife','Vehicles','Props']]:folder.mkdir(parents=True,exist_ok=True)
random.seed(1047)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
MATS={}; PARTS=[]
PALETTE={
 'Oak':((.29,.15,.065),0,.72),'Slate':((.055,.16,.37),.12,.64),
 'Stone':((.48,.48,.40),0,.88),'Plaster':((.77,.69,.49),0,.91),
 'BlueCloth':((.025,.10,.34),0,.87),'Steel':((.43,.49,.53),.88,.55),
 'Gold':((.74,.43,.09),.72,.50),'Leather':((.22,.105,.045),0,.77),
 'Skin':((.57,.34,.21),0,.62),'Linen':((.77,.72,.60),0,.95),
 'Fur':((.29,.31,.30),0,.94),'Dark':((.045,.038,.03),.15,.62),
 'Glass':((.25,.40,.39),.35,.22),'Burgundy':((.28,.035,.045),0,.85)}
def save_image(name,pixels,colorspace='sRGB'):
    path=ART/'Textures'/(name+'.png');im=bpy.data.images.new(name,width=pixels.shape[1],height=pixels.shape[0],alpha=True)
    im.colorspace_settings.name=colorspace;im.pixels.foreach_set(pixels.astype(np.float32).ravel());im.filepath_raw=str(path);im.file_format='PNG';im.save();return im
def textures():
    n=1024;y,x=np.mgrid[0:n,0:n]/n;rng=np.random.default_rng(736)
    for name,(color,metal,rough) in PALETTE.items():
        fine=rng.normal(0,.018,(n,n));coarse=np.sin(x*19+np.sin(y*11))*np.cos(y*23)*.035
        if name in ['Oak','Leather']:
            field=np.sin((x+np.sin(y*8)*.022)*260+np.sin(x*47)*2)*.04+np.sin(x*50+y*5)*.025+fine
        elif name in ['BlueCloth','Linen','Burgundy']:
            field=(np.sin(x*math.pi*512)+np.sin(y*math.pi*512))*.018+fine*.4
        elif name=='Fur':field=fine*.5+coarse*.12
        elif name=='Slate':field=coarse+fine+np.sin(y*180+x*11)*.015
        else:field=coarse+fine
        rgba=np.ones((n,n,4));rgba[:,:,:3]=np.clip(np.array(color)[None,None,:]*(1+field[:,:,None]*2.5),0,1);base=save_image('KOD_'+name+'_BaseColor',rgba)
        gy,gx=np.gradient(field);normal=np.ones((n,n,4));normal[:,:,0]=np.clip(.5-gx*2,0,1);normal[:,:,1]=np.clip(.5-gy*2,0,1);normal[:,:,2]=1;norm=save_image('KOD_'+name+'_Normal',normal,'Non-Color')
        roughmap=np.clip(rough+field*.8,0,1);rgba[:,:,:3]=roughmap[:,:,None];roughtex=save_image('KOD_'+name+'_Roughness',rgba,'Non-Color')
        rgba[:,:,:3]=metal;metaltex=save_image('KOD_'+name+'_Metallic',rgba,'Non-Color')
        rgba[:,:,:3]=1;save_image('KOD_'+name+'_AO',rgba,'Non-Color') # Neutral shared AO: no false baked shadows.
        rgba[:,:,:3]=metal;rgba[:,:,3]=1-roughmap;save_image('KOD_'+name+'_MetallicSmoothness',rgba,'Non-Color')
        mat=bpy.data.materials.new('KOD_'+name);mat.use_nodes=True;nodes=mat.node_tree.nodes;bs=nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1)
        for image,socket in [(base,'Base Color'),(roughtex,'Roughness'),(metaltex,'Metallic')]:
            node=nodes.new('ShaderNodeTexImage');node.image=image;mat.node_tree.links.new(node.outputs['Color'],bs.inputs[socket])
        node=nodes.new('ShaderNodeTexImage');node.image=norm;nm=nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.25;mat.node_tree.links.new(node.outputs['Color'],nm.inputs['Color']);mat.node_tree.links.new(nm.outputs['Normal'],bs.inputs['Normal']);MATS[name]=mat
def use(obj,name,mat,bone=None):
    obj.name=name;obj.data.name=name+'_Mesh';obj.data.materials.clear();obj.data.materials.append(MATS[mat]);PARTS.append(obj)
    if bone:obj.vertex_groups.new(name=bone).add(list(range(len(obj.data.vertices))),1,'REPLACE')
    return obj
def box(name,at,size,mat='Oak',bevel=0,bone=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Crafted edges','BEVEL');mod.width=bevel;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=mod.name)
    return use(o,name,mat,bone)
def beam(name,a,b,width,depth=None,mat='Oak',bevel=.02,bone=None):
    a,b=Vector(a),Vector(b);o=box(name,(a+b)/2,(width,depth or width,(b-a).length),mat,bevel,bone);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def uv_sphere(name,at,size,mat='Steel',segments=12,rings=8,bone=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=at);o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for p in o.data.polygons:p.use_smooth=True
    return use(o,name,mat,bone)
def cyl(name,at,radius,depth,mat='Oak',vertices=12,bone=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=at);return use(bpy.context.object,name,mat,bone)
def mesh(name,vertices,faces,mat,bone=None):
    data=bpy.data.meshes.new(name+'_Mesh');data.from_pydata(vertices,[],faces);data.update();o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);return use(o,name,mat,bone)
def torus(name,at,major,minor,mat='Steel',bone=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=6,location=at,major_radius=major,minor_radius=minor);return use(bpy.context.object,name,mat,bone)
def loft(name,points,radii,mat,sides=12,bone=None):
    # Quad rings follow the centreline; local radius axes are perpendicular to it.
    verts=[]
    for i,p in enumerate(points):
        tangent=Vector(points[min(len(points)-1,i+1)])-Vector(points[max(0,i-1)]);tangent.normalize();reference=Vector((1,0,0));a=reference-tangent*tangent.dot(reference)
        if a.length<.1:reference=Vector((0,1,0));a=reference-tangent*tangent.dot(reference)
        a.normalize();b=tangent.cross(a).normalized();rx,ry=radii[i]
        for j in range(sides):v=Vector(p)+a*rx*math.cos(j*math.tau/sides)+b*ry*math.sin(j*math.tau/sides);verts.append(tuple(v))
    faces=[tuple(range(sides-1,-1,-1))]
    for i in range(len(points)-1):
        for j in range(sides):faces.append((i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j))
    faces.append(tuple(range((len(points)-1)*sides,len(points)*sides)));o=mesh(name,verts,faces,mat,bone)
    for p in o.data.polygons:p.use_smooth=True
    return o
def join_meshes(objects,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;o.data.name=name+'_Mesh';bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');return o
def unwrap(o):
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.016);bpy.ops.object.mode_set(mode='OBJECT')
def house():
    box('PlasterVolume',(0,0,1.95),(5.7,4.5,3.1),'Plaster')
    for side in [-1,1]:
        for i in range(9):box('FoundationBlock',(-2.7+i*.675,side*2.3,.3),(.65,.4,.55),'Stone')
        for i in range(6):box('FoundationSide',(side*2.9,-1.8+i*.72,.3),(.4,.7,.55),'Stone')
        for x in [-2.9,0,2.9]:box('FrontUpright',(x,side*2.31,1.95),(.20,.22,3.15),'Oak')
        for z in [.65,2.9,3.45]:box('FacadeBeam',(0,side*2.32,z),(6.05,.23,.20),'Oak')
        for y in [-1.45,0,1.45]:box('SideUpright',(side*2.91,y,1.95),(.23,.16,3.15),'Oak',.015)
        for y in [-1.45,0]:beam('DiagonalBrace',(side*2.93,y,.8),(side*2.93,y+1.4,2.85),.13,mat='Oak',bevel=.01)
        mesh('GablePlaster', [(-2.85,side*2.25,3.4),(2.85,side*2.25,3.4),(0,side*2.25,5.45)],[(0,1,2) if side<0 else (2,1,0)],'Plaster')
        for x in [-1,1]:beam('GableRafter',(x*3.2,side*2.65,3.32),(0,side*2.65,5.6),.19,mat='Oak')
        beam('GableKingpost',(0,side*2.4,3.4),(0,side*2.4,5.4),.17,mat='Oak')
        beam('GableBrace',(-1.5,side*2.4,3.5),(0,side*2.4,4.7),.13,mat='Oak')
        beam('GableBrace',(1.5,side*2.4,3.5),(0,side*2.4,4.7),.13,mat='Oak')
        substrate=box('RoofBoarding',(side*1.55,0,4.38),(3.9,5.6,.10),'Oak');substrate.rotation_euler[1]=side*math.atan(.69)
        for row in range(7):
            x=side*(.18+row*.46);z=5.56-abs(x)*.69
            for col in range(9):
                y=-2.5+col*.625+(row%2)*.08;o=box('BlueSlateTile',(x,y,z),(.56,.65,.085),'Slate');o.rotation_euler[1]=side*math.atan(.69)
        beam('EaveFascia',(side*3.18,-2.8,3.35),(side*3.18,2.8,3.35),.18,mat='Oak')
    beam('RoofRidge',(0,-2.85,5.64),(0,2.85,5.64),.17,mat='Gold')
    # Front windows have separate shutters, mullions and recessed glazing.
    for x in [-1.65,1.65]:
        box('WindowRecess',(x,-2.38,2.12),(1.0,.12,1.25),'Dark')
        box('WindowGlass',(x,-2.46,2.14),(.79,.06,1.08),'Glass')
        for s in [-1,1]:
            box('WindowFrame',(x+s*.46,-2.50,2.14),(.1,.12,1.35),'Oak',.01)
            o=box('OpenShutter',(x+s*.78,-2.54,2.14),(.44,.10,1.14),'Oak');o.rotation_euler[2]=s*.25
        for z in [1.49,2.77]:box('WindowLintel',(x,-2.50,z),(1.1,.14,.12),'Oak',.01)
        for dx in [-.18,.18]:box('WindowMullion',(x+dx,-2.52,2.15),(.04,.05,1.1),'Gold')
        box('WindowCrossbar',(x,-2.53,2.15),(.8,.05,.06),'Gold')
        box('WindowSill',(x,-2.55,1.42),(1.25,.40,.14),'Stone',.02)
    box('DoorRecess',(0,-2.4,1.4),(1.18,.13,2.12),'Dark')
    for i in range(6):box('DoorPlank',(-.48+i*.19,-2.5,1.35),(.18,.12,1.95),'Oak',.01)
    for z in [.85,1.8]:box('DoorStrap',(0,-2.58,z),(1.04,.06,.09),'Steel')
    for x in [-.64,.64]:box('DoorJamb',(x,-2.47,1.4),(.16,.28,2.2),'Stone',.03)
    box('DoorLintel',(0,-2.47,2.51),(1.45,.28,.2),'Stone',.03)
    ring=torus('DoorHandle',(.3,-2.63,1.32),.085,.018,'Steel');ring.rotation_euler[0]=math.pi/2
    box('Doorstep',(0,-2.8,.25),(1.6,.85,.25),'Stone',.035)
    for x in [-.95,.95]:beam('PorchBracket',(x,-2.45,2.55),(x,-3.15,2.95),.13,mat='Oak')
    for i in range(6):
        o=box('PorchSlate',(-1.05+i*.42,-2.92,3.0),(.45,1.2,.09),'Slate');o.rotation_euler[0]=-.18
    for row in range(7):
        for x in [-.2,.2]:box('ChimneyStone',(1.55+x,1.15,4.3+row*.25),(.39,.65,.23),'Stone')
    box('ChimneyCrown',(1.55,1.15,6.1),(1,.9,.15),'Stone',.02)
    box('ChimneySoot',(1.55,1.15,6.19),(.55,.47,.025),'Dark')
    # Small original faction device (three golden chevrons, not copied heraldry).
    box('BlueHouseBanner',(0,-2.48,4.1),(.64,.04,.82),'BlueCloth')
    for z in [3.9,4.1,4.3]:
        beam('CrownChevron',(-.21,-2.515,z),(0,-2.515,z+.12),.045,.025,'Gold',0)
        beam('CrownChevron',(0,-2.515,z+.12),(.21,-2.515,z),.045,.025,'Gold',0)
def stats(o):
    o.data.calc_loop_triangles();return {'triangles':len(o.data.loop_triangles),'vertices':len(o.data.vertices),'uv':len(o.data.uv_layers)>0,'materials':len(o.data.materials),'degenerate_triangles':sum(1 for p in o.data.loop_triangles if p.area<1e-10)}
def camera_render(asset_name,span=7,focus=(0,0,2.2)):
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
    scene.render.resolution_x=1400;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
    world=bpy.data.worlds.new('KOD Studio');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.18,.24,.31,1);world.node_tree.nodes['Background'].inputs[1].default_value=.55
    bpy.ops.object.camera_add(location=(span*1.1,-span*1.4,span*.9));cam=bpy.context.object;cam.name='ReviewCamera';cam.rotation_euler=(Vector(focus)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=span*1.65;scene.camera=cam
    bpy.ops.object.light_add(type='AREA',location=(span*.3,-span*.7,span*1.4));key=bpy.context.object;key.name='StudioKey';key.data.energy=span*span*95;key.data.shape='DISK';key.data.size=span*.75;key.rotation_euler=(Vector(focus)-key.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.light_add(type='AREA',location=(-span,span*.5,span*.7));rim=bpy.context.object;rim.name='StudioRim';rim.data.energy=span*span*65;rim.data.size=span*.8;rim.rotation_euler=(Vector(focus)-rim.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='PreviewGround';mat=bpy.data.materials.new('PreviewGroundMaterial');mat.diffuse_color=(.10,.135,.11,1);floor.data.materials.append(mat)
    scene.view_settings.view_transform='AgX';scene.render.filepath=str(PREVIEW/(asset_name+'_Blender.png'));bpy.ops.render.render(write_still=True)
    for o in [cam,key,rim,floor]:bpy.data.objects.remove(o,do_unlink=True)
def export_asset(name,category,span=7,focus=(0,0,2.2),rig=None,attachments=None):
    global PARTS
    lod0=join_meshes(PARTS,name+'_LOD0');unwrap(lod0)
    # Unique packed UVs allow a real per-asset AO bake, separate from unlit Base Color.
    ao=bpy.data.images.new(name+'_AO',width=1024,height=1024,alpha=False);ao.colorspace_settings.name='Non-Color'
    targets=[]
    for mat in lod0.data.materials:
        node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=ao;mat.node_tree.nodes.active=node;targets.append((mat,node))
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.bake.margin=12;scene.render.bake.use_clear=True;bpy.context.view_layer.objects.active=lod0;lod0.select_set(True)
    bpy.ops.object.bake(type='AO');ao.filepath_raw=str(ART/'Textures'/(name+'_AO.png'));ao.file_format='PNG';ao.save()
    for mat,node in targets:mat.node_tree.nodes.remove(node)
    root=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(root)
    objects=[root];metrics=[]
    if rig:rig.parent=root;objects.append(rig)
    for i,ratio in enumerate([1,.5,.2]):
        if i==0:o=lod0
        else:
            o=lod0.copy();o.data=lod0.data.copy();bpy.context.collection.objects.link(o);o.name=name+'_LOD'+str(i);bpy.context.view_layer.objects.active=o
            mod=o.modifiers.new('LOD simplification','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name)
        o.parent=root;objects.append(o);metrics.append(stats(o));o.hide_render=i>0;o.hide_set(i>0)
        if rig:mod=o.modifiers.new('Skin','ARMATURE');mod.object=rig
    if attachments:
        for part in attachments:
            unwrap(part);part.name+='_LOD0';part.parent=root;objects.append(part)
            if rig:mod=part.modifiers.new('Attachment skin','ARMATURE');mod.object=rig
            for i,ratio in [(1,.5),(2,.2)]:
                copy=part.copy();copy.data=part.data.copy();bpy.context.collection.objects.link(copy);copy.name=part.name.replace('_LOD0','_LOD'+str(i));bpy.context.view_layer.objects.active=copy;mod=copy.modifiers.new('Wheel LOD','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name);copy.parent=root;copy.hide_render=True;objects.append(copy)
            for i in range(3):
                matching=[o for o in objects if o.type=='MESH' and o.name.startswith(part.name.split('_LOD')[0]) and o.name.endswith('_LOD'+str(i))]
                for o in matching:metrics[i]['triangles']+=stats(o)['triangles'];metrics[i]['vertices']+=stats(o)['vertices']
    if rig:
        rig.animation_data_create();rig.animation_data.action=bpy.data.actions.get(name+'_Idle')
    bpy.context.preferences.filepaths.save_version=0
    for action in bpy.data.actions:action.use_fake_user=True
    for img in bpy.data.images:
        if img.filepath and 'KOD_' in img.name:img.filepath='//../../Assets/_Game/Art/Textures/'+img.name+'.png'
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(name+'.blend')))
    # All LODs are exported. Unity LODGroup hides levels, not FBX visibility flags.
    for o in objects:o.hide_set(False);o.hide_render=False
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(ART/'Models'/category/(name+'.fbx')),use_selection=True,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=bool(rig),bake_anim_use_all_actions=bool(rig),bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,mesh_smooth_type='FACE',use_mesh_modifiers=True,path_mode='RELATIVE')
    for i,o in enumerate(objects):
        if o.type=='MESH':o.hide_render=not o.name.endswith('_LOD0')
    camera_render(name,span,focus)
    record={'name':name,'category':category,'lods':metrics,'rig':bool(rig),'animations':[a.name for a in bpy.data.actions if a.name.startswith(name+'_')],'scale_m':list(lod0.dimensions),'status':'Needs Improvement — awaiting Unity visual validation'}
    (SOURCE/(name+'_metrics.json')).write_text(json.dumps(record,indent=2),encoding='utf-8');PARTS=[]
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    return record
if PHASE not in ['refine_wolf','refine_post','repair_animations']:textures()
if PHASE in ['house','all']:house();export_asset('KOD_House','Buildings')
if PHASE in ['core','all','characters']:exec((Path(__file__).parent/'asset_models.py').read_text(encoding='utf-8'))
if PHASE=='refine_wolf':exec((Path(__file__).parent/'refine_wolf.py').read_text(encoding='utf-8'))
if PHASE=='refine_post':exec((Path(__file__).parent/'refine_post.py').read_text(encoding='utf-8'))
if PHASE=='repair_animations':exec((Path(__file__).parent/'repair_animations.py').read_text(encoding='utf-8'))
print('KOD_ASSET_PRODUCTION_COMPLETE')
