using UnityEngine;
namespace KingOrDeath {
    public class ResourceNode : MonoBehaviour {
        public ResourceKind resource=ResourceKind.Wood;public int remaining=800;
        public int Harvest(int requested){int n=Mathf.Min(remaining,requested);remaining-=n;return n;}
    }
}
