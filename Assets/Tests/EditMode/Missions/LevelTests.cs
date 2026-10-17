using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class LevelTests
    {
        // Row 0 is the top, so the ground is y = 0 and the top row is y = 3.
        static readonly string[] Small =
        {
            "........",
            ".---..d.",
            "P.s.=..F",
            "########",
        };

        [Test]
        public void TheSizeComesFromTheMap()
        {
            var level = new Level(Small);
            Assert.AreEqual(8, level.Width);
            Assert.AreEqual(4, level.Height);
        }

        [Test]
        public void TilesAreReadWithTheFirstRowOnTop()
        {
            var level = new Level(Small);
            Assert.AreEqual(Level.Ground, level.Get(1, 0));
            Assert.AreEqual(Level.Block, level.Get(4, 1));
            Assert.AreEqual(Level.Bridge, level.Get(2, 2));
            Assert.AreEqual(Level.Empty, level.Get(1, 3));
        }

        [Test]
        public void MarkersLeaveEmptyTiles()
        {
            var level = new Level(Small);
            Assert.AreEqual(Level.Empty, level.Get(0, 1)); // P
            Assert.AreEqual(Level.Empty, level.Get(2, 1)); // s
            Assert.AreEqual(Level.Empty, level.Get(6, 2)); // d
            Assert.AreEqual(Level.Empty, level.Get(7, 1)); // F
        }

        [Test]
        public void ThePlayerStartsAtTheFeetOfTheP()
        {
            Assert.AreEqual(new Vector2(0.5f, 1f), new Level(Small).PlayerStart);
        }

        [Test]
        public void SpawnsAreSortedLeftToRight()
        {
            var spawns = new Level(Small).Spawns;
            Assert.AreEqual(3, spawns.Count);
            Assert.AreEqual(SpawnKind.Rifleman, spawns[0].Kind);
            Assert.AreEqual(new Vector2(2.5f, 1f), spawns[0].Pos);
            Assert.AreEqual(SpawnKind.Drone, spawns[1].Kind);
            Assert.AreEqual(new Vector2(6.5f, 2f), spawns[1].Pos);
            Assert.AreEqual(SpawnKind.Flag, spawns[2].Kind);
            Assert.AreEqual(new Vector2(7.5f, 1f), spawns[2].Pos);
        }

        [Test]
        public void EachMarkerMakesItsKind()
        {
            var spawns = new Level(new[] { "srdTHB+FUWMxZVILK", "#################" }).Spawns;
            var kinds = new[]
            {
                SpawnKind.Rifleman, SpawnKind.Runner, SpawnKind.Drone, SpawnKind.Tank,
                SpawnKind.CrateHeavy, SpawnKind.CrateBombs, SpawnKind.Medkit, SpawnKind.Flag, SpawnKind.OneUp,
                SpawnKind.TowerScenery, SpawnKind.MotorPool, SpawnKind.Barrel, SpawnKind.LiveFence, SpawnKind.SteamVent,
                SpawnKind.Icicles, SpawnKind.Lift, SpawnKind.Checkpoint,
            };
            Assert.AreEqual(kinds.Length, spawns.Count);
            for (int i = 0; i < kinds.Length; i++) Assert.AreEqual(kinds[i], spawns[i].Kind);
        }

        [Test]
        public void UnknownCharactersAreEmpty()
        {
            var level = new Level(new[] { "?%", "##" });
            Assert.AreEqual(Level.Empty, level.Get(0, 1));
            Assert.AreEqual(Level.Empty, level.Get(1, 1));
            Assert.AreEqual(0, level.Spawns.Count);
        }

        [Test]
        public void ShortRowsArePaddedWithEmptyTiles()
        {
            var level = new Level(new[] { "#", "###" });
            Assert.AreEqual(3, level.Width);
            Assert.AreEqual(Level.Empty, level.Get(1, 1));
            Assert.AreEqual(Level.Ground, level.Get(1, 0));
        }

        [Test]
        public void OutsideTheMapIsWallsAndFloorBelowAndSkyAbove()
        {
            var level = new Level(Small);
            Assert.AreEqual(Level.Ground, level.Get(-1, 1));
            Assert.AreEqual(Level.Ground, level.Get(8, 1));
            Assert.AreEqual(Level.Ground, level.Get(3, -1));
            Assert.AreEqual(Level.Empty, level.Get(3, 4));
        }

        [Test]
        public void GroundAndSandbagsAreSolidButBridgesAreNot()
        {
            var level = new Level(Small);
            Assert.IsTrue(level.IsSolid(1, 0));
            Assert.IsTrue(level.IsSolid(4, 1));
            Assert.IsFalse(level.IsSolid(2, 2));
            Assert.IsFalse(level.IsSolid(1, 1));
            Assert.IsTrue(level.IsSolidAt(new Vector2(4.5f, 1.5f)));
            Assert.IsFalse(level.IsSolidAt(new Vector2(2.5f, 2.5f)));
        }

        [Test]
        public void TopSurfaceIsTheHighestStandableTile()
        {
            var level = new Level(Small);
            Assert.AreEqual(3f, level.TopSurfaceY(3.5f)); // bridge
            Assert.AreEqual(2f, level.TopSurfaceY(4.5f)); // sandbags
            Assert.AreEqual(1f, level.TopSurfaceY(0.5f)); // ground
            Assert.AreEqual(1f, level.TopSurfaceY(-5f));  // clamped to the first column
            Assert.AreEqual(1f, level.TopSurfaceY(100f)); // clamped to the last column
        }
    }
}
