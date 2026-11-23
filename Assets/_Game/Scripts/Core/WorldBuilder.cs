using UnityEngine;
namespace KingOrDeath {
    public static class WorldBuilder {
        public static Unit SpawnUnit(UnitKind kind,Faction faction,Vector3 p,string name){
            var go=ModelFactory.UnitModel(kind,faction,p,name);var u=go.AddComponent<Unit>();u.kind=kind;u.faction=faction;u.title=name;
            u.maxHealth=u.health=kind==UnitKind.Wagon?240:kind==UnitKind.Wolf?65:kind==UnitKind.Worker?65:120;
            var motor=go.AddComponent<UnitMotor>();var cfg=GameSession.I?GameSession.I.config:null;
            motor.speed=kind==UnitKind.Wagon?(cfg?cfg.wagonSpeed:3.4f):kind==UnitKind.Wolf?(cfg?cfg.wolfSpeed:6):kind==UnitKind.Worker?(cfg?cfg.workerSpeed:4):(cfg?cfg.soldierSpeed:5);
            if(kind==UnitKind.Worker)go.AddComponent<WorkerAgent>();else if(kind==UnitKind.Wagon)go.AddComponent<TradeRoute>();else{var c=go.AddComponent<CombatAgent>();if(kind==UnitKind.Wolf){c.damage=11;c.interval=.8f;c.awareness=11;}else go.AddComponent<EscortOrder>();}
            return u;
        }
        public static void Build(GameConfig config){
            Random.InitState(1739);
            var world=new GameObject("THE FIRST FRONTIER");var session=world.AddComponent<GameSession>();session.config=config;world.AddComponent<NavGrid>();world.AddComponent<CorpseSystem>();world.AddComponent<EcologySystem>();world.AddComponent<TerritorySystem>();world.AddComponent<BuildingPlacement>();world.AddComponent<SelectionController>();world.AddComponent<Hud>();world.AddComponent<RuntimeVerification>();
            var land=new GameObject("Landscape");
            ModelFactory.Part("Earth plinth",PrimitiveType.Cube,land.transform,new Vector3(0,-1.5f,0),new Vector3(128,3,96),ModelFactory.Material(new Color(.23f,.27f,.17f)));
            for(int x=-62;x<64;x+=4)for(int z=-46;z<48;z+=4){float tone=Random.Range(-3,4)*.01f;ModelFactory.Part("Meadow",PrimitiveType.Cube,land.transform,new Vector3(x,-.06f,z),new Vector3(4,.1f,4),ModelFactory.Material(new Color(.35f+tone,.43f+tone,.25f+tone)));}
            var floor=land.AddComponent<BoxCollider>();floor.center=new Vector3(0,-.3f,0);floor.size=new Vector3(128,.5f,96);
            ModelFactory.Part("River banks",PrimitiveType.Cube,land.transform,new Vector3(9,.015f,0),new Vector3(10,.08f,96),ModelFactory.Material(new Color(.43f,.43f,.3f)));
            ModelFactory.Part("Silverrun River",PrimitiveType.Cube,land.transform,new Vector3(9,.08f,0),new Vector3(7.8f,.1f,96),ModelFactory.Material(new Color(.22f,.49f,.52f)));
            for(int z=-45;z<48;z+=5)ModelFactory.Part("Current",PrimitiveType.Cube,land.transform,new Vector3(9+Random.Range(-2f,2f),.15f,z),new Vector3(Random.Range(.8f,2f),.015f,.12f),ModelFactory.Material(new Color(.47f,.65f,.61f)));
            Road(land.transform,new Vector3(-58,.07f,-5),new Vector3(60,.07f,-5),4.5f);
            Road(land.transform,new Vector3(-23,.07f,-5),new Vector3(-17,.07f,24),3);
            Road(land.transform,new Vector3(33,.07f,-5),new Vector3(36,.07f,17),3);
            var bridge=ModelFactory.Group("Stonebridge",new Vector3(9,0,-5));ModelFactory.Part("Deck",PrimitiveType.Cube,bridge.transform,new Vector3(0,.3f,0),new Vector3(12,.5f,7),ModelFactory.Material(ModelFactory.Stone));
            for(int side=-1;side<=1;side+=2)for(int x=-5;x<=5;x+=2){ModelFactory.Part("Bridge parapet",PrimitiveType.Cube,bridge.transform,new Vector3(x,.9f,side*3.5f),new Vector3(1.6f,1.3f,.6f),ModelFactory.Material(ModelFactory.Stone));}
            var keep=ModelFactory.Castle(new Vector3(-35,0,-17));session.keep=keep.AddComponent<ResourceStore>();session.keep.title="Crownwatch";session.keep.radius=config.keepRadius;session.keep.dockingDistance=9;session.keep.wood=220;session.keep.stone=120;session.keep.food=100;
            for(int i=0;i<4;i++){var h=ModelFactory.House("Crownwatch cottage",new Vector3(-47+i*7,0,i%2==0?1:6));h.AddComponent<Building>();}
            session.posts=new[]{Post("Pineward",new Vector3(-17,0,23),config),Post("Eastwatch",new Vector3(37,0,12),config)};
            for(int i=0;i<100;i++){
                Vector3 p=i<50?new Vector3(Random.Range(-58f,-25f),0,Random.Range(19f,44f)):new Vector3(Random.Range(20f,57f),0,Random.Range(27f,44f));
                if(Vector3.Distance(p,session.den)<6)continue;ModelFactory.Tree(p,Random.Range(.65f,1.3f),i%4==0);
            }
            for(int i=0;i<10;i++)ModelFactory.Tree(new Vector3(-7+(i%3)*2,0,19+(i/3)*3),.9f,true);
            var quarry=ModelFactory.Group("Limestone outcrop",new Vector3(-49,0,-30));for(int i=0;i<7;i++)ModelFactory.Part("Limestone",PrimitiveType.Sphere,quarry.transform,new Vector3(Random.Range(-2f,2f),.7f,Random.Range(-2f,2f)),new Vector3(2,1.6f,2),ModelFactory.Material(ModelFactory.Stone));quarry.AddComponent<ResourceNode>().resource=ResourceKind.Stone;ModelFactory.BoxHit(quarry,new Vector3(6,3,6),Vector3.up);
            var farm=ModelFactory.Group("Golden field",new Vector3(-18,0,-28));ModelFactory.Part("Soil",PrimitiveType.Cube,farm.transform,Vector3.zero,new Vector3(9,.12f,8),ModelFactory.Material(new Color(.37f,.28f,.15f)));for(int i=0;i<6;i++)ModelFactory.Part("Wheat row",PrimitiveType.Cube,farm.transform,new Vector3(i*1.3f-3,.4f,0),new Vector3(.7f,.8f,7),ModelFactory.Material(ModelFactory.Gold));farm.AddComponent<ResourceNode>().resource=ResourceKind.Food;ModelFactory.BoxHit(farm,new Vector3(9,1,8),Vector3.up*.5f);
            var den=ModelFactory.Group("Greyfang Den",session.den);for(int i=0;i<6;i++){float a=i*Mathf.PI/3;ModelFactory.Part("Den boulder",PrimitiveType.Sphere,den.transform,new Vector3(Mathf.Cos(a)*3,1.2f,Mathf.Sin(a)*3),new Vector3(3,2.7f,3),ModelFactory.Material(new Color(.3f,.34f,.3f)));}
            var ruins=ModelFactory.Group("The Forgotten Crown",new Vector3(38,0,-28));for(int i=0;i<6;i++){var col=ModelFactory.Part("Broken pillar",PrimitiveType.Cylinder,ruins.transform,new Vector3(i%3*4,1.5f,i/3*6),new Vector3(1.4f,Random.Range(1.4f,2.8f),1.4f),ModelFactory.Material(new Color(.55f,.55f,.43f)));col.transform.rotation=Quaternion.Euler(0,0,Random.Range(-10f,10f));}ModelFactory.Part("Ancient lintel",PrimitiveType.Cube,ruins.transform,new Vector3(2,4.5f,0),new Vector3(6,1,1.7f),ModelFactory.Material(ModelFactory.Stone));
            for(int i=0;i<55;i++){var p=new Vector3(Random.Range(-60f,60f),0,Random.Range(-43f,43f));if(Mathf.Abs(p.x-9)<7||Mathf.Abs(p.z+5)<7)continue;var rock=ModelFactory.Part("Fieldstone",PrimitiveType.Sphere,land.transform,p+Vector3.up*.2f,new Vector3(.7f,.45f,.65f),ModelFactory.Material(new Color(.43f,.46f,.35f)));}
            for(int i=0;i<8;i++)SpawnUnit(UnitKind.Swordsman,Faction.Player,new Vector3(-28+(i%4)*2,0,-6-(i/4)*2),"Crown swordsman");
            for(int i=0;i<4;i++)SpawnUnit(UnitKind.Worker,Faction.Player,new Vector3(-21+i*2,0,-15),"Frontier worker");
            SpawnUnit(UnitKind.Wagon,Faction.Player,new Vector3(-36,0,-5),"Crown caravan");
            for(int i=0;i<3;i++)SpawnUnit(UnitKind.Wolf,Faction.Wildlife,session.den+new Vector3(-4+i*2,0,-5),"Greyfang wolf");
            for(int i=0;i<3;i++)SpawnUnit(UnitKind.Swordsman,Faction.Enemy,new Vector3(24+i*2,0,-7),"Ashen swordsman");
            var cam=new GameObject("RTS Camera");cam.tag="MainCamera";var camera=cam.AddComponent<UnityEngine.Camera>();camera.orthographic=true;camera.orthographicSize=34;camera.farClipPlane=250;camera.nearClipPlane=.1f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.32f,.29f);cam.AddComponent<AudioListener>();cam.AddComponent<RtsCamera>();cam.transform.rotation=Quaternion.Euler(55,0,0);cam.transform.position=new Vector3(-4,0,2)-cam.transform.forward*85;
            var sun=new GameObject("Late afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(42,-35,0);sun.color=new Color(1,.91f,.78f);sun.intensity=1.35f;sun.shadows=LightShadows.Soft;RenderSettings.sun=sun;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.52f,.62f,.65f);RenderSettings.ambientEquatorColor=new Color(.36f,.42f,.37f);RenderSettings.ambientGroundColor=new Color(.22f,.24f,.19f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.48f,.56f,.54f);RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=100;RenderSettings.fogEndDistance=190;
        }
        static void Road(Transform parent,Vector3 a,Vector3 b,float width){var go=ModelFactory.Part("Trade road",PrimitiveType.Cube,parent,(a+b)/2,new Vector3(width,.08f,Vector3.Distance(a,b)),ModelFactory.Material(new Color(.53f,.46f,.31f)));go.transform.rotation=Quaternion.LookRotation(b-a);}
        static Outpost Post(string title,Vector3 p,GameConfig config){
            var house=ModelFactory.House(title+" store",p+Vector3.right*5);house.AddComponent<Building>();var store=house.AddComponent<ResourceStore>();store.title=title;store.owner=Faction.Neutral;store.radius=config.outpostRadius;store.wood=0;store.stone=60;store.food=35;
            var flagRoot=ModelFactory.Group(title+" control flag",p);ModelFactory.Part("Flagpole",PrimitiveType.Cylinder,flagRoot.transform,Vector3.up*2.5f,new Vector3(.13f,2.5f,.13f),ModelFactory.Material(ModelFactory.Wood));var flag=ModelFactory.Part("Pennant",PrimitiveType.Cube,flagRoot.transform,new Vector3(.9f,4.3f,0),new Vector3(1.8f,1.1f,.08f),ModelFactory.Material(ModelFactory.Gold));
            ModelFactory.Ring(title+" capture circle",p,7,new Color(.6f,.56f,.36f));var post=flagRoot.AddComponent<Outpost>();post.store=store;post.flag=flag.GetComponent<Renderer>();return post;
        }
    }
}
