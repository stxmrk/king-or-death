using System.Collections.Generic;
using UnityEngine;

namespace KingOrDeath
{
    public class SelectionController : MonoBehaviour
    {
        public static SelectionController I;
        public List<Unit> selected = new List<Unit>();
        public ResourceStore selectedStore;
        public CorpseAction corpseAction = CorpseAction.Bury;
        public Rect dragRect;
        public bool dragging;
        Vector2 begin;
        BuildingPlacement placement;

        void Awake()
        {
            I = this;
            placement = GetComponent<BuildingPlacement>();
        }

        public static bool GroundPoint(out Vector3 point)
        {
            var ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }
            point = default;
            return false;
        }

        public void Clear()
        {
            foreach (var u in selected)
                if (u)
                    u.selected = false;
            selected.Clear();
            selectedStore = null;
        }

        public void Select(Unit unit)
        {
            if (!unit || unit.faction != Faction.Player || selected.Contains(unit))
                return;
            unit.selected = true;
            selected.Add(unit);
        }

        public void SelectArmy()
        {
            Clear();
            foreach (var u in Unit.All)
                if (u.faction == Faction.Player && u.kind == UnitKind.Swordsman)
                    Select(u);
        }

        public void BeginSelection(Vector2 point)
        {
            begin = point;
            dragging = true;
            dragRect = new Rect(point, Vector2.zero);
        }

        public void EndSelection(Vector2 point, bool additive)
        {
            dragRect = Rect.MinMaxRect(
                Mathf.Min(begin.x, point.x),
                Mathf.Min(begin.y, point.y),
                Mathf.Max(begin.x, point.x),
                Mathf.Max(begin.y, point.y)
            );
            dragging = false;
            if (!additive)
                Clear();
            if (dragRect.width + dragRect.height > 12)
            {
                foreach (var u in Unit.All)
                {
                    var p = UnityEngine.Camera.main.WorldToScreenPoint(u.transform.position);
                    if (p.z > 0 && dragRect.Contains(new Vector2(p.x, Screen.height - p.y)))
                        Select(u);
                }
            }
            else if (
                Physics.Raycast(
                    UnityEngine.Camera.main.ScreenPointToRay(
                        new Vector3(point.x, Screen.height - point.y, 0)
                    ),
                    out var hit,
                    300
                )
            )
            {
                var unit = hit.collider.GetComponentInParent<Unit>();
                if (unit)
                    Select(unit);
                else
                    selectedStore = hit.collider.GetComponentInParent<ResourceStore>();
            }
        }

        void OnGUI()
        {
            if (placement.active || Time.timeScale == 0)
                return;
            var e = Event.current;
            // Preserve event positions: rapid drags can deliver down/up between simulation frames.
            if (e.type == EventType.MouseDown && e.button == 0 && !Hud.OverUI())
                BeginSelection(e.mousePosition);
            if (e.type == EventType.MouseDrag && dragging)
                dragRect = Rect.MinMaxRect(
                    Mathf.Min(begin.x, e.mousePosition.x),
                    Mathf.Min(begin.y, e.mousePosition.y),
                    Mathf.Max(begin.x, e.mousePosition.x),
                    Mathf.Max(begin.y, e.mousePosition.y)
                );
            if (e.type == EventType.MouseUp && e.button == 0 && dragging)
                EndSelection(e.mousePosition, e.shift);
        }

        // Square block centred on the clicked point, 1.8 m spacing.
        public static Vector3 FormationOffset(int index, int count)
        {
            int cols = Mathf.CeilToInt(Mathf.Sqrt(count)),
                rows = Mathf.CeilToInt(count / (float)cols);
            return new Vector3(
                (index % cols - (cols - 1) * .5f) * 1.8f,
                0,
                (index / cols - (rows - 1) * .5f) * 1.8f
            );
        }

        void Update()
        {
            selected.RemoveAll(u => !u);
            if (Input.GetKeyDown(KeyCode.Space))
                GameSession.I.Pause();
            if (Time.timeScale == 0)
                return;
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SelectArmy();
            if (Input.GetKeyDown(KeyCode.B))
                placement.Begin();
            if (placement.active)
                return;
            if (Input.GetMouseButtonDown(1) && !Hud.OverUI() && GroundPoint(out var goal))
            {
                Physics.Raycast(
                    UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition),
                    out var hit,
                    300
                );
                var victim = hit.collider ? hit.collider.GetComponentInParent<Unit>() : null;
                var node = hit.collider ? hit.collider.GetComponentInParent<ResourceNode>() : null;
                var corpse = hit.collider ? hit.collider.GetComponentInParent<Corpse>() : null;
                int index = 0;
                foreach (var unit in selected)
                {
                    var fighter = unit.GetComponent<CombatAgent>();
                    var worker = unit.GetComponent<WorkerAgent>();
                    var escort = unit.GetComponent<EscortOrder>();
                    if (escort)
                        escort.wagon = null;
                    if (victim && unit.IsHostile(victim) && fighter)
                        fighter.Attack(victim);
                    else if (node && worker)
                        worker.Gather(node);
                    else if (corpse && worker)
                        worker.Clean(corpse, corpseAction);
                    else
                    {
                        if (worker && worker.corpse && worker.corpse.state == RemainsState.Carried)
                            unit.Motor.Go(goal);
                        else
                            unit.Move(goal + FormationOffset(index, selected.Count));
                    }
                    index++;
                }
                GameSession.I.Mark("orders");
            }
        }
    }
}
