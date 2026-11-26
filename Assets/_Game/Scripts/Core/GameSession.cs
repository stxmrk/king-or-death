using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KingOrDeath
{
    public enum Outcome
    {
        Playing,
        Victory,
        Defeat,
    }

    public class LogEntry
    {
        public string text;
        public float at, // unscaled time, drives the HUD fade
            clock; // match time shown next to the entry
        public bool alert;
        public Vector3? where;
    }

    public class GameSession : MonoBehaviour
    {
        public const int LogSize = 6;
        public const float AutosaveSeconds = 180;
        public static readonly string[] Objectives =
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

        public static GameSession I { get; private set; }
        public static event Action<Outcome> Ended;
        // Big centre-screen headline: title Loc key, already localized subtitle, HUD icon key (Hud.uss "icon-*").
        public static event Action<string, string, string> Announced;
        public GameConfig config;
        public ResourceStore keep;
        public Outpost[] posts;
        public Vector3 den = new Vector3(38, 0, 32);

        [NonSerialized]
        public HashSet<string> milestones = new HashSet<string>();

        [NonSerialized]
        public MatchStats stats = new MatchStats();

        [NonSerialized]
        public readonly List<LogEntry> log = new List<LogEntry>();
        public string notice;
        public bool raidCalled;
        public float playTime;
        public float speed = 1;
        public Outcome outcome;
        float nextCheck,
            nextAutosave;

        void Awake()
        {
            I = this;
            Time.timeScale = 1;
        }

        void Start()
        {
            var pending = SaveSystem.Pending;
            SaveSystem.Pending = null;
            if (pending != null)
            {
                SaveSystem.Apply(pending);
                Notify(Loc.F("n.loaded", pending.SavedAt.ToString("g", SaveSlotsView.Culture)));
            }
            else
                Notify(Loc.T("n.start"));
            nextAutosave = playTime + AutosaveSeconds;
        }

        void OnDestroy()
        {
            if (I == this)
                I = null;
        }

        void Update()
        {
            playTime += Time.deltaTime;
            if (RuntimeVerification.Active || outcome != Outcome.Playing || Time.time < nextCheck)
                return;
            nextCheck = Time.time + 1;
            if (Array.TrueForAll(Objectives, milestones.Contains))
                End(Outcome.Victory);
            else if (!Unit.All.Exists(u => u.faction == Faction.Player && u.kind != UnitKind.Wagon))
                End(Outcome.Defeat);
            else if (playTime >= nextAutosave)
            {
                nextAutosave = playTime + AutosaveSeconds;
                TrySave(SaveSystem.Auto, SaveSlotsView.Kind(SaveSystem.Auto));
            }
        }

        void End(Outcome result)
        {
            outcome = result;
            Time.timeScale = 0;
            Ended?.Invoke(result);
        }

        public bool TrySave(string id, string label)
        {
            if (outcome == Outcome.Defeat)
                return false;
            try
            {
                SaveSystem.Save(id, label);
                Notify(Loc.T("hud.n.saved") + " · " + label);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Notify(Loc.F("n.savefail", e.Message), true);
                return false;
            }
        }

        // Adds a log entry; a position lets the HUD ping it on the minimap and jump the camera there.
        public void Notify(string message, bool alert = false, Vector3? where = null)
        {
            notice = message;
            log.Add(new LogEntry { text = message, at = Time.unscaledTime, clock = playTime, alert = alert, where = where });
            if (log.Count > LogSize)
                log.RemoveAt(0);
            Debug.Log("[Frontier] " + message);
        }

        public void Announce(string titleKey, string subtitle, string icon)
        {
            Announced?.Invoke(titleKey, subtitle, icon);
        }

        public void Mark(string key)
        {
            milestones.Add(key);
        }

        public void Pause()
        {
            Time.timeScale = Time.timeScale == 0 ? speed : 0;
        }

        public void SetSpeed(float value)
        {
            speed = value;
            Time.timeScale = value;
        }

        public void Restart()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void Raid()
        {
            if (raidCalled)
                return;
            raidCalled = true;
            for (int i = 0; i < 5; i++)
            {
                var u = WorldBuilder.SpawnUnit(
                    UnitKind.Swordsman,
                    Faction.Enemy,
                    new Vector3(50 + i * 1.5f, 0, -9 + i),
                    "Ashen raider"
                );
                u.GetComponent<CombatAgent>().rally = new Vector3(-7, 0, -5);
            }
            Notify(Loc.T("n.raid"), true, new Vector3(50, 0, -5));
            Announce("ann.raid", Loc.T("ann.raid.sub"), "unit_ashen");
        }
    }
}
