using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KingOrDeath.Tests
{
    // Pure-logic checks. AddComponent in edit mode skips Awake/OnEnable, so setup calls what Awake would.
    public class SimulationTests
    {
        NavGrid grid;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            grid = new GameObject("Nav").AddComponent<NavGrid>();
            NavGrid.I = grid;
            grid.Rebuild();
        }

        static ResourceStore Store(Vector3 p, float radius, int wood = 100, int stone = 50)
        {
            var s = new GameObject("Store").AddComponent<ResourceStore>();
            s.transform.position = p;
            s.title = "Keep";
            s.radius = radius;
            s.wood = wood;
            s.stone = stone;
            return s;
        }

        [Test]
        public void PathAcrossRiverUsesBridgeAndNeverCutsCorners()
        {
            var path = grid.Path(new Vector3(-20, 0, 20), new Vector3(30, 0, 20));
            Assert.IsNotNull(path);
            Assert.IsTrue(path.Exists(p => p.x > 5 && p.x < 13 && Mathf.Abs(p.z + 5) <= 4));
            for (int i = 0; i < path.Count; i++)
            {
                Assert.IsFalse(NavGrid.River(path[i]), "path enters river at " + path[i]);
                if (i == 0)
                    continue;
                var a = NavGrid.CellAt(path[i - 1]);
                var d = NavGrid.CellAt(path[i]) - a;
                Assert.LessOrEqual(Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)), 1);
                if (d.x != 0 && d.y != 0)
                {
                    Assert.IsTrue(grid.Walkable(a + new Vector2Int(d.x, 0)));
                    Assert.IsTrue(grid.Walkable(a + new Vector2Int(0, d.y)));
                }
            }
        }

        [Test]
        public void BlockedBridgeLeavesNoPath()
        {
            grid.Block(new Vector3(9, 0, -5), new Vector2(10, 10));
            Assert.IsNull(grid.Path(new Vector3(-20, 0, 20), new Vector3(30, 0, 20)));
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(8)]
        [TestCase(30)]
        public void FormationIsCentredAndDistinct(int count)
        {
            var sum = Vector3.zero;
            var seen = new System.Collections.Generic.HashSet<Vector3>();
            for (int i = 0; i < count; i++)
            {
                var o = SelectionController.FormationOffset(i, count);
                sum += o;
                Assert.IsTrue(seen.Add(o), "duplicate slot " + o);
                Assert.LessOrEqual(o.magnitude, Mathf.Sqrt(count) * 1.8f);
            }
            // Partial last row skews the mean slightly, never by more than half a row.
            Assert.LessOrEqual((sum / count).magnitude, .9f);
        }

        [Test]
        public void StoreTakeClampsAndSpendIsAtomic()
        {
            var s = Store(Vector3.zero, 10, wood: 30, stone: 5);
            Assert.AreEqual(30, s.Take(ResourceKind.Wood, 100));
            Assert.AreEqual(0, s.Take(ResourceKind.Stone, -4));
            s.Add(ResourceKind.Stone, -10);
            Assert.AreEqual(5, s.stone);
            s.wood = 60;
            Assert.IsFalse(s.Spend(50, 20));
            Assert.AreEqual(60, s.wood, "failed spend must not deduct");
            Assert.IsTrue(s.Spend(50, 5));
            Assert.AreEqual((10, 0), (s.wood, s.stone));
        }

        [Test]
        public void PlacementValidInsideOwnTerritory()
        {
            var keep = Store(new Vector3(-30, 0, 20), 24);
            Assert.IsTrue(TerritorySystem.Valid(new Vector3(-30, 0, 26), out var store, out _));
            Assert.AreSame(keep, store);
        }

        [Test]
        public void PlacementRejectsOutsideEnemyRoadBuildingsAndPoverty()
        {
            var keep = Store(new Vector3(-30, 0, 20), 30);
            Assert.IsFalse(TerritorySystem.Valid(new Vector3(30, 0, 30), out _, out var r));
            Assert.AreEqual(Loc.T("pl.outside"), r);

            Assert.IsFalse(TerritorySystem.Valid(new Vector3(-30, 0, -1), out _, out r));
            Assert.AreEqual(Loc.T("pl.blocked"), r);

            var b = new GameObject("House").AddComponent<Building>();
            b.transform.position = new Vector3(-30, 0, 30);
            Assert.IsFalse(TerritorySystem.Valid(new Vector3(-30, 0, 26), out _, out r));
            Assert.AreEqual(Loc.T("pl.blocked"), r);
            Object.DestroyImmediate(b.gameObject);

            keep.wood = 49;
            Assert.IsFalse(TerritorySystem.Valid(new Vector3(-30, 0, 26), out _, out r));
            StringAssert.StartsWith(Loc.T("pl.cost").Split(new[] { "{0}" }, System.StringSplitOptions.None)[0], r);

            keep.wood = 100;
            keep.owner = Faction.Enemy;
            Assert.IsFalse(TerritorySystem.Valid(new Vector3(-30, 0, 26), out _, out r));
            Assert.AreEqual(Loc.T("pl.outside"), r);
        }
    }
}
