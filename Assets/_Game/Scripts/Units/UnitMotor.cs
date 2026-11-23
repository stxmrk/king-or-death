using System.Collections.Generic;
using UnityEngine;
namespace KingOrDeath {
    public class UnitMotor : MonoBehaviour {
        public float speed=5;
        public bool Arrived=>path==null||index>=path.Count;
        public bool blocked;
        public Vector3 destination;
        List<Vector3> path;int index, revision;float retry;
        public bool Go(Vector3 point){destination=point;path=NavGrid.I.Path(transform.position,point);index=0;revision=NavGrid.I.revision;blocked=path==null;retry=Time.time+2;return !blocked;}
        public void Stop(){path=null;blocked=false;}
        void Update(){
            if(blocked){if(Time.time>=retry)Go(destination);return;}
            if(Arrived)return;
            if(revision!=NavGrid.I.revision){Go(destination);return;}
            var delta=path[index]-transform.position;delta.y=0;
            if(delta.magnitude<.15f){index++;return;}
            transform.position+=delta.normalized*Mathf.Min(speed*Time.deltaTime,delta.magnitude);
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),Time.deltaTime*10);
        }
    }
}
