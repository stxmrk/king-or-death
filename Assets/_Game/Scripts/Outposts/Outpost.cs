using UnityEngine;

namespace KingOrDeath
{
    public class Outpost : MonoBehaviour
    {
        public ResourceStore store;
        public float progress;
        public Faction challenger = Faction.Neutral;
        public bool contested;
        public Renderer flag;
        float next;

        void Update()
        {
            if (Time.time < next)
                return;
            next = Time.time + .2f;
            Tick(.2f);
        }

        public void ShowOwner()
        {
            if (flag)
                flag.sharedMaterial = ModelFactory.Material(
                    store.owner == Faction.Player ? ModelFactory.Teal
                        : store.owner == Faction.Enemy ? ModelFactory.Red
                        : ModelFactory.Gold
                );
        }

        public void Tick(float dt)
        {
            bool player = false,
                enemy = false;
            foreach (var u in Unit.All)
                if (
                    u.kind == UnitKind.Swordsman
                    && Vector3.Distance(u.transform.position, transform.position) < 7
                )
                {
                    player |= u.faction == Faction.Player;
                    enemy |= u.faction == Faction.Enemy;
                }
            contested = player && enemy;
            if (contested)
                return;
            var side =
                player ? Faction.Player
                : enemy ? Faction.Enemy
                : Faction.Neutral;
            if (side == Faction.Neutral || side == store.owner)
            {
                progress = Mathf.Max(0, progress - dt * .1f);
                return;
            }
            if (challenger != side)
            {
                progress = 0;
                challenger = side;
            }
            progress += dt / GameSession.I.config.captureSeconds;
            if (progress >= 1)
            {
                store.owner = side;
                progress = 0;
                ShowOwner();
                GameSession.I.Notify(
                    Loc.F("n.captured", Loc.Name(store.title), Loc.T("faction." + side)),
                    side != Faction.Player,
                    transform.position
                );
                if (side == Faction.Player)
                    GameSession.I.Mark("capture");
                GameSession.I.Announce(
                    side == Faction.Player ? "ann.captured" : "ann.lost",
                    Loc.Name(store.title),
                    side == Faction.Player ? "unit_store" : "unit_ashen"
                );
            }
        }
    }
}
