using UnityEngine;
namespace KingOrDeath {
    public class ModelAnimationDriver:MonoBehaviour {
        public Unit unit;Animator animator;string current;Vector3 previous;Transform[] wheels;
        void Start(){animator=GetComponentInChildren<Animator>();previous=transform.position;wheels=System.Array.FindAll(GetComponentsInChildren<Transform>(),t=>t.name.StartsWith("Wheel_")&&t.name.Contains("_LOD"));}
        void Update(){
            if(!unit)return;float moved=Vector3.Distance(transform.position,previous);previous=transform.position;foreach(var w in wheels)w.Rotate(transform.right,moved/.55f*Mathf.Rad2Deg,Space.World);
            if(!animator||!animator.runtimeAnimatorController)return;string state="Idle";var worker=unit.GetComponent<WorkerAgent>();var fight=unit.GetComponent<CombatAgent>();
            if(!unit.Motor.Arrived)state=unit.kind==UnitKind.Worker?(worker.carried>0?"Carry":"Walk"):"Run";
            else if(worker&&worker.state.StartsWith("Gathering"))state="ChopWood";
            else if(fight&&fight.target&&Vector3.Distance(unit.transform.position,fight.target.transform.position)<fight.range+.3f)state=unit.kind==UnitKind.Wolf?"Attack":"Attack_01";
            animator.speed=state=="Run"?unit.Motor.speed/2.8f:(state=="Walk"||state=="Carry")?unit.Motor.speed/1.8f:1;
            if(state!=current){animator.CrossFade(state,.15f);current=state;}
        }
    }
}
