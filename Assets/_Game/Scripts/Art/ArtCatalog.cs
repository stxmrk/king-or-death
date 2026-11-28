using UnityEngine;
namespace KingOrDeath {
    public class ArtCatalog:ScriptableObject {public Material enemyCloth;public GameObject house,swordsman,worker,wolf,outpost,wagon;public GameObject For(UnitKind kind)=>kind==UnitKind.Worker?worker:kind==UnitKind.Wolf?wolf:kind==UnitKind.Wagon?wagon:swordsman;}
}
