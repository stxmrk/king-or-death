using UnityEngine;

namespace KingOrDeath
{
    public class BuildingPlacement : MonoBehaviour
    {
        public bool active;
        public string reason;
        GameObject ghost;
        LineRenderer[] rings;

        public void Begin()
        {
            Cancel();
            active = true;
            ghost = ModelFactory.House("Placement preview", Vector3.zero);
            foreach (var c in ghost.GetComponentsInChildren<Collider>())
                Destroy(c);
            var stores = FindObjectsByType<ResourceStore>();
            rings = new LineRenderer[stores.Length];
            for (int i = 0; i < stores.Length; i++)
                if (stores[i].owner == Faction.Player)
                    rings[i] = ModelFactory.Ring(
                        "Build radius",
                        stores[i].transform.position,
                        stores[i].radius,
                        new Color(.6f, .8f, .55f)
                    );
        }

        public void Cancel()
        {
            active = false;
            if (ghost)
                Destroy(ghost);
            if (rings != null)
                foreach (var r in rings)
                    if (r)
                        Destroy(r.gameObject);
        }

        void Update()
        {
            if (!active || PauseMenu.IsOpen)
                return;
            // Esc is routed through PauseMenu so one press never both cancels and opens the menu.
            if (Input.GetMouseButtonDown(1))
            {
                Cancel();
                return;
            }
            if (Hud.OverUI())
                return;
            if (!SelectionController.GroundPoint(out var p))
                return;
            p.x = Mathf.Round(p.x / 2) * 2;
            p.z = Mathf.Round(p.z / 2) * 2;
            ghost.transform.position = p;
            bool valid = TerritorySystem.Valid(p, out var store, out reason);
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = ModelFactory.Material(
                    valid ? new Color(.25f, .8f, .4f) : new Color(.9f, .2f, .15f)
                );
            if (Input.GetMouseButtonDown(0) && valid && store.Spend(50, 20))
            {
                var go = ModelFactory.House("Frontier house", p);
                go.AddComponent<Building>();
                NavGrid.I.Rebuild();
                GameSession.I.Mark("build");
                GameSession.I.Notify(
                    "House built using resources at " + store.title + ". Territory is unchanged."
                );
                Cancel();
            }
        }
    }
}
