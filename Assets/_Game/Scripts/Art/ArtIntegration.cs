using UnityEngine;
namespace KingOrDeath {
    [DefaultExecutionOrder(-20)]
    public class ArtIntegration:MonoBehaviour {
        public static ArtIntegration I;public ArtCatalog catalog;
        void Awake(){I=this;}
        void Start(){
            foreach(var u in Unit.All)Install(u);
            foreach(var b in FindObjectsByType<Building>())if(!b.GetComponent<ResourceStore>()||b.GetComponent<ResourceStore>()!=GameSession.I.keep){ReplaceChildren(b.transform,catalog.house);b.footprint=new Vector2(7,7);}
            foreach(var post in GameSession.I.posts){var go=Instantiate(catalog.outpost,post.transform);DisableColliders(go);if(post.flag)post.flag.enabled=false;foreach(var r in go.GetComponentsInChildren<Renderer>(true))if(r.name.StartsWith("CaptureFlag")&&r.name.EndsWith("_LOD0"))post.flag=r;go.AddComponent<FlagOwnershipVisual>().post=post;var obstacle=post.gameObject.AddComponent<Building>();obstacle.footprint=new Vector2(4,4);}
            NavGrid.I.Rebuild();
        }
        public void Install(Unit unit){
            if(!catalog||unit.transform.Find("AssetVisual"))return;var model=catalog.For(unit.kind);if(!model)return;
            var go=ReplaceChildren(unit.transform,model);go.name="AssetVisual";var driver=go.AddComponent<ModelAnimationDriver>();driver.unit=unit;
            TintFaction(go,unit.faction);
        }
        void TintFaction(GameObject go,Faction faction){if(faction!=Faction.Enemy)return;foreach(var r in go.GetComponentsInChildren<Renderer>()){foreach(var m in r.materials)if(m.name.Contains("BlueCloth")){var ao=m.GetTexture("_OcclusionMap");if(catalog.enemyCloth)m.CopyPropertiesFromMaterial(catalog.enemyCloth);m.SetTexture("_OcclusionMap",ao);}}}
        GameObject ReplaceChildren(Transform parent,GameObject prefab){foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}var visual=Instantiate(prefab,parent);visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;var target=parent.GetComponent<BoxCollider>();var shape=visual.GetComponent<BoxCollider>();if(target&&shape){target.center=shape.center;target.size=shape.size;}DisableColliders(visual);return visual;}
        static void DisableColliders(GameObject go){foreach(var c in go.GetComponentsInChildren<Collider>())c.enabled=false;}
        public GameObject House(string title,Vector3 p){var go=Instantiate(catalog.house,p,Quaternion.identity);go.name=title;return go;}
        public bool CopyCorpse(Unit unit,GameObject corpse){
            var prefab=catalog.For(unit.kind);if(!prefab)return false;var visual=Instantiate(prefab,corpse.transform);DisableColliders(visual);visual.transform.localRotation=unit.transform.rotation;TintFaction(visual,unit.faction);
            var animator=visual.GetComponentInChildren<Animator>();if(animator&&animator.runtimeAnimatorController){animator.Rebind();animator.Play("Die",0,1);animator.Update(0);animator.enabled=false;}
            else{visual.transform.localRotation*=Quaternion.Euler(85,0,0);}
            float lowest=float.PositiveInfinity;var group=visual.GetComponent<LODGroup>();foreach(var renderer in group.GetLODs()[0].renderers){if(renderer is SkinnedMeshRenderer skin){var mesh=new Mesh();skin.BakeMesh(mesh);foreach(var vertex in mesh.vertices)lowest=Mathf.Min(lowest,skin.transform.TransformPoint(vertex).y);Destroy(mesh);}else lowest=Mathf.Min(lowest,renderer.bounds.min.y);}if(!float.IsInfinity(lowest))visual.transform.position+=Vector3.up*(corpse.transform.position.y+.02f-lowest);
            return true;
        }
    }
}
