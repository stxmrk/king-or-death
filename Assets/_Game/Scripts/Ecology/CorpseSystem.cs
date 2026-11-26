using System.Collections.Generic;
using UnityEngine;

namespace KingOrDeath
{
    public class CorpseSystem : MonoBehaviour
    {
        public static CorpseSystem I;
        public readonly List<Corpse> remains = new List<Corpse>();

        void Awake()
        {
            I = this;
        }

        public Corpse Create(Unit unit)
        {
            var corpse = Create(unit.kind, unit.faction, unit.transform.position);
            corpse.name = "Remains — " + unit.title;
            GameSession.I.Mark("remains");
            return corpse;
        }

        public Corpse Create(UnitKind kind, Faction faction, Vector3 position)
        {
            var go = new GameObject("Remains");
            go.transform.position = new Vector3(position.x, .18f, position.z);
            var body = ModelFactory.Part(
                "Body",
                PrimitiveType.Capsule,
                go.transform,
                Vector3.zero,
                new Vector3(.65f, .9f, .55f),
                ModelFactory.Material(
                    faction == Faction.Player ? ModelFactory.Teal : ModelFactory.Red
                )
            );
            body.transform.localRotation = Quaternion.Euler(0, 0, 90);
            ModelFactory.Part(
                "Head",
                PrimitiveType.Sphere,
                go.transform,
                new Vector3(.95f, 0, 0),
                Vector3.one * .4f,
                ModelFactory.Material(new Color(.65f, .53f, .4f))
            );
            var hit = go.AddComponent<BoxCollider>();
            hit.size = new Vector3(2.3f, .7f, 1);
            var corpse = go.AddComponent<Corpse>();
            corpse.kind = kind;
            corpse.faction = faction;
            corpse.diedAt = Time.time;
            remains.Add(corpse);
            return corpse;
        }
    }
}
