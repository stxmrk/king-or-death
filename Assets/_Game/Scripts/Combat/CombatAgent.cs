using UnityEngine;

namespace KingOrDeath
{
    public class CombatAgent : MonoBehaviour
    {
        public float damage = 14,
            range = 2.2f,
            interval = 1,
            awareness = 8;
        public Unit target;
        public Vector3 rally;
        public bool hunting;
        Unit unit;
        float nextHit,
            nextThink;
        bool commanded;

        void Awake()
        {
            unit = GetComponent<Unit>();
        }

        public void Attack(Unit victim)
        {
            target = victim;
            commanded = true;
        }

        public void ClearTarget()
        {
            target = null;
            commanded = false;
        }

        void Update()
        {
            if (Time.time >= nextThink)
            {
                nextThink = Time.time + .35f;
                bool allowed = unit.kind != UnitKind.Wolf || hunting;
                if (!target && allowed && (unit.Motor.Arrived || unit.faction != Faction.Player))
                {
                    float nearest = awareness;
                    foreach (var other in Unit.All)
                    {
                        if (!unit.IsHostile(other))
                            continue;
                        float d = Vector3.Distance(transform.position, other.transform.position);
                        if (d < nearest)
                        {
                            nearest = d;
                            target = other;
                        }
                    }
                }
                if (target)
                {
                    if (
                        !commanded
                        && Vector3.Distance(transform.position, target.transform.position)
                            > awareness * 2
                    )
                    {
                        target = null;
                    }
                    else if (
                        Vector3.Distance(transform.position, target.transform.position) > range
                    )
                        unit.Motor.Go(target.transform.position);
                }
                else if (
                    unit.faction == Faction.Enemy
                    && rally != Vector3.zero
                    && unit.Motor.Arrived
                )
                    unit.Motor.Go(rally);
            }
            if (!target)
                return;
            if (Vector3.Distance(transform.position, target.transform.position) <= range)
            {
                unit.Motor.Stop();
                transform.LookAt(
                    new Vector3(
                        target.transform.position.x,
                        transform.position.y,
                        target.transform.position.z
                    )
                );
                if (Time.time >= nextHit)
                {
                    nextHit = Time.time + interval;
                    target.Damage(damage);
                    GameSession.I.Mark("battle");
                }
            }
        }
    }
}
