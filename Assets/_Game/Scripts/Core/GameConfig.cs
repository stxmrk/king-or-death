using UnityEngine;
namespace KingOrDeath {
    public enum Faction { Neutral, Player, Enemy, Wildlife }
    public enum UnitKind { Swordsman, Worker, Wagon, Wolf }
    public enum ResourceKind { Wood, Stone, Food }
    [CreateAssetMenu(menuName="KING OR DEATH/Game configuration")]
    public class GameConfig : ScriptableObject {
        public float captureSeconds=14, ecologyInterval=2, scentDelay=16, scentRadius=65;
        public int remainsThreshold=2;
        public float keepRadius=24, outpostRadius=15, harvestSeconds=3, cleanupSeconds=4;
        public int workerCapacity=8, wagonCapacity=40;
        public float soldierSpeed=5, workerSpeed=4, wagonSpeed=3.4f, wolfSpeed=6;
    }
}
