using UnityEngine;

namespace KingOrDeath
{
    public class TerritorySystem : MonoBehaviour
    {
        public static ResourceStore At(Vector3 position, float margin = 0)
        {
            ResourceStore best = null;
            float d = float.MaxValue;
            foreach (var s in FindObjectsByType<ResourceStore>())
            {
                float dist = Vector3.Distance(s.transform.position, position);
                if (s.owner == Faction.Player && dist + margin <= s.radius && dist < d)
                {
                    best = s;
                    d = dist;
                }
            }
            return best;
        }

        public static bool Valid(Vector3 p, out ResourceStore store, out string reason)
        {
            store = At(p, 4);
            reason = Loc.T("pl.outside");
            if (!store)
                return false;
            reason = Loc.T("pl.blocked");
            if (Mathf.Abs(p.z + 5) < 5 || Mathf.Abs(p.x) > 59 || Mathf.Abs(p.z) > 43)
                return false;
            for (float x = -3; x <= 3; x += 2)
            for (float z = -3; z <= 3; z += 2)
                if (!NavGrid.I.Walkable(p + new Vector3(x, 0, z)))
                    return false;
            foreach (var b in FindObjectsByType<Building>())
                if (
                    Mathf.Abs(p.x - b.transform.position.x) < b.footprint.x / 2 + 4
                    && Mathf.Abs(p.z - b.transform.position.z) < b.footprint.y / 2 + 4
                )
                    return false;
            foreach (var n in FindObjectsByType<ResourceNode>())
                if (Vector3.Distance(p, n.transform.position) < 4)
                    return false;
            reason = Loc.F("pl.cost", Loc.Name(store.title));
            if (store.wood < 50 || store.stone < 20)
                return false;
            reason = Loc.F("pl.from", Loc.Name(store.title));
            return true;
        }
    }
}
