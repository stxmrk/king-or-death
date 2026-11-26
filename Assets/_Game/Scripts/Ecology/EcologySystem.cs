using UnityEngine;

namespace KingOrDeath
{
    public interface IEcologyProcess
    {
        void TickEcology(float elapsed);
    }

    public class EcologySystem : MonoBehaviour, IEcologyProcess
    {
        public float scent;
        public bool wolvesHunting;
        float next;

        void Update()
        {
            if (Time.time < next)
                return;
            next = Time.time + GameSession.I.config.ecologyInterval;
            TickEcology(GameSession.I.config.ecologyInterval);
        }

        public void TickEcology(float elapsed)
        {
            var cfg = GameSession.I.config;
            Vector3 center = Vector3.zero;
            scent = 0;
            foreach (var corpse in CorpseSystem.I.remains)
            {
                if (!corpse || Time.time - corpse.diedAt < cfg.scentDelay)
                    continue;
                float distance = Vector3.Distance(corpse.transform.position, GameSession.I.den);
                if (distance > cfg.scentRadius)
                    continue;
                float weight = corpse.Influence * (1 - distance / cfg.scentRadius * .4f);
                scent += weight;
                center += corpse.transform.position * weight;
            }
            bool hunt = scent >= cfg.remainsThreshold * .65f;
            if (hunt && !wolvesHunting)
            {
                GameSession.I.Notify(Loc.T("n.wolves"), true, GameSession.I.den);
                GameSession.I.Mark("wolves");
                GameSession.I.Announce("ann.wolves", Loc.T("ann.wolves.sub"), "unit_wolf");
            }
            if (!hunt && wolvesHunting)
                GameSession.I.Notify(Loc.T("n.wolves.gone"), false, GameSession.I.den);
            wolvesHunting = hunt;
            foreach (var u in Unit.All)
                if (u.kind == UnitKind.Wolf)
                {
                    var fighter = u.GetComponent<CombatAgent>();
                    fighter.hunting = hunt;
                    if (!hunt)
                        fighter.ClearTarget();
                    if (!fighter.target)
                    {
                        var goal = hunt ? center / Mathf.Max(.01f, scent) : GameSession.I.den;
                        if (Vector3.Distance(u.transform.position, goal) > 4)
                            u.Motor.Go(goal);
                    }
                }
        }
    }
}
