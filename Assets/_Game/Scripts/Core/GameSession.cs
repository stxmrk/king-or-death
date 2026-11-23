using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace KingOrDeath {
    public class GameSession : MonoBehaviour {
        public static GameSession I { get; private set; }
        public GameConfig config;
        public ResourceStore keep;
        public Outpost[] posts;
        public Vector3 den=new Vector3(38,0,32);
        [System.NonSerialized] public HashSet<string> milestones=new HashSet<string>();
        public string notice="THE FIRST FRONTIER — secure a post, then establish a supply line.";
        public float noticeUntil;
        public bool raidCalled;
        void Awake(){ I=this; Time.timeScale=1; }
        void Start(){ Notify(notice); }
        public void Notify(string message){ notice=message; noticeUntil=Time.unscaledTime+10; Debug.Log("[Frontier] "+message); }
        public void Mark(string key){ milestones.Add(key); }
        public void Pause(){ Time.timeScale=Time.timeScale==0?1:0; }
        public void Restart(){ Time.timeScale=1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        public void Raid(){
            if(raidCalled)return; raidCalled=true;
            for(int i=0;i<5;i++) {
                var u=WorldBuilder.SpawnUnit(UnitKind.Swordsman,Faction.Enemy,new Vector3(50+i*1.5f,0,-9+i),"Ashen raider");
                u.GetComponent<CombatAgent>().rally=new Vector3(-7,0,-5);
            }
            Notify("RAID — armed men approach from the eastern road. Defend the bridge.");
        }
    }
}
