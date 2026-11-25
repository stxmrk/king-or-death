using System.Collections.Generic;
using UnityEngine;

namespace KingOrDeath
{
    public class Unit : MonoBehaviour
    {
        public static readonly List<Unit> All = new List<Unit>();
        public UnitKind kind;
        public Faction faction;
        public string title;
        public float maxHealth = 100,
            health = 100;
        public bool selected;
        public UnitMotor Motor => GetComponent<UnitMotor>();

        public bool IsHostile(Unit other) =>
            other
            && faction != other.faction
            && faction != Faction.Neutral
            && other.faction != Faction.Neutral;

        void OnEnable()
        {
            if (!All.Contains(this))
                All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        public void Damage(float value)
        {
            health = Mathf.Max(0, health - value);
            if (health > 0)
                return;
            if (kind == UnitKind.Wagon)
                GetComponent<TradeRoute>().DropCargo();
            else
                CorpseSystem.I.Create(this);
            if (faction == Faction.Player)
                GameSession.I.Notify(title + " has fallen.");
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public void Move(Vector3 p)
        {
            var fighter = GetComponent<CombatAgent>();
            if (fighter)
                fighter.ClearTarget();
            var worker = GetComponent<WorkerAgent>();
            if (worker)
                worker.Cancel();
            var trade = GetComponent<TradeRoute>();
            if (trade)
                trade.paused = true;
            Motor.Go(p);
            GameSession.I.Mark("orders");
        }
    }
}
