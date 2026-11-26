using UnityEngine;

namespace KingOrDeath
{
    public class WorkerAgent : MonoBehaviour
    {
        public ResourceNode node;
        public ResourceStore store;
        public int carried;
        public ResourceKind cargo;
        public Corpse corpse;
        public CorpseAction cleanup = CorpseAction.Bury;
        public string state = "ws.Idle"; // Loc key: {0} carried, {1} cargo, {2} node resource
        Unit unit;
        float timer;
        bool delivering;

        void Awake()
        {
            unit = GetComponent<Unit>();
        }

        public void Cancel()
        {
            DropBody();
            if (corpse)
                corpse.claimed = false;
            corpse = null;
            node = null;
            state = "ws.Idle";
            timer = 0;
        }

        public void Gather(ResourceNode n)
        {
            Cancel();
            node = n;
            store = ResourceStore.NearestOwned(n.transform.position, unit.faction);
            delivering = carried > 0;
            state = "ws.ToResource";
            unit.Motor.Go(delivering && store ? store.Dock : n.transform.position);
        }

        public void Clean(Corpse c, CorpseAction action)
        {
            if (c.claimed)
                return;
            Cancel();
            corpse = c;
            c.claimed = true;
            cleanup = action;
            state = "ws." + action;
            unit.Motor.Go(c.transform.position);
        }

        void Update()
        {
            if (corpse)
            {
                if (cleanup == CorpseAction.Carry && corpse.state == RemainsState.Carried)
                {
                    corpse.transform.position = transform.position + Vector3.up;
                    return;
                }
                if (Vector3.Distance(transform.position, corpse.transform.position) < 3)
                {
                    timer += Time.deltaTime;
                    if (timer >= GameSession.I.config.cleanupSeconds)
                    {
                        corpse.Apply(cleanup);
                        if (cleanup != CorpseAction.Carry)
                            corpse = null;
                        timer = 0;
                        GameSession.I.Mark("cleanup");
                        if (cleanup != CorpseAction.Carry)
                            GameSession.I.stats.remainsCleared++;
                    }
                }
                return;
            }
            if (!node)
                return;
            if (!store || store.owner != unit.faction)
            {
                store = ResourceStore.NearestOwned(transform.position, unit.faction);
                if (!store)
                {
                    state = "ws.NoStore";
                    return;
                }
                delivering = carried > 0;
                unit.Motor.Go(delivering ? store.Dock : node.transform.position);
            }
            if (delivering)
            {
                state = "ws.Delivering";
                if (Vector3.Distance(transform.position, store.Dock) < 3)
                {
                    store.Add(cargo, carried);
                    GameSession.I.stats.harvested += carried;
                    carried = 0;
                    delivering = false;
                    GameSession.I.Mark("harvest");
                    unit.Motor.Go(node.transform.position);
                }
            }
            else if (Vector3.Distance(transform.position, node.transform.position) < 3)
            {
                state = "ws.Gathering";
                timer += Time.deltaTime;
                if (timer >= GameSession.I.config.harvestSeconds)
                {
                    timer = 0;
                    cargo = node.resource;
                    carried = node.Harvest(GameSession.I.config.workerCapacity);
                    if (carried == 0)
                    {
                        Cancel();
                        state = "ws.Exhausted";
                        return;
                    }
                    delivering = true;
                    unit.Motor.Go(store.Dock);
                }
            }
        }

        public void DropBody()
        {
            if (!corpse || corpse.state != RemainsState.Carried)
                return;
            corpse.transform.position = transform.position + transform.right * 1.5f;
            corpse.transform.position = new Vector3(
                corpse.transform.position.x,
                .18f,
                corpse.transform.position.z
            );
            corpse.state = RemainsState.Exposed;
            corpse.claimed = false;
            corpse = null;
            state = "ws.Idle";
        }

        void OnDisable()
        {
            if (corpse)
            {
                corpse.claimed = false;
                if (corpse.state == RemainsState.Carried)
                {
                    corpse.state = RemainsState.Exposed;
                    corpse.transform.position = new Vector3(
                        transform.position.x,
                        .18f,
                        transform.position.z
                    );
                }
            }
        }
    }
}
