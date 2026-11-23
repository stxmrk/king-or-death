using UnityEngine;
namespace KingOrDeath {
    public class Hud : MonoBehaviour {
        GUIStyle text,small,title,button;Texture2D pixel;bool help=true;float scale;
        static Color ink=new Color(.055f,.075f,.077f,.96f),gold=new Color(.75f,.62f,.36f),pale=new Color(.87f,.87f,.75f);
        public static bool OverUI(){float y=Screen.height-Input.mousePosition.y;return y<100f*Screen.height/900f||y>Screen.height-190f*Screen.height/900f||(Input.mousePosition.x>Screen.width-300f*Screen.height/900f&&y<450f*Screen.height/900f);}
        void Init(){if(pixel)return;pixel=Texture2D.whiteTexture;text=new GUIStyle(GUI.skin.label){fontSize=16,normal={textColor=pale}};small=new GUIStyle(text){fontSize=13};title=new GUIStyle(text){fontSize=24,fontStyle=FontStyle.Bold,normal={textColor=gold}};button=new GUIStyle(GUI.skin.button){fontSize=13,normal={textColor=pale},alignment=TextAnchor.MiddleCenter};}
        void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,pixel);GUI.color=old;}
        void Label(float x,float y,float w,string value,GUIStyle style=null){GUI.Label(new Rect(x,y,w,28),value,style??text);}
        bool Button(float x,float y,float w,string value)=>GUI.Button(new Rect(x,y,w,28),value,button);
        void Panel(Rect r){Fill(r,ink);Fill(new Rect(r.x,r.y,r.width,2),gold);}
        void OnGUI(){
            Init();if(!GameSession.I||!SelectionController.I)return;
            scale=Screen.height/900f;float width=Screen.width/scale;var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            var session=GameSession.I;var select=SelectionController.I;var store=select.selectedStore?select.selectedStore:session.keep;
            Panel(new Rect(0,0,width,82));Label(24,12,330,"KING OR DEATH",title);Label(25,44,345,"A WORLD REMEMBERS EVERY BATTLE",small);
            Label(390,12,280,store.title.ToUpper()+"  /  "+store.owner,small);Label(390,39,480,"WOOD  "+store.wood+"     STONE  "+store.stone+"     FOOD  "+store.food);
            if(Button(width-305,16,85,Time.timeScale==0?"Resume":"Pause"))session.Pause();if(Button(width-210,16,90,"Restart"))session.Restart();if(Button(width-110,16,85,"Help"))help=!help;
            Label(width-300,49,290,"THE FIRST FRONTIER   ·   v0.1.0",small);
            if(Time.unscaledTime<session.noticeUntil){Panel(new Rect(24,100,Mathf.Min(930,width-360),48));Label(38,111,920,session.notice,small);}
            DrawWorldLabels();DrawObjectives(width);
            float bottom=710;Panel(new Rect(0,bottom,width,190));
            DrawMinimap(new Rect(16,bottom+14,240,160));
            Label(280,bottom+12,400,select.selected.Count>0?select.selected.Count+" SELECTED":"FRONTIER COMMAND",title);
            if(select.selected.Count>0&&select.selected[0]){
                var u=select.selected[0];Label(280,bottom+48,360,u.title+"  ·  "+Mathf.CeilToInt(u.health)+" / "+u.maxHealth,small);
                var worker=u.GetComponent<WorkerAgent>();if(worker){Label(280,bottom+72,350,worker.state+"  |  Cargo "+worker.carried,small);if(Button(280,bottom+103,92,"Bury"))select.corpseAction=CorpseAction.Bury;if(Button(378,bottom+103,92,"Burn"))select.corpseAction=CorpseAction.Burn;if(Button(476,bottom+103,92,"Hide"))select.corpseAction=CorpseAction.Hide;if(Button(280,bottom+137,92,"Carry"))select.corpseAction=CorpseAction.Carry;if(Button(378,bottom+137,92,"Drop body"))worker.DropBody();Label(480,bottom+137,240,"RMB corpse: "+select.corpseAction,small);}
                else if(u.kind==UnitKind.Swordsman){Label(280,bottom+78,380,"RMB move / attack   ·   Shift adds selection",small);if(Button(280,bottom+112,190,"Escort caravan")){var wagon=FindAnyObjectByType<TradeRoute>();if(wagon)foreach(var s in select.selected){var e=s.GetComponent<EscortOrder>();if(e)e.wagon=wagon.GetComponent<Unit>();}session.Notify("Selected swordsmen are escorting the caravan.");}}
            }else{Label(280,bottom+51,410,"Select soldiers, workers or a local store.",small);Label(280,bottom+78,410,"1  Select army     B  Build     Space  Pause",small);Label(280,bottom+105,410,"WASD pan  ·  Wheel zoom  ·  Q/E rotate",small);Label(280,bottom+132,410,"Drag a frame to select several units.",small);}
            float cx=Mathf.Max(690,width*.44f);Label(cx,bottom+12,340,"LOCAL DEVELOPMENT",small);
            if(Button(cx,bottom+43,225,"Build house  ·  50 W / 20 S"))GetComponent<BuildingPlacement>().Begin();
            if(Button(cx,bottom+79,225,"Recruit sword  ·  20 W / 25 F")){if(store.owner==Faction.Player&&store.Spend(20,0,25))WorldBuilder.SpawnUnit(UnitKind.Swordsman,Faction.Player,store.Dock,"Crown swordsman");else session.Notify("Not enough local resources, or store is not yours.");}
            if(Button(cx,bottom+115,108,"Wood crew"))AssignCrew();if(Button(cx+117,bottom+115,108,"Call raid"))session.Raid();
            Label(cx,bottom+150,280,"Houses do not extend territory.",small);
            DrawCaravan(width-415,bottom);
            var placement=GetComponent<BuildingPlacement>();if(placement.active){Panel(new Rect(width/2-260,640,520,48));Label(width/2-245,650,500,placement.reason+"  ·  Esc cancels",small);}
            if(help){Panel(new Rect(24,165,430,155));Label(40,176,400,"YOUR FIRST ORDERS",title);Label(40,212,400,"1. Select army [1], right-click the northern flag.",small);Label(40,239,400,"2. Capture Pineward, then choose Wood crew.",small);Label(40,266,400,"3. Start caravan; defend it or bury the fallen.",small);}
            if(Time.timeScale==0){Panel(new Rect(width/2-150,390,300,80));Label(width/2-65,412,200,"PAUSED",title);}
            GUI.matrix=old;
            if(select.dragging&&select.dragRect.width>3){Fill(select.dragRect,new Color(.4f,.75f,.65f,.18f));GUI.Box(select.dragRect,GUIContent.none);}
        }
        void DrawObjectives(float width){
            string[] keys={"orders","capture","build","harvest","load","delivery","battle","remains","wolves","cleanup"};string[] labels={"Command your troops","Capture a frontier post","Build within your territory","Deliver a worker's harvest","Load the caravan","Complete a trade delivery","Fight the Ashen host","See the fallen remain","Watch wolves follow the scent","Clear the remains"};
            Panel(new Rect(width-286,100,270,318));Label(width-270,112,250,"THE FIRST FRONTIER",small);
            for(int i=0;i<keys.Length;i++)Label(width-270,145+i*25,255,(GameSession.I.milestones.Contains(keys[i])?"✓  ":"·  ")+labels[i],small);
            var ecology=GetComponent<EcologySystem>();Label(width-270,393,255,"SCENT "+ecology.scent.ToString("0.0")+"   /   "+(ecology.wolvesHunting?"PACK HUNTING":"PACK AT DEN"),small);
        }
        void DrawCaravan(float x,float y){
            var route=FindAnyObjectByType<TradeRoute>();Label(x,y+12,400,"CARAVAN / PHYSICAL LOGISTICS",small);if(!route){Label(x,y+50,390,"Caravan lost. Salvage cargo with workers.",small);return;}
            Label(x,y+39,390,route.state+"   ·   "+route.cargo+" / "+GameSession.I.config.wagonCapacity+" "+(route.cargo>0?route.carriedResource:route.resource),small);
            Label(x,y+64,390,(route.source?route.source.title:"No source")+" → "+(route.destination?route.destination.title:"No destination"),small);
            if(Button(x,y+94,130,"Pineward → Keep"))Route(route,0);
            if(Button(x+138,y+94,130,"Eastwatch → Keep"))Route(route,1);
            if(Button(x+276,y+94,110,route.paused?"Resume":"Stop"))route.Toggle();
            if(Button(x,y+131,122,"Cargo: "+route.resource))route.resource=(ResourceKind)(((int)route.resource+1)%3);
            if(Button(x+130,y+131,110,"Reverse")){if(!route.SetRoute(route.destination,route.source))GameSession.I.Notify("Both route stores must be controlled.");}
            if(Button(x+248,y+131,138,"Select caravan")){SelectionController.I.Clear();SelectionController.I.Select(route.GetComponent<Unit>());}
        }
        void Route(TradeRoute route,int index){if(!route.SetRoute(GameSession.I.posts[index].store,GameSession.I.keep))GameSession.I.Notify("Capture this post before opening its trade route.");}
        void AssignCrew(){var post=GameSession.I.posts[0];if(post.store.owner!=Faction.Player){GameSession.I.Notify("Capture Pineward before assigning its wood crew.");return;}ResourceNode best=null;float distance=float.MaxValue;foreach(var n in FindObjectsByType<ResourceNode>())if(n.resource==ResourceKind.Wood&&n.remaining>0){float d=Vector3.Distance(n.transform.position,post.store.Dock);if(d<distance){distance=d;best=n;}}int count=0;foreach(var unit in Unit.All)if(unit.faction==Faction.Player&&unit.kind==UnitKind.Worker&&count++<2)unit.GetComponent<WorkerAgent>().Gather(best);GameSession.I.Notify("Two workers assigned to Pineward's woodland. Wood stays in its local store.");}
        void DrawWorldLabels(){
            var cam=UnityEngine.Camera.main;foreach(var u in Unit.All){var p=cam.WorldToScreenPoint(u.transform.position+Vector3.up*2.8f);if(p.z<=0)continue;float x=p.x/scale,y=(Screen.height-p.y)/scale;if(y<85||y>700)continue;
                if(u.selected||u.health<u.maxHealth){Fill(new Rect(x-20,y,40,5),Color.black);Fill(new Rect(x-19,y+1,38*u.health/u.maxHealth,3),u.faction==Faction.Player?new Color(.37f,.75f,.56f):ModelFactory.Red);}if(u.selected)Label(x-5,y+7,20,"◆",small);
            }
            foreach(var post in GameSession.I.posts){var p=cam.WorldToScreenPoint(post.transform.position+Vector3.up*6);if(p.z<0)continue;float x=p.x/scale-65,y=(Screen.height-p.y)/scale;Fill(new Rect(x,y,145,40),ink);Label(x+8,y+1,140,post.store.title+" · "+post.store.owner,small);Fill(new Rect(x+8,y+29,129,4),new Color(.25f,.28f,.22f));Fill(new Rect(x+8,y+29,129*post.progress,4),post.contested?ModelFactory.Red:gold);}
        }
        void DrawMinimap(Rect r){
            Fill(r,new Color(.21f,.28f,.2f));Fill(new Rect(r.x+(69f/128)*r.width,r.y,8f/128*r.width,r.height),new Color(.25f,.46f,.48f));
            foreach(var u in Unit.All){float x=r.x+(u.transform.position.x+64)/128*r.width,y=r.y+(48-u.transform.position.z)/96*r.height;Fill(new Rect(x-2,y-2,4,4),u.faction==Faction.Player?new Color(.55f,.9f,.76f):u.faction==Faction.Enemy?new Color(.9f,.35f,.22f):Color.gray);}
            foreach(var s in FindObjectsByType<ResourceStore>()){float x=r.x+(s.transform.position.x+64)/128*r.width,y=r.y+(48-s.transform.position.z)/96*r.height;Fill(new Rect(x-4,y-4,8,8),s.owner==Faction.Player?gold:Color.white);}
            if(Event.current.type==EventType.MouseDown&&r.Contains(Event.current.mousePosition)){var m=Event.current.mousePosition;UnityEngine.Camera.main.GetComponent<RtsCamera>().focus=new Vector3((m.x-r.x)/r.width*128-64,0,48-(m.y-r.y)/r.height*96);Event.current.Use();}
        }
    }
}
