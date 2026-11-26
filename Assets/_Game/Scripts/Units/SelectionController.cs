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

        // Control groups 2..9 (1 stays "whole army"). Version bumps on every change so the HUD rebuilds its tiles.
        public readonly List<Unit>[] groups = new List<Unit>[10];
        public int groupsVersion;
        int lastGroup = -1;
        float lastGroupAt;
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

        public static Vector3 Centroid(List<Unit> units)
        {
            Vector3 sum = Vector3.zero;
            int n = 0;
            foreach (var u in units)
                if (u)
                {
                    sum += u.transform.position;
                    n++;
                }
            return n > 0 ? sum / n : Vector3.zero;
        }

        public static void FocusCamera(Vector3 at)
        {
            var cam = UnityEngine.Camera.main;
            if (cam)
                cam.GetComponent<RtsCamera>().focus = new Vector3(at.x, 0, at.z);
        }

        public void Assign(int group)
        {
            selected.RemoveAll(u => !u);
            groups[group] = selected.Count > 0 ? new List<Unit>(selected) : null;
            groupsVersion++;
            if (selected.Count > 0)
                GameSession.I.Notify(Loc.F("n.group", group, selected.Count));
        }

        // Selects a group; a second press within 0.4 s brings the camera to it.
        public void Recall(int group)
        {
            var g = groups[group];
            if (g == null)
                return;
            int before = g.Count;
            g.RemoveAll(u => !u);
            if (g.Count != before)
                groupsVersion++;
            if (g.Count == 0)
            {
                groups[group] = null;
                groupsVersion++;
                return;
            }
            Clear();
            foreach (var u in g)
                Select(u);
            if (lastGroup == group && Time.unscaledTime - lastGroupAt < .4f)
                FocusCamera(Centroid(g));
            lastGroup = group;
            lastGroupAt = Time.unscaledTime;
        }

        // Plain move for the whole selection in formation (minimap right-click).
        public void MoveSelected(Vector3 goal)
        {
            selected.RemoveAll(u => !u);
            if (selected.Count == 0)
                return;
            int index = 0;
            foreach (var unit in selected)
            {
                var escort = unit.GetComponent<EscortOrder>();
                if (escort)
                    escort.wagon = null;
                unit.Move(goal + FormationOffset(index++, selected.Count));
            }
            UiAudio.Play("ui_tab", .6f);
            Hud.Order(goal, false);
            GameSession.I.Mark("orders");
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
            if (PauseMenu.IsOpen)
                return;
            if (Input.GetKeyDown(KeyCode.Space))
                GameSession.I.Pause();
            if (Time.timeScale == 0)
                return;
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SelectArmy();
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            for (int g = 2; g <= 9; g++)
                if (Input.GetKeyDown(KeyCode.Alpha0 + g))
                {
                    if (ctrl)
                        Assign(g);
                    else
                        Recall(g);
                }
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
                if (selected.Count > 0)
                    UiAudio.Play("ui_tab", .6f);
                if (selected.Count > 0)
                    Hud.Order(
                        victim ? victim.transform.position
                            : node ? node.transform.position
                            : corpse ? corpse.transform.position
                            : goal,
                        victim && selected.Exists(u => u && u.IsHostile(victim))
                    );
                GameSession.I.Mark("orders");
            }
        }
    }
}
