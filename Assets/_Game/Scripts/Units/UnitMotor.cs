using System.Collections.Generic;
using UnityEngine;

namespace KingOrDeath
{
    public class UnitMotor : MonoBehaviour
    {
        public float speed = 5;
        public float radius = .6f;
        static readonly List<UnitMotor> All = new List<UnitMotor>();
        public bool Arrived => path == null || index >= path.Count;
        public bool blocked;
        public Vector3 destination;
        List<Vector3> path;
        int index,
            revision;
        float retry;

        public bool Go(Vector3 point)
        {
            destination = point;
            path = NavGrid.I.Path(transform.position, point);
            index = 0;
            revision = NavGrid.I.revision;
            blocked = path == null;
            retry = Time.time + 2;
            return !blocked;
        }

        public void Stop()
        {
            path = null;
            blocked = false;
        }

        void Awake()
        {
            var unit = GetComponent<Unit>();
            if (unit && unit.kind == UnitKind.Wagon)
                radius = 1.4f;
        }

        void OnEnable() => All.Add(this);

        void OnDisable() => All.Remove(this);

        // Soft separation so units stop stacking; only ever moves onto walkable cells.
        // shortcut: O(n^2) over all motors, fine for ~300 units; switch to a spatial hash beyond that.
        void Separate()
        {
            var push = Vector3.zero;
            int me = All.IndexOf(this);
            for (int i = 0; i < All.Count; i++)
            {
                var other = All[i];
                if (i == me)
                    continue;
                var d = transform.position - other.transform.position;
                d.y = 0;
                float min = radius + other.radius,
                    sq = d.sqrMagnitude;
                if (sq >= min * min)
                    continue;
                float dist = Mathf.Sqrt(sq);
                var dir =
                    dist > 1e-4f
                        ? d / dist
                        // Exact overlap: split by list order so the pair moves apart, not together.
                        : (me < i ? Vector3.right : Vector3.left);
                push += dir * (min - dist);
            }
            if (push == Vector3.zero)
                return;
            var next =
                transform.position
                + Vector3.ClampMagnitude(push * .5f, speed * .5f * Time.deltaTime);
            if (NavGrid.I.Walkable(next))
                transform.position = next;
        }

        void Update()
        {
            Separate();
            if (blocked)
            {
                if (Time.time >= retry)
                    Go(destination);
                return;
            }
            if (Arrived)
                return;
            if (revision != NavGrid.I.revision)
            {
                Go(destination);
                return;
            }
            var delta = path[index] - transform.position;
            delta.y = 0;
            if (delta.magnitude < .15f)
            {
                index++;
                return;
            }
            transform.position +=
                delta.normalized * Mathf.Min(speed * Time.deltaTime, delta.magnitude);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(delta),
                Time.deltaTime * 10
            );
        }
    }
}
