using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    // In-match HUD on UI Toolkit (Hud.uxml). Static labels are Loc keys in UXML; values refresh every frame.
    // shortcut: simulation strings (log messages, worker/caravan states, placement reasons, place names) stay English,
    // localize them when those systems get message keys.
    [RequireComponent(typeof(UIDocument))]
    public class Hud : MonoBehaviour
    {
        public static Hud I { get; private set; }

        // One button of the command card. Visible commands fill the card in list order; the hotkey works while visible.
        class Command
        {
            public string id;
            public KeyCode key;
            public int wood,
                stone,
                food;
            public Func<bool> visible,
                on;
            public Action run;
            public Button button;
        }

        VisualElement root, hudRoot, helpPanel, objectivesPanel, placementBanner, pausedBanner, dragRect;
        VisualElement tooltip, tooltipCost, objectiveFill, scentFill, scentMark, scentMeter, wolfThreat, hostThreat;
        VisualElement singlePanel, multiPanel, idlePanel, portrait, healthBar, healthFill, storeStock, unitGrid;
        VisualElement commandCard, caravanControls, cargoFill, announce, announceIcon, hoverTip, hoverBar, hoverFill;
        Label announceTitle, announceText, hoverTitle, hoverMeta;
        float announcedAt = -99,
            lastAttackWarning = -99,
            lastVoiceAt = -99;
        string cursor;
        Label woodValue, stoneValue, foodValue, armyValue, workerValue, clockLabel, objectiveCount;
        Label packLabel, scentLabel, hostLabel, hostDetail, placementText, tooltipTitle, tooltipKey, tooltipText;
        Label selectionTitle, selectionMeta, healthText, selectionState, selectionDetail, storeWood, storeStone, storeFood;
        Label multiTitle, cardTitle, caravanState, caravanRoute;
        Button pauseButton, routeToggleButton, cargoButton, objectivesButton, helpButton;
        Button[] speedButtons;
        HudOverlay overlay;
        HudMinimap minimap;
        readonly List<VisualElement> objectiveRows = new List<VisualElement>();
        readonly List<(VisualElement row, Label time, Label text)> logRows = new List<(VisualElement, Label, Label)>();
        LogEntry[] shownLog = new LogEntry[GameSession.LogSize];
        LogEntry lastHeard;
        VisualElement uiTip;
        Label uiTipText;
        string tipKey;
        float tipSince;
        ResourceStore lastStore;
        readonly int[] lastStock = new int[3];
        Label[] stockLabels;
        readonly List<(Label label, float born)> floaters = new List<(Label, float)>();
        readonly List<(Label label, Vector3 at, float born)> hits = new List<(Label, Vector3, float)>();

        // Stinger per announcement title (Resources/Audio, tools/audio_gen.py).
        static readonly Dictionary<string, string> stingers = new Dictionary<string, string>
        {
            ["ann.raid"] = "sting_raid",
            ["ann.wolves"] = "sting_wolves",
            ["ann.captured"] = "sting_capture",
            ["ann.lost"] = "sting_lost",
            ["ann.delivery"] = "sting_delivery",
            ["ann.wagon"] = "sting_wagon",
        };

        // Unit under the pointer, ringed by HudOverlay.
        public static Unit HoverUnit;
        VisualElement nextObjective;
        Label nextHint;

        // Ground markers for right-click orders, drawn by HudOverlay.
        public static readonly List<(Vector3 at, float time, bool attack)> Orders = new List<(Vector3, float, bool)>();

        public static void Order(Vector3 at, bool attack) => Orders.Add((at, Time.unscaledTime, attack));
        readonly List<(ResourceStore store, Button tab)> storeTabs = new List<(ResourceStore, Button)>();
        readonly List<(Outpost post, VisualElement tag, Label text, VisualElement fill)> postTags =
            new List<(Outpost, VisualElement, Label, VisualElement)>();
        readonly List<(Unit unit, VisualElement fill)> tiles = new List<(Unit, VisualElement)>();
        readonly List<Command> commands = new List<Command>();
        BuildingPlacement placement;
        EcologySystem ecology;
        bool help,
            showObjectives = true,
            hasWorker,
            hasSoldier;
        int selectedCount;

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
            GameSession.Announced -= Announce;
            Unit.Damaged -= OnDamaged;
            HoverUnit = null;
            UiFx.SetCursor(null);
        }

        Label L(string name) => root.Q<Label>(name);

        void Start()
        {
            help = GameSettings.Hints; // settings load after scene objects deserialize
            UiFx.UsePanel(GetComponent<UIDocument>());
            root = GetComponent<UIDocument>().rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            hudRoot = root.Q("hudRoot");
            overlay = root.Q<HudOverlay>("worldLayer");
            minimap = root.Q<HudMinimap>("minimap");
            helpPanel = root.Q("helpPanel");
            objectivesPanel = root.Q("objectivesPanel");
            placementBanner = root.Q("placementBanner");
            pausedBanner = root.Q("pausedBanner");
            dragRect = root.Q("dragRect");
            tooltip = root.Q("tooltip");
            tooltipCost = root.Q("tooltipCost");
            objectiveFill = root.Q("objectiveFill");
            scentFill = root.Q("scentFill");
            scentMark = root.Q("scentMark");
            scentMeter = scentFill.parent;
            wolfThreat = root.Q(className: "icon-unit_wolf").parent;
            hostThreat = root.Q(className: "icon-unit_ashen").parent;
            singlePanel = root.Q("singlePanel");
            multiPanel = root.Q("multiPanel");
            idlePanel = root.Q("idlePanel");
            portrait = root.Q("portrait");
            healthBar = root.Q("healthBar");
            healthFill = root.Q("healthFill");
            storeStock = root.Q("storeStock");
            unitGrid = root.Q("unitGrid");
            commandCard = root.Q("commandCard");
            caravanControls = root.Q("caravanControls");
            cargoFill = root.Q("cargoFill");
            nextObjective = root.Q("nextObjective");
            nextHint = L("nextHint");
            announce = root.Q("announce");
            announceIcon = root.Q("announceIcon");
            announceTitle = L("announceTitle");
            announceText = L("announceText");
            hoverTip = root.Q("hoverTip");
            hoverBar = root.Q("hoverBar");
            hoverFill = root.Q("hoverFill");
            hoverTitle = L("hoverTitle");
            hoverMeta = L("hoverMeta");
            woodValue = L("woodValue");
            stoneValue = L("stoneValue");
            foodValue = L("foodValue");
            armyValue = L("armyValue");
            workerValue = L("workerValue");
            clockLabel = L("clockLabel");
            objectiveCount = L("objectiveCount");
            packLabel = L("packLabel");
            scentLabel = L("scentLabel");
            hostLabel = L("hostLabel");
            hostDetail = L("hostDetail");
            placementText = L("placementText");
            tooltipTitle = L("tooltipTitle");
            tooltipKey = L("tooltipKey");
            tooltipText = L("tooltipText");
            selectionTitle = L("selectionTitle");
            selectionMeta = L("selectionMeta");
            healthText = L("healthText");
            selectionState = L("selectionState");
            selectionDetail = L("selectionDetail");
            storeWood = L("storeWood");
            storeStone = L("storeStone");
            storeFood = L("storeFood");
            multiTitle = L("multiTitle");
            cardTitle = L("cardTitle");
            caravanState = L("caravanState");
            caravanRoute = L("caravanRoute");
            pauseButton = root.Q<Button>("pauseButton");
            routeToggleButton = root.Q<Button>("routeToggleButton");
            cargoButton = root.Q<Button>("cargoButton");
            objectivesButton = root.Q<Button>("objectivesButton");
            helpButton = root.Q<Button>("helpButton");
            speedButtons = new[] { root.Q<Button>("speed1"), root.Q<Button>("speed2"), root.Q<Button>("speed3") };
            placement = FindAnyObjectByType<BuildingPlacement>();
            ecology = FindAnyObjectByType<EcologySystem>();

            var session = GameSession.I;
            var list = root.Q("objectiveList");
            foreach (var key in GameSession.Objectives)
            {
                var row = new VisualElement();
                row.AddToClassList("objective");
                var mark = new VisualElement();
                mark.AddToClassList("objective-mark");
                row.Add(mark);
                var text = new Label("obj." + key);
                text.AddToClassList("hud-small");
                row.Add(text);
                row.tooltip = "obj." + key + ".hint";
                list.Add(row);
                objectiveRows.Add(row);
            }
            var log = root.Q("eventLog");
            for (int i = 0; i < GameSession.LogSize; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("log-entry");
                int slot = i;
                row.RegisterCallback<PointerDownEvent>(e =>
                {
                    e.StopPropagation();
                    var where = shownLog[slot]?.where;
                    var cam = UnityEngine.Camera.main;
                    if (where.HasValue && cam)
                        cam.GetComponent<RtsCamera>().focus = where.Value;
                });
                var time = new Label { pickingMode = PickingMode.Ignore };
                time.AddToClassList("log-time");
                var text = new Label { pickingMode = PickingMode.Ignore };
                text.AddToClassList("log-text");
                row.Add(time);
                row.Add(text);
                log.Add(row);
                logRows.Add((row, time, text));
            }
            var tabs = root.Q("storeTabs");
            var stores = new List<ResourceStore> { session.keep };
            foreach (var post in session.posts)
            {
                stores.Add(post.store);
                AddPostTag(post);
            }
            foreach (var store in stores)
            {
                var tab = new Button();
                tab.AddToClassList("store-tab");
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.AddToClassList("store-dot");
                tab.Add(dot);
                tab.Add(new Label { pickingMode = PickingMode.Ignore });
                var s = store;
                tab.clicked += () =>
                {
                    SelectionController.I.Clear();
                    SelectionController.I.selectedStore = s;
                };
                tabs.Add(tab);
                tab.tooltip = "tip.tab";
                storeTabs.Add((store, tab));
            }

            BuildCommands();
            Wire();
            uiTip = root.Q("uiTip");
            uiTipText = L("uiTipText");
            stockLabels = new[] { woodValue, stoneValue, foodValue };
            UiFx.Decorate(root);
            UiAudio.Wire(root);
            UiAudio.Music("music_match");
            UiAudio.Ambience("amb_frontier");
            Unit.Damaged += OnDamaged;
            minimap.Bake();
            Loc.Apply(root);
            Loc.Changed += Relocalize;
            GameSession.Announced += Announce;
        }

        void Announce(string titleKey, string subtitle, string icon)
        {
            announceTitle.text = Loc.T(titleKey);
            announceText.text = subtitle;
            SetIcon(announceIcon, icon);
            announce.RemoveFromClassList("hidden");
            announcedAt = Time.unscaledTime;
            if (stingers.TryGetValue(titleKey, out var sting))
            {
                UiAudio.Play(sting);
                UiAudio.Voice("vo_" + titleKey.Substring(4), .9f); // ann.raid -> vo_raid
                lastVoiceAt = Time.unscaledTime;
            }
        }

        // Fade in over 0.3 s, hold, fade out over the last 0.8 s of 4.5 s.
        void RefreshAnnounce()
        {
            float age = Time.unscaledTime - announcedAt;
            Show(announce, age < 4.5f);
            announce.style.opacity = Mathf.Min(Mathf.Clamp01(age / .3f), Mathf.Clamp01((4.5f - age) / .8f));
            announce.style.translate = new Translate(0, Mathf.Lerp(-14, 0, Mathf.Clamp01(age / .3f)));
        }

        // "+40" / "-50" drifting under a top-bar stock value when the selected store changes.
        void Float(Label value, int delta)
        {
            var f = new Label((delta > 0 ? "+" : "") + delta) { pickingMode = PickingMode.Ignore };
            f.AddToClassList("res-delta");
            f.AddToClassList(delta > 0 ? "res-delta--gain" : "res-delta--loss");
            value.parent.Add(f);
            floaters.Add((f, Time.unscaledTime));
        }

        void OnDamaged(Unit unit, float amount)
        {
            if (!unit)
                return;
            // advisor warning when our people are hit, at most every 30 s, with a minimap ping
            if (unit.faction == Faction.Player && GameSession.I.playTime > 3 && Time.unscaledTime - lastAttackWarning > 30)
            {
                lastAttackWarning = Time.unscaledTime;
                lastVoiceAt = Time.unscaledTime;
                UiAudio.Voice("vo_attack");
                GameSession.I.Notify(Loc.T("n.attack"), true, unit.transform.position);
            }
            if (!GameSettings.DamageNumbers || hits.Count > 40)
                return;
            var l = new Label(Mathf.RoundToInt(amount).ToString()) { pickingMode = PickingMode.Ignore };
            l.AddToClassList("hit");
            l.AddToClassList(unit.faction == Faction.Player ? "hit--ours" : "hit--theirs");
            overlay.Add(l);
            hits.Add((l, unit.transform.position + new Vector3(UnityEngine.Random.Range(-.5f, .5f), 0, 0), Time.unscaledTime));
        }

        void RefreshFloaters()
        {
            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                var (label, born) = floaters[i];
                float t = (Time.unscaledTime - born) / 1.4f;
                if (t >= 1)
                {
                    label.RemoveFromHierarchy();
                    floaters.RemoveAt(i);
                    continue;
                }
                label.style.top = 30 + t * 16;
                label.style.opacity = 1 - t * t;
            }
            var cam = UnityEngine.Camera.main;
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                var (label, at, born) = hits[i];
                float t = (Time.unscaledTime - born) / .9f;
                if (t >= 1 || !cam)
                {
                    label.RemoveFromHierarchy();
                    hits.RemoveAt(i);
                    continue;
                }
                var p = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, at + Vector3.up * (3.2f + t * 1.6f), cam);
                label.style.left = p.x - 12;
                label.style.top = p.y;
                label.style.opacity = 1 - t * t;
                label.style.scale = new Scale(Vector2.one * (t < .12f ? 1.35f - t * 3 : 1));
            }
        }

        // Plain-text hint for any element with a tooltip key, after the pointer rests on it for a moment.
        void RefreshTip()
        {
            var p = PointerPanel();
            string key = null;
            for (var e = root.panel.Pick(p); e != null && key == null; e = e.parent)
                if (!string.IsNullOrEmpty(e.tooltip))
                    key = e.tooltip;
            if (key != tipKey)
            {
                tipKey = key;
                tipSince = Time.unscaledTime;
            }
            bool show = key != null && Time.unscaledTime - tipSince > .35f;
            Show(uiTip, show);
            if (!show)
                return;
            uiTipText.text = Loc.T(key);
            float w = uiTip.layout.width > 0 ? uiTip.layout.width : 260;
            float h = uiTip.layout.height > 0 ? uiTip.layout.height : 40;
            var size = root.layout.size;
            uiTip.style.left = Mathf.Min(p.x + 16, size.x - w - 8);
            uiTip.style.top = p.y + 24 + h > size.y ? p.y - h - 10 : p.y + 24;
        }

        void SetCursor(string name)
        {
            if (cursor == name)
                return;
            cursor = name;
            UiFx.SetCursor(name);
        }

        // Name, owner and health of whatever the pointer is over in the world, and the matching cursor.
        void RefreshHover()
        {
            var cam = UnityEngine.Camera.main;
            string kind = "default";
            bool show = false;
            HoverUnit = null;
            if (cam && !placement.active && !OverUI() && Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit, 300))
            {
                var unit = hit.collider.GetComponentInParent<Unit>();
                var corpse = hit.collider.GetComponentInParent<Corpse>();
                var node = hit.collider.GetComponentInParent<ResourceNode>();
                var store = hit.collider.GetComponentInParent<ResourceStore>();
                float health = -1;
                show = true;
                if (unit)
                {
                    HoverUnit = unit;
                    hoverTitle.text = Loc.Name(unit.title);
                    hoverMeta.text = Loc.T("kind." + unit.kind) + " · " + Loc.T("faction." + unit.faction);
                    health = unit.health / unit.maxHealth;
                    hoverFill.EnableInClassList("hover-fill--foe", unit.faction != Faction.Player);
                    if (SelectionController.I.selected.Exists(s => s && s.GetComponent<CombatAgent>() && s.IsHostile(unit)))
                        kind = "attack";
                }
                else if (corpse)
                {
                    hoverTitle.text = Loc.T("hover.remains");
                    hoverMeta.text = Loc.T("remains." + corpse.state);
                    if (hasWorker)
                        kind = "interact";
                }
                else if (node)
                {
                    hoverTitle.text = Loc.T("res." + node.resource);
                    hoverMeta.text = string.Format(Loc.T("hover.left"), node.remaining);
                    if (hasWorker)
                        kind = "interact";
                }
                else if (store)
                {
                    hoverTitle.text = Loc.Name(store.title);
                    hoverMeta.text = Loc.T(store == GameSession.I.keep ? "store.keep" : "store.post") + " · " + Loc.T("faction." + store.owner);
                }
                else
                    show = false;
                Show(hoverBar, health >= 0);
                if (health >= 0)
                    hoverFill.style.width = Length.Percent(health * 100);
            }
            Show(hoverTip, show);
            if (show)
            {
                var p = PointerPanel();
                hoverTip.style.left = p.x + 20;
                hoverTip.style.top = p.y + 22;
            }
            SetCursor(kind);
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

        void Add(string id, KeyCode key, Func<bool> visible, Action run, Func<bool> on = null, int wood = 0, int stone = 0, int food = 0)
        {
            var c = new Command
            {
                id = id,
                key = key,
                visible = visible,
                run = run,
                on = on,
                wood = wood,
                stone = stone,
                food = food,
            };
            c.button = new Button(() => c.run());
            c.button.AddToClassList("cmd");
            c.button.AddToClassList("icon-cmd_" + id);
            var badge = new Label(key.ToString()) { pickingMode = PickingMode.Ignore };
            badge.AddToClassList("cmd-key");
            c.button.Add(badge);
            c.button.RegisterCallback<PointerEnterEvent>(_ => ShowTip(c));
            c.button.RegisterCallback<PointerLeaveEvent>(_ => tooltip.AddToClassList("hidden"));
            commandCard.Add(c.button);
            commands.Add(c);
        }

        void BuildCommands()
        {
            var select = SelectionController.I;
            var session = GameSession.I;
            Func<bool> worker = () => hasWorker;
            Add("bury", KeyCode.Z, worker, () => select.corpseAction = CorpseAction.Bury, () => select.corpseAction == CorpseAction.Bury);
            Add("burn", KeyCode.X, worker, () => select.corpseAction = CorpseAction.Burn, () => select.corpseAction == CorpseAction.Burn);
            Add("hide", KeyCode.C, worker, () => select.corpseAction = CorpseAction.Hide, () => select.corpseAction == CorpseAction.Hide);
            Add("carry", KeyCode.V, worker, () => select.corpseAction = CorpseAction.Carry, () => select.corpseAction == CorpseAction.Carry);
            Add("drop", KeyCode.G, worker, DropBodies);
            Add("escort", KeyCode.F, () => hasSoldier, Escort);
            Add("halt", KeyCode.H, () => selectedCount > 0, Halt);
            Add("build", KeyCode.B, () => true, () => placement.Begin(), null, 50, 20);
            Add("recruit", KeyCode.R, () => !hasWorker, Recruit, null, 20, 0, 25);
            Add("crew", KeyCode.T, () => selectedCount == 0, AssignCrew);
            Add("raid", KeyCode.N, () => selectedCount == 0 && !session.raidCalled, session.Raid);
        }

        ResourceStore ActiveStore => SelectionController.I.selectedStore ? SelectionController.I.selectedStore : GameSession.I.keep;

        void ShowTip(Command c)
        {
            tooltipTitle.text = Loc.T("cmd." + c.id);
            tooltipKey.text = c.key.ToString();
            tooltipText.text = Loc.T("cmd." + c.id + ".desc");
            tooltipCost.Clear();
            var store = ActiveStore;
            AddCost("res_wood", c.wood, store.wood);
            AddCost("res_stone", c.stone, store.stone);
            AddCost("res_food", c.food, store.food);
            Show(tooltipCost, c.wood + c.stone + c.food > 0);
            var card = commandCard.worldBound;
            tooltip.style.left = card.x;
            tooltip.style.bottom = 208;
            tooltip.RemoveFromClassList("hidden");
        }

        void AddCost(string icon, int cost, int have)
        {
            if (cost <= 0)
                return;
            var e = new VisualElement();
            e.AddToClassList("cost");
            e.EnableInClassList("cost--short", have < cost);
            var i = new VisualElement();
            i.AddToClassList("cost-icon");
            i.AddToClassList("icon-" + icon);
            e.Add(i);
            e.Add(new Label(cost.ToString()));
            tooltipCost.Add(e);
        }

        void On(string name, Action action) => root.Q<Button>(name).clicked += action;

        void Wire()
        {
            var session = GameSession.I;
            var select = SelectionController.I;
            On("pauseButton", session.Pause);
            for (int i = 0; i < speedButtons.Length; i++)
            {
                int speed = i + 1;
                speedButtons[i].clicked += () => session.SetSpeed(speed);
            }
            On(
                "menuButton",
                () =>
                {
                    if (PauseMenu.I)
                        PauseMenu.I.Open();
                }
            );
            On("helpButton", () => help = !help);
            On("objectivesButton", () => showObjectives = !showObjectives);
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

        static string WorkerState(WorkerAgent w) =>
            Loc.F(w.state, w.carried, Loc.T("res." + w.cargo), w.node ? Loc.T("res." + w.node.resource) : "");

        static string RouteState(TradeRoute r) => Loc.F(r.state, Loc.T("res." + r.resource));

        static void Show(VisualElement e, bool visible) => e.EnableInClassList("hidden", !visible);

        static string PortraitOf(Unit u) =>
            u.kind == UnitKind.Wagon ? "unit_wagon"
            : u.kind == UnitKind.Wolf ? "unit_wolf"
            : u.kind == UnitKind.Worker ? "unit_worker"
            : u.faction == Faction.Enemy ? "unit_ashen"
            : "unit_swordsman";

        // Swaps an "icon-*" class; the current key is kept in userData (only on non-text elements, see Loc.Apply).
        static void SetIcon(VisualElement e, string key)
        {
            if (e.userData as string == key)
                return;
            if (e.userData is string old)
                e.RemoveFromClassList("icon-" + old);
            e.userData = key;
            e.AddToClassList("icon-" + key);
        }

        static void SetOwnerClass(VisualElement e, string prefix, Faction owner)
        {
            foreach (Faction f in Enum.GetValues(typeof(Faction)))
                e.EnableInClassList(prefix + f, f == owner);
        }

        void Update()
        {
            var session = GameSession.I;
            var select = SelectionController.I;
            if (root?.panel == null || !session || !select)
                return;
            UiFx.Fit();
            // narrow logical widths (large UI scale, 4:3 screens) drop the brand text and unit counts
            hudRoot.EnableInClassList("hud--compact", hudRoot.layout.width < 1500);
            Show(hudRoot, !PauseMenu.IsOpen);
            if (PauseMenu.IsOpen)
                return;

            var sel = select.selected;
            selectedCount = sel.Count;
            hasWorker = sel.Exists(u => u && u.kind == UnitKind.Worker);
            hasSoldier = sel.Exists(u => u && u.kind == UnitKind.Swordsman);

            bool paused = Time.timeScale == 0;
            if (!paused && !placement.active)
                foreach (var c in commands)
                    if (c.visible() && Input.GetKeyDown(c.key))
                    {
                        UiAudio.Play("ui_click");
                        c.run();
                    }

            RefreshTop(session, paused);
            RefreshLog(session);
            RefreshSide(session);
            Show(helpPanel, help);
            helpButton.EnableInClassList("hud-button--on", help);
            Show(pausedBanner, paused);
            Show(placementBanner, placement.active);
            if (placement.active)
                placementText.text = placement.reason + " · " + Loc.T("hud.place.cancel");

            RefreshAnnounce();
            RefreshHover();
            RefreshTip();
            RefreshFloaters();
            RefreshSelection(select);
            RefreshCommands();
            RefreshCaravan();
            RefreshWorld();
        }

        void RefreshTop(GameSession session, bool paused)
        {
            var store = ActiveStore;
            var stock = new[] { store.wood, store.stone, store.food };
            for (int i = 0; i < 3; i++)
            {
                if (store == lastStore && stock[i] != lastStock[i])
                    Float(stockLabels[i], stock[i] - lastStock[i]);
                lastStock[i] = stock[i];
                stockLabels[i].text = stock[i].ToString();
            }
            lastStore = store;
            int army = 0,
                workers = 0;
            foreach (var u in Unit.All)
                if (u.faction == Faction.Player)
                {
                    army += u.kind == UnitKind.Swordsman ? 1 : 0;
                    workers += u.kind == UnitKind.Worker ? 1 : 0;
                }
            armyValue.text = army.ToString();
            workerValue.text = workers.ToString();
            foreach (var (s, tab) in storeTabs)
            {
                tab.EnableInClassList("store-tab--active", s == store);
                tab.Q<Label>().text = Loc.Name(s.title);
                SetOwnerClass(tab, "store-tab--", s.owner);
            }
            clockLabel.text = Loc.Clock(session.playTime);
            pauseButton.EnableInClassList("speed-button--on", paused);
            for (int i = 0; i < speedButtons.Length; i++)
                speedButtons[i].EnableInClassList("speed-button--on", !paused && Mathf.Approximately(session.speed, i + 1));
        }

        void RefreshLog(GameSession session)
        {
            var log = session.log;
            var newest = log.Count > 0 ? log[log.Count - 1] : null;
            if (newest != lastHeard)
            {
                if (newest != null && newest.alert && lastHeard != null && Time.unscaledTime - Mathf.Max(announcedAt, lastVoiceAt) > .5f)
                    UiAudio.Play("ui_alert");
                lastHeard = newest;
            }
            for (int i = 0; i < logRows.Count; i++)
            {
                var (row, time, text) = logRows[i];
                int index = log.Count - 1 - i;
                float age = index >= 0 ? Time.unscaledTime - log[index].at : 99;
                Show(row, age < 12);
                if (age >= 12)
                    continue;
                var entry = log[index];
                shownLog[i] = entry;
                row.EnableInClassList("log-entry--link", entry.where.HasValue);
                row.tooltip = entry.where.HasValue ? "tip.log" : null;
                time.text = Loc.Clock(entry.clock);
                text.text = entry.text;
                row.EnableInClassList("log-entry--alert", entry.alert);
                row.style.opacity = Mathf.Clamp01((12 - age) / 2);
            }
        }

        void RefreshSide(GameSession session)
        {
            Show(objectivesPanel, showObjectives);
            objectivesButton.EnableInClassList("hud-button--on", showObjectives);
            int done = 0,
                next = -1;
            for (int i = 0; i < GameSession.Objectives.Length; i++)
            {
                bool d = session.milestones.Contains(GameSession.Objectives[i]);
                done += d ? 1 : 0;
                if (!d && next < 0)
                    next = i;
                objectiveRows[i].EnableInClassList("objective--done", d);
                objectiveRows[i].EnableInClassList("objective--next", i == next);
            }
            Show(nextObjective, next >= 0);
            if (next >= 0)
                nextHint.text = Loc.T("obj." + GameSession.Objectives[next] + ".hint");
            objectiveCount.text = done + " / " + GameSession.Objectives.Length;
            objectiveFill.style.width = Length.Percent(100f * done / GameSession.Objectives.Length);

            float threshold = session.config.remainsThreshold * .65f;
            float scale = Mathf.Max(threshold * 2, .01f);
            scentFill.style.width = Length.Percent(Mathf.Clamp01(ecology.scent / scale) * 100);
            scentMark.style.left = Length.Percent(threshold / scale * 100);
            scentMeter.EnableInClassList("hud-meter--danger", ecology.wolvesHunting);
            wolfThreat.EnableInClassList("threat--active", ecology.wolvesHunting);
            packLabel.text = Loc.T(ecology.wolvesHunting ? "hud.pack.hunt" : "hud.pack.den");
            scentLabel.text = Loc.T("hud.scent") + " " + ecology.scent.ToString("0.0") + " / " + threshold.ToString("0.0");
            int host = 0;
            foreach (var u in Unit.All)
                host += u.faction == Faction.Enemy ? 1 : 0;
            hostThreat.EnableInClassList("threat--active", host > 0);
            hostLabel.text = string.Format(Loc.T("hud.host"), host);
            hostDetail.text = Loc.T(host == 0 ? "hud.host.none" : session.raidCalled ? "hud.host.raid" : "hud.host.bridge");
        }

        void RefreshSelection(SelectionController select)
        {
            var sel = select.selected;
            var u = sel.Count > 0 ? sel[0] : null;
            var store = sel.Count == 0 ? select.selectedStore : null;
            Show(singlePanel, sel.Count == 1 || store);
            Show(multiPanel, sel.Count > 1);
            Show(idlePanel, sel.Count == 0 && !store);
            cardTitle.text = Loc.T("hud.commands");

            if (sel.Count > 1)
            {
                multiTitle.text = string.Format(Loc.T("hud.selected"), sel.Count);
                bool same = tiles.Count == sel.Count;
                for (int i = 0; same && i < sel.Count; i++)
                    same = tiles[i].unit == sel[i];
                if (!same)
                    RebuildTiles(sel);
                foreach (var (unit, fill) in tiles)
                    if (unit)
                        fill.style.width = Length.Percent(100 * unit.health / unit.maxHealth);
                return;
            }
            if (store)
            {
                SetIcon(portrait, "unit_store");
                selectionTitle.text = Loc.Name(store.title);
                selectionMeta.text = (
                    Loc.T(store == GameSession.I.keep ? "store.keep" : "store.post")
                    + " · "
                    + Loc.T("faction." + store.owner)
                ).ToUpper();
                Show(healthBar, false);
                Show(storeStock, true);
                storeWood.text = store.wood.ToString();
                storeStone.text = store.stone.ToString();
                storeFood.text = store.food.ToString();
                selectionState.text = string.Format(Loc.T("store.radius"), Mathf.RoundToInt(store.radius));
                selectionDetail.text = Loc.T("store.hint");
                return;
            }
            if (!u)
                return;
            SetIcon(portrait, PortraitOf(u));
            Show(healthBar, true);
            Show(storeStock, false);
            selectionTitle.text = Loc.Name(u.title);
            selectionMeta.text = (Loc.T("kind." + u.kind) + " · " + Loc.T("faction." + u.faction)).ToUpper();
            float hp = u.health / u.maxHealth;
            healthFill.style.width = Length.Percent(hp * 100);
            healthBar.EnableInClassList("hud-health--low", hp < .35f);
            healthText.text = Mathf.CeilToInt(u.health) + " / " + u.maxHealth;
            var worker = u.GetComponent<WorkerAgent>();
            var route = u.GetComponent<TradeRoute>();
            var fighter = u.GetComponent<CombatAgent>();
            var escort = u.GetComponent<EscortOrder>();
            if (worker)
            {
                selectionState.text = WorkerState(worker);
                selectionDetail.text =
                    Loc.T("hud.cargo") + ": " + worker.carried + " " + Loc.T("res." + worker.cargo)
                    + "  ·  " + Loc.T("hud.rmb.corpse") + " " + Loc.T("corpse." + select.corpseAction);
            }
            else if (route)
            {
                selectionState.text = RouteState(route);
                selectionDetail.text = Loc.T("hud.cargo") + ": " + route.cargo + " / " + GameSession.I.config.wagonCapacity;
            }
            else
            {
                selectionState.text =
                    fighter && fighter.target ? Loc.T("state.fight") + " " + Loc.Name(fighter.target.title)
                    : escort && escort.wagon ? Loc.T("state.escort")
                    : u.Motor.Arrived ? Loc.T("state.idle")
                    : Loc.T("state.move");
                selectionDetail.text = Loc.T("hud.sword.hint");
            }
        }

        void RebuildTiles(List<Unit> sel)
        {
            unitGrid.Clear();
            tiles.Clear();
            foreach (var unit in sel)
            {
                var tile = new VisualElement();
                tile.AddToClassList("unit-tile");
                tile.AddToClassList("icon-" + PortraitOf(unit));
                var bar = new VisualElement { pickingMode = PickingMode.Ignore };
                bar.AddToClassList("tile-bar");
                var fill = new VisualElement { pickingMode = PickingMode.Ignore };
                fill.AddToClassList("tile-fill");
                bar.Add(fill);
                tile.Add(bar);
                var u = unit;
                tile.RegisterCallback<PointerDownEvent>(e =>
                {
                    e.StopPropagation();
                    if (!u)
                        return;
                    SelectionController.I.Clear();
                    SelectionController.I.Select(u);
                });
                unitGrid.Add(tile);
                tiles.Add((unit, fill));
            }
        }

        void RefreshCommands()
        {
            var store = ActiveStore;
            foreach (var c in commands)
            {
                Show(c.button, c.visible());
                c.button.EnableInClassList("cmd--active", c.on != null && c.on());
                c.button.EnableInClassList("cmd--dim", store.wood < c.wood || store.stone < c.stone || store.food < c.food);
            }
            if (!tooltip.ClassListContains("hidden") && !commandCard.worldBound.Contains(PointerPanel()))
                tooltip.AddToClassList("hidden");
        }

        Vector2 PointerPanel() =>
            RuntimePanelUtils.ScreenToPanel(
                root.panel,
                new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)
            );

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
            int capacity = GameSession.I.config.wagonCapacity;
            caravanState.text =
                RouteState(route) + " · " + route.cargo + " / " + capacity + " "
                + Loc.T("res." + (route.cargo > 0 ? route.carriedResource : route.resource));
            caravanRoute.text =
                (route.source ? Loc.Name(route.source.title) : Loc.T("hud.no.source"))
                + " › "
                + (route.destination ? Loc.Name(route.destination.title) : Loc.T("hud.no.destination"));
            cargoFill.style.width = Length.Percent(100f * route.cargo / capacity);
            routeToggleButton.text = Loc.T(route.paused ? "hud.resume" : "hud.stop");
            cargoButton.text = Loc.T("hud.cargo") + ": " + Loc.T("res." + route.resource);
        }

        void RefreshWorld()
        {
            var cam = UnityEngine.Camera.main;
            var panel = root.panel;
            foreach (var (post, tag, text, fill) in postTags)
            {
                var world = post.transform.position + Vector3.up * 6;
                Show(tag, cam.WorldToScreenPoint(world).z > 0);
                var p = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, cam);
                tag.style.left = p.x - 75;
                tag.style.top = p.y;
                text.text = Loc.Name(post.store.title) + " · " + Loc.T("faction." + post.store.owner);
                fill.style.width = Length.Percent(post.progress * 100);
                tag.EnableInClassList("post-tag--contested", post.contested);
                SetOwnerClass(tag, "post-tag--", post.store.owner);
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

        void DropBodies()
        {
            foreach (var u in SelectionController.I.selected)
            {
                var worker = u ? u.GetComponent<WorkerAgent>() : null;
                if (worker)
                    worker.DropBody();
            }
        }

        void Halt()
        {
            foreach (var u in SelectionController.I.selected)
            {
                if (!u)
                    continue;
                var fighter = u.GetComponent<CombatAgent>();
                if (fighter)
                    fighter.ClearTarget();
                var escort = u.GetComponent<EscortOrder>();
                if (escort)
                    escort.wagon = null;
                var worker = u.GetComponent<WorkerAgent>();
                if (worker)
                    worker.Cancel();
                var trade = u.GetComponent<TradeRoute>();
                if (trade)
                    trade.paused = true;
                u.Motor.Stop();
            }
        }

        void Escort()
        {
            var wagon = Caravan();
            if (!wagon)
                return;
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
            var store = ActiveStore;
            if (store.owner == Faction.Player && store.Spend(20, 0, 25))
            {
                WorldBuilder.SpawnUnit(UnitKind.Swordsman, Faction.Player, store.Dock, "Crown swordsman");
                GameSession.I.stats.recruited++;
            }
            else
                GameSession.I.Notify(Loc.T("hud.n.recruit"), true);
        }

        void Route(int index)
        {
            var route = Caravan();
            if (route && !route.SetRoute(GameSession.I.posts[index].store, GameSession.I.keep))
                GameSession.I.Notify(Loc.T("hud.n.route.capture"), true);
        }

        void AssignCrew()
        {
            var post = GameSession.I.posts[0];
            if (post.store.owner != Faction.Player)
            {
                GameSession.I.Notify(Loc.T("hud.n.crew.capture"), true);
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

        // Circle on the ground, projected point by point so it matches the camera's tilt.
        public static void WorldRing(Painter2D p, VisualElement e, UnityEngine.Camera cam, Vector3 at, float radius, Color c, float width)
        {
            p.strokeColor = c;
            p.lineWidth = width;
            p.BeginPath();
            for (int i = 0; i <= 24; i++)
            {
                float a = i * Mathf.PI * 2 / 24;
                var w = at + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                var s = RuntimePanelUtils.CameraTransformWorldToPanel(e.panel, w, cam);
                if (i == 0)
                    p.MoveTo(s);
                else
                    p.LineTo(s);
            }
            p.Stroke();
        }

        public static void Disc(Painter2D p, Vector2 center, float radius, Color c)
        {
            p.fillColor = c;
            p.BeginPath();
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2 / 10;
                var v = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                if (i == 0)
                    p.MoveTo(v);
                else
                    p.LineTo(v);
            }
            p.ClosePath();
            p.Fill();
        }

        public static void Ring(Painter2D p, Vector2 center, float radius, Color c, float width)
        {
            p.strokeColor = c;
            p.lineWidth = width;
            p.BeginPath();
            for (int i = 0; i <= 20; i++)
            {
                float a = i * Mathf.PI * 2 / 20;
                var v = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                if (i == 0)
                    p.MoveTo(v);
                else
                    p.LineTo(v);
            }
            p.Stroke();
        }

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
            Hud.Orders.RemoveAll(o => Time.unscaledTime - o.time > .6f);
            foreach (var (at, time, attack) in Hud.Orders)
            {
                float t = (Time.unscaledTime - time) / .6f;
                var c = attack ? new Color(.95f, .35f, .22f) : new Color(.55f, .9f, .7f);
                c.a = 1 - t;
                HudDraw.WorldRing(p, this, cam, at, Mathf.Lerp(1.8f, .5f, t), c, 2);
            }
            foreach (var u in Unit.All)
            {
                if (!u)
                    continue;
                bool hovered = u == Hud.HoverUnit;
                if (u.selected || hovered)
                {
                    var ring = u.selected ? new Color(.86f, .72f, .42f, .95f)
                        : u.faction == Faction.Player ? new Color(.85f, .85f, .8f, .6f)
                        : new Color(.95f, .35f, .22f, .8f);
                    HudDraw.WorldRing(p, this, cam, u.transform.position + Vector3.up * .05f, u.kind == UnitKind.Wagon ? 1.7f : .85f, ring, u.selected ? 1.8f : 1.3f);
                }
                if (!(u.selected || hovered || u.health < u.maxHealth || GameSettings.AlwaysHealth))
                    continue;
                var world = u.transform.position + Vector3.up * 2.8f;
                if (cam.WorldToScreenPoint(world).z <= 0)
                    continue;
                var s = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, cam);
                if (s.y < 60 || s.y > 690)
                    continue;
                float hp = Mathf.Clamp01(u.health / u.maxHealth);
                var fill = u.faction == Faction.Player
                    ? Color.Lerp(new Color(.86f, .3f, .2f), new Color(.37f, .78f, .56f), Mathf.InverseLerp(.2f, .7f, hp))
                    : new Color(.88f, .3f, .2f);
                HudDraw.Fill(p, s.x - 21, s.y - 1, 42, 7, new Color(0, 0, 0, .85f));
                HudDraw.Fill(p, s.x - 20, s.y, 40, 5, new Color(.18f, .08f, .07f));
                HudDraw.Fill(p, s.x - 20, s.y, 40 * hp, 5, fill);
                HudDraw.Fill(p, s.x - 20, s.y, 40 * hp, 1.5f, new Color(1, 1, 1, .25f));
                // a tick every quarter so damage reads at a glance
                for (int q = 1; q < 4; q++)
                    HudDraw.Fill(p, s.x - 20 + q * 10, s.y, 1, 5, new Color(0, 0, 0, .55f));
            }
        }
    }

    // Top-down map of the 128x96 world with the camera's view outline; clicking moves the camera focus there.
    [UxmlElement]
    public partial class HudMinimap : VisualElement
    {
        static readonly Color Ground = new Color(.2f, .26f, .18f),
            Forest = new Color(.14f, .21f, .14f),
            River = new Color(.22f, .42f, .44f),
            Road = new Color(.42f, .37f, .26f),
            Crown = new Color(.55f, .9f, .76f),
            Ashen = new Color(.95f, .38f, .25f),
            Wild = new Color(.75f, .75f, .7f),
            Remains = new Color(.45f, .12f, .1f);

        bool baked;

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

        // Renders the 128 x 96 m world once from straight above, without units or fog, as the map background.
        public void Bake()
        {
            var go = new GameObject("Minimap camera");
            var cam = go.AddComponent<UnityEngine.Camera>();
            cam.enabled = false;
            cam.orthographic = true;
            cam.orthographicSize = 48;
            cam.aspect = 128f / 96;
            cam.nearClipPlane = 1;
            cam.farClipPlane = 200;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Ground;
            go.transform.SetPositionAndRotation(new Vector3(0, 120, 0), Quaternion.Euler(90, 0, 0));
            var rt = new RenderTexture(1024, 768, 24);
            cam.targetTexture = rt;
            var hidden = new List<Renderer>();
            foreach (var u in Unit.All)
                foreach (var r in u.GetComponentsInChildren<Renderer>())
                    if (r.enabled)
                    {
                        r.enabled = false;
                        hidden.Add(r);
                    }
            bool fog = RenderSettings.fog;
            RenderSettings.fog = false;
            try
            {
                cam.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                style.backgroundImage = Background.FromTexture2D(tex);
                baked = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Minimap bake failed, using the drawn map: " + e.Message);
            }
            finally
            {
                RenderSettings.fog = fog;
                foreach (var r in hidden)
                    r.enabled = true;
                cam.targetTexture = null;
                rt.Release();
                UnityEngine.Object.Destroy(go);
            }
        }

        Vector2 Map(Vector3 world)
        {
            var r = contentRect;
            return new Vector2((world.x + 64) / 128 * r.width, (48 - world.z) / 96 * r.height);
        }

        void Dot(Painter2D p, Vector3 world, float size, Color c)
        {
            var m = Map(world);
            HudDraw.Fill(p, m.x - size / 2, m.y - size / 2, size, size, c);
        }

        void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            var r = contentRect;
            if (!baked)
                DrawTerrain(p, r);
            DrawMarkers(p);
            DrawView(p);
        }

        // Fallback terrain from WorldBuilder's layout when the bake could not render.
        void DrawTerrain(Painter2D p, Rect r)
        {
            HudDraw.Fill(p, 0, 0, r.width, r.height, Ground);
            var a = Map(new Vector3(-58, 0, 44));
            var b = Map(new Vector3(-25, 0, 19));
            HudDraw.Fill(p, a.x, a.y, b.x - a.x, b.y - a.y, Forest);
            a = Map(new Vector3(20, 0, 44));
            b = Map(new Vector3(57, 0, 27));
            HudDraw.Fill(p, a.x, a.y, b.x - a.x, b.y - a.y, Forest);
            a = Map(new Vector3(-58, 0, -3.5f));
            b = Map(new Vector3(60, 0, -6.5f));
            HudDraw.Fill(p, a.x, a.y, b.x - a.x, b.y - a.y, Road);
            a = Map(new Vector3(5, 0, 48));
            b = Map(new Vector3(13, 0, -48));
            HudDraw.Fill(p, a.x, a.y, b.x - a.x, b.y - a.y, River);
        }

        void DrawMarkers(Painter2D p)
        {
            var session = GameSession.I;
            if (session)
            {
                Dot(p, session.den, 7, new Color(.3f, .3f, .28f));
                foreach (var c in CorpseSystem.I.remains)
                    if (c && (c.state == RemainsState.Exposed || c.state == RemainsState.Hidden))
                        Dot(p, c.transform.position, 3, Remains);
                foreach (var e in session.log)
                {
                    float age = Time.unscaledTime - e.at;
                    if (!e.where.HasValue || age > 5)
                        continue;
                    var col = e.alert ? Ashen : HudDraw.Gold;
                    col.a = 1 - age / 5;
                    HudDraw.Ring(p, Map(e.where.Value), 3 + age % 1 * 12, col, 1.6f);
                }
            }
            foreach (var s in UnityEngine.Object.FindObjectsByType<ResourceStore>())
                Dot(p, s.transform.position, 9, s.owner == Faction.Player ? HudDraw.Gold : s.owner == Faction.Enemy ? Ashen : Color.white);
            foreach (var u in Unit.All)
                if (u)
                    Dot(
                        p,
                        u.transform.position,
                        u.kind == UnitKind.Wagon ? 6 : 4,
                        u.faction == Faction.Player ? Crown
                            : u.faction == Faction.Enemy ? Ashen
                            : Wild
                    );
        }

        // Outline of the ground the camera sees: the four screen corners projected onto y = 0.
        void DrawView(Painter2D p)
        {
            var cam = UnityEngine.Camera.main;
            if (!cam)
                return;
            var plane = new Plane(Vector3.up, Vector3.zero);
            var corners = new Vector2[4];
            var screen = new[] { new Vector3(0, 0), new Vector3(Screen.width, 0), new Vector3(Screen.width, Screen.height), new Vector3(0, Screen.height) };
            for (int i = 0; i < 4; i++)
            {
                var ray = cam.ScreenPointToRay(screen[i]);
                if (!plane.Raycast(ray, out float d))
                    return;
                var m = Map(ray.GetPoint(d));
                corners[i] = new Vector2(Mathf.Clamp(m.x, 0, contentRect.width), Mathf.Clamp(m.y, 0, contentRect.height));
            }
            p.strokeColor = new Color(.95f, .92f, .8f, .85f);
            p.lineWidth = 1.2f;
            p.BeginPath();
            p.MoveTo(corners[0]);
            for (int i = 1; i < 4; i++)
                p.LineTo(corners[i]);
            p.ClosePath();
            p.Stroke();
        }
    }
}
