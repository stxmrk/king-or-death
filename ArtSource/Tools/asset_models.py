# Executed inside asset_production.py after the house pipeline is approved.
def insignia(at,bone='Chest',size=.12):
    x,y,z=at
    for dz in [-size,0,size]:
        beam('CrownChevron',(x-size,y,z+dz),(x,y,z+dz+size*.5),.018,.014,'Gold',0,bone)
        beam('CrownChevron',(x,y,z+dz+size*.5),(x+size,y,z+dz),.018,.014,'Gold',0,bone)
def cloth_panel(name,top,bottom,width,mat,bone,rows=7):
    verts=[]
    for row in range(rows):
        t=row/(rows-1);c=Vector(top).lerp(Vector(bottom),t)
        for col in range(7):
            f=col/6;verts.append((c.x+(f-.5)*width*(1+t*.15),c.y-math.sin(f*math.pi*6)*.009,c.z))
    faces=[]
    for row in range(rows-1):
        for col in range(6):a=row*7+col;faces.append((a,a+1,a+8,a+7))
    o=mesh(name,verts,faces,mat,bone);mod=o.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.008;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name);return o
def axe(at,bone='RightHand'):
    x,y,z=at;beam('AxeOakHaft',(x,y,z-.30),(x,y,z+.35),.035,mat='Oak',bevel=0,bone=bone)
    verts=[(x+dx,y+dy,z+dz) for dx,dy,dz in [(-.02,-.035,.19),(.23,-.025,.12),(.25,-.025,.4),(-.02,-.035,.32),(-.02,.035,.19),(.23,.025,.12),(.25,.025,.4),(-.02,.035,.32)]]
    mesh('ForgedAxeHead',verts,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'Steel',bone)
def human(worker=False):
    bodymat='Linen' if worker else 'Steel'
    loft('Torso',[(0,0,z) for z in [.95,1.03,1.16,1.30,1.43,1.50]],[(.15,.12),(.155,.13),(.17,.13),(.21,.14),(.235,.13),(.19,.11)],bodymat,20,'Chest')
    loft('Neck',[(0,0,1.47),(0,0,1.60)],[(.07,.065),(.065,.06)],'Skin',16,'Neck')
    loft('Face',[(0,-.015,z) for z in [1.57,1.60,1.67,1.76,1.80]],[(.045,.06),(.075,.08),(.095,.09),(.09,.085),(.055,.05)],'Skin',20,'Head')
    # Readable nose, brow, lips and ears; forward is Blender -Y.
    loft('NoseBridge',[(0,-.086,1.735),(0,-.107,1.70),(0,-.125,1.675)],[(.016,.011),(.018,.014),(.023,.018)],'Skin',10,'Head')
    uv_sphere('NoseTip',(0,-.126,1.673),(.024,.025,.018),'Skin',12,7,'Head')
    for s in [-1,1]:
        uv_sphere('Ear',(s*.098,-.005,1.69),(.021,.024,.039),'Skin',10,6,'Head')
        uv_sphere('EyeWhite',(s*.042,-.092,1.709),(.015,.009,.007),'Linen',10,5,'Head');uv_sphere('Iris',(s*.042,-.100,1.709),(.006,.004,.006),'Dark',10,5,'Head')
        beam('Brow',(s*.020,-.10,1.731),(s*.065,-.09,1.729),.009,.008,'Leather',0,'Head')
    box('Mouth',(0,-.096,1.628),(.055,.012,.009),'Leather',0,'Head')
    for s,label in [(-1,'Left'),(1,'Right')]:
        hip=(s*.105,0,.98);knee=(s*.12,-.015,.55);ankle=(s*.12,0,.12)
        loft('WoolBreeches',[hip,(s*.11,0,.87),(s*.115,.01,.72),knee],[(.10,.11),(.115,.115),(.09,.09),(.07,.075)],'BlueCloth',16,label+'UpperLeg')
        loft('LowerLeg',[knee,(s*.12,.015,.43),(s*.12,.01,.28),ankle],[(.074,.08),(.088,.085),(.065,.06),(.055,.055)],'Leather' if worker else 'Steel',16,label+'LowerLeg')
        uv_sphere('Boot',(s*.12,-.075,.10),(.082,.18,.10),'Leather',16,8,label+'Foot')
        box('BootSole',(s*.12,-.075,.035),(.17,.32,.045),'Dark',.01,label+'Foot')
        shoulder=(s*.23,0,1.44);elbow=(s*.40,0,1.19);wrist=(s*.48,-.015,.98)
        loft('UpperSleeve',[shoulder,(s*.285,0,1.39),(s*.35,0,1.29),elbow],[(.09,.09),(.095,.1),(.085,.09),(.065,.07)],bodymat,16,label+'UpperArm')
        loft('Forearm',[elbow,(s*.445,0,1.10),wrist],[(.069,.07),(.072,.075),(.043,.045)],'Skin' if worker else 'Steel',16,label+'LowerArm')
        uv_sphere('Hand',(s*.50,-.02,.93),(.052,.04,.073),'Skin' if worker else 'Leather',14,7,label+'Hand')
        for finger in range(3):loft('Finger',[(s*(.48+finger*.018),-.028,.93),(s*(.48+finger*.018),-.035,.86)],[(.012,.014),(.010,.01)],'Skin' if worker else 'Leather',6,label+'Hand')
        if not worker:
            uv_sphere('Pauldron',(s*.235,0,1.45),(.125,.13,.075),'Steel',16,8,label+'UpperArm')
            for z in [1.39,1.35]:beam('ShoulderGoldRim',(s*.19,-.13,z),(s*.33,-.10,z-.02),.022,mat='Gold',bevel=0,bone=label+'UpperArm')
            uv_sphere('KneeGuard',(s*.12,-.077,.55),(.08,.047,.082),'Steel',14,8,label+'LowerLeg')
            for z in [.23,.36,.46]:box('GreaveRivet',(s*.12,-.081,z),(.025,.015,.025),'Gold',0,label+'LowerLeg')
        else:
            for z in [1.25,1.29]:loft('RolledCuff',[(s*.36,0,z),(s*.38,0,z-.035)],[(.094,.095),(.094,.095)],'Linen',12,label+'UpperArm')
    loft('WaistBelt',[(0,0,1.02),(0,0,1.075)],[(.18,.145),(.18,.145)],'Leather',20,'Hips')
    box('BeltBuckle',(0,-.154,1.045),(.09,.025,.065),'Gold',.008,'Hips')
    if worker:
        cloth_panel('LeatherApron',(0,-.145,1.40),(0,-.19,.70),.31,'Leather','Chest')
        for s in [-1,1]:beam('ApronStrap',(s*.12,-.15,1.31),(s*.15,-.08,1.49),.035,.015,'Leather',0,'Chest')
        loft('SculptedBeard',[(0,-.025,1.64),(0,-.028,1.61),(0,-.045,1.57)],[(.079,.069),(.068,.065),(.032,.031)],'Leather',16,'Head')
        uv_sphere('SoftBlueCap',(.015,.015,1.79),(.115,.105,.065),'BlueCloth',20,10,'Head')
        loft('CapBand',[(0,0,1.765),(0,0,1.786)],[(.103,.095),(.103,.095)],'BlueCloth',20,'Head')
        axe((.51,-.055,.96));box('ToolPouch',(-.20,.02,.98),(.10,.14,.15),'Leather',.01,'Hips')
    else:
        uv_sphere('SteelHelmet',(0,.005,1.77),(.112,.108,.102),'Steel',20,12,'Head')
        beam('NasalGuard',(0,-.112,1.80),(0,-.13,1.68),.026,.02,'Steel',0,'Head')
        for s in [-1,1]:box('CheekGuard',(s*.094,-.045,1.675),(.024,.11,.14),'Steel',.01,'Head')
        loft('MailCollar',[(0,0,1.45),(0,0,1.55)],[(.12,.105),(.082,.08)],'Steel',20,'Neck')
        cloth_panel('RoyalTabardFront',(0,-.147,1.43),(0,-.17,.73),.31,'BlueCloth','Chest')
        cloth_panel('RoyalTabardBack',(0,.15,1.40),(0,.18,.73),.31,'BlueCloth','Chest')
        insignia((0,-.178,1.28),size=.065)
        for s in [-1,1]:beam('TabardTrim',(s*.16,-.18,.74),(s*.15,-.155,1.42),.014,.009,'Gold',0,'Chest')
        # Shield face is gently convex, with an independent steel rim.
        verts=[(-.68,-.13,1.28),(-.32,-.13,1.28),(-.31,-.14,.95),(-.50,-.19,.77),(-.69,-.14,.95),(-.50,-.22,1.08)]
        mesh('RoyalShield',verts,[(i,(i+1)%5,5) for i in range(5)]+[(4,3,2,1,0)],'BlueCloth','LeftHand')
        for i in range(5):beam('ShieldRim',verts[i],verts[(i+1)%5],.022,mat='Steel',bevel=0,bone='LeftHand')
        insignia((-.5,-.238,1.10),'LeftHand',.055)
        beam('SwordGrip',(.51,-.05,.87),(.51,-.05,1.03),.035,mat='Leather',bevel=0,bone='RightHand')
        beam('SwordGuard',(.40,-.05,1.03),(.62,-.05,1.03),.025,mat='Gold',bevel=0,bone='RightHand')
        mesh('SwordBlade',[(.48,-.06,1.05),(.54,-.06,1.05),(.535,-.06,1.60),(.51,-.06,1.72),(.485,-.06,1.60),(.51,-.08,1.35),(.51,-.035,1.35)],[(0,1,5),(1,2,5),(2,3,5),(3,4,5),(4,0,5),(1,0,6),(2,1,6),(3,2,6),(4,3,6),(0,4,6)],'Steel','RightHand')
def human_rig(name):
    specs=[('Hips',(0,0,.95),(0,0,1.09),None),('Spine',(0,0,1.09),(0,0,1.27),'Hips'),('Chest',(0,0,1.27),(0,0,1.48),'Spine'),('Neck',(0,0,1.48),(0,0,1.59),'Chest'),('Head',(0,0,1.59),(0,0,1.82),'Neck')]
    for s,label in [(-1,'Left'),(1,'Right')]:
        specs += [(label+'UpperLeg',(s*.105,0,.98),(s*.12,-.015,.55),'Hips'),(label+'LowerLeg',(s*.12,-.015,.55),(s*.12,0,.12),label+'UpperLeg'),(label+'Foot',(s*.12,0,.12),(s*.12,-.18,.07),label+'LowerLeg'),(label+'UpperArm',(s*.23,0,1.44),(s*.40,0,1.19),'Chest'),(label+'LowerArm',(s*.40,0,1.19),(s*.48,-.015,.98),label+'UpperArm'),(label+'Hand',(s*.48,-.015,.98),(s*.50,-.02,.88),label+'LowerArm')]
    return make_rig(name,specs)
def make_rig(name,specs):
    bpy.ops.object.armature_add();rig=bpy.context.object;rig.name=name+'_Rig';bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
    for n,h,t,parent in specs:
        b=rig.data.edit_bones.new(n);b.head=h;b.tail=t
        if parent:b.parent=rig.data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT');return rig
def animate_human(rig,name,worker=False):
    clips=['Idle','Walk','Carry','ChopWood','PickUp','Die'] if worker else ['Idle','Walk','Run','Attack_01','Attack_02','Hit','Die']
    rig.animation_data_create();bpy.context.scene.render.fps=30
    for clip in clips:
        action=bpy.data.actions.new(name+'_'+clip);rig.animation_data.action=action;length=40 if clip=='Die' else 30
        for frame in range(1,length+2,3):
            t=(frame-1)/length;phase=t*math.tau
            for pb in rig.pose.bones:pb.rotation_mode='XYZ';pb.rotation_euler=(0,0,0);pb.location=(0,0,0)
            if clip in ['Walk','Run','Carry']:
                amp=.52 if clip!='Run' else .78
                for s,label in [(-1,'Left'),(1,'Right')]:
                    wave=math.sin(phase)*s;rig.pose.bones[label+'UpperLeg'].rotation_euler.x=wave*amp;rig.pose.bones[label+'LowerLeg'].rotation_euler.x=max(0,-wave)*.8;rig.pose.bones[label+'Foot'].rotation_euler.x=-wave*.14;rig.pose.bones[label+'UpperArm'].rotation_euler.x=-wave*.35
                    if clip=='Carry':rig.pose.bones[label+'UpperArm'].rotation_euler.x=-.65;rig.pose.bones[label+'LowerArm'].rotation_euler.x=-1.1
                rig.pose.bones['Hips'].location.y=.016*math.cos(phase*2)
            elif clip in ['Attack_01','Attack_02','ChopWood']:
                swing=math.sin(phase-math.pi/2);rig.pose.bones['RightUpperArm'].rotation_euler.x=swing*1.05-.15;rig.pose.bones['RightLowerArm'].rotation_euler.x=-.4-swing*.45;rig.pose.bones['Chest'].rotation_euler.z=math.sin(phase)*(.23 if clip!='ChopWood' else .08)
            elif clip=='Hit':rig.pose.bones['Chest'].rotation_euler.x=math.sin(t*math.pi)*.35
            elif clip=='PickUp':rig.pose.bones['Chest'].rotation_euler.x=math.sin(t*math.pi)*.7;rig.pose.bones['RightUpperArm'].rotation_euler.x=-math.sin(t*math.pi)*.8
            elif clip=='Die':rig.pose.bones['Hips'].rotation_euler.x=min(1,t*1.5)*1.48;rig.pose.bones['Hips'].location.y=-.84*min(1,t*1.5)
            else:rig.pose.bones['Chest'].rotation_euler.x=math.sin(phase)*.022
            for pb in rig.pose.bones:pb.keyframe_insert('rotation_euler',frame=frame);pb.keyframe_insert('location',frame=frame)
        for fc in action.fcurves:
            for k in fc.keyframe_points:k.interpolation='BEZIER'
    rig.animation_data.action=bpy.data.actions[name+'_Idle'];bpy.context.scene.frame_set(1)
def wolf():
    loft('WolfTorso',[(0,y,z) for y,z in [(-.43,.67),(-.28,.70),(-.05,.65),(.22,.64),(.43,.66),(.55,.62)]],[(.14,.19),(.20,.25),(.19,.23),(.15,.20),(.19,.23),(.12,.16)],'Fur',24,'Spine')
    loft('WolfNeck',[(0,-.34,.64),(0,-.47,.80),(0,-.56,.92)],[(.19,.23),(.18,.20),(.13,.14)],'Fur',20,'Neck')
    loft('WolfHead',[(0,-.52,.88),(0,-.67,.88),(0,-.79,.83)],[(.12,.14),(.14,.13),(.09,.09)],'Fur',20,'Head')
    loft('Muzzle',[(0,-.72,.82),(0,-.88,.79),(0,-.98,.78)],[(.085,.075),(.065,.055),(.05,.045)],'Fur',16,'Head')
    uv_sphere('Nose',(0,-.985,.795),(.053,.035,.035),'Dark',14,8,'Head')
    beam('MouthLine',(-.055,-.92,.759),(.055,-.92,.759),.012,mat='Dark',bevel=0,bone='Head')
    for s in [-1,1]:
        uv_sphere('WolfEye',(s*.105,-.735,.905),(.022,.016,.014),'Gold',12,6,'Head');uv_sphere('WolfPupil',(s*.11,-.747,.905),(.010,.008,.012),'Dark',10,6,'Head')
        mesh('WolfEar',[(s*.06,-.60,.95),(s*.14,-.51,.95),(s*.13,-.54,1.10),(s*.105,-.64,1.0)],[(0,1,2),(0,2,3),(3,2,1),(0,3,1)],'Fur','Head')
        for front,y in [(True,-.30),(False,.40)]:
            label=('Front' if front else 'Back')+('.L' if s<0 else '.R');hip=(s*.15,y,.66);knee=(s*.165,y-.06,.38);ankle=(s*.165,y+.06,.13)
            loft('WolfUpperLeg',[hip,(s*.17,y,.57),(s*.17,y-.04,.46),knee],[(.10,.11),(.11,.105),(.07,.075),(.05,.055)],'Fur',16,label+'Upper')
            loft('WolfLowerLeg',[knee,(s*.165,y+.04,.23),ankle],[(.05,.055),(.035,.04),(.035,.035)],'Fur',14,label+'Lower')
            uv_sphere('WolfJoint',knee,(.06,.06,.065),'Fur',12,7,label+'Lower');uv_sphere('WolfAnkle',ankle,(.045,.06,.06),'Fur',12,7,label+'Foot')
            uv_sphere('WolfPaw',(s*.165,y-.02,.07),(.065,.11,.06),'Fur',16,8,label+'Foot')
            for toe in [-1,0,1]:uv_sphere('WolfToe',(s*.165+toe*.032,y-.105,.055),(.023,.04,.026),'Dark',8,5,label+'Foot')
    loft('WolfTail',[(0,.50,.66),(0,.74,.58),(0,.95,.45),(0,1.11,.40)],[(.10,.10),(.12,.12),(.10,.10),(.025,.03)],'Fur',16,'Tail')
    for i in range(12):
        angle=i*2.4;y=-.35+(i%6)*.14;x=math.cos(angle)*.17;z=.69+math.sin(angle)*.18
        mesh('FurTuft',[(x-.025,y,z),(x+.025,y,z),(x,y+.08,z-.02),(x,y+.01,z+.012)],[(0,1,3),(0,3,2),(1,2,3),(0,2,1)],'Fur','Spine')
def wolf_rig(name):
    specs=[('Hips',(0,.35,.65),(0,.1,.65),None),('Spine',(0,.1,.65),(0,-.34,.72),'Hips'),('Neck',(0,-.34,.72),(0,-.55,.88),'Spine'),('Head',(0,-.55,.88),(0,-.86,.82),'Neck'),('Tail',(0,.5,.66),(0,1.05,.42),'Hips')]
    for s in [-1,1]:
        for front,y in [(True,-.30),(False,.40)]:
            n=('Front' if front else 'Back')+('.L' if s<0 else '.R');specs += [(n+'Upper',(s*.15,y,.66),(s*.165,y-.06,.38),'Spine' if front else 'Hips'),(n+'Lower',(s*.165,y-.06,.38),(s*.165,y+.06,.13),n+'Upper'),(n+'Foot',(s*.165,y+.06,.13),(s*.165,y-.09,.06),n+'Lower')]
    return make_rig(name,specs)
def animate_wolf(rig,name):
    rig.animation_data_create();bpy.context.scene.render.fps=30
    for clip in ['Idle','Walk','Run','Stalk','Attack','Die']:
        action=bpy.data.actions.new(name+'_'+clip);rig.animation_data.action=action
        for frame in range(1,32,3):
            t=(frame-1)/30;phase=t*math.tau
            for p in rig.pose.bones:p.rotation_mode='XYZ';p.rotation_euler=(0,0,0);p.location=(0,0,0)
            if clip in ['Walk','Run','Stalk']:
                for front in [True,False]:
                    for s in [-1,1]:
                        n=('Front' if front else 'Back')+('.L' if s<0 else '.R');wave=math.sin(phase+(0 if s<0 else math.pi)+(0 if front else math.pi));rig.pose.bones[n+'Upper'].rotation_euler.x=wave*(.65 if clip=='Run' else .38);rig.pose.bones[n+'Lower'].rotation_euler.x=max(0,-wave)*.75
                rig.pose.bones['Hips'].location.z=-.08 if clip=='Stalk' else math.cos(phase*2)*.02
            elif clip=='Attack':rig.pose.bones['Neck'].rotation_euler.x=math.sin(phase)*.4;rig.pose.bones['Spine'].rotation_euler.x=math.sin(phase)*.17
            elif clip=='Die':rig.pose.bones['Hips'].rotation_euler.y=min(1,t*1.6)*1.5;rig.pose.bones['Hips'].location.z=-.48*min(1,t*1.6)
            else:rig.pose.bones['Neck'].rotation_euler.z=math.sin(phase)*.04
            rig.pose.bones['Tail'].rotation_euler.z=math.sin(phase)*.1
            for p in rig.pose.bones:p.keyframe_insert('rotation_euler',frame=frame);p.keyframe_insert('location',frame=frame)
    rig.animation_data.action=bpy.data.actions[name+'_Idle'];bpy.context.scene.frame_set(1)
def outpost():
    for x in [-1.7,1.7]:
        for y in [-1.7,1.7]:
            box('TowerFoot',(x,y,.3),(.8,.8,.6),'Stone',.035);beam('TowerUpright',(x,y,.5),(x,y,6.7),.30,mat='Oak')
    for side in [-1,1]:
        beam('TowerCrossbrace',(side*1.7,-1.7,1),(side*1.7,1.7,4.2),.18,mat='Oak')
        beam('TowerCrossbrace',(side*1.7,1.7,1),(side*1.7,-1.7,4.2),.18,mat='Oak')
        beam('TowerCrossbrace',(-1.7,side*1.7,1),(1.7,side*1.7,4.2),.18,mat='Oak')
    for i in range(15):box('PlatformPlank',(-2.1+i*.3,0,4.3),(.285,4.5,.18),'Oak')
    for side in [-1,1]:
        for i in range(11):
            box('Balustrade',(side*2.13,-2+i*.4,4.85),(.1,.1,1),'Oak');box('Balustrade',(-2+i*.4,side*2.13,4.85),(.1,.1,1),'Oak')
        box('Railing',(side*2.13,0,5.35),(.15,4.5,.15),'Oak',.015);box('Railing',(0,side*2.13,5.35),(4.5,.15,.15),'Oak',.015)
        for row in range(5):
            for col in range(8):
                x=side*(row*.46+.15);o=box('WatchRoofTile',(x,-2.1+col*.61,7.7-abs(x)*.6),(.55,.65,.07),'Slate');o.rotation_euler[1]=side*math.atan(.6)
    beam('WatchRoofRidge',(0,-2.5,7.8),(0,2.5,7.8),.17,mat='Gold')
    for x in [-.52,.52]:beam('LadderSide',(x,-2.7,0),(x,-1.4,4.5),.11,mat='Oak',bevel=0)
    for i in range(14):box('LadderRung',(0,-2.7+i*.093,.2+i*.30),(1.12,.13,.08),'Oak')
    for side in [-1,1]:
        for i in range(9):
            x=side*3.2;y=-2.5+i*.65;cyl('PalisadeLog',(x,y,1.15),.19,2.3,'Oak',8)
            mesh('PalisadePoint',[(x+math.cos(k*math.tau/8)*.19,y+math.sin(k*math.tau/8)*.19,2.3) for k in range(8)]+[(x,y,2.65)],[(k,(k+1)%8,8) for k in range(8)],'Oak')
        box('PalisadeBrace',(side*3.2,.1,.9),(.12,5.9,.16),'Oak')
    beam('SignalPole',(0,0,7.7),(0,0,9.2),.085,mat='Oak',bevel=0)
    cloth_panel('RoyalWatchBanner',(.5,-.01,9.1),(.5,-.04,8.5),.95,'BlueCloth',None,5)
def wagon():
    for i in range(10):box('WagonDeck',(-.9+i*.2,0,.90),(.19,3.4,.16),'Oak')
    for side in [-1,1]:
        for z in [1.05,1.27,1.49]:box('CartSideBoard',(side*1.02,0,z),(.12,3.4,.19),'Oak',.01)
        for y in [-1.5,0,1.5]:box('IronUpright',(side*1.10,y,1.23),(.05,.09,.8),'Steel')
        beam('Shaft',(side*.65,-1.4,.85),(side*.55,-3.1,.62),.10,mat='Oak')
    for y in [-1.7,1.7]:
        for z in [1.05,1.27,1.49]:box('Tailboard',(0,y,z),(2.05,.13,.19),'Oak',.01)
    for y in [-1.05,1.05]:
        axle=cyl('IronAxle',(0,y,.6),.09,2.65,'Steel');axle.rotation_euler[1]=math.pi/2
        for s in [-1,1]:
            wheel=[];start=len(PARTS);center=(s*1.23,y,.58)
            rim=torus('Wheel_Rim',center,.53,.065,'Dark');rim.rotation_euler[1]=math.pi/2
            woodrim=torus('Wheel_WoodRim',center,.48,.065,'Oak');woodrim.rotation_euler[1]=math.pi/2
            hub=cyl('Wheel_Hub',center,.12,.25,'Steel');hub.rotation_euler[1]=math.pi/2
            for i in range(10):a=i*math.tau/10;beam('Wheel_Spoke',center,(center[0],y+math.sin(a)*.48,.58+math.cos(a)*.48),.045,mat='Oak',bevel=0)
            wheel=PARTS[start:];del PARTS[start:];wheelobj=join_meshes(wheel,'Wheel_'+('L' if s<0 else 'R')+('F' if y<0 else 'B'));bpy.context.scene.cursor.location=center;bpy.ops.object.origin_set(type='ORIGIN_CURSOR');WHEELS.append(wheelobj)
    for i in range(8):
        o=cyl('CargoLog',(-.6+(i%4)*.4,0,1.22+(i//4)*.35),.17,2.65,'Oak',12);o.rotation_euler[0]=math.pi/2
        for y in [-1.34,1.34]:
            end=cyl('LogEnd',(-.6+(i%4)*.4,y,1.22+(i//4)*.35),.145,.015,'Plaster',12);end.rotation_euler[0]=math.pi/2
    for y in [-.9,.9]:box('CargoStrap',(0,y,1.68),(1.7,.075,.045),'Leather')
    box('DriverSeat',(0,-1.4,1.85),(1.5,.48,.18),'Oak',.025)
    box('CaravanBanner',(-1.10,.4,1.35),(.025,.6,.6),'BlueCloth')

records=[]
for name,isworker in [('KOD_Human_Swordsman',False),('KOD_Human_Worker',True)]:
    human(isworker);tools=[]
    for label,prefixes in [('Weapon_Axe',('AxeOakHaft','ForgedAxeHead')),('Weapon_Sword',('SwordGrip','SwordGuard','SwordBlade')),('Weapon_Shield',('RoyalShield','ShieldRim'))]:
        pieces=[p for p in PARTS if p.name.startswith(prefixes)]
        if label=='Weapon_Shield':pieces += [p for p in PARTS if p.name.startswith('CrownChevron') and p.vertex_groups.get('LeftHand')]
        if pieces:
            for p in pieces:PARTS.remove(p)
            tools.append(join_meshes(pieces,label))
    rig=human_rig(name);animate_human(rig,name,isworker);records.append(export_asset(name,'Characters',1.6,(0,0,.95),rig,tools))
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
if PHASE=='characters':
    (SOURCE/'CharacterMetrics.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
    print('KOD_CHARACTER_REFINEMENT_COMPLETE')
    # Exit this companion script without terminating Blender before the main script finishes.
else:
    wolf();rig=wolf_rig('KOD_Wolf');animate_wolf(rig,'KOD_Wolf');records.append(export_asset('KOD_Wolf','Wildlife',1.45,(0,0,.5),rig))
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    outpost();records.append(export_asset('KOD_Outpost','Buildings',9,(0,0,4)))
    WHEELS=[];wagon();records.append(export_asset('KOD_TradingWagon','Vehicles',4,(0,-.4,1.0),attachments=WHEELS))
    (SOURCE/'CorePackMetrics.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
