using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KingOrDeath
{
    [Serializable]
    public class MatchStats
    {
        public int enemiesSlain,
            unitsLost,
            recruited,
            harvested,
            delivered,
            built,
            remainsCleared;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string scenario = "The First Frontier";
        public string label;
        public long savedAtTicks;
        public float playTime;
        public Vector3 cameraFocus;
        public float cameraZoom,
            cameraYaw;
        public bool raidCalled,
            victory;
        public List<string> milestones = new List<string>();
        public MatchStats stats = new MatchStats();
        public List<StoreState> stores = new List<StoreState>();
        public List<PostState> posts = new List<PostState>();
        public List<UnitState> units = new List<UnitState>();
        public List<NodeState> nodes = new List<NodeState>();
        public List<Vector3> houses = new List<Vector3>();
        public List<CorpseState> corpses = new List<CorpseState>();

        public DateTime SavedAt => new DateTime(savedAtTicks);
    }

    [Serializable]
    public class StoreState
    {
        public string title;
        public Faction owner;
        public int wood,
            stone,
            food;
    }

    [Serializable]
    public class PostState
    {
        public float progress;
        public Faction challenger;
    }

    [Serializable]
    public class UnitState
    {
        public UnitKind kind;
        public Faction faction;
        public string title;
        public Vector3 position;
        public float yaw,
            health;
        public Vector3 rally;
        public bool escorting;
        // worker
        public bool gathering;
        public Vector3 node;
        public int carried;
        public ResourceKind carriedKind;
        // wagon
        public string source,
            destination;
        public ResourceKind resource;
        public int cargo;
        public bool paused,
            outbound;
    }

    [Serializable]
    public class NodeState
    {
        public Vector3 position;
        public ResourceKind resource;
        public int remaining;
        public bool salvage;
    }

    [Serializable]
    public class CorpseState
    {
        public UnitKind kind;
        public Faction faction;
        public Vector3 position;
        public float age;
        public RemainsState state;
    }

    // Match snapshots as JSON files in persistentDataPath/saves. Loading reloads the game scene and applies the
    // snapshot from GameSession.Start.
    // shortcut: unit identity is not kept, so attack targets, corpse-cleanup jobs and caravan escorts other than
    // "escort the wagon" restart idle; add stable unit ids when commands need to survive a load.
    public static class SaveSystem
    {
        public const string Auto = "autosave",
            Quick = "quicksave";
        public static SaveData Pending;

        public static string Folder => Path.Combine(Application.persistentDataPath, "saves");

        public class Slot
        {
            public string id;
            public SaveData data;
        }

        public static List<Slot> List()
        {
            var slots = new List<Slot>();
            if (!Directory.Exists(Folder))
                return slots;
            foreach (var file in Directory.GetFiles(Folder, "*.json"))
            {
                try
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(file));
                    if (data != null && data.version == 1)
                        slots.Add(new Slot { id = Path.GetFileNameWithoutExtension(file), data = data });
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Skipping unreadable save " + file + ": " + e.Message);
                }
            }
            return slots.OrderByDescending(s => s.data.savedAtTicks).ToList();
        }

        public static Slot Latest() => List().FirstOrDefault();

        public static string NewId() => "save_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

        public static void Write(string id, SaveData data)
        {
            Directory.CreateDirectory(Folder);
            string path = Path.Combine(Folder, id + ".json"),
                temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data));
            // write-then-swap so a crash mid-write never destroys the previous save
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
        }

        public static void Delete(string id)
        {
            foreach (var ext in new[] { ".json", ".png" })
            {
                string path = Path.Combine(Folder, id + ext);
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public static void Save(string id, string label)
        {
            var data = Capture();
            data.label = label;
            Write(id, data);
            try
            {
                WriteThumbnail(id);
            }
            catch (Exception e)
            {
                // a save without a picture is still a good save
                Debug.LogWarning("Save thumbnail failed: " + e.Message);
            }
        }

        // 384 x 216 view from the game camera, rendered off-screen so no UI ends up in it.
        static void WriteThumbnail(string id)
        {
            var cam = UnityEngine.Camera.main;
            if (!cam)
                return;
            var rt = RenderTexture.GetTemporary(384, 216, 24);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = previous;
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(Path.Combine(Folder, id + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
        }

        // Caller owns (and destroys) the texture; null when the save has no picture.
        public static Texture2D LoadThumbnail(string id)
        {
            string path = Path.Combine(Folder, id + ".png");
            if (!File.Exists(path))
                return null;
            var tex = new Texture2D(2, 2);
            return tex.LoadImage(File.ReadAllBytes(path)) ? tex : null;
        }

        public static void Load(SaveData data)
        {
            Pending = data;
            Time.timeScale = 1;
            SceneManager.LoadScene(MainMenu.GameScene);
        }

        static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0, p.z);

        public static SaveData Capture()
        {
            var g = GameSession.I;
            var data = new SaveData
            {
                savedAtTicks = DateTime.Now.Ticks,
                playTime = g.playTime,
                raidCalled = g.raidCalled,
                victory = g.outcome == Outcome.Victory,
                milestones = g.milestones.ToList(),
                stats = JsonUtility.FromJson<MatchStats>(JsonUtility.ToJson(g.stats)),
            };
            var cam = UnityEngine.Camera.main ? UnityEngine.Camera.main.GetComponent<RtsCamera>() : null;
            if (cam)
            {
                data.cameraFocus = cam.focus;
                data.cameraZoom = cam.zoom;
                data.cameraYaw = cam.yaw;
            }
            foreach (var s in Stores())
                data.stores.Add(new StoreState { title = s.title, owner = s.owner, wood = s.wood, stone = s.stone, food = s.food });
            foreach (var p in g.posts)
                data.posts.Add(new PostState { progress = p.progress, challenger = p.challenger });
            foreach (var u in Unit.All)
            {
                if (!u || u.health <= 0)
                    continue;
                var st = new UnitState
                {
                    kind = u.kind,
                    faction = u.faction,
                    title = u.title,
                    position = Flat(u.transform.position),
                    yaw = u.transform.eulerAngles.y,
                    health = u.health,
                };
                var combat = u.GetComponent<CombatAgent>();
                if (combat)
                    st.rally = combat.rally;
                var escort = u.GetComponent<EscortOrder>();
                st.escorting = escort && escort.wagon;
                var worker = u.GetComponent<WorkerAgent>();
                if (worker)
                {
                    st.gathering = worker.node;
                    st.node = worker.node ? worker.node.transform.position : Vector3.zero;
                    st.carried = worker.carried;
                    st.carriedKind = worker.cargo;
                }
                var trade = u.GetComponent<TradeRoute>();
                if (trade)
                {
                    st.source = trade.source ? trade.source.title : null;
                    st.destination = trade.destination ? trade.destination.title : null;
                    st.resource = trade.resource;
                    st.carriedKind = trade.carriedResource;
                    st.cargo = trade.cargo;
                    st.paused = trade.paused;
                    st.outbound = trade.outbound;
                }
                data.units.Add(st);
            }
            foreach (var n in UnityEngine.Object.FindObjectsByType<ResourceNode>())
                data.nodes.Add(
                    new NodeState
                    {
                        position = n.transform.position,
                        resource = n.resource,
                        remaining = n.remaining,
                        salvage = n.name == "Salvage",
                    }
                );
            foreach (var b in UnityEngine.Object.FindObjectsByType<Building>())
                if (b.name == BuildingPlacement.HouseName)
                    data.houses.Add(b.transform.position);
            foreach (var c in CorpseSystem.I.remains)
            {
                if (!c)
                    continue;
                // a carried body is saved where its bearer stands, as if dropped
                bool carried = c.state == RemainsState.Carried;
                data.corpses.Add(
                    new CorpseState
                    {
                        kind = c.kind,
                        faction = c.faction,
                        position = Flat(c.transform.position),
                        age = Time.time - c.diedAt,
                        state = carried ? RemainsState.Exposed : c.state,
                    }
                );
            }
            return data;
        }

        static ResourceStore[] Stores() => UnityEngine.Object.FindObjectsByType<ResourceStore>();

        // Replaces the freshly built scenario state with the snapshot. Called once from GameSession.Start.
        public static void Apply(SaveData data)
        {
            var g = GameSession.I;
            g.playTime = data.playTime;
            g.raidCalled = data.raidCalled;
            g.outcome = data.victory ? Outcome.Victory : Outcome.Playing; // a won match does not end again on load
            g.milestones = new HashSet<string>(data.milestones);
            g.stats = data.stats ?? new MatchStats();
            var cam = UnityEngine.Camera.main ? UnityEngine.Camera.main.GetComponent<RtsCamera>() : null;
            if (cam && data.cameraZoom > 0)
            {
                cam.focus = data.cameraFocus;
                cam.zoom = data.cameraZoom;
                cam.yaw = data.cameraYaw;
            }

            var stores = Stores();
            foreach (var st in data.stores)
            {
                var s = stores.FirstOrDefault(x => x.title == st.title);
                if (!s)
                    continue;
                s.owner = st.owner;
                s.wood = st.wood;
                s.stone = st.stone;
                s.food = st.food;
            }
            for (int i = 0; i < g.posts.Length && i < data.posts.Count; i++)
            {
                g.posts[i].progress = data.posts[i].progress;
                g.posts[i].challenger = data.posts[i].challenger;
                g.posts[i].ShowOwner();
            }

            var nodes = UnityEngine.Object.FindObjectsByType<ResourceNode>().ToList();
            foreach (var n in data.nodes)
            {
                var match = nodes.FirstOrDefault(x => (x.transform.position - n.position).sqrMagnitude < .01f);
                if (!match && n.salvage)
                {
                    match = ModelFactory.Crate("Salvage", n.position).AddComponent<ResourceNode>();
                    match.resource = n.resource;
                }
                if (match)
                    match.remaining = n.remaining;
            }
            nodes = UnityEngine.Object.FindObjectsByType<ResourceNode>().ToList();

            foreach (var p in data.houses)
                BuildingPlacement.CreateHouse(p);
            if (data.houses.Count > 0)
                NavGrid.I.Rebuild();

            foreach (var u in Unit.All.ToArray())
            {
                u.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(u.gameObject);
            }
            SelectionController.I.Clear();
            Unit wagon = null;
            var escorts = new List<EscortOrder>();
            foreach (var st in data.units)
            {
                var u = WorldBuilder.SpawnUnit(st.kind, st.faction, st.position, st.title);
                u.transform.rotation = Quaternion.Euler(0, st.yaw, 0);
                u.health = Mathf.Min(st.health, u.maxHealth);
                var combat = u.GetComponent<CombatAgent>();
                if (combat)
                    combat.rally = st.rally;
                if (st.escorting)
                    escorts.Add(u.GetComponent<EscortOrder>());
                var worker = u.GetComponent<WorkerAgent>();
                if (worker)
                {
                    worker.carried = st.carried;
                    worker.cargo = st.carriedKind;
                    var node = st.gathering
                        ? nodes.FirstOrDefault(x => (x.transform.position - st.node).sqrMagnitude < .01f)
                        : null;
                    if (node)
                        worker.Gather(node);
                }
                var trade = u.GetComponent<TradeRoute>();
                if (trade)
                {
                    wagon = u;
                    trade.source = stores.FirstOrDefault(x => x.title == st.source);
                    trade.destination = stores.FirstOrDefault(x => x.title == st.destination);
                    trade.resource = st.resource;
                    trade.carriedResource = st.carriedKind;
                    trade.cargo = st.cargo;
                    trade.outbound = st.outbound;
                    trade.paused = st.paused || !trade.source || !trade.destination;
                    if (!trade.paused)
                        u.Motor.Go(trade.outbound ? trade.destination.Dock : trade.source.Dock);
                }
            }
            foreach (var e in escorts)
                if (e)
                    e.wagon = wagon;

            foreach (var c in CorpseSystem.I.remains)
                if (c)
                    UnityEngine.Object.Destroy(c.gameObject);
            CorpseSystem.I.remains.Clear();
            foreach (var st in data.corpses)
            {
                var c = CorpseSystem.I.Create(st.kind, st.faction, st.position);
                c.diedAt = Time.time - st.age;
                if (st.state != RemainsState.Exposed)
                    c.Apply(
                        st.state == RemainsState.Buried ? CorpseAction.Bury
                            : st.state == RemainsState.Burned ? CorpseAction.Burn
                            : CorpseAction.Hide,
                        false
                    );
            }
        }
    }
}
