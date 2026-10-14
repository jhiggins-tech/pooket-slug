using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class TilePhysicsTests
    {
        static readonly float HalfWidth = Content.Trooper.HalfWidth, Height = Content.Trooper.StandHeight;
        const float Dt = 0.1f;

        static readonly string[] Flat =
        {
            "..........",
            "..........",
            "..........",
            "##########",
        };

        // A bridge at y = 2 (standing height 3), with sandbags at its right end.
        static readonly string[] Bridge =
        {
            "..........",
            "....---=..",
            "..........",
            "##########",
        };

        static MoveResult Move(string[] map, ref Vector2 feet, ref Vector2 vel, bool dropThrough = false) =>
            TilePhysics.Move(new Level(map), ref feet, HalfWidth, Height, ref vel, Dt, dropThrough);

        [Test]
        public void FallingOntoTheGroundLands()
        {
            var feet = new Vector2(5f, 1.2f);
            var vel = new Vector2(0f, -5f);
            MoveResult r = Move(Flat, ref feet, ref vel);
            Assert.IsTrue(r.Grounded);
            Assert.AreEqual(1f, feet.y, 1e-5f);
            Assert.AreEqual(0f, vel.y);
        }

        [Test]
        public void NothingBelowMeansFalling()
        {
            var feet = new Vector2(5f, 3f);
            var vel = new Vector2(0f, -5f);
            MoveResult r = Move(Flat, ref feet, ref vel);
            Assert.IsFalse(r.Grounded);
            Assert.AreEqual(2.5f, feet.y, 1e-5f);
            Assert.AreEqual(-5f, vel.y);
        }

        [Test]
        public void RunningIntoAWallStops()
        {
            string[] map = { "..........", "..........", "......#...", "##########" };
            var feet = new Vector2(5f, 1f);
            var vel = new Vector2(10f, -0.4f);
            MoveResult r = Move(map, ref feet, ref vel);
            Assert.IsTrue(r.HitWall);
            Assert.AreEqual(6f - HalfWidth, feet.x, 1e-5f);
            Assert.AreEqual(0f, vel.x);
            Assert.IsTrue(r.Grounded);
        }

        [Test]
        public void RunningLeftIntoAWallStops()
        {
            string[] map = { "..........", "..........", "...#......", "##########" };
            var feet = new Vector2(4.5f, 1f);
            var vel = new Vector2(-10f, -0.4f);
            MoveResult r = Move(map, ref feet, ref vel);
            Assert.IsTrue(r.HitWall);
            Assert.AreEqual(4f + HalfWidth, feet.x, 1e-5f);
        }

        [Test]
        public void TheLevelsEdgesAreWalls()
        {
            var feet = new Vector2(0.5f, 1f);
            var vel = new Vector2(-10f, -0.4f);
            MoveResult r = Move(Flat, ref feet, ref vel);
            Assert.IsTrue(r.HitWall);
            Assert.AreEqual(HalfWidth, feet.x, 1e-5f);
        }

        [Test]
        public void JumpingIntoACeilingStops()
        {
            string[] map = { "..........", ".....#....", "..........", "..........", "##########" };
            var feet = new Vector2(5.5f, 1f);
            var vel = new Vector2(0f, 10f);
            MoveResult r = Move(map, ref feet, ref vel);
            Assert.IsTrue(r.HitCeiling);
            Assert.AreEqual(3f - Height, feet.y, 1e-5f);
            Assert.AreEqual(0f, vel.y);
        }

        [Test]
        public void ABridgeHoldsYouFromAbove()
        {
            var feet = new Vector2(5f, 3.2f);
            var vel = new Vector2(0f, -5f);
            MoveResult r = Move(Bridge, ref feet, ref vel);
            Assert.IsTrue(r.Grounded);
            Assert.AreEqual(3f, feet.y, 1e-5f);
        }

        [Test]
        public void YouJumpUpThroughABridge()
        {
            var feet = new Vector2(5f, 1f);
            var vel = new Vector2(0f, 10f);
            MoveResult r = Move(Bridge, ref feet, ref vel);
            Assert.IsFalse(r.HitCeiling);
            Assert.AreEqual(2f, feet.y, 1e-5f);
            Assert.AreEqual(10f, vel.y);
        }

        [Test]
        public void ABridgeDoesNotCatchYouFromInside()
        {
            var feet = new Vector2(5f, 2.5f);
            var vel = new Vector2(0f, -5f);
            MoveResult r = Move(Bridge, ref feet, ref vel);
            Assert.IsFalse(r.Grounded);
            Assert.AreEqual(2f, feet.y, 1e-5f);
        }

        [Test]
        public void DroppingThroughABridge()
        {
            var feet = new Vector2(5f, 3f);
            var vel = new Vector2(0f, -1f);
            MoveResult r = Move(Bridge, ref feet, ref vel, dropThrough: true);
            Assert.IsFalse(r.Grounded);
            Assert.AreEqual(2.9f, feet.y, 1e-5f);
        }

        [Test]
        public void OnBridgeIsOnlyTrueOverABridgeAlone()
        {
            var bridge = new Level(Bridge);
            Assert.IsTrue(TilePhysics.OnBridge(bridge, new Vector2(5f, 3f), HalfWidth));
            Assert.IsFalse(TilePhysics.OnBridge(bridge, new Vector2(6.9f, 3f), HalfWidth)); // half on the sandbags
            Assert.IsFalse(TilePhysics.OnBridge(bridge, new Vector2(2f, 3f), HalfWidth));   // over nothing
            Assert.IsFalse(TilePhysics.OnBridge(new Level(Flat), new Vector2(5f, 1f), HalfWidth));
        }
    }
}
