using UnityEngine;
namespace KingOrDeath {
    public class FlagOwnershipVisual:MonoBehaviour {
        public Outpost post;Faction previous=(Faction)(-1);
        void LateUpdate(){if(!post||previous==post.store.owner)return;previous=post.store.owner;var mat=ModelFactory.Material(previous==Faction.Player?new Color(.08f,.18f,.6f):previous==Faction.Enemy?ModelFactory.Red:ModelFactory.Gold);foreach(var r in GetComponentsInChildren<Renderer>(true))if(r.name.StartsWith("CaptureFlag")){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)mats[i]=mat;r.sharedMaterials=mats;}}
    }
}
