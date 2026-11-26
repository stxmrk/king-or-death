using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    // In-match HUD on UI Toolkit (Hud.uxml). Static labels are Loc keys in UXML; values refresh every frame.
    // shortcut: simulation strings (notices, worker/caravan states, placement reasons, place names) stay English,
    // localize them when those systems get message keys.
    [RequireComponent(typeof(UIDocument))]
    public class Hud : MonoBehaviour
    {
        public static Hud I { get; private set; }

        static readonly string[] objectives =
        {
            "orders",
            "capture",
            "build",
            "harvest",
            "load",
            "delivery",
            "battle",
            "remains",
            "wolves",
            "cleanup",
        };

        VisualElement root, hudRoot, notice, helpPanel, placementBanner, pausedBanner, dragRect;
        VisualElement workerPanel, swordPanel, idlePanel, caravanControls;
        Label storeName, storeStock, versionLabel, noticeText, placementText, scentLabel;
        Label selectionTitle, unitInfo, workerState, corpseActionLabel, caravanState, caravanRoute;
        Button pauseButton, routeToggleButton, cargoButton;
        HudOverlay overlay;
        HudMinimap minimap;
        readonly List<VisualElement> objectiveRows = new List<VisualElement>();
        readonly List<(Outpost post, VisualElement tag, Label text, VisualElement fill)> postTags =
            new List<(Outpost, VisualElement, Label, VisualElement)>();
        BuildingPlacement placement;
        EcologySystem ecology;
        bool help;

        // True when the pointer is over any pickable UI element, so world clicks should be ignored.
        public static bool OverUI()
        {
            var panel = I ? I.root.panel : null;
            if (panel == null)
                return false;
            var p = RuntimePanelUtils.ScreenToPanel(
                panel,
                new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)
            );
            return panel.Pick(p) != null;
        }

        void Awake() => I = this;

        void OnDestroy()
        {
            if (I == this)
                I = null;
            Loc.Changed -= Relocalize;
        }

        void Start()
        {
            help = GameSettings.Hints; // settings load after scene objects deserialize
            root = GetComponent<UIDocument>().rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            hudRoot = root.Q("hudRoot");
            overlay = root.Q<HudOverlay>("worldLayer");
            minimap = root.Q<HudMinimap>("minimap");
            notice = root.Q("notice");
            helpPanel = root.Q("helpPanel");
            placementBanner = root.Q("placementBanner");
            pausedBanner = root.Q("pausedBanner");
            dragRect = root.Q("dragRect");
            workerPanel = root.Q("workerPanel");
            swordPanel = root.Q("swordPanel");
            idlePanel = root.Q("idlePanel");
            caravanControls = root.Q("caravanControls");
            storeName = root.Q<Label>("storeName");
            storeStock = root.Q<Label>("storeStock");
            versionLabel = root.Q<Label>("versionLabel");
            noticeText = root.Q<Label>("noticeText");
            placementText = root.Q<Label>("placementText");
            scentLabel = root.Q<Label>("scentLabel");
            selectionTitle = root.Q<Label>("selectionTitle");
            unitInfo = root.Q<Label>("unitInfo");
            workerState = root.Q<Label>("workerState");
            corpseActionLabel = root.Q<Label>("corpseActionLabel");
            caravanState = root.Q<Label>("caravanState");
            caravanRoute = root.Q<Label>("caravanRoute");
            pauseButton = root.Q<Button>("pauseButton");
            routeToggleButton = root.Q<Button>("routeToggleButton");
            cargoButton = root.Q<Button>("cargoButton");
            placement = FindAnyObjectByType<BuildingPlacement>();
            ecology = FindAnyObjectByType<EcologySystem>();

            var list = root.Q("objectiveList");
            foreach (var key in objectives)
            {
                var row = new VisualElement();
                row.AddToClassList("objective");
                var mark = new VisualElement();
                mark.AddToClassList("objective-mark");
                row.Add(mark);
                var text = new Label("obj." + key);
                text.AddToClassList("hud-small");
                row.Add(text);
                list.Add(row);
                objectiveRows.Add(row);
            }
            foreach (var post in GameSession.I.posts)
                AddPostTag(post);

            Wire();
            Loc.Apply(root);
            Loc.Changed += Relocalize;
        }

        void Relocalize() => Loc.Apply(root);

        void AddPostTag(Outpost post)
        {
            var tag = new VisualElement { pickingMode = PickingMode.Ignore };
            tag.AddToClassList("post-tag");
            var text = new Label { pickingMode = PickingMode.Ignore };
            text.AddToClassList("hud-small");
            tag.Add(text);
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("post-tag-bar");
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("post-tag-fill");
            bar.Add(fill);
            tag.Add(bar);
            overlay.Add(tag);
            postTags.Add((post, tag, text, fill));
        }

        void On(string name, System.Action action) => root.Q<Button>(name).clicked += action;

        void Wire()
        {
            var session = GameSession.I;
            var select = SelectionController.I;
            On("pauseButton", session.Pause);
            On(
                "menuButton",
                () =>
                {
                    if (PauseMenu.I)
                        PauseMenu.I.Open();
                }
            );
            On("helpButton", () => help = !help);
            On("buryButton", () => select.corpseAction = CorpseAction.Bury);
            On("burnButton", () => select.corpseAction = CorpseAction.Burn);
            On("hideButton", () => select.corpseAction = CorpseAction.Hide);
            On("carryButton", () => select.corpseAction = CorpseAction.Carry);
            On(
                "dropButton",
                () =>
                {
                    var worker = FirstSelected()?.GetComponent<WorkerAgent>();
                    if (worker)
                        worker.DropBody();
                }
            );
            On("escortButton", Escort);
            On("buildButton", () => placement.Begin());
            On("recruitButton", Recruit);
            On("crewButton", AssignCrew);
            On("raidButton", session.Raid);
            On("routePineButton", () => Route(0));
            On("routeEastButton", () => Route(1));
            On(
                "routeToggleButton",
                () =>
                {
                    var route = Caravan();
                    if (route)
                        route.Toggle();
                }
            );
            On(
                "cargoButton",
                () =>
                {
                    var route = Caravan();
                    if (route)
                        route.resource = (ResourceKind)(((int)route.resource + 1) % 3);
                }
            );
            On(
                "reverseButton",
                () =>
                {
                    var route = Caravan();
                    if (route && !route.SetRoute(route.destination, route.source))
                        session.Notify(Loc.T("hud.n.route.both"));
                }
            );
            On(
                "selectCaravanButton",
                () =>
                {
                    var route = Caravan();
                    if (!route)
                        return;
                    select.Clear();
                    select.Select(route.GetComponent<Unit>());
                }
            );
        }

        static TradeRoute Caravan() => FindAnyObjectByType<TradeRoute>();

        static Unit FirstSelected()
        {
            var s = SelectionController.I.selected;
            return s.Count > 0 && s[0] ? s[0] : null;
        }

        static void Show(VisualElement e, bool visible) => e.EnableInClassList("hidden", !visible);

        void Update()
        {
            var session = GameSession.I;
            var select = SelectionController.I;
            if (root == null || !session || !select)
                return;
            Show(hudRoot, !PauseMenu.IsOpen);
            if (PauseMenu.IsOpen)
                return;

            var store = select.selectedStore ? select.selectedStore : session.keep;
            storeName.text = store.title.ToUpper() + " · " + Loc.T("faction." + store.owner).ToUpper();
            storeStock.text = string.Format(Loc.T("hud.stock"), store.wood, store.stone, store.food);
            bool paused = Time.timeScale == 0;
            pauseButton.text = Loc.T(paused ? "hud.resume" : "hud.pause");
            versionLabel.text = Loc.T("hud.scenario") + " · v" + Application.version;
            Show(notice, Time.unscaledTime < session.noticeUntil);
            noticeText.text = session.notice;
            Show(helpPanel, help);
            Show(pausedBanner, paused);
            Show(placementBanner, placement.active);
            if (placement.active)
                placementText.text = placement.reason + " · " + Loc.T("hud.place.cancel");

            for (int i = 0; i < objectives.Length; i++)
                objectiveRows[i].EnableInClassList("objective--done", session.milestones.Contains(objectives[i]));
            scentLabel.text =
                Loc.T("hud.scent")
                + " "
                + ecology.scent.ToString("0.0")
                + " / "
                + Loc.T(ecology.wolvesHunting ? "hud.pack.hunt" : "hud.pack.den");

            RefreshSelection(select);
            RefreshCaravan();
            RefreshWorld();
        }

        void RefreshSelection(SelectionController select)
        {
            var u = FirstSelected();
            var worker = u ? u.GetComponent<WorkerAgent>() : null;
            selectionTitle.text =
                select.selected.Count > 0
                    ? string.Format(Loc.T("hud.selected"), select.selected.Count)
                    : Loc.T("hud.command");
            Show(unitInfo, u);
            Show(workerPanel, worker);
            Show(swordPanel, u && !worker && u.kind == UnitKind.Swordsman);
            Show(idlePanel, !u);
            if (u)
                unitInfo.text = u.title + " · " + Mathf.CeilToInt(u.health) + " / " + u.maxHealth;
            if (worker)
            {
                workerState.text = worker.state + " | " + Loc.T("hud.cargo") + " " + worker.carried;
                corpseActionLabel.text =
                    Loc.T("hud.rmb.corpse") + " " + Loc.T("corpse." + select.corpseAction);
            }
        }

        void RefreshCaravan()
        {
            var route = Caravan();
            Show(caravanControls, route);
            Show(caravanRoute, route);
            if (!route)
            {
                caravanState.text = Loc.T("hud.caravan.lost");
                return;
            }
            caravanState.text =
                route.state
                + " · "
                + route.cargo
                + " / "
                + GameSession.I.config.wagonCapacity
                + " "
                + Loc.T("res." + (route.cargo > 0 ? route.carriedResource : route.resource));
            caravanRoute.text =
                (route.source ? route.source.title : Loc.T("hud.no.source"))
                + " › "
                + (route.destination ? route.destination.title : Loc.T("hud.no.destination"));
            routeToggleButton.text = Loc.T(route.paused ? "hud.resume" : "hud.stop");
            cargoButton.text = Loc.T("hud.cargo") + ": " + Loc.T("res." + route.resource);
        }

        void RefreshWorld()
        {
            var cam = UnityEngine.Camera.main;
            var panel = root.panel;
            foreach (var (post, tag, text, fill) in postTags)
            {
                var screen = cam.WorldToScreenPoint(post.transform.position + Vector3.up * 6);
                Show(tag, screen.z > 0);
                var p = RuntimePanelUtils.CameraTransformWorldToPanel(panel, post.transform.position + Vector3.up * 6, cam);
                tag.style.left = p.x - 65;
                tag.style.top = p.y;
                text.text = post.store.title + " · " + Loc.T("faction." + post.store.owner);
                fill.style.width = Length.Percent(post.progress * 100);
                tag.EnableInClassList("post-tag--contested", post.contested);
            }
            overlay.MarkDirtyRepaint();
            minimap.MarkDirtyRepaint();

            var select = SelectionController.I;
            bool drag = select.dragging && select.dragRect.width > 3;
            Show(dragRect, drag);
            if (drag)
            {
                var a = RuntimePanelUtils.ScreenToPanel(panel, select.dragRect.min);
                var b = RuntimePanelUtils.ScreenToPanel(panel, select.dragRect.max);
                dragRect.style.left = a.x;
                dragRect.style.top = a.y;
                dragRect.style.width = b.x - a.x;
                dragRect.style.height = b.y - a.y;
            }
        }

        void Escort()
        {
            var wagon = Caravan();
            if (wagon)
                foreach (var s in SelectionController.I.selected)
                {
                    var e = s ? s.GetComponent<EscortOrder>() : null;
                    if (e)
                        e.wagon = wagon.GetComponent<Unit>();
                }
            GameSession.I.Notify(Loc.T("hud.n.escort"));
        }

        void Recruit()
        {
            var store = SelectionController.I.selectedStore ? SelectionController.I.selectedStore : GameSession.I.keep;
            if (store.owner == Faction.Player && store.Spend(20, 0, 25))
                WorldBuilder.SpawnUnit(UnitKind.Swordsman, Faction.Player, store.Dock, "Crown swordsman");
            else
                GameSession.I.Notify(Loc.T("hud.n.recruit"));
        }

        void Route(int index)
        {
            var route = Caravan();
            if (route && !route.SetRoute(GameSession.I.posts[index].store, GameSession.I.keep))
                GameSession.I.Notify(Loc.T("hud.n.route.capture"));
        }

        void AssignCrew()
        {
            var post = GameSession.I.posts[0];
            if (post.store.owner != Faction.Player)
            {
                GameSession.I.Notify(Loc.T("hud.n.crew.capture"));
                return;
            }
            ResourceNode best = null;
            float distance = float.MaxValue;
            foreach (var n in FindObjectsByType<ResourceNode>())
                if (n.resource == ResourceKind.Wood && n.remaining > 0)
                {
                    float d = Vector3.Distance(n.transform.position, post.store.Dock);
                    if (d < distance)
                    {
                        distance = d;
                        best = n;
                    }
                }
            int count = 0;
            foreach (var unit in Unit.All)
                if (unit.faction == Faction.Player && unit.kind == UnitKind.Worker && count++ < 2)
                    unit.GetComponent<WorkerAgent>().Gather(best);
            GameSession.I.Notify(Loc.T("hud.n.crew"));
        }
    }

    static class HudDraw
    {
        public static readonly Color Gold = new Color(.75f, .62f, .36f);

        public static void Fill(Painter2D p, float x, float y, float w, float h, Color c)
        {
            p.fillColor = c;
            p.BeginPath();
            p.MoveTo(new Vector2(x, y));
            p.LineTo(new Vector2(x + w, y));
            p.LineTo(new Vector2(x + w, y + h));
            p.LineTo(new Vector2(x, y + h));
            p.ClosePath();
            p.Fill();
        }
    }

    // Health bars and selection marks over units, drawn in panel space.
    [UxmlElement]
    public partial class HudOverlay : VisualElement
    {
        public HudOverlay() => generateVisualContent += Draw;

        void Draw(MeshGenerationContext ctx)
        {
            var cam = UnityEngine.Camera.main;
            if (!cam || panel == null)
                return;
            var p = ctx.painter2D;
            foreach (var u in Unit.All)
            {
                if (!u || !(u.selected || u.health < u.maxHealth))
                    continue;
                var world = u.transform.position + Vector3.up * 2.8f;
                if (cam.WorldToScreenPoint(world).z <= 0)
                    continue;
                var s = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, cam);
                if (s.y < 85 || s.y > 700)
                    continue;
                HudDraw.Fill(p, s.x - 20, s.y, 40, 5, Color.black);
                HudDraw.Fill(
                    p,
                    s.x - 19,
                    s.y + 1,
                    38 * u.health / u.maxHealth,
                    3,
                    u.faction == Faction.Player ? new Color(.37f, .75f, .56f) : ModelFactory.Red
                );
                if (u.selected)
                {
                    p.fillColor = HudDraw.Gold;
                    p.BeginPath();
                    p.MoveTo(new Vector2(s.x, s.y + 9));
                    p.LineTo(new Vector2(s.x + 4, s.y + 13));
                    p.LineTo(new Vector2(s.x, s.y + 17));
                    p.LineTo(new Vector2(s.x - 4, s.y + 13));
                    p.ClosePath();
                    p.Fill();
                }
            }
        }
    }

    // Top-down map of the 128x96 world; clicking moves the camera focus there.
    [UxmlElement]
    public partial class HudMinimap : VisualElement
    {
        public HudMinimap()
        {
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(e =>
            {
                var r = contentRect;
                var cam = UnityEngine.Camera.main;
                if (cam)
                    cam.GetComponent<RtsCamera>().focus = new Vector3(
                        e.localPosition.x / r.width * 128 - 64,
                        0,
                        48 - e.localPosition.y / r.height * 96
                    );
                e.StopPropagation();
            });
        }

        void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            var r = contentRect;
            HudDraw.Fill(p, 0, 0, r.width, r.height, new Color(.21f, .28f, .2f));
            HudDraw.Fill(p, 69f / 128 * r.width, 0, 8f / 128 * r.width, r.height, new Color(.25f, .46f, .48f));
            foreach (var u in Unit.All)
            {
                if (!u)
                    continue;
                float x = (u.transform.position.x + 64) / 128 * r.width,
                    y = (48 - u.transform.position.z) / 96 * r.height;
                HudDraw.Fill(
                    p,
                    x - 2,
                    y - 2,
                    4,
                    4,
                    u.faction == Faction.Player ? new Color(.55f, .9f, .76f)
                        : u.faction == Faction.Enemy ? new Color(.9f, .35f, .22f)
                        : Color.gray
                );
            }
            foreach (var s in Object.FindObjectsByType<ResourceStore>())
            {
                float x = (s.transform.position.x + 64) / 128 * r.width,
                    y = (48 - s.transform.position.z) / 96 * r.height;
                HudDraw.Fill(p, x - 4, y - 4, 8, 8, s.owner == Faction.Player ? HudDraw.Gold : Color.white);
            }
        }
    }
}
