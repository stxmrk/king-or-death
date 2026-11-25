using UnityEngine;

namespace KingOrDeath
{
    public enum RemainsState
    {
        Exposed,
        Carried,
        Hidden,
        Buried,
        Burned,
        Decayed,
    }

    public enum CorpseAction
    {
        Bury,
        Burn,
        Hide,
        Carry,
        Leave,
    }

    public class Corpse : MonoBehaviour
    {
        public UnitKind kind;
        public float diedAt;
        public RemainsState state;
        public bool claimed;
        public float Influence =>
            state == RemainsState.Exposed ? 1
            : state == RemainsState.Hidden ? .25f
            : 0;

        public void Apply(CorpseAction action)
        {
            claimed = false;
            if (action == CorpseAction.Carry)
            {
                state = RemainsState.Carried;
                claimed = true;
                return;
            }
            state =
                action == CorpseAction.Bury ? RemainsState.Buried
                : action == CorpseAction.Burn ? RemainsState.Burned
                : action == CorpseAction.Hide ? RemainsState.Hidden
                : RemainsState.Exposed;
            if (state == RemainsState.Buried || state == RemainsState.Burned)
            {
                foreach (var r in GetComponentsInChildren<Renderer>())
                    r.enabled = false;
                var marker = ModelFactory.Part(
                    state == RemainsState.Buried ? "Grave marker" : "Ashes",
                    PrimitiveType.Cube,
                    transform,
                    Vector3.zero,
                    state == RemainsState.Buried
                        ? new Vector3(.2f, .8f, .15f)
                        : new Vector3(1.4f, .06f, .6f),
                    ModelFactory.Material(
                        state == RemainsState.Buried
                            ? ModelFactory.Wood
                            : new Color(.12f, .12f, .12f)
                    )
                );
                marker.transform.position = transform.position + Vector3.up * .2f;
            }
            if (state == RemainsState.Hidden)
                foreach (var r in GetComponentsInChildren<Renderer>())
                    r.sharedMaterial = ModelFactory.Material(new Color(.2f, .28f, .14f));
            GameSession.I.Notify(
                "Remains: " + state + ". Scent influence " + Influence.ToString("0.00") + "."
            );
        }
    }
}
