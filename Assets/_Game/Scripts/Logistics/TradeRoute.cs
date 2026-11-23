using UnityEngine;
namespace KingOrDeath {
    public class TradeRoute : MonoBehaviour {
        public ResourceStore source,destination;public ResourceKind resource=ResourceKind.Wood,carriedResource;
        public int cargo;public bool paused=true;public bool outbound;
        public string state="No route";public Unit escort;
        Unit unit;float timer;public int Delivered {get;private set;}
        void Awake(){unit=GetComponent<Unit>();}
        public bool SetRoute(ResourceStore from,ResourceStore to){
            if(!from||!to||from==to||from.owner!=unit.faction||to.owner!=unit.faction)return false;
            source=from;destination=to;paused=false;outbound=cargo>0;unit.Motor.Go(outbound?to.Dock:from.Dock);state="To source";return true;
        }
        public void Toggle(){paused=!paused;if(paused)unit.Motor.Stop();else if(source&&destination)unit.Motor.Go(outbound?destination.Dock:source.Dock);}
        void Update(){
            if(paused||!source||!destination){if(paused)state="Stopped";return;}
            if(source.owner!=unit.faction||destination.owner!=unit.faction){paused=true;unit.Motor.Stop();state="Route suspended: ownership lost";GameSession.I.Notify(state);return;}
            if(unit.Motor.blocked){state="Route blocked — retrying";return;}
            var goal=outbound?destination:source;
            if(Vector3.Distance(transform.position,goal.Dock)>3){state=outbound?"Delivering":"Returning";return;}
            timer+=Time.deltaTime;if(timer<1.5f)return;timer=0;
            if(outbound){goal.Add(carriedResource,cargo);Delivered+=cargo;if(cargo>0){GameSession.I.Mark("delivery");GameSession.I.Notify(cargo+" "+carriedResource+" delivered to "+goal.title);}cargo=0;outbound=false;unit.Motor.Go(source.Dock);}
            else{carriedResource=resource;cargo=source.Take(resource,GameSession.I.config.wagonCapacity);if(cargo==0){state="Waiting for "+resource;return;}outbound=true;GameSession.I.Mark("load");unit.Motor.Go(destination.Dock);}
        }
        public void DropCargo(){if(cargo<=0)return;var go=ModelFactory.Crate("Salvage",transform.position);var node=go.AddComponent<ResourceNode>();node.resource=carriedResource;node.remaining=cargo/2;cargo=0;GameSession.I.Notify("Wagon destroyed. Half the cargo is recoverable at the wreck.");}
    }
}
