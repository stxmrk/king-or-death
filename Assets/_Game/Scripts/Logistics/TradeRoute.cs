using UnityEngine;

namespace KingOrDeath
{
    public class TradeRoute : MonoBehaviour
    {
        public ResourceStore source,
            destination;
        public ResourceKind resource = ResourceKind.Wood,
            carriedResource;
        public int cargo;
        public bool paused = true;
        public bool outbound;
        public string state = "ts.NoRoute"; // Loc key, {0} = resource
        public Unit escort;
        Unit unit;
        float timer;
        public int Delivered { get; private set; }

        void Awake()
        {
            unit = GetComponent<Unit>();
        }

        public bool SetRoute(ResourceStore from, ResourceStore to)
        {
            if (
                !from
                || !to
                || from == to
                || from.owner != unit.faction
                || to.owner != unit.faction
            )
                return false;
            source = from;
            destination = to;
            paused = false;
            outbound = cargo > 0;
            unit.Motor.Go(outbound ? to.Dock : from.Dock);
            state = "ts.ToSource";
            return true;
        }

        public void Toggle()
        {
            paused = !paused;
            if (paused)
                unit.Motor.Stop();
            else if (source && destination)
                unit.Motor.Go(outbound ? destination.Dock : source.Dock);
        }

        void Update()
        {
            if (paused || !source || !destination)
            {
                if (paused)
                    state = "ts.Stopped";
                return;
            }
            if (source.owner != unit.faction || destination.owner != unit.faction)
            {
                paused = true;
                unit.Motor.Stop();
                state = "ts.Suspended";
                GameSession.I.Notify(Loc.T("n.route.lost"), true, transform.position);
                return;
            }
            if (unit.Motor.blocked)
            {
                state = "ts.Blocked";
                return;
            }
            var goal = outbound ? destination : source;
            if (Vector3.Distance(transform.position, goal.Dock) > 3)
            {
                state = outbound ? "ts.Delivering" : "ts.Returning";
                return;
            }
            timer += Time.deltaTime;
            if (timer < 1.5f)
                return;
            timer = 0;
            if (outbound)
            {
                goal.Add(carriedResource, cargo);
                Delivered += cargo;
                GameSession.I.stats.delivered += cargo;
                if (cargo > 0)
                {
                    if (!GameSession.I.milestones.Contains("delivery"))
                        GameSession.I.Announce("ann.delivery", Loc.Name(goal.title), "unit_wagon");
                    GameSession.I.Mark("delivery");
                    GameSession.I.Notify(
                        Loc.F("n.delivered", cargo, Loc.T("res." + carriedResource), Loc.Name(goal.title)),
                        false,
                        goal.Dock
                    );
                }
                cargo = 0;
                outbound = false;
                unit.Motor.Go(source.Dock);
            }
            else
            {
                carriedResource = resource;
                cargo = source.Take(resource, GameSession.I.config.wagonCapacity);
                if (cargo == 0)
                {
                    state = "ts.Waiting";
                    return;
                }
                outbound = true;
                GameSession.I.Mark("load");
                unit.Motor.Go(destination.Dock);
            }
        }

        public void DropCargo()
        {
            if (cargo <= 0)
                return;
            var go = ModelFactory.Crate("Salvage", transform.position);
            var node = go.AddComponent<ResourceNode>();
            node.resource = carriedResource;
            node.remaining = cargo / 2;
            cargo = 0;
            GameSession.I.Notify(Loc.T("n.wagon"), true, transform.position);
        }
    }
}
