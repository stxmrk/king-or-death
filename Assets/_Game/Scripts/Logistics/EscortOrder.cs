using UnityEngine;

namespace KingOrDeath
{
    public class EscortOrder : MonoBehaviour
    {
        public Unit wagon;
        float next;

        void Update()
        {
            if (!wagon || Time.time < next)
                return;
            next = Time.time + 1;
            var combat = GetComponent<CombatAgent>();
            if (combat && combat.target)
                return;
            if (Vector3.Distance(transform.position, wagon.transform.position) > 5)
                GetComponent<UnitMotor>().Go(wagon.transform.position + Vector3.left * 3);
        }
    }
}
