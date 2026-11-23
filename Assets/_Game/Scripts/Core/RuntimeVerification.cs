using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace KingOrDeath {
    // Opt-in integration runner executes the production components in the actual player.
    public class RuntimeVerification : MonoBehaviour {
        List<string> results=new List<string>();string output;bool failed;
        IEnumerator Start(){
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-verify");if(index<0)yield break;
            output=index+1<args.Length?args[index+1]:Application.persistentDataPath;Directory.CreateDirectory(output);
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"TheFirstFrontier.png"));yield return new WaitForSeconds(1);
            Time.timeScale=8;StartCoroutine(Watchdog());yield return Run();Time.timeScale=1;
            File.WriteAllLines(Path.Combine(output,"RuntimeVerification.txt"),results);Debug.Log("VERIFICATION "+(failed?"FAILED":"PASSED"));Application.Quit(failed?1:0);
        }
        IEnumerator Watchdog(){yield return new WaitForSecondsRealtime(180);results.Add("FAIL: Verification watchdog expired");File.WriteAllLines(Path.Combine(output,"RuntimeVerification.txt"),results);Application.Quit(2);}
        void Check(bool ok,string name){results.Add((ok?"PASS: ":"FAIL: ")+name);Debug.Log(results[results.Count-1]);failed|=!ok;}
        IEnumerator Until(Func<bool> condition,float seconds){float end=Time.time+seconds;while(!condition()&&Time.time<end)yield return null;}
        IEnumerator Run(){
            var g=GameSession.I;var post=g.posts[0];var soldiers=new List<Unit>();var workers=new List<WorkerAgent>();TradeRoute route=null;
            foreach(var u in Unit.All){if(u.faction==Faction.Player&&u.kind==UnitKind.Swordsman)soldiers.Add(u);if(u.faction==Faction.Player&&u.kind==UnitKind.Worker)workers.Add(u.GetComponent<WorkerAgent>());if(u.kind==UnitKind.Wagon)route=u.GetComponent<TradeRoute>();}
            Check(soldiers.Count==8&&workers.Count==4&&route,"Scene contains initial army, workers and wagon");
            Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);foreach(var w in workers){var p=UnityEngine.Camera.main.WorldToScreenPoint(w.transform.position);var xy=new Vector2(p.x,Screen.height-p.y);min=Vector2.Min(min,xy);max=Vector2.Max(max,xy);}SelectionController.I.BeginSelection(min-Vector2.one*10);SelectionController.I.EndSelection(max+Vector2.one*10,false);Check(SelectionController.I.selected.Count==4,"Drag rectangle selects all four workers");SelectionController.I.Clear();
            var path=NavGrid.I.Path(new Vector3(-1,0,20),new Vector3(20,0,20));bool bridge=path!=null;bool crossed=false;if(path!=null)foreach(var p in path){bridge&=!NavGrid.River(p);if(p.x>5&&p.x<13&&Mathf.Abs(p.z+5)<=4)crossed=true;}Check(bridge&&crossed,"A* crosses river exclusively through bridge");
            foreach(var u in soldiers)u.Move(post.transform.position+new Vector3(UnityEngine.Random.Range(-3f,3f),0,-2));
            yield return Until(()=>post.store.owner==Faction.Player,50);Check(post.store.owner==Faction.Player,"Soldiers walk to flag and capture neutral post");
            var contest=WorldBuilder.SpawnUnit(UnitKind.Swordsman,Faction.Enemy,post.transform.position,"Contesting fixture");post.progress=.3f;post.Tick(.5f);Check(post.contested&&Mathf.Abs(post.progress-.3f)<.001f,"Opposing soldiers freeze contested capture progress");contest.gameObject.SetActive(false);Destroy(contest.gameObject);post.progress=0;yield return null;
            Check(!TerritorySystem.Valid(new Vector3(55,0,-35),out _,out _),"Building rejected outside territory");
            var buildPoint=new Vector3(-36,0,-34);bool canBuild=TerritorySystem.Valid(buildPoint,out var local,out var why);Check(canBuild,"Valid placement inside keep territory: "+why);
            float radius=g.keep.radius;if(canBuild&&local.Spend(50,20)){ModelFactory.House("Verification house",buildPoint).AddComponent<Building>();NavGrid.I.Rebuild();}Check(g.keep.radius==radius,"Ordinary house does not extend territory");
            ResourceNode tree=null;float nearest=float.MaxValue;foreach(var n in FindObjectsByType<ResourceNode>()){float d=Vector3.Distance(n.transform.position,post.store.Dock);if(n.resource==ResourceKind.Wood&&d<nearest){nearest=d;tree=n;}}
            int keepBefore=g.keep.wood;workers[0].Gather(tree);workers[1].Gather(tree);yield return Until(()=>post.store.wood>=16,65);Check(post.store.wood>=16,"Workers harvest and physically deposit at post");Check(g.keep.wood==keepBefore,"Harvest does not teleport into main keep");
            route.SetRoute(post.store,g.keep);yield return Until(()=>route.Delivered>0,100);Check(route.Delivered>0&&g.keep.wood>keepBefore,"Caravan loads, travels and unloads at keep");
            route.Toggle();foreach(var w in workers)w.Cancel();
            int reservedWood=post.store.wood;post.store.wood=0;route.SetRoute(post.store,g.keep);yield return Until(()=>route.state=="Waiting for Wood",60);Check(route.state=="Waiting for Wood"&&route.cargo==0,"Empty source makes caravan wait without creating cargo");post.store.wood=reservedWood+8;yield return Until(()=>route.cargo>0,5);Check(route.cargo>0,"Caravan resumes loading when source receives stock");route.Toggle();
            // Actual melee between two units, followed by ecological tick and physical cleanup.
            var ally=WorldBuilder.SpawnUnit(UnitKind.Swordsman,Faction.Player,new Vector3(25,0,21),"Verification defender");
            var enemy=WorldBuilder.SpawnUnit(UnitKind.Swordsman,Faction.Enemy,new Vector3(27,0,21),"Verification raider");enemy.health=10;ally.GetComponent<CombatAgent>().Attack(enemy);
            int corpsesBefore=CorpseSystem.I.remains.Count;yield return Until(()=>CorpseSystem.I.remains.Count>corpsesBefore,10);Check(CorpseSystem.I.remains.Count>corpsesBefore,"Melee damage kills unit and leaves persistent corpse");
            var second=WorldBuilder.SpawnUnit(UnitKind.Swordsman,Faction.Enemy,new Vector3(25,0,22),"Second raider");second.Damage(999);
            yield return new WaitForSeconds(g.config.scentDelay+g.config.ecologyInterval+2);Check(GetComponent<EcologySystem>().wolvesHunting,"Aged remains attract existing wolves from den");
            bool moved=false;foreach(var u in Unit.All)if(u.kind==UnitKind.Wolf&&Vector3.Distance(u.transform.position,g.den)>9)moved=true;yield return new WaitForSeconds(5);foreach(var u in Unit.All)if(u.kind==UnitKind.Wolf&&Vector3.Distance(u.transform.position,g.den)>9)moved=true;Check(moved,"Wolves travel physically away from forest den");
            var cleanTarget=CorpseSystem.I.remains[CorpseSystem.I.remains.Count-1];var cleaner=WorldBuilder.SpawnUnit(UnitKind.Worker,Faction.Player,cleanTarget.transform.position+Vector3.left*2,"Verification cleaner");cleaner.health=10000;cleaner.GetComponent<WorkerAgent>().Clean(cleanTarget,CorpseAction.Bury);yield return Until(()=>cleanTarget.state==RemainsState.Buried,15);Check(cleanTarget.state==RemainsState.Buried&&cleanTarget.Influence==0,"Worker reaches body and buries it, removing scent");
            int oldWood=post.store.wood;post.store.wood=0;Check(!post.store.Spend(1,0)&&post.store.wood==0,"Insufficient local stock rejects spending without underflow");post.store.wood=oldWood;
            foreach(var s in soldiers)if(s)s.Move(new Vector3(-25,0,-4));yield return new WaitForSeconds(14);var captor=WorldBuilder.SpawnUnit(UnitKind.Swordsman,Faction.Enemy,post.transform.position,"Recapture fixture");captor.health=10000;captor.GetComponent<CombatAgent>().enabled=false; // Hold this test soldier on the flag instead of chasing nearby workers.
            route.SetRoute(post.store,g.keep);yield return Until(()=>post.store.owner==Faction.Enemy,25);Check(post.store.owner==Faction.Enemy,"Enemy physically recaptures a player-owned post");yield return new WaitForSeconds(.5f);Check(route.paused,"Loss of post suspends caravan route");captor.gameObject.SetActive(false);Destroy(captor.gameObject);post.store.owner=Faction.Player;
            route.cargo=20;route.carriedResource=ResourceKind.Wood;var wreck=route.transform.position;route.GetComponent<Unit>().Damage(999);yield return null;bool salvage=false;foreach(var n in FindObjectsByType<ResourceNode>())if(n.name=="Salvage"&&n.remaining==10&&Vector3.Distance(n.transform.position,wreck)<1)salvage=true;Check(salvage,"Destroyed caravan drops half of its cargo as recoverable resource");
            // Enclose a walkable target with dynamic obstacles: no route must cross them.
            var barriers=new List<GameObject>();Vector3 center=new Vector3(48,0,-20);
            for(int i=0;i<4;i++){var b=new GameObject("Test blockade");b.transform.position=center+(i<2?new Vector3(i==0?-6:6,0,0):new Vector3(0,0,i==2?-6:6));var footprint=b.AddComponent<Building>();footprint.footprint=i<2?new Vector2(2,16):new Vector2(16,2);barriers.Add(b);}NavGrid.I.Rebuild();Check(NavGrid.I.Path(new Vector3(35,0,-20),center)==null,"Fully blocked route returns no path");foreach(var b in barriers)Destroy(b);yield return null;NavGrid.I.Rebuild();Check(NavGrid.I.Path(new Vector3(35,0,-20),center)!=null,"Route recovers after removing blockade");
        }
    }
}
