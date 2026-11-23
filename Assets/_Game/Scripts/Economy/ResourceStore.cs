using UnityEngine;
namespace KingOrDeath {
    public class ResourceStore : MonoBehaviour {
        public string title;public Faction owner=Faction.Player;public int wood,stone,food;public float radius;
        public float dockingDistance=5;
        public Vector3 Dock=>transform.position+Vector3.back*dockingDistance;
        public int Amount(ResourceKind k)=>k==ResourceKind.Wood?wood:k==ResourceKind.Stone?stone:food;
        public void Add(ResourceKind k,int amount){amount=Mathf.Max(0,amount);if(k==ResourceKind.Wood)wood+=amount;else if(k==ResourceKind.Stone)stone+=amount;else food+=amount;}
        public int Take(ResourceKind k,int desired){int n=Mathf.Min(Amount(k),Mathf.Max(0,desired));if(k==ResourceKind.Wood)wood-=n;else if(k==ResourceKind.Stone)stone-=n;else food-=n;return n;}
        public bool Spend(int w,int s,int f=0){if(wood<w||stone<s||food<f)return false;wood-=w;stone-=s;food-=f;return true;}
        public static ResourceStore NearestOwned(Vector3 p,Faction f){ResourceStore best=null;float d=float.MaxValue;foreach(var s in FindObjectsByType<ResourceStore>()){float n=Vector3.Distance(p,s.Dock);if(s.owner==f&&n<d){d=n;best=s;}}return best;}
    }
}
